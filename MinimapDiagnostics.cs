using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Numerics;
using System.Text;

namespace FFXIVHudPlugin;

/// <summary>
/// Snapshot of minimap state for troubleshooting map load and marker placement.
/// </summary>
public sealed class MinimapDiagnosticReport
{
    public string Text { get; init; } = string.Empty;
    public DateTime CapturedUtc { get; init; } = DateTime.UtcNow;
}

internal static class MinimapDiagnostics
{
    public static MinimapDiagnosticReport Capture(
        HudConfiguration config,
        IClientState clientState,
        IObjectTable objectTable,
        IPartyList partyList,
        IDataManager dataManager,
        MinimapMapTextureCache mapTextureCache,
        MinimapSnapshot snapshot)
    {
        var builder = new StringBuilder(2048);
        var version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "unknown";
        builder.AppendLine("=== FFXIV Hud Reimagined — Minimap Diagnostics ===");
        builder.AppendLine($"Build: {version}");
        builder.AppendLine($"Captured (UTC): {DateTime.UtcNow:O}");
        builder.AppendLine();

        AppendClientState(builder, clientState);
        AppendAgentMap(builder);
        AppendLuminaMap(builder, dataManager, clientState);
        AppendTexture(builder, mapTextureCache, snapshot);
        AppendUvAndZoom(builder, config, snapshot);
        AppendPlayerAndMarkers(builder, clientState, objectTable, partyList, dataManager, snapshot);
        AppendMarkerDraw(builder, snapshot);
        AppendNativeMinimap(builder);

        builder.AppendLine();
        builder.AppendLine("Copy this entire block when reporting minimap issues.");
        return new MinimapDiagnosticReport
        {
            Text = builder.ToString(),
            CapturedUtc = DateTime.UtcNow,
        };
    }

    private static void AppendClientState(StringBuilder builder, IClientState clientState)
    {
        builder.AppendLine("[Client]");
        builder.AppendLine($"  Logged in: {clientState.IsLoggedIn}");
        builder.AppendLine($"  TerritoryType: {clientState.TerritoryType}");
        builder.AppendLine($"  ClientState.MapId: {clientState.MapId}");
    }

    private static unsafe void AppendAgentMap(StringBuilder builder)
    {
        builder.AppendLine("[AgentMap]");
        var agentMap = AgentMap.Instance();
        if (agentMap is null)
        {
            builder.AppendLine("  AgentMap: null");
            return;
        }

        builder.AppendLine($"  CurrentMapId: {agentMap->CurrentMapId}");
        builder.AppendLine($"  CurrentTerritoryId: {agentMap->CurrentTerritoryId}");
        builder.AppendLine($"  CurrentMapMarkerRange: {agentMap->CurrentMapMarkerRange}");
        builder.AppendLine($"  CurrentMapSizeFactor: {agentMap->CurrentMapSizeFactor}");
        builder.AppendLine($"  CurrentMapSizeFactorFloat: {agentMap->CurrentMapSizeFactorFloat:F3}");
        builder.AppendLine($"  CurrentOffsetX: {agentMap->CurrentOffsetX}");
        builder.AppendLine($"  CurrentOffsetY: {agentMap->CurrentOffsetY}");
        builder.AppendLine($"  MiniMapMarkerCount: {agentMap->MiniMapMarkerCount}");
        builder.AppendLine($"  MapTitle: {agentMap->MapTitleString.ToString()}");
        builder.AppendLine($"  CurrentMapBgPath: {agentMap->CurrentMapBgPath.ToString()}");
        builder.AppendLine($"  SelectedMapBgPath: {agentMap->SelectedMapBgPath.ToString()}");
        builder.AppendLine($"  CurrentMapPath: {agentMap->CurrentMapPath.ToString()}");
        builder.AppendLine($"  SelectedMapPath: {agentMap->SelectedMapPath.ToString()}");
    }

    private static void AppendLuminaMap(StringBuilder builder, IDataManager dataManager, IClientState clientState)
    {
        builder.AppendLine("[Lumina Map row]");
        var mapId = ResolveMapRowId(dataManager, clientState);
        if (mapId == 0)
        {
            builder.AppendLine("  (no map row resolved)");
            return;
        }

        var sheet = dataManager.GetExcelSheet<Map>();
        if (sheet is null || !sheet.TryGetRow(mapId, out var mapRow))
        {
            builder.AppendLine($"  RowId {mapId}: not found in sheet");
            return;
        }

        builder.AppendLine($"  RowId: {mapId}");
        builder.AppendLine($"  Id: {mapRow.Id}");
        builder.AppendLine($"  SizeFactor: {mapRow.SizeFactor}");
        builder.AppendLine($"  OffsetX: {mapRow.OffsetX}");
        builder.AppendLine($"  OffsetY: {mapRow.OffsetY}");
    }

    private static void AppendTexture(
        StringBuilder builder,
        MinimapMapTextureCache mapTextureCache,
        MinimapSnapshot snapshot)
    {
        builder.AppendLine("[Map texture]");
        builder.AppendLine($"  Snapshot.HasMapTexture: {snapshot.HasMapTexture}");
        builder.AppendLine($"  Drawable: {MinimapTextureUtil.IsDrawable(snapshot.MapTexture)}");
        builder.AppendLine($"  Last loaded path: {mapTextureCache.LastLoadedPath}");
        builder.AppendLine($"  Is mask path: {MinimapMapPathResolver.IsMaskMapPath(mapTextureCache.LastLoadedPath)}");
        builder.AppendLine($"  Last load note: {mapTextureCache.LastLoadNote}");
        if (MinimapTextureUtil.IsDrawable(snapshot.MapTexture))
        {
            var wrap = snapshot.MapTexture!.GetWrapOrEmpty();
            builder.AppendLine($"  Wrap size: {wrap.Width}x{wrap.Height}");
        }

        builder.AppendLine("  Path candidates tried (first 12):");
        var candidates = mapTextureCache.GetLastCandidatePaths(12);
        if (candidates.Count == 0)
        {
            builder.AppendLine("    (none)");
        }
        else
        {
            foreach (var path in candidates)
            {
                builder.AppendLine($"    - {path}");
            }
        }
    }

    private static unsafe void AppendUvAndZoom(
        StringBuilder builder,
        HudConfiguration config,
        MinimapSnapshot snapshot)
    {
        builder.AppendLine("[UV / zoom]");
        builder.AppendLine($"  Config visible range (yalms): {config.MinimapVisibleRangeYalms:F1}");
        builder.AppendLine($"  Snapshot visible range (yalms): {snapshot.VisibleRangeYalms:F1}");
        var agentMap = AgentMap.Instance();
        if (agentMap is not null)
        {
            builder.AppendLine($"  AgentMap CurrentMapMarkerRange: {agentMap->CurrentMapMarkerRange}");
        }

        builder.AppendLine($"  MapUvMin: {snapshot.MapUvMin}");
        builder.AppendLine($"  MapUvMax: {snapshot.MapUvMax}");
        builder.AppendLine($"  Native north locked: {snapshot.NativeNorthLockedUp}");
    }

    private static void AppendPartyMembers(
        StringBuilder builder,
        IObjectTable objectTable,
        IPartyList partyList,
        IDataManager dataManager,
        IClientState clientState,
        MinimapSnapshot snapshot,
        int offsetX,
        int offsetY,
        uint sizeFactor)
    {
        builder.AppendLine($"  Custom party blips drawn: {snapshot.Blips.Count}");
        var player = objectTable.LocalPlayer;
        if (player is null)
        {
            return;
        }

        var territoryType = clientState.TerritoryType;
        var listed = 0;
        for (var i = 0; i < partyList.Length && listed < 8; i++)
        {
            var member = partyList[i];
            if (member.EntityId == 0 || member.EntityId == player.EntityId)
            {
                continue;
            }

            listed++;
            var inTerritory = member.Territory.RowId == territoryType;
            var pos = member.Position;
            var inRange = false;
            if (inTerritory && float.IsFinite(pos.X) && float.IsFinite(pos.Z))
            {
                var playerTex = MinimapMapMath.WorldToMapTextureCoords(player.Position, offsetX, offsetY, sizeFactor);
                var memberTex = MinimapMapMath.WorldToMapTextureCoords(pos, offsetX, offsetY, sizeFactor);
                var maxTexDistance = snapshot.VisibleRangeYalms * (sizeFactor / 100f);
                inRange = (memberTex - playerTex).Length() <= maxTexDistance;
            }

            builder.AppendLine(
                $"  Party[{listed - 1}] {member.Name.TextValue} entity {member.EntityId:X} " +
                $"HP {member.CurrentHP}/{member.MaxHP} territory {member.Territory.RowId} " +
                $"sameZone {inTerritory} inRange {inRange} world ({pos.X:F1}, {pos.Z:F1})");
        }

        if (listed == 0)
        {
            builder.AppendLine("  Party members (excluding self): none");
        }

        var dutySupportListed = 0;
        var playerObjectId = player.GameObjectId;
        foreach (var obj in objectTable)
        {
            if (obj.ObjectKind != ObjectKind.BattleNpc || obj is not ICharacter character)
            {
                continue;
            }

            if (obj.GameObjectId == playerObjectId || character.ClassJob.RowId == 0 || character.MaxHp == 0 || character.CurrentHp == 0)
            {
                continue;
            }

            if (dutySupportListed >= 4)
            {
                break;
            }

            dutySupportListed++;
            var pos = character.Position;
            var playerTex = MinimapMapMath.WorldToMapTextureCoords(player.Position, offsetX, offsetY, sizeFactor);
            var memberTex = MinimapMapMath.WorldToMapTextureCoords(pos, offsetX, offsetY, sizeFactor);
            var maxTexDistance = snapshot.VisibleRangeYalms * (sizeFactor / 100f);
            var inRange = (memberTex - playerTex).Length() <= maxTexDistance;

            var colorText = "fallback";
            if (MinimapRoleColor.TryResolveArgb(dataManager, character.ClassJob.RowId, out var roleColor))
            {
                colorText = $"role {roleColor:X8}";
            }

            builder.AppendLine(
                $"  DutySupport[{dutySupportListed - 1}] {character.Name.TextValue} entity {character.EntityId:X} " +
                $"job {character.ClassJob.RowId} inRange {inRange} color {colorText} world ({pos.X:F1}, {pos.Z:F1})");
        }

        if (dutySupportListed == 0)
        {
            builder.AppendLine("  DutySupport candidates in object table: none");
        }
    }

    private static unsafe void AppendPlayerAndMarkers(
        StringBuilder builder,
        IClientState clientState,
        IObjectTable objectTable,
        IPartyList partyList,
        IDataManager dataManager,
        MinimapSnapshot snapshot)
    {
        builder.AppendLine("[Player / markers]");
        var player = objectTable.LocalPlayer;
        if (player is null)
        {
            builder.AppendLine("  LocalPlayer: null");
            return;
        }

        var agentMap = AgentMap.Instance();
        if (agentMap is null || agentMap->CurrentMapId == 0)
        {
            builder.AppendLine("  AgentMap unavailable for marker math");
            return;
        }

        var offsetX = -agentMap->CurrentOffsetX;
        var offsetY = -agentMap->CurrentOffsetY;
        var sizeFactor = (uint)Math.Max(agentMap->CurrentMapSizeFactor, (short)1);
        var playerTex = MinimapMapMath.WorldToMapTextureCoords(player.Position, offsetX, offsetY, sizeFactor);
        builder.AppendLine($"  Player world XZ: ({player.Position.X:F2}, {player.Position.Z:F2})");
        builder.AppendLine($"  Player texture coords: ({playerTex.X:F1}, {playerTex.Y:F1})");
        builder.AppendLine($"  Transform offsets used: ({offsetX}, {offsetY}), sizeFactor {sizeFactor}");
        AppendPartyMembers(builder, objectTable, partyList, dataManager, clientState, snapshot, offsetX, offsetY, sizeFactor);

        builder.AppendLine($"  FlagMarkerCount: {agentMap->FlagMarkerCount}");
        if (agentMap->FlagMarkerCount > 0)
        {
            ref readonly var flag = ref agentMap->FlagMapMarkers[0];
            builder.AppendLine(
                $"  Flag map {flag.MapId} (current {agentMap->CurrentMapId}) icon {flag.MapMarker.IconId} " +
                $"world ({flag.XFloat:F1}, {flag.YFloat:F1})");
        }

        builder.AppendLine($"  TempMapMarkerCount: {agentMap->TempMapMarkerCount}");

        var gatheringShown = 0;
        foreach (ref readonly var gathering in agentMap->MiniMapGatheringMarkers)
        {
            if (gathering.MapMarker.IconId == 0)
            {
                continue;
            }

            gatheringShown++;
            if (gatheringShown > 3)
            {
                break;
            }

            var worldX = gathering.MapMarker.X / 16f;
            var worldZ = gathering.MapMarker.Y / 16f;
            builder.AppendLine(
                $"  GatheringCategory[{gatheringShown - 1}] icon {gathering.MapMarker.IconId} " +
                $"ShouldRender {gathering.ShouldRender} world ({worldX:F1}, {worldZ:F1})");
        }

        if (gatheringShown == 0)
        {
            builder.AppendLine("  GatheringCategory markers: none (IconId 0 in all 6 slots)");
        }

        var maxTexDistance = snapshot.VisibleRangeYalms * (sizeFactor / 100f);
        var inRangeMiniMap = 0;
        var nearestMiniMapDist = float.MaxValue;
        var markerTotal = Math.Min(agentMap->MiniMapMarkerCount, agentMap->MiniMapMarkers.Length);
        for (var i = 0; i < markerTotal; i++)
        {
            ref readonly var entry = ref agentMap->MiniMapMarkers[i];
            var worldX = entry.MapMarker.X / 16f;
            var worldZ = entry.MapMarker.Y / 16f;
            var markerTex = MinimapMapMath.WorldToMapTextureCoords(
                new Vector3(worldX, 0f, worldZ),
                offsetX,
                offsetY,
                sizeFactor);
            var dist = (markerTex - playerTex).Length();
            if (dist < nearestMiniMapDist)
            {
                nearestMiniMapDist = dist;
            }

            if (dist <= maxTexDistance)
            {
                inRangeMiniMap++;
            }
        }

        builder.AppendLine(
            $"  MiniMapMarkers in visible range: {inRangeMiniMap}/{markerTotal} " +
            $"(max tex distance {maxTexDistance:F1}, nearest {nearestMiniMapDist:F1})");

        var gatheringPointsNear = 0;
        var visibleRange = snapshot.VisibleRangeYalms + 8f;
        var visibleRangeSq = visibleRange * visibleRange;
        foreach (var obj in objectTable)
        {
            if (obj.ObjectKind != ObjectKind.GatheringPoint || !obj.IsTargetable)
            {
                continue;
            }

            var delta = obj.Position - player.Position;
            delta.Y = 0f;
            if (delta.LengthSquared() <= visibleRangeSq)
            {
                gatheringPointsNear++;
            }
        }

        builder.AppendLine($"  GatheringPoint objects in range: {gatheringPointsNear}");

        var fateManager = FateManager.Instance();
        if (fateManager is null)
        {
            builder.AppendLine("  FateManager: null");
        }
        else
        {
            var fateCount = (int)fateManager->Fates.LongCount;
            builder.AppendLine($"  FateManager active fates: {fateCount}");
            var fateShown = 0;
            var fateInRange = 0;
            for (var i = 0; i < Math.Min(fateCount, 32); i++)
            {
                if (fateManager->Fates.First is null)
                {
                    break;
                }

                var fate = fateManager->Fates.First[i].Value;
                if (fate is null)
                {
                    continue;
                }

                var iconId = fate->MapIconId != 0 ? fate->MapIconId : fate->IconId;
                var worldX = fate->Location.X;
                var worldZ = fate->Location.Z;
                var markerTex = MinimapMapMath.WorldToMapTextureCoords(
                    new Vector3(worldX, 0f, worldZ),
                    offsetX,
                    offsetY,
                    sizeFactor);
                var dist = (markerTex - playerTex).Length();
                if (dist <= maxTexDistance)
                {
                    fateInRange++;
                }

                if (fateShown >= 3)
                {
                    continue;
                }

                fateShown++;
                builder.AppendLine(
                    $"  Fate[{fateShown - 1}] id {fate->FateId} state {fate->State} icon {iconId} " +
                    $"radius {fate->Radius:F1}y world ({worldX:F1}, {worldZ:F1}) texDist {dist:F1}");
            }

            builder.AppendLine($"  Fates in visible range: {fateInRange}");
        }

        var eventMarkerTotal = (int)agentMap->EventMarkers.LongCount;
        builder.AppendLine($"  EventMarkers count: {eventMarkerTotal}");
        var eventShown = 0;
        var eventInRange = 0;
        for (var i = 0; i < Math.Min(eventMarkerTotal, 96); i++)
        {
            if (agentMap->EventMarkers.First is null)
            {
                break;
            }

            var marker = agentMap->EventMarkers.First[i];
            if (marker.IconId == 0)
            {
                continue;
            }

            if (marker.MapId != 0 && marker.MapId != agentMap->CurrentMapId)
            {
                continue;
            }

            var markerTex = MinimapMapMath.WorldToMapTextureCoords(
                new Vector3(marker.Position.X, 0f, marker.Position.Z),
                offsetX,
                offsetY,
                sizeFactor);
            var dist = (markerTex - playerTex).Length();
            if (dist <= maxTexDistance)
            {
                eventInRange++;
            }

            if (eventShown >= 3)
            {
                continue;
            }

            eventShown++;
            builder.AppendLine(
                $"  Event[{eventShown - 1}] icon {marker.IconId} map {marker.MapId} " +
                $"world ({marker.Position.X:F1}, {marker.Position.Z:F1}) texDist {dist:F1}");
        }

        if (eventMarkerTotal == 0)
        {
            builder.AppendLine("  Event markers: none in AgentMap vector");
        }
        else
        {
            builder.AppendLine($"  Event markers on this map in range: {eventInRange}");
        }

        var mapMarkerTotal = Math.Min(agentMap->MapMarkerCount, agentMap->MapMarkers.Length);
        var mapInRange = 0;
        var mapShown = 0;
        builder.AppendLine($"  MapMarkers count: {mapMarkerTotal}");
        for (var i = 0; i < mapMarkerTotal; i++)
        {
            ref readonly var marker = ref agentMap->MapMarkers[i];
            if (marker.MapMarker.IconId == 0)
            {
                continue;
            }

            var worldX = marker.MapMarker.X / 16f;
            var worldZ = marker.MapMarker.Y / 16f;
            var markerTex = MinimapMapMath.WorldToMapTextureCoords(
                new Vector3(worldX, 0f, worldZ),
                offsetX,
                offsetY,
                sizeFactor);
            var dist = (markerTex - playerTex).Length();
            if (dist <= maxTexDistance)
            {
                mapInRange++;
            }

            if (mapShown >= 3)
            {
                continue;
            }

            mapShown++;
            builder.AppendLine(
                $"  MapMarker[{mapShown - 1}] icon {marker.MapMarker.IconId} dataType {marker.DataType} " +
                $"world ({worldX:F1}, {worldZ:F1}) texDist {dist:F1}");
        }

        if (mapMarkerTotal > 0)
        {
            builder.AppendLine($"  Map markers in visible range: {mapInRange}");
        }

        var markerCount = Math.Min(agentMap->MiniMapMarkerCount, agentMap->MiniMapMarkers.Length);
        markerCount = Math.Min(markerCount, 5);
        builder.AppendLine($"  Sample MiniMapMarkers ({markerCount} shown):");
        for (var i = 0; i < markerCount; i++)
        {
            ref readonly var entry = ref agentMap->MiniMapMarkers[i];
            var iconId = entry.MapMarker.IconId;
            var worldX = entry.MapMarker.X / 16f;
            var worldZ = entry.MapMarker.Y / 16f;
            var markerTex = MinimapMapMath.WorldToMapTextureCoords(
                new Vector3(worldX, 0f, worldZ),
                offsetX,
                offsetY,
                sizeFactor);
            var texDelta = markerTex - playerTex;
            builder.AppendLine(
                $"    [{i}] icon {iconId} raw XY ({entry.MapMarker.X}, {entry.MapMarker.Y}) " +
                $"world ({worldX:F1}, {worldZ:F1}) texDelta ({texDelta.X:F1}, {texDelta.Y:F1}) len {texDelta.Length():F1}");
        }
    }

    private static unsafe void AppendMarkerDraw(StringBuilder builder, MinimapSnapshot snapshot)
    {
        builder.AppendLine("[Custom minimap draw]");
        var agentMap = AgentMap.Instance();
        var agentCount = agentMap is null ? 0 : agentMap->MiniMapMarkerCount;
        builder.AppendLine($"  AgentMap MiniMapMarkerCount: {agentCount}");
        builder.AppendLine($"  ImGui icon markers: {snapshot.IconMarkers.Count}");
        builder.AppendLine($"  Fate area rings: {snapshot.FateAreas.Count}");
        builder.AppendLine($"  Party blips: {snapshot.Blips.Count}");
        var enemyBlips = 0;
        var partyBlips = 0;
        foreach (var blip in snapshot.Blips)
        {
            if (blip.Kind == MinimapBlipKind.Enemy)
            {
                enemyBlips++;
            }
            else
            {
                partyBlips++;
            }
        }

        builder.AppendLine($"  Party/duty blips: {partyBlips}");
        builder.AppendLine($"  Enemy blips: {enemyBlips}");
        builder.AppendLine(
            "    sources: flag, temp, GatheringPoint, gathering categories, FateManager, EventMarkers, MiniMapMarkers");
        builder.AppendLine("    placement: map texture delta (UV window scale)");
        builder.AppendLine($"  AgentMap available: {MinimapNaviMapMarkers.IsAddonLoaded()}");
    }

    private static unsafe void AppendNativeMinimap(StringBuilder builder)
    {
        builder.AppendLine("[_NaviMap]");
        if (!MinimapNativeMapTexture.TryGetMapImagePath(out var nativePath, out var addonLoaded))
        {
            builder.AppendLine("  Addon: not loaded");
            return;
        }

        builder.AppendLine($"  Addon loaded: {addonLoaded}");
        builder.AppendLine($"  MapImage texture path: {nativePath}");

        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return;
        }

        var addon = (AddonNaviMap*)stage->RaptureAtkUnitManager->GetAddonByName(
            NativeMinimapVisibility.AddonName,
            1);
        if (addon is null)
        {
            return;
        }

        builder.AppendLine($"  Addon IsVisible: {addon->IsVisible} (expected true while hidden via alpha)");
        builder.AppendLine($"  Addon alpha: {addon->Alpha}");
        var iconsRoot = (AtkComponentNode*)addon->UldManager.NodeList[2];
        if (iconsRoot is not null)
        {
            builder.AppendLine($"  Icons root visible: {iconsRoot->AtkResNode.IsVisible()}");
        }

        ref var naviMap = ref addon->NaviMap;
        builder.AppendLine($"  NaviMapMarkers span length: {naviMap.NaviMapMarkers.Length}");
    }

    private static unsafe uint ResolveMapRowId(IDataManager dataManager, IClientState clientState)
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is not null && agentMap->CurrentMapId != 0)
        {
            return agentMap->CurrentMapId;
        }

        if (clientState.MapId != 0)
        {
            return clientState.MapId;
        }

        var territorySheet = dataManager.GetExcelSheet<TerritoryType>();
        if (territorySheet is null || !territorySheet.TryGetRow(clientState.TerritoryType, out var territory))
        {
            return 0;
        }

        return territory.Map.RowId;
    }
}
