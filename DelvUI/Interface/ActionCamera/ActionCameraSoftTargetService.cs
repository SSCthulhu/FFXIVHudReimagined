using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;

namespace DelvUI.Interface.ActionCamera
{
    internal sealed class ActionCameraSoftTargetService
    {
        private const int AcquireStableFrames = 6;
        private const int ClearGraceFrames = 90;

        private readonly ActionCameraConfig _config;
        private ActionCameraSoftTargetCandidate _candidate;
        private uint _lastAssignedObjectId;
        private uint _pendingObjectId;
        private int _pendingStableFrames;
        private int _noCandidateGraceFrames;
        private uint _lockedObjectId;

        public ActionCameraSoftTargetService(ActionCameraConfig config)
        {
            _config = config;
        }

        public ActionCameraSoftTargetCandidate Candidate => _candidate;
        public bool HasCandidate => _candidate.HasCandidate;
        public bool HasLockedTarget => _lockedObjectId != 0;
        public uint LockedObjectId => _lockedObjectId;

        public void Update(bool allowTargeting = true)
        {
            if (!allowTargeting || !_config.EnableSoftTargetSuggestion)
            {
                ClearAll();
                return;
            }

            if (_lockedObjectId != 0)
            {
                if (TryApplyLockedTarget())
                {
                    return;
                }

                // Locked target no longer valid (dead/despawned/etc).
                _lockedObjectId = 0;
            }

            if (!TryGetScreenCenter(out var screenCenter))
            {
                _candidate = default;
                ClearSoftTargetWhenNoCandidate();
                return;
            }

            if (!TryGetCameraConeData(out var cameraPos, out var cameraForward))
            {
                _candidate = default;
                ClearSoftTargetWhenNoCandidate();
                return;
            }

            float bestScore = float.MaxValue;
            uint bestObjectId = 0;
            Vector2 bestScreenPos = Vector2.Zero;

            const float maxDepth = 120f;
            const float minForwardDot = 0.28f;

            for (int i = 0; i < Plugin.ObjectTable.Length; i++)
            {
                var obj = Plugin.ObjectTable[i];
                if (obj == null)
                {
                    continue;
                }

                if (!TryGetEnemyCandidateCharacter(obj, out var character))
                {
                    continue;
                }

                var worldPos = obj.Position + new Vector3(0f, 1.2f, 0f);
                var toTarget = worldPos - cameraPos;
                float distance = toTarget.Length();
                if (distance < 0.01f || distance > maxDepth)
                {
                    continue;
                }

                var toTargetDir = toTarget / distance;
                float forwardDot = Vector3.Dot(cameraForward, toTargetDir);
                if (forwardDot < minForwardDot)
                {
                    continue;
                }

                if (!TryWorldToScreen(worldPos, out var screenPos))
                {
                    continue;
                }

                var delta = screenPos - screenCenter;
                float adjustedDeltaY = delta.Y < 0f ? delta.Y * 0.42f : delta.Y * 0.90f;
                float score = (delta.X * delta.X) + (adjustedDeltaY * adjustedDeltaY);
                float radius = _config.SoftTargetScreenRadius;
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

            _candidate = bestObjectId == 0
                ? default
                : new ActionCameraSoftTargetCandidate(true, bestObjectId, bestScreenPos, bestScore);

            if (_config.AutoTarget && _candidate.HasCandidate)
            {
                _noCandidateGraceFrames = 0;
                if (_pendingObjectId != _candidate.ObjectId)
                {
                    _pendingObjectId = _candidate.ObjectId;
                    _pendingStableFrames = 1;
                    Plugin.TargetManager.SoftTarget = null;
                    return;
                }

                _pendingStableFrames++;
                if (_pendingStableFrames < AcquireStableFrames)
                {
                    return;
                }

                if (TryApplySoftTargetByObjectId(_candidate.ObjectId))
                {
                    _lastAssignedObjectId = _candidate.ObjectId;
                }
            }
            else if (!_candidate.HasCandidate)
            {
                if (_config.AutoTarget && TryBuildStickyAssignedCandidate(out var stickyCandidate))
                {
                    _noCandidateGraceFrames++;
                    if (_noCandidateGraceFrames < ClearGraceFrames)
                    {
                        _candidate = stickyCandidate;
                        return;
                    }
                }

                ClearSoftTargetWhenNoCandidate();
                _pendingObjectId = 0;
                _pendingStableFrames = 0;
                _noCandidateGraceFrames = 0;
                _lastAssignedObjectId = 0;
            }
        }

        public void ClearAll()
        {
            _candidate = default;
            if (Plugin.TargetManager.SoftTarget != null)
            {
                Plugin.TargetManager.SoftTarget = null;
            }

            _pendingObjectId = 0;
            _pendingStableFrames = 0;
            _noCandidateGraceFrames = 0;
            _lastAssignedObjectId = 0;
            _lockedObjectId = 0;
        }

        public void ToggleLockedTarget()
        {
            if (_lockedObjectId != 0)
            {
                _lockedObjectId = 0;
                return;
            }

            // Prefer current soft target candidate, then hard target.
            uint candidateId = _candidate.HasCandidate
                ? _candidate.ObjectId
                : (uint)(Plugin.TargetManager.Target?.GameObjectId ?? 0);

            if (candidateId == 0)
            {
                return;
            }

            _lockedObjectId = candidateId;
            _pendingObjectId = candidateId;
            _pendingStableFrames = AcquireStableFrames;
        }

        private bool TryApplyLockedTarget()
        {
            for (int i = 0; i < Plugin.ObjectTable.Length; i++)
            {
                var obj = Plugin.ObjectTable[i];
                if (obj == null || (uint)obj.GameObjectId != _lockedObjectId)
                {
                    continue;
                }

                if (!TryGetEnemyCandidateCharacter(obj, out _))
                {
                    return false;
                }

                var worldPos = obj.Position + new Vector3(0f, 1.2f, 0f);
                if (!TryWorldToScreen(worldPos, out var screenPos))
                {
                    return false;
                }

                _candidate = new ActionCameraSoftTargetCandidate(true, _lockedObjectId, screenPos, 0f);
                if (_config.AutoTarget)
                {
                    TryApplySoftTargetByObjectId(_lockedObjectId);
                }

                return true;
            }

            return false;
        }

        private bool TryApplySoftTargetByObjectId(uint objectId)
        {
            for (int i = 0; i < Plugin.ObjectTable.Length; i++)
            {
                var obj = Plugin.ObjectTable[i];
                if (obj == null)
                {
                    continue;
                }

                if ((uint)obj.GameObjectId != objectId)
                {
                    continue;
                }

                if (!ReferenceEquals(Plugin.TargetManager.SoftTarget, obj))
                {
                    Plugin.TargetManager.SoftTarget = obj;
                }

                // Keep native UI target indicators in sync with action-camera targeting.
                // Without this, actions can route to SoftTarget while nameplate arrows/highlights
                // continue to reflect a stale hard target.
                if (!ReferenceEquals(Plugin.TargetManager.Target, obj))
                {
                    Plugin.TargetManager.Target = obj;
                }

                return true;
            }

            return false;
        }

        private bool TryBuildStickyAssignedCandidate(out ActionCameraSoftTargetCandidate stickyCandidate)
        {
            stickyCandidate = default;
            if (_lastAssignedObjectId == 0)
            {
                return false;
            }

            for (int i = 0; i < Plugin.ObjectTable.Length; i++)
            {
                var obj = Plugin.ObjectTable[i];
                if (obj == null || (uint)obj.GameObjectId != _lastAssignedObjectId)
                {
                    continue;
                }

                if (!TryGetEnemyCandidateCharacter(obj, out _))
                {
                    return false;
                }

                var worldPos = obj.Position + new Vector3(0f, 1.2f, 0f);
                if (!TryWorldToScreen(worldPos, out var screenPos))
                {
                    return false;
                }

                stickyCandidate = new ActionCameraSoftTargetCandidate(true, _lastAssignedObjectId, screenPos, _candidate.Score);
                return true;
            }

            return false;
        }

        private static void ClearSoftTargetWhenNoCandidate()
        {
            if (Plugin.TargetManager.SoftTarget == null)
            {
                return;
            }

            Plugin.TargetManager.SoftTarget = null;
        }

        private static bool TryGetEnemyCandidateCharacter(IGameObject obj, out ICharacter character)
        {
            character = default!;

            if (obj.ObjectKind != ObjectKind.BattleNpc || !obj.IsTargetable || obj is not ICharacter typedCharacter)
            {
                return false;
            }

            character = typedCharacter;
            if (character.CurrentHp == 0 || character.MaxHp == 0)
            {
                return false;
            }

            if (character.ClassJob.RowId != 0)
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
}
