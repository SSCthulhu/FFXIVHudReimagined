using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    internal sealed class MinimapStateProvider
    {
        private const long EventMarkerRefreshIntervalMs = 750;

        private readonly MinimapConfig _configuration;
        private readonly MinimapMapTextureCache _mapTextureCache = new();
        private readonly MinimapPlayerIndicatorCache _playerIndicatorCache = new();
        private readonly MinimapMarkerIconCache _markerIconCache = new();
        private long _lastEventMarkerRefreshMs;

        public MinimapDiagnosticReport LatestDiagnostics { get; private set; } = new();

        public MinimapStateProvider(MinimapConfig configuration)
        {
            _configuration = configuration;
        }

        public MinimapSnapshot Build()
        {
            var player = Plugin.ObjectTable.LocalPlayer;
            if (player is null)
            {
                return MinimapSnapshot.Empty;
            }

            var range = MinimapLayout.ClampVisibleRange(_configuration.VisibleRangeYalms);
            var minimapSize = MinimapLayout.ClampSize(_configuration.Size);
            var markerIconSize = MinimapLayout.ClampMarkerIconSize(_configuration.MarkerIconSize);
            var blips = new List<MinimapBlip>(8);
            var iconMarkers = new List<MinimapIconMarker>(MinimapLayout.MaxNativeMarkersPerFrame);
            var fateAreas = new List<MinimapFateArea>(8);

            var hasMapTexture = _mapTextureCache.TryGetCurrentMapTexture(out var mapTexture) && MinimapTextureUtil.IsDrawable(mapTexture);
            var mapUvMin = Vector2.Zero;
            var mapUvMax = Vector2.One;
            var hasMapTransform = TryResolveMapTransform(out var offsetX, out var offsetY, out var sizeFactor);
            TryRefreshEventMarkers();

            if (hasMapTexture && hasMapTransform && !TryGetVisibleMapUvWindow(player, range, out mapUvMin, out mapUvMax))
            {
                mapUvMin = new Vector2(0.45f, 0.45f);
                mapUvMax = new Vector2(0.55f, 0.55f);
            }

            if (hasMapTransform)
            {
                var contentHalf = minimapSize * 0.5f;
                var partyBlipRadius = MinimapLayout.ClampPlayerPinSize(_configuration.PlayerPinSize);
                MinimapPartyBlips.TryCollect(player, contentHalf, mapUvMin, mapUvMax, offsetX, offsetY, sizeFactor, range, partyBlipRadius, blips);
                MinimapEnemyBlips.TryCollect(player, contentHalf, mapUvMin, mapUvMax, offsetX, offsetY, sizeFactor, range, partyBlipRadius, blips);
            }

            if (_configuration.ShowNativeMarkers && hasMapTransform)
            {
                _markerIconCache.BeginFrame();
                var contentHalf = minimapSize * 0.5f;
                var markerLimit = iconMarkers.Count + MinimapLayout.MaxNativeMarkersPerFrame;

                MinimapFlagMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, markerLimit);
                MinimapTempMapMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, markerLimit);
                MinimapGatheringPointMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, markerLimit);
                MinimapGatheringMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, markerLimit);
                MinimapFateMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, fateAreas, markerLimit);
                MinimapEventMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, markerLimit);
                MinimapNaviMapMarkers.TryCollect(contentHalf, mapUvMin, mapUvMax, player.Position, offsetX, offsetY, sizeFactor, range, markerIconSize, _markerIconCache, iconMarkers, markerLimit);
            }

            var hasNativeMapFrame = MinimapNativeFrame.TryGetMapImageTransform(out var nativeFrame);
            var hasCameraMapYaw = MinimapCameraHeading.TryGetMapYaw(out var cameraMapYaw);
            var playerIndicator = _playerIndicatorCache.GetAssets();
            var classJobId = player.ClassJob.RowId;

            var snapshot = new MinimapSnapshot
            {
                IsActive = true,
                PlayerYaw = player.Rotation,
                CameraMapYaw = cameraMapYaw,
                HasCameraMapYaw = hasCameraMapYaw,
                MapTitle = GetMapTitle(),
                Blips = blips,
                IconMarkers = iconMarkers,
                FateAreas = fateAreas,
                MapTexture = mapTexture,
                MapUvMin = mapUvMin,
                MapUvMax = mapUvMax,
                HasMapTexture = hasMapTexture,
                PlayerIndicator = playerIndicator,
                HasNativeMapFrame = hasNativeMapFrame,
                NativeMapImageRotation = nativeFrame.Rotation,
                NativeMapImageScaleX = nativeFrame.ScaleX,
                NativeMapImageScaleY = nativeFrame.ScaleY,
                NativeNorthLockedUp = nativeFrame.NorthLockedUp,
                NativePlayerConeRotation = nativeFrame.PlayerConeRotation,
                VisibleRangeYalms = range,
                PlayerClassJobId = classJobId,
                PlayerPinFillColor = MinimapPlayerPinColor.Resolve(_configuration, classJobId)
            };

            if (_configuration.ShowDiagnostics)
            {
                LatestDiagnostics = new MinimapDiagnosticReport
                {
                    Text = $"Map: {snapshot.MapTitle}\nMarkers: {snapshot.IconMarkers.Count}\nBlips: {snapshot.Blips.Count}\nTexture: {_mapTextureCache.LastLoadedPath}"
                };
            }

            return snapshot;
        }

        private bool TryGetVisibleMapUvWindow(ICharacter player, float visibleRangeYalms, out Vector2 uvMin, out Vector2 uvMax)
        {
            uvMin = Vector2.Zero;
            uvMax = Vector2.One;
            if (!TryResolveMapTransform(out var offsetX, out var offsetY, out var sizeFactor))
            {
                return false;
            }

            var mapTextureCoords = MinimapMapMath.WorldToMapTextureCoords(player.Position, offsetX, offsetY, sizeFactor);
            return MinimapMapMath.TryGetVisibleMapUvWindow(mapTextureCoords, visibleRangeYalms, sizeFactor, out uvMin, out uvMax);
        }

        private unsafe bool TryResolveMapTransform(out int offsetX, out int offsetY, out uint sizeFactor)
        {
            offsetX = 0;
            offsetY = 0;
            sizeFactor = 100;

            var agentMap = AgentMap.Instance();
            if (agentMap is not null && agentMap->CurrentMapId != 0)
            {
                offsetX = -agentMap->CurrentOffsetX;
                offsetY = -agentMap->CurrentOffsetY;
                sizeFactor = (uint)Math.Max(agentMap->CurrentMapSizeFactor, (short)1);
                return true;
            }

            var mapId = ResolveMapId();
            if (mapId == 0)
            {
                return false;
            }

            var sheet = Plugin.DataManager.GetExcelSheet<Map>();
            if (sheet is null || !sheet.TryGetRow(mapId, out var mapRow))
            {
                return false;
            }

            offsetX = mapRow.OffsetX;
            offsetY = mapRow.OffsetY;
            sizeFactor = mapRow.SizeFactor;
            return true;
        }

        private unsafe uint ResolveMapId()
        {
            var agentMap = AgentMap.Instance();
            if (agentMap is not null && agentMap->CurrentMapId != 0)
            {
                return agentMap->CurrentMapId;
            }

            if (Plugin.ClientState.MapId != 0)
            {
                return Plugin.ClientState.MapId;
            }

            var territorySheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
            if (territorySheet is null || !territorySheet.TryGetRow(Plugin.ClientState.TerritoryType, out var territory))
            {
                return 0;
            }

            return territory.Map.RowId;
        }

        private unsafe void TryRefreshEventMarkers()
        {
            var now = Environment.TickCount64;
            if (now - _lastEventMarkerRefreshMs < EventMarkerRefreshIntervalMs)
            {
                return;
            }

            _lastEventMarkerRefreshMs = now;
            var agentMap = AgentMap.Instance();
            if (agentMap is null || agentMap->CurrentMapId == 0)
            {
                return;
            }

            try
            {
                agentMap->UpdateEventMapMarkers(&agentMap->EventMarkersPtrs);
            }
            catch
            {
            }
        }

        private static unsafe string GetMapTitle()
        {
            var agentMap = AgentMap.Instance();
            return agentMap is null ? string.Empty : agentMap->MapTitleString.ToString();
        }
    }
}
