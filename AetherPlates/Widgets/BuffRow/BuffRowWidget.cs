namespace FFXIVHudPlugin.AetherPlates.Widgets.BuffRow;

using Dalamud.Interface.Textures;
using Dalamud.Bindings.ImGui;
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
        var rowScale = debuffs
            ? Math.Clamp(context.CategoryVisual.DebuffRowScale, 0.25f, 8f)
            : Math.Clamp(context.CategoryVisual.BuffRowScale, 0.25f, 8f);
        var iconHeight = debuffs
            ? Math.Max(8f, context.Profile.DebuffRow.IconSize) * iconScale * rowScale
            : Math.Max(8f, context.Profile.BuffRow.IconSize) * iconScale * rowScale;
        var iconWidth = StatusLaneLayout.GetIconWidth(iconHeight);
        var gap = debuffs
            ? Math.Max(0f, context.Profile.DebuffRow.IconGap) * iconScale * rowScale
            : Math.Max(0f, context.Profile.BuffRow.IconGap) * iconScale * rowScale;
        var maxIcons = debuffs
            ? Math.Clamp(context.Profile.DebuffRow.MaxIcons, 1, 24)
            : Math.Clamp(context.Profile.BuffRow.MaxIcons, 1, 24);
        var x = layout.Position.X;
        var y = layout.Position.Y;
        var rowWidth = layout.Size.X;
        var drawn = 0;
        var localId = (uint)context.Tracked.ObjectId;
        var isPreview = context.Tracked.Address == nint.Zero;
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

            if (!isPreview && onlyMine && status.SourceId != localId)
            {
                continue;
            }

            if (!isPreview && whitelist.Count > 0 && !whitelist.Contains(status.StatusId))
            {
                continue;
            }

            if (!isPreview && blacklist.Contains(status.StatusId))
            {
                continue;
            }

            var minX = debuffs
                ? x + rowWidth - iconWidth - ((iconWidth + gap) * drawn)
                : x + ((iconWidth + gap) * drawn);
            var min = new Vector2(minX, y);
            var max = min + new Vector2(iconWidth, iconHeight);
            DrawStatusIcon(context, drawContext, min, max, status, debuffs, rowScale);
            drawn++;
        }
    }

    internal static void DrawStatusIcon(
        NameplateContext context,
        DrawContext drawContext,
        Vector2 min,
        Vector2 max,
        StatusSnapshot status,
        bool debuffs,
        float rowScale)
    {
        using var fontScope = GameFontRegistry.PushFont(context.FontFamilyId);
        var tint = 0xFFFFFFFF;

        if (status.IconId != 0)
        {
            ISharedImmediateTexture? texture = null;
            try
            {
                texture = context.TextureProvider.GetFromGameIcon(new GameIconLookup(status.IconId));
            }
            catch
            {
                texture = null;
            }

            if (texture is not null)
            {
                drawContext.DrawImage(texture, min + new Vector2(1f, 1f), max - new Vector2(1f, 1f), tint);
            }
            else
            {
                drawContext.DrawFilledRect(min + new Vector2(1f, 1f), max - new Vector2(1f, 1f), debuffs ? 0x80A05050 : 0x805080A0, 2f);
            }
        }
        else
        {
            drawContext.DrawFilledRect(min + new Vector2(1f, 1f), max - new Vector2(1f, 1f), debuffs ? 0x80A05050 : 0x805080A0, 2f);
        }

        if (status.StackCount > 1)
        {
            var stackFontSize = Math.Max(10f, 11f * rowScale);
            drawContext.DrawText(
                new Vector2(max.X - (9f * rowScale), max.Y - (13f * rowScale)),
                0xFFFFFFFF,
                status.StackCount.ToString(),
                stackFontSize);
        }

        if (status.RemainingTime > 0.05f)
        {
            var timeText = status.RemainingTime >= 10f
                ? $"{MathF.Floor(status.RemainingTime):0}"
                : $"{status.RemainingTime:0.0}";
            var timerFontSize = Math.Max(10f, 11f * rowScale);
            var baseFontSize = Math.Max(1f, ImGui.GetFontSize());
            var timerSize = ImGui.CalcTextSize(timeText) * (timerFontSize / baseFontSize);
            var platePadX = 3f * rowScale;
            var platePadY = 1f * rowScale;
            var plateWidth = timerSize.X + (platePadX * 2f);
            var plateHeight = timerSize.Y + (platePadY * 2f);
            var plateMin = new Vector2(
                min.X + ((max.X - min.X - plateWidth) * 0.5f),
                max.Y - 1f);
            var plateMax = new Vector2(plateMin.X + plateWidth, plateMin.Y + plateHeight);
            drawContext.DrawFilledRect(plateMin, plateMax, 0xC0000000, 2.5f);
            drawContext.DrawBorder(plateMin, plateMax, 0x70000000, 2.5f, 1f);

            var timerPos = new Vector2(plateMin.X + platePadX, plateMin.Y + platePadY - 0.5f);
            drawContext.DrawText(new Vector2(timerPos.X - 1f, timerPos.Y), 0xE0000000, timeText, timerFontSize);
            drawContext.DrawText(new Vector2(timerPos.X + 1f, timerPos.Y), 0xE0000000, timeText, timerFontSize);
            drawContext.DrawText(new Vector2(timerPos.X, timerPos.Y - 1f), 0xE0000000, timeText, timerFontSize);
            drawContext.DrawText(new Vector2(timerPos.X, timerPos.Y + 1f), 0xE0000000, timeText, timerFontSize);
            drawContext.DrawText(timerPos, GetTimerColor(status.RemainingTime), timeText, timerFontSize);
        }
    }

    private static uint GetTimerColor(float remainingTime)
    {
        if (remainingTime < 3f)
        {
            return 0xFF5A5AFF;
        }

        if (remainingTime <= 5f)
        {
            return 0xFF5AD9FF;
        }

        return 0xFF7DFF7D;
    }
}
