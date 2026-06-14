using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapMapPathResolver
    {
        public static unsafe List<string> BuildCandidates(uint mapRowId, Map? mapRow, AgentMap* agentMap)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var candidates = new List<string>(20);

            if (agentMap is not null)
            {
                TryAdd(agentMap->CurrentMapPath.ToString(), seen, candidates);
                TryAdd(agentMap->SelectedMapPath.ToString(), seen, candidates);
                TryAdd(agentMap->CurrentMapBgPath.ToString(), seen, candidates);
                TryAdd(agentMap->SelectedMapBgPath.ToString(), seen, candidates);
            }

            if (mapRowId != 0)
            {
                var mapKey = mapRow.HasValue
                    ? mapRow.Value.Id.ToString().Replace("/", string.Empty, StringComparison.Ordinal)
                    : mapRowId.ToString();
                if (string.IsNullOrWhiteSpace(mapKey))
                {
                    mapKey = mapRowId.ToString();
                }

                TryAdd($"ui/map/{mapRowId}/{mapKey}.tex", seen, candidates);
                TryAdd($"ui/map/{mapKey}/{mapKey}.tex", seen, candidates);
                TryAdd($"ui/map/{mapRowId}/{mapKey}_s.tex", seen, candidates);
                TryAdd($"ui/map/{mapRowId}/{mapKey}_l.tex", seen, candidates);
                TryAdd($"ui/map/{mapRowId}/{mapKey}_m.tex", seen, candidates);
            }

            return candidates;
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

            return normalized.EndsWith("m_m", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryAdd(string path, HashSet<string> seen, List<string> candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && seen.Add(path))
            {
                candidates.Add(path);
            }
        }
    }
}
