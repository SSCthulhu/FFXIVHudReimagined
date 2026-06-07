using System.Numerics;
using FFXIVHudPlugin.AetherPlates.Configuration;
using Dalamud.Plugin.Services;
using FFXIVHudPlugin.AetherPlates.Styles;

namespace FFXIVHudPlugin.AetherPlates.Data;

public sealed record NameplateContext(
    TrackedObject Tracked,
    NameplateProfile Profile,
    Configuration.CategoryVisualSettings CategoryVisual,
    ITextureProvider TextureProvider,
    Vector2 AnchorScreenPosition,
    float GlobalScale,
    bool IsTarget,
    bool IsFocusTarget,
    bool IsBoss,
    bool IsPartyMember,
    bool IsAllianceMember,
    bool IsHostile,
    bool IsFriendly,
    float Distance,
    int FontFamilyId = 0)
{
    public NameplateStyle? ActiveStyle { get; init; }
}
