using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin;

public sealed class ConfigWindow
{
    private const float NavSidebarWidth = 268f;
    private const float NavButtonHeight = 30f;
    private const float PresetButtonHeight = 36f;

    private readonly HudConfiguration config;
    private readonly HudStateProvider? stateProvider;
    private string customLayoutNameBuffer = string.Empty;
    private int selectedCustomLayoutIndex = -1;
    private string pendingDeleteCustomLayoutName = string.Empty;
    private ConfigSettingsTab selectedTab = ConfigSettingsTab.General;

    public ConfigWindow(HudConfiguration config, HudStateProvider? stateProvider = null)
    {
        this.config = config;
        this.stateProvider = stateProvider;
        this.SyncCustomLayoutSelectionIndex();
    }

    public bool IsOpen { get; set; }

    public void Draw()
    {
        if (!this.IsOpen)
        {
            return;
        }

        var isOpen = this.IsOpen;
        ImGui.SetNextWindowSize(new Vector2(816f, 560f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("FFXIV Hud Reimagined Config", ref isOpen))
        {
            this.IsOpen = isOpen;
            ImGui.End();
            return;
        }

        this.IsOpen = isOpen;

        ImGui.BeginChild("##ConfigNavSidebar", new Vector2(NavSidebarWidth, 0f), true);
        this.DrawSettingsNavSidebar();
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##ConfigSettingsContent", new Vector2(0f, 0f), true);
        this.DrawSelectedSettingsTab();
        ImGui.EndChild();

        ImGui.End();
    }

    private void DrawSettingsNavSidebar()
    {
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();

        if (this.DrawSettingsNavButton(ConfigSettingsTab.General, "General Settings"))
        {
            this.selectedTab = ConfigSettingsTab.General;
        }

        if (this.DrawSettingsNavButton(ConfigSettingsTab.Orb, "Parameter Orb settings"))
        {
            this.selectedTab = ConfigSettingsTab.Orb;
        }

        if (this.DrawSettingsNavButton(ConfigSettingsTab.Hotbar, "Hotbar Settings"))
        {
            this.selectedTab = ConfigSettingsTab.Hotbar;
        }

        if (this.DrawSettingsNavButton(ConfigSettingsTab.BuffDebuff, "Buff/Debuff Settings"))
        {
            this.selectedTab = ConfigSettingsTab.BuffDebuff;
        }

        if (this.DrawSettingsNavButton(ConfigSettingsTab.Minimap, "Minimap Settings"))
        {
            this.selectedTab = ConfigSettingsTab.Minimap;
        }
    }

    private bool DrawSettingsNavButton(ConfigSettingsTab tab, string label)
    {
        var selected = this.selectedTab == tab;
        if (selected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF6B4A24);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF866034);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF9A6F3B);
        }

        var clicked = ImGui.Button(label, new Vector2(-1f, NavButtonHeight));
        if (selected)
        {
            ImGui.PopStyleColor(3);
        }

        return clicked;
    }

    private void DrawSelectedSettingsTab()
    {
        switch (this.selectedTab)
        {
            case ConfigSettingsTab.Orb:
                this.DrawOrbSettingsTab();
                break;
            case ConfigSettingsTab.Hotbar:
                this.DrawHotbarSettingsTab();
                break;
            case ConfigSettingsTab.BuffDebuff:
                this.DrawBuffDebuffSettingsTab();
                break;
            case ConfigSettingsTab.Minimap:
                this.DrawMinimapSettingsTab();
                break;
            default:
                this.DrawGeneralSettingsTab();
                break;
        }
    }

    private void DrawGeneralSettingsTab()
    {
        ImGui.TextUnformatted("General Settings");
        ImGui.Spacing();

        this.DrawPresetsSection();
        ImGui.Spacing();
        this.DrawCustomLayoutsSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var enabled = this.config.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            this.config.Enabled = enabled;
            this.config.Save();
        }

        var statusTooltips = this.config.EnableStatusTooltips;
        if (ImGui.Checkbox("Enable Tooltips", ref statusTooltips))
        {
            this.config.EnableStatusTooltips = statusTooltips;
            this.config.Save();
        }

        var showSlidecast = this.config.ShowSlidecastMarker;
        if (ImGui.Checkbox("Show Slidecast Marker", ref showSlidecast))
        {
            this.config.ShowSlidecastMarker = showSlidecast;
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Display");
        ImGui.TextColored(0xFF9AA1AB, "Values move in 0.5 steps. Double-click any value to type an exact number.");

        var scale = this.config.GlobalScale;
        if (DrawPreciseFloat("Global Scale", ref scale, 0.5f, 4.0f, "%.1f"))
        {
            this.config.GlobalScale = scale;
            this.config.Save();
        }

        var opacity = this.config.GlobalOpacity;
        if (DrawPreciseFloat("Global Opacity", ref opacity, 0.2f, 1.0f, "%.1f"))
        {
            this.config.GlobalOpacity = opacity;
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("HUD Position");

        var offsetBounds = HudLayoutOrigin.GetOffsetBounds(ImGui.GetMainViewport().Size);
        var hudOffsetX = this.config.HudOffsetX;
        if (DrawPreciseFloat("HUD Offset X", ref hudOffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.HudOffsetX = hudOffsetX;
            this.config.Save();
        }

        var hudOffsetY = this.config.HudOffsetY;
        if (DrawPreciseFloat("HUD Offset Y", ref hudOffsetY, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.HudOffsetY = hudOffsetY;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "Offsets are screen pixels from the viewport center (0,0 = middle).");
        ImGui.TextColored(0xFF9AA1AB, "HUD Offset shifts every element together.");
        ImGui.TextColored(
            0xFF9AA1AB,
            $"Range follows your resolution (±{offsetBounds.MaxX:0} X, ±{offsetBounds.MaxY:0} Y).");
    }

    private void DrawPresetsSection()
    {
        ImGui.TextUnformatted("Layout Presets");
        ImGui.TextColored(0xFF9AA1AB, "Apply a tuned baseline, then adjust individual tabs as needed.");
        this.DrawActiveLayoutLabel();
        ImGui.Spacing();

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = Math.Max(96f, (ImGui.GetContentRegionAvail().X - (spacing * 2f)) / 3f);

        if (ImGui.Button("Default", new Vector2(buttonWidth, PresetButtonHeight)))
        {
            HudConfiguration.ApplyPreset(this.config, HudPreset.Default);
            this.config.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Expanded", new Vector2(buttonWidth, PresetButtonHeight)))
        {
            HudConfiguration.ApplyPreset(this.config, HudPreset.Expanded);
            this.config.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("ARPG", new Vector2(buttonWidth, PresetButtonHeight)))
        {
            HudConfiguration.ApplyPreset(this.config, HudPreset.Arpg);
            this.config.Save();
        }
    }

    private void DrawActiveLayoutLabel()
    {
        if (!string.IsNullOrWhiteSpace(this.config.SelectedCustomLayoutName))
        {
            ImGui.TextColored(0xFFB8D4FF, $"Active layout: Custom — {this.config.SelectedCustomLayoutName}");
            return;
        }

        var presetLabel = this.config.Preset switch
        {
            HudPreset.Default => "Default",
            HudPreset.Expanded => "Expanded",
            HudPreset.Arpg => "ARPG",
            _ => this.config.Preset.ToString(),
        };
        ImGui.TextColored(0xFFB8D4FF, $"Active layout: Preset — {presetLabel}");
    }

    private void DrawCustomLayoutsSection()
    {
        ImGui.TextUnformatted("Custom Layouts");
        ImGui.TextColored(0xFF9AA1AB, "Save your current slider values, then load them from the dropdown below.");

        ImGui.Spacing();
        ImGui.BeginChild("##CustomLayoutsPanel", new Vector2(0f, 156f), true);
        this.config.CustomLayouts ??= new();
        this.SyncCustomLayoutSelectionIndex();

        if (this.config.CustomLayouts.Count == 0)
        {
            ImGui.TextColored(0xFF9AA1AB, "No saved layouts yet. Name your layout and click Save Layout.");
        }
        else
        {
            var names = new string[this.config.CustomLayouts.Count];
            for (var i = 0; i < this.config.CustomLayouts.Count; i++)
            {
                names[i] = this.config.CustomLayouts[i].Name;
            }

            ImGui.TextUnformatted("Saved layout");
            ImGui.SetNextItemWidth(-1f);
            var previousIndex = this.selectedCustomLayoutIndex;
            if (ImGui.Combo("##SavedCustomLayout", ref this.selectedCustomLayoutIndex, names, names.Length))
            {
                if (this.selectedCustomLayoutIndex >= 0 &&
                    this.selectedCustomLayoutIndex < this.config.CustomLayouts.Count &&
                    this.selectedCustomLayoutIndex != previousIndex)
                {
                    var selected = this.config.CustomLayouts[this.selectedCustomLayoutIndex];
                    if (HudConfiguration.TryApplyCustomLayout(this.config, selected.Name))
                    {
                        this.config.Save();
                    }
                }
            }
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("New layout name");
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##CustomLayoutName", "e.g. Raid UI, Leveling, etc.", ref this.customLayoutNameBuffer, 64);

        ImGui.Spacing();
        var actionWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
        if (ImGui.Button("Save Layout", new Vector2(actionWidth, 0f)))
        {
            if (HudConfiguration.TrySaveCustomLayout(this.config, this.customLayoutNameBuffer, out var error))
            {
                this.SyncCustomLayoutSelectionIndex();
                this.config.Save();
            }
            else if (error.Length > 0)
            {
                ImGui.TextColored(0xFFFF8080, error);
            }
        }

        ImGui.SameLine();
        var canDelete = this.config.CustomLayouts.Count > 0 && this.selectedCustomLayoutIndex >= 0;
        if (!canDelete)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Delete Selected", new Vector2(actionWidth, 0f)))
        {
            if (this.selectedCustomLayoutIndex >= 0 && this.selectedCustomLayoutIndex < this.config.CustomLayouts.Count)
            {
                this.pendingDeleteCustomLayoutName = this.config.CustomLayouts[this.selectedCustomLayoutIndex].Name;
                ImGui.OpenPopup("ConfirmDeleteCustomLayout");
            }
        }

        if (!canDelete)
        {
            ImGui.EndDisabled();
        }

        ImGui.EndChild();
        this.DrawDeleteCustomLayoutConfirmPopup();
    }

    private void DrawOrbSettingsTab()
    {
        var offsetBounds = HudLayoutOrigin.GetOffsetBounds(ImGui.GetMainViewport().Size);

        ImGui.TextUnformatted("HP Orb");
        ImGui.Spacing();

        var orbRadius = this.config.OrbRadius;
        if (DrawPreciseFloat("Orb Radius", ref orbRadius, 32f, 160f, "%.1f"))
        {
            this.config.OrbRadius = orbRadius;
            this.config.Save();
        }

        var orbOffsetX = this.config.OrbOffsetX;
        if (DrawPreciseFloat("Orb Offset X", ref orbOffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.OrbOffsetX = orbOffsetX;
            this.config.Save();
        }

        var orbOffsetY = this.config.OrbOffsetY;
        if (DrawPreciseFloat("Orb Offset Y", ref orbOffsetY, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.OrbOffsetY = orbOffsetY;
            this.config.Save();
        }

        var orbThickness = this.config.OrbThickness;
        if (DrawPreciseFloat("Orb Thickness", ref orbThickness, 4f, 28f, "%.1f"))
        {
            this.config.OrbThickness = orbThickness;
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("MP Ring");
        ImGui.Spacing();

        var mpThicknessScale = this.config.MpRingThicknessScale;
        if (DrawPreciseFloat("MP Ring Thickness Scale", ref mpThicknessScale, 0.2f, 1.2f, "%.1f"))
        {
            this.config.MpRingThicknessScale = mpThicknessScale;
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Castbar Settings");
        ImGui.Spacing();

        var slidecastOffset = this.config.SlidecastOffsetSeconds;
        if (DrawPreciseFloat("Slidecast Offset (seconds)", ref slidecastOffset, 0.05f, 1.20f, "%.2f", 0.05f))
        {
            this.config.SlidecastOffsetSeconds = slidecastOffset;
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Limit Break Gauge");

        var lbOffsetX = this.config.LimitBreakOffsetX;
        if (DrawPreciseFloat("LB Gauge X Offset", ref lbOffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.LimitBreakOffsetX = lbOffsetX;
            this.config.Save();
        }

        var lbOffset = this.config.LimitBreakYOffset;
        if (DrawPreciseFloat("LB Gauge Y Offset", ref lbOffset, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.LimitBreakYOffset = lbOffset;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "HP Orb and LB offsets use each element's center from screen center.");
    }

    private void DrawHotbarSettingsTab()
    {
        ImGui.TextUnformatted("Hotbar Settings");
        ImGui.Spacing();

        var offsetBounds = HudLayoutOrigin.GetOffsetBounds(ImGui.GetMainViewport().Size);

        var hotbar1Enabled = this.config.Hotbar1Enabled;
        if (DrawSectionHeaderWithEnable("Hotbar 1", ref hotbar1Enabled))
        {
            this.config.Hotbar1Enabled = hotbar1Enabled;
            this.config.Save();
        }

        var hotbar1OffsetX = this.config.Hotbar1OffsetX;
        if (DrawPreciseFloat("Hotbar 1 Offset X", ref hotbar1OffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.Hotbar1OffsetX = hotbar1OffsetX;
            this.config.Save();
        }

        var hotbar1OffsetY = this.config.Hotbar1OffsetY;
        if (DrawPreciseFloat("Hotbar 1 Offset Y", ref hotbar1OffsetY, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.Hotbar1OffsetY = hotbar1OffsetY;
            this.config.Save();
        }

        var hotbar1VisibleSlots = this.config.Hotbar1VisibleSlotCount;
        if (DrawPreciseInt("Hotbar 1 Visible Slots", ref hotbar1VisibleSlots, 1, HotbarSlotVisibility.MaxTotalSlots))
        {
            this.config.Hotbar1VisibleSlotCount = HotbarSlotVisibility.ClampTotal(hotbar1VisibleSlots);
            this.config.Save();
        }

        var hotbar1SlotsPerRow = this.config.Hotbar1SlotsPerRow;
        if (DrawPreciseInt(
                "Hotbar 1 Slots Per Row",
                ref hotbar1SlotsPerRow,
                HotbarGridLayout.MinSlotsPerRow,
                HotbarGridLayout.MaxSlotsPerRow))
        {
            this.config.Hotbar1SlotsPerRow = HotbarGridLayout.ClampSlotsPerRow(hotbar1SlotsPerRow);
            this.config.Save();
        }

        this.DrawHotbar1SlotSizeGapControls();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var hotbar2Enabled = this.config.Hotbar2Enabled;
        if (DrawSectionHeaderWithEnable("Hotbar 2", ref hotbar2Enabled))
        {
            this.config.Hotbar2Enabled = hotbar2Enabled;
            this.config.Save();
        }

        var hotbar2OffsetX = this.config.Hotbar2OffsetX;
        if (DrawPreciseFloat("Hotbar 2 Offset X", ref hotbar2OffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.Hotbar2OffsetX = hotbar2OffsetX;
            this.config.Save();
        }

        var hotbar2OffsetY = this.config.Hotbar2OffsetY;
        if (DrawPreciseFloat("Hotbar 2 Offset Y", ref hotbar2OffsetY, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.Hotbar2OffsetY = hotbar2OffsetY;
            this.config.Save();
        }

        var hotbar2VisibleSlots = this.config.Hotbar2VisibleSlotCount;
        if (DrawPreciseInt("Hotbar 2 Visible Slots", ref hotbar2VisibleSlots, 1, HotbarSlotVisibility.MaxTotalSlots))
        {
            this.config.Hotbar2VisibleSlotCount = HotbarSlotVisibility.ClampTotal(hotbar2VisibleSlots);
            this.config.Save();
        }

        var hotbar2SlotsPerRow = this.config.Hotbar2SlotsPerRow;
        if (DrawPreciseInt(
                "Hotbar 2 Slots Per Row",
                ref hotbar2SlotsPerRow,
                HotbarGridLayout.MinSlotsPerRow,
                HotbarGridLayout.MaxSlotsPerRow))
        {
            this.config.Hotbar2SlotsPerRow = HotbarGridLayout.ClampSlotsPerRow(hotbar2SlotsPerRow);
            this.config.Save();
        }

        this.DrawHotbar2SlotSizeGapControls();
    }

    private void DrawMinimapSettingsTab()
    {
        ImGui.TextUnformatted("Minimap Settings");
        ImGui.Spacing();

        ImGui.TextUnformatted("General");
        var minimapEnabled = this.config.MinimapEnabled;
        if (ImGui.Checkbox("Minimap Enabled", ref minimapEnabled))
        {
            this.config.MinimapEnabled = minimapEnabled;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "Custom minimap using the zone map texture. Hides the game's _NaviMap while enabled.");

        var squareMinimap = this.config.MinimapSquare;
        if (ImGui.Checkbox("Square Minimap", ref squareMinimap))
        {
            this.config.MinimapSquare = squareMinimap;
            this.config.Save();
        }

        var northLocked = this.config.MinimapNorthLocked;
        if (ImGui.Checkbox("Lock North Up", ref northLocked))
        {
            this.config.MinimapNorthLocked = northLocked;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "North stays at the top; the facing cone follows your camera. Syncs the game's compass lock.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Map Markers");
        ImGui.TextColored(0xFF9AA1AB, "Quest pins, shops, and other icons from the game's minimap marker list.");

        var showNativeMarkers = this.config.MinimapShowNativeMarkers;
        if (ImGui.Checkbox("Show Map Markers", ref showNativeMarkers))
        {
            this.config.MinimapShowNativeMarkers = showNativeMarkers;
            this.config.Save();
        }

        var markerIconSize = this.config.MinimapMarkerIconSize;
        if (DrawPreciseFloat(
                "Map Marker Icon Size",
                ref markerIconSize,
                MinimapLayout.MinMarkerIconSize,
                MinimapLayout.MaxMarkerIconSize,
                "%.0f",
                1f))
        {
            this.config.MinimapMarkerIconSize = MinimapLayout.ClampMarkerIconSize(markerIconSize);
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Layout");
        ImGui.TextColored(0xFF9AA1AB, "Zoom (yalms): lower = closer view, higher = more territory. Map and markers use the same range.");

        var minimapOffsetBounds = MinimapLayout.GetOffsetBounds(
            ImGui.GetMainViewport().Size,
            this.config.MinimapSize);

        var minimapSize = this.config.MinimapSize;
        if (DrawPreciseFloat("Minimap Size", ref minimapSize, MinimapLayout.MinSize, MinimapLayout.MaxSize, "%.1f"))
        {
            this.config.MinimapSize = MinimapLayout.ClampSize(minimapSize);
            this.config.Save();
        }

        var borderThickness = this.config.MinimapBorderThickness;
        if (DrawPreciseFloat(
                "Border Thickness",
                ref borderThickness,
                MinimapLayout.MinBorderThickness,
                MinimapLayout.MaxBorderThickness,
                "%.1f",
                0.5f))
        {
            this.config.MinimapBorderThickness = MinimapLayout.ClampBorderThickness(borderThickness);
            this.config.Save();
        }

        var borderColor = this.config.MinimapBorderColor;
        if (this.DrawColorPicker("Border Color", ref borderColor))
        {
            this.config.MinimapBorderColor = borderColor;
            this.config.Save();
        }

        var zoomYalms = this.config.MinimapVisibleRangeYalms;
        if (DrawPreciseFloat(
                "Zoom (yalms)",
                ref zoomYalms,
                MinimapLayout.MinVisibleRangeYalms,
                MinimapLayout.MaxVisibleRangeYalms,
                "%.1f"))
        {
            this.config.MinimapVisibleRangeYalms = MinimapLayout.ClampVisibleRange(zoomYalms);
            this.config.Save();
        }

        var minimapOffsetX = this.config.MinimapOffsetX;
        if (DrawPreciseFloat(
                "Minimap Offset X",
                ref minimapOffsetX,
                minimapOffsetBounds.MinX,
                minimapOffsetBounds.MaxX,
                "%.1f"))
        {
            this.config.MinimapOffsetX = MinimapLayout.ClampOffsetX(
                minimapOffsetX,
                ImGui.GetMainViewport().Size,
                this.config.MinimapSize);
            this.config.Save();
        }

        var minimapOffsetY = this.config.MinimapOffsetY;
        if (DrawPreciseFloat(
                "Minimap Offset Y",
                ref minimapOffsetY,
                minimapOffsetBounds.MinY,
                minimapOffsetBounds.MaxY,
                "%.1f"))
        {
            this.config.MinimapOffsetY = MinimapLayout.ClampOffsetY(
                minimapOffsetY,
                ImGui.GetMainViewport().Size,
                this.config.MinimapSize);
            this.config.Save();
        }

        ImGui.TextColored(
            0xFF9AA1AB,
            $"Offsets use screen pixels from the viewport center (±{minimapOffsetBounds.MaxX:0} X, ±{minimapOffsetBounds.MaxY:0} Y).");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Facing Cone");
        ImGui.TextColored(0xFF9AA1AB, "Orange wedge showing which way you are facing. Size is relative to the minimap radius.");

        var facingConeSize = this.config.MinimapFacingConeSizeScale;
        if (DrawPreciseFloat(
                "Facing Cone Size",
                ref facingConeSize,
                MinimapLayout.MinFacingConeSizeScale,
                MinimapLayout.MaxFacingConeSizeScale,
                "%.2f",
                0.01f))
        {
            this.config.MinimapFacingConeSizeScale = MinimapLayout.ClampFacingConeSizeScale(facingConeSize);
            this.config.Save();
        }

        var facingConeOpacity = this.config.MinimapFacingConeOpacity;
        if (DrawPreciseFloat(
                "Facing Cone Opacity",
                ref facingConeOpacity,
                MinimapLayout.MinFacingConeOpacity,
                MinimapLayout.MaxFacingConeOpacity,
                "%.2f",
                0.01f))
        {
            this.config.MinimapFacingConeOpacity = MinimapLayout.ClampFacingConeOpacity(facingConeOpacity);
            this.config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        this.DrawMinimapTroubleshootingSection();
    }

    private void DrawMinimapTroubleshootingSection()
    {
        ImGui.TextUnformatted("Troubleshooting");
        var buildVersion = typeof(Plugin).Assembly.GetName().Version;
        ImGui.TextColored(
            0xFF6B9E6B,
            buildVersion is null
                ? "Loaded build: unknown"
                : $"Loaded build: {buildVersion}");

        var showDiagnostics = this.config.MinimapShowDiagnostics;
        if (ImGui.Checkbox("Enable minimap diagnostics", ref showDiagnostics))
        {
            this.config.MinimapShowDiagnostics = showDiagnostics;
            this.config.Save();
        }

        ImGui.TextColored(
            0xFF9AA1AB,
            "Leave off during normal play. Turn on to capture a report when map or markers misbehave in a zone.");

        if (!this.config.MinimapShowDiagnostics)
        {
            return;
        }

        this.DrawMinimapDiagnosticsPanel();
    }

    private void DrawMinimapDiagnosticsPanel()
    {
        ImGui.Spacing();
        ImGui.TextColored(
            0xFF9AA1AB,
            "Stand in the problem zone with the minimap enabled, then copy the report below.");

        if (this.stateProvider is null)
        {
            ImGui.TextColored(0xFFFF8080, "Diagnostics unavailable (not in game).");
            return;
        }

        var report = this.stateProvider.MinimapDiagnostics.Text;
        if (string.IsNullOrWhiteSpace(report))
        {
            ImGui.TextColored(0xFFFFCC80, "Waiting for HUD update… move slightly or wait one frame.");
            return;
        }

        if (ImGui.Button("Copy diagnostics to clipboard"))
        {
            ImGui.SetClipboardText(report);
        }

        ImGui.SameLine();
        ImGui.TextColored(0xFF9AA1AB, "Paste into Discord, GitHub, or a text file.");

        ImGui.BeginChild("MinimapDiagnosticsScroll", new Vector2(0f, 220f), true);
        ImGui.TextUnformatted(report);
        ImGui.EndChild();
    }

    private void DrawBuffDebuffSettingsTab()
    {
        ImGui.TextUnformatted("Buff/Debuff Settings");
        ImGui.Spacing();

        ImGui.TextUnformatted("Testing");
        var showTestStatusEffects = this.config.ShowTestStatusEffects;
        if (ImGui.Checkbox("Show Test Buffs & Debuffs (1 row each)", ref showTestStatusEffects))
        {
            this.config.ShowTestStatusEffects = showTestStatusEffects;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "Fills one row per lane. Buffs show a blue \"B\" tile; debuffs show a red \"D\" tile.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var offsetBounds = HudLayoutOrigin.GetOffsetBounds(ImGui.GetMainViewport().Size);
        this.DrawBuffSettingsSection(offsetBounds);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        this.DrawDebuffSettingsSection(offsetBounds);
    }

    private void DrawDeleteCustomLayoutConfirmPopup()
    {
        var center = ImGui.GetMainViewport().Pos + (ImGui.GetMainViewport().Size * 0.5f);
        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        if (!ImGui.BeginPopupModal("ConfirmDeleteCustomLayout", ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        ImGui.TextUnformatted("Delete this custom layout?");
        ImGui.Spacing();
        ImGui.TextUnformatted($"Layout: {this.pendingDeleteCustomLayoutName}");
        ImGui.TextColored(0xFFFF8080, "This action cannot be undone.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Delete", new Vector2(120f, 0f)))
        {
            if (HudConfiguration.TryDeleteCustomLayout(this.config, this.pendingDeleteCustomLayoutName))
            {
                this.selectedCustomLayoutIndex = -1;
                this.pendingDeleteCustomLayoutName = string.Empty;
                this.SyncCustomLayoutSelectionIndex();
                this.config.Save();
            }

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120f, 0f)))
        {
            this.pendingDeleteCustomLayoutName = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void SyncCustomLayoutSelectionIndex()
    {
        this.config.CustomLayouts ??= new();
        this.selectedCustomLayoutIndex = -1;
        if (string.IsNullOrWhiteSpace(this.config.SelectedCustomLayoutName))
        {
            return;
        }

        for (var i = 0; i < this.config.CustomLayouts.Count; i++)
        {
            if (string.Equals(
                    this.config.CustomLayouts[i].Name,
                    this.config.SelectedCustomLayoutName,
                    StringComparison.OrdinalIgnoreCase))
            {
                this.selectedCustomLayoutIndex = i;
                if (string.IsNullOrWhiteSpace(this.customLayoutNameBuffer))
                {
                    this.customLayoutNameBuffer = this.config.CustomLayouts[i].Name;
                }

                return;
            }
        }
    }

    private void DrawBuffSettingsSection(ScreenOffsetBounds offsetBounds)
    {
        ImGui.TextUnformatted("Buffs");

        var buffSize = this.config.BuffIconSize;
        if (DrawPreciseFloat("Buff Icon Size", ref buffSize, 18f, 120f, "%.1f"))
        {
            this.config.BuffIconSize = buffSize;
            this.config.Save();
        }

        var buffGap = this.config.BuffIconGap;
        if (DrawPreciseFloat("Buff Icon Gap", ref buffGap, 0f, 18f, "%.1f"))
        {
            this.config.BuffIconGap = buffGap;
            this.config.Save();
        }

        var buffOffsetX = this.config.BuffOffsetX;
        if (DrawPreciseFloat("Buff X Offset", ref buffOffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.BuffOffsetX = buffOffsetX;
            this.config.Save();
        }

        var buffOffsetY = this.config.BuffOffsetY;
        if (DrawPreciseFloat("Buff Y Offset", ref buffOffsetY, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.BuffOffsetY = buffOffsetY;
            this.config.Save();
        }

        if (this.DrawStatusGrowDirectionCombo("Buff Grow Direction", this.config.BuffGrowDirection, out var buffGrowDirection))
        {
            this.config.BuffGrowDirection = buffGrowDirection;
            this.config.Save();
        }

        if (this.DrawStatusTimerPlacementCombo("Buff Timer Placement", this.config.BuffTimerPlacement, out var buffTimerPlacement))
        {
            this.config.BuffTimerPlacement = buffTimerPlacement;
            this.config.Save();
        }

        var buffMaxPerRow = this.config.BuffMaxIconsPerRow;
        if (DrawPreciseInt(
            "Buff Icons Per Row",
            ref buffMaxPerRow,
            StatusLaneLayout.MinMaxIconsPerRow,
            StatusLaneLayout.MaxMaxIconsPerRow))
        {
            this.config.BuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(buffMaxPerRow);
            this.config.Save();
        }
    }

    private void DrawDebuffSettingsSection(ScreenOffsetBounds offsetBounds)
    {
        ImGui.TextUnformatted("Debuffs");

        var debuffSize = this.config.DebuffIconSize;
        if (DrawPreciseFloat("Debuff Icon Size", ref debuffSize, 18f, 120f, "%.1f"))
        {
            this.config.DebuffIconSize = debuffSize;
            this.config.Save();
        }

        var debuffGap = this.config.DebuffIconGap;
        if (DrawPreciseFloat("Debuff Icon Gap", ref debuffGap, 0f, 18f, "%.1f"))
        {
            this.config.DebuffIconGap = debuffGap;
            this.config.Save();
        }

        var debuffOffsetX = this.config.DebuffOffsetX;
        if (DrawPreciseFloat("Debuff X Offset", ref debuffOffsetX, offsetBounds.MinX, offsetBounds.MaxX, "%.1f"))
        {
            this.config.DebuffOffsetX = debuffOffsetX;
            this.config.Save();
        }

        var debuffOffsetY = this.config.DebuffOffsetY;
        if (DrawPreciseFloat("Debuff Y Offset", ref debuffOffsetY, offsetBounds.MinY, offsetBounds.MaxY, "%.1f"))
        {
            this.config.DebuffOffsetY = debuffOffsetY;
            this.config.Save();
        }

        if (this.DrawStatusGrowDirectionCombo("Debuff Grow Direction", this.config.DebuffGrowDirection, out var debuffGrowDirection))
        {
            this.config.DebuffGrowDirection = debuffGrowDirection;
            this.config.Save();
        }

        if (this.DrawStatusTimerPlacementCombo("Debuff Timer Placement", this.config.DebuffTimerPlacement, out var debuffTimerPlacement))
        {
            this.config.DebuffTimerPlacement = debuffTimerPlacement;
            this.config.Save();
        }

        var debuffMaxPerRow = this.config.DebuffMaxIconsPerRow;
        if (DrawPreciseInt(
            "Debuff Icons Per Row",
            ref debuffMaxPerRow,
            StatusLaneLayout.MinMaxIconsPerRow,
            StatusLaneLayout.MaxMaxIconsPerRow))
        {
            this.config.DebuffMaxIconsPerRow = StatusLaneLayout.ClampMaxIconsPerRow(debuffMaxPerRow);
            this.config.Save();
        }
    }

    private bool DrawStatusGrowDirectionCombo(
        string label,
        StatusLaneGrowDirection growDirection,
        out StatusLaneGrowDirection updatedGrowDirection)
    {
        updatedGrowDirection = growDirection;
        var growIndex = (int)growDirection;
        if (growIndex < 0 || growIndex > 3)
        {
            growIndex = 0;
        }

        var growLabels = new[]
        {
            "Left to Right - Up",
            "Right to Left - Down",
            "Left to Right - Down",
            "Right to Left - Up",
        };
        if (!ImGui.Combo(label, ref growIndex, growLabels, growLabels.Length))
        {
            return false;
        }

        updatedGrowDirection = (StatusLaneGrowDirection)growIndex;
        return updatedGrowDirection != growDirection;
    }

    private bool DrawStatusTimerPlacementCombo(
        string label,
        StatusTimerPlacement timerPlacement,
        out StatusTimerPlacement updatedTimerPlacement)
    {
        updatedTimerPlacement = timerPlacement;
        var placementIndex = timerPlacement == StatusTimerPlacement.Top ? 1 : 0;
        var placementLabels = new[] { "Bottom", "Top" };
        if (!ImGui.Combo(label, ref placementIndex, placementLabels, placementLabels.Length))
        {
            return false;
        }

        updatedTimerPlacement = placementIndex == 1
            ? StatusTimerPlacement.Top
            : StatusTimerPlacement.Bottom;
        return updatedTimerPlacement != timerPlacement;
    }

    private void DrawHotbar1SlotSizeGapControls()
    {
        var slotSize = this.config.Hotbar1SlotSize;
        if (DrawPreciseFloat("Hotbar 1 Slot Size", ref slotSize, HotbarLayout.MinSlotSize, HotbarLayout.MaxSlotSize, "%.1f"))
        {
            this.config.Hotbar1SlotSize = slotSize;
            this.config.Save();
        }

        var slotGap = this.config.Hotbar1SlotGap;
        if (DrawPreciseFloat("Hotbar 1 Slot Gap", ref slotGap, HotbarLayout.MinSlotGap, HotbarLayout.MaxSlotGap, "%.1f"))
        {
            this.config.Hotbar1SlotGap = slotGap;
            this.config.Save();
        }
    }

    private void DrawHotbar2SlotSizeGapControls()
    {
        var slotSize = this.config.Hotbar2SlotSize;
        if (DrawPreciseFloat("Hotbar 2 Slot Size", ref slotSize, HotbarLayout.MinSlotSize, HotbarLayout.MaxSlotSize, "%.1f"))
        {
            this.config.Hotbar2SlotSize = slotSize;
            this.config.Save();
        }

        var slotGap = this.config.Hotbar2SlotGap;
        if (DrawPreciseFloat("Hotbar 2 Slot Gap", ref slotGap, HotbarLayout.MinSlotGap, HotbarLayout.MaxSlotGap, "%.1f"))
        {
            this.config.Hotbar2SlotGap = slotGap;
            this.config.Save();
        }
    }

    private const float DefaultFloatStep = 0.5f;

    private static float SnapToStep(float value, float min, float max, float step)
    {
        if (step <= 0f)
        {
            return Math.Clamp(value, min, max);
        }

        var snapped = MathF.Round(value / step) * step;
        return Math.Clamp(snapped, min, max);
    }

    private bool DrawColorPicker(string label, ref uint argbColor)
    {
        var color = HudColorConversion.ToVector4(argbColor);
        if (!ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.AlphaBar))
        {
            return false;
        }

        argbColor = HudColorConversion.ToImGuiColor(color);
        return true;
    }

    private static bool DrawPreciseFloat(
        string label,
        ref float value,
        float min,
        float max,
        string format,
        float step = DefaultFloatStep)
    {
        if (!ImGui.DragFloat(label, ref value, step, min, max, format))
        {
            return false;
        }

        value = SnapToStep(value, min, max, step);
        return true;
    }

    private static bool DrawPreciseInt(string label, ref int value, int min, int max)
    {
        if (!ImGui.DragInt(label, ref value, 0.05f, min, max))
        {
            return false;
        }

        value = Math.Clamp(value, min, max);
        return true;
    }

    private static bool DrawSectionHeaderWithEnable(string title, ref bool enabled)
    {
        var changed = false;
        if (!ImGui.BeginTable($"##Section_{title}", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings))
        {
            return changed;
        }

        ImGui.TableSetupColumn("##Title", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Enable", ImGuiTableColumnFlags.WidthFixed, 92f);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(title);
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            changed = true;
        }

        ImGui.EndTable();
        return changed;
    }

    private enum ConfigSettingsTab
    {
        General = 0,
        Orb = 1,
        Hotbar = 2,
        BuffDebuff = 3,
        Minimap = 4,
    }
}
