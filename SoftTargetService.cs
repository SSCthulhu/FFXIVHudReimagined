using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace FFXIVHudPlugin;

/// <summary>
/// Optional scaffold for future soft target suggestion logic.
/// </summary>
internal sealed class SoftTargetService
{
    private readonly ActionCameraConfiguration config;
    private readonly IObjectTable objectTable;
    private readonly ITargetManager targetManager;
    private SoftTargetCandidate candidate;
    private uint lastAssignedObjectId;
    private uint pendingObjectId;
    private int pendingStableFrames;

    public SoftTargetService(
        ActionCameraConfiguration config,
        IObjectTable objectTable,
        ITargetManager targetManager)
    {
        this.config = config;
        this.objectTable = objectTable;
        this.targetManager = targetManager;
    }

    /// <summary>
    /// True when a soft target candidate exists this frame.
    /// </summary>
    public bool HasCandidate => this.candidate.HasCandidate;
    public SoftTargetCandidate Candidate => this.candidate;

    /// <summary>
    /// Placeholder update path for future center-ray candidate scans.
    /// </summary>
    public void Update()
    {
        if (!this.config.EnableSoftTargetSuggestion)
        {
            this.candidate = default;
            if (this.targetManager.SoftTarget is not null)
            {
                this.targetManager.SoftTarget = null;
            }
            this.pendingObjectId = 0;
            this.pendingStableFrames = 0;
            this.lastAssignedObjectId = 0;
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
        const float maxDepth = 120f;
        const float minForwardDot = 0.28f; // Broad forward cone (~74 deg half-angle)

        for (var i = 0; i < this.objectTable.Length; i++)
        {
            var obj = this.objectTable[i];
            if (obj is null || obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc || !obj.IsTargetable)
            {
                continue;
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

        if (this.config.AutoTarget && this.candidate.HasCandidate)
        {
            if (this.pendingObjectId != this.candidate.ObjectId)
            {
                this.pendingObjectId = this.candidate.ObjectId;
                this.pendingStableFrames = 1;
                return;
            }

            this.pendingStableFrames++;
            if (this.pendingStableFrames < 6)
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
            this.ClearSoftTargetWhenNoCandidate();
            this.pendingObjectId = 0;
            this.pendingStableFrames = 0;
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

    private void ClearSoftTargetWhenNoCandidate()
    {
        if (this.targetManager.SoftTarget is null)
        {
            return;
        }

        this.targetManager.SoftTarget = null;
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
