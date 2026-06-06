using FFXIVHudPlugin.AetherPlates.Configuration;
using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Services;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using System.Numerics;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace FFXIVHudPlugin.AetherPlates.Core;

public sealed class NameplateManager
{
    internal enum NameplateCategory
    {
        Self,
        SelfCompanion,
        SelfPet,
        Party,
        PartyCompanion,
        PartyPet,
        Alliance,
        AlliancePet,
        Friend,
        FriendCompanion,
        FriendPet,
        OtherPc,
        OtherCompanion,
        OtherPet,
        EnemyUnengaged,
        EnemyEngaged,
        EnemyClaimed,
        EnemyUnclaimed,
        EnemyFeast,
        EnemyFeastPet,
        Npc,
        Object,
        Minion,
        HousingFurniture,
        HousingField,
        UnknownFriendly,
    }

    private readonly NameplateTracker tracker;
    private readonly NameplateRenderer renderer;
    private readonly IProjectionService projectionService;
    private readonly NativeNameplateAnchorService nativeAnchorService;
    private readonly ITextureProvider textureProvider;
    private readonly PluginConfiguration configuration;
    private readonly Dictionary<ulong, ActorStateCacheEntry> actorCache = new();
    private Dictionary<ulong, TrackedObject> currentObjectsById = new();
    private long frameCounter;

    private sealed class ActorStateCacheEntry
    {
        public required TrackedObject Tracked { get; init; }
        public required Vector2 LastAnchor { get; set; }
        public required NameplateCategory LastCategory { get; set; }
        public required long LastSeenFrame { get; set; }
        public required bool HadNativeAnchor { get; set; }
    }

    internal IReadOnlyDictionary<ulong, TrackedObject> CurrentObjectsById => this.currentObjectsById;

    public NameplateManager(
        NameplateTracker tracker,
        NameplateRenderer renderer,
        IProjectionService projectionService,
        NativeNameplateAnchorService nativeAnchorService,
        ITextureProvider textureProvider,
        PluginConfiguration configuration)
    {
        this.tracker = tracker;
        this.renderer = renderer;
        this.projectionService = projectionService;
        this.nativeAnchorService = nativeAnchorService;
        this.textureProvider = textureProvider;
        this.configuration = configuration;
    }

    public void UpdateAndDraw()
    {
        this.frameCounter++;
        if (!this.configuration.Enabled)
        {
            return;
        }

        if (!this.configuration.CategoryVisibility.IsAnyEnabled())
        {
            return;
        }

        this.tracker.Update();
        var tracked = this.tracker.CurrentFrame.Objects;
        var profile = this.configuration.GetActiveProfile();
        var offset = new Vector3(0f, this.configuration.VerticalOffset, 0f);
        var localPlayerId = 0UL;
        var ownerLookup = new Dictionary<ulong, TrackedObject>(tracked.Count);
        for (var i = 0; i < tracked.Count; i++)
        {
            var trackedObj = tracked[i];
            ownerLookup[trackedObj.ObjectId] = trackedObj;
            if (trackedObj.IsPlayerCharacter)
            {
                localPlayerId = trackedObj.ObjectId;
            }
        }
        this.currentObjectsById = ownerLookup;

        for (var i = 0; i < tracked.Count; i++)
        {
            var obj = tracked[i];
            if (!ShouldRenderObject(obj))
            {
                continue;
            }

            var nativeKind = this.nativeAnchorService.TryGetKind(obj.ObjectId, out var plateKind) ? plateKind : (NamePlateKind?)null;
            var category = NameplateCategoryResolver.ResolveForTrackedObject(obj, localPlayerId, nativeKind, ownerLookup);
            if (!this.IsCategoryEnabled(category))
            {
                continue;
            }

            if (this.RequiresNativePresence(category) && !this.nativeAnchorService.IsInCurrentNativeSet(obj.ObjectId))
            {
                // NPC/minion/object-like categories should only be rendered when the game currently has
                // an active native nameplate for that actor; this prevents phantom labels from cache/projection.
                continue;
            }

            var categoryVisual = this.configuration.GetVisualSettingsForCategory(category);
            var isHostile = category is NameplateCategory.EnemyUnengaged
                or NameplateCategory.EnemyEngaged
                or NameplateCategory.EnemyClaimed
                or NameplateCategory.EnemyUnclaimed
                or NameplateCategory.EnemyFeast
                or NameplateCategory.EnemyFeastPet;
            var isFriendly = !isHostile && category != NameplateCategory.Self;

            if (this.configuration.EnableDistanceCulling &&
                !this.IsInRangeForCategory(obj, category, isHostile, isFriendly))
            {
                continue;
            }

            if (!this.TryResolveAnchor(obj, offset, out var screen))
            {
                continue;
            }

            this.actorCache[obj.ObjectId] = new ActorStateCacheEntry
            {
                Tracked = obj,
                LastAnchor = screen,
                LastCategory = category,
                LastSeenFrame = this.frameCounter,
                HadNativeAnchor = this.nativeAnchorService.IsInCurrentNativeSet(obj.ObjectId),
            };

            var context = new NameplateContext(
                obj,
                profile,
                categoryVisual,
                this.textureProvider,
                screen,
                this.configuration.TemporaryGlobalScale,
                obj.IsTarget,
                obj.IsFocusTarget,
                false,
                obj.IsPartyMember,
                obj.IsAllianceMember,
                isHostile,
                isFriendly,
                obj.Distance);

            this.renderer.DrawNameplate(context, categoryVisual.EnabledWidgetIdsSet);
        }

        this.TrimActorCache();
    }

    private bool IsInRangeForCategory(
        TrackedObject obj,
        NameplateCategory category,
        bool isHostile,
        bool isFriendly)
    {
        var inCombatRangeActive = this.configuration.EnableDynamicCombatRange &&
                                  this.IsCombatRelevant(obj, category, isHostile);
        var enemyRange = inCombatRangeActive
            ? this.configuration.CombatEnemyMaxDistanceYalms
            : this.configuration.EnemyMaxDistanceYalms;
        var friendlyRange = inCombatRangeActive
            ? this.configuration.CombatFriendlyMaxDistanceYalms
            : this.configuration.FriendlyMaxDistanceYalms;

        if (category == NameplateCategory.Self)
        {
            return obj.Distance <= Math.Max(1f, this.configuration.PlayerMaxDistanceYalms);
        }

        if (isHostile)
        {
            return obj.Distance <= Math.Max(1f, enemyRange);
        }

        if (isFriendly || category is NameplateCategory.Party or NameplateCategory.Alliance or NameplateCategory.Friend or NameplateCategory.OtherPc or NameplateCategory.Self)
        {
            return obj.Distance <= Math.Max(1f, friendlyRange);
        }

        return true;
    }

    private bool IsCombatRelevant(TrackedObject obj, NameplateCategory category, bool isHostile)
    {
        if (category is NameplateCategory.EnemyUnengaged
            or NameplateCategory.EnemyEngaged
            or NameplateCategory.EnemyClaimed
            or NameplateCategory.EnemyUnclaimed
            or NameplateCategory.EnemyFeast
            or NameplateCategory.EnemyFeastPet || isHostile)
        {
            return true;
        }

        if (obj.CastInfo.IsCasting)
        {
            return true;
        }

        return obj.IsTarget;
    }

    private bool IsCategoryEnabled(NameplateCategory category)
    {
        var c = this.configuration.CategoryVisibility;
        return category switch
        {
            NameplateCategory.Self => c.Self,
            NameplateCategory.SelfCompanion => c.SelfCompanion,
            NameplateCategory.SelfPet => c.SelfPet,
            NameplateCategory.Party => c.PartyMember,
            NameplateCategory.PartyCompanion => c.PartyCompanion,
            NameplateCategory.PartyPet => c.PartyPet,
            NameplateCategory.Alliance => c.AllianceMember,
            NameplateCategory.AlliancePet => c.AlliancePet,
            NameplateCategory.Friend => c.Friend,
            NameplateCategory.FriendCompanion => c.FriendCompanion,
            NameplateCategory.FriendPet => c.FriendPet,
            NameplateCategory.OtherPc => c.OtherPc,
            NameplateCategory.OtherCompanion => c.OtherCompanion,
            NameplateCategory.OtherPet => c.OtherPet,
            NameplateCategory.EnemyUnengaged => c.EnemyUnengaged,
            NameplateCategory.EnemyEngaged => c.EnemyEngaged,
            NameplateCategory.EnemyClaimed => c.EnemyClaimed,
            NameplateCategory.EnemyUnclaimed => c.EnemyUnclaimed,
            NameplateCategory.EnemyFeast => c.EnemyFeast,
            NameplateCategory.EnemyFeastPet => c.EnemyFeastPet,
            NameplateCategory.Npc => c.Npc,
            NameplateCategory.Object => c.Object,
            NameplateCategory.Minion => c.Minion,
            NameplateCategory.HousingFurniture => c.HousingFurniture,
            NameplateCategory.HousingField => c.HousingField,
            // Unknown classifications should never be implicitly shown; this avoids random uncategorized NPC plates.
            NameplateCategory.UnknownFriendly => false,
            _ => false,
        };
    }

    private bool TryResolveAnchor(TrackedObject obj, Vector3 offset, out Vector2 screen)
    {
        if (this.nativeAnchorService.TryGetAnchor(obj.ObjectId, out screen) && this.IsValidAnchor(screen))
        {
            return true;
        }

        var projectionOffset = new Vector3(0f, MathF.Max(offset.Y, obj.Height * 2.2f), 0f);
        if (this.projectionService.WorldToScreen(obj.Position + projectionOffset, out screen) && this.IsValidAnchor(screen))
        {
            return true;
        }

        if (this.actorCache.TryGetValue(obj.ObjectId, out var cached) &&
            this.frameCounter - cached.LastSeenFrame <= 30 &&
            this.IsValidAnchor(cached.LastAnchor))
        {
            screen = cached.LastAnchor;
            return true;
        }

        return false;
    }

    private static bool ShouldRenderObject(TrackedObject obj)
    {
        if (obj.Targetable)
        {
            return true;
        }

        return obj.Kind is ObjectKind.Companion or ObjectKind.EventNpc;
    }

    private bool RequiresNativePresence(NameplateCategory category)
    {
        return category is NameplateCategory.Npc
            or NameplateCategory.Minion
            or NameplateCategory.Object
            or NameplateCategory.HousingFurniture
            or NameplateCategory.HousingField;
    }

    private void TrimActorCache()
    {
        if (this.actorCache.Count == 0)
        {
            return;
        }

        var staleIds = new List<ulong>();
        foreach (var pair in this.actorCache)
        {
            if (this.frameCounter - pair.Value.LastSeenFrame > 180)
            {
                staleIds.Add(pair.Key);
            }
        }

        for (var i = 0; i < staleIds.Count; i++)
        {
            this.actorCache.Remove(staleIds[i]);
        }
    }

    private bool IsValidAnchor(Vector2 screen)
    {
        if (!float.IsFinite(screen.X) || !float.IsFinite(screen.Y))
        {
            return false;
        }

        if (screen.X <= 1f || screen.Y <= 1f)
        {
            return false;
        }

        var viewport = Dalamud.Bindings.ImGui.ImGui.GetMainViewport();
        if (viewport.Size.X <= 0f || viewport.Size.Y <= 0f)
        {
            return true;
        }

        return screen.X >= viewport.Pos.X - 64f &&
               screen.Y >= viewport.Pos.Y - 64f &&
               screen.X <= viewport.Pos.X + viewport.Size.X + 64f &&
               screen.Y <= viewport.Pos.Y + viewport.Size.Y + 64f;
    }
}
