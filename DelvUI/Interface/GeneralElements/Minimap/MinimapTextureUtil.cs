using Dalamud.Interface.Textures;

namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapTextureUtil
    {
        public static bool IsDrawable(ISharedImmediateTexture? texture)
        {
            if (texture is null)
            {
                return false;
            }

            var wrap = texture.GetWrapOrEmpty();
            return wrap.Handle != 0 && wrap.Width > 0 && wrap.Height > 0;
        }
    }
}
