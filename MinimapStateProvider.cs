using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace FFXIVHudPlugin;

public sealed class MinimapStateProvider
{
    private const float PartyBlipRadius = 4f;

    private readonly IObjectTable objectTable;
    private readonly IPartyList partyList;
    private readonly IDataManager dataManager;
    private readonly IClientState clientState;
    private readonly HudConfiguration configuration;
    private readonly MinimapMapTextureCache mapTextureCache;
    private readonly MinimapPlayerIndicatorCache playerIndicatorCache;
    private readonly MinimapMarkerIconCache markerIconCache;

    public MinimapDiagnosticReport LatestDiagnostics { get; private set; } = new();

    public MinimapStateProvider(
        IObjectTable objectTable,
        IPartyList partyList,
        IDataManager dataManager,
        IClientState clientState,
        ITextureProvider textureProvider,
        HudConfiguration configuration)
    {
        this.objectTable = objectTable;
        this.partyList = partyList;
        this.dataManager = dataManager;
        this.clientState = clientState;
        this.configuration = configuration;
        this.mapTextureCache = new MinimapMapTextureCache(textureProvider, dataManager);
        this.playerIndicatorCache = new MinimapPlayerIndicatorCache(textureProvider);
        this.markerIconCache = new MinimapMarkerIconCache(textureProvider);
    }

    public MinimapSnapshot Build(
        float visibleRangeYalms,
        bool showNativeMarkers,
        float minimapSize,
        float markerIconSize)
    {
        var player = this.objectTable.LocalPlayer;
        if (player is null)
        {
            return MinimapSnapshot.Empty;
        }

        var range = MinimapLayout.ClampVisibleRange(visibleRangeYalms);
        var blips = new List<MinimapBlip>(8);
        var iconMarkers = new List<MinimapIconMarker>(MinimapLayout.MaxNativeMarkersPerFrame);
        this.AddPartyBlips(player, range, blips);

        var hasMapTexture = this.mapTextureCache.TryGetCurrentMapTexture(out var mapTexture) &&
                            MinimapTextureUtil.IsDrawable(mapTexture);
        var mapUvMin = Vector2.Zero;
        var mapUvMax = Vector2.One;
        var hasMapTransform = this.TryResolveMapTransform(out var offsetX, out var offsetY, out var sizeFactor);
        if (hasMapTexture && hasMapTransform &&
            !this.TryGetVisibleMapUvWindow(player, range, out mapUvMin, out mapUvMax))
        {
            mapUvMin = new Vector2(0.45f, 0.45f);
            mapUvMax = new Vector2(0.55f, 0.55f);
        }

        if (showNativeMarkers && hasMapTransform)
        {
            this.markerIconCache.BeginFrame();
            var contentHalf = MinimapLayout.ClampSize(minimapSize) * 0.5f;
            MinimapNaviMapMarkers.TryCollect(
                contentHalf,
                mapUvMin,
                mapUvMax,
                player.Position,
                offsetX,
                offsetY,
                sizeFactor,
                range,
                MinimapLayout.ClampMarkerIconSize(markerIconSize),
                this.markerIconCache,
                iconMarkers);
        }

        var hasNativeMapFrame = MinimapNativeFrame.TryGetMapImageTransform(out var nativeFrame);
        var hasCameraMapYaw = MinimapCameraHeading.TryGetMapYaw(out var cameraMapYaw);

        var snapshot = new MinimapSnapshot
        {
            IsActive = true,
            PlayerYaw = player.Rotation,
            CameraMapYaw = cameraMapYaw,
            HasCameraMapYaw = hasCameraMapYaw,
            MapTitle = this.GetMapTitle(),
            Blips = blips,
            IconMarkers = iconMarkers,
            MapTexture = mapTexture,
            MapUvMin = mapUvMin,
            MapUvMax = mapUvMax,
            HasMapTexture = hasMapTexture,
            PlayerIndicator = this.playerIndicatorCache.GetAssets(),
            HasNativeMapFrame = hasNativeMapFrame,
            NativeMapImageRotation = nativeFrame.Rotation,
            NativeMapImageScaleX = nativeFrame.ScaleX,
            NativeMapImageScaleY = nativeFrame.ScaleY,
            NativeNorthLockedUp = nativeFrame.NorthLockedUp,
            NativePlayerConeRotation = nativeFrame.PlayerConeRotation,
            VisibleRangeYalms = range,
        };

        if (this.configuration.MinimapShowDiagnostics)
        {
            this.LatestDiagnostics = MinimapDiagnostics.Capture(
                this.configuration,
                this.clientState,
                this.objectTable,
                this.dataManager,
                this.mapTextureCache,
                snapshot);
        }

        return snapshot;
    }

    private bool TryGetVisibleMapUvWindow(ICharacter player, float visibleRangeYalms, out Vector2 uvMin, out Vector2 uvMax)
    {
        uvMin = Vector2.Zero;
        uvMax = Vector2.One;

        if (!this.TryResolveMapTransform(out var offsetX, out var offsetY, out var sizeFactor))
        {
            return false;
        }

        var mapTextureCoords = MinimapMapMath.WorldToMapTextureCoords(
            player.Position,
            offsetX,
            offsetY,
            sizeFactor);
        return MinimapMapMath.TryGetVisibleMapUvWindow(
            mapTextureCoords,
            visibleRangeYalms,
            sizeFactor,
            out uvMin,
            out uvMax);
    }

    private unsafe bool TryResolveMapTransform(
        out int offsetX,
        out int offsetY,
        out uint sizeFactor)
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

        if (!this.TryGetCurrentMapRow(out var mapRow))
        {
            return false;
        }

        offsetX = mapRow.OffsetX;
        offsetY = mapRow.OffsetY;
        sizeFactor = mapRow.SizeFactor;
        return true;
    }

    private bool TryGetCurrentMapRow(out Map mapRow)
    {
        mapRow = default;
        var mapId = this.ResolveMapId();
        if (mapId == 0)
        {
            return false;
        }

        var sheet = this.dataManager.GetExcelSheet<Map>();
        if (sheet is null || !sheet.TryGetRow(mapId, out mapRow))
        {
            return false;
        }

        return true;
    }

    private unsafe uint ResolveMapId()
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is not null && agentMap->CurrentMapId != 0)
        {
            return agentMap->CurrentMapId;
        }

        if (this.clientState.MapId != 0)
        {
            return this.clientState.MapId;
        }

        var territorySheet = this.dataManager.GetExcelSheet<TerritoryType>();
        if (territorySheet is null || !territorySheet.TryGetRow(this.clientState.TerritoryType, out var territory))
        {
            return 0;
        }

        return territory.Map.RowId;
    }

    private void AddPartyBlips(ICharacter player, float visibleRangeYalms, List<MinimapBlip> blips)
    {
        var origin = player.Position;
        for (var i = 0; i < this.partyList.Length; i++)
        {
            var member = this.partyList[i];
            if (member.ObjectId == 0 || member.ObjectId == player.GameObjectId)
            {
                continue;
            }

            var gameObject = this.objectTable.SearchById(member.ObjectId);
            if (gameObject is not ICharacter character)
            {
                continue;
            }

            if (!TryCreateBlip(origin, character.Position, visibleRangeYalms, 0xFF66D4FF, PartyBlipRadius, out var blip))
            {
                continue;
            }

            blips.Add(blip);
        }
    }

    private static bool TryCreateBlip(
        Vector3 playerPosition,
        Vector3 worldPosition,
        float visibleRangeYalms,
        uint color,
        float radius,
        out MinimapBlip blip)
    {
        return TryCreateBlip(
            playerPosition,
            worldPosition.X,
            worldPosition.Z,
            visibleRangeYalms,
            color,
            radius,
            out blip);
    }

    private static bool TryCreateBlip(
        Vector3 playerPosition,
        float worldX,
        float worldZ,
        float visibleRangeYalms,
        uint color,
        float radius,
        out MinimapBlip blip)
    {
        var delta = new Vector2(worldX - playerPosition.X, worldZ - playerPosition.Z);
        if (delta.Length() > visibleRangeYalms)
        {
            blip = default;
            return false;
        }

        blip = new MinimapBlip
        {
            LocalOffset = delta,
            Color = color,
            Radius = radius,
        };
        return true;
    }

    private unsafe string GetMapTitle()
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is null)
        {
            return string.Empty;
        }

        return agentMap->MapTitleString.ToString();
    }
}
