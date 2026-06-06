using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;

namespace FFXIVHudPlugin;

public enum SoftTargetRejectReason
{
    None = 0,
    NotBattleNpc = 1,
    NotTargetable = 2,
    NotCharacter = 3,
    Dead = 4,
    HasClassJob = 5,
    NotAttackable = 6,
    HasOwner = 7,
    NotEngaged = 8,
}

/// <summary>
/// Optional scaffold for future soft target suggestion logic.
/// </summary>
internal sealed class SoftTargetService
{
    private const int SoftTargetAcquireStableFrames = 6;
    private const int SoftTargetClearGraceFrames = 90;

    private readonly ActionCameraConfiguration config;
    private readonly IObjectTable objectTable;
    private readonly IPartyList partyList;
    private readonly ITargetManager targetManager;
    private SoftTargetCandidate candidate;
    private uint lastAssignedObjectId;
    private uint pendingObjectId;
    private int pendingStableFrames;
    private int noCandidateGraceFrames;
    private int debugScannedCount;
    private int debugEnemyCandidateCount;
    private int debugEngagedCandidateCount;
    private uint debugLastRejectedObjectId;
    private SoftTargetRejectReason debugLastRejectReason;

    public SoftTargetService(
        ActionCameraConfiguration config,
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetManager targetManager)
    {
        this.config = config;
        this.objectTable = objectTable;
        this.partyList = partyList;
        this.targetManager = targetManager;
    }

    /// <summary>
    /// True when a soft target candidate exists this frame.
    /// </summary>
    public bool HasCandidate => this.candidate.HasCandidate;
    public SoftTargetCandidate Candidate => this.candidate;
    public int DebugScannedCount => this.debugScannedCount;
    public int DebugEnemyCandidateCount => this.debugEnemyCandidateCount;
    public int DebugEngagedCandidateCount => this.debugEngagedCandidateCount;
    public uint DebugLastRejectedObjectId => this.debugLastRejectedObjectId;
    public SoftTargetRejectReason DebugLastRejectReason => this.debugLastRejectReason;

    /// <summary>
    /// Placeholder update path for future center-ray candidate scans.
    /// </summary>
    public void Update(bool allowTargeting = true)
    {
        if (!allowTargeting || !this.config.EnableSoftTargetSuggestion)
        {
            this.candidate = default;
            if (this.targetManager.SoftTarget is not null)
            {
                this.targetManager.SoftTarget = null;
            }
            this.pendingObjectId = 0;
            this.pendingStableFrames = 0;
            this.noCandidateGraceFrames = 0;
            this.lastAssignedObjectId = 0;
            this.debugScannedCount = 0;
            this.debugEnemyCandidateCount = 0;
            this.debugEngagedCandidateCount = 0;
            this.debugLastRejectedObjectId = 0;
            this.debugLastRejectReason = SoftTargetRejectReason.None;
            return;
        }

        if (!TryGetScreenCenter(out var screenCenter))
        {
            this.candidate = default;
            this.ClearSoftTargetWhenNoCandidate();
            return;
        }

        if (!TryGetCameraConeData(out var cameraPos, out var cameraForward))
        {
            this.candidate = default;
            this.ClearSoftTargetWhenNoCandidate();
            return;
        }

        var bestScore = float.MaxValue;
        var bestObjectId = 0u;
        var bestScreenPos = Vector2.Zero;
        var scannedCount = 0;
        var enemyCandidateCount = 0;
        var engagedCandidateCount = 0; // Diagnostic-only count; no longer a hard filter.
        var lastRejectedObjectId = 0u;
        var lastRejectReason = SoftTargetRejectReason.None;
        const float maxDepth = 120f;
        const float minForwardDot = 0.28f; // Broad forward cone (~74 deg half-angle)

        for (var i = 0; i < this.objectTable.Length; i++)
        {
            var obj = this.objectTable[i];
            if (obj is null)
            {
                continue;
            }

            scannedCount++;
            if (!this.TryGetEnemyCandidateCharacter(obj, out var character, out var rejectReason))
            {
                lastRejectedObjectId = (uint)obj.GameObjectId;
                lastRejectReason = rejectReason;
                continue;
            }

            enemyCandidateCount++;
            if (IsEngagedWithPlayerGroup(character))
            {
                engagedCandidateCount++;
            }
            var worldPos = obj.Position + new Vector3(0f, 1.2f, 0f);
            var toTarget = worldPos - cameraPos;
            var distance = toTarget.Length();
            if (distance < 0.01f || distance > maxDepth)
            {
                continue;
            }

            var toTargetDir = toTarget / distance;
            var forwardDot = Vector3.Dot(cameraForward, toTargetDir);
            if (forwardDot < minForwardDot)
            {
                continue;
            }

            if (!TryWorldToScreen(worldPos, out var screenPos))
            {
                continue;
            }

            var delta = screenPos - screenCenter;
            // Favor a wider vertical capture in the upper screen region for isometric/high-pitch views.
            var adjustedDeltaY = delta.Y < 0f ? delta.Y * 0.42f : delta.Y * 0.90f;
            var score = (delta.X * delta.X) + (adjustedDeltaY * adjustedDeltaY);
            var radius = this.config.SoftTargetScreenRadius;
            if (score > radius * radius)
            {
                continue;
            }

            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestObjectId = unchecked((uint)obj.GameObjectId);
            bestScreenPos = screenPos;
        }

        this.candidate = bestObjectId == 0
            ? default
            : new SoftTargetCandidate(true, bestObjectId, bestScreenPos, bestScore);
        this.debugScannedCount = scannedCount;
        this.debugEnemyCandidateCount = enemyCandidateCount;
        this.debugEngagedCandidateCount = engagedCandidateCount;
        this.debugLastRejectedObjectId = lastRejectedObjectId;
        this.debugLastRejectReason = lastRejectReason;

        if (this.config.AutoTarget && this.candidate.HasCandidate)
        {
            this.noCandidateGraceFrames = 0;
            if (this.pendingObjectId != this.candidate.ObjectId)
            {
                this.pendingObjectId = this.candidate.ObjectId;
                this.pendingStableFrames = 1;
                return;
            }

            this.pendingStableFrames++;
            if (this.pendingStableFrames < SoftTargetAcquireStableFrames)
            {
                return;
            }

            if (this.TryApplySoftTargetByObjectId(this.candidate.ObjectId))
            {
                this.lastAssignedObjectId = this.candidate.ObjectId;
            }
        }
        else if (!this.candidate.HasCandidate)
        {
            // Keep previous soft target briefly to avoid repeated acquire/clear SFX while aim jitters.
            if (this.config.AutoTarget && this.TryBuildStickyAssignedCandidate(out var stickyCandidate))
            {
                this.noCandidateGraceFrames++;
                if (this.noCandidateGraceFrames < SoftTargetClearGraceFrames)
                {
                    this.candidate = stickyCandidate;
                    return;
                }
            }

            this.ClearSoftTargetWhenNoCandidate();
            this.pendingObjectId = 0;
            this.pendingStableFrames = 0;
            this.noCandidateGraceFrames = 0;
            this.lastAssignedObjectId = 0;
        }
    }

    private bool TryApplySoftTargetByObjectId(uint objectId)
    {
        for (var i = 0; i < this.objectTable.Length; i++)
        {
            var obj = this.objectTable[i];
            if (obj is null)
            {
                continue;
            }

            if ((uint)obj.GameObjectId != objectId)
            {
                continue;
            }

            // Use soft-target slot instead of hard target to avoid frequent target
            // acquire SFX while still steering actions toward what the reticle faces.
            if (!ReferenceEquals(this.targetManager.SoftTarget, obj))
            {
                this.targetManager.SoftTarget = obj;
            }

            return true;
        }

        return false;
    }

    private bool TryBuildStickyAssignedCandidate(out SoftTargetCandidate stickyCandidate)
    {
        stickyCandidate = default;
        if (this.lastAssignedObjectId == 0)
        {
            return false;
        }

        for (var i = 0; i < this.objectTable.Length; i++)
        {
            var obj = this.objectTable[i];
            if (obj is null || (uint)obj.GameObjectId != this.lastAssignedObjectId)
            {
                continue;
            }

            if (!this.TryGetEnemyCandidateCharacter(obj, out _, out _))
            {
                return false;
            }

            var worldPos = obj.Position + new Vector3(0f, 1.2f, 0f);
            if (!TryWorldToScreen(worldPos, out var screenPos))
            {
                return false;
            }

            var score = this.candidate.Score;
            if (!TryGetScreenCenter(out var screenCenter))
            {
                screenCenter = Vector2.Zero;
            }

            if (screenCenter != Vector2.Zero)
            {
                var delta = screenPos - screenCenter;
                score = (delta.X * delta.X) + (delta.Y * delta.Y);
            }

            stickyCandidate = new SoftTargetCandidate(true, this.lastAssignedObjectId, screenPos, score);
            return true;
        }

        return false;
    }

    private void ClearSoftTargetWhenNoCandidate()
    {
        if (this.targetManager.SoftTarget is null)
        {
            return;
        }

        this.targetManager.SoftTarget = null;
    }

    private bool TryGetEnemyCandidateCharacter(
        IGameObject obj,
        out ICharacter character,
        out SoftTargetRejectReason rejectReason)
    {
        character = default!;
        rejectReason = SoftTargetRejectReason.None;

        if (obj.ObjectKind != ObjectKind.BattleNpc)
        {
            rejectReason = SoftTargetRejectReason.NotBattleNpc;
            return false;
        }

        if (!obj.IsTargetable)
        {
            rejectReason = SoftTargetRejectReason.NotAttackable;
            return false;
        }

        if (obj is not ICharacter typedCharacter)
        {
            rejectReason = SoftTargetRejectReason.NotCharacter;
            return false;
        }

        character = typedCharacter;
        if (character.CurrentHp == 0 || character.MaxHp == 0)
        {
            rejectReason = SoftTargetRejectReason.Dead;
            return false;
        }

        // Duty support/trust companions expose class jobs like player characters.
        // Hostile battle enemies normally do not, so exclude non-zero class-job rows.
        if (character.ClassJob.RowId != 0)
        {
            rejectReason = SoftTargetRejectReason.HasClassJob;
            return false;
        }

        if (character.OwnerId != 0 && this.IsOwnedByPlayerGroup(character.OwnerId))
        {
            rejectReason = SoftTargetRejectReason.HasOwner;
            return false;
        }

        return true;
    }

    private bool IsOwnedByPlayerGroup(ulong ownerId)
    {
        if (ownerId == 0)
        {
            return false;
        }

        var playerObjectId = this.objectTable.LocalPlayer?.GameObjectId ?? 0;
        if (ownerId == playerObjectId)
        {
            return true;
        }

        for (var i = 0; i < this.partyList.Length; i++)
        {
            var member = this.partyList[i];
            var memberObjectId = member?.GameObject?.GameObjectId ?? 0;
            if (ownerId == memberObjectId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEngagedWithPlayerGroup(ICharacter enemy)
    {
        if ((enemy.StatusFlags & StatusFlags.InCombat) == 0)
        {
            return false;
        }

        return true;
    }

    private static bool TryGetScreenCenter(out Vector2 center)
    {
        var viewport = ImGui.GetMainViewport();
        if (viewport.Size.X <= 0f || viewport.Size.Y <= 0f)
        {
            center = default;
            return false;
        }

        center = viewport.Pos + (viewport.Size * 0.5f);
        return true;
    }

    private static unsafe bool TryWorldToScreen(Vector3 world, out Vector2 screen)
    {
        screen = default;
        var sceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
        if (sceneCameraManager is null || sceneCameraManager->CurrentCamera is null)
        {
            return false;
        }

        var worldCopy = world;
        if (!sceneCameraManager->CurrentCamera->WorldToScreen(worldCopy, out var screenNative))
        {
            return false;
        }

        screen = new Vector2(screenNative.X, screenNative.Y);
        return true;
    }

    private static unsafe bool TryGetCameraConeData(out Vector3 cameraPosition, out Vector3 cameraForward)
    {
        cameraPosition = default;
        cameraForward = default;

        var sceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
        if (sceneCameraManager is null || sceneCameraManager->CurrentCamera is null)
        {
            return false;
        }

        var camera = sceneCameraManager->CurrentCamera;
        cameraPosition = new Vector3(camera->Position.X, camera->Position.Y, camera->Position.Z);
        var lookAt = new Vector3(camera->LookAtVector.X, camera->LookAtVector.Y, camera->LookAtVector.Z);
        var forward = lookAt - cameraPosition;
        var len = forward.Length();
        if (len < 0.0001f)
        {
            return false;
        }

        cameraForward = forward / len;
        return true;
    }
}
