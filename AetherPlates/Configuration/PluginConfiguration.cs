using FFXIVHudPlugin.AetherPlates.Styles;
namespace FFXIVHudPlugin.AetherPlates.Configuration;

[Serializable]
public sealed class PluginConfiguration
{
    private const float BuffDebuffBaseHeight = 20f;
    private const int BuffDebuffBaseMaxIcons = 8;
    private const float BuffDebuffBaseGap = 2f;
    private static readonly float BuffDebuffBaseWidth = (StatusLaneLayout.GetIconWidth(BuffDebuffBaseHeight) * BuffDebuffBaseMaxIcons) + (BuffDebuffBaseGap * (BuffDebuffBaseMaxIcons - 1));
    public bool Enabled { get; set; } = false;
    public float VerticalOffset { get; set; } = 2.05f;
    public float TemporaryGlobalScale { get; set; } = 1.0f;
    public bool EnableDistanceCulling { get; set; } = true;
    public float EnemyMaxDistanceYalms { get; set; } = 35f;
    public float FriendlyMaxDistanceYalms { get; set; } = 28f;
    public float PlayerMaxDistanceYalms { get; set; } = 120f;
    public bool EnableDynamicCombatRange { get; set; } = false;
    public float CombatEnemyMaxDistanceYalms { get; set; } = 45f;
    public float CombatFriendlyMaxDistanceYalms { get; set; } = 32f;
    public bool EnableOcclusionCulling { get; set; } = false;
    public NameplateOcclusionMode OcclusionMode { get; set; } = NameplateOcclusionMode.Simple;
    public NameplateOcclusionType OcclusionType { get; set; } = NameplateOcclusionType.Walls;
    public System.Numerics.Vector2 BossTargetBarAnchorOffset { get; set; } = new(0f, 0f);
    public int DefaultFontFamilyId { get; set; } = 0;
    public NameplateCategoryVisibility CategoryVisibility { get; set; } = new();
    public CategoryVisualSettings SelfVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings SelfCompanionVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings SelfPetVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings PartyVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings PartyCompanionVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings PartyPetVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings AllianceVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings AlliancePetVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings FriendVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings FriendCompanionVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings FriendPetVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings OtherPcVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings OtherCompanionVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings OtherPetVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings EnemyUnengagedVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings EnemyEngagedVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings EnemyClaimedVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings EnemyUnclaimedVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings EnemyFeastVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings EnemyFeastPetVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings BossVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings NpcVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings ObjectVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings MinionVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings HousingFurnitureVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public CategoryVisualSettings HousingFieldVisual { get; set; } = CategoryVisualSettings.CreateDefault();
    public string ActiveProfileId { get; set; } = "default";
    public List<NameplateProfile> Profiles { get; set; } = new();
    public string SelectedSavedProfileId { get; set; } = string.Empty;
    public List<NameplateSavedProfile> SavedProfiles { get; set; } = new();

    [NonSerialized]
    private HashSet<string>? enabledWidgetIdsSet;

    public IReadOnlySet<string> EnabledWidgetIdsSet
    {
        get
        {
            // Always derive from current config booleans/profile state.
            // This avoids stale persisted getter-only collection data from older config payloads.
            return BuildEnabledWidgetSet();
        }
    }

    public NameplateProfile GetActiveProfile()
    {
        this.EnsureCategoryVisualDefaults();
        if (this.Profiles.Count == 0)
        {
            this.Profiles.Add(NameplateProfile.CreateDefault());
            this.ActiveProfileId = this.Profiles[0].Id;
            this.enabledWidgetIdsSet = null;
        }

        for (var i = 0; i < this.Profiles.Count; i++)
        {
            if (string.Equals(this.Profiles[i].Id, this.ActiveProfileId, StringComparison.Ordinal))
            {
                EnsureProfileDefaults(this.Profiles[i]);
                return this.Profiles[i];
            }
        }

        this.ActiveProfileId = this.Profiles[0].Id;
        this.enabledWidgetIdsSet = null;
        EnsureProfileDefaults(this.Profiles[0]);
        return this.Profiles[0];
    }

    public void InvalidateCaches()
    {
        this.enabledWidgetIdsSet = null;
    }

    public IReadOnlyList<NameplateStyle> GetActiveStyles()
    {
        return this.GetActiveProfile().Styles;
    }

    internal CategoryVisualSettings GetVisualSettingsForCategory(Core.NameplateManager.NameplateCategory category)
    {
        this.EnsureCategoryVisualDefaults();
        return category switch
        {
            Core.NameplateManager.NameplateCategory.Self => this.SelfVisual,
            Core.NameplateManager.NameplateCategory.SelfCompanion => this.SelfCompanionVisual,
            Core.NameplateManager.NameplateCategory.SelfPet => this.SelfPetVisual,
            Core.NameplateManager.NameplateCategory.Party => this.PartyVisual,
            Core.NameplateManager.NameplateCategory.PartyCompanion => this.PartyCompanionVisual,
            Core.NameplateManager.NameplateCategory.PartyPet => this.PartyPetVisual,
            Core.NameplateManager.NameplateCategory.Alliance => this.AllianceVisual,
            Core.NameplateManager.NameplateCategory.AlliancePet => this.AlliancePetVisual,
            Core.NameplateManager.NameplateCategory.Friend => this.FriendVisual,
            Core.NameplateManager.NameplateCategory.FriendCompanion => this.FriendCompanionVisual,
            Core.NameplateManager.NameplateCategory.FriendPet => this.FriendPetVisual,
            Core.NameplateManager.NameplateCategory.OtherPc => this.OtherPcVisual,
            Core.NameplateManager.NameplateCategory.OtherCompanion => this.OtherCompanionVisual,
            Core.NameplateManager.NameplateCategory.OtherPet => this.OtherPetVisual,
            Core.NameplateManager.NameplateCategory.EnemyUnengaged => this.EnemyUnengagedVisual,
            Core.NameplateManager.NameplateCategory.EnemyEngaged => this.EnemyEngagedVisual,
            Core.NameplateManager.NameplateCategory.EnemyClaimed => this.EnemyClaimedVisual,
            Core.NameplateManager.NameplateCategory.EnemyUnclaimed => this.EnemyUnclaimedVisual,
            Core.NameplateManager.NameplateCategory.EnemyFeast => this.EnemyFeastVisual,
            Core.NameplateManager.NameplateCategory.EnemyFeastPet => this.EnemyFeastPetVisual,
            Core.NameplateManager.NameplateCategory.Boss => this.BossVisual,
            Core.NameplateManager.NameplateCategory.Npc => this.NpcVisual,
            Core.NameplateManager.NameplateCategory.Object => this.ObjectVisual,
            Core.NameplateManager.NameplateCategory.Minion => this.MinionVisual,
            Core.NameplateManager.NameplateCategory.HousingFurniture => this.HousingFurnitureVisual,
            Core.NameplateManager.NameplateCategory.HousingField => this.HousingFieldVisual,
            _ => this.SelfVisual,
        };
    }

    private HashSet<string> BuildEnabledWidgetSet()
    {
        var activeProfile = this.GetActiveProfile();
        return activeProfile.EnabledWidgets.Count == 0
            ? new HashSet<string>(new[] { "health_bar", "name_text", "target_indicator", "cast_bar", "cast_bar_text" }, StringComparer.Ordinal)
            : new HashSet<string>(activeProfile.EnabledWidgets, StringComparer.Ordinal);
    }

    private void EnsureCategoryVisualDefaults()
    {
        this.DefaultFontFamilyId = Math.Max(0, this.DefaultFontFamilyId);
        if (!Enum.IsDefined(typeof(NameplateOcclusionMode), this.OcclusionMode))
        {
            this.OcclusionMode = NameplateOcclusionMode.Simple;
        }

        if (!Enum.IsDefined(typeof(NameplateOcclusionType), this.OcclusionType))
        {
            this.OcclusionType = NameplateOcclusionType.Walls;
        }

        if (!float.IsFinite(this.BossTargetBarAnchorOffset.X) || !float.IsFinite(this.BossTargetBarAnchorOffset.Y))
        {
            this.BossTargetBarAnchorOffset = System.Numerics.Vector2.Zero;
        }
        this.SelfVisual ??= CategoryVisualSettings.CreateDefault();
        this.SelfCompanionVisual ??= CategoryVisualSettings.CreateDefault();
        this.SelfPetVisual ??= CategoryVisualSettings.CreateDefault();
        this.PartyVisual ??= CategoryVisualSettings.CreateDefault();
        this.PartyCompanionVisual ??= CategoryVisualSettings.CreateDefault();
        this.PartyPetVisual ??= CategoryVisualSettings.CreateDefault();
        this.AllianceVisual ??= CategoryVisualSettings.CreateDefault();
        this.AlliancePetVisual ??= CategoryVisualSettings.CreateDefault();
        this.FriendVisual ??= CategoryVisualSettings.CreateDefault();
        this.FriendCompanionVisual ??= CategoryVisualSettings.CreateDefault();
        this.FriendPetVisual ??= CategoryVisualSettings.CreateDefault();
        this.OtherPcVisual ??= CategoryVisualSettings.CreateDefault();
        this.OtherCompanionVisual ??= CategoryVisualSettings.CreateDefault();
        this.OtherPetVisual ??= CategoryVisualSettings.CreateDefault();
        this.EnemyUnengagedVisual ??= CategoryVisualSettings.CreateDefault();
        this.EnemyEngagedVisual ??= CategoryVisualSettings.CreateDefault();
        this.EnemyClaimedVisual ??= CategoryVisualSettings.CreateDefault();
        this.EnemyUnclaimedVisual ??= CategoryVisualSettings.CreateDefault();
        this.EnemyFeastVisual ??= CategoryVisualSettings.CreateDefault();
        this.EnemyFeastPetVisual ??= CategoryVisualSettings.CreateDefault();
        this.BossVisual ??= CategoryVisualSettings.CreateDefault();
        this.NpcVisual ??= CategoryVisualSettings.CreateDefault();
        this.ObjectVisual ??= CategoryVisualSettings.CreateDefault();
        this.MinionVisual ??= CategoryVisualSettings.CreateDefault();
        this.HousingFurnitureVisual ??= CategoryVisualSettings.CreateDefault();
        this.HousingFieldVisual ??= CategoryVisualSettings.CreateDefault();

        this.SelfVisual.EnsureDefaults();
        this.SelfCompanionVisual.EnsureDefaults();
        this.SelfPetVisual.EnsureDefaults();
        this.PartyVisual.EnsureDefaults();
        this.PartyCompanionVisual.EnsureDefaults();
        this.PartyPetVisual.EnsureDefaults();
        this.AllianceVisual.EnsureDefaults();
        this.AlliancePetVisual.EnsureDefaults();
        this.FriendVisual.EnsureDefaults();
        this.FriendCompanionVisual.EnsureDefaults();
        this.FriendPetVisual.EnsureDefaults();
        this.OtherPcVisual.EnsureDefaults();
        this.OtherCompanionVisual.EnsureDefaults();
        this.OtherPetVisual.EnsureDefaults();
        this.EnemyUnengagedVisual.EnsureDefaults();
        this.EnemyEngagedVisual.EnsureDefaults();
        this.EnemyClaimedVisual.EnsureDefaults();
        this.EnemyUnclaimedVisual.EnsureDefaults();
        this.EnemyFeastVisual.EnsureDefaults();
        this.EnemyFeastPetVisual.EnsureDefaults();
        this.BossVisual.EnsureDefaults();
        this.NpcVisual.EnsureDefaults();
        this.ObjectVisual.EnsureDefaults();
        this.MinionVisual.EnsureDefaults();
        this.HousingFurnitureVisual.EnsureDefaults();
        this.HousingFieldVisual.EnsureDefaults();
    }

    internal int ResolveFontFamilyId(CategoryVisualSettings visuals)
    {
        var useGlobalFont = visuals.UseGlobalFont ?? visuals.FontFamilyId == 0;
        return useGlobalFont ? this.DefaultFontFamilyId : visuals.FontFamilyId;
    }

    private static void EnsureProfileDefaults(NameplateProfile profile)
    {
        profile.EnabledWidgets ??= new HashSet<string>(StringComparer.Ordinal);
        profile.HealthBar ??= new HealthBarWidgetConfig();
        profile.NameText ??= new NameTextWidgetConfig();
        if (!Enum.IsDefined(typeof(NameplateTextAlignment), profile.NameText.Alignment))
        {
            profile.NameText.Alignment = NameplateTextAlignment.Center;
        }
        profile.TargetIndicator ??= new TargetIndicatorWidgetConfig();
        profile.TargetIndicator.Opacity = Math.Clamp(profile.TargetIndicator.Opacity, 0f, 1f);
        profile.TargetIndicator.Scale = Math.Clamp(profile.TargetIndicator.Scale, 0.25f, 8f);
        if (!float.IsFinite(profile.TargetIndicator.Offset.X) || !float.IsFinite(profile.TargetIndicator.Offset.Y))
        {
            profile.TargetIndicator.Offset = System.Numerics.Vector2.Zero;
        }
        profile.TargetIndicator.Size = new System.Numerics.Vector2(
            Math.Max(4f, profile.TargetIndicator.Size.X),
            Math.Max(4f, profile.TargetIndicator.Size.Y));
        profile.CastBar ??= new CastBarWidgetConfig();
        profile.BuffRow ??= new BuffRowWidgetConfig();
        profile.DebuffRow ??= new DebuffRowWidgetConfig();
        profile.Styles ??= new List<NameplateStyle> { StyleManager.CreateFallback() };
        profile.EnabledWidgets.Add("cast_bar");
        profile.EnabledWidgets.Add("title_text");
        profile.EnabledWidgets.Add("job_icon");
        profile.EnabledWidgets.Add("buff_row");
        profile.EnabledWidgets.Add("debuff_row");
        MigrateBuffDebuffRowDefaultLayout(profile);
    }

    private static void MigrateBuffDebuffRowDefaultLayout(NameplateProfile profile)
    {
        if (profile.Styles.Count == 0)
        {
            return;
        }

        var style = profile.Styles[0];
        if (style.WidgetLayouts.TryGetValue("buff_row", out var buffLayout))
        {
            if (buffLayout.Anchor == Layout.WidgetAnchor.Top &&
                Math.Abs(buffLayout.Offset.X) <= 1f &&
                Math.Abs(buffLayout.Offset.Y - 2f) <= 1f)
            {
                buffLayout.Anchor = Layout.WidgetAnchor.TopRight;
                buffLayout.Offset = new System.Numerics.Vector2(76f, -32f);
            }

            var isPriorBuffDefault = buffLayout.Anchor == Layout.WidgetAnchor.TopRight &&
                                     Math.Abs(buffLayout.Offset.X - 76f) <= 2f &&
                                     Math.Abs(buffLayout.Offset.Y + 28f) <= 2f;
            if (isPriorBuffDefault)
            {
                buffLayout.Offset = new System.Numerics.Vector2(76f, -32f);
            }

            if (Math.Abs(buffLayout.Size.X - 176f) <= 2f && Math.Abs(buffLayout.Size.Y - 22f) <= 2f)
            {
                buffLayout.Size = new System.Numerics.Vector2(BuffDebuffBaseWidth, BuffDebuffBaseHeight);
            }

            // Ensure row width/height remain proportional for scale-based rendering.
            if (buffLayout.Size.Y > 0.001f)
            {
                var inferredScale = Math.Clamp(buffLayout.Size.Y / BuffDebuffBaseHeight, 0.25f, 8f);
                buffLayout.Size = new System.Numerics.Vector2(BuffDebuffBaseWidth * inferredScale, BuffDebuffBaseHeight * inferredScale);
            }
        }

        if (style.WidgetLayouts.TryGetValue("debuff_row", out var debuffLayout))
        {
            if (debuffLayout.Anchor == Layout.WidgetAnchor.Top &&
                Math.Abs(debuffLayout.Offset.X) <= 1f &&
                Math.Abs(debuffLayout.Offset.Y - 24f) <= 1f)
            {
                debuffLayout.Anchor = Layout.WidgetAnchor.TopLeft;
                debuffLayout.Offset = new System.Numerics.Vector2(-76f, -32f);
            }

            var isPriorDebuffDefault = debuffLayout.Anchor == Layout.WidgetAnchor.TopLeft &&
                                       Math.Abs(debuffLayout.Offset.X + 76f) <= 2f &&
                                       Math.Abs(debuffLayout.Offset.Y + 28f) <= 2f;
            if (isPriorDebuffDefault)
            {
                debuffLayout.Offset = new System.Numerics.Vector2(-76f, -32f);
            }

            if (Math.Abs(debuffLayout.Size.X - 176f) <= 2f && Math.Abs(debuffLayout.Size.Y - 22f) <= 2f)
            {
                debuffLayout.Size = new System.Numerics.Vector2(BuffDebuffBaseWidth, BuffDebuffBaseHeight);
            }

            // Ensure row width/height remain proportional for scale-based rendering.
            if (debuffLayout.Size.Y > 0.001f)
            {
                var inferredScale = Math.Clamp(debuffLayout.Size.Y / BuffDebuffBaseHeight, 0.25f, 8f);
                debuffLayout.Size = new System.Numerics.Vector2(BuffDebuffBaseWidth * inferredScale, BuffDebuffBaseHeight * inferredScale);
            }
        }

        if (style.WidgetLayouts.TryGetValue("cast_bar", out var castLayout))
        {
            var isLegacyCastDefault = castLayout.Anchor == Layout.WidgetAnchor.Top &&
                                      Math.Abs(castLayout.Offset.X) <= 1f &&
                                      Math.Abs(castLayout.Offset.Y + 16f) <= 2f;
            var isPriorCastDefault = castLayout.Anchor == Layout.WidgetAnchor.Top &&
                                     Math.Abs(castLayout.Offset.X) <= 1f &&
                                     Math.Abs(castLayout.Offset.Y + 22f) <= 2f;
            if (isLegacyCastDefault || isPriorCastDefault)
            {
                castLayout.Anchor = Layout.WidgetAnchor.Top;
                castLayout.Offset = new System.Numerics.Vector2(0f, -22f);
            }
        }

        if (style.WidgetLayouts.TryGetValue("cast_bar_text", out var castTextLayout))
        {
            var isLegacyCastTextDefault = castTextLayout.Anchor == Layout.WidgetAnchor.Top &&
                                          Math.Abs(castTextLayout.Offset.X) <= 1f &&
                                          Math.Abs(castTextLayout.Offset.Y + 20f) <= 2f;
            if (isLegacyCastTextDefault)
            {
                castTextLayout.Anchor = Layout.WidgetAnchor.Top;
                castTextLayout.Offset = new System.Numerics.Vector2(0f, -20f);
            }
        }

        if (style.WidgetLayouts.TryGetValue("name_text", out var nameLayout))
        {
            var isLegacyCentered = nameLayout.Anchor == Layout.WidgetAnchor.Top &&
                                   Math.Abs(nameLayout.Offset.X) <= 1f &&
                                   Math.Abs(nameLayout.Offset.Y + 54f) <= 1f;
            var isLegacyFarLeft = nameLayout.Anchor == Layout.WidgetAnchor.TopLeft &&
                                  Math.Abs(nameLayout.Offset.X + 138f) <= 2f &&
                                  Math.Abs(nameLayout.Offset.Y + 42f) <= 2f;
            var isCurrentDefault = nameLayout.Anchor == Layout.WidgetAnchor.Top &&
                                   Math.Abs(nameLayout.Offset.X - 20f) <= 2f &&
                                   Math.Abs(nameLayout.Offset.Y + 35f) <= 2f;
            if (isLegacyCentered || isLegacyFarLeft || isCurrentDefault)
            {
                nameLayout.Anchor = Layout.WidgetAnchor.Top;
                nameLayout.Offset = new System.Numerics.Vector2(20f, -39f);
            }
        }

        if (style.WidgetLayouts.TryGetValue("title_text", out var titleLayout))
        {
            var isLegacyCentered = titleLayout.Anchor == Layout.WidgetAnchor.Top &&
                                   Math.Abs(titleLayout.Offset.X - 20f) <= 2f &&
                                   Math.Abs(titleLayout.Offset.Y + 18f) <= 2f;
            if (isLegacyCentered)
            {
                titleLayout.Anchor = Layout.WidgetAnchor.Top;
                titleLayout.Offset = new System.Numerics.Vector2(20f, -58f);
            }
        }
    }
}

public enum NameplateOcclusionMode
{
    None = 0,
    Simple = 1,
    Full = 2,
}

public enum NameplateOcclusionType
{
    Walls = 0,
    WallsAndObjects = 1,
}

[Serializable]
public sealed class NameplateCategoryVisibility
{
    // Own tab
    public bool Self { get; set; } = true;
    public bool SelfCompanion { get; set; } = true;
    public bool SelfPet { get; set; } = true;

    // Others tab
    public bool PartyMember { get; set; } = true;
    public bool PartyCompanion { get; set; } = true;
    public bool PartyPet { get; set; } = true;
    public bool AllianceMember { get; set; } = true;
    public bool AlliancePet { get; set; } = true;
    public bool OtherPc { get; set; } = true;
    public bool OtherCompanion { get; set; } = true;
    public bool OtherPet { get; set; } = true;
    public bool Friend { get; set; } = true;
    public bool FriendCompanion { get; set; } = true;
    public bool FriendPet { get; set; } = true;

    // NPCs tab
    public bool EnemyUnengaged { get; set; } = true;
    public bool EnemyEngaged { get; set; } = true;
    public bool EnemyClaimed { get; set; } = true;
    public bool EnemyUnclaimed { get; set; } = true;
    public bool EnemyFeast { get; set; } = true;
    public bool EnemyFeastPet { get; set; } = true;
    public bool Boss { get; set; } = true;
    public bool Npc { get; set; } = true;
    public bool Object { get; set; } = true;
    public bool Minion { get; set; } = true;
    public bool HousingFurniture { get; set; } = true;
    public bool HousingField { get; set; } = true;

    public bool IsAnyEnemyEnabled()
    {
        return this.EnemyUnengaged || this.EnemyEngaged || this.EnemyClaimed || this.EnemyUnclaimed || this.EnemyFeast || this.EnemyFeastPet || this.Boss;
    }

    public bool IsAnyEnabled()
    {
        return this.Self ||
               this.SelfCompanion ||
               this.SelfPet ||
               this.PartyMember ||
               this.PartyCompanion ||
               this.PartyPet ||
               this.AllianceMember ||
               this.AlliancePet ||
               this.OtherPc ||
               this.OtherCompanion ||
               this.OtherPet ||
               this.Friend ||
               this.FriendCompanion ||
               this.FriendPet ||
               this.IsAnyEnemyEnabled() ||
               this.Npc ||
               this.Object ||
               this.Minion ||
               this.HousingFurniture ||
               this.HousingField;
    }

    public bool IsAllEnabled()
    {
        return this.Self &&
               this.SelfCompanion &&
               this.SelfPet &&
               this.PartyMember &&
               this.PartyCompanion &&
               this.PartyPet &&
               this.AllianceMember &&
               this.AlliancePet &&
               this.OtherPc &&
               this.OtherCompanion &&
               this.OtherPet &&
               this.Friend &&
               this.FriendCompanion &&
               this.FriendPet &&
               this.EnemyUnengaged &&
               this.EnemyEngaged &&
               this.EnemyClaimed &&
               this.EnemyUnclaimed &&
               this.EnemyFeast &&
               this.EnemyFeastPet &&
               this.Boss &&
               this.Npc &&
               this.Object &&
               this.Minion &&
               this.HousingFurniture &&
               this.HousingField;
    }
}

[Serializable]
public sealed class NameplateProfile
{
    public string Id { get; set; } = "default";
    public string DisplayName { get; set; } = "Default";
    public HashSet<string> EnabledWidgets { get; set; } = new(StringComparer.Ordinal)
    {
        "health_bar",
        "name_text",
        "target_indicator",
        "cast_bar",
        "buff_row",
        "debuff_row",
    };
    public HealthBarWidgetConfig HealthBar { get; set; } = new();
    public NameTextWidgetConfig NameText { get; set; } = new();
    public TargetIndicatorWidgetConfig TargetIndicator { get; set; } = new();
    public CastBarWidgetConfig CastBar { get; set; } = new();
    public BuffRowWidgetConfig BuffRow { get; set; } = new();
    public DebuffRowWidgetConfig DebuffRow { get; set; } = new();
    public List<NameplateStyle> Styles { get; set; } = new()
    {
        StyleManager.CreateFallback(),
    };

    public static NameplateProfile CreateDefault()
    {
        return new NameplateProfile
        {
            Id = "default",
            DisplayName = "Default",
        };
    }
}

[Serializable]
public sealed class NameplateSavedProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "New Profile";
    public PluginConfiguration Snapshot { get; set; } = new();
}

[Serializable]
public sealed class HealthBarWidgetConfig
{
    public float Width { get; set; } = 140f;
    public float Height { get; set; } = 14f;
    public string Texture { get; set; } = string.Empty;
    public uint BackgroundColor { get; set; } = 0xB81C1C1C;
    public uint BorderColor { get; set; } = 0xFF000000;
}

[Serializable]
public sealed class NameTextWidgetConfig
{
    public string Format { get; set; } = "[Lv{Level}] {Name}";
    public bool Outline { get; set; } = true;
    public bool Shadow { get; set; } = true;
    public float FontScale { get; set; } = 1.0f;
    public int TruncateAt { get; set; } = 36;
    public NameplateTextAlignment Alignment { get; set; } = NameplateTextAlignment.Center;
}

[Serializable]
public sealed class TargetIndicatorWidgetConfig
{
    public TargetIndicatorStyle Style { get; set; } = TargetIndicatorStyle.SideArrows;
    public uint Color { get; set; } = 0xFF4AB3FF;
    public float Opacity { get; set; } = 1.0f;
    public System.Numerics.Vector2 Size { get; set; } = new(24f, 12f);
    public float Scale { get; set; } = 1.0f;
    public System.Numerics.Vector2 Offset { get; set; } = new(0f, 0f);
}

public enum TargetIndicatorStyle
{
    SideArrows = 0,
    DoubleSideArrows = 1,
    TopArrow = 2,
    GlowBorder = 3,
}

public enum NameplateTextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2,
}

public enum NameplateTextEdge
{
    Left = 0,
    Right = 1,
}

public enum NameplateJobIconType
{
    Type1 = 0,
    Type2 = 1,
}

[Serializable]
public sealed class CastBarWidgetConfig
{
    public float Width { get; set; } = 140f;
    public float Height { get; set; } = 10f;
    public uint BackgroundColor { get; set; } = 0xB81C1C1C;
    public uint FillColor { get; set; } = 0xFFE4D59B;
    public uint BorderColor { get; set; } = 0xFF000000;
    public uint InterruptibleColor { get; set; } = 0xFF56D980;
    public uint NotInterruptibleColor { get; set; } = 0xFF4A4ACC;
    public bool ShowSpark { get; set; } = true;
    public bool ShowSafeZoneMarker { get; set; } = true;
    public float SafeZoneSeconds { get; set; } = 0.5f;
}

[Serializable]
public sealed class BuffRowWidgetConfig
{
    public float IconSize { get; set; } = 18f;
    public float IconGap { get; set; } = 2f;
    public int MaxIcons { get; set; } = 8;
    public bool OnlyMine { get; set; } = false;
    public HashSet<uint> Whitelist { get; set; } = new();
    public HashSet<uint> Blacklist { get; set; } = new();
}

[Serializable]
public sealed class DebuffRowWidgetConfig
{
    public float IconSize { get; set; } = 18f;
    public float IconGap { get; set; } = 2f;
    public int MaxIcons { get; set; } = 8;
    public bool OnlyMine { get; set; } = false;
    public HashSet<uint> Whitelist { get; set; } = new();
    public HashSet<uint> Blacklist { get; set; } = new();
}

[Serializable]
public sealed class CategoryVisualSettings
{
    public bool HealthBarEnabled { get; set; } = true;
    public float HealthBarCornerRoundness { get; set; } = 0.5f;
    public bool UseCustomHealthBarColors { get; set; } = false;
    public uint HealthBarFillColor { get; set; } = 0xFF4AB34A;
    public uint HealthBarBackgroundColor { get; set; } = 0xB81C1C1C;
    public uint HealthBarBorderColor { get; set; } = 0xFF000000;
    public bool BossShowHpValueText { get; set; } = false;
    public bool BossShowHpPercentText { get; set; } = false;
    public float BossHpValueTextFontSize { get; set; } = 14f;
    public float BossHpPercentTextFontSize { get; set; } = 14f;
    public System.Numerics.Vector2 BossHpValueTextOffset { get; set; } = new(6f, -20f);
    public System.Numerics.Vector2 BossHpPercentTextOffset { get; set; } = new(-44f, -20f);
    public bool? BossHpValueTextUseGlobalFont { get; set; } = null;
    public bool? BossHpPercentTextUseGlobalFont { get; set; } = null;
    public int BossHpValueTextFontFamilyId { get; set; } = 0;
    public int BossHpPercentTextFontFamilyId { get; set; } = 0;
    public bool NameTextEnabled { get; set; } = true;
    public float NameTextFontSize { get; set; } = 16f;
    public NameplateTextAlignment NameTextAlignment { get; set; } = NameplateTextAlignment.Center;
    public bool TitleTextEnabled { get; set; } = false;
    public float TitleTextFontSize { get; set; } = 14f;
    public NameplateTextAlignment TitleTextAlignment { get; set; } = NameplateTextAlignment.Center;
    public bool? TitleTextUseGlobalFont { get; set; } = null;
    public int TitleTextFontFamilyId { get; set; } = 0;
    public bool JobIconEnabled { get; set; } = false;
    public NameplateTextEdge JobIconNameTextEdge { get; set; } = NameplateTextEdge.Left;
    public float JobIconNameTextGap { get; set; } = 4f;
    public NameplateJobIconType JobIconType { get; set; } = NameplateJobIconType.Type1;
    public bool TargetIndicatorEnabled { get; set; } = true;
    public bool TargetIndicatorCenterWithHealthBar { get; set; } = false;
    public bool CastBarEnabled { get; set; } = true;
    public float CastBarCornerRoundness { get; set; } = 0.5f;
    public bool UseCustomCastBarColors { get; set; } = false;
    public uint CastBarFillColor { get; set; } = 0xFFE4D59B;
    public uint CastBarBackgroundColor { get; set; } = 0xB81C1C1C;
    public uint CastBarBorderColor { get; set; } = 0xFF000000;
    public uint CastBarInterruptibleColor { get; set; } = 0xFF56D980;
    public uint CastBarNotInterruptibleColor { get; set; } = 0xFF4A4ACC;
    public bool CastBarTextEnabled { get; set; } = true;
    public float CastBarTextFontSize { get; set; } = 14f;
    public NameplateTextAlignment CastBarTextAlignment { get; set; } = NameplateTextAlignment.Center;
    public bool BuffRowEnabled { get; set; } = true;
    public bool BuffRowCenterWithHealthBar { get; set; } = false;
    public float BuffRowScale { get; set; } = 1.0f;
    public bool DebuffRowEnabled { get; set; } = true;
    public bool DebuffRowCenterWithHealthBar { get; set; } = false;
    public float DebuffRowScale { get; set; } = 1.0f;
    public bool? UseGlobalFont { get; set; } = null;
    public int FontFamilyId { get; set; } = 0;
    public Dictionary<string, WidgetLayoutRule> WidgetLayouts { get; set; } = new(StringComparer.Ordinal);
    [NonSerialized]
    private HashSet<string>? enabledWidgetIdsSet;

    public IReadOnlySet<string> EnabledWidgetIdsSet
    {
        get
        {
            // Always derive from current widget toggles.
            // This prevents stale serialized getter-only values from overriding live toggle fields.
            return this.BuildEnabledWidgetSet();
        }
    }

    public static CategoryVisualSettings CreateDefault()
    {
        var value = new CategoryVisualSettings();
        value.EnsureDefaults();
        return value;
    }

    public void EnsureDefaults()
    {
        const float buffDebuffBaseWidth = 138.8f;
        const float buffDebuffBaseHeight = 20f;
        this.WidgetLayouts ??= new Dictionary<string, WidgetLayoutRule>(StringComparer.Ordinal);
        this.FontFamilyId = Math.Max(0, this.FontFamilyId);
        this.BossHpValueTextFontFamilyId = Math.Max(0, this.BossHpValueTextFontFamilyId);
        this.BossHpPercentTextFontFamilyId = Math.Max(0, this.BossHpPercentTextFontFamilyId);
        this.TitleTextFontFamilyId = Math.Max(0, this.TitleTextFontFamilyId);
        this.BossHpValueTextFontSize = Math.Clamp(this.BossHpValueTextFontSize, 8f, 64f);
        this.BossHpPercentTextFontSize = Math.Clamp(this.BossHpPercentTextFontSize, 8f, 64f);
        if (!float.IsFinite(this.BossHpValueTextOffset.X) || !float.IsFinite(this.BossHpValueTextOffset.Y))
        {
            this.BossHpValueTextOffset = new System.Numerics.Vector2(6f, -20f);
        }

        if (!float.IsFinite(this.BossHpPercentTextOffset.X) || !float.IsFinite(this.BossHpPercentTextOffset.Y))
        {
            this.BossHpPercentTextOffset = new System.Numerics.Vector2(-44f, -20f);
        }
        this.NameTextFontSize = Math.Clamp(this.NameTextFontSize, 8f, 64f);
        this.TitleTextFontSize = Math.Clamp(this.TitleTextFontSize, 8f, 64f);
        this.CastBarTextFontSize = Math.Clamp(this.CastBarTextFontSize, 8f, 64f);
        this.HealthBarCornerRoundness = Math.Clamp(float.IsFinite(this.HealthBarCornerRoundness) ? this.HealthBarCornerRoundness : 0.5f, 0f, 1f);
        this.CastBarCornerRoundness = Math.Clamp(float.IsFinite(this.CastBarCornerRoundness) ? this.CastBarCornerRoundness : 0.5f, 0f, 1f);
        if (!Enum.IsDefined(typeof(NameplateTextAlignment), this.NameTextAlignment))
        {
            this.NameTextAlignment = NameplateTextAlignment.Center;
        }
        if (!Enum.IsDefined(typeof(NameplateTextAlignment), this.TitleTextAlignment))
        {
            this.TitleTextAlignment = NameplateTextAlignment.Center;
        }
        if (!Enum.IsDefined(typeof(NameplateTextAlignment), this.CastBarTextAlignment))
        {
            this.CastBarTextAlignment = NameplateTextAlignment.Center;
        }
        if (!Enum.IsDefined(typeof(NameplateTextEdge), this.JobIconNameTextEdge))
        {
            this.JobIconNameTextEdge = NameplateTextEdge.Left;
        }

        if (!Enum.IsDefined(typeof(NameplateJobIconType), this.JobIconType))
        {
            this.JobIconType = NameplateJobIconType.Type1;
        }

        this.JobIconNameTextGap = Math.Clamp(
            float.IsFinite(this.JobIconNameTextGap) ? this.JobIconNameTextGap : 4f,
            -128f,
            128f);
        this.BuffRowScale = Math.Clamp(this.BuffRowScale, 0.25f, 8f);
        this.DebuffRowScale = Math.Clamp(this.DebuffRowScale, 0.25f, 8f);
        CopyFallbackLayout("health_bar");
        CopyFallbackLayout("name_text");
        CopyFallbackLayout("title_text");
        CopyFallbackLayout("job_icon");
        CopyFallbackLayout("target_indicator");
        CopyFallbackLayout("cast_bar");
        CopyFallbackLayout("cast_bar_text");
        CopyFallbackLayout("buff_row");
        CopyFallbackLayout("debuff_row");
        if (this.WidgetLayouts.TryGetValue("buff_row", out var buffRowRule))
        {
            buffRowRule.Size = new System.Numerics.Vector2(buffDebuffBaseWidth * this.BuffRowScale, buffDebuffBaseHeight * this.BuffRowScale);
        }

        if (this.WidgetLayouts.TryGetValue("debuff_row", out var debuffRowRule))
        {
            debuffRowRule.Size = new System.Numerics.Vector2(buffDebuffBaseWidth * this.DebuffRowScale, buffDebuffBaseHeight * this.DebuffRowScale);
        }
        this.enabledWidgetIdsSet = null;
    }

    public bool IsWidgetEnabled(string widgetId)
    {
        return widgetId switch
        {
            "health_bar" => this.HealthBarEnabled,
            "name_text" => this.NameTextEnabled,
            "title_text" => this.TitleTextEnabled,
            "job_icon" => this.JobIconEnabled,
            "target_indicator" => this.TargetIndicatorEnabled,
            "cast_bar" => this.CastBarEnabled,
            "cast_bar_text" => this.CastBarTextEnabled,
            "buff_row" => this.BuffRowEnabled,
            "debuff_row" => this.DebuffRowEnabled,
            _ => false,
        };
    }

    public void SetWidgetEnabled(string widgetId, bool enabled)
    {
        switch (widgetId)
        {
            case "health_bar":
                this.HealthBarEnabled = enabled;
                break;
            case "name_text":
                this.NameTextEnabled = enabled;
                break;
            case "title_text":
                this.TitleTextEnabled = enabled;
                break;
            case "job_icon":
                this.JobIconEnabled = enabled;
                break;
            case "target_indicator":
                this.TargetIndicatorEnabled = enabled;
                break;
            case "cast_bar":
                this.CastBarEnabled = enabled;
                break;
            case "cast_bar_text":
                this.CastBarTextEnabled = enabled;
                break;
            case "buff_row":
                this.BuffRowEnabled = enabled;
                break;
            case "debuff_row":
                this.DebuffRowEnabled = enabled;
                break;
        }

        this.enabledWidgetIdsSet = null;
    }

    private HashSet<string> BuildEnabledWidgetSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (this.HealthBarEnabled)
        {
            set.Add("health_bar");
        }

        if (this.NameTextEnabled)
        {
            set.Add("name_text");
        }

        if (this.TitleTextEnabled)
        {
            set.Add("title_text");
        }

        if (this.JobIconEnabled)
        {
            set.Add("job_icon");
        }

        if (this.TargetIndicatorEnabled)
        {
            set.Add("target_indicator");
        }

        if (this.CastBarEnabled)
        {
            set.Add("cast_bar");
        }

        if (this.CastBarTextEnabled)
        {
            set.Add("cast_bar_text");
        }

        if (this.BuffRowEnabled)
        {
            set.Add("buff_row");
        }

        if (this.DebuffRowEnabled)
        {
            set.Add("debuff_row");
        }

        return set;
    }

    private void CopyFallbackLayout(string widgetId)
    {
        if (this.WidgetLayouts.ContainsKey(widgetId))
        {
            return;
        }

        var fallback = StyleManager.CreateFallback();
        if (fallback.WidgetLayouts.TryGetValue(widgetId, out var source))
        {
            this.WidgetLayouts[widgetId] = new WidgetLayoutRule
            {
                WidgetId = source.WidgetId,
                Anchor = source.Anchor,
                Offset = source.Offset,
                Size = source.Size,
                Visible = source.Visible,
            };
            return;
        }

        this.WidgetLayouts[widgetId] = WidgetLayoutRule.Default(widgetId);
    }
}
