namespace FFXIVHudPlugin.AetherPlates.Widgets.BuffRow;

using Dalamud.Interface.Textures;
using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using System.Numerics;

public sealed class BuffRowWidget : INameplateWidget
{
    public string Id => "buff_row";

    public Vector2 GetDesiredSize(NameplateContext context)
    {
        var cfg = context.Profile.BuffRow;
        var maxIcons = Math.Clamp(cfg.MaxIcons, 1, 24);
        return new Vector2((cfg.IconSize * maxIcons) + (cfg.IconGap * (maxIcons - 1)), cfg.IconSize);
    }

    public void Draw(NameplateContext context, DrawContext drawContext, WidgetLayout layout)
    {
        DrawStatusRow(context, drawContext, layout, false);
    }

    internal static void DrawStatusRow(NameplateContext context, DrawContext drawContext, WidgetLayout layout, bool debuffs)
    {
        var statuses = context.Tracked.Statuses;
        var iconScale = Math.Clamp(context.GlobalScale, 0.5f, 3.0f);
        var iconSize = debuffs
            ? Math.Max(8f, context.Profile.DebuffRow.IconSize) * iconScale
            : Math.Max(8f, context.Profile.BuffRow.IconSize) * iconScale;
        var gap = debuffs
            ? Math.Max(0f, context.Profile.DebuffRow.IconGap) * iconScale
            : Math.Max(0f, context.Profile.BuffRow.IconGap) * iconScale;
        var maxIcons = debuffs
            ? Math.Clamp(context.Profile.DebuffRow.MaxIcons, 1, 24)
            : Math.Clamp(context.Profile.BuffRow.MaxIcons, 1, 24);
        var x = layout.Position.X;
        var y = layout.Position.Y;
        var rowWidth = layout.Size.X;
        var drawn = 0;
        var localId = (uint)context.Tracked.ObjectId;
        var onlyMine = debuffs ? context.Profile.DebuffRow.OnlyMine : context.Profile.BuffRow.OnlyMine;
        var whitelist = debuffs ? context.Profile.DebuffRow.Whitelist : context.Profile.BuffRow.Whitelist;
        var blacklist = debuffs ? context.Profile.DebuffRow.Blacklist : context.Profile.BuffRow.Blacklist;

        for (var i = 0; i < statuses.Count && drawn < maxIcons; i++)
        {
            var status = statuses[i];
            if (status.IsDebuff != debuffs)
            {
                continue;
            }

            if (onlyMine && status.SourceId != localId)
            {
                continue;
            }

            if (whitelist.Count > 0 && !whitelist.Contains(status.StatusId))
            {
                continue;
            }

            if (blacklist.Contains(status.StatusId))
            {
                continue;
            }

            var minX = debuffs
                ? x + rowWidth - iconSize - ((iconSize + gap) * drawn)
                : x + ((iconSize + gap) * drawn);
            var min = new Vector2(minX, y);
            var max = min + new Vector2(iconSize, iconSize);
            DrawStatusIcon(context, drawContext, min, max, status, debuffs);
            drawn++;
        }
    }

    internal static void DrawStatusIcon(
        NameplateContext context,
        DrawContext drawContext,
        Vector2 min,
        Vector2 max,
        StatusSnapshot status,
        bool debuffs)
    {
        var tint = debuffs ? 0xFFF0B0B0 : 0xFFFFFFFF;
        var bg = debuffs ? 0xAA3A2020 : 0xAA202A3A;
        drawContext.DrawFilledRect(min, max, bg, 3f);

        if (status.IconId != 0)
        {
            var texture = context.TextureProvider.GetFromGameIcon(new GameIconLookup(status.IconId));
            if (texture is not null)
            {
                drawContext.DrawImage(texture, min + new Vector2(1f, 1f), max - new Vector2(1f, 1f), tint);
            }
            else
            {
                drawContext.DrawFilledRect(min + new Vector2(1f, 1f), max - new Vector2(1f, 1f), debuffs ? 0x80A05050 : 0x805080A0, 2f);
            }
        }

        drawContext.DrawBorder(min, max, 0xFF000000, 3f, 1f);

        if (status.StackCount > 1)
        {
            drawContext.DrawText(new Vector2(max.X - 10f, max.Y - 14f), 0xFFFFFFFF, status.StackCount.ToString(), 12f);
        }

        if (status.RemainingTime > 0.05f)
        {
            var timeText = status.RemainingTime >= 10f
                ? $"{MathF.Floor(status.RemainingTime):0}"
                : $"{status.RemainingTime:0.0}";
            drawContext.DrawText(new Vector2(min.X + 1f, max.Y - 12f), 0xFFFEFEFE, timeText, 11f);
        }
    }
}
