using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Reflection;

namespace FFXIVHudPlugin;

public sealed class HudStateProvider
{
    private readonly IObjectTable objectTable;
    private readonly IDataManager dataManager;
    private readonly ITextureProvider textureProvider;
    private readonly IPluginLog pluginLog;
    private readonly HudConfiguration configuration;
    private readonly IPartyList partyList;
    private readonly ICondition condition;
    private readonly LimitBreakTracker limitBreakTracker;
    private readonly MinimapStateProvider minimapStateProvider;

    private HudStateSnapshot snapshot = new();
    private DateTime lastStatusRefreshUtc = DateTime.MinValue;
    private bool loggedHotbarFallback;
    private readonly ExcelSheet<ActionTransient>? actionTransientSheet;
    private readonly Dictionary<ulong, float> statusTimerTracker = new();
    private float hpAnimatedRatio = 1f;
    private float lastActualHpRatio = 1f;
    private float hpDrainFromRatio = 1f;
    private float hpDrainToRatio = 1f;
    private float hpDrainElapsedSeconds;
    private float hpDrainDurationSeconds;
    private float hpDrainHoldRemainingSeconds;
    private bool hpDrainActive;
    private float hpRegenFromRatio = 1f;
    private float hpRegenToRatio = 1f;
    private float hpRegenElapsedSeconds;
    private float hpRegenDurationSeconds;
    private bool hpRegenActive;
    private DateTime lastHpSampleUtc = DateTime.UtcNow;
    private float mpAnimatedRatio = 1f;
    private float lastActualMpRatio = 1f;
    private float mpDrainFromRatio = 1f;
    private float mpDrainToRatio = 1f;
    private float mpDrainElapsedSeconds;
    private float mpDrainDurationSeconds;
    private float mpDrainHoldRemainingSeconds;
    private bool mpDrainActive;
    private float mpRegenFromRatio = 1f;
    private float mpRegenToRatio = 1f;
    private float mpRegenElapsedSeconds;
    private float mpRegenDurationSeconds;
    private bool mpRegenActive;
    private DateTime lastMpSampleUtc = DateTime.UtcNow;
    private DateTime lastOrdersDiagnosticsUtc = DateTime.MinValue;
    private DateTime runtimeOrderCacheUtc = DateTime.MinValue;
    private IReadOnlyList<HotbarAssignEntry> runtimeOrderCache = Array.Empty<HotbarAssignEntry>();
    private DateTime runtimeSquadronSheetCacheUtc = DateTime.MinValue;
    private IReadOnlyList<HotbarAssignEntry> runtimeSquadronSheetCache = Array.Empty<HotbarAssignEntry>();
    private readonly Dictionary<string, HotbarAssignEntry> capturedSquadronCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, uint> capturedSquadronIconIds = new(StringComparer.OrdinalIgnoreCase);
    private DateTime lastSquadronCaptureUtc = DateTime.MinValue;
    private DateTime lastSquadronCaptureSaveUtc = DateTime.MinValue;

    public HudStateProvider(
        IObjectTable objectTable,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        IPluginLog pluginLog,
        IPartyList partyList,
        ICondition condition,
        IClientState clientState,
        HudConfiguration configuration)
    {
        this.objectTable = objectTable;
        this.dataManager = dataManager;
        this.textureProvider = textureProvider;
        this.pluginLog = pluginLog;
        this.partyList = partyList;
        this.condition = condition;
        this.configuration = configuration;
        this.limitBreakTracker = new LimitBreakTracker();
        this.minimapStateProvider = new MinimapStateProvider(
            objectTable,
            partyList,
            dataManager,
            clientState,
            textureProvider);
        this.actionTransientSheet = this.dataManager.GetExcelSheet<ActionTransient>();
        this.LoadCapturedSquadronCommandsFromConfig();
    }

    public HudStateSnapshot Snapshot => this.snapshot;

    public unsafe bool TryExecuteHotbarSlot(int barIndex, int gameSlotIndex)
    {
        var hotbarModule = RaptureHotbarModule.Instance();
        if (hotbarModule is null)
        {
            return false;
        }

        var barId = (uint)Math.Clamp(barIndex, 0, 1);
        var absoluteIndex = (uint)Math.Clamp(gameSlotIndex, 0, HotbarSlotVisibility.MaxTotalSlots - 1);
        var slot = hotbarModule->GetSlotById(barId, absoluteIndex);
        if (slot is null)
        {
            return false;
        }

        // Prefer executing the live slot pointer (matches native hotbar button behavior).
        if (hotbarModule->ExecuteSlot(slot) != 0)
        {
            return true;
        }

        // Fallback for edge-cases where pointer path declines but id path succeeds.
        return hotbarModule->ExecuteSlotById(barId, absoluteIndex) != 0;
    }

    public unsafe bool TryAssignCommandToHotbarSlot(
        int barIndex,
        int gameSlotIndex,
        HotbarAssignCommandKind commandKind,
        uint commandId)
    {
        var hotbarModule = RaptureHotbarModule.Instance();
        if (hotbarModule is null)
        {
            return false;
        }

        if (commandId == 0)
        {
            return false;
        }

        var barId = (uint)Math.Clamp(barIndex, 0, 1);
        var absoluteIndex = (uint)Math.Clamp(gameSlotIndex, 0, HotbarSlotVisibility.MaxTotalSlots - 1);
        var targetSlot = hotbarModule->GetSlotById(barId, absoluteIndex);
        if (targetSlot is null)
        {
            return false;
        }

        var commandType = MapAssignCommandKindToHotbarSlotType(commandKind);
        if (commandType == RaptureHotbarModule.HotbarSlotType.Action)
        {
            var actionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            var candidates = new List<uint>(3);
            void AddCandidate(uint id)
            {
                if (id != 0 && !candidates.Contains(id))
                {
                    candidates.Add(id);
                }
            }
            AddCandidate(commandId);
            if (actionManager is not null)
            {
                AddCandidate(actionManager->GetAdjustedActionId(commandId));
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                if (TryApplySlotAssignment(hotbarModule, barId, targetSlot, (int)absoluteIndex, commandType, candidates[i], "picker"))
                {
                    return true;
                }
            }

            return false;
        }

        return TryApplySlotAssignment(hotbarModule, barId, targetSlot, (int)absoluteIndex, commandType, commandId, "picker");
    }

    private unsafe bool TryApplySlotAssignment(
        RaptureHotbarModule* hotbarModule,
        uint barId,
        RaptureHotbarModule.HotbarSlot* targetSlot,
        int absoluteIndex,
        RaptureHotbarModule.HotbarSlotType commandType,
        uint commandId,
        string source)
    {
        var beforeType = targetSlot->CommandType;
        var beforeId = targetSlot->CommandId;

        // First attempt: native helper that should handle persistence and normalization.
        hotbarModule->SetAndSaveSlot(barId, (uint)absoluteIndex, commandType, commandId, ignoreSharedHotbars: false, allowSaveToPvP: true);
        if (IsSlotAssignmentApplied(targetSlot, beforeType, beforeId, commandType, commandId))
        {
            return true;
        }

        // Fallback path: set live slot directly and force-save to active class-job hotbar.
        targetSlot->Set(commandType, commandId);
        targetSlot->LoadIconId();
        var classJobId = (uint)hotbarModule->ActiveHotbarClassJobId;
        var isPvpSlot = hotbarModule->PvPHotbarsActive;
        hotbarModule->WriteSavedSlot(classJobId, barId, (uint)absoluteIndex, targetSlot, ignoreSharedHotbars: false, isPvpSlot: isPvpSlot);

        var applied = IsSlotAssignmentApplied(targetSlot, beforeType, beforeId, commandType, commandId);
        if (!applied)
        {
            this.pluginLog.Debug(
                $"Fallback write failed ({source}) slot={absoluteIndex} type={commandType} id={commandId} " +
                $"before=({beforeType},{beforeId}) after=({targetSlot->CommandType},{targetSlot->CommandId}) " +
                $"activeJob={classJobId} pvp={isPvpSlot}");
        }
        else
        {
            this.pluginLog.Debug(
                $"Fallback write applied ({source}) slot={absoluteIndex} type={commandType} id={commandId} " +
                $"after=({targetSlot->CommandType},{targetSlot->CommandId})");
        }

        return applied;
    }

    private static unsafe bool IsSlotAssignmentApplied(
        RaptureHotbarModule.HotbarSlot* slot,
        RaptureHotbarModule.HotbarSlotType beforeType,
        uint beforeId,
        RaptureHotbarModule.HotbarSlotType expectedType,
        uint expectedId)
    {
        var afterType = slot->CommandType;
        var afterId = slot->CommandId;
        return afterType == expectedType && afterId == expectedId;
    }

    public unsafe bool TryClearHotbarSlot(int barIndex, int gameSlotIndex)
    {
        var hotbarModule = RaptureHotbarModule.Instance();
        if (hotbarModule is null)
        {
            return false;
        }

        var barId = (uint)Math.Clamp(barIndex, 0, 1);
        var absoluteIndex = (uint)Math.Clamp(gameSlotIndex, 0, HotbarSlotVisibility.MaxTotalSlots - 1);
        var targetSlot = hotbarModule->GetSlotById(barId, absoluteIndex);
        if (targetSlot is null)
        {
            return false;
        }

        return TryApplySlotAssignment(
            hotbarModule,
            barId,
            targetSlot,
            (int)absoluteIndex,
            RaptureHotbarModule.HotbarSlotType.Empty,
            0,
            "picker-clear");
    }

    public void Update()
    {
        try
        {
            TryCaptureSquadronCommandsFromHotbars();
            var player = this.objectTable.LocalPlayer;
            if (player is null)
            {
                var (leftHotbar, rightHotbar) = this.ResolveHotbarPair(
                    GameHotbar.Hotbar1BarIndex,
                    this.configuration.LeftHotbarActions,
                    this.configuration.RightHotbarActions,
                    this.configuration.Hotbar1VisibleSlotCount);
                var (leftHotbar2, rightHotbar2) = this.ResolveHotbarPair(
                    GameHotbar.Hotbar2BarIndex,
                    this.configuration.LeftHotbar2Actions,
                    this.configuration.RightHotbar2Actions,
                    this.configuration.Hotbar2VisibleSlotCount);

                var (testBuffs, testDebuffs) = this.ResolveStatusEffects(Array.Empty<StatusViewModel>(), Array.Empty<StatusViewModel>());
                this.snapshot = new HudStateSnapshot
                {
                    HasPlayer = false,
                    Buffs = testBuffs,
                    Debuffs = testDebuffs,
                    LeftHotbar = leftHotbar,
                    RightHotbar = rightHotbar,
                    LeftHotbar2 = leftHotbar2,
                    RightHotbar2 = rightHotbar2,
                    LimitBreak = this.limitBreakTracker.GetState(this.partyList.Length, this.condition[ConditionFlag.BoundByDuty]),
                    Minimap = MinimapSnapshot.Empty,
                };
                return;
            }

            var hp = player.CurrentHp;
            var hpMax = player.MaxHp;
            var shieldPercent = Math.Clamp((int)player.ShieldPercentage, 0, 100);
            var shieldRatio = shieldPercent / 100f;
            var shieldAmount = hpMax == 0 ? 0u : (uint)MathF.Round(hpMax * shieldRatio);
            var mp = player.CurrentMp;
            var mpMax = player.MaxMp == 0 ? 10000u : player.MaxMp;
            var battleChara = player as IBattleChara;
            var isCasting = battleChara?.IsCasting ?? false;
            var castTotal = isCasting && battleChara is not null ? Math.Max(0f, battleChara.TotalCastTime) : 0f;
            var castProgress = isCasting && battleChara is not null && battleChara.TotalCastTime > 0.01f
                ? Math.Clamp(battleChara.CurrentCastTime / battleChara.TotalCastTime, 0f, 1f)
                : 0f;
            var currentHpRatio = hpMax == 0 ? 0f : hp / (float)hpMax;
            var hpNowUtc = DateTime.UtcNow;
            var hpDeltaSeconds = Math.Clamp(Math.Max(0f, (float)(hpNowUtc - this.lastHpSampleUtc).TotalSeconds), 0f, 0.05f);
            this.lastHpSampleUtc = hpNowUtc;

            if (currentHpRatio + 0.001f < this.lastActualHpRatio)
            {
                this.hpDrainFromRatio = Math.Max(this.hpAnimatedRatio, this.lastActualHpRatio);
                this.hpDrainToRatio = currentHpRatio;
                var delta = Math.Max(0.001f, this.hpDrainFromRatio - this.hpDrainToRatio);
                this.hpDrainDurationSeconds = Math.Clamp(delta / 0.30f, 0.35f, 1.10f);
                this.hpDrainHoldRemainingSeconds = Math.Clamp(0.08f + (delta * 0.28f), 0.08f, 0.24f);
                this.hpDrainElapsedSeconds = 0f;
                this.hpDrainActive = true;
                this.hpRegenActive = false;
            }
            else if (currentHpRatio > this.lastActualHpRatio + 0.001f)
            {
                this.hpRegenFromRatio = Math.Min(this.hpAnimatedRatio, this.lastActualHpRatio);
                this.hpRegenToRatio = currentHpRatio;
                var delta = Math.Max(0.001f, this.hpRegenToRatio - this.hpRegenFromRatio);
                this.hpRegenDurationSeconds = Math.Clamp(delta / 0.55f, 0.20f, 0.85f);
                this.hpRegenElapsedSeconds = 0f;
                this.hpRegenActive = true;
            }

            if (this.hpDrainActive)
            {
                if (this.hpDrainHoldRemainingSeconds > 0f)
                {
                    this.hpDrainHoldRemainingSeconds = Math.Max(0f, this.hpDrainHoldRemainingSeconds - hpDeltaSeconds);
                    this.hpAnimatedRatio = this.hpDrainFromRatio;
                }
                else
                {
                    this.hpDrainElapsedSeconds += hpDeltaSeconds;
                    var t = this.hpDrainDurationSeconds <= 0.001f
                        ? 1f
                        : Math.Clamp(this.hpDrainElapsedSeconds / this.hpDrainDurationSeconds, 0f, 1f);
                    var eased = 1f - MathF.Pow(1f - t, 3f);
                    this.hpAnimatedRatio = this.hpDrainFromRatio + ((this.hpDrainToRatio - this.hpDrainFromRatio) * eased);
                    if (t >= 1f)
                    {
                        this.hpDrainActive = false;
                    }
                }
            }
            else if (this.hpRegenActive)
            {
                this.hpRegenElapsedSeconds += hpDeltaSeconds;
                var t = this.hpRegenDurationSeconds <= 0.001f
                    ? 1f
                    : Math.Clamp(this.hpRegenElapsedSeconds / this.hpRegenDurationSeconds, 0f, 1f);
                var eased = 1f - MathF.Pow(1f - t, 2.2f);
                this.hpAnimatedRatio = this.hpRegenFromRatio + ((this.hpRegenToRatio - this.hpRegenFromRatio) * eased);
                if (t >= 1f)
                {
                    this.hpRegenActive = false;
                }
            }
            else
            {
                this.hpAnimatedRatio = currentHpRatio;
            }

            this.lastActualHpRatio = currentHpRatio;
            var currentMpRatio = mpMax == 0 ? 0f : mp / (float)mpMax;
            var nowUtc = DateTime.UtcNow;
            var deltaSeconds = Math.Clamp(Math.Max(0f, (float)(nowUtc - this.lastMpSampleUtc).TotalSeconds), 0f, 0.05f);
            this.lastMpSampleUtc = nowUtc;

            // Detect new MP spend and start a deterministic drain animation.
            if (currentMpRatio + 0.001f < this.lastActualMpRatio)
            {
                this.mpDrainFromRatio = Math.Max(this.mpAnimatedRatio, this.lastActualMpRatio);
                this.mpDrainToRatio = currentMpRatio;
                var delta = Math.Max(0.001f, this.mpDrainFromRatio - this.mpDrainToRatio);
                // Keep the spent segment visible long enough to read, similar to native bars.
                this.mpDrainDurationSeconds = Math.Clamp(delta / 0.36f, 0.32f, 0.95f);
                this.mpDrainHoldRemainingSeconds = Math.Clamp(0.05f + (delta * 0.20f), 0.05f, 0.18f);
                this.mpDrainElapsedSeconds = 0f;
                this.mpDrainActive = true;
                this.mpRegenActive = false;
            }
            else if (currentMpRatio > this.lastActualMpRatio + 0.001f)
            {
                this.mpRegenFromRatio = Math.Min(this.mpAnimatedRatio, this.lastActualMpRatio);
                this.mpRegenToRatio = currentMpRatio;
                var delta = Math.Max(0.001f, this.mpRegenToRatio - this.mpRegenFromRatio);
                this.mpRegenDurationSeconds = Math.Clamp(delta / 0.70f, 0.18f, 0.65f);
                this.mpRegenElapsedSeconds = 0f;
                this.mpRegenActive = true;
            }

            if (this.mpDrainActive)
            {
                if (this.mpDrainHoldRemainingSeconds > 0f)
                {
                    this.mpDrainHoldRemainingSeconds = Math.Max(0f, this.mpDrainHoldRemainingSeconds - deltaSeconds);
                    this.mpAnimatedRatio = this.mpDrainFromRatio;
                }
                else
                {
                    this.mpDrainElapsedSeconds += deltaSeconds;
                    var t = this.mpDrainDurationSeconds <= 0.001f
                        ? 1f
                        : Math.Clamp(this.mpDrainElapsedSeconds / this.mpDrainDurationSeconds, 0f, 1f);
                    // Ease-out keeps the start readable while still finishing quickly.
                    var eased = 1f - MathF.Pow(1f - t, 3f);
                    this.mpAnimatedRatio = this.mpDrainFromRatio + ((this.mpDrainToRatio - this.mpDrainFromRatio) * eased);
                    if (t >= 1f)
                    {
                        this.mpDrainActive = false;
                    }
                }
            }
            else if (this.mpRegenActive)
            {
                this.mpRegenElapsedSeconds += deltaSeconds;
                var t = this.mpRegenDurationSeconds <= 0.001f
                    ? 1f
                    : Math.Clamp(this.mpRegenElapsedSeconds / this.mpRegenDurationSeconds, 0f, 1f);
                var eased = 1f - MathF.Pow(1f - t, 2.2f);
                this.mpAnimatedRatio = this.mpRegenFromRatio + ((this.mpRegenToRatio - this.mpRegenFromRatio) * eased);
                if (t >= 1f)
                {
                    this.mpRegenActive = false;
                }
            }
            else
            {
                // When not spending MP, keep display synced (including MP regen).
                this.mpAnimatedRatio = currentMpRatio;
            }

            this.lastActualMpRatio = currentMpRatio;

            var refreshStatuses = (DateTime.UtcNow - this.lastStatusRefreshUtc).TotalMilliseconds >= 160;
            var liveBuffs = refreshStatuses ? this.BuildStatuses(player.StatusList, false) : this.snapshot.Buffs;
            var liveDebuffs = refreshStatuses ? this.BuildStatuses(player.StatusList, true) : this.snapshot.Debuffs;
            if (refreshStatuses)
            {
                this.lastStatusRefreshUtc = DateTime.UtcNow;
            }

            var (buffs, debuffs) = this.ResolveStatusEffects(liveBuffs, liveDebuffs);

            var (left, right) = this.ResolveHotbarPair(
                GameHotbar.Hotbar1BarIndex,
                this.configuration.LeftHotbarActions,
                this.configuration.RightHotbarActions,
                this.configuration.Hotbar1VisibleSlotCount);
            var (left2, right2) = this.ResolveHotbarPair(
                GameHotbar.Hotbar2BarIndex,
                this.configuration.LeftHotbar2Actions,
                this.configuration.RightHotbar2Actions,
                this.configuration.Hotbar2VisibleSlotCount);

            this.snapshot = new HudStateSnapshot
            {
                HasPlayer = true,
                PlayerName = player.Name.TextValue,
                CurrentHp = hp,
                MaxHp = hpMax,
                HpAnimatedRatio = this.hpAnimatedRatio,
                ShieldRatio = shieldRatio,
                ShieldAmount = shieldAmount,
                CurrentMp = mp,
                MaxMp = mpMax,
                MpAnimatedRatio = this.mpAnimatedRatio,
                IsCasting = isCasting,
                CastProgressRatio = castProgress,
                CastTotalSeconds = castTotal,
                Buffs = buffs,
                Debuffs = debuffs,
                LeftHotbar = left,
                RightHotbar = right,
                LeftHotbar2 = left2,
                RightHotbar2 = right2,
                LimitBreak = this.limitBreakTracker.GetState(this.partyList.Length, this.condition[ConditionFlag.BoundByDuty]),
                Minimap = this.configuration.MinimapEnabled
                    ? this.minimapStateProvider.Build(
                        this.configuration.MinimapVisibleRangeYalms,
                        this.configuration.MinimapShowNativeMarkers,
                        this.configuration.MinimapSize)
                    : MinimapSnapshot.Empty,
            };
        }
        catch (Exception ex)
        {
            this.pluginLog.Warning(ex, "Failed to update HUD state.");
        }
    }

    private (IReadOnlyList<StatusViewModel> Buffs, IReadOnlyList<StatusViewModel> Debuffs) ResolveStatusEffects(
        IReadOnlyList<StatusViewModel> liveBuffs,
        IReadOnlyList<StatusViewModel> liveDebuffs)
    {
        if (!this.configuration.ShowTestStatusEffects)
        {
            return (liveBuffs, liveDebuffs);
        }

        return (this.BuildTestStatusEffects(false), this.BuildTestStatusEffects(true));
    }

    private IReadOnlyList<StatusViewModel> BuildTestStatusEffects(bool debuffs)
    {
        var count = debuffs
            ? StatusLaneLayout.ClampMaxIconsPerRow(this.configuration.DebuffMaxIconsPerRow)
            : StatusLaneLayout.ClampMaxIconsPerRow(this.configuration.BuffMaxIconsPerRow);
        var list = new List<StatusViewModel>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new StatusViewModel
            {
                StatusId = (uint)((debuffs ? 90_000 : 80_000) + i),
                Name = debuffs ? $"Test Debuff {i + 1}" : $"Test Buff {i + 1}",
                Description = "Layout preview placeholder.",
                RemainingTime = Math.Max(6f, 42f - (i * 3.2f)),
                ShowTimer = true,
                IsDebuff = debuffs,
                Icon = null,
            });
        }

        return list;
    }

    private IReadOnlyList<StatusViewModel> BuildStatuses(Dalamud.Game.ClientState.Statuses.StatusList statuses, bool debuffs)
    {
        var sheet = this.dataManager.GetExcelSheet<Status>();
        if (sheet is null)
        {
            return Array.Empty<StatusViewModel>();
        }

        var list = new List<StatusViewModel>(30);
        for (var i = 0; i < statuses.Length; i++)
        {
            var entry = statuses[i];
            if (entry is null || entry.StatusId == 0)
            {
                continue;
            }

            if (!sheet.TryGetRow(entry.StatusId, out var row))
            {
                continue;
            }

            var isDetrimental = row.StatusCategory is 2 or 7;
            if (isDetrimental != debuffs)
            {
                continue;
            }

            list.Add(new StatusViewModel
            {
                StatusId = entry.StatusId,
                Name = row.Name.ToString(),
                Description = row.Description.ToString(),
                RemainingTime = entry.RemainingTime,
                ShowTimer = this.ShouldShowStatusTimer(entry.StatusId, entry.Param, entry.RemainingTime, debuffs),
                IsDebuff = debuffs,
                Icon = this.GetStatusIcon(row.Icon),
            });
        }

        var maxVisible = debuffs
            ? StatusLaneLayout.GetMaxVisibleStatusCount(this.configuration.DebuffMaxIconsPerRow)
            : StatusLaneLayout.GetMaxVisibleStatusCount(this.configuration.BuffMaxIconsPerRow);
        return list
            .OrderBy(x => x.RemainingTime <= 0.1f ? float.MaxValue : x.RemainingTime)
            .Take(maxVisible)
            .ToArray();
    }

    private bool ShouldShowStatusTimer(uint statusId, ushort param, float remainingTime, bool isDebuff)
    {
        if (remainingTime <= 0.05f)
        {
            return false;
        }

        // Most timed effects are below this and should always show.
        if (remainingTime < 58f)
        {
            return true;
        }

        // For long/static durations (common around 60s), only show if timer is actually ticking down.
        var key = ((ulong)statusId << 32) | ((ulong)param << 16) | (isDebuff ? 1UL : 0UL);
        var hasPrevious = this.statusTimerTracker.TryGetValue(key, out var previousRemaining);
        this.statusTimerTracker[key] = remainingTime;
        if (!hasPrevious)
        {
            return false;
        }

        return previousRemaining - remainingTime > 0.015f;
    }

    private IReadOnlyList<HotbarSlotViewModel> BuildConfiguredHotbar(IReadOnlyList<uint> actionIds, bool left)
    {
        var sheet = this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        if (sheet is null)
        {
            return Array.Empty<HotbarSlotViewModel>();
        }

        var list = new List<HotbarSlotViewModel>(6);
        var keys = left
            ? new[] { "1", "2", "3", "4", "5", "6" }
            : new[] { "Q", "E", "R", "C", "Z", "X" };

        for (var i = 0; i < actionIds.Count && i < 6; i++)
        {
            var actionId = actionIds[i];
            if (!sheet.TryGetRow(actionId, out var row))
            {
                list.Add(new HotbarSlotViewModel
                {
                    GameSlotIndex = left ? i : i + HotbarSlotVisibility.MaxSlotsPerSide,
                    ActionId = actionId,
                    TooltipId = actionId,
                    Label = $"Action {actionId}",
                    Description = string.Empty,
                    Keybind = keys[i],
                    CooldownRatio = 0f,
                    CooldownSecondsRemaining = 0f,
                    CastTimeSeconds = 0f,
                    RecastTimeSeconds = 0f,
                    RangeYalms = 0,
                    RadiusYalms = 0,
                    RequiredLevel = 0,
                    JobAbbrev = string.Empty,
                    ChargesCurrent = 0,
                    ChargesMax = 0,
                    IsUsable = false,
                });
                continue;
            }

            // Cooldown/charges are represented with synthetic values for v1 until dynamic hotbar reads are enabled.
            var synthetic = (Environment.TickCount64 / 1000.0 + i * 0.6) % 8.0;
            var ratio = (float)(synthetic / 8.0);
            list.Add(new HotbarSlotViewModel
            {
                GameSlotIndex = left ? i : i + HotbarSlotVisibility.MaxSlotsPerSide,
                ActionId = actionId,
                TooltipId = actionId,
                Label = row.Name.ToString(),
                Description = this.GetActionDescription(actionId),
                Keybind = keys[i],
                CooldownRatio = ratio < 0.04f ? 0f : ratio,
                CooldownSecondsRemaining = ratio < 0.04f ? 0f : (1f - ratio) * 8f,
                CastTimeSeconds = row.Cast100ms / 10f,
                RecastTimeSeconds = row.Recast100ms / 10f,
                RangeYalms = row.Range,
                RadiusYalms = row.EffectRange,
                RequiredLevel = row.ClassJobLevel,
                JobAbbrev = this.GetActionJobAbbreviation(row),
                ChargesCurrent = row.MaxCharges > 0 ? Math.Max(1, (int)row.MaxCharges - (ratio > 0.6f ? 1 : 0)) : 0,
                ChargesMax = (int)row.MaxCharges,
                IsUsable = true,
                IsProc = ratio < 0.04f && (i % 3 == 0),
                Icon = this.GetActionIcon(row.Icon),
            });
        }

        return list;
    }

    private (IReadOnlyList<HotbarSlotViewModel> Left, IReadOnlyList<HotbarSlotViewModel> Right) ResolveHotbarPair(
        int barIndex,
        IReadOnlyList<uint> leftFallbackActionIds,
        IReadOnlyList<uint> rightFallbackActionIds,
        int visibleSlotCount)
    {
        var (left, right) = this.BuildLiveHotbarPair(barIndex);
        if (left.Count == 0 || right.Count == 0)
        {
            left = this.BuildConfiguredHotbar(leftFallbackActionIds, true);
            right = this.BuildConfiguredHotbar(rightFallbackActionIds, false);
        }

        visibleSlotCount = HotbarSlotVisibility.ClampTotal(visibleSlotCount);
        return (
            HotbarSlotVisibility.SliceLeft(left, visibleSlotCount),
            HotbarSlotVisibility.SliceRight(right, visibleSlotCount));
    }

    private (IReadOnlyList<HotbarSlotViewModel> Left, IReadOnlyList<HotbarSlotViewModel> Right) BuildLiveHotbarPair(int barIndex)
    {
        var sheet = this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        if (sheet is null)
        {
            return (Array.Empty<HotbarSlotViewModel>(), Array.Empty<HotbarSlotViewModel>());
        }

        unsafe
        {
            var hotbarModule = RaptureHotbarModule.Instance();
            if (hotbarModule is null)
            {
                return (Array.Empty<HotbarSlotViewModel>(), Array.Empty<HotbarSlotViewModel>());
            }

            var barId = (uint)Math.Clamp(barIndex, 0, 1);
            var left = new List<HotbarSlotViewModel>(6);
            var right = new List<HotbarSlotViewModel>(6);
            var leftKeys = new[] { "1", "2", "3", "4", "5", "6" };
            var rightKeys = new[] { "Q", "E", "R", "C", "Z", "X" };

            for (var slotIndex = 0; slotIndex < 12; slotIndex++)
            {
                var slot = hotbarModule->GetSlotById(barId, (uint)slotIndex);
                if (slot is null)
                {
                    continue;
                }

                var appearanceType = RaptureHotbarModule.HotbarSlotType.Empty;
                var appearanceId = 0u;
                ushort unk = 0;
                RaptureHotbarModule.GetSlotAppearance(&appearanceType, &appearanceId, &unk, hotbarModule, slot);

                var fallbackKeybind = slotIndex < 6 ? leftKeys[slotIndex] : rightKeys[slotIndex - 6];
                var keybind = SanitizeKeybind(slot->KeybindHintString, fallbackKeybind);
                var model = this.BuildLiveHotbarSlotModel(
                    sheet,
                    slot,
                    slotIndex,
                    appearanceType,
                    appearanceId,
                    slot->CommandType,
                    slot->CommandId,
                    keybind);
                if (slotIndex < 6)
                {
                    left.Add(model);
                }
                else
                {
                    right.Add(model);
                }
            }

            while (left.Count < 6)
            {
                left.Add(this.BuildEmptyLiveSlot(leftKeys[left.Count], left.Count));
            }

            while (right.Count < 6)
            {
                right.Add(this.BuildEmptyLiveSlot(rightKeys[right.Count], right.Count + HotbarSlotVisibility.MaxSlotsPerSide));
            }

            return (left, right);
        }
    }

    private unsafe HotbarSlotViewModel BuildLiveHotbarSlotModel(
        ExcelSheet<Lumina.Excel.Sheets.Action> actionSheet,
        RaptureHotbarModule.HotbarSlot* liveSlot,
        int gameSlotIndex,
        RaptureHotbarModule.HotbarSlotType slotType,
        uint actionId,
        RaptureHotbarModule.HotbarSlotType commandType,
        uint commandId,
        string keybind)
    {
        // Use the live slot icon so special commands (e.g. Limit Break) match the real hotbar exactly.
        liveSlot->LoadIconId();
        var liveIcon = this.GetActionIcon(liveSlot->IconId);

        if (slotType == RaptureHotbarModule.HotbarSlotType.Empty || actionId == 0)
        {
            return this.BuildEmptyLiveSlot(keybind, gameSlotIndex);
        }

        if (!IsActionLikeSlotType(slotType))
        {
            if (!this.loggedHotbarFallback)
            {
                this.pluginLog.Information("Hotbar contains non-action slot types. Unsupported entries are rendered as placeholders.");
                this.loggedHotbarFallback = true;
            }

            return new HotbarSlotViewModel
            {
                GameSlotIndex = gameSlotIndex,
                ActionId = actionId,
                TooltipId = actionId,
                TooltipKindLabel = slotType.ToString(),
                Label = slotType.ToString(),
                Description = string.Empty,
                Keybind = keybind,
                CooldownRatio = 0f,
                CooldownSecondsRemaining = 0f,
                CastTimeSeconds = 0f,
                RecastTimeSeconds = 0f,
                RangeYalms = 0,
                RadiusYalms = 0,
                RequiredLevel = 0,
                JobAbbrev = string.Empty,
                ChargesCurrent = 0,
                ChargesMax = 0,
                IsUsable = true,
                Icon = liveIcon,
            };
        }

        if (!actionSheet.TryGetRow(actionId, out var row))
        {
            return new HotbarSlotViewModel
            {
                GameSlotIndex = gameSlotIndex,
                ActionId = actionId,
                TooltipId = actionId,
                Label = $"Action {actionId}",
                Description = string.Empty,
                Keybind = keybind,
                CooldownRatio = 0f,
                CooldownSecondsRemaining = 0f,
                CastTimeSeconds = 0f,
                RecastTimeSeconds = 0f,
                RangeYalms = 0,
                RadiusYalms = 0,
                RequiredLevel = 0,
                JobAbbrev = string.Empty,
                ChargesCurrent = 0,
                ChargesMax = 0,
                IsUsable = true,
                Icon = liveIcon,
            };
        }

        var cooldownSecondsRemaining = 0;
        var cooldownPercent = liveSlot->GetSlotActionCooldownPercentage(&cooldownSecondsRemaining, 0);
        // The native percentage behaves like progress on many actions, while the visual effect
        // needs remaining cooldown (1 -> just used, 0 -> ready).
        var cooldownProgress = Math.Clamp(cooldownPercent / 100f, 0f, 1f);
        var cooldownRatio = cooldownProgress <= 0.001f && cooldownSecondsRemaining <= 0
            ? 0f
            : 1f - cooldownProgress;
        cooldownRatio = Math.Clamp(cooldownRatio, 0f, 1f);
        var chargesCurrent = (int)liveSlot->GetApparentIconRecastCharges();
        var chargesMax = (int)row.MaxCharges;
        if (chargesMax > 0)
        {
            chargesCurrent = Math.Clamp(chargesCurrent, 0, chargesMax);
        }

        var tooltipType = commandType;
        var tooltipId = commandId != 0 ? commandId : actionId;
        var displayNamePtr = liveSlot->GetDisplayNameForSlot(slotType, actionId);
        var displayName = displayNamePtr.ToString();
        var popupHelp = liveSlot->PopUpHelp.ToString();
        var actionDescription = this.GetDescriptionForCommandType(tooltipType, tooltipId, actionId);
        var normalizedPopupHelp = SanitizeSheetText(popupHelp);
        var tooltipKindLabel = IsActionLikeSlotType(tooltipType) ||
                               tooltipType is RaptureHotbarModule.HotbarSlotType.GeneralAction
                                           or RaptureHotbarModule.HotbarSlotType.MainCommand
                                           or RaptureHotbarModule.HotbarSlotType.ExtraCommand
            ? "Ability"
            : tooltipType.ToString();
        var description = IsPlaceholderPopupHelp(normalizedPopupHelp, displayName)
            ? actionDescription
            : normalizedPopupHelp;

        return new HotbarSlotViewModel
        {
            GameSlotIndex = gameSlotIndex,
            ActionId = actionId,
            TooltipId = tooltipId,
            TooltipKindLabel = tooltipKindLabel,
            Label = string.IsNullOrWhiteSpace(displayName) ? row.Name.ToString() : displayName,
            Description = description,
            Keybind = keybind,
            CooldownRatio = cooldownRatio,
            CooldownSecondsRemaining = Math.Max(0f, cooldownSecondsRemaining),
            CastTimeSeconds = row.Cast100ms / 10f,
            RecastTimeSeconds = row.Recast100ms / 10f,
            RangeYalms = row.Range,
            RadiusYalms = row.EffectRange,
            RequiredLevel = row.ClassJobLevel,
            JobAbbrev = this.GetActionJobAbbreviation(row),
            ChargesCurrent = chargesCurrent,
            ChargesMax = chargesMax,
            IsUsable = liveSlot->IsSlotUsable(slotType, actionId),
            IsProc = liveSlot->IsActionHighlighted(slotType, actionId),
            Icon = liveIcon ?? this.GetActionIcon(row.Icon),
        };
    }

    private HotbarSlotViewModel BuildEmptyLiveSlot(string keybind, int gameSlotIndex)
    {
        return new HotbarSlotViewModel
        {
            GameSlotIndex = gameSlotIndex,
            ActionId = 0,
            TooltipId = 0,
            Label = string.Empty,
            Description = string.Empty,
            Keybind = keybind,
            CooldownRatio = 0f,
            CooldownSecondsRemaining = 0f,
            CastTimeSeconds = 0f,
            RecastTimeSeconds = 0f,
            RangeYalms = 0,
            RadiusYalms = 0,
            RequiredLevel = 0,
            JobAbbrev = string.Empty,
            ChargesCurrent = 0,
            ChargesMax = 0,
            IsUsable = false,
        };
    }

    private static bool IsActionLikeSlotType(RaptureHotbarModule.HotbarSlotType slotType)
    {
        return slotType is
            RaptureHotbarModule.HotbarSlotType.Action or
            RaptureHotbarModule.HotbarSlotType.CraftAction or
            RaptureHotbarModule.HotbarSlotType.PetAction or
            RaptureHotbarModule.HotbarSlotType.BuddyAction or
            RaptureHotbarModule.HotbarSlotType.GeneralAction or
            RaptureHotbarModule.HotbarSlotType.MainCommand or
            RaptureHotbarModule.HotbarSlotType.ExtraCommand or
            RaptureHotbarModule.HotbarSlotType.PvPCombo;
    }

    public IReadOnlyList<HotbarAssignEntry> GetAssignableEntries(HotbarAssignCategory category)
    {
        return category switch
        {
            HotbarAssignCategory.Actions => this.GetAssignableJobActionEntries(),
            HotbarAssignCategory.Role => this.GetAssignableRoleActionEntries(),
            HotbarAssignCategory.Duties => this.GetAssignableDutyEntries(),
            HotbarAssignCategory.Performance => this.IsPerformanceAvailableForCurrentJob()
                ? this.GetAssignablePerformanceEntries()
                : Array.Empty<HotbarAssignEntry>(),
            HotbarAssignCategory.Orders => this.GetAssignableOrderEntries(HotbarOrderSection.Companion),
            HotbarAssignCategory.General => this.GetAssignableGeneralEntries(),
            HotbarAssignCategory.MainCommands => this.GetAssignableMainCommandSectionEntries(HotbarMainCommandSection.Character),
            HotbarAssignCategory.Extras => this.GetAssignableExtrasEntries(),
            _ => Array.Empty<HotbarAssignEntry>(),
        };
    }

    public bool IsPerformanceAvailableForCurrentJob()
    {
        var player = this.objectTable.LocalPlayer;
        var playerJobId = player?.ClassJob.RowId ?? 0u;
        if (playerJobId == 0)
        {
            return false;
        }

        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (classJobSheet is null || !classJobSheet.TryGetRow(playerJobId, out var classJob))
        {
            return false;
        }

        var abbrev = SanitizeSheetText(classJob.Abbreviation.ToString()).ToUpperInvariant();
        return abbrev == "BRD";
    }

    public string GetCurrentClassJobLabel()
    {
        static string ToTitleCaseWords(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                var token = words[i];
                words[i] = token.Length == 1
                    ? token.ToUpperInvariant()
                    : char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant();
            }

            return string.Join(" ", words);
        }

        var player = this.objectTable.LocalPlayer;
        var playerJobId = player?.ClassJob.RowId ?? 0u;
        if (playerJobId == 0)
        {
            return "N/A";
        }

        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (classJobSheet is null || !classJobSheet.TryGetRow(playerJobId, out var classJob))
        {
            return $"Job {playerJobId}";
        }

        var name = ToTitleCaseWords(SanitizeSheetText(classJob.Name.ToString()));
        var abbrev = SanitizeSheetText(classJob.Abbreviation.ToString()).ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(abbrev))
        {
            return $"{name} ({abbrev})";
        }

        return !string.IsNullOrWhiteSpace(name) ? name : !string.IsNullOrWhiteSpace(abbrev) ? abbrev : $"Job {playerJobId}";
    }

    public bool IsPetOrdersAvailableForCurrentJob()
    {
        var player = this.objectTable.LocalPlayer;
        var playerJobId = player?.ClassJob.RowId ?? 0u;
        if (playerJobId == 0)
        {
            return false;
        }

        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (classJobSheet is null || !classJobSheet.TryGetRow(playerJobId, out var classJob))
        {
            return false;
        }

        var abbrev = SanitizeSheetText(classJob.Abbreviation.ToString()).ToUpperInvariant();
        return abbrev is "ACN" or "SMN" or "SCH";
    }

    public IReadOnlyList<HotbarAssignEntry> GetAssignableOrderEntries(HotbarOrderSection section)
    {
        static List<string> BuildTargetList(params string[] names)
        {
            var list = new List<string>(names.Length);
            for (var i = 0; i < names.Length; i++)
            {
                list.Add(names[i]);
            }

            return list;
        }

        static IReadOnlyList<string> BuildAliasesForTarget(string target)
        {
            var normalized = NormalizeNameToken(target);
            return normalized switch
            {
                "DISPLAYORDERHOTBAR" => new[] { "DISPLAYORDERHOTBAR", "ORDERHOTBAR" },
                "EXECUTELIMITBREAK" => new[] { "EXECUTELIMITBREAK", "LIMITBREAK" },
                "REENGAGE" => new[] { "REENGAGE", "RE-ENGAGE" },
                "DEFENDERSTANCE" => new[] { "DEFENDERSTANCE", "DEFENSIVESTANCE" },
                "HEALERSTANCE" => new[] { "HEALERSTANCE", "HEALSTANCE" },
                "ATTACKERSTANCE" => new[] { "ATTACKERSTANCE", "ATTACKSTANCE" },
                _ => new[] { normalized },
            };
        }

        static IReadOnlyList<string> BuildTokensForTarget(string target)
        {
            return target
                .Replace("-", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeNameToken)
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToArray();
        }

        static int CommandKindPreference(HotbarAssignCommandKind kind)
        {
            return kind switch
            {
                HotbarAssignCommandKind.BuddyAction => 0,
                HotbarAssignCommandKind.GeneralAction => 1,
                HotbarAssignCommandKind.MainCommand => 2,
                HotbarAssignCommandKind.ExtraCommand => 3,
                _ => 4,
            };
        }

        var companionTargets = BuildTargetList(
            "Withdraw",
            "Follow",
            "Free Stance",
            "Defender Stance",
            "Healer Stance",
            "Attacker Stance");

        var squadronTargets = BuildTargetList(
            "Display Order Hotbar",
            "Engage",
            "Disengage",
            "Execute Limit Break",
            "Re-engage");

        var petTargets = BuildTargetList(
            "Away",
            "Heel",
            "Place",
            "Stay",
            "Guard",
            "Steady");

        var targets = section switch
        {
            HotbarOrderSection.Companion => companionTargets,
            HotbarOrderSection.Squadron => squadronTargets,
            HotbarOrderSection.Pets => petTargets,
            _ => companionTargets,
        };

        if (section == HotbarOrderSection.Pets && !this.IsPetOrdersAvailableForCurrentJob())
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var pool = this.GetUnfilteredAllAssignableEntries();
        var runtimePool = this.GetRuntimeOrderEntries();
        if (runtimePool.Count > 0)
        {
            var merged = new List<HotbarAssignEntry>(pool.Count + runtimePool.Count);
            merged.AddRange(pool);
            var existingKeys = new HashSet<string>(
                merged.Select(entry => $"{(int)entry.CommandKind}:{entry.CommandId}"),
                StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < runtimePool.Count; i++)
            {
                var candidate = runtimePool[i];
                var key = $"{(int)candidate.CommandKind}:{candidate.CommandId}";
                if (existingKeys.Add(key))
                {
                    merged.Add(candidate);
                }
            }

            pool = CoalesceAssignableEntriesByDisplayName(merged);
        }
        if (section == HotbarOrderSection.Squadron)
        {
            var squadronFallback = this.GetRuntimeSquadronSheetEntries();
            if (squadronFallback.Count > 0)
            {
                var merged = new List<HotbarAssignEntry>(pool.Count + squadronFallback.Count);
                merged.AddRange(pool);
                var existingKeys = new HashSet<string>(
                    merged.Select(entry => $"{(int)entry.CommandKind}:{entry.CommandId}"),
                    StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < squadronFallback.Count; i++)
                {
                    var candidate = squadronFallback[i];
                    var key = $"{(int)candidate.CommandKind}:{candidate.CommandId}";
                    if (existingKeys.Add(key))
                    {
                        merged.Add(candidate);
                    }
                }

                pool = CoalesceAssignableEntriesByDisplayName(merged);
            }
        }
        var chosen = new List<HotbarAssignEntry>(targets.Count);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var targetKey = NormalizeNameToken(target);
            if (section == HotbarOrderSection.Squadron &&
                this.capturedSquadronCommands.TryGetValue(targetKey, out var captured))
            {
                chosen.Add(new HotbarAssignEntry
                {
                    CommandKind = captured.CommandKind,
                    CommandId = captured.CommandId,
                    Name = target,
                    Description = captured.Description,
                    RequiredLevel = captured.RequiredLevel,
                    JobAbbrev = captured.JobAbbrev,
                    Affinity = captured.Affinity,
                    Icon = captured.Icon,
                });
                continue;
            }

            var aliases = BuildAliasesForTarget(target)
                .Select(NormalizeNameToken)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (aliases.Length == 0)
            {
                continue;
            }

            var tokens = BuildTokensForTarget(target);
            var primaryAlias = aliases[0];
            var match = pool
                .Where(entry =>
                {
                    var entryName = NormalizeNameToken(entry.Name);
                    if (aliases.Contains(entryName, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (tokens.Count == 0)
                    {
                        return false;
                    }

                    // Fallback for regional/localized naming variants:
                    // require all significant target tokens to be present.
                    for (var t = 0; t < tokens.Count; t++)
                    {
                        if (!entryName.Contains(tokens[t], StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    return true;
                })
                .OrderBy(entry =>
                {
                    var entryName = NormalizeNameToken(entry.Name);
                    var exact = aliases.Any(alias => entryName.Equals(alias, StringComparison.OrdinalIgnoreCase));
                    return exact ? 0 : 1;
                })
                .ThenBy(entry => CommandKindPreference(entry.CommandKind))
                .ThenBy(entry =>
                {
                    var entryName = NormalizeNameToken(entry.Name);
                    return Math.Abs(entryName.Length - primaryAlias.Length);
                })
                .ThenBy(entry => entry.CommandId)
                .FirstOrDefault();
            if (match is null)
            {
                continue;
            }

            var dedupeKey = $"{(int)match.CommandKind}:{match.CommandId}";
            if (used.Add(dedupeKey))
            {
                // Force display names to requested hardcoded labels/order.
                chosen.Add(new HotbarAssignEntry
                {
                    CommandKind = match.CommandKind,
                    CommandId = match.CommandId,
                    Name = target,
                    Description = match.Description,
                    RequiredLevel = match.RequiredLevel,
                    JobAbbrev = match.JobAbbrev,
                    Affinity = match.Affinity,
                    Icon = match.Icon,
                });
            }
        }

        if (section == HotbarOrderSection.Squadron && chosen.Count < targets.Count)
        {
            var now = DateTime.UtcNow;
            if ((now - this.lastOrdersDiagnosticsUtc).TotalSeconds >= 5)
            {
                this.lastOrdersDiagnosticsUtc = now;
                var interesting = pool
                    .Where(entry =>
                    {
                        var n = NormalizeNameToken(entry.Name);
                        return n.Contains("ENGAGE", StringComparison.OrdinalIgnoreCase) ||
                               n.Contains("DISENGAGE", StringComparison.OrdinalIgnoreCase) ||
                               n.Contains("ORDER", StringComparison.OrdinalIgnoreCase) ||
                               n.Contains("LIMITBREAK", StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => $"{entry.Name} [{entry.CommandKind}:{entry.CommandId}]")
                    .ToArray();
                var runtimeInteresting = runtimePool
                    .Where(entry =>
                    {
                        var n = NormalizeNameToken(entry.Name);
                        return n.Contains("ENGAGE", StringComparison.OrdinalIgnoreCase) ||
                               n.Contains("DISENGAGE", StringComparison.OrdinalIgnoreCase) ||
                               n.Contains("ORDER", StringComparison.OrdinalIgnoreCase) ||
                               n.Contains("LIMITBREAK", StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => $"{entry.Name} [{entry.CommandKind}:{entry.CommandId}]")
                    .ToArray();
                this.pluginLog.Information(
                    $"Orders/Squadron diagnostics: resolved={chosen.Count}/{targets.Count}; " +
                    $"sheetCandidates={(interesting.Length == 0 ? "<none>" : string.Join(" | ", interesting))}; " +
                    $"runtimeCandidates={(runtimeInteresting.Length == 0 ? "<none>" : string.Join(" | ", runtimeInteresting))}");
            }
        }

        return chosen;
    }

    private void TryCaptureSquadronCommandsFromHotbars(bool force = false)
    {
        var now = DateTime.UtcNow;
        if (!force && (now - this.lastSquadronCaptureUtc).TotalSeconds < 1.5)
        {
            return;
        }

        this.lastSquadronCaptureUtc = now;

        static string CanonicalSquadronTarget(string normalizedName)
        {
            if (normalizedName.Contains("DISPLAYORDERHOTBAR", StringComparison.OrdinalIgnoreCase))
            {
                return "DISPLAYORDERHOTBAR";
            }

            if (normalizedName.Equals("ENGAGE", StringComparison.OrdinalIgnoreCase))
            {
                return "ENGAGE";
            }

            if (normalizedName.Equals("DISENGAGE", StringComparison.OrdinalIgnoreCase))
            {
                return "DISENGAGE";
            }

            if (normalizedName.Contains("EXECUTELIMITBREAK", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Equals("LIMITBREAK", StringComparison.OrdinalIgnoreCase))
            {
                return "EXECUTELIMITBREAK";
            }

            if (normalizedName.Contains("REENGAGE", StringComparison.OrdinalIgnoreCase))
            {
                return "REENGAGE";
            }

            return string.Empty;
        }

        unsafe
        {
            var hotbarModule = RaptureHotbarModule.Instance();
            if (hotbarModule is null)
            {
                return;
            }

            var captureCountBefore = this.capturedSquadronCommands.Count;
            for (uint hotbarId = 0; hotbarId < 40; hotbarId++)
            {
                for (uint slotId = 0; slotId < 16; slotId++)
                {
                    var slot = hotbarModule->GetSlotById(hotbarId, slotId);
                    if (slot is null)
                    {
                        continue;
                    }

                    var commandType = slot->CommandType;
                    var commandId = slot->CommandId;
                    if (commandId == 0)
                    {
                        continue;
                    }

                    var displayName = SanitizeSheetText(slot->GetDisplayNameForSlot(commandType, commandId).ToString());
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    var canonical = CanonicalSquadronTarget(NormalizeNameToken(displayName));
                    if (string.IsNullOrWhiteSpace(canonical))
                    {
                        continue;
                    }

                    slot->LoadIconId();
                    this.capturedSquadronCommands[canonical] = new HotbarAssignEntry
                    {
                        CommandKind = HotbarAssignCommandKind.Action,
                        CommandId = commandId,
                        Name = displayName,
                        Description = string.Empty,
                        Affinity = string.Empty,
                        Icon = this.GetActionIcon(slot->IconId),
                    };
                    this.capturedSquadronIconIds[canonical] = slot->IconId;
                }
            }

            // Mission-specific Orders bars are not always inside low hotbar IDs.
            // Sweep a wider range less frequently to capture context-bound commands.
            for (uint hotbarId = 40; hotbarId < 256; hotbarId++)
            {
                for (uint slotId = 0; slotId < 16; slotId++)
                {
                    var slot = hotbarModule->GetSlotById(hotbarId, slotId);
                    if (slot is null)
                    {
                        continue;
                    }

                    var commandType = slot->CommandType;
                    var commandId = slot->CommandId;
                    if (commandId == 0)
                    {
                        continue;
                    }

                    var displayName = SanitizeSheetText(slot->GetDisplayNameForSlot(commandType, commandId).ToString());
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue;
                    }

                    var canonical = CanonicalSquadronTarget(NormalizeNameToken(displayName));
                    if (string.IsNullOrWhiteSpace(canonical))
                    {
                        continue;
                    }

                    slot->LoadIconId();
                    this.capturedSquadronCommands[canonical] = new HotbarAssignEntry
                    {
                        CommandKind = HotbarAssignCommandKind.Action,
                        CommandId = commandId,
                        Name = displayName,
                        Description = string.Empty,
                        Affinity = string.Empty,
                        Icon = this.GetActionIcon(slot->IconId),
                    };
                    this.capturedSquadronIconIds[canonical] = slot->IconId;
                }
            }

            InferMissingSquadronCommandsFromCaptured();

            if (this.capturedSquadronCommands.Count > captureCountBefore)
            {
                SaveCapturedSquadronCommandsToConfigIfDue();
                var captured = this.capturedSquadronCommands
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key} => {kv.Value.CommandKind}:{kv.Value.CommandId}")
                    .ToArray();
                this.pluginLog.Information(
                    $"Captured Squadron commands: {(captured.Length == 0 ? "<none>" : string.Join(" | ", captured))}");
            }
        }
    }

    private void InferMissingSquadronCommandsFromCaptured()
    {
        if (this.capturedSquadronCommands.ContainsKey("REENGAGE"))
        {
            return;
        }

        if (!this.capturedSquadronCommands.TryGetValue("ENGAGE", out var engage) ||
            !this.capturedSquadronCommands.TryGetValue("DISENGAGE", out var disengage))
        {
            return;
        }

        // In Squadron command missions these are commonly Action ids 1/2/3/4/5.
        // If we observed Engage and Disengage in that pattern but missed Re-engage,
        // infer the missing id so the picker remains stable across sessions.
        if (engage.CommandKind == HotbarAssignCommandKind.Action &&
            disengage.CommandKind == HotbarAssignCommandKind.Action &&
            engage.CommandId == 1 &&
            disengage.CommandId == 2)
        {
            this.capturedSquadronCommands["REENGAGE"] = new HotbarAssignEntry
            {
                CommandKind = HotbarAssignCommandKind.Action,
                CommandId = 3,
                Name = "Re-engage",
                Description = string.Empty,
                Affinity = string.Empty,
                Icon = null,
            };
            SaveCapturedSquadronCommandsToConfigIfDue();
        }

    }

    private void LoadCapturedSquadronCommandsFromConfig()
    {
        var list = this.configuration.CapturedSquadronCommands;
        if (list is null || list.Count == 0)
        {
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var row = list[i];
            if (row is null || string.IsNullOrWhiteSpace(row.TargetKey) || row.CommandId == 0)
            {
                continue;
            }

            var key = NormalizeNameToken(row.TargetKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            this.capturedSquadronCommands[key] = new HotbarAssignEntry
            {
                CommandKind = row.CommandKind,
                CommandId = row.CommandId,
                Name = string.IsNullOrWhiteSpace(row.DisplayName) ? row.TargetKey : row.DisplayName,
                Description = string.Empty,
                Affinity = string.Empty,
                Icon = row.IconId == 0 ? null : this.GetActionIcon(row.IconId),
            };
            if (row.IconId != 0)
            {
                this.capturedSquadronIconIds[key] = row.IconId;
            }
        }

        SaveCapturedSquadronCommandsToConfigIfDue();
    }

    private void SaveCapturedSquadronCommandsToConfigIfDue()
    {
        var now = DateTime.UtcNow;
        if ((now - this.lastSquadronCaptureSaveUtc).TotalSeconds < 0.5)
        {
            return;
        }

        this.lastSquadronCaptureSaveUtc = now;
        var rows = this.capturedSquadronCommands
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new SquadronCapturedCommandConfig
            {
                TargetKey = kv.Key,
                CommandKind = kv.Value.CommandKind,
                CommandId = kv.Value.CommandId,
                DisplayName = kv.Value.Name,
                IconId = this.capturedSquadronIconIds.TryGetValue(kv.Key, out var iconId) ? iconId : 0u,
            })
            .ToList();
        this.configuration.CapturedSquadronCommands = rows;
        this.configuration.Save();
    }

    public IReadOnlyList<HotbarAssignEntry> GetAssignableMainCommandSectionEntries(HotbarMainCommandSection section)
    {
        static List<string> BuildTargets(params string[] names)
        {
            return names.ToList();
        }

        static IReadOnlyList<string> BuildAliasesForTarget(string target)
        {
            var normalized = NormalizeNameToken(target);
            return normalized switch
            {
                "ACTIONSTRAITS" => new[] { "ACTIONSTRAITS", "ACTIONANDTRAITS" },
                "ADVENTUREPLATE" => new[] { "ADVENTUREPLATE", "ADVENTURERPLATE" },
                "ADVENTURERPLATE" => new[] { "ADVENTURERPLATE", "ADVENTUREPLATE" },
                "VCDUNGEONFINDER" => new[] { "VCDUNGEONFINDER", "VARIANTCRITERIONDUNGEONFINDER", "VARIANTDUNGEONFINDER", "CRITERIONDUNGEONFINDER" },
                "PARTYMEMBERS" => new[] { "PARTYMEMBERS", "PARTYLIST" },
                "CROSSWORLDLINKSHELLS" => new[] { "CROSSWORLDLINKSHELLS", "CROSSLINKSHELLS" },
                "AETHERCURRENTS" => new[] { "AETHERCURRENTS", "AETHERCURRENT" },
                "SHAREDFATE" => new[] { "SHAREDFATE", "SHAREDFATES" },
                "CHARACTERCONFIGURATION" => new[] { "CHARACTERCONFIGURATION", "CHARCONFIG" },
                "SYSTEMCONFIGURATION" => new[] { "SYSTEMCONFIGURATION", "SYSCONFIG" },
                "HUDLAYOUT" => new[] { "HUDLAYOUT", "HUD" },
                "USERMACROS" => new[] { "USERMACROS", "MACRO" },
                _ => new[] { normalized },
            };
        }

        static int CommandKindPreference(HotbarAssignCommandKind kind)
        {
            return kind switch
            {
                HotbarAssignCommandKind.MainCommand => 0,
                HotbarAssignCommandKind.ExtraCommand => 1,
                HotbarAssignCommandKind.GeneralAction => 2,
                HotbarAssignCommandKind.BuddyAction => 3,
                HotbarAssignCommandKind.PetAction => 4,
                _ => 5,
            };
        }

        var targets = section switch
        {
            HotbarMainCommandSection.Character => BuildTargets(
                "Stance",
                "Actions & Traits",
                "Adventurer Plate",
                "Portraits",
                "Currency",
                "Character",
                "Armoury Chest",
                "Inventory",
                "Chocobo Saddlebag",
                "Companion",
                "Mount Guide",
                "Minion Guide",
                "Facewear",
                "Fashion Accessories",
                "Blue Magic Spellbook",
                "PvP Profile",
                "Gold Saucer",
                "Achievements"),
            HotbarMainCommandSection.Duty => BuildTargets(
                "Recommendations",
                "Collection",
                "Key Items",
                "Journal",
                "Duty Finder",
                "Raid Finder",
                "V&C Dungeon Finder",
                "Duty Support",
                "Trust",
                "Duty Recorder",
                "New Game+",
                "Hall of the Novice",
                "Timers"),
            HotbarMainCommandSection.Logs => BuildTargets(
                "Hunting Log",
                "Sightseeing Log",
                "Crafting Log",
                "Gathering Log",
                "Fishing Log",
                "Fish Guide",
                "Orchestrion List",
                "Challenge Log"),
            HotbarMainCommandSection.Travel => BuildTargets(
                "Aether Currents",
                "Mount Speed",
                "Shared FATE",
                "Map"),
            HotbarMainCommandSection.Party => BuildTargets(
                "Party Members",
                "Party Finder",
                "Signs",
                "Waymarks",
                "Strategy Board",
                "Record Ready Check",
                "Ready Check",
                "Countdown"),
            HotbarMainCommandSection.Social => BuildTargets(
                "Player Search",
                "Fellowship Finder",
                "Emotes",
                "Free Company",
                "Housing",
                "PvP Team",
                "Linkshells",
                "Cross-world Linkshells",
                "Fellowships",
                "Friend List",
                "Contacts",
                "Blacklist",
                "Mute List",
                "Term Filter"),
            HotbarMainCommandSection.System => BuildTargets(
                "Support Desk",
                "Official Sites",
                "Playguide",
                "Active Help",
                "Character Configuration",
                "System Configuration",
                "HUD Layout",
                "User Macros",
                "Keybind",
                "Configuration Sharing",
                "License",
                "Log Out",
                "Exit Game"),
            _ => BuildTargets("Stance", "Actions & Traits", "Adventure Plate"),
        };

        var pool = this.GetUnfilteredAllAssignableEntries();
        var chosen = new List<HotbarAssignEntry>(targets.Count);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var aliases = BuildAliasesForTarget(target)
                .Select(NormalizeNameToken)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (aliases.Length == 0)
            {
                continue;
            }

            var match = pool
                .Where(entry => aliases.Contains(NormalizeNameToken(entry.Name), StringComparer.OrdinalIgnoreCase))
                .OrderBy(entry => CommandKindPreference(entry.CommandKind))
                .ThenBy(entry => entry.CommandId)
                .FirstOrDefault();
            if (match is null)
            {
                continue;
            }

            var key = $"{(int)match.CommandKind}:{match.CommandId}";
            if (!used.Add(key))
            {
                continue;
            }

            chosen.Add(new HotbarAssignEntry
            {
                CommandKind = match.CommandKind,
                CommandId = match.CommandId,
                Name = target,
                Description = match.Description,
                RequiredLevel = match.RequiredLevel,
                JobAbbrev = match.JobAbbrev,
                Affinity = match.Affinity,
                Icon = match.Icon,
            });
        }

        return chosen;
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableExtrasEntries()
    {
        static List<string> BuildTargets(params string[] names)
        {
            return names.ToList();
        }

        static IReadOnlyList<string> BuildAliasesForTarget(string target)
        {
            var normalized = NormalizeNameToken(target);
            return normalized switch
            {
                "GROUPPOSE" => new[] { "GROUPPOSE", "GPOSE" },
                "IDLINGCAMERA" => new[] { "IDLINGCAMERA", "IDLECAMERA" },
                "COMMANDPANEL" => new[] { "COMMANDPANEL" },
                "ALARM" => new[] { "ALARM" },
                _ => new[] { normalized },
            };
        }

        static int CommandKindPreference(HotbarAssignCommandKind kind)
        {
            return kind switch
            {
                HotbarAssignCommandKind.ExtraCommand => 0,
                HotbarAssignCommandKind.MainCommand => 1,
                HotbarAssignCommandKind.GeneralAction => 2,
                _ => 3,
            };
        }

        var targets = BuildTargets(
            "Group Pose",
            "Idling Camera",
            "Alarm",
            "Command Panel");

        var pool = this.GetUnfilteredAllAssignableEntries();
        var chosen = new List<HotbarAssignEntry>(targets.Count);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var aliases = BuildAliasesForTarget(target)
                .Select(NormalizeNameToken)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (aliases.Length == 0)
            {
                continue;
            }

            var match = pool
                .Where(entry => aliases.Contains(NormalizeNameToken(entry.Name), StringComparer.OrdinalIgnoreCase))
                .OrderBy(entry => CommandKindPreference(entry.CommandKind))
                .ThenBy(entry => entry.CommandId)
                .FirstOrDefault();
            if (match is null)
            {
                continue;
            }

            var key = $"{(int)match.CommandKind}:{match.CommandId}";
            if (!used.Add(key))
            {
                continue;
            }

            chosen.Add(new HotbarAssignEntry
            {
                CommandKind = match.CommandKind,
                CommandId = match.CommandId,
                Name = target,
                Description = match.Description,
                RequiredLevel = match.RequiredLevel,
                JobAbbrev = match.JobAbbrev,
                Affinity = match.Affinity,
                Icon = match.Icon,
            });
        }

        return chosen;
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableGeneralEntries()
    {
        static List<string> BuildTargets(params string[] names)
        {
            return names.ToList();
        }

        static IReadOnlyList<string> BuildAliasesForTarget(string target)
        {
            var normalized = NormalizeNameToken(target);
            return normalized switch
            {
                "LIMITBREAK" => new[] { "LIMITBREAK", "EXECUTELIMITBREAK" },
                "AUTOATTACK" => new[] { "AUTOATTACK", "AUTO-ATTACK" },
                "TARGETFORWARD" => new[] { "TARGETFORWARD", "TARGETNEXT" },
                "TARGETBACK" => new[] { "TARGETBACK", "TARGETPREVIOUS" },
                "ATTACKTARGET" => new[] { "ATTACKTARGET", "ATTACK1", "ATTACKSIGN" },
                "BINDTARGET" => new[] { "BINDTARGET", "BINDSIGN" },
                "IGNORETARGET" => new[] { "IGNORETARGET", "IGNORESIGN" },
                "SQUARETARGET" => new[] { "SQUARETARGET", "SQUARESIGN" },
                "CIRCLETARGET" => new[] { "CIRCLETARGET", "CIRCLESIGN" },
                "PLUSTARGET" => new[] { "PLUSTARGET", "PLUSSIGN", "CROSSSIGN" },
                "TRIANGLETARGET" => new[] { "TRIANGLETARGET", "TRIANGLESIGN" },
                "MOUNTROULETTE" => new[] { "MOUNTROULETTE", "ROULETTEMOUNT" },
                "MINIONROULETTE" => new[] { "MINIONROULETTE", "ROULETTEMINION" },
                "MATERIAEXTRACTION" => new[] { "MATERIAEXTRACTION", "EXTRACTMATERIA" },
                "MATERIAMELDING" => new[] { "MATERIAMELDING", "MELDMATERIA" },
                "CASTGLAMOUR" => new[] { "CASTGLAMOUR", "GLAMOURCAST" },
                "GLAMOURPLATE" => new[] { "GLAMOURPLATE", "PLATEGLAMOUR" },
                "AETHERIALREDUCTION" => new[] { "AETHERIALREDUCTION", "REDUCTION" },
                "NEXTBOARD" => new[] { "NEXTBOARD", "BOARDNEXT" },
                "PREVIOUSBOARD" => new[] { "PREVIOUSBOARD", "BOARDPREVIOUS" },
                _ => new[] { normalized },
            };
        }

        static int CommandKindPreference(HotbarAssignCommandKind kind)
        {
            return kind switch
            {
                HotbarAssignCommandKind.GeneralAction => 0,
                HotbarAssignCommandKind.MainCommand => 1,
                HotbarAssignCommandKind.ExtraCommand => 2,
                HotbarAssignCommandKind.BuddyAction => 3,
                HotbarAssignCommandKind.PetAction => 4,
                _ => 5,
            };
        }

        var targets = BuildTargets(
            "Sprint",
            "Teleport",
            "Return",
            "Limit Break",
            "Auto-attack",
            "Jump",
            "Target Forward",
            "Target Back",
            "Attack Target",
            "Bind Target",
            "Ignore Target",
            "Square Target",
            "Circle Target",
            "Plus Target",
            "Triangle Target",
            "Mount Roulette",
            "Minion Roulette",
            "Repair",
            "Materia Extraction",
            "Materia Melding",
            "Dye",
            "Cast Glamour",
            "Glamour Plate",
            "Desynthesis",
            "Aetherial Reduction",
            "Decipher",
            "Dig",
            "Next Board",
            "Previous Board");

        var pool = this.GetUnfilteredAllAssignableEntries();
        var chosen = new List<HotbarAssignEntry>(targets.Count);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var aliases = BuildAliasesForTarget(target)
                .Select(NormalizeNameToken)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (aliases.Length == 0)
            {
                continue;
            }

            var match = pool
                .Where(entry => aliases.Contains(NormalizeNameToken(entry.Name), StringComparer.OrdinalIgnoreCase))
                .OrderBy(entry => CommandKindPreference(entry.CommandKind))
                .ThenBy(entry => entry.CommandId)
                .FirstOrDefault();
            if (match is null)
            {
                continue;
            }

            var key = $"{(int)match.CommandKind}:{match.CommandId}";
            if (!used.Add(key))
            {
                continue;
            }

            chosen.Add(new HotbarAssignEntry
            {
                CommandKind = match.CommandKind,
                CommandId = match.CommandId,
                Name = target,
                Description = match.Description,
                RequiredLevel = match.RequiredLevel,
                JobAbbrev = match.JobAbbrev,
                Affinity = match.Affinity,
                Icon = match.Icon,
            });
        }

        return chosen;
    }

    private IReadOnlyList<HotbarAssignEntry> GetRuntimeOrderEntries()
    {
        var now = DateTime.UtcNow;
        if ((now - this.runtimeOrderCacheUtc).TotalSeconds < 20 && this.runtimeOrderCache.Count > 0)
        {
            return this.runtimeOrderCache;
        }

        unsafe
        {
            var hotbarModule = RaptureHotbarModule.Instance();
            if (hotbarModule is null)
            {
                return Array.Empty<HotbarAssignEntry>();
            }

            var list = new List<HotbarAssignEntry>(512);
            var slotTypes = new[]
            {
                (RaptureHotbarModule.HotbarSlotType.BuddyAction, HotbarAssignCommandKind.BuddyAction),
                (RaptureHotbarModule.HotbarSlotType.PetAction, HotbarAssignCommandKind.PetAction),
                (RaptureHotbarModule.HotbarSlotType.GeneralAction, HotbarAssignCommandKind.GeneralAction),
                (RaptureHotbarModule.HotbarSlotType.MainCommand, HotbarAssignCommandKind.MainCommand),
                (RaptureHotbarModule.HotbarSlotType.ExtraCommand, HotbarAssignCommandKind.ExtraCommand),
                (RaptureHotbarModule.HotbarSlotType.Unknown23, HotbarAssignCommandKind.Unknown23),
                (RaptureHotbarModule.HotbarSlotType.Unknown28, HotbarAssignCommandKind.Unknown28),
            };

            for (var s = 0; s < slotTypes.Length; s++)
            {
                var slotType = slotTypes[s].Item1;
                var kind = slotTypes[s].Item2;
                var slotProbe = hotbarModule->GetSlotById(0, 0);
                if (slotProbe is null)
                {
                    continue;
                }

                // Squadron-like command ids can live outside the low ranges.
                // Probe a deeper range and cache results to avoid frame hitches.
                for (uint id = 1; id <= 5000; id++)
                {
                    var namePtr = slotProbe->GetDisplayNameForSlot(slotType, id);
                    var rawName = namePtr.ToString();
                    var name = SanitizeSheetText(rawName);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (name.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Unknown ", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Action ", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var iconIdRaw = slotProbe->GetIconIdForSlot(slotType, id);
                    var iconId = iconIdRaw > 0 ? (uint)iconIdRaw : 0u;
                    list.Add(new HotbarAssignEntry
                    {
                        CommandKind = kind,
                        CommandId = id,
                        Name = name,
                        Description = string.Empty,
                        Affinity = string.Empty,
                        Icon = this.GetActionIcon(iconId),
                    });
                }
            }

            this.runtimeOrderCache = CoalesceAssignableEntriesByDisplayName(list);
            this.runtimeOrderCacheUtc = now;
            return this.runtimeOrderCache;
        }
    }

    private IReadOnlyList<HotbarAssignEntry> GetRuntimeSquadronSheetEntries()
    {
        var now = DateTime.UtcNow;
        if ((now - this.runtimeSquadronSheetCacheUtc).TotalSeconds < 45 && this.runtimeSquadronSheetCache.Count > 0)
        {
            return this.runtimeSquadronSheetCache;
        }

        static bool IsTargetName(string normalized)
        {
            return normalized is "DISPLAYORDERHOTBAR" or "ENGAGE" or "DISENGAGE" or "EXECUTELIMITBREAK" or "REENGAGE";
        }

        static HotbarAssignCommandKind MapTypeNameToKind(string typeName)
        {
            if (typeName.Equals("BuddyAction", StringComparison.OrdinalIgnoreCase))
            {
                return HotbarAssignCommandKind.BuddyAction;
            }

            if (typeName.Equals("GeneralAction", StringComparison.OrdinalIgnoreCase))
            {
                return HotbarAssignCommandKind.GeneralAction;
            }

            if (typeName.Equals("ExtraCommand", StringComparison.OrdinalIgnoreCase))
            {
                return HotbarAssignCommandKind.ExtraCommand;
            }

            return HotbarAssignCommandKind.MainCommand;
        }

        try
        {
            var rowAssembly = typeof(Lumina.Excel.Sheets.Action).Assembly;
            var getSheetMethod = this.dataManager.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name == "GetExcelSheet" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 0);
            if (getSheetMethod is null)
            {
                return Array.Empty<HotbarAssignEntry>();
            }

            var candidateTypes = rowAssembly
                .GetTypes()
                .Where(t =>
                    t.IsValueType &&
                    t.Namespace == "Lumina.Excel.Sheets" &&
                    (t.Name.Contains("Buddy", StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Contains("Companion", StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Contains("Order", StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Contains("Gc", StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Contains("Squad", StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            var found = new List<HotbarAssignEntry>(32);
            for (var i = 0; i < candidateTypes.Length; i++)
            {
                var type = candidateTypes[i];
                object? sheetObj;
                try
                {
                    var generic = getSheetMethod.MakeGenericMethod(type);
                    sheetObj = generic.Invoke(this.dataManager, null);
                }
                catch
                {
                    continue;
                }

                if (sheetObj is null || sheetObj is not System.Collections.IEnumerable enumerable)
                {
                    continue;
                }

                foreach (var rowObj in enumerable)
                {
                    if (rowObj is null)
                    {
                        continue;
                    }

                    var rowType = rowObj.GetType();
                    var nameValue = GetMemberValue(rowType, rowObj, "Name");
                    if (nameValue is null)
                    {
                        continue;
                    }

                    var name = SanitizeSheetText(nameValue.ToString() ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var normalized = NormalizeNameToken(name);
                    if (!IsTargetName(normalized))
                    {
                        continue;
                    }

                    var rowIdValue = GetMemberValue(rowType, rowObj, "RowId");
                    var rowId = rowIdValue is not null ? ExtractRowIdLike(rowIdValue) : 0u;
                    if (rowId == 0)
                    {
                        continue;
                    }

                    var iconValue = GetMemberValue(rowType, rowObj, "Icon");
                    var iconId = iconValue is not null ? ExtractIconId(iconValue) : 0u;
                    var descriptionValue = GetMemberValue(rowType, rowObj, "Description");
                    var popupHelpValue = GetMemberValue(rowType, rowObj, "PopupHelp");
                    var description = SanitizeSheetText(descriptionValue?.ToString() ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        description = SanitizeSheetText(popupHelpValue?.ToString() ?? string.Empty);
                    }

                    found.Add(new HotbarAssignEntry
                    {
                        CommandKind = MapTypeNameToKind(type.Name),
                        CommandId = rowId,
                        Name = name,
                        Description = description,
                        Affinity = string.Empty,
                        Icon = this.GetActionIcon(iconId),
                    });
                }
            }

            this.runtimeSquadronSheetCache = CoalesceAssignableEntriesByDisplayName(found);
            this.runtimeSquadronSheetCacheUtc = now;
            return this.runtimeSquadronSheetCache;
        }
        catch (Exception ex)
        {
            this.pluginLog.Debug(ex, "Runtime squadron sheet scan failed.");
            return Array.Empty<HotbarAssignEntry>();
        }
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableDutyEntries()
    {
        var general = this.GetAssignableSheetEntries<GeneralAction>(HotbarAssignCommandKind.GeneralAction);
        var filtered = general
            .Where(entry =>
                entry.Name.Contains("Duty Action", StringComparison.OrdinalIgnoreCase) ||
                entry.Name.Contains("Phantom Action", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return CoalesceAssignableEntriesByDisplayName(filtered)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignablePerformanceEntries()
    {
        static bool HasPerformanceTag(string name, string description)
        {
            return description.Contains("Performance Action", StringComparison.OrdinalIgnoreCase) ||
                   description.Contains("Performance Actions", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Performance", StringComparison.OrdinalIgnoreCase);
        }

        var list = new List<HotbarAssignEntry>(64);
        // Primary source: Perform sheet (instrument actions).
        list.AddRange(this.GetAssignablePerformEntries());

        // Secondary sources: legacy command sheets where some clients/locales expose performance text.
        var general = this.GetAssignableSheetEntries<GeneralAction>(HotbarAssignCommandKind.GeneralAction);
        list.AddRange(general.Where(entry => HasPerformanceTag(entry.Name, entry.Description)));
        var main = this.GetAssignableMainCommandEntries();
        list.AddRange(main.Where(entry => HasPerformanceTag(entry.Name, entry.Description)));
        var extras = this.GetAssignableExtraCommandEntries();
        list.AddRange(extras.Where(entry => HasPerformanceTag(entry.Name, entry.Description)));

        // Explicit instrument-name fallback requested by user examples.
        var instrumentKeywords = new[] { "Harp", "Piano", "Lute", "Fiddle", "Flute", "Oboe" };
        list.AddRange(general.Where(entry => instrumentKeywords.Any(k => entry.Name.Contains(k, StringComparison.OrdinalIgnoreCase))));
        list.AddRange(main.Where(entry => instrumentKeywords.Any(k => entry.Name.Contains(k, StringComparison.OrdinalIgnoreCase))));
        list.AddRange(extras.Where(entry => instrumentKeywords.Any(k => entry.Name.Contains(k, StringComparison.OrdinalIgnoreCase))));

        return CoalesceAssignableEntriesByDisplayName(list)
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignablePerformEntries()
    {
        static string GetDisplayNameFromRawPerformName(string rawName)
        {
            var normalized = NormalizeNameToken(rawName).ToLowerInvariant();
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            // Common internal Perform keys -> player-facing labels.
            if (normalized.Contains("cleanguitar", StringComparison.OrdinalIgnoreCase))
            {
                return "Electric Guitar: Clean";
            }

            if (normalized.Contains("muteguitar", StringComparison.OrdinalIgnoreCase))
            {
                return "Electric Guitar: Muted";
            }

            if (normalized.Contains("driveguitar", StringComparison.OrdinalIgnoreCase))
            {
                return "Electric Guitar: Overdriven";
            }

            if (normalized.Contains("powerguitar", StringComparison.OrdinalIgnoreCase))
            {
                return "Electric Guitar: Power Chords";
            }

            if (normalized.Contains("fxguitar", StringComparison.OrdinalIgnoreCase))
            {
                return "Electric Guitar: Special";
            }

            if (normalized.Contains("contrabass", StringComparison.OrdinalIgnoreCase))
            {
                return "Double Bass";
            }

            if (normalized.Contains("snaredrum", StringComparison.OrdinalIgnoreCase))
            {
                return "Snare Drum";
            }

            if (normalized.Contains("bassdrum", StringComparison.OrdinalIgnoreCase))
            {
                return "Bass Drum";
            }

            if (normalized.Contains("panpipes", StringComparison.OrdinalIgnoreCase))
            {
                return "Panpipes";
            }

            // Strip leading numeric prefixes used by internal naming (e.g., "028cleanguitar").
            var trimmed = rawName.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            if (trimmed.Length == 0)
            {
                trimmed = rawName;
            }

            trimmed = trimmed
                .Replace("_", " ", StringComparison.Ordinal)
                .Replace("-", " ", StringComparison.Ordinal)
                .Trim();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            // Title-case simple tokens while preserving spaces.
            var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
            {
                var token = words[i];
                if (token.Length == 0)
                {
                    continue;
                }

                words[i] = token.Length == 1
                    ? token.ToUpperInvariant()
                    : char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant();
            }

            return string.Join(" ", words);
        }

        var sheet = this.dataManager.GetExcelSheet<Perform>();
        if (sheet is null)
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var list = new List<HotbarAssignEntry>(32);
        foreach (var row in sheet)
        {
            if (row.RowId == 0)
            {
                continue;
            }

            var rowType = typeof(Perform);
            var nameValue = GetMemberValue(rowType, row, "Name");
            var iconValue = GetMemberValue(rowType, row, "Icon");
            var descriptionValue = GetMemberValue(rowType, row, "Description") ?? GetMemberValue(rowType, row, "PopupHelp");
            if (nameValue is null || iconValue is null)
            {
                continue;
            }

            var rawName = SanitizeSheetText(nameValue.ToString() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(rawName))
            {
                continue;
            }

            var displayName = GetDisplayNameFromRawPerformName(rawName);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var iconId = ExtractIconId(iconValue);
            if (iconId == 0)
            {
                continue;
            }

            var description = SanitizeSheetText(descriptionValue?.ToString() ?? string.Empty);
            list.Add(new HotbarAssignEntry
            {
                // Treat performance commands as GeneralAction ids for assignment compatibility.
                CommandKind = HotbarAssignCommandKind.GeneralAction,
                CommandId = row.RowId,
                Name = displayName,
                Description = description,
                Affinity = string.Empty,
                Icon = this.GetActionIcon(iconId),
            });
        }

        return list;
    }

    private static string SanitizeKeybind(string? keybindHint, string fallback)
    {
        if (string.IsNullOrWhiteSpace(keybindHint))
        {
            return fallback;
        }

        var cleaned = keybindHint
            .Replace("\0", string.Empty, StringComparison.Ordinal)
            .Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }


    private static string SanitizeSheetText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input
            .Replace("\0", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static RaptureHotbarModule.HotbarSlotType MapAssignCommandKindToHotbarSlotType(HotbarAssignCommandKind commandKind)
    {
        return commandKind switch
        {
            HotbarAssignCommandKind.GeneralAction => RaptureHotbarModule.HotbarSlotType.GeneralAction,
            HotbarAssignCommandKind.MainCommand => RaptureHotbarModule.HotbarSlotType.MainCommand,
            HotbarAssignCommandKind.ExtraCommand => RaptureHotbarModule.HotbarSlotType.ExtraCommand,
            HotbarAssignCommandKind.BuddyAction => RaptureHotbarModule.HotbarSlotType.BuddyAction,
            HotbarAssignCommandKind.PetAction => RaptureHotbarModule.HotbarSlotType.PetAction,
            HotbarAssignCommandKind.Unknown23 => RaptureHotbarModule.HotbarSlotType.Unknown23,
            HotbarAssignCommandKind.Unknown28 => RaptureHotbarModule.HotbarSlotType.Unknown28,
            _ => RaptureHotbarModule.HotbarSlotType.Action,
        };
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableJobActionEntries()
    {
        var actionSheet = this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        var actionCategorySheet = this.dataManager.GetExcelSheet<ActionCategory>();
        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (actionSheet is null)
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var player = this.objectTable.LocalPlayer;
        var playerJobId = player?.ClassJob.RowId ?? 0u;
        if (playerJobId == 0)
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var allowedClassJobIds = new HashSet<uint> { playerJobId };
        if (classJobSheet is not null && classJobSheet.TryGetRow(playerJobId, out var playerClassJob))
        {
            // Job actions tab should include both advanced job and its base class
            // (e.g. BRD + ARC, PLD + GLA) to match Actions & Traits behavior.
            var parentValue = GetMemberValue(typeof(ClassJob), playerClassJob, "ClassJobParent");
            if (parentValue is not null)
            {
                var parentId = ExtractRowIdLike(parentValue);
                if (parentId != 0)
                {
                    allowedClassJobIds.Add(parentId);
                }
            }
        }

        var playerLevel = (int)(player?.Level ?? 100u);
        var list = new List<HotbarAssignEntry>(256);
        foreach (var row in actionSheet)
        {
            if (row.RowId == 0 || row.Icon == 0)
            {
                continue;
            }

            var name = SanitizeSheetText(row.Name.ToString());
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!IsActionCategoryAllowedForActionsTab(row, actionCategorySheet))
            {
                continue;
            }

            if (IsPvpAction(row, actionCategorySheet))
            {
                continue;
            }

            if (!IsLikelyAssignablePlayerAction(row))
            {
                continue;
            }

            // Actions tab: current job and its base class actions.
            if (!allowedClassJobIds.Contains(row.ClassJob.RowId))
            {
                continue;
            }

            if (row.ClassJobLevel > playerLevel)
            {
                continue;
            }

            list.Add(new HotbarAssignEntry
            {
                CommandKind = HotbarAssignCommandKind.Action,
                CommandId = row.RowId,
                Name = name,
                Description = SanitizeSheetText(this.GetActionDescription(row.RowId)),
                RequiredLevel = row.ClassJobLevel,
                JobAbbrev = this.GetActionJobAbbreviation(row),
                Affinity = this.GetActionAffinitySummary(row),
                Icon = this.GetActionIcon(row.Icon),
            });
        }

        return KeepHighestRankEntriesPerActionFamily(CoalesceAssignableEntriesByDisplayName(list))
            .OrderBy(x => x.RequiredLevel == 0 ? int.MaxValue : x.RequiredLevel)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableRoleActionEntries()
    {
        var actionSheet = this.dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (actionSheet is null)
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var player = this.objectTable.LocalPlayer;
        var playerJobId = player?.ClassJob.RowId ?? 0u;
        if (playerJobId == 0 || classJobSheet is null || !classJobSheet.TryGetRow(playerJobId, out var playerJob))
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var playerLevel = (int)(player?.Level ?? 100u);
        var playerJobAbbrev = SanitizeSheetText(playerJob.Abbreviation.ToString()).ToUpperInvariant();
        var canonicalRoleNames = GetCanonicalRoleActionNamesForJob(playerJobAbbrev);
        if (canonicalRoleNames.Count == 0)
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var list = new List<HotbarAssignEntry>(32);
        foreach (var row in actionSheet)
        {
            if (row.RowId == 0 || row.Icon == 0)
            {
                continue;
            }

            var name = SanitizeSheetText(row.Name.ToString());
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var normalizedName = NormalizeNameToken(name);
            if (!canonicalRoleNames.Contains(normalizedName))
            {
                continue;
            }

            if (row.ClassJobLevel > playerLevel)
            {
                continue;
            }

            var affinity = this.GetActionAffinitySummary(row);
            list.Add(new HotbarAssignEntry
            {
                CommandKind = HotbarAssignCommandKind.Action,
                CommandId = row.RowId,
                Name = name,
                Description = SanitizeSheetText(this.GetActionDescription(row.RowId)),
                RequiredLevel = row.ClassJobLevel,
                JobAbbrev = this.GetActionJobAbbreviation(row),
                Affinity = affinity,
                Icon = this.GetActionIcon(row.Icon),
            });
        }

        return CoalesceAssignableEntriesByDisplayName(list)
            .OrderBy(x => x.RequiredLevel == 0 ? int.MaxValue : x.RequiredLevel)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static HashSet<string> GetCanonicalRoleActionNamesForJob(string playerJobAbbrev)
    {
        static HashSet<string> SetOf(params string[] names)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < names.Length; i++)
            {
                set.Add(NormalizeNameToken(names[i]));
            }

            return set;
        }

        // Explicit job -> role-group mapping to avoid enum mismatch issues.
        var healerJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CNJ", "WHM", "SCH", "AST", "SGE" };
        var tankJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GLA", "PLD", "MRD", "WAR", "DRK", "GNB" };
        var meleeJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PGL", "MNK", "LNC", "DRG", "ROG", "NIN", "SAM", "RPR", "VPR" };
        var physRangedJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ARC", "BRD", "MCH", "DNC" };
        var magicJobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "THM", "BLM", "ACN", "SMN", "RDM", "PCT", "BLU" };

        if (tankJobs.Contains(playerJobAbbrev))
        {
            // Tank
            return SetOf("Rampart", "Low Blow", "Provoke", "Interject", "Reprisal", "Arm's Length", "Shirk");
        }

        if (healerJobs.Contains(playerJobAbbrev))
        {
            return SetOf("Repose", "Esuna", "Lucid Dreaming", "Swiftcast", "Surecast", "Rescue");
        }

        if (meleeJobs.Contains(playerJobAbbrev))
        {
            return SetOf("Second Wind", "Leg Sweep", "Bloodbath", "Feint", "Arm's Length", "True North");
        }

        if (physRangedJobs.Contains(playerJobAbbrev))
        {
            return SetOf("Leg Graze", "Second Wind", "Foot Graze", "Peloton", "Head Graze", "Arm's Length");
        }

        if (magicJobs.Contains(playerJobAbbrev))
        {
            return SetOf("Addle", "Sleep", "Lucid Dreaming", "Swiftcast", "Surecast");
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeNameToken(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var chars = input
            .Where(char.IsLetterOrDigit)
            .ToArray();
        return new string(chars).ToUpperInvariant();
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableSheetEntries<TSheet>(HotbarAssignCommandKind kind)
        where TSheet : struct, IExcelRow<TSheet>
    {
        var sheet = this.dataManager.GetExcelSheet<TSheet>();
        if (sheet is null)
        {
            return Array.Empty<HotbarAssignEntry>();
        }

        var list = new List<HotbarAssignEntry>(128);
        foreach (var row in sheet)
        {
            var rowId = row.RowId;
            if (rowId == 0)
            {
                continue;
            }

            var rowType = typeof(TSheet);
            var nameValue = GetMemberValue(rowType, row, "Name");
            var iconValue = GetMemberValue(rowType, row, "Icon");
            var descriptionValue = GetMemberValue(rowType, row, "Description");
            var popupHelpValue = GetMemberValue(rowType, row, "PopupHelp");
            if (nameValue is null || iconValue is null)
            {
                continue;
            }

            var name = SanitizeSheetText(nameValue.ToString() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var iconId = ExtractIconId(iconValue);
            if (iconId == 0)
            {
                continue;
            }

            var description = SanitizeSheetText(descriptionValue?.ToString() ?? string.Empty);
            if (string.IsNullOrWhiteSpace(description))
            {
                description = SanitizeSheetText(popupHelpValue?.ToString() ?? string.Empty);
            }
            list.Add(new HotbarAssignEntry
            {
                CommandKind = kind,
                CommandId = rowId,
                Name = name,
                Description = description,
                Affinity = string.Empty,
                Icon = this.GetActionIcon(iconId),
            });
        }

        return list
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableMainCommandEntries(string? keywordFilter = null)
    {
        var all = this.GetAssignableSheetEntries<MainCommand>(HotbarAssignCommandKind.MainCommand);
        if (string.IsNullOrWhiteSpace(keywordFilter))
        {
            return all;
        }

        var keyword = keywordFilter.Trim();
        var filtered = all
            .Where(entry =>
                entry.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                entry.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return filtered.Length > 0 ? filtered : all;
    }

    private IReadOnlyList<HotbarAssignEntry> GetAssignableExtraCommandEntries(string? keywordFilter = null)
    {
        var all = this.GetAssignableSheetEntries<ExtraCommand>(HotbarAssignCommandKind.ExtraCommand);
        if (string.IsNullOrWhiteSpace(keywordFilter))
        {
            return all;
        }

        var keyword = keywordFilter.Trim();
        var filtered = all
            .Where(entry =>
                entry.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                entry.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return filtered.Length > 0 ? filtered : all;
    }

    private IReadOnlyList<HotbarAssignEntry> GetUnfilteredAllAssignableEntries()
    {
        var list = new List<HotbarAssignEntry>(256);
        list.AddRange(this.GetAssignableSheetEntries<GeneralAction>(HotbarAssignCommandKind.GeneralAction));
        list.AddRange(this.GetAssignableSheetEntries<MainCommand>(HotbarAssignCommandKind.MainCommand));
        list.AddRange(this.GetAssignableSheetEntries<ExtraCommand>(HotbarAssignCommandKind.ExtraCommand));
        list.AddRange(this.GetAssignableSheetEntries<BuddyAction>(HotbarAssignCommandKind.BuddyAction));
        list.AddRange(this.GetAssignableSheetEntries<Companion>(HotbarAssignCommandKind.BuddyAction));
        return CoalesceAssignableEntriesByDisplayName(list);
    }

    private static IReadOnlyList<HotbarAssignEntry> CoalesceAssignableEntriesByDisplayName(IEnumerable<HotbarAssignEntry> entries)
    {
        var chosen = new Dictionary<string, HotbarAssignEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var key = entry.Name.Trim();
            if (key.Length == 0)
            {
                continue;
            }

            if (!chosen.TryGetValue(key, out var existing))
            {
                chosen[key] = entry;
                continue;
            }

            // Prefer entries that look like canonical Actions & Traits rows:
            // - has an acquisition level,
            // - has non-empty description,
            // - if tied, keep the lower command id for stability.
            var existingScore = ScoreAssignableEntry(existing);
            var incomingScore = ScoreAssignableEntry(entry);
            if (incomingScore > existingScore ||
                (incomingScore == existingScore && entry.CommandId < existing.CommandId))
            {
                chosen[key] = entry;
            }
        }

        return chosen.Values.ToArray();
    }

    private static object? GetMemberValue<T>(Type rowType, T row, string memberName)
    {
        var prop = rowType.GetProperty(memberName);
        if (prop is not null)
        {
            return prop.GetValue(row);
        }

        var field = rowType.GetField(memberName);
        return field?.GetValue(row);
    }

    private static uint ExtractIconId(object iconValue)
    {
        return iconValue switch
        {
            uint ui => ui,
            int i when i > 0 => (uint)i,
            ushort us => us,
            short s when s > 0 => (uint)s,
            byte b => b,
            long l when l > 0 => (uint)l,
            ulong ul when ul > 0 => (uint)Math.Min(ul, uint.MaxValue),
            _ => ExtractRowIdLike(iconValue),
        };
    }

    private static uint ExtractRowIdLike(object value)
    {
        var type = value.GetType();
        var rowIdProperty = type.GetProperty("RowId");
        if (rowIdProperty is not null)
        {
            var obj = rowIdProperty.GetValue(value);
            return obj is uint ui ? ui : 0u;
        }

        var rowIdField = type.GetField("RowId");
        if (rowIdField is not null)
        {
            var obj = rowIdField.GetValue(value);
            return obj is uint ui ? ui : 0u;
        }

        return 0u;
    }

    private static IReadOnlyList<HotbarAssignEntry> KeepHighestRankEntriesPerActionFamily(IEnumerable<HotbarAssignEntry> entries)
    {
        var chosen = new Dictionary<string, HotbarAssignEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var familyKey = GetActionFamilyKey(entry.Name);
            if (!chosen.TryGetValue(familyKey, out var existing))
            {
                chosen[familyKey] = entry;
                continue;
            }

            // Keep the highest rank currently available in this family.
            // Since caller pre-filters by player level, higher required level implies newer rank.
            if (entry.RequiredLevel > existing.RequiredLevel)
            {
                chosen[familyKey] = entry;
                continue;
            }

            if (entry.RequiredLevel == existing.RequiredLevel && entry.CommandId > existing.CommandId)
            {
                chosen[familyKey] = entry;
            }
        }

        return chosen.Values.ToArray();
    }

    private static string GetActionFamilyKey(string name)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        // Collapse rank suffixes used by FFXIV upgrades: "II", "III", "IV", etc.
        // Example: "Toxikon" and "Toxikon II" map to the same family key.
        var lastSpace = normalized.LastIndexOf(' ');
        if (lastSpace > 0 && lastSpace < normalized.Length - 1)
        {
            var suffix = normalized[(lastSpace + 1)..];
            if (IsRomanNumeralToken(suffix))
            {
                normalized = normalized[..lastSpace];
            }
        }

        return normalized.Trim().ToLowerInvariant();
    }

    private static bool IsRomanNumeralToken(string token)
    {
        if (token.Length == 0 || token.Length > 6)
        {
            return false;
        }

        foreach (var c in token)
        {
            if (c is not ('I' or 'V' or 'X'))
            {
                return false;
            }
        }

        return true;
    }

    private static int ScoreAssignableEntry(HotbarAssignEntry entry)
    {
        var score = 0;
        if (entry.RequiredLevel > 0)
        {
            score += 4;
        }

        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            score += 2;
        }

        if (!string.IsNullOrWhiteSpace(entry.JobAbbrev))
        {
            score += 1;
        }

        return score;
    }

    private static bool IsActionCategoryAllowedForActionsTab(
        Lumina.Excel.Sheets.Action row,
        ExcelSheet<ActionCategory>? actionCategorySheet)
    {
        var categoryId = row.ActionCategory.RowId;
        if (categoryId == 0)
        {
            return false;
        }

        // Stable fallback ids for standard combat actions in the Action sheet.
        // (Spell, Weaponskill, Ability)
        if (categoryId is 2 or 3 or 4)
        {
            return true;
        }

        if (actionCategorySheet is null || !actionCategorySheet.TryGetRow(categoryId, out var category))
        {
            return false;
        }

        var categoryName = SanitizeSheetText(category.Name.ToString());
        return categoryName.Contains("Spell", StringComparison.OrdinalIgnoreCase) ||
               categoryName.Contains("Weaponskill", StringComparison.OrdinalIgnoreCase) ||
               categoryName.Contains("Ability", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPvpAction(
        Lumina.Excel.Sheets.Action row,
        ExcelSheet<ActionCategory>? actionCategorySheet)
    {
        // Prefer explicit flags when available in this Lumina schema.
        if (ReadBooleanProperty(row, "IsPvP") ||
            ReadBooleanProperty(row, "PvP") ||
            ReadBooleanProperty(row, "Pvp"))
        {
            return true;
        }

        var categoryId = row.ActionCategory.RowId;
        if (actionCategorySheet is not null && actionCategorySheet.TryGetRow(categoryId, out var category))
        {
            var categoryName = SanitizeSheetText(category.Name.ToString());
            if (categoryName.Contains("PvP", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLikelyAssignablePlayerAction(Lumina.Excel.Sheets.Action row)
    {
        // Exclude rows that explicitly mark as non-player/non-assignable in some schema variants.
        if (ReadBooleanProperty(row, "IsPlayerAction") == false &&
            HasProperty(row, "IsPlayerAction"))
        {
            return false;
        }

        if (ReadBooleanProperty(row, "CanBePutOnHotbar") == false &&
            HasProperty(row, "CanBePutOnHotbar"))
        {
            return false;
        }

        if (ReadBooleanProperty(row, "IsUnassignable") &&
            HasProperty(row, "IsUnassignable"))
        {
            return false;
        }

        // Generic duplicated placeholder actions should not appear in the Actions picker.
        var name = SanitizeSheetText(row.Name.ToString());
        if (name.Equals("Attack", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool HasProperty<T>(T row, string propertyName)
    {
        return typeof(T).GetProperty(propertyName) is not null;
    }

    private static bool ReadBooleanProperty<T>(T row, string propertyName)
    {
        var prop = typeof(T).GetProperty(propertyName);
        if (prop is null)
        {
            return false;
        }

        var value = prop.GetValue(row);
        return value switch
        {
            bool b => b,
            byte bt => bt != 0,
            sbyte sb => sb != 0,
            short s => s != 0,
            ushort us => us != 0,
            int i => i != 0,
            uint ui => ui != 0,
            _ => false,
        };
    }

    private string GetActionJobAbbreviation(Lumina.Excel.Sheets.Action row)
    {
        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (classJobSheet is null || row.ClassJob.RowId == 0)
        {
            return string.Empty;
        }

        return classJobSheet.TryGetRow(row.ClassJob.RowId, out var classJob)
            ? classJob.Abbreviation.ToString()
            : string.Empty;
    }

    private string GetActionAffinitySummary(Lumina.Excel.Sheets.Action row)
    {
        var classJobCategoryId = row.ClassJobCategory.RowId;
        if (classJobCategoryId == 0)
        {
            var direct = this.GetActionJobAbbreviation(row);
            return string.IsNullOrWhiteSpace(direct) ? string.Empty : direct;
        }

        var classJobCategorySheet = this.dataManager.GetExcelSheet<ClassJobCategory>();
        var classJobSheet = this.dataManager.GetExcelSheet<ClassJob>();
        if (classJobCategorySheet is null || classJobSheet is null || !classJobCategorySheet.TryGetRow(classJobCategoryId, out var category))
        {
            var direct = this.GetActionJobAbbreviation(row);
            return string.IsNullOrWhiteSpace(direct) ? string.Empty : direct;
        }

        var categoryType = category.GetType();
        var allowedByAbbrev = new List<string>(8);
        foreach (var job in classJobSheet)
        {
            var abbrev = SanitizeSheetText(job.Abbreviation.ToString()).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(abbrev))
            {
                continue;
            }

            var prop = categoryType.GetProperty(abbrev, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop is null)
            {
                continue;
            }

            var value = prop.GetValue(category);
            var isAllowed = value switch
            {
                bool b => b,
                byte bt => bt != 0,
                sbyte sb => sb != 0,
                short s => s != 0,
                ushort us => us != 0,
                int i => i != 0,
                uint ui => ui != 0,
                _ => false,
            };

            if (isAllowed && !allowedByAbbrev.Contains(abbrev, StringComparer.OrdinalIgnoreCase))
            {
                allowedByAbbrev.Add(abbrev);
            }
        }

        if (allowedByAbbrev.Count == 0)
        {
            var direct = this.GetActionJobAbbreviation(row);
            return string.IsNullOrWhiteSpace(direct) ? string.Empty : direct;
        }

        return string.Join(" ", allowedByAbbrev);
    }

    private string GetActionDescription(uint actionId)
    {
        var sheet = this.actionTransientSheet;
        if (sheet is null)
        {
            return string.Empty;
        }

        if (!sheet.TryGetRow(actionId, out var row))
        {
            return string.Empty;
        }

        return row.Description.ToString();
    }

    private string GetDescriptionForCommandType(
        RaptureHotbarModule.HotbarSlotType commandType,
        uint commandId,
        uint apparentActionId)
    {
        if (commandType == RaptureHotbarModule.HotbarSlotType.GeneralAction)
        {
            var description = this.GetSheetDescription<GeneralAction>(commandId);
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }
        else if (commandType == RaptureHotbarModule.HotbarSlotType.MainCommand)
        {
            var description = this.GetSheetDescription<MainCommand>(commandId);
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }
        else if (commandType == RaptureHotbarModule.HotbarSlotType.ExtraCommand)
        {
            var description = this.GetSheetDescription<ExtraCommand>(commandId);
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }
        else if (commandType == RaptureHotbarModule.HotbarSlotType.BuddyAction)
        {
            var description = this.GetSheetDescription<BuddyAction>(commandId);
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }

        return this.GetActionDescription(apparentActionId);
    }

    private string GetSheetDescription<TSheet>(uint rowId) where TSheet : struct, IExcelRow<TSheet>
    {
        if (rowId == 0)
        {
            return string.Empty;
        }

        var sheet = this.dataManager.GetExcelSheet<TSheet>();
        if (sheet is null || !sheet.TryGetRow(rowId, out var row))
        {
            return string.Empty;
        }

        var rowType = typeof(TSheet);
        var descriptionProp = rowType.GetProperty("Description");
        if (descriptionProp is null)
        {
            return string.Empty;
        }

        var value = descriptionProp.GetValue(row);
        return value?.ToString() ?? string.Empty;
    }

    private static bool IsPlaceholderPopupHelp(string popupHelp, string label)
    {
        if (string.IsNullOrWhiteSpace(popupHelp))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return false;
        }

        var trimmedPopup = popupHelp.Trim();
        var trimmedLabel = label.Trim();
        if (trimmedPopup.Equals(trimmedLabel, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Many special command slots expose popup text like "Limit Break [=]" instead of real description.
        return trimmedPopup.StartsWith(trimmedLabel, StringComparison.OrdinalIgnoreCase) &&
               trimmedPopup.Contains('[', StringComparison.Ordinal);
    }

    private ISharedImmediateTexture? GetActionIcon(uint iconId)
    {
        if (iconId == 0)
        {
            return null;
        }

        return this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId));
    }

    private ISharedImmediateTexture? GetStatusIcon(uint iconId)
    {
        if (iconId == 0)
        {
            return null;
        }

        return this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId));
    }
}
