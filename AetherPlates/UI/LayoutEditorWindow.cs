using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using FFXIVHudPlugin.AetherPlates.Configuration;
using FFXIVHudPlugin.AetherPlates.Core;
using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using FFXIVHudPlugin.AetherPlates.Styles;
using FFXIVHudPlugin.AetherPlates.Widgets;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.UI;

public sealed class LayoutEditorWindow
{
    public sealed record CategoryEditorTarget(
        string Id,
        string Title,
        NameplateManager.NameplateCategory Category,
        Func<bool> GetEnabled,
        Action<bool> SetEnabled,
        CategoryVisualSettings Visuals);

    private readonly PluginConfiguration config;
    private readonly Action onConfigChanged;
    private readonly ITextureProvider textureProvider;
    private readonly WidgetRegistry widgetRegistry;
    private readonly LayoutEngine layoutEngine;
    private readonly StyleManager styleManager;
    private readonly NameplateRenderer previewRenderer;
    private CategoryEditorTarget? activeTarget;
    private static CategoryVisualSettings? copiedVisualSettings;
    private static string copiedFromCategoryTitle = string.Empty;
    private string fontStatusMessage = string.Empty;
    private string previewErrorMessage = string.Empty;

    public LayoutEditorWindow(
        PluginConfiguration config,
        Action onConfigChanged,
        ITextureProvider textureProvider)
    {
        this.config = config;
        this.onConfigChanged = onConfigChanged;
        this.textureProvider = textureProvider;

        this.widgetRegistry = new WidgetRegistry();
        WidgetRegistration.RegisterBuiltIns(this.widgetRegistry);
        this.layoutEngine = new LayoutEngine();
        this.styleManager = new StyleManager(config.GetActiveStyles);
        this.previewRenderer = new NameplateRenderer(
            this.widgetRegistry,
            this.layoutEngine,
            this.styleManager,
            new ImGuiRenderer(ImGuiRenderer.DrawLayer.Window));
    }

    public bool IsOpen { get; private set; }
    public string? ActiveCategoryId => this.IsOpen ? this.activeTarget?.Id : null;

    public void Open(CategoryEditorTarget target)
    {
        this.activeTarget = target;
        this.IsOpen = true;
    }

    public void Draw()
    {
        if (!this.IsOpen || this.activeTarget is null)
        {
            return;
        }

        var title = $"{this.activeTarget.Title} Designer###AetherPlatesCategoryDesigner";
        var open = this.IsOpen;
        ImGui.SetNextWindowSize(new Vector2(760f, 640f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(720f, 640f), new Vector2(4096f, 4096f));
        if (!ImGui.Begin(title, ref open))
        {
            this.IsOpen = open;
            ImGui.End();
            return;
        }

        this.IsOpen = open;
        DrawHeader(this.activeTarget);
        DrawPreview(this.activeTarget);
        DrawWidgetEditor(this.activeTarget.Visuals, this.activeTarget.Id);

        ImGui.Spacing();
        if (ImGui.Button("Close Designer"))
        {
            this.IsOpen = false;
        }

        ImGui.End();
    }

    private void DrawHeader(CategoryEditorTarget target)
    {
        var enabled = target.GetEnabled();
        if (ImGui.Checkbox("Enable Category", ref enabled))
        {
            target.SetEnabled(enabled);
            this.onConfigChanged();
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy Design"))
        {
            copiedVisualSettings = CloneVisualSettings(target.Visuals);
            copiedFromCategoryTitle = target.Title;
        }

        ImGui.SameLine();
        var canPaste = copiedVisualSettings is not null;
        if (!canPaste)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Paste Design") && copiedVisualSettings is not null)
        {
            ApplyVisualSettings(target.Visuals, copiedVisualSettings);
            this.onConfigChanged();
        }

        if (!canPaste)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        if (copiedVisualSettings is null)
        {
            ImGui.TextColored(0xFF9AA1AB, "No copied design.");
        }
        else
        {
            ImGui.TextColored(0xFF9AA1AB, $"Copied from: {copiedFromCategoryTitle}");
        }
        ImGui.Separator();
    }

    private void DrawPreview(CategoryEditorTarget target)
    {
        ImGui.TextUnformatted("Preview");
        var available = ImGui.GetContentRegionAvail();
        var previewHeight = Math.Clamp(available.Y * 0.35f, 150f, 190f);
        var previewSize = new Vector2(Math.Max(280f, available.X), previewHeight);
        var previewPos = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("##nameplate_preview_canvas", previewSize);

        var drawList = ImGui.GetWindowDrawList();
        var canvasMin = previewPos;
        var canvasMax = previewPos + previewSize;
        drawList.AddRectFilled(canvasMin, canvasMax, 0xCC101216, 6f);
        drawList.AddRect(canvasMin, canvasMax, 0xFF2D313B, 6f, ImDrawFlags.None, 1f);

        var anchor = previewPos + (previewSize * 0.5f);
        try
        {
            var context = BuildPreviewContext(target, anchor);
            context = this.CenterContextByWidgetBounds(context, target.Visuals.EnabledWidgetIdsSet, anchor);
            this.previewRenderer.DrawNameplate(context, target.Visuals.EnabledWidgetIdsSet);
            this.previewErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            this.previewErrorMessage = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(this.previewErrorMessage))
        {
            drawList.AddText(
                canvasMin + new Vector2(10f, 10f),
                0xFFDD8080,
                $"Preview render issue: {this.previewErrorMessage}");
        }
    }

    private NameplateContext CenterContextByWidgetBounds(NameplateContext context, IReadOnlySet<string> enabledWidgetIds, Vector2 canvasCenter)
    {
        if (enabledWidgetIds.Count == 0)
        {
            return context;
        }

        var style = this.styleManager.Select(context);
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        var hasAny = false;

        foreach (var widget in this.widgetRegistry.Widgets)
        {
            if (!enabledWidgetIds.Contains(widget.Id))
            {
                continue;
            }

            var desired = widget.GetDesiredSize(context);
            var layout = this.layoutEngine.Calculate(context, style, widget.Id, desired);
            if (!layout.Visible)
            {
                continue;
            }

            hasAny = true;
            min = Vector2.Min(min, layout.Position);
            max = Vector2.Max(max, layout.Position + layout.Size);
        }

        if (!hasAny)
        {
            return context;
        }

        var boundsCenter = (min + max) * 0.5f;
        var delta = canvasCenter - boundsCenter;
        return context with { AnchorScreenPosition = context.AnchorScreenPosition + delta };
    }

    private NameplateContext BuildPreviewContext(CategoryEditorTarget target, Vector2 anchor)
    {
        var profile = this.config.GetActiveProfile();
        var tracked = BuildPreviewTrackedObject(target.Category);
        var isHostile = target.Category is NameplateManager.NameplateCategory.EnemyUnengaged
            or NameplateManager.NameplateCategory.EnemyEngaged
            or NameplateManager.NameplateCategory.EnemyClaimed
            or NameplateManager.NameplateCategory.EnemyUnclaimed
            or NameplateManager.NameplateCategory.EnemyFeast
            or NameplateManager.NameplateCategory.EnemyFeastPet;

        return new NameplateContext(
            tracked,
            profile,
            target.Visuals,
            this.textureProvider,
            anchor,
            this.config.TemporaryGlobalScale,
            true,
            false,
            false,
            tracked.IsPartyMember,
            tracked.IsAllianceMember,
            isHostile,
            !isHostile,
            tracked.Distance,
            this.config.ResolveFontFamilyId(target.Visuals));
    }

    private static TrackedObject BuildPreviewTrackedObject(NameplateManager.NameplateCategory category)
    {
        var kind = ObjectKind.BattleNpc;
        var name = "Preview Target";
        var subKind = (byte)BattleNpcSubKind.Combatant;
        var isPlayer = false;
        var isParty = false;
        var isAlliance = false;
        var isFriend = false;
        var isHostile = false;
        var enemyState = EnemyNameplateState.Unknown;

        switch (category)
        {
            case NameplateManager.NameplateCategory.Self:
                kind = ObjectKind.Pc;
                name = "You";
                isPlayer = true;
                break;
            case NameplateManager.NameplateCategory.Party:
                kind = ObjectKind.Pc;
                name = "Party Member";
                isParty = true;
                break;
            case NameplateManager.NameplateCategory.Alliance:
                kind = ObjectKind.Pc;
                name = "Alliance Member";
                isAlliance = true;
                break;
            case NameplateManager.NameplateCategory.Friend:
                kind = ObjectKind.Pc;
                name = "Friend";
                isFriend = true;
                break;
            case NameplateManager.NameplateCategory.OtherPc:
                kind = ObjectKind.Pc;
                name = "Other Player";
                break;
            case NameplateManager.NameplateCategory.SelfCompanion:
            case NameplateManager.NameplateCategory.PartyCompanion:
            case NameplateManager.NameplateCategory.FriendCompanion:
            case NameplateManager.NameplateCategory.OtherCompanion:
                kind = ObjectKind.BattleNpc;
                subKind = (byte)BattleNpcSubKind.Buddy;
                name = "Companion";
                break;
            case NameplateManager.NameplateCategory.SelfPet:
            case NameplateManager.NameplateCategory.PartyPet:
            case NameplateManager.NameplateCategory.FriendPet:
            case NameplateManager.NameplateCategory.AlliancePet:
            case NameplateManager.NameplateCategory.OtherPet:
                kind = ObjectKind.BattleNpc;
                subKind = (byte)BattleNpcSubKind.Pet;
                name = "Pet";
                break;
            case NameplateManager.NameplateCategory.Minion:
                kind = ObjectKind.Companion;
                subKind = 0;
                name = "Minion";
                break;
            case NameplateManager.NameplateCategory.Npc:
                kind = ObjectKind.EventNpc;
                subKind = 0;
                name = "NPC";
                break;
            case NameplateManager.NameplateCategory.Object:
                kind = ObjectKind.EventObj;
                subKind = 0;
                name = "Object";
                break;
            case NameplateManager.NameplateCategory.HousingFurniture:
                kind = ObjectKind.HousingEventObject;
                subKind = 0;
                name = "Housing Furniture";
                break;
            case NameplateManager.NameplateCategory.EnemyUnengaged:
                name = "Unengaged Enemy";
                isHostile = true;
                enemyState = EnemyNameplateState.Unengaged;
                break;
            case NameplateManager.NameplateCategory.EnemyEngaged:
                name = "Engaged Enemy";
                isHostile = true;
                enemyState = EnemyNameplateState.Engaged;
                break;
            case NameplateManager.NameplateCategory.EnemyClaimed:
                name = "Claimed Enemy";
                isHostile = true;
                enemyState = EnemyNameplateState.Claimed;
                break;
            case NameplateManager.NameplateCategory.EnemyUnclaimed:
                name = "Unclaimed Enemy";
                isHostile = true;
                enemyState = EnemyNameplateState.Unclaimed;
                break;
            case NameplateManager.NameplateCategory.EnemyFeast:
                name = "Feast Enemy";
                isHostile = true;
                enemyState = EnemyNameplateState.Feast;
                break;
            case NameplateManager.NameplateCategory.EnemyFeastPet:
                name = "Feast Enemy Pet";
                isHostile = true;
                enemyState = EnemyNameplateState.FeastPet;
                break;
        }

        var statuses = new List<StatusSnapshot>
        {
            new(1201, 2, 18f, 1, false, "Buff", 0),
            new(1202, 1, 12f, 1, false, "Buff", 0),
            new(2201, 2, 7.5f, 1, true, "Debuff", 0),
            new(2202, 1, 22f, 1, true, "Debuff", 0),
        };

        var cast = new CastSnapshot(true, "Sample Cast", 1.7f, 3.0f, true);

        return new TrackedObject(
            1,
            1,
            nint.Zero,
            name,
            kind,
            7600,
            10000,
            0.12f,
            new Vector3(0f, 0f, 0f),
            1.5f,
            true,
            15f,
            19,
            100,
            true,
            false,
            isHostile,
            !isHostile,
            isParty,
            isAlliance,
            isFriend,
            enemyState,
            0,
            subKind,
            isPlayer,
            statuses,
            cast);
    }

    private void DrawWidgetEditor(CategoryVisualSettings visuals, string categoryId)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Widget Settings");

        DrawFontSelector(visuals, categoryId);
        ImGui.Spacing();

        DrawWidgetControls(visuals, categoryId, "health_bar", "Health Bar");
        DrawWidgetControls(visuals, categoryId, "name_text", "Name Text");
        DrawWidgetControls(visuals, categoryId, "target_indicator", "Target Indicator");
        DrawWidgetControls(visuals, categoryId, "cast_bar", "Cast Bar");
        DrawWidgetControls(visuals, categoryId, "cast_bar_text", "Cast Bar Text");
        DrawWidgetControls(visuals, categoryId, "buff_row", "Buff Row");
        DrawWidgetControls(visuals, categoryId, "debuff_row", "Debuff Row");
    }

    private void DrawWidgetControls(CategoryVisualSettings visuals, string categoryId, string widgetId, string label)
    {
        var enabled = visuals.IsWidgetEnabled(widgetId);
        if (ImGui.Checkbox($"{label}##{categoryId}_{widgetId}_enabled", ref enabled))
        {
            visuals.SetWidgetEnabled(widgetId, enabled);
            this.onConfigChanged();
        }

        if (!visuals.WidgetLayouts.TryGetValue(widgetId, out var rule))
        {
            visuals.EnsureDefaults();
            rule = visuals.WidgetLayouts[widgetId];
        }

        if (!enabled)
        {
            return;
        }

        ImGui.Indent();
        var offset = rule.Offset;
        if (ImGui.DragFloat2($"Offset (X/Y)##{categoryId}_{widgetId}_offset", ref offset, 0.5f, -600f, 600f, "%.1f"))
        {
            rule.Offset = offset;
            this.onConfigChanged();
        }

        var size = rule.Size;
        if (string.Equals(widgetId, "name_text", StringComparison.Ordinal))
        {
            var fontSize = visuals.NameTextFontSize;
            if (ImGui.DragFloat($"Font Size##{categoryId}_{widgetId}_font_size", ref fontSize, 0.25f, 8f, 64f, "%.1f"))
            {
                visuals.NameTextFontSize = Math.Clamp(fontSize, 8f, 64f);
                this.onConfigChanged();
            }
        }
        else if (string.Equals(widgetId, "cast_bar_text", StringComparison.Ordinal))
        {
            var fontSize = visuals.CastBarTextFontSize;
            if (ImGui.DragFloat($"Font Size##{categoryId}_{widgetId}_font_size", ref fontSize, 0.25f, 8f, 64f, "%.1f"))
            {
                visuals.CastBarTextFontSize = Math.Clamp(fontSize, 8f, 64f);
                this.onConfigChanged();
            }
        }
        else if (string.Equals(widgetId, "buff_row", StringComparison.Ordinal) ||
                 string.Equals(widgetId, "debuff_row", StringComparison.Ordinal))
        {
            var baseWidth = StatusLaneLayout.GetIconWidth(20f) * 8f + (2f * 7f);
            const float baseHeight = 20f;
            var currentScale = string.Equals(widgetId, "buff_row", StringComparison.Ordinal)
                ? visuals.BuffRowScale
                : visuals.DebuffRowScale;
            currentScale = Math.Clamp(currentScale, 0.25f, 8f);
            if (ImGui.DragFloat($"Scale##{categoryId}_{widgetId}_scale", ref currentScale, 0.01f, 0.25f, 8f, "%.2f"))
            {
                var clamped = Math.Clamp(currentScale, 0.25f, 8f);
                if (string.Equals(widgetId, "buff_row", StringComparison.Ordinal))
                {
                    visuals.BuffRowScale = clamped;
                }
                else
                {
                    visuals.DebuffRowScale = clamped;
                }

                // Keep row bounds proportional so W/H stays uniform with icon scaling.
                rule.Size = new Vector2(baseWidth * clamped, baseHeight * clamped);
                this.onConfigChanged();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Scales icon size, spacing, and timer text while keeping default row dimensions.");
            }
        }
        else if (ImGui.DragFloat2($"Size (W/H)##{categoryId}_{widgetId}_size", ref size, 0.5f, 0f, 600f, "%.1f"))
        {
            rule.Size = new Vector2(Math.Max(0f, size.X), Math.Max(0f, size.Y));
            this.onConfigChanged();
        }

        ImGui.Unindent();
    }

    private void DrawFontSelector(CategoryVisualSettings visuals, string categoryId)
    {
        var useGlobalFont = visuals.UseGlobalFont ?? visuals.FontFamilyId == 0;
        if (ImGui.Checkbox($"Use Global Default Font##{categoryId}_use_global_font", ref useGlobalFont))
        {
            visuals.UseGlobalFont = useGlobalFont;
            this.onConfigChanged();
        }

        var (ids, labels) = GameFontRegistry.GetFontOptions();
        var count = Math.Max(1, labels.Length);
        var normalizedId = GameFontRegistry.NormalizeFamilyId(visuals.FontFamilyId);
        var current = Array.IndexOf(ids, normalizedId);
        if (current < 0)
        {
            current = 0;
        }

        if (useGlobalFont)
        {
            var resolvedGlobal = GameFontRegistry.NormalizeFamilyId(this.config.DefaultFontFamilyId);
            var globalIndex = Array.IndexOf(ids, resolvedGlobal);
            if (globalIndex < 0)
            {
                globalIndex = 0;
            }

            ImGui.TextColored(0xFF9AA1AB, $"Using Global Font: {labels[globalIndex]}");
        }
        else
        {
            ImGui.TextUnformatted("Font");
            if (ImGui.Combo($"Font##{categoryId}_font", ref current, labels, labels.Length))
            {
                var selectedId = current >= 0 && current < ids.Length ? ids[current] : 0;
                visuals.FontFamilyId = GameFontRegistry.NormalizeFamilyId(selectedId);
                this.onConfigChanged();
            }
        }

        if (ImGui.Button($"Reload Fonts##{categoryId}_reload_fonts"))
        {
            GameFontRegistry.Reload(out this.fontStatusMessage);
            visuals.FontFamilyId = GameFontRegistry.NormalizeFamilyId(visuals.FontFamilyId);
            this.onConfigChanged();
        }

        ImGui.SameLine();
        ImGui.TextColored(0xFF9AA1AB, "Supports FFXIV fonts and bundled .ttf/.otf in fonts/");
        if (!string.IsNullOrWhiteSpace(this.fontStatusMessage))
        {
            ImGui.TextColored(0xFF9AA1AB, this.fontStatusMessage);
        }
    }

    private static CategoryVisualSettings CloneVisualSettings(CategoryVisualSettings source)
    {
        var clone = new CategoryVisualSettings
        {
            HealthBarEnabled = source.HealthBarEnabled,
            NameTextEnabled = source.NameTextEnabled,
            NameTextFontSize = source.NameTextFontSize,
            TargetIndicatorEnabled = source.TargetIndicatorEnabled,
            CastBarEnabled = source.CastBarEnabled,
            CastBarTextEnabled = source.CastBarTextEnabled,
            CastBarTextFontSize = source.CastBarTextFontSize,
            BuffRowEnabled = source.BuffRowEnabled,
            BuffRowScale = source.BuffRowScale,
            DebuffRowEnabled = source.DebuffRowEnabled,
            DebuffRowScale = source.DebuffRowScale,
            UseGlobalFont = source.UseGlobalFont,
            FontFamilyId = source.FontFamilyId,
            WidgetLayouts = new Dictionary<string, WidgetLayoutRule>(StringComparer.Ordinal),
        };

        foreach (var pair in source.WidgetLayouts)
        {
            var rule = pair.Value;
            clone.WidgetLayouts[pair.Key] = new WidgetLayoutRule
            {
                WidgetId = rule.WidgetId,
                Anchor = rule.Anchor,
                Offset = rule.Offset,
                Size = rule.Size,
                Visible = rule.Visible,
            };
        }

        return clone;
    }

    private static void ApplyVisualSettings(CategoryVisualSettings destination, CategoryVisualSettings source)
    {
        destination.HealthBarEnabled = source.HealthBarEnabled;
        destination.NameTextEnabled = source.NameTextEnabled;
        destination.NameTextFontSize = source.NameTextFontSize;
        destination.TargetIndicatorEnabled = source.TargetIndicatorEnabled;
        destination.CastBarEnabled = source.CastBarEnabled;
        destination.CastBarTextEnabled = source.CastBarTextEnabled;
        destination.CastBarTextFontSize = source.CastBarTextFontSize;
        destination.BuffRowEnabled = source.BuffRowEnabled;
        destination.BuffRowScale = source.BuffRowScale;
        destination.DebuffRowEnabled = source.DebuffRowEnabled;
        destination.DebuffRowScale = source.DebuffRowScale;
        destination.UseGlobalFont = source.UseGlobalFont;
        destination.FontFamilyId = source.FontFamilyId;

        destination.WidgetLayouts = new Dictionary<string, WidgetLayoutRule>(StringComparer.Ordinal);
        foreach (var pair in source.WidgetLayouts)
        {
            var rule = pair.Value;
            destination.WidgetLayouts[pair.Key] = new WidgetLayoutRule
            {
                WidgetId = rule.WidgetId,
                Anchor = rule.Anchor,
                Offset = rule.Offset,
                Size = rule.Size,
                Visible = rule.Visible,
            };
        }

        destination.EnsureDefaults();
    }
}
