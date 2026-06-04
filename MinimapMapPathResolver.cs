using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace FFXIVHudPlugin;

/// <summary>
/// Builds candidate game texture paths for the active minimap map image.
/// </summary>
internal static class MinimapMapPathResolver
{
    public static unsafe List<string> BuildCandidates(uint mapRowId, Map? mapRow, AgentMap* agentMap)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>(20);

        if (agentMap is not null)
        {
            AddAgentPaths(agentMap, seen, candidates);
        }

        AddLuminaPaths(mapRowId, mapRow, seen, candidates);
        return candidates;
    }

    private static unsafe void AddAgentPaths(AgentMap* agentMap, HashSet<string> seen, List<string> candidates)
    {
        // Base map first; *BgPath and *_m paths are masks/overlays, not the scrolling minimap image.
        TryAdd(agentMap->CurrentMapPath.ToString(), seen, candidates);
        TryAdd(agentMap->SelectedMapPath.ToString(), seen, candidates);
        TryAdd(agentMap->CurrentMapBgPath.ToString(), seen, candidates);
        TryAdd(agentMap->SelectedMapBgPath.ToString(), seen, candidates);
    }

    private static void AddLuminaPaths(uint mapRowId, Map? mapRow, HashSet<string> seen, List<string> candidates)
    {
        if (mapRowId == 0)
        {
            return;
        }

        var mapKey = mapRowId.ToString();
        if (mapRow.HasValue)
        {
            mapKey = mapRow.Value.Id.ToString().Replace("/", string.Empty, StringComparison.Ordinal);
            if (mapKey.Length == 0)
            {
                mapKey = mapRowId.ToString();
            }
        }

        TryAdd($"ui/map/{mapRowId}/{mapKey}.tex", seen, candidates);
        TryAdd($"ui/map/{mapKey}/{mapKey}.tex", seen, candidates);
        TryAdd($"ui/map/{mapRowId}/{mapKey}_s.tex", seen, candidates);
        TryAdd($"ui/map/{mapRowId}/{mapKey}_l.tex", seen, candidates);
        TryAdd($"ui/map/{mapRowId}/{mapKey}_m.tex", seen, candidates);
    }

    public static bool IsMaskMapPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/').TrimEnd();
        if (normalized.EndsWith(".tex", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }
        else if (normalized.EndsWith(".atex", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^5];
        }

        // Mask/minimap overlay paths use a trailing "m_m" segment (e.g. s1f100m_m), not base map "_m" (e.g. s1f100_m).
        return normalized.EndsWith("m_m", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryAdd(string path, HashSet<string> seen, List<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (seen.Add(path))
        {
            candidates.Add(path);
        }
    }
}
