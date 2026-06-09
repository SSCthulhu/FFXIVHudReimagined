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
    private static readonly Dictionary<string, string> WidgetLabelsById = new(StringComparer.Ordinal)
    {
        ["job_icon"] = "Job Icon",
        ["health_bar"] = "Health Bar",
        ["name_text"] = "Name Text",
        ["title_text"] = "Title Text",
        ["cast_bar"] = "Cast Bar",
        ["cast_bar_text"] = "Cast Bar Text",
        ["buff_row"] = "Buff Row",
        ["debuff_row"] = "Debuff Row",
    };

    // Top-most first for click hit-testing.
    private static readonly string[] WidgetHitTestOrder =
    {
        "job_icon",
        "name_text",
        "title_text",
        "target_indicator",
        "cast_bar_text",
        "cast_bar",
        "buff_row",
        "debuff_row",
        "health_bar",
    };

    private static readonly string[] WidgetEditorOrder =
    {
        "health_bar",
        "name_text",
        "title_text",
        "job_icon",
        "cast_bar",
        "cast_bar_text",
        "buff_row",
        "debuff_row",
    };

    public sealed record CategoryEditorTarget(
        string Id,
        string Title,
        NameplateManager.NameplateCategory Category,
        Func<bool> GetEnabled,
        Action<bool> SetEnabled,
        CategoryVisualSettings Visuals);

    private PluginConfiguration config;
    private readonly Action onConfigChanged;
    private readonly ITextureProvider textureProvider;
    private readonly WidgetRegistry widgetRegistry;
    private readonly LayoutEngine layoutEngine;
    private StyleManager styleManager;
    private NameplateRenderer previewRenderer;
    private CategoryEditorTarget? activeTarget;
    private static CategoryVisualSettings? copiedVisualSettings;
    private static string copiedFromCategoryTitle = string.Empty;
    private string fontStatusMessage = string.Empty;
    private string previewErrorMessage = string.Empty;
    private string? selectedWidgetId;
    private float previewScaleMultiplier = 1.65f;

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

    public void UpdateConfiguration(PluginConfiguration configuration)
    {
        this.config = configuration;
        this.styleManager = new StyleManager(this.config.GetActiveStyles);
        this.previewRenderer = new NameplateRenderer(
            this.widgetRegistry,
            this.layoutEngine,
            this.styleManager,
            new ImGuiRenderer(ImGuiRenderer.DrawLayer.Window));
        this.activeTarget = null;
        this.IsOpen = false;
    }

    public bool IsOpen { get; private set; }
    public string? ActiveCategoryId => this.IsOpen ? this.activeTarget?.Id : null;

    public void Open(CategoryEditorTarget target)
    {
        this.activeTarget = target;
        this.selectedWidgetId = null;
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

        if (target.Category == NameplateManager.NameplateCategory.Boss)
        {
            ImGui.Spacing();
            var bossAnchorOffset = this.config.BossTargetBarAnchorOffset;
            if (ImGui.DragFloat2("Boss Screen Offset From Center (X/Y)", ref bossAnchorOffset, 0.5f, -2000f, 2000f, "%.1f"))
            {
                this.config.BossTargetBarAnchorOffset = bossAnchorOffset;
                this.onConfigChanged();
            }

            ImGui.TextColored(0xFF9AA1AB, "(0,0) is screen center. This moves the entire Boss plate anchor.");
        }

        ImGui.Separator();
    }

    private void DrawPreview(CategoryEditorTarget target)
    {
        ImGui.TextUnformatted("Preview");
        var zoom = this.previewScaleMultiplier;
        ImGui.SetNextItemWidth(220f);
        if (ImGui.SliderFloat("Preview Zoom", ref zoom, 1.0f, 2.5f, "%.2fx"))
        {
            this.previewScaleMultiplier = Math.Clamp(zoom, 1.0f, 2.5f);
        }

        var available = ImGui.GetContentRegionAvail();
        var previewHeight = Math.Clamp(available.Y * 0.50f, 230f, 360f);
        var previewSize = new Vector2(Math.Max(280f, available.X), previewHeight);
        var previewPos = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("##nameplate_preview_canvas", previewSize);

        var drawList = ImGui.GetWindowDrawList();
        var canvasMin = previewPos;
        var canvasMax = previewPos + previewSize;
        drawList.AddRectFilled(canvasMin, canvasMax, 0xCC101216, 6f);
        drawList.AddRect(canvasMin, canvasMax, 0xFF2D313B, 6f, ImDrawFlags.None, 1f);
        var clickedInCanvas = ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var mousePos = ImGui.GetMousePos();

        var anchor = previewPos + (previewSize * 0.5f);
        try
        {
            var context = BuildPreviewContext(target, anchor);
            context = this.CenterContextByWidgetBounds(context, target.Visuals.EnabledWidgetIdsSet, anchor);
            var layouts = this.BuildPreviewWidgetLayouts(context, target.Visuals.EnabledWidgetIdsSet);
            if (clickedInCanvas)
            {
                var clickedWidget = HitTestWidget(mousePos, layouts);
                this.selectedWidgetId = string.Equals(clickedWidget, "target_indicator", StringComparison.Ordinal)
                    ? "health_bar"
                    : clickedWidget;
            }

            this.previewRenderer.DrawNameplate(context, target.Visuals.EnabledWidgetIdsSet);
            if (!string.IsNullOrWhiteSpace(this.selectedWidgetId) &&
                layouts.TryGetValue(this.selectedWidgetId, out var selectedLayout))
            {
                drawList.AddRect(
                    selectedLayout.Position - new Vector2(2f, 2f),
                    selectedLayout.Position + selectedLayout.Size + new Vector2(2f, 2f),
                    0xFFD9A95B,
                    3f,
                    ImDrawFlags.None,
                    2f);

                if (WidgetLabelsById.TryGetValue(this.selectedWidgetId, out var selectedLabel))
                {
                    drawList.AddText(
                        selectedLayout.Position + new Vector2(2f, -16f),
                        0xFFD9A95B,
                        selectedLabel);
                }
            }
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

    private Dictionary<string, WidgetLayout> BuildPreviewWidgetLayouts(NameplateContext context, IReadOnlySet<string> enabledWidgetIds)
    {
        var style = this.styleManager.Select(context);
        var layouts = new Dictionary<string, WidgetLayout>(StringComparer.Ordinal);
        foreach (var widget in this.widgetRegistry.Widgets)
        {
            if (!enabledWidgetIds.Contains(widget.Id))
            {
                continue;
            }

            var desired = widget.GetDesiredSize(context);
            var layout = this.layoutEngine.Calculate(context, style, widget.Id, desired);
            if (layout.Visible)
            {
                layouts[widget.Id] = layout;
            }
        }

        return layouts;
    }

    private static string? HitTestWidget(Vector2 mousePos, IReadOnlyDictionary<string, WidgetLayout> layouts)
    {
        foreach (var widgetId in WidgetHitTestOrder)
        {
            if (layouts.TryGetValue(widgetId, out var layout) && IsInside(mousePos, layout))
            {
                return widgetId;
            }
        }

        foreach (var pair in layouts)
        {
            if (IsInside(mousePos, pair.Value))
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static bool IsInside(Vector2 point, WidgetLayout layout)
    {
        return point.X >= layout.Position.X &&
               point.Y >= layout.Position.Y &&
               point.X <= layout.Position.X + layout.Size.X &&
               point.Y <= layout.Position.Y + layout.Size.Y;
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

            // Target indicator positioning is derived from health bar options.
            // Exclude it from preview recenter bounds so "Center with HP Bar" does
            // not shift the entire preview plate.
            if (string.Equals(widget.Id, "target_indicator", StringComparison.Ordinal))
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
            or NameplateManager.NameplateCategory.EnemyFeastPet
            or NameplateManager.NameplateCategory.Boss;

        return new NameplateContext(
            tracked,
            profile,
            target.Visuals,
            this.textureProvider,
            anchor,
            this.config.TemporaryGlobalScale * this.previewScaleMultiplier,
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
            case NameplateManager.NameplateCategory.Boss:
                name = "Boss (Target Bar)";
                isHostile = true;
                enemyState = EnemyNameplateState.Engaged;
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
            62101,
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
            0,
            isPlayer,
            "of the Seventh Dawn",
            statuses,
            cast);
    }

    private void DrawWidgetEditor(CategoryVisualSettings visuals, string categoryId)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Widget Settings");
        ImGui.TextColored(0xFF9AA1AB, "Click a widget in preview or select one from Plate Elements.");

        if (string.IsNullOrWhiteSpace(this.selectedWidgetId) ||
            Array.IndexOf(WidgetEditorOrder, this.selectedWidgetId) < 0)
        {
            this.selectedWidgetId = WidgetEditorOrder[0];
        }

        ImGui.Spacing();
        var leftWidth = 220f;
        var rightWidth = Math.Max(240f, ImGui.GetContentRegionAvail().X - leftWidth - 8f);

        ImGui.BeginChild("##nameplate_widget_selector", new Vector2(leftWidth, 320f), true);
        ImGui.TextUnformatted("Plate Elements");
        ImGui.Spacing();
        DrawWidgetSelectorList(visuals);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##nameplate_widget_settings", new Vector2(rightWidth, 320f), true);
        ImGui.TextUnformatted("Settings");
        ImGui.Spacing();
        DrawSelectedWidgetSettings(visuals, categoryId);
        ImGui.EndChild();

        ImGui.Spacing();
        DrawFontSelector(visuals, categoryId);
    }

    private void DrawWidgetSelectorList(CategoryVisualSettings visuals)
    {
        for (var i = 0; i < WidgetEditorOrder.Length; i++)
        {
            var widgetId = WidgetEditorOrder[i];
            if (!WidgetLabelsById.TryGetValue(widgetId, out var label))
            {
                continue;
            }

            var isSelected = string.Equals(this.selectedWidgetId, widgetId, StringComparison.Ordinal);
            if (ImGui.Selectable(label, isSelected))
            {
                this.selectedWidgetId = widgetId;
            }

            if (ImGui.IsItemHovered())
            {
                var enabled = visuals.IsWidgetEnabled(widgetId);
                ImGui.SetTooltip(enabled ? "Enabled" : "Disabled");
            }
        }
    }

    private void DrawSelectedWidgetSettings(CategoryVisualSettings visuals, string categoryId)
    {
        if (string.IsNullOrWhiteSpace(this.selectedWidgetId))
        {
            ImGui.TextColored(0xFF9AA1AB, "Select a plate element to edit its settings.");
            return;
        }

        var selectedWidgetId = string.Equals(this.selectedWidgetId, "target_indicator", StringComparison.Ordinal)
            ? "health_bar"
            : this.selectedWidgetId;
        var editorWidgetId = selectedWidgetId;

        if (!WidgetLabelsById.TryGetValue(selectedWidgetId, out var selectedLabel))
        {
            ImGui.TextColored(0xFF9AA1AB, "Select a plate element to edit its settings.");
            return;
        }

        var header = $"Editing: {selectedLabel}";
        ImGui.TextUnformatted(header);
        DrawWidgetControls(visuals, categoryId, editorWidgetId, selectedLabel);
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
        if (string.Equals(widgetId, "health_bar", StringComparison.Ordinal))
        {
            DrawHealthBarColorControls(categoryId, visuals);
            var hpRoundness = visuals.HealthBarCornerRoundness;
            if (ImGui.SliderFloat($"Corner Roundness##{categoryId}_{widgetId}_corner_roundness", ref hpRoundness, 0f, 1f, "%.2f"))
            {
                visuals.HealthBarCornerRoundness = Math.Clamp(hpRoundness, 0f, 1f);
                this.onConfigChanged();
            }
            DrawTargetIndicatorStyleControls(categoryId, visuals);
            ImGui.Spacing();
            if (ImGui.DragFloat2($"Size (W/H)##{categoryId}_{widgetId}_size", ref size, 0.5f, 0f, 600f, "%.1f"))
            {
                rule.Size = new Vector2(Math.Max(0f, size.X), Math.Max(0f, size.Y));
                this.onConfigChanged();
            }
        }
        else if (string.Equals(widgetId, "name_text", StringComparison.Ordinal))
        {
            var fontSize = visuals.NameTextFontSize;
            if (ImGui.DragFloat($"Font Size##{categoryId}_{widgetId}_font_size", ref fontSize, 0.25f, 8f, 64f, "%.1f"))
            {
                visuals.NameTextFontSize = Math.Clamp(fontSize, 8f, 64f);
                this.onConfigChanged();
            }

            DrawTextAlignmentControls(
                categoryId,
                "name_text",
                () => visuals.NameTextAlignment,
                value => visuals.NameTextAlignment = value);
        }
        else if (string.Equals(widgetId, "title_text", StringComparison.Ordinal))
        {
            var fontSize = visuals.TitleTextFontSize;
            if (ImGui.DragFloat($"Font Size##{categoryId}_{widgetId}_font_size", ref fontSize, 0.25f, 8f, 64f, "%.1f"))
            {
                visuals.TitleTextFontSize = Math.Clamp(fontSize, 8f, 64f);
                this.onConfigChanged();
            }

            DrawTextAlignmentControls(
                categoryId,
                "title_text",
                () => visuals.TitleTextAlignment,
                value => visuals.TitleTextAlignment = value);

            var useGlobalFont = visuals.TitleTextUseGlobalFont ?? visuals.TitleTextFontFamilyId == 0;
            if (ImGui.Checkbox($"Use Global Default Font##{categoryId}_{widgetId}_use_global_font", ref useGlobalFont))
            {
                visuals.TitleTextUseGlobalFont = useGlobalFont;
                this.onConfigChanged();
            }

            var (ids, labels) = GameFontRegistry.GetFontOptions();
            var normalized = GameFontRegistry.NormalizeFamilyId(visuals.TitleTextFontFamilyId);
            var current = Array.IndexOf(ids, normalized);
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
                if (ImGui.Combo($"Font##{categoryId}_{widgetId}_font", ref current, labels, labels.Length))
                {
                    var selectedId = current >= 0 && current < ids.Length ? ids[current] : 0;
                    visuals.TitleTextFontFamilyId = GameFontRegistry.NormalizeFamilyId(selectedId);
                    this.onConfigChanged();
                }
            }
        }
        else if (string.Equals(widgetId, "job_icon", StringComparison.Ordinal))
        {
            const float jobIconBaseSize = 20f;
            var currentScale = Math.Clamp(rule.Size.Y > 0.001f ? rule.Size.Y / jobIconBaseSize : 1f, 0.25f, 8f);
            if (ImGui.DragFloat($"Scale##{categoryId}_{widgetId}_scale", ref currentScale, 0.01f, 0.25f, 8f, "%.2f"))
            {
                var clamped = Math.Clamp(currentScale, 0.25f, 8f);
                var iconSize = jobIconBaseSize * clamped;
                rule.Size = new Vector2(iconSize, iconSize);
                this.onConfigChanged();
            }

            var edgeOptions = new[] { "Left of Name Text", "Right of Name Text" };
            var edgeIndex = visuals.JobIconNameTextEdge == NameplateTextEdge.Right ? 1 : 0;
            if (ImGui.Combo($"Anchor to Name Text Edge##{categoryId}_{widgetId}_edge", ref edgeIndex, edgeOptions, edgeOptions.Length))
            {
                visuals.JobIconNameTextEdge = edgeIndex == 1 ? NameplateTextEdge.Right : NameplateTextEdge.Left;
                this.onConfigChanged();
            }

            var iconTypeOptions = new[] { "Type 1", "Type 2" };
            var iconTypeIndex = visuals.JobIconType == NameplateJobIconType.Type2 ? 1 : 0;
            if (ImGui.Combo($"Job Icon Type##{categoryId}_{widgetId}_icon_type", ref iconTypeIndex, iconTypeOptions, iconTypeOptions.Length))
            {
                visuals.JobIconType = iconTypeIndex == 1 ? NameplateJobIconType.Type2 : NameplateJobIconType.Type1;
                this.onConfigChanged();
            }

            var gap = visuals.JobIconNameTextGap;
            if (ImGui.DragFloat($"Name Text Gap##{categoryId}_{widgetId}_name_gap", ref gap, 0.5f, -128f, 128f, "%.1f"))
            {
                visuals.JobIconNameTextGap = Math.Clamp(gap, -128f, 128f);
                this.onConfigChanged();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Scales the job icon uniformly.");
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

            DrawTextAlignmentControls(
                categoryId,
                "cast_bar_text",
                () => visuals.CastBarTextAlignment,
                value => visuals.CastBarTextAlignment = value);
        }
        else if (string.Equals(widgetId, "cast_bar", StringComparison.Ordinal))
        {
            DrawCastBarColorControls(categoryId, visuals);
            var castRoundness = visuals.CastBarCornerRoundness;
            if (ImGui.SliderFloat($"Corner Roundness##{categoryId}_{widgetId}_corner_roundness", ref castRoundness, 0f, 1f, "%.2f"))
            {
                visuals.CastBarCornerRoundness = Math.Clamp(castRoundness, 0f, 1f);
                this.onConfigChanged();
            }
            ImGui.Spacing();
            if (ImGui.DragFloat2($"Size (W/H)##{categoryId}_{widgetId}_size", ref size, 0.5f, 0f, 600f, "%.1f"))
            {
                rule.Size = new Vector2(Math.Max(0f, size.X), Math.Max(0f, size.Y));
                this.onConfigChanged();
            }
        }
        else if (string.Equals(widgetId, "buff_row", StringComparison.Ordinal) ||
                 string.Equals(widgetId, "debuff_row", StringComparison.Ordinal))
        {
            DrawRowCenterWithHealthBarToggle(categoryId, visuals, widgetId);
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

    private void DrawRowCenterWithHealthBarToggle(string categoryId, CategoryVisualSettings visuals, string widgetId)
    {
        var isBuff = string.Equals(widgetId, "buff_row", StringComparison.Ordinal);
        var centered = isBuff ? visuals.BuffRowCenterWithHealthBar : visuals.DebuffRowCenterWithHealthBar;
        if (ImGui.Checkbox($"Center with HP Bar##{categoryId}_{widgetId}_center_with_health", ref centered))
        {
            if (isBuff)
            {
                visuals.BuffRowCenterWithHealthBar = centered;
            }
            else
            {
                visuals.DebuffRowCenterWithHealthBar = centered;
            }

            this.onConfigChanged();
        }
    }

    private void DrawHealthBarColorControls(string categoryId, CategoryVisualSettings visuals)
    {
        var useCustom = visuals.UseCustomHealthBarColors;
        if (ImGui.Checkbox($"Use Custom HP Bar Colors##{categoryId}_health_custom_colors", ref useCustom))
        {
            visuals.UseCustomHealthBarColors = useCustom;
            this.onConfigChanged();
        }

        if (!useCustom)
        {
            return;
        }

        ImGui.Indent();
        DrawColorEdit(
            $"HP Fill Color##{categoryId}_health_fill_color",
            visuals.HealthBarFillColor,
            value => visuals.HealthBarFillColor = value);
        DrawColorEdit(
            $"HP Background Color##{categoryId}_health_bg_color",
            visuals.HealthBarBackgroundColor,
            value => visuals.HealthBarBackgroundColor = value);
        DrawColorEdit(
            $"HP Border Color##{categoryId}_health_border_color",
            visuals.HealthBarBorderColor,
            value => visuals.HealthBarBorderColor = value);
        ImGui.Unindent();
    }

    private void DrawCastBarColorControls(string categoryId, CategoryVisualSettings visuals)
    {
        var useCustom = visuals.UseCustomCastBarColors;
        if (ImGui.Checkbox($"Use Custom Cast Bar Colors##{categoryId}_cast_custom_colors", ref useCustom))
        {
            visuals.UseCustomCastBarColors = useCustom;
            this.onConfigChanged();
        }

        if (!useCustom)
        {
            return;
        }

        ImGui.Indent();
        DrawColorEdit(
            $"Cast Fill Color##{categoryId}_cast_fill_color",
            visuals.CastBarFillColor,
            value => visuals.CastBarFillColor = value);
        DrawColorEdit(
            $"Cast Background Color##{categoryId}_cast_bg_color",
            visuals.CastBarBackgroundColor,
            value => visuals.CastBarBackgroundColor = value);
        DrawColorEdit(
            $"Cast Border Color##{categoryId}_cast_border_color",
            visuals.CastBarBorderColor,
            value => visuals.CastBarBorderColor = value);
        DrawColorEdit(
            $"Interruptible Border Color##{categoryId}_cast_interruptible_color",
            visuals.CastBarInterruptibleColor,
            value => visuals.CastBarInterruptibleColor = value);
        DrawColorEdit(
            $"Not Interruptible Border Color##{categoryId}_cast_not_interruptible_color",
            visuals.CastBarNotInterruptibleColor,
            value => visuals.CastBarNotInterruptibleColor = value);
        ImGui.Unindent();
    }

    private void DrawColorEdit(string label, uint color, Action<uint> setColor)
    {
        var value = FFXIVHudPlugin.HudColorConversion.ToVector4(color);
        if (ImGui.ColorEdit4(label, ref value, ImGuiColorEditFlags.AlphaBar))
        {
            setColor(FFXIVHudPlugin.HudColorConversion.ToImGuiColor(value));
            this.onConfigChanged();
        }
    }

    private void DrawTextAlignmentControls(
        string categoryId,
        string widgetId,
        Func<NameplateTextAlignment> getAlignment,
        Action<NameplateTextAlignment> setAlignment)
    {
        var options = new[] { "Left", "Center", "Right" };
        var current = Math.Clamp((int)getAlignment(), 0, options.Length - 1);
        if (ImGui.Combo($"Text Alignment##{categoryId}_{widgetId}_text_align", ref current, options, options.Length))
        {
            setAlignment((NameplateTextAlignment)current);
            this.onConfigChanged();
        }
    }

    private void DrawTargetIndicatorStyleControls(string categoryId, CategoryVisualSettings visuals)
    {
        var targetCfg = this.config.GetActiveProfile().TargetIndicator;
        var indicatorEnabled = visuals.TargetIndicatorEnabled;
        if (ImGui.Checkbox($"Target Indicator##{categoryId}_health_target_indicator_enabled", ref indicatorEnabled))
        {
            visuals.TargetIndicatorEnabled = indicatorEnabled;
            this.onConfigChanged();
        }

        if (!indicatorEnabled)
        {
            DrawBossHealthTextControls(categoryId, visuals);
            return;
        }

        ImGui.Indent();

        var styleOptions = new[]
        {
            "Side Arrows",
            "Double Side Arrows",
            "Top Arrow",
            "Health Bar Glow",
        };
        var styleIndex = Math.Clamp((int)targetCfg.Style, 0, styleOptions.Length - 1);
        if (ImGui.Combo($"Indicator Style##{categoryId}_health_target_indicator_style", ref styleIndex, styleOptions, styleOptions.Length))
        {
            targetCfg.Style = (TargetIndicatorStyle)styleIndex;
            this.onConfigChanged();
        }

        var indicatorColor = FFXIVHudPlugin.HudColorConversion.ToVector4(targetCfg.Color);
        if (ImGui.ColorEdit4($"Indicator Color##{categoryId}_health_target_indicator_color", ref indicatorColor, ImGuiColorEditFlags.AlphaBar))
        {
            targetCfg.Color = FFXIVHudPlugin.HudColorConversion.ToImGuiColor(indicatorColor);
            this.onConfigChanged();
        }

        var indicatorOpacity = targetCfg.Opacity;
        if (ImGui.DragFloat($"Indicator Opacity##{categoryId}_health_target_indicator_opacity", ref indicatorOpacity, 0.01f, 0f, 1f, "%.2f"))
        {
            targetCfg.Opacity = Math.Clamp(indicatorOpacity, 0f, 1f);
            this.onConfigChanged();
        }

        var indicatorSize = targetCfg.Size;
        if (ImGui.DragFloat2($"Indicator Size (W/H)##{categoryId}_health_target_indicator_size", ref indicatorSize, 0.5f, 4f, 256f, "%.1f"))
        {
            targetCfg.Size = new Vector2(Math.Max(4f, indicatorSize.X), Math.Max(4f, indicatorSize.Y));
            this.onConfigChanged();
        }

        var indicatorScale = targetCfg.Scale;
        if (ImGui.DragFloat($"Indicator Scale##{categoryId}_health_target_indicator_scale", ref indicatorScale, 0.01f, 0.25f, 8f, "%.2f"))
        {
            targetCfg.Scale = Math.Clamp(indicatorScale, 0.25f, 8f);
            this.onConfigChanged();
        }

        var centerWithHealth = visuals.TargetIndicatorCenterWithHealthBar;
        if (ImGui.Checkbox($"Center with HP Bar##{categoryId}_health_target_indicator_center_with_health", ref centerWithHealth))
        {
            visuals.TargetIndicatorCenterWithHealthBar = centerWithHealth;
            this.onConfigChanged();
        }

        var indicatorOffset = targetCfg.Offset;
        if (ImGui.DragFloat2($"Indicator Offset (X/Y)##{categoryId}_health_target_indicator_offset", ref indicatorOffset, 0.5f, -600f, 600f, "%.1f"))
        {
            targetCfg.Offset = indicatorOffset;
            this.onConfigChanged();
        }

        DrawBossHealthTextControls(categoryId, visuals);
        ImGui.Unindent();
    }

    private void DrawBossHealthTextControls(string categoryId, CategoryVisualSettings visuals)
    {
        if (!string.Equals(categoryId, "boss", StringComparison.Ordinal))
        {
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Boss Health Text");

        var showHpValueText = visuals.BossShowHpValueText;
        if (ImGui.Checkbox($"Show Remaining HP / Total HP##{categoryId}_health_show_hp_value_text", ref showHpValueText))
        {
            visuals.BossShowHpValueText = showHpValueText;
            this.onConfigChanged();
        }
        if (showHpValueText)
        {
            var valueFontSize = visuals.BossHpValueTextFontSize;
            var valueOffset = visuals.BossHpValueTextOffset;
            var valueUseGlobalFont = visuals.BossHpValueTextUseGlobalFont ?? visuals.BossHpValueTextFontFamilyId == 0;
            var valueFontFamilyId = visuals.BossHpValueTextFontFamilyId;
            DrawBossTextDetailControls(
                categoryId,
                "boss_hp_value_text",
                ref valueFontSize,
                ref valueOffset,
                ref valueUseGlobalFont,
                ref valueFontFamilyId);
            visuals.BossHpValueTextFontSize = valueFontSize;
            visuals.BossHpValueTextOffset = valueOffset;
            visuals.BossHpValueTextUseGlobalFont = valueUseGlobalFont;
            visuals.BossHpValueTextFontFamilyId = valueFontFamilyId;
        }

        var showHpPercentText = visuals.BossShowHpPercentText;
        if (ImGui.Checkbox($"Show HP % Remaining##{categoryId}_health_show_hp_percent_text", ref showHpPercentText))
        {
            visuals.BossShowHpPercentText = showHpPercentText;
            this.onConfigChanged();
        }
        if (showHpPercentText)
        {
            var percentFontSize = visuals.BossHpPercentTextFontSize;
            var percentOffset = visuals.BossHpPercentTextOffset;
            var percentUseGlobalFont = visuals.BossHpPercentTextUseGlobalFont ?? visuals.BossHpPercentTextFontFamilyId == 0;
            var percentFontFamilyId = visuals.BossHpPercentTextFontFamilyId;
            DrawBossTextDetailControls(
                categoryId,
                "boss_hp_percent_text",
                ref percentFontSize,
                ref percentOffset,
                ref percentUseGlobalFont,
                ref percentFontFamilyId);
            visuals.BossHpPercentTextFontSize = percentFontSize;
            visuals.BossHpPercentTextOffset = percentOffset;
            visuals.BossHpPercentTextUseGlobalFont = percentUseGlobalFont;
            visuals.BossHpPercentTextFontFamilyId = percentFontFamilyId;
        }
    }

    private void DrawBossTextDetailControls(
        string categoryId,
        string controlId,
        ref float fontSize,
        ref Vector2 offset,
        ref bool useGlobalFont,
        ref int fontFamilyId)
    {
        ImGui.Indent();

        var localFontSize = fontSize;
        if (ImGui.DragFloat($"Font Size##{categoryId}_{controlId}_font_size", ref localFontSize, 0.25f, 8f, 64f, "%.1f"))
        {
            fontSize = Math.Clamp(localFontSize, 8f, 64f);
            this.onConfigChanged();
        }

        var localOffset = offset;
        if (ImGui.DragFloat2($"Offset (X/Y)##{categoryId}_{controlId}_offset", ref localOffset, 0.5f, -600f, 600f, "%.1f"))
        {
            offset = localOffset;
            this.onConfigChanged();
        }

        if (ImGui.Checkbox($"Use Global Default Font##{categoryId}_{controlId}_use_global_font", ref useGlobalFont))
        {
            this.onConfigChanged();
        }

        var (ids, labels) = GameFontRegistry.GetFontOptions();
        var normalized = GameFontRegistry.NormalizeFamilyId(fontFamilyId);
        var current = Array.IndexOf(ids, normalized);
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
            if (ImGui.Combo($"Font##{categoryId}_{controlId}_font", ref current, labels, labels.Length))
            {
                var selectedId = current >= 0 && current < ids.Length ? ids[current] : 0;
                fontFamilyId = GameFontRegistry.NormalizeFamilyId(selectedId);
                this.onConfigChanged();
            }
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

    internal static CategoryVisualSettings CloneVisualSettings(CategoryVisualSettings source)
    {
        var clone = new CategoryVisualSettings
        {
            HealthBarEnabled = source.HealthBarEnabled,
            HealthBarCornerRoundness = source.HealthBarCornerRoundness,
            UseCustomHealthBarColors = source.UseCustomHealthBarColors,
            HealthBarFillColor = source.HealthBarFillColor,
            HealthBarBackgroundColor = source.HealthBarBackgroundColor,
            HealthBarBorderColor = source.HealthBarBorderColor,
            BossShowHpValueText = source.BossShowHpValueText,
            BossShowHpPercentText = source.BossShowHpPercentText,
            BossHpValueTextFontSize = source.BossHpValueTextFontSize,
            BossHpPercentTextFontSize = source.BossHpPercentTextFontSize,
            BossHpValueTextOffset = source.BossHpValueTextOffset,
            BossHpPercentTextOffset = source.BossHpPercentTextOffset,
            BossHpValueTextUseGlobalFont = source.BossHpValueTextUseGlobalFont,
            BossHpPercentTextUseGlobalFont = source.BossHpPercentTextUseGlobalFont,
            BossHpValueTextFontFamilyId = source.BossHpValueTextFontFamilyId,
            BossHpPercentTextFontFamilyId = source.BossHpPercentTextFontFamilyId,
            NameTextEnabled = source.NameTextEnabled,
            NameTextFontSize = source.NameTextFontSize,
            NameTextAlignment = source.NameTextAlignment,
            TitleTextEnabled = source.TitleTextEnabled,
            TitleTextFontSize = source.TitleTextFontSize,
            TitleTextAlignment = source.TitleTextAlignment,
            TitleTextUseGlobalFont = source.TitleTextUseGlobalFont,
            TitleTextFontFamilyId = source.TitleTextFontFamilyId,
            JobIconEnabled = source.JobIconEnabled,
            JobIconNameTextEdge = source.JobIconNameTextEdge,
            JobIconNameTextGap = source.JobIconNameTextGap,
            JobIconType = source.JobIconType,
            TargetIndicatorEnabled = source.TargetIndicatorEnabled,
            TargetIndicatorCenterWithHealthBar = source.TargetIndicatorCenterWithHealthBar,
            CastBarEnabled = source.CastBarEnabled,
            CastBarCornerRoundness = source.CastBarCornerRoundness,
            UseCustomCastBarColors = source.UseCustomCastBarColors,
            CastBarFillColor = source.CastBarFillColor,
            CastBarBackgroundColor = source.CastBarBackgroundColor,
            CastBarBorderColor = source.CastBarBorderColor,
            CastBarInterruptibleColor = source.CastBarInterruptibleColor,
            CastBarNotInterruptibleColor = source.CastBarNotInterruptibleColor,
            CastBarTextEnabled = source.CastBarTextEnabled,
            CastBarTextFontSize = source.CastBarTextFontSize,
            CastBarTextAlignment = source.CastBarTextAlignment,
            BuffRowEnabled = source.BuffRowEnabled,
            BuffRowCenterWithHealthBar = source.BuffRowCenterWithHealthBar,
            BuffRowScale = source.BuffRowScale,
            DebuffRowEnabled = source.DebuffRowEnabled,
            DebuffRowCenterWithHealthBar = source.DebuffRowCenterWithHealthBar,
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

    internal static void ApplyVisualSettings(CategoryVisualSettings destination, CategoryVisualSettings source)
    {
        destination.HealthBarEnabled = source.HealthBarEnabled;
        destination.HealthBarCornerRoundness = source.HealthBarCornerRoundness;
        destination.UseCustomHealthBarColors = source.UseCustomHealthBarColors;
        destination.HealthBarFillColor = source.HealthBarFillColor;
        destination.HealthBarBackgroundColor = source.HealthBarBackgroundColor;
        destination.HealthBarBorderColor = source.HealthBarBorderColor;
        destination.BossShowHpValueText = source.BossShowHpValueText;
        destination.BossShowHpPercentText = source.BossShowHpPercentText;
        destination.BossHpValueTextFontSize = source.BossHpValueTextFontSize;
        destination.BossHpPercentTextFontSize = source.BossHpPercentTextFontSize;
        destination.BossHpValueTextOffset = source.BossHpValueTextOffset;
        destination.BossHpPercentTextOffset = source.BossHpPercentTextOffset;
        destination.BossHpValueTextUseGlobalFont = source.BossHpValueTextUseGlobalFont;
        destination.BossHpPercentTextUseGlobalFont = source.BossHpPercentTextUseGlobalFont;
        destination.BossHpValueTextFontFamilyId = source.BossHpValueTextFontFamilyId;
        destination.BossHpPercentTextFontFamilyId = source.BossHpPercentTextFontFamilyId;
        destination.NameTextEnabled = source.NameTextEnabled;
        destination.NameTextFontSize = source.NameTextFontSize;
        destination.NameTextAlignment = source.NameTextAlignment;
        destination.TitleTextEnabled = source.TitleTextEnabled;
        destination.TitleTextFontSize = source.TitleTextFontSize;
        destination.TitleTextAlignment = source.TitleTextAlignment;
        destination.TitleTextUseGlobalFont = source.TitleTextUseGlobalFont;
        destination.TitleTextFontFamilyId = source.TitleTextFontFamilyId;
        destination.JobIconEnabled = source.JobIconEnabled;
        destination.JobIconNameTextEdge = source.JobIconNameTextEdge;
        destination.JobIconNameTextGap = source.JobIconNameTextGap;
        destination.JobIconType = source.JobIconType;
        destination.TargetIndicatorEnabled = source.TargetIndicatorEnabled;
        destination.TargetIndicatorCenterWithHealthBar = source.TargetIndicatorCenterWithHealthBar;
        destination.CastBarEnabled = source.CastBarEnabled;
        destination.CastBarCornerRoundness = source.CastBarCornerRoundness;
        destination.UseCustomCastBarColors = source.UseCustomCastBarColors;
        destination.CastBarFillColor = source.CastBarFillColor;
        destination.CastBarBackgroundColor = source.CastBarBackgroundColor;
        destination.CastBarBorderColor = source.CastBarBorderColor;
        destination.CastBarInterruptibleColor = source.CastBarInterruptibleColor;
        destination.CastBarNotInterruptibleColor = source.CastBarNotInterruptibleColor;
        destination.CastBarTextEnabled = source.CastBarTextEnabled;
        destination.CastBarTextFontSize = source.CastBarTextFontSize;
        destination.CastBarTextAlignment = source.CastBarTextAlignment;
        destination.BuffRowEnabled = source.BuffRowEnabled;
        destination.BuffRowCenterWithHealthBar = source.BuffRowCenterWithHealthBar;
        destination.BuffRowScale = source.BuffRowScale;
        destination.DebuffRowEnabled = source.DebuffRowEnabled;
        destination.DebuffRowCenterWithHealthBar = source.DebuffRowCenterWithHealthBar;
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
