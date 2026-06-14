using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DelvUI.Interface.GeneralElements
{
    internal sealed class MinimapMarkerIconCache
    {
        private readonly Dictionary<uint, ISharedImmediateTexture?> _icons = new(32);
        private int _loadsThisFrame;

        public void BeginFrame()
        {
            _icons.Clear();
            _loadsThisFrame = 0;
        }

        public ISharedImmediateTexture? TryGetIcon(uint iconId)
        {
            if (iconId == 0)
            {
                return null;
            }

            if (_icons.TryGetValue(iconId, out var cached))
            {
                return cached;
            }

            if (_loadsThisFrame >= MinimapLayout.MaxNativeMarkerIconLoadsPerFrame)
            {
                return null;
            }

            _loadsThisFrame++;
            var texture = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId));
            _icons[iconId] = texture;
            return texture;
        }

        public bool TryGetDrawableIcon(uint iconId, out ISharedImmediateTexture? texture)
        {
            texture = TryGetIcon(iconId);
            return MinimapTextureUtil.IsDrawable(texture);
        }
    }

    internal sealed class MinimapMapTextureCache
    {
        private string _cachedTexturePath = string.Empty;
        private ISharedImmediateTexture? _cachedTexture;
        private readonly List<string> _lastCandidatePaths = new(32);

        public string LastLoadedPath { get; private set; } = string.Empty;
        public string LastLoadNote { get; private set; } = string.Empty;

        public IReadOnlyList<string> GetLastCandidatePaths(int maxCount) =>
            _lastCandidatePaths.Take(Math.Max(maxCount, 0)).ToList();

        public unsafe bool TryGetCurrentMapTexture(out ISharedImmediateTexture? texture)
        {
            texture = null;
            _lastCandidatePaths.Clear();
            LastLoadedPath = string.Empty;
            LastLoadNote = "No path produced a drawable texture.";

            if (MinimapNativeMapTexture.TryGetMapImagePath(out var nativeMapPath) &&
                TryLoadPath(nativeMapPath, "Loaded from _NaviMap MapImage.", out texture))
            {
                return true;
            }

            var agentMap = AgentMap.Instance();
            var mapRowId = ResolveMapRowId(agentMap);
            Map? mapRow = null;
            if (mapRowId != 0)
            {
                var sheet = Plugin.DataManager.GetExcelSheet<Map>();
                if (sheet is not null && sheet.TryGetRow(mapRowId, out var row))
                {
                    mapRow = row;
                }
            }

            foreach (var candidate in MinimapMapPathResolver.BuildCandidates(mapRowId, mapRow, agentMap))
            {
                _lastCandidatePaths.Add(candidate);
                if (!TryLoadPath(candidate, "Loaded from AgentMap/Lumina path.", out var loaded))
                {
                    continue;
                }

                if (MinimapMapPathResolver.IsMaskMapPath(candidate))
                {
                    continue;
                }

                texture = loaded;
                return true;
            }

            return false;
        }

        private bool TryLoadPath(string path, string successNote, out ISharedImmediateTexture? texture)
        {
            texture = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = NormalizeGamePath(path);
            if (!string.Equals(_cachedTexturePath, normalized, StringComparison.Ordinal))
            {
                _cachedTexturePath = normalized;
                _cachedTexture = Plugin.TextureProvider.GetFromGame(normalized);
            }

            if (!MinimapTextureUtil.IsDrawable(_cachedTexture))
            {
                return false;
            }

            texture = _cachedTexture;
            LastLoadedPath = _cachedTexturePath;
            LastLoadNote = successNote;
            return true;
        }

        private static unsafe uint ResolveMapRowId(AgentMap* agentMap) =>
            agentMap is not null && agentMap->CurrentMapId != 0 ? agentMap->CurrentMapId : 0;

        private static string NormalizeGamePath(string path) => path.Trim().Replace('\\', '/').TrimStart('/');
    }
}
