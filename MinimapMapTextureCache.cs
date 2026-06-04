using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace FFXIVHudPlugin;

public sealed class MinimapMapTextureCache
{
    private readonly ITextureProvider textureProvider;
    private readonly IDataManager dataManager;
    private string cachedTexturePath = string.Empty;
    private ISharedImmediateTexture? cachedTexture;
    private readonly List<string> lastCandidatePaths = new(32);

    public string LastLoadedPath { get; private set; } = string.Empty;
    public string LastLoadNote { get; private set; } = string.Empty;

    public MinimapMapTextureCache(ITextureProvider textureProvider, IDataManager dataManager)
    {
        this.textureProvider = textureProvider;
        this.dataManager = dataManager;
    }

    public IReadOnlyList<string> GetLastCandidatePaths(int maxCount) =>
        this.lastCandidatePaths.Take(Math.Max(maxCount, 0)).ToList();

    public unsafe bool TryGetCurrentMapTexture(out ISharedImmediateTexture? texture)
    {
        texture = null;
        this.lastCandidatePaths.Clear();
        this.LastLoadedPath = string.Empty;
        this.LastLoadNote = "No path produced a drawable texture.";

        if (MinimapNativeMapTexture.TryGetMapImagePath(out var nativeMapPath, out _) &&
            this.TryLoadPath(nativeMapPath, "Loaded from _NaviMap MapImage.", out texture))
        {
            return true;
        }

        var agentMap = AgentMap.Instance();
        var mapRowId = ResolveMapRowId(agentMap);
        Map? mapRow = null;
        if (mapRowId != 0)
        {
            var sheet = this.dataManager.GetExcelSheet<Map>();
            if (sheet is not null && sheet.TryGetRow(mapRowId, out var row))
            {
                mapRow = row;
            }
        }

        ISharedImmediateTexture? maskFallback = null;
        var maskFallbackPath = string.Empty;

        foreach (var candidate in MinimapMapPathResolver.BuildCandidates(mapRowId, mapRow, agentMap))
        {
            foreach (var path in EnumeratePathVariants(candidate))
            {
                this.lastCandidatePaths.Add(path);
                if (!this.TryLoadPath(path, "Loaded from AgentMap/Lumina path.", out var loaded))
                {
                    continue;
                }

                if (MinimapMapPathResolver.IsMaskMapPath(path))
                {
                    maskFallback ??= loaded;
                    maskFallbackPath = this.cachedTexturePath;
                    continue;
                }

                texture = loaded;
                return true;
            }
        }

        if (maskFallback is not null)
        {
            this.cachedTexturePath = maskFallbackPath;
            this.cachedTexture = maskFallback;
            texture = maskFallback;
            this.LastLoadedPath = maskFallbackPath;
            this.LastLoadNote = "Loaded mask texture (no base map found).";
            return true;
        }

        this.cachedTexturePath = string.Empty;
        this.cachedTexture = null;
        return false;
    }

    private bool TryLoadPath(string path, string successNote, out ISharedImmediateTexture? texture)
    {
        texture = null;
        if (!this.TryGetLoadedTexture(path, out texture))
        {
            return false;
        }

        this.LastLoadedPath = this.cachedTexturePath;
        this.LastLoadNote = successNote;
        return true;
    }

    private static unsafe uint ResolveMapRowId(AgentMap* agentMap)
    {
        if (agentMap is not null && agentMap->CurrentMapId != 0)
        {
            return agentMap->CurrentMapId;
        }

        return 0;
    }

    private bool TryGetLoadedTexture(string path, out ISharedImmediateTexture? texture)
    {
        texture = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = NormalizeGamePath(path);
        if (normalized.Length == 0)
        {
            return false;
        }

        if (!string.Equals(this.cachedTexturePath, normalized, StringComparison.Ordinal))
        {
            this.cachedTexturePath = normalized;
            this.cachedTexture = this.textureProvider.GetFromGame(normalized);
        }

        if (this.cachedTexture is null)
        {
            return false;
        }

        if (!this.cachedTexture.TryGetWrap(out var wrap, out _) || wrap.Width <= 0 || wrap.Height <= 0)
        {
            return false;
        }

        texture = this.cachedTexture;
        return true;
    }

    private static IEnumerable<string> EnumeratePathVariants(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            yield break;
        }

        var normalized = NormalizeGamePath(rawPath);
        if (normalized.Length == 0)
        {
            yield break;
        }

        yield return normalized;

        if (!normalized.EndsWith(".tex", StringComparison.OrdinalIgnoreCase))
        {
            yield return normalized + ".tex";
            yield return normalized + ".atex";
        }

        if (!normalized.StartsWith("ui/", StringComparison.OrdinalIgnoreCase))
        {
            yield return "ui/" + normalized.TrimStart('/');
        }
    }

    private static string NormalizeGamePath(string path)
    {
        var trimmed = path.Trim().Replace('\\', '/');
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return trimmed.TrimStart('/');
    }
}
