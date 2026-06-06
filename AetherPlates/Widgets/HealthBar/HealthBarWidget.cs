using FFXIVHudPlugin.AetherPlates.Data;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Rendering;
using System.Collections.Concurrent;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.Widgets.HealthBar;

public sealed class HealthBarWidget : INameplateWidget
{
    private sealed class SmoothState
    {
        public float Displayed;
        public float DamageTrail = 1f;
    }

    private static readonly ConcurrentDictionary<ulong, SmoothState> SmoothStates = new();
    public string Id => "health_bar";

    public Vector2 GetDesiredSize(NameplateContext context)
    {
        return new Vector2(context.Profile.HealthBar.Width, context.Profile.HealthBar.Height);
    }

    public void Draw(NameplateContext context, DrawContext drawContext, WidgetLayout layout)
    {
        var currentRatio = context.Tracked.MaxHp == 0 ? 0f : context.Tracked.CurrentHp / (float)context.Tracked.MaxHp;
        currentRatio = Math.Clamp(currentRatio, 0f, 1f);
        var state = SmoothStates.GetOrAdd(context.Tracked.ObjectId, _ => new SmoothState { Displayed = currentRatio, DamageTrail = currentRatio });
        state.Displayed = Lerp(state.Displayed, currentRatio, 0.20f);
        state.DamageTrail = state.DamageTrail < state.Displayed
            ? state.Displayed
            : Lerp(state.DamageTrail, state.Displayed, 0.08f);

        var min = layout.Position;
        var max = layout.Position + layout.Size;
        var width = max.X - min.X;
        var fillMax = new Vector2(min.X + width * state.Displayed, max.Y);
        var damageMax = new Vector2(min.X + width * state.DamageTrail, max.Y);
        var radius = 4f;

        var healthColor = context.ActiveStyle?.HealthColor ?? 0xFF4AB34A;
        var healthBackgroundColor = context.ActiveStyle?.HealthBackgroundColor ?? context.Profile.HealthBar.BackgroundColor;
        drawContext.DrawFilledRect(min, max, healthBackgroundColor, radius);
        if (damageMax.X > fillMax.X + 0.5f)
        {
            drawContext.DrawFilledRect(new Vector2(fillMax.X, min.Y), damageMax, 0xAA2F2FFF, radius);
        }

        drawContext.DrawFilledRect(min, fillMax, healthColor, radius);
        drawContext.DrawBorder(min, max, context.Profile.HealthBar.BorderColor, radius, 1.3f);

        var shieldMax = min.X + width * Math.Clamp(state.Displayed + context.Tracked.ShieldRatio, 0f, 1f);
        if (shieldMax > fillMax.X + 1f)
        {
            drawContext.DrawFilledRect(new Vector2(fillMax.X, min.Y), new Vector2(shieldMax, max.Y), 0x664AB3E8, radius);
        }
    }

    private static float Lerp(float from, float to, float amount)
    {
        return from + ((to - from) * Math.Clamp(amount, 0f, 1f));
    }
}
