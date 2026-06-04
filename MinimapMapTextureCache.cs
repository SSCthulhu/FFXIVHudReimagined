using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace FFXIVHudPlugin;

public sealed class MinimapMapTextureCache
{
    private readonly ITextureProvider textureProvider;
    private string cachedTexturePath = string.Empty;
    private ISharedImmediateTexture? cachedTexture;

    public MinimapMapTextureCache(ITextureProvider textureProvider)
    {
        this.textureProvider = textureProvider;
    }

    public unsafe bool TryGetCurrentMapTexture(out ISharedImmediateTexture? texture)
    {
        texture = null;
        foreach (var candidate in EnumeratePathCandidates(ResolveCurrentMapTexturePath()))
        {
            if (this.TryGetLoadedTexture(candidate, out texture))
            {
                return true;
            }
        }

        this.cachedTexturePath = string.Empty;
        this.cachedTexture = null;
        return false;
    }

    private bool TryGetLoadedTexture(string path, out ISharedImmediateTexture? texture)
    {
        texture = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (!string.Equals(this.cachedTexturePath, path, StringComparison.Ordinal))
        {
            this.cachedTexturePath = path;
            this.cachedTexture = this.textureProvider.GetFromGame(path);
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

    private unsafe static IEnumerable<string> EnumeratePathCandidates(string rawPath)
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

    private unsafe static string ResolveCurrentMapTexturePath()
    {
        var agentMap = AgentMap.Instance();
        if (agentMap is null)
        {
            return string.Empty;
        }

        var backgroundPath = agentMap->CurrentMapBgPath.ToString();
        if (!string.IsNullOrWhiteSpace(backgroundPath))
        {
            return backgroundPath;
        }

        var selectedBackgroundPath = agentMap->SelectedMapBgPath.ToString();
        if (!string.IsNullOrWhiteSpace(selectedBackgroundPath))
        {
            return selectedBackgroundPath;
        }

        return agentMap->CurrentMapPath.ToString();
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
