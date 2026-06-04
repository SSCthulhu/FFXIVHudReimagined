using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace FFXIVHudPlugin;

/// <summary>
/// Per-frame cache for native minimap marker icons with a hard load budget.
/// </summary>
internal sealed class MinimapMarkerIconCache
{
    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<uint, ISharedImmediateTexture?> icons = new(32);
    private int loadsThisFrame;

    public MinimapMarkerIconCache(ITextureProvider textureProvider)
    {
        this.textureProvider = textureProvider;
    }

    public void BeginFrame()
    {
        this.icons.Clear();
        this.loadsThisFrame = 0;
    }

    public ISharedImmediateTexture? TryGetIcon(uint iconId)
    {
        if (iconId == 0)
        {
            return null;
        }

        if (this.icons.TryGetValue(iconId, out var cached))
        {
            return cached;
        }

        if (this.loadsThisFrame >= MinimapLayout.MaxNativeMarkerIconLoadsPerFrame)
        {
            return null;
        }

        this.loadsThisFrame++;
        var texture = this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId));
        this.icons[iconId] = texture;
        return texture;
    }

    public bool TryGetDrawableIcon(uint iconId, out ISharedImmediateTexture? texture)
    {
        texture = null;
        var resolved = this.TryGetIcon(iconId);
        if (resolved is null)
        {
            return false;
        }

        var wrap = resolved.GetWrapOrEmpty();
        if (wrap.Handle == 0 || wrap.Width <= 0 || wrap.Height <= 0)
        {
            return false;
        }

        texture = resolved;
        return true;
    }
}
