using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
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
        AppendPlayerAndMarkers(builder, objectTable, dataManager);
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

    private static unsafe void AppendPlayerAndMarkers(
        StringBuilder builder,
        IObjectTable objectTable,
        IDataManager dataManager)
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
        builder.AppendLine("    placement: map texture delta (UV window scale)");
        builder.AppendLine($"  NaviMap addon loaded: {MinimapNaviMapMarkers.IsAddonLoaded()}");
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
