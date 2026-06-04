using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

public sealed class LimitBreakTracker
{
    public LimitBreakViewModel GetState(int partyLength, bool isInDuty)
    {
        var partyMaxSegments = partyLength >= 8 ? 3 : partyLength >= 4 ? 2 : 0;
        if (TryReadNativeGauge(out var nativeFill, out var nativeMaxSegments, out var hasController, out var controllerUnits, out var controllerBarUnits))
        {
            return new LimitBreakViewModel
            {
                SegmentFill = nativeFill,
                // Trust native LB bar count when available (supports Duty Support/Trust contexts).
                MaxSegments = nativeMaxSegments,
            };
        }

        // Duty Support / Trust edge-case:
        // Sometimes party-list count is not populated, but LB controller data exists.
        // If we have controller units + per-bar units, infer a light-party (2 chunk) layout.
        if (isInDuty && hasController && controllerBarUnits > 0 && partyMaxSegments <= 0)
        {
            var inferredSegments = 2;
            var inferredFill = new[] { 0f, 0f, 0f };
            for (var i = 0; i < inferredSegments; i++)
            {
                var segmentUnits = Math.Clamp(controllerUnits - (i * controllerBarUnits), 0, controllerBarUnits);
                inferredFill[i] = segmentUnits / (float)controllerBarUnits;
            }

            return new LimitBreakViewModel
            {
                SegmentFill = inferredFill,
                MaxSegments = inferredSegments,
            };
        }

        return new LimitBreakViewModel
        {
            // Do not self-charge with synthetic estimates.
            // Until a true native LB source is wired, keep the gauge empty.
            SegmentFill = new[] { 0f, 0f, 0f },
            MaxSegments = partyMaxSegments,
        };
    }

    private static unsafe bool TryReadNativeGauge(out float[] segmentFill, out int maxSegments, out bool hasController, out int currentUnits, out int barUnits)
    {
        segmentFill = new[] { 0f, 0f, 0f };
        maxSegments = 0;
        hasController = false;
        currentUnits = 0;
        barUnits = 0;

        // Use the same source DelvUI uses for LB: LimitBreakController units and bar count.
        var lbController = LimitBreakController.Instance();
        if (lbController is null)
        {
            return false;
        }
        hasController = true;

        var barCount = Math.Clamp((int)lbController->BarCount, 0, 3);
        barUnits = (int)lbController->BarUnits;
        if (barCount <= 0)
        {
            return false;
        }
        if (barUnits <= 0)
        {
            return false;
        }

        currentUnits = Math.Max(0, (int)lbController->CurrentUnits);
        maxSegments = barCount;
        for (var i = 0; i < barCount && i < 3; i++)
        {
            var segmentUnits = Math.Clamp(currentUnits - (i * barUnits), 0, barUnits);
            segmentFill[i] = segmentUnits / (float)barUnits;
        }

        return true;
    }
}
