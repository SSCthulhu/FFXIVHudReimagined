using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace FFXIVHudPlugin;

/// <summary>
/// Official FFXIV role colors for the minimap player pin (tank / healer / DPS / crafter-gatherer).
/// </summary>
internal static class MinimapRoleColor
{
    // ImGui ABGR (0xAARRGGBB) from the game's role color palette.
    public const uint TankArgb = 0xFF803A2D;       // #2d3a80 — (45, 58, 128)
    public const uint HealerArgb = 0xFF246634;     // #346624 — (52, 102, 36)
    public const uint DpsArgb = 0xFF282873;        // #732828 — (115, 40, 40)
    public const uint CrafterGathererArgb = 0xFF4CA3BF; // #bfa34c — (191, 163, 76)

    private static readonly HashSet<string> CrafterGathererAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL",
        "MIN", "BTN", "FSH",
    };

    public static bool TryResolveArgb(IDataManager dataManager, uint classJobId, out uint argb)
    {
        argb = 0;
        if (classJobId == 0)
        {
            return false;
        }

        var classJobSheet = dataManager.GetExcelSheet<ClassJob>();
        if (classJobSheet is null || !classJobSheet.TryGetRow(classJobId, out var classJob))
        {
            return false;
        }

        var abbrev = classJob.Abbreviation.ToString();
        if (CrafterGathererAbbreviations.Contains(abbrev))
        {
            argb = CrafterGathererArgb;
            return true;
        }

        var roleSheet = dataManager.GetExcelSheet<Role>();
        if (roleSheet is null || classJob.Role == 0 || !roleSheet.TryGetRow(classJob.Role, out var roleRow))
        {
            return false;
        }

        // Role.Type in sheet data: 1 = tank, 2 = DPS, 3 = healer (not 2 = healer / 3 = DPS).
        argb = roleRow.Type switch
        {
            1 => TankArgb,
            2 => DpsArgb,
            3 => HealerArgb,
            _ => 0,
        };
        return argb != 0;
    }
}
