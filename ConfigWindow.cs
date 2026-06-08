using Dalamud.Bindings.ImGui;
using System.IO;
using System.Numerics;

namespace FFXIVHudPlugin;

public sealed class ConfigWindow
{
    private const float NavSidebarWidth = 268f;
    private const float NavButtonHeight = 30f;
    private const float PresetButtonHeight = 36f;
    private const bool ShowDeveloperMinimapAdvancedSection = false;
    private const bool ShowDeveloperActionCameraAdvancedSection = false;

    private readonly HudConfiguration config;
    private readonly AetherPlates.UI.ConfigWindow aetherPlatesConfigWindow;
    private readonly Action<ActionCameraConfiguration>? onActionCameraConfigChanged;
    private readonly HudStateProvider? stateProvider;
    private string customLayoutNameBuffer = string.Empty;
    private int selectedCustomLayoutIndex = -1;
    private string pendingDeleteCustomLayoutName = string.Empty;
    private bool requestOpenDeleteCustomLayoutPopup;
    private bool requestOpenDeleteAllCustomLayoutsPopup;
    private ConfigBucket selectedBucket = ConfigBucket.HudLayout;

    public ConfigWindow(
        HudConfiguration config,
        HudStateProvider? stateProvider = null,
        Action<ActionCameraConfiguration>? onActionCameraConfigChanged = null)
    {
        this.config = config;
        this.stateProvider = stateProvider;
        this.onActionCameraConfigChanged = onActionCameraConfigChanged;
        this.aetherPlatesConfigWindow = new AetherPlates.UI.ConfigWindow(
            this.config.AetherPlates,
            this.config.Save,
            this.stateProvider?.TextureProvider ?? throw new InvalidOperationException("Texture provider is required for nameplate designer."));
        this.SyncCustomLayoutSelectionIndex();
    }

    public bool IsOpen { get; set; }
    public void SelectMinimapTab() => this.selectedBucket = ConfigBucket.Minimap;

    public void Draw()
    {
        if (!this.IsOpen)
        {
            return;
        }

        var isOpen = this.IsOpen;
        ImGui.SetNextWindowSize(new Vector2(816f, 560f), ImGuiCond.FirstUseEver);
        var io = ImGui.GetIO();
        var previousConfigFlags = io.ConfigFlags;
        var previousMouseDrawCursor = io.MouseDrawCursor;
        io.ConfigFlags |= ImGuiConfigFlags.NoMouseCursorChange;
        io.MouseDrawCursor = true;
        try
        {
            if (!ImGui.Begin(
                    "FFXIV Hud Reimagined Config",
                    ref isOpen,
                    ImGuiWindowFlags.NoNavInputs))
            {
                this.IsOpen = isOpen;
                ImGui.End();
                return;
            }

            this.IsOpen = isOpen;

            ImGui.BeginChild("##ConfigNavSidebar", new Vector2(NavSidebarWidth, 0f), false);
            this.DrawSettingsNavSidebar();
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("##ConfigSettingsContent", new Vector2(0f, 0f), false);
            this.DrawSelectedSettingsTab();
            ImGui.EndChild();

            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
            }

            this.DrawDeleteCustomLayoutConfirmPopup();
            this.DrawDeleteAllCustomLayoutsConfirmPopup();
            ImGui.End();
        }
        finally
        {
            io.ConfigFlags = previousConfigFlags;
            io.MouseDrawCursor = previousMouseDrawCursor;
        }
    }

    private void DrawSettingsNavSidebar()
    {
        ImGui.TextUnformatted("Components");
        ImGui.Separator();
        ImGui.Spacing();

        if (this.DrawSettingsNavButton(ConfigBucket.HudLayout, "HUD Layout"))
        {
            this.selectedBucket = ConfigBucket.HudLayout;
        }

        if (this.DrawSettingsNavButton(ConfigBucket.Minimap, "Minimap"))
        {
            this.selectedBucket = ConfigBucket.Minimap;
        }

        if (this.DrawSettingsNavButton(ConfigBucket.ActionCamera, "Action Camera"))
        {
            this.selectedBucket = ConfigBucket.ActionCamera;
        }

        if (this.DrawSettingsNavButton(ConfigBucket.Nameplate, "Nameplate"))
        {
            this.selectedBucket = ConfigBucket.Nameplate;
        }
    }

    private bool DrawSettingsNavButton(ConfigBucket bucket, string label)
    {
        var selected = this.selectedBucket == bucket;
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
        switch (this.selectedBucket)
        {
            case ConfigBucket.HudLayout:
                this.DrawHudLayoutBucket();
                break;
            case ConfigBucket.Minimap:
                this.DrawMinimapBucket();
                break;
            case ConfigBucket.ActionCamera:
                this.DrawActionCameraBucket();
                break;
            case ConfigBucket.Nameplate:
                this.DrawNameplateBucket();
                break;
            default:
                this.DrawHudLayoutBucket();
                break;
        }
    }

    private void DrawHudLayoutBucket()
    {
        if (ImGui.BeginTabBar("##HudLayoutTabs"))
        {
            if (ImGui.BeginTabItem("General Settings"))
            {
                this.DrawGeneralSettingsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Parameter Orb Settings"))
            {
                this.DrawOrbSettingsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Hotbar Settings"))
            {
                this.DrawHotbarSettingsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Buff/Debuff Settings"))
            {
                this.DrawBuffDebuffSettingsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawNameplateBucket()
    {
        if (ImGui.BeginTabBar("##NameplateTabs"))
        {
            if (ImGui.BeginTabItem("General Settings"))
            {
                this.aetherPlatesConfigWindow.DrawGeneralSettingsSection();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Category Designer"))
            {
                this.aetherPlatesConfigWindow.DrawCategoryDesignerSection();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawMinimapBucket()
    {
        if (ImGui.BeginTabBar("##MinimapTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                this.DrawMinimapGeneralTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Layout"))
            {
                this.DrawMinimapLayoutTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Markers"))
            {
                this.DrawMinimapMarkersTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawActionCameraBucket()
    {
        if (ImGui.BeginTabBar("##ActionCameraTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                this.DrawActionCameraSettingsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawActionCameraSettingsTab()
    {
        ImGui.TextUnformatted("Action Camera");
        ImGui.Spacing();
        ImGui.TextColored(0xFF9AA1AB, "Standalone camera-control mode for mouse and keyboard.");
        ImGui.TextColored(0xFF9AA1AB, "Preserves vanilla combat and targeting. No action-combat logic.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var actionConfig = this.config.ActionCamera;

        ImGui.TextColored(0xFF9AA1AB, "Camera Backend: RMB Latch");

        var enabled = actionConfig.Enabled;
        if (DrawSettingCheckbox("Enable Action Camera", ref enabled))
        {
            actionConfig.Enabled = enabled;
            this.NotifyActionCameraConfigChanged();
        }

        var unlockMode = actionConfig.UnlockMode;
        if (this.DrawActionCameraUnlockModeCombo(ref unlockMode))
        {
            actionConfig.UnlockMode = unlockMode;
            this.NotifyActionCameraConfigChanged();
        }

        if (actionConfig.UnlockMode == ActionCameraUnlockMode.Toggle)
        {
            var toggleKey = actionConfig.ToggleUnlockKey;
            if (this.DrawActionCameraKeyCombo("Toggle Unlock Key", ref toggleKey))
            {
                actionConfig.ToggleUnlockKey = toggleKey;
                this.NotifyActionCameraConfigChanged();
            }
        }
        else
        {
            var holdKey = actionConfig.HoldUnlockKey;
            if (this.DrawActionCameraKeyCombo("Hold Unlock Key", ref holdKey))
            {
                actionConfig.HoldUnlockKey = holdKey;
                this.NotifyActionCameraConfigChanged();
            }
        }

        ImGui.TextColored(0xFF9AA1AB, "Auto Unlock During UI Interaction: Always On");
        ImGui.TextColored(0xFF9AA1AB, "Escape Always Unlocks Cursor: Always On");

        ImGui.TextColored(0xFF9AA1AB, "Reacquire On Toggle Key: Internal Setting");
        ImGui.TextColored(0xFF9AA1AB, "Prevent RMB Camera Disruption: Internal Setting");

        var showReticle = actionConfig.ShowReticle;
        if (DrawSettingCheckbox("Show Center Reticle", ref showReticle))
        {
            actionConfig.ShowReticle = showReticle;
            this.NotifyActionCameraConfigChanged();
        }

        var softTarget = actionConfig.EnableSoftTargetSuggestion;
        if (DrawSettingCheckbox("Enable Soft Targeting", ref softTarget))
        {
            actionConfig.EnableSoftTargetSuggestion = softTarget;
            this.NotifyActionCameraConfigChanged();
        }

        var autoTarget = actionConfig.AutoTarget;
        if (DrawSettingCheckbox("Auto Target Reticle Candidate", ref autoTarget))
        {
            actionConfig.AutoTarget = autoTarget;
            this.NotifyActionCameraConfigChanged();
        }

        var softRadius = actionConfig.SoftTargetScreenRadius;
        if (DrawPreciseFloat("Soft Target Radius (px)", ref softRadius, 80f, 1200f, "%.0f", 5f))
        {
            actionConfig.SoftTargetScreenRadius = softRadius;
            this.NotifyActionCameraConfigChanged();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Sensitivity");

        var horizontal = actionConfig.HorizontalSensitivity;
        if (DrawPreciseFloat("Horizontal Sensitivity", ref horizontal, 0.1f, 5.0f, "%.2f", 0.05f))
        {
            actionConfig.HorizontalSensitivity = horizontal;
            this.NotifyActionCameraConfigChanged();
        }

        var vertical = actionConfig.VerticalSensitivity;
        if (DrawPreciseFloat("Vertical Sensitivity", ref vertical, 0.1f, 5.0f, "%.2f", 0.05f))
        {
            actionConfig.VerticalSensitivity = vertical;
            this.NotifyActionCameraConfigChanged();
        }

        if (ShowDeveloperActionCameraAdvancedSection)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextUnformatted("Advanced");
            ImGui.TextColored(0xFF9AA1AB, "Enable only when troubleshooting camera behavior.");

            var showDebug = actionConfig.ShowDebugOverlay;
            if (DrawSettingCheckbox("Show Debug Overlay", ref showDebug))
            {
                actionConfig.ShowDebugOverlay = showDebug;
                this.NotifyActionCameraConfigChanged();
            }
        }
    }

    private void NotifyActionCameraConfigChanged()
    {
        this.config.Save();
        this.onActionCameraConfigChanged?.Invoke(this.config.ActionCamera);
    }

    private bool DrawActionCameraUnlockModeCombo(ref ActionCameraUnlockMode mode)
    {
        var index = mode == ActionCameraUnlockMode.Toggle ? 1 : 0;
        var labels = new[] { "Hold", "Toggle" };
        if (!ImGui.Combo("Unlock Mode", ref index, labels, labels.Length))
        {
            return false;
        }

        mode = index == 1 ? ActionCameraUnlockMode.Toggle : ActionCameraUnlockMode.Hold;
        return true;
    }

    private bool DrawActionCameraKeyCombo(string label, ref Dalamud.Game.ClientState.Keys.VirtualKey key)
    {
        var keys = label.Contains("Toggle", StringComparison.OrdinalIgnoreCase)
            ? ActionCameraKeyPresets.ToggleKeys
            : ActionCameraKeyPresets.HoldKeys;
        return this.DrawSpecificKeyCombo(label, keys, ref key);
    }

    private bool DrawSpecificKeyCombo(
        string label,
        Dalamud.Game.ClientState.Keys.VirtualKey[] keys,
        ref Dalamud.Game.ClientState.Keys.VirtualKey key)
    {
        var labels = new string[keys.Length];
        var selectedIndex = 0;
        for (var i = 0; i < keys.Length; i++)
        {
            labels[i] = ActionCameraKeyPresets.GetLabel(keys[i]);
            if (keys[i] == key)
            {
                selectedIndex = i;
            }
        }

        if (!ImGui.Combo(label, ref selectedIndex, labels, labels.Length))
        {
            return false;
        }

        key = keys[selectedIndex];
        return true;
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
        if (DrawSettingCheckbox("Enable HUD Reimagined", ref enabled))
        {
            this.config.Enabled = enabled;
            this.config.Save();
        }

        var statusTooltips = this.config.EnableStatusTooltips;
        if (DrawSettingCheckbox("Enable Hotbar Tooltips", ref statusTooltips))
        {
            this.config.EnableStatusTooltips = statusTooltips;
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
                this.requestOpenDeleteCustomLayoutPopup = true;
            }
        }

        if (!canDelete)
        {
            ImGui.EndDisabled();
        }

        ImGui.Spacing();
        var canDeleteAny = this.config.CustomLayouts.Count > 0;
        if (!canDeleteAny)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Delete All Layouts", new Vector2(-1f, 0f)))
        {
            this.requestOpenDeleteAllCustomLayoutsPopup = true;
        }

        if (!canDeleteAny)
        {
            ImGui.EndDisabled();
        }

        ImGui.EndChild();
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

        var showSlidecast = this.config.ShowSlidecastMarker;
        if (DrawSettingCheckbox("Show Slidecast Marker", ref showSlidecast))
        {
            this.config.ShowSlidecastMarker = showSlidecast;
            this.config.Save();
        }

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

    private void DrawMinimapGeneralTab()
    {
        ImGui.TextUnformatted("General");
        ImGui.Spacing();

        var minimapEnabled = this.config.MinimapEnabled;
        if (DrawSettingCheckbox("Minimap Enabled", ref minimapEnabled))
        {
            this.config.MinimapEnabled = minimapEnabled;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "Custom minimap using the zone map texture. Hides the game's _NaviMap while enabled.");

        var squareMinimap = this.config.MinimapSquare;
        if (DrawSettingCheckbox("Square Minimap", ref squareMinimap))
        {
            this.config.MinimapSquare = squareMinimap;
            this.config.Save();
        }

        var northLocked = this.config.MinimapNorthLocked;
        if (DrawSettingCheckbox("Lock North Up", ref northLocked))
        {
            this.config.MinimapNorthLocked = northLocked;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "North stays at the top; the facing cone follows your camera. Syncs the game's compass lock.");

        var showCardinalDirections = this.config.MinimapShowCardinalDirections;
        if (DrawSettingCheckbox("Show Cardinal Directions", ref showCardinalDirections))
        {
            this.config.MinimapShowCardinalDirections = showCardinalDirections;
            this.config.Save();
        }

        ImGui.TextColored(0xFF9AA1AB, "Shows N / E / S / W labels around the minimap edge.");
    }

    private void DrawMinimapLayoutTab()
    {
        ImGui.TextUnformatted("Layout");
        ImGui.Spacing();

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
    }

    private void DrawMinimapMarkersTab()
    {
        ImGui.TextUnformatted("Map Markers");
        ImGui.Spacing();
        ImGui.TextColored(
            0xFF9AA1AB,
            "Map flag, gathering, FATEs (FateManager), quest events, then minimap pins from AgentMap.");

        var showNativeMarkers = this.config.MinimapShowNativeMarkers;
        if (DrawSettingCheckbox("Show Map Markers", ref showNativeMarkers))
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
        ImGui.TextUnformatted("Player Pin");
        ImGui.TextColored(0xFF9AA1AB, "Teardrop at center: points where your character faces (cone still follows camera).");

        var useRolePinColor = this.config.MinimapUseRolePinColor;
        if (DrawSettingCheckbox("Use Role Color", ref useRolePinColor))
        {
            this.config.MinimapUseRolePinColor = useRolePinColor;
            this.config.Save();
        }

        ImGui.TextColored(
            0xFF9AA1AB,
            "Tank, Healer, DPS, or Crafter/Gatherer — uses the official in-game role palette.");

        var playerPinSize = this.config.MinimapPlayerPinSize;
        if (DrawPreciseFloat(
                "Player Pin Size",
                ref playerPinSize,
                MinimapLayout.MinPlayerPinSize,
                MinimapLayout.MaxPlayerPinSize,
                "%.1f",
                0.5f))
        {
            this.config.MinimapPlayerPinSize = MinimapLayout.ClampPlayerPinSize(playerPinSize);
            this.config.Save();
        }

        if (!this.config.MinimapUseRolePinColor)
        {
            var playerPinColor = this.config.MinimapPlayerPinColor;
            if (this.DrawColorPicker("Player Pin Color", ref playerPinColor))
            {
                this.config.MinimapPlayerPinColor = playerPinColor;
                this.config.Save();
            }
        }
        else
        {
            ImGui.TextColored(0xFF9AA1AB, "Custom pin color is used only when role color cannot be resolved.");
        }

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

        if (ShowDeveloperMinimapAdvancedSection)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            this.DrawMinimapAdvancedSection();
        }
    }

    private void DrawMinimapAdvancedSection()
    {
        ImGui.TextUnformatted("Advanced");
        ImGui.TextColored(
            0xFF9AA1AB,
            "Diagnostics are intended for troubleshooting marker/map issues and are disabled for normal play.");
        var assembly = typeof(Plugin).Assembly;
        var buildVersion = assembly.GetName().Version;
        ImGui.TextColored(
            0xFF6B9E6B,
            buildVersion is null
                ? "Loaded build: unknown"
                : $"Loaded build: {buildVersion}");

        var assemblyPath = assembly.Location;
        ImGui.TextColored(
            0xFF9AA1AB,
            string.IsNullOrWhiteSpace(assemblyPath)
                ? "Loaded assembly path: unknown"
                : $"Loaded assembly path: {assemblyPath}");

        var loadedWriteTime = string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath)
            ? "unknown"
            : File.GetLastWriteTime(assemblyPath).ToString("yyyy-MM-dd HH:mm:ss");
        ImGui.TextColored(0xFF9AA1AB, $"Loaded assembly write time (local): {loadedWriteTime}");

        var showDiagnostics = this.config.MinimapShowDiagnostics;
        if (DrawSettingCheckbox("Enable Diagnostics", ref showDiagnostics))
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
        if (DrawSettingCheckbox("Show Test Buffs & Debuffs (1 row each)", ref showTestStatusEffects))
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
        if (this.requestOpenDeleteCustomLayoutPopup)
        {
            ImGui.OpenPopup("ConfirmDeleteCustomLayout");
            this.requestOpenDeleteCustomLayoutPopup = false;
        }

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

        if (ImGui.Button("Yes", new Vector2(120f, 0f)))
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
        if (ImGui.Button("No", new Vector2(120f, 0f)))
        {
            this.pendingDeleteCustomLayoutName = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawDeleteAllCustomLayoutsConfirmPopup()
    {
        if (this.requestOpenDeleteAllCustomLayoutsPopup)
        {
            ImGui.OpenPopup("ConfirmDeleteAllCustomLayouts");
            this.requestOpenDeleteAllCustomLayoutsPopup = false;
        }

        var center = ImGui.GetMainViewport().Pos + (ImGui.GetMainViewport().Size * 0.5f);
        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        if (!ImGui.BeginPopupModal("ConfirmDeleteAllCustomLayouts", ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        ImGui.TextUnformatted("Delete all custom layouts?");
        ImGui.Spacing();
        ImGui.TextColored(0xFFFF8080, "This will remove every saved custom layout and cannot be undone.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Yes", new Vector2(120f, 0f)))
        {
            if (this.config.CustomLayouts.Count > 0)
            {
                this.config.CustomLayouts.Clear();
                this.config.SelectedCustomLayoutName = string.Empty;
                this.selectedCustomLayoutIndex = -1;
                this.pendingDeleteCustomLayoutName = string.Empty;
                this.config.Save();
            }

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("No", new Vector2(120f, 0f)))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void SyncCustomLayoutSelectionIndex()
    {
        this.config.CustomLayouts ??= new();
        if (this.config.CustomLayouts.Count == 0)
        {
            this.selectedCustomLayoutIndex = -1;
            return;
        }

        if (string.IsNullOrWhiteSpace(this.config.SelectedCustomLayoutName))
        {
            if (this.selectedCustomLayoutIndex < 0 || this.selectedCustomLayoutIndex >= this.config.CustomLayouts.Count)
            {
                this.selectedCustomLayoutIndex = 0;
            }

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

        if (this.selectedCustomLayoutIndex < 0 || this.selectedCustomLayoutIndex >= this.config.CustomLayouts.Count)
        {
            this.selectedCustomLayoutIndex = 0;
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
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        var changed = ImGui.ColorEdit4("##value", ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoLabel);
        ImGui.SameLine(0f, 6f);
        ImGui.TextUnformatted(label);
        ImGui.PopID();
        if (!changed)
        {
            return false;
        }

        argbColor = HudColorConversion.ToImGuiColor(color);
        return true;
    }

    /// <summary>
    /// Checkbox with a separate text label so the label does not fight the checkbox for hover (hand vs arrow flicker).
    /// </summary>
    private static bool DrawSettingCheckbox(string label, ref bool value)
    {
        ImGui.PushID(label);
        var changed = ImGui.Checkbox(label, ref value);
        ImGui.PopID();
        return changed;
    }

    private static bool DrawPreciseFloat(
        string label,
        ref float value,
        float min,
        float max,
        string format,
        float step = DefaultFloatStep)
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - ImGui.CalcTextSize(label).X - 12f);
        var changed = ImGui.DragFloat("##value", ref value, step, min, max, format);
        if (changed)
        {
            value = SnapToStep(value, min, max, step);
        }

        ImGui.SameLine(0f, 6f);
        ImGui.TextUnformatted(label);
        ImGui.PopID();
        return changed;
    }

    private static bool DrawPreciseInt(string label, ref int value, int min, int max)
    {
        ImGui.PushID(label);
        ImGui.AlignTextToFramePadding();
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - ImGui.CalcTextSize(label).X - 12f);
        var changed = ImGui.DragInt("##value", ref value, 0.05f, min, max);
        if (changed)
        {
            value = Math.Clamp(value, min, max);
        }

        ImGui.SameLine(0f, 6f);
        ImGui.TextUnformatted(label);
        ImGui.PopID();
        return changed;
    }

    private static bool DrawSectionHeaderWithEnable(string title, ref bool enabled)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(title);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - 4f);
        return ImGui.Checkbox($"##sectionEnabled_{title}", ref enabled);
    }

    private enum ConfigBucket
    {
        HudLayout = 0,
        Minimap = 1,
        ActionCamera = 2,
        Nameplate = 3,
    }

}
