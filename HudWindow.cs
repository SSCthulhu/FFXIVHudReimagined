using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;

namespace FFXIVHudPlugin;

public sealed class HudWindow
{
    private readonly HudConfiguration config;
    private readonly HudStateProvider stateProvider;
    private bool assignWindowOpen;
    private int assignTargetBarIndex = GameHotbar.Hotbar1BarIndex;
    private int assignTargetGameSlotIndex;
    private HotbarAssignCategory assignCategory = HotbarAssignCategory.Actions;
    private HotbarOrderSection assignOrderSection = HotbarOrderSection.Companion;
    private HotbarMainCommandSection assignMainCommandSection = HotbarMainCommandSection.Character;
    private IReadOnlyList<HotbarAssignEntry> assignEntries = Array.Empty<HotbarAssignEntry>();
    private string assignFilter = string.Empty;
    private string assignLastPlayerName = string.Empty;
    private uint assignLastPlayerJobId;

    public HudWindow(HudConfiguration config, HudStateProvider stateProvider)
    {
        this.config = config;
        this.stateProvider = stateProvider;
    }

    public void Draw()
    {
        if (!this.config.Enabled)
        {
            return;
        }

        var flags = ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNav |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoInputs;

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("FFXIVHudPlugin##Overlay", flags);

        var draw = ImGui.GetWindowDrawList();
        var snapshot = this.stateProvider.Snapshot;
        var layout = HudLayoutEngine.Calculate(this.config);
        var alpha = Math.Clamp(this.config.GlobalOpacity, 0f, 1f);

        HudRenderer.DrawCenterOrb(draw, this.config, snapshot, layout, alpha);
        HudRenderer.DrawHotbars(draw, this.config, snapshot, layout, alpha);
        HudRenderer.DrawStatusRows(draw, this.config, snapshot, layout, alpha);
        HudRenderer.DrawLimitBreak(draw, this.config, snapshot, layout, alpha);
        HudRenderer.DrawCastArc(draw, this.config, snapshot, layout, alpha);

        if (this.config.MinimapEnabled)
        {
            MinimapRenderer.Draw(draw, this.config, snapshot.Minimap, layout.MinimapCenter, alpha);
        }

        ImGui.End();
        ImGui.PopStyleVar(2);

        DrawHotbarInputOverlay(this.config, layout, snapshot);
        DrawAssignWindow(layout);
    }

    private void DrawHotbarInputOverlay(HudConfiguration config, HudLayoutRects layout, HudStateSnapshot snapshot)
    {
        if (config.Hotbar1Enabled)
        {
            DrawHotbarInputGrid(
                GameHotbar.Hotbar1BarIndex,
                snapshot.LeftHotbar,
                snapshot.RightHotbar,
                layout.Hotbar1Start,
                config.Hotbar1SlotsPerRow,
                HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar1BarIndex),
                HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar1BarIndex));
        }

        if (config.Hotbar2Enabled)
        {
            DrawHotbarInputGrid(
                GameHotbar.Hotbar2BarIndex,
                snapshot.LeftHotbar2,
                snapshot.RightHotbar2,
                layout.Hotbar2Start,
                config.Hotbar2SlotsPerRow,
                HotbarLayout.GetScaledSlotSize(config, GameHotbar.Hotbar2BarIndex),
                HotbarLayout.GetScaledSlotGap(config, GameHotbar.Hotbar2BarIndex));
        }
    }

    private void DrawHotbarInputGrid(
        int barIndex,
        IReadOnlyList<HotbarSlotViewModel> leftSlots,
        IReadOnlyList<HotbarSlotViewModel> rightSlots,
        Vector2 gridStart,
        int slotsPerRow,
        float slotSize,
        float gap)
    {
        DrawHotbarInputLane(barIndex, leftSlots, gridStart, slotsPerRow, slotSize, gap);
        DrawHotbarInputLane(barIndex, rightSlots, gridStart, slotsPerRow, slotSize, gap);
    }

    private void DrawHotbarInputLane(
        int barIndex,
        IReadOnlyList<HotbarSlotViewModel> slots,
        Vector2 gridStart,
        int slotsPerRow,
        float slotSize,
        float gap)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var min = HotbarGridLayout.GetSlotTopLeft(gridStart, slot.GameSlotIndex, slotsPerRow, slotSize, gap);
            DrawHotbarInputSlot(barIndex, min, slotSize, slot.GameSlotIndex, slot.ActionId != 0);
        }
    }

    private void DrawHotbarInputSlot(int barIndex, Vector2 min, float slotSize, int gameSlotIndex, bool hasAction)
    {
        var flags = ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNav |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoBackground;

        ImGui.SetNextWindowPos(min);
        ImGui.SetNextWindowSize(new Vector2(slotSize, slotSize));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin($"FFXIVHudPlugin##HotbarInputSlot_{barIndex}_{gameSlotIndex}", flags);
        var clicked = ImGui.InvisibleButton("##HotbarInputButton", new Vector2(slotSize, slotSize));
        if (clicked && hasAction)
        {
            this.stateProvider.TryExecuteHotbarSlot(barIndex, gameSlotIndex);
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
        {
            var isSameTarget =
                this.assignWindowOpen &&
                this.assignTargetBarIndex == barIndex &&
                this.assignTargetGameSlotIndex == gameSlotIndex;
            if (isSameTarget)
            {
                this.assignWindowOpen = false;
            }
            else
            {
                this.assignTargetBarIndex = barIndex;
                this.assignTargetGameSlotIndex = gameSlotIndex;
                this.assignWindowOpen = true;
                this.assignCategory = HotbarAssignCategory.Actions;
                this.assignOrderSection = HotbarOrderSection.Companion;
                this.assignMainCommandSection = HotbarMainCommandSection.Character;
                this.assignFilter = string.Empty;
                this.assignEntries = this.stateProvider.GetAssignableEntries(this.assignCategory);
                CaptureAssignRefreshKey(this.stateProvider.Snapshot);
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawAssignWindow(HudLayoutRects layout)
    {
        if (!this.assignWindowOpen)
        {
            return;
        }

        var (slotMin, slotMax) = GetSlotRect(
            layout,
            this.config,
            this.assignTargetBarIndex,
            this.assignTargetGameSlotIndex);
        var desiredSize = new Vector2(900f, 520f);
        var viewport = ImGui.GetMainViewport();
        var margin = 12f;
        var connectionGap = 14f;

        // Prefer opening above the slot; flip below if insufficient space.
        var openAbove = (slotMin.Y - connectionGap - desiredSize.Y) >= (viewport.Pos.Y + margin);
        var preferredPos = openAbove
            ? new Vector2(slotMin.X, slotMin.Y - connectionGap - desiredSize.Y)
            : new Vector2(slotMin.X, slotMax.Y + connectionGap);
        var minX = viewport.Pos.X + margin;
        var maxX = viewport.Pos.X + viewport.Size.X - desiredSize.X - margin;
        preferredPos.X = Math.Clamp(preferredPos.X, minX, maxX);

        var open = this.assignWindowOpen;
        ImGui.SetNextWindowPos(preferredPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(desiredSize, ImGuiCond.Always);
        var assignFlags = ImGuiWindowFlags.NoResize;
        if (!ImGui.Begin(
                $"Assign Hotbar Slot##{this.assignTargetBarIndex}_{this.assignTargetGameSlotIndex}",
                ref open,
                assignFlags))
        {
            this.assignWindowOpen = open;
            ImGui.End();
            return;
        }

        this.assignWindowOpen = open;
        RefreshAssignEntriesIfPlayerJobChanged();

        // Draw a visual tether from panel edge to target slot for stronger integration.
        var panelPos = ImGui.GetWindowPos();
        var panelSize = ImGui.GetWindowSize();
        var slotAnchor = openAbove
            ? new Vector2((slotMin.X + slotMax.X) * 0.5f, slotMin.Y)
            : new Vector2((slotMin.X + slotMax.X) * 0.5f, slotMax.Y);
        var panelAnchorX = Math.Clamp(slotAnchor.X, panelPos.X + 18f, panelPos.X + panelSize.X - 18f);
        var panelAnchor = openAbove
            ? new Vector2(panelAnchorX, panelPos.Y + panelSize.Y)
            : new Vector2(panelAnchorX, panelPos.Y);
        var connectorColor = 0xE6B78F3B;
        var connectorGlow = 0x8CB78F3B;
        var connectorDraw = ImGui.GetForegroundDrawList();
        connectorDraw.AddLine(panelAnchor, slotAnchor, connectorGlow, 7f);
        connectorDraw.AddLine(panelAnchor, slotAnchor, connectorColor, 3f);
        connectorDraw.AddCircleFilled(slotAnchor, 3.5f, connectorColor);
        connectorDraw.AddCircleFilled(panelAnchor, 3.0f, connectorColor);

        if (ImGui.BeginTable("##AssignHeaderRow", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableSetupColumn("##AssignHeaderLeft", ImGuiTableColumnFlags.WidthStretch, 0.48f);
            ImGui.TableSetupColumn("##AssignHeaderRight", ImGuiTableColumnFlags.WidthStretch, 0.52f);
            ImGui.TableNextColumn();
            var absoluteSlotIndex = this.assignTargetGameSlotIndex + 1;
            var hotbarNumber = this.assignTargetBarIndex + 1;
            ImGui.TextUnformatted($"Target Slot: Hotbar {hotbarNumber} Slot {absoluteSlotIndex}");

            ImGui.TableNextColumn();
            var classJobLabel = this.stateProvider.GetCurrentClassJobLabel();
            var classJobText = $"Class/Job: {classJobLabel}";
            var availableWidth = ImGui.GetContentRegionAvail().X;
            var textWidth = ImGui.CalcTextSize(classJobText).X;
            if (textWidth < availableWidth)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availableWidth - textWidth));
            }

            ImGui.TextUnformatted(classJobText);

            ImGui.EndTable();
        }

        if (ImGui.Button("Clear Slot"))
        {
            this.stateProvider.TryClearHotbarSlot(
                this.assignTargetBarIndex,
                this.assignTargetGameSlotIndex);
            this.assignWindowOpen = false;
            ImGui.End();
            return;
        }

        ImGui.SameLine();
        if (ImGui.Button("Close"))
        {
            this.assignWindowOpen = false;
            ImGui.End();
            return;
        }

        ImGui.Separator();

        var categoryChanged = false;
        var showPerformance = this.stateProvider.IsPerformanceAvailableForCurrentJob();
        if (!showPerformance && this.assignCategory == HotbarAssignCategory.Performance)
        {
            this.assignCategory = HotbarAssignCategory.Actions;
            this.assignEntries = this.stateProvider.GetAssignableEntries(this.assignCategory);
        }
        ImGui.BeginChild("##AssignCategories", new Vector2(190f, 0f), true);
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.Actions, "Actions");
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.Role, "Role");
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.Duties, "Duties");
        if (showPerformance)
        {
            categoryChanged |= DrawCategoryButton(HotbarAssignCategory.Performance, "Performance");
        }
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.Orders, "Orders");
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.General, "General");
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.MainCommands, "Main Commands");
        categoryChanged |= DrawCategoryButton(HotbarAssignCategory.Extras, "Extras");
        ImGui.EndChild();

        if (categoryChanged)
        {
            this.assignEntries = ResolveEntriesForCurrentCategory();
            this.assignFilter = string.Empty;
            CaptureAssignRefreshKey(this.stateProvider.Snapshot);
        }

        ImGui.SameLine();

        ImGui.BeginChild("##AssignEntries", new Vector2(0f, 0f), true);
        if (this.assignCategory == HotbarAssignCategory.Orders)
        {
            var petsAvailable = this.stateProvider.IsPetOrdersAvailableForCurrentJob();
            var sectionChanged = false;
            sectionChanged |= DrawOrderSectionButton(HotbarOrderSection.Companion, "Companion");
            ImGui.SameLine();
            sectionChanged |= DrawOrderSectionButton(HotbarOrderSection.Squadron, "Squadron");
            ImGui.SameLine();
            if (petsAvailable)
            {
                sectionChanged |= DrawOrderSectionButton(HotbarOrderSection.Pets, "Pets");
            }
            else if (this.assignOrderSection == HotbarOrderSection.Pets)
            {
                this.assignOrderSection = HotbarOrderSection.Companion;
                sectionChanged = true;
            }

            if (sectionChanged)
            {
                this.assignEntries = this.stateProvider.GetAssignableOrderEntries(this.assignOrderSection);
                this.assignFilter = string.Empty;
            }

            ImGui.Separator();
        }
        else if (this.assignCategory == HotbarAssignCategory.MainCommands)
        {
            var sectionChanged = false;
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.Character, "Character");
            ImGui.SameLine();
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.Duty, "Duty");
            ImGui.SameLine();
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.Logs, "Logs");
            ImGui.SameLine();
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.Travel, "Travel");
            ImGui.SameLine();
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.Party, "Party");
            ImGui.SameLine();
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.Social, "Social");
            ImGui.SameLine();
            sectionChanged |= DrawMainCommandSectionButton(HotbarMainCommandSection.System, "System");
            if (sectionChanged)
            {
                this.assignEntries = this.stateProvider.GetAssignableMainCommandSectionEntries(this.assignMainCommandSection);
                this.assignFilter = string.Empty;
            }

            ImGui.Separator();
        }

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##AssignSearch", "Search action...", ref this.assignFilter, 128);
        ImGui.Separator();

        ImGui.BeginChild("##AssignEntriesScroll", new Vector2(0f, 0f), false);
        var filter = this.assignFilter.Trim();
        for (var i = 0; i < this.assignEntries.Count; i++)
        {
            var entry = this.assignEntries[i];
            if (!string.IsNullOrWhiteSpace(filter) &&
                entry.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var subtitle = BuildEntrySubtitle(entry);
            var rowHeight = 30f;
            var rowWidth = Math.Max(1f, ImGui.GetContentRegionAvail().X);
            var rowClicked = ImGui.InvisibleButton(
                $"##AssignRow_{entry.CommandKind}_{entry.CommandId}",
                new Vector2(rowWidth, rowHeight));
            var rowHovered = ImGui.IsItemHovered();
            var rowMin = ImGui.GetItemRectMin();
            var rowMax = ImGui.GetItemRectMax();
            var rowDraw = ImGui.GetWindowDrawList();
            if (rowHovered)
            {
                rowDraw.AddRectFilled(rowMin, rowMax, 0x606B4A24, 4f);
                rowDraw.AddRect(rowMin, rowMax, 0xA0B78F3B, 4f, ImDrawFlags.None, 1.25f);
                if (this.config.EnableStatusTooltips)
                {
                    DrawAssignEntryTooltip(entry, rowMin, rowMax, this.config.GlobalOpacity);
                }
            }

            var iconMin = new Vector2(rowMin.X + 4f, rowMin.Y + 2f);
            var iconMax = new Vector2(iconMin.X + 26f, iconMin.Y + 26f);
            if (entry.Icon is not null)
            {
                var wrap = entry.Icon.GetWrapOrEmpty();
                rowDraw.AddImage(wrap.Handle, iconMin, iconMax);
            }

            var textY = rowMin.Y + 6f;
            var namePos = new Vector2(iconMax.X + 8f, textY);
            rowDraw.AddText(namePos, 0xFFE6E8EC, entry.Name);
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var nameWidth = ImGui.CalcTextSize(entry.Name).X;
                var subtitlePos = new Vector2(namePos.X + nameWidth + 10f, textY);
                rowDraw.AddText(subtitlePos, 0xFF9AA1AB, subtitle);
            }

            if (rowClicked)
            {
                if (this.stateProvider.TryAssignCommandToHotbarSlot(
                        this.assignTargetBarIndex,
                        this.assignTargetGameSlotIndex,
                        entry.CommandKind,
                        entry.CommandId))
                {
                    this.assignWindowOpen = false;
                    ImGui.EndChild();
                    ImGui.EndChild();
                    ImGui.End();
                    return;
                }
            }
        }

        ImGui.EndChild();
        ImGui.EndChild();
        ImGui.End();
    }

    private void RefreshAssignEntriesIfPlayerJobChanged()
    {
        var snapshot = this.stateProvider.Snapshot;
        var currentName = snapshot.HasPlayer ? snapshot.PlayerName : string.Empty;
        var currentJobId = ExtractCurrentJobId();
        var changed = !string.Equals(this.assignLastPlayerName, currentName, StringComparison.Ordinal) ||
                      this.assignLastPlayerJobId != currentJobId;
        if (!changed)
        {
            return;
        }

        this.assignEntries = ResolveEntriesForCurrentCategory();
        CaptureAssignRefreshKey(snapshot);
    }

    private void CaptureAssignRefreshKey(HudStateSnapshot snapshot)
    {
        this.assignLastPlayerName = snapshot.HasPlayer ? snapshot.PlayerName : string.Empty;
        this.assignLastPlayerJobId = ExtractCurrentJobId();
    }

    private static unsafe uint ExtractCurrentJobId()
    {
        var hotbarModule = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.Instance();
        if (hotbarModule is null)
        {
            return 0u;
        }

        return hotbarModule->ActiveHotbarClassJobId;
    }

    private bool DrawCategoryButton(HotbarAssignCategory category, string label)
    {
        var selected = this.assignCategory == category;
        if (selected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF6B4A24);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF866034);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF9A6F3B);
        }

        var clicked = ImGui.Button(label, new Vector2(-1f, 28f));
        if (selected)
        {
            ImGui.PopStyleColor(3);
        }

        if (clicked)
        {
            this.assignCategory = category;
            if (category == HotbarAssignCategory.Orders && this.assignOrderSection == HotbarOrderSection.Pets && !this.stateProvider.IsPetOrdersAvailableForCurrentJob())
            {
                this.assignOrderSection = HotbarOrderSection.Companion;
            }

            return true;
        }

        return false;
    }

    private IReadOnlyList<HotbarAssignEntry> ResolveEntriesForCurrentCategory()
    {
        return this.assignCategory switch
        {
            HotbarAssignCategory.Orders => this.stateProvider.GetAssignableOrderEntries(this.assignOrderSection),
            HotbarAssignCategory.MainCommands => this.stateProvider.GetAssignableMainCommandSectionEntries(this.assignMainCommandSection),
            _ => this.stateProvider.GetAssignableEntries(this.assignCategory),
        };
    }

    private bool DrawOrderSectionButton(HotbarOrderSection section, string label)
    {
        var selected = this.assignOrderSection == section;
        if (selected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF6B4A24);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF866034);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF9A6F3B);
        }

        var clicked = ImGui.Button(label);
        if (selected)
        {
            ImGui.PopStyleColor(3);
        }

        if (clicked)
        {
            this.assignOrderSection = section;
            return true;
        }

        return false;
    }

    private bool DrawMainCommandSectionButton(HotbarMainCommandSection section, string label)
    {
        var selected = this.assignMainCommandSection == section;
        if (selected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF6B4A24);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF866034);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF9A6F3B);
        }

        var clicked = ImGui.Button(label);
        if (selected)
        {
            ImGui.PopStyleColor(3);
        }

        if (clicked)
        {
            this.assignMainCommandSection = section;
            return true;
        }

        return false;
    }

    private static string BuildEntrySubtitle(HotbarAssignEntry entry)
    {
        var level = entry.RequiredLevel > 0 ? $"Lv {entry.RequiredLevel}" : string.Empty;
        var job = string.IsNullOrWhiteSpace(entry.Affinity)
            ? string.IsNullOrWhiteSpace(entry.JobAbbrev) ? string.Empty : entry.JobAbbrev
            : entry.Affinity;

        if (!string.IsNullOrWhiteSpace(level) && !string.IsNullOrWhiteSpace(job))
        {
            return $"{level} · {job}";
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            return level;
        }

        if (!string.IsNullOrWhiteSpace(job))
        {
            return job;
        }

        return string.Empty;
    }

    private static void DrawAssignEntryTooltip(HotbarAssignEntry entry, Vector2 rectMin, Vector2 rectMax, float alpha)
    {
        var tooltipPos = new Vector2(rectMax.X + 10f, rectMin.Y + 2f);
        ImGui.SetNextWindowPos(tooltipPos);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16f, 14f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 9f);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, ApplyAlpha(0xEE1A1C22, alpha));
        ImGui.PushStyleColor(ImGuiCol.Border, ApplyAlpha(0xA0404248, alpha));

        ImGui.BeginTooltip();
        var iconSize = new Vector2(48f, 48f);
        if (entry.Icon is not null)
        {
            var wrap = entry.Icon.GetWrapOrEmpty();
            ImGui.Image(wrap.Handle, iconSize);
        }
        else
        {
            ImGui.Dummy(iconSize);
        }

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.SetWindowFontScale(1.25f);
        ImGui.TextUnformatted(entry.Name);
        ImGui.SetWindowFontScale(1.0f);
        ImGui.TextColored(0xFFC0C6D2, $"{GetTooltipKindLabel(entry.CommandKind)} [{entry.CommandId}]");
        ImGui.EndGroup();

        ImGui.Separator();
        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            ImGui.PushTextWrapPos(720f);
            ImGui.TextUnformatted(entry.Description);
            ImGui.PopTextWrapPos();
        }
        else
        {
            ImGui.TextUnformatted("No description available.");
        }

        if (!string.IsNullOrWhiteSpace(entry.JobAbbrev) || entry.RequiredLevel > 0)
        {
            ImGui.Spacing();
            var req = entry.RequiredLevel > 0 ? $"Lv. {entry.RequiredLevel}" : "--";
            var affinity = string.IsNullOrWhiteSpace(entry.Affinity)
                ? (string.IsNullOrWhiteSpace(entry.JobAbbrev) ? "--" : entry.JobAbbrev)
                : entry.Affinity;
            ImGui.TextColored(0xFFBDD77C, $"Acquired {req}    Affinity {affinity}");
        }

        ImGui.EndTooltip();
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);
    }

    private static string GetTooltipKindLabel(HotbarAssignCommandKind commandKind)
    {
        return commandKind switch
        {
            HotbarAssignCommandKind.GeneralAction => "General Action",
            HotbarAssignCommandKind.MainCommand => "Main Command",
            HotbarAssignCommandKind.ExtraCommand => "Extra Command",
            HotbarAssignCommandKind.BuddyAction => "Buddy Action",
            HotbarAssignCommandKind.PetAction => "Pet Action",
            HotbarAssignCommandKind.Unknown23 => "Command",
            HotbarAssignCommandKind.Unknown28 => "Command",
            _ => "Action",
        };
    }

    private static uint ApplyAlpha(uint colorAbgr, float alpha)
    {
        var clampedAlpha = Math.Clamp(alpha, 0f, 1f);
        var srcAlpha = (byte)((colorAbgr >> 24) & 0xFF);
        var resultAlpha = (byte)Math.Clamp((int)MathF.Round(srcAlpha * clampedAlpha), 0, 255);
        return (colorAbgr & 0x00FFFFFF) | ((uint)resultAlpha << 24);
    }

    private static (Vector2 Min, Vector2 Max) GetSlotRect(
        HudLayoutRects layout,
        HudConfiguration config,
        int barIndex,
        int gameSlotIndex)
    {
        var visibleCount = barIndex == GameHotbar.Hotbar2BarIndex
            ? config.Hotbar2VisibleSlotCount
            : config.Hotbar1VisibleSlotCount;
        var slotsPerRow = barIndex == GameHotbar.Hotbar2BarIndex
            ? config.Hotbar2SlotsPerRow
            : config.Hotbar1SlotsPerRow;
        var gridStart = barIndex == GameHotbar.Hotbar2BarIndex
            ? layout.Hotbar2Start
            : layout.Hotbar1Start;
        var slotSize = HotbarLayout.GetScaledSlotSize(config, barIndex);
        var gap = HotbarLayout.GetScaledSlotGap(config, barIndex);
        if (!HotbarGridLayout.TryGetSlotRect(
                gridStart,
                gameSlotIndex,
                visibleCount,
                slotsPerRow,
                slotSize,
                gap,
                out var min,
                out var max))
        {
            return (Vector2.Zero, Vector2.Zero);
        }

        return (min, max);
    }
}
