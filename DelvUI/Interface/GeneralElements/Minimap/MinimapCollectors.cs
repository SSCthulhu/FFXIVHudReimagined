using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using DelvUI.Config;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using DelvUI.Helpers;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapPartyBlips
    {
        private static readonly PluginConfigColor FallbackBlipColor = new PluginConfigColor(new Vector4(102f / 255f, 212f / 255f, 255f / 255f, 1f));

        public static int TryCollect(
            ICharacter player,
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float blipRadius,
            List<MinimapBlip> blips)
        {
            var playerPosition = player.Position;
            var playerEntityId = player.EntityId;
            var collected = 0;

            for (var i = 0; i < Plugin.PartyList.Length; i++)
            {
                var member = Plugin.PartyList[i];
                if (member is null)
                {
                    continue;
                }

                if (member.EntityId == 0 || member.EntityId == playerEntityId)
                {
                    continue;
                }

                if (!MinimapMarkerPlacement.TryGetMarkerScreenOffset(
                        member.Position.X,
                        member.Position.Z,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        out var screenOffset))
                {
                    continue;
                }

                var memberColor = member.GameObject is not null
                    ? ColorUtils.ColorForCharacter(member.GameObject, useRoleColor: true) ?? FallbackBlipColor
                    : FallbackBlipColor;
                var color = memberColor.Base;

                blips.Add(new MinimapBlip
                {
                    ScreenOffset = screenOffset,
                    Kind = MinimapBlipKind.Party,
                    Color = color,
                    Radius = blipRadius
                });
                collected++;
            }

            return collected;
        }
    }

    internal static class MinimapEnemyBlips
    {
        private const uint EnemyBlipColor = 0xFF2020FF;

        public static int TryCollect(
            ICharacter player,
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float blipRadius,
            List<MinimapBlip> blips)
        {
            var collected = 0;
            var playerPosition = player.Position;
            var playerObjectId = player.GameObjectId;
            var engagedTargetIds = new HashSet<ulong> { playerObjectId };

            for (var i = 0; i < Plugin.PartyList.Length; i++)
            {
                var member = Plugin.PartyList[i];
                if (member is null)
                {
                    continue;
                }

                if (member.GameObject is not null)
                {
                    engagedTargetIds.Add(member.GameObject.GameObjectId);
                }
            }

            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj.ObjectKind != ObjectKind.BattleNpc || obj is not ICharacter enemy)
                {
                    continue;
                }

                if (enemy.GameObjectId == playerObjectId || enemy.CurrentHp == 0 || enemy.MaxHp == 0)
                {
                    continue;
                }

                var hostile = (enemy.StatusFlags & StatusFlags.Hostile) != 0;
                var inCombat = (enemy.StatusFlags & StatusFlags.InCombat) != 0;
                if (!hostile || !inCombat || !engagedTargetIds.Contains(enemy.TargetObjectId))
                {
                    continue;
                }

                if (!MinimapMarkerPlacement.TryGetMarkerScreenOffset(
                        enemy.Position.X,
                        enemy.Position.Z,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        out var screenOffset))
                {
                    continue;
                }

                blips.Add(new MinimapBlip
                {
                    ScreenOffset = screenOffset,
                    Kind = MinimapBlipKind.Enemy,
                    Color = EnemyBlipColor,
                    Radius = blipRadius
                });
                collected++;
            }

            return collected;
        }
    }

    internal static class MinimapFlagMarkers
    {
        private const uint DefaultFlagIconId = 0xEC91;

        public static unsafe int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var agentMap = AgentMap.Instance();
            if (agentMap is null || agentMap->CurrentMapId == 0 || agentMap->FlagMarkerCount == 0)
            {
                return 0;
            }

            ref readonly var flag = ref agentMap->FlagMapMarkers[0];
            if (flag.MapId != agentMap->CurrentMapId)
            {
                return 0;
            }

            var iconId = flag.MapMarker.IconId == 0 ? DefaultFlagIconId : flag.MapMarker.IconId;
            return MinimapMarkerPlacement.TryAddIconMarker(
                flag.XFloat,
                flag.YFloat,
                iconId,
                playerPosition,
                offsetX,
                offsetY,
                sizeFactor,
                visibleRangeYalms,
                contentHalf,
                mapUvMin,
                mapUvMax,
                markerIconSize,
                iconCache,
                markers) ? 1 : 0;
        }
    }

    internal static class MinimapTempMapMarkers
    {
        public static unsafe int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var agentMap = AgentMap.Instance();
            if (agentMap is null || agentMap->CurrentMapId == 0 || agentMap->TempMapMarkerCount == 0)
            {
                return 0;
            }

            var markerCount = Math.Min(agentMap->TempMapMarkerCount, agentMap->TempMapMarkers.Length);
            var collected = 0;
            for (var i = 0; i < markerCount && markers.Count < maxMarkers; i++)
            {
                ref readonly var entry = ref agentMap->TempMapMarkers[i];
                if (MinimapMarkerPlacement.TryAddIconMarker(
                        entry.MapMarker.X / 16f,
                        entry.MapMarker.Y / 16f,
                        entry.MapMarker.IconId,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        markerIconSize,
                        iconCache,
                        markers))
                {
                    collected++;
                }
            }

            return collected;
        }
    }

    internal static class MinimapGatheringMarkers
    {
        public static unsafe int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var agentMap = AgentMap.Instance();
            if (agentMap is null || agentMap->CurrentMapId == 0)
            {
                return 0;
            }

            var collected = 0;
            foreach (ref readonly var entry in agentMap->MiniMapGatheringMarkers)
            {
                if (markers.Count >= maxMarkers)
                {
                    break;
                }

                if (MinimapMarkerPlacement.TryAddIconMarker(
                        entry.MapMarker.X / 16f,
                        entry.MapMarker.Y / 16f,
                        entry.MapMarker.IconId,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        markerIconSize,
                        iconCache,
                        markers))
                {
                    collected++;
                }
            }

            return collected;
        }
    }

    internal static class MinimapGatheringPointMarkers
    {
        private const int MaxGatheringPointsPerFrame = 24;
        private const float PositionDedupeGridYalms = 3f;

        public static int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var gatheringSheet = Plugin.DataManager.GetExcelSheet<GatheringPoint>();
            if (gatheringSheet is null)
            {
                return 0;
            }

            var seenCells = new HashSet<(int X, int Z)>();
            var maxWorldDistance = visibleRangeYalms + 8f;
            var maxWorldDistanceSq = maxWorldDistance * maxWorldDistance;
            var collected = 0;

            foreach (var obj in Plugin.ObjectTable)
            {
                if (collected >= MaxGatheringPointsPerFrame || markers.Count >= maxMarkers)
                {
                    break;
                }

                if (obj.ObjectKind != ObjectKind.GatheringPoint || !obj.IsTargetable)
                {
                    continue;
                }

                var delta = obj.Position - playerPosition;
                delta.Y = 0f;
                if (delta.LengthSquared() > maxWorldDistanceSq)
                {
                    continue;
                }

                var cellX = (int)MathF.Round(obj.Position.X / PositionDedupeGridYalms);
                var cellZ = (int)MathF.Round(obj.Position.Z / PositionDedupeGridYalms);
                if (!seenCells.Add((cellX, cellZ)))
                {
                    continue;
                }

                var pointRow = gatheringSheet.GetRow(obj.BaseId);
                var iconId = (uint)(pointRow.GatheringPointBase.ValueNullable?.GatheringType.ValueNullable?.IconMain ?? 0);
                if (iconId == 0)
                {
                    continue;
                }

                if (MinimapMarkerPlacement.TryAddIconMarker(
                        obj.Position.X,
                        obj.Position.Z,
                        iconId,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        markerIconSize,
                        iconCache,
                        markers))
                {
                    collected++;
                }
            }

            return collected;
        }
    }

    internal static class MinimapFateMarkers
    {
        private const int MaxFatesToScan = 32;
        private const int MaxFateAreasPerFrame = 8;
        private const float PixelsPerYalmScale = 0.86f;
        private const float MinAreaRadiusPixels = 6f;
        private const float MaxAreaRadiusPixelsScale = 1.15f;

        public static unsafe int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            List<MinimapFateArea> fateAreas,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var fateManager = FateManager.Instance();
            if (fateManager is null)
            {
                return 0;
            }

            ref var fates = ref fateManager->Fates;
            var count = (int)Math.Min(fates.LongCount, MaxFatesToScan);
            if (count <= 0 || fates.First == null)
            {
                return 0;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var pixelsPerYalm = (contentHalf * PixelsPerYalmScale) / Math.Max(visibleRangeYalms, 1f);
            var maxAreaRadiusPixels = contentHalf * MaxAreaRadiusPixelsScale;
            var collected = 0;

            for (var i = 0; i < count && markers.Count < maxMarkers; i++)
            {
                var fate = fates.First[i].Value;
                if (fate is null || !IsFateActive(fate, now))
                {
                    continue;
                }

                var worldX = fate->Location.X;
                var worldZ = fate->Location.Z;
                var worldRadius = fate->Radius;
                if (!float.IsFinite(worldX) || !float.IsFinite(worldZ) || !float.IsFinite(worldRadius))
                {
                    continue;
                }

                if (!MinimapMarkerPlacement.TryGetMarkerScreenOffset(
                        worldX,
                        worldZ,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        out var screenOffset))
                {
                    continue;
                }

                if (fateAreas.Count < MaxFateAreasPerFrame && worldRadius > 0.5f)
                {
                    var radiusPixels = Math.Clamp(worldRadius * pixelsPerYalm, MinAreaRadiusPixels, maxAreaRadiusPixels);
                    fateAreas.Add(new MinimapFateArea { ScreenOffset = screenOffset, RadiusPixels = radiusPixels });
                }

                var iconId = fate->MapIconId != 0 ? fate->MapIconId : fate->IconId;
                if (MinimapMarkerPlacement.TryAddIconMarker(
                        worldX,
                        worldZ,
                        iconId,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        markerIconSize,
                        iconCache,
                        markers))
                {
                    collected++;
                }
            }

            return collected;
        }

        private static unsafe bool IsFateActive(FateContext* fate, long nowUnix)
        {
            if (fate->State is FateState.Ended or FateState.Failed)
            {
                return false;
            }

            var start = fate->StartTimeEpoch;
            var duration = fate->Duration;
            if (start <= 0 || duration <= 0)
            {
                return fate->State is FateState.Running or FateState.Preparing or FateState.Ending;
            }

            var end = start + duration;
            return start <= nowUnix && nowUnix <= end;
        }
    }

    internal static class MinimapEventMarkers
    {
        private const int MaxEventMarkersToScan = 96;
        private const uint PlayerMarkerIconId = 60443;

        public static unsafe int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var agentMap = AgentMap.Instance();
            if (agentMap is null || agentMap->CurrentMapId == 0)
            {
                return 0;
            }

            ref var eventMarkers = ref agentMap->EventMarkers;
            var count = (int)Math.Min(eventMarkers.LongCount, MaxEventMarkersToScan);
            if (count <= 0 || eventMarkers.First == null)
            {
                return 0;
            }

            var currentMapId = agentMap->CurrentMapId;
            var currentTerritoryId = (ushort)agentMap->CurrentTerritoryId;
            var seen = new HashSet<(uint IconId, int X, int Z)>();
            var collected = 0;

            for (var i = 0; i < count && markers.Count < maxMarkers; i++)
            {
                var marker = eventMarkers.First[i];
                if (marker.IconId == 0 || marker.IconId == PlayerMarkerIconId)
                {
                    continue;
                }

                if (marker.MapId != 0 && marker.MapId != currentMapId)
                {
                    continue;
                }

                if (marker.TerritoryTypeId != 0 && marker.TerritoryTypeId != currentTerritoryId)
                {
                    continue;
                }

                var worldX = marker.Position.X;
                var worldZ = marker.Position.Z;
                if (!float.IsFinite(worldX) || !float.IsFinite(worldZ))
                {
                    continue;
                }

                if (!seen.Add((marker.IconId, (int)MathF.Round(worldX), (int)MathF.Round(worldZ))))
                {
                    continue;
                }

                if (MinimapMarkerPlacement.TryAddIconMarker(
                        worldX,
                        worldZ,
                        marker.IconId,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        markerIconSize,
                        iconCache,
                        markers))
                {
                    collected++;
                }
            }

            return collected;
        }
    }

    internal static class MinimapNaviMapMarkers
    {
        private const uint PlayerMarkerIconId = 60443;

        public static unsafe int TryCollect(
            float contentHalf,
            Vector2 mapUvMin,
            Vector2 mapUvMax,
            Vector3 playerPosition,
            int offsetX,
            int offsetY,
            uint sizeFactor,
            float visibleRangeYalms,
            float markerIconSize,
            MinimapMarkerIconCache iconCache,
            List<MinimapIconMarker> markers,
            int maxMarkers)
        {
            if (maxMarkers <= 0 || markers.Count >= maxMarkers)
            {
                return 0;
            }

            var agentMap = AgentMap.Instance();
            if (agentMap is null || agentMap->CurrentMapId == 0)
            {
                return 0;
            }

            var markerCount = Math.Min(agentMap->MiniMapMarkerCount, agentMap->MiniMapMarkers.Length);
            var collected = 0;
            for (var i = 0; i < markerCount && markers.Count < maxMarkers; i++)
            {
                ref readonly var entry = ref agentMap->MiniMapMarkers[i];
                var iconId = entry.MapMarker.IconId;
                if (iconId == 0 || iconId == PlayerMarkerIconId)
                {
                    continue;
                }

                if (MinimapMarkerPlacement.TryAddIconMarker(
                        entry.MapMarker.X / 16f,
                        entry.MapMarker.Y / 16f,
                        iconId,
                        playerPosition,
                        offsetX,
                        offsetY,
                        sizeFactor,
                        visibleRangeYalms,
                        contentHalf,
                        mapUvMin,
                        mapUvMax,
                        markerIconSize,
                        iconCache,
                        markers))
                {
                    collected++;
                }
            }

            return collected;
        }
    }
}
