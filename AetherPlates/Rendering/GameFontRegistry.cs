using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using System.IO;

namespace FFXIVHudPlugin.AetherPlates.Rendering;

public static class GameFontRegistry
{
    private const int CustomFontIdBase = 1000;
    private static readonly Dictionary<int, IFontHandle> HandlesByFontId = new();
    private static readonly Dictionary<int, string> LabelsByFontId = new();
    private static readonly List<int> OrderedFontIds = new();
    private static readonly List<string> FontSearchDirectories = new();
    private static IUiBuilder? uiBuilder;
    private static IPluginLog? pluginLog;
    private static bool initialized;

    public static void Initialize(
        IUiBuilder uiBuilder,
        IPluginLog pluginLog,
        string? pluginAssemblyDirectory,
        string? pluginConfigDirectory)
    {
        GameFontRegistry.uiBuilder = uiBuilder;
        GameFontRegistry.pluginLog = pluginLog;
        BuildSearchDirectories(pluginAssemblyDirectory, pluginConfigDirectory);
        initialized = true;
        RebuildHandles(out _);
    }

    public static int NormalizeFamilyId(int familyId)
    {
        if (familyId <= 0)
        {
            return 0;
        }

        return HandlesByFontId.ContainsKey(familyId) ? familyId : 0;
    }

    public static bool Reload(out string message)
    {
        if (!initialized || uiBuilder is null || pluginLog is null)
        {
            message = "Fonts are not initialized yet.";
            return false;
        }

        return RebuildHandles(out message);
    }

    public static (int[] ids, string[] labels) GetFontOptions()
    {
        if (OrderedFontIds.Count == 0)
        {
            return (new[] { 0 }, new[] { "Default (Dalamud)" });
        }

        var ids = OrderedFontIds.ToArray();
        var labels = ids.Select(id => LabelsByFontId.TryGetValue(id, out var label)
            ? label
            : $"Font {id}")
            .ToArray();
        return (ids, labels);
    }

    public static IDisposable? PushFont(int familyId)
    {
        familyId = NormalizeFamilyId(familyId);
        if (familyId == 0 || !HandlesByFontId.TryGetValue(familyId, out var handle))
        {
            return null;
        }

        return new FontScope(handle);
    }

    private static bool RebuildHandles(out string message)
    {
        if (uiBuilder is null || pluginLog is null)
        {
            message = "Font system is unavailable.";
            return false;
        }

        DisposeHandles();
        OrderedFontIds.Clear();
        LabelsByFontId.Clear();

        OrderedFontIds.Add(0);
        LabelsByFontId[0] = "Default (Dalamud)";

        var loadedGameFonts = 0;
        foreach (var family in Enum.GetValues<GameFontFamily>().Where(value => (int)value > 0).OrderBy(value => (int)value))
        {
            if (IsHiddenGameFontFamily(family))
            {
                continue;
            }

            var familyId = (int)family;
            try
            {
                var style = new GameFontStyle(family, 16f);
                var handle = uiBuilder.FontAtlas.NewGameFontHandle(style);
                HandlesByFontId[familyId] = handle;
                OrderedFontIds.Add(familyId);
                LabelsByFontId[familyId] = $"{GetFriendlyGameFontName(family)} ({familyId})";
                loadedGameFonts++;
            }
            catch (Exception ex)
            {
                pluginLog.Warning(ex, $"Failed to initialize game font family {family} ({familyId}).");
            }
        }

        var customFiles = EnumerateCustomFontFiles(out var scanSummary).ToArray();
        var bundledFonts = SelectBundledFontRepresentatives(customFiles).ToArray();
        var loadedCustomFonts = 0;
        for (var i = 0; i < bundledFonts.Length; i++)
        {
            var fontId = CustomFontIdBase + i;
            var path = bundledFonts[i].Path;
            try
            {
                var handle = uiBuilder.FontAtlas.NewDelegateFontHandle(
                    e => e.OnPreBuild(
                        tk => tk.AddFontFromFile(
                            path,
                            new SafeFontConfig
                            {
                                SizePx = 16f,
                            })));
                HandlesByFontId[fontId] = handle;
                OrderedFontIds.Add(fontId);
                LabelsByFontId[fontId] = bundledFonts[i].DisplayName;
                loadedCustomFonts++;
            }
            catch (Exception ex)
            {
                pluginLog.Warning(ex, $"Failed to load bundled font '{path}'.");
            }
        }

        message = loadedCustomFonts > 0
            ? $"Loaded {loadedGameFonts} game fonts and {loadedCustomFonts} bundled font families. {scanSummary}"
            : $"Loaded {loadedGameFonts} game fonts. {scanSummary}";
        return true;
    }

    private static void DisposeHandles()
    {
        foreach (var handle in HandlesByFontId.Values)
        {
            handle.Dispose();
        }

        HandlesByFontId.Clear();
    }

    private static IEnumerable<string> EnumerateCustomFontFiles(out string summary)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parts = new List<string>();

        try
        {
            foreach (var directory in FontSearchDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    parts.Add($"{directory} (missing)");
                    continue;
                }

                try
                {
                    var ttf = Directory.EnumerateFiles(directory, "*.ttf", SearchOption.AllDirectories);
                    var otf = Directory.EnumerateFiles(directory, "*.otf", SearchOption.AllDirectories);
                    var local = ttf.Concat(otf).ToArray();
                    foreach (var file in local)
                    {
                        files.Add(file);
                    }

                    parts.Add($"{directory} ({local.Length} file(s))");
                }
                catch (UnauthorizedAccessException)
                {
                    parts.Add($"{directory} (access denied)");
                }
                catch (IOException)
                {
                    parts.Add($"{directory} (io error)");
                }
            }

            summary = $"Scanned: {string.Join("; ", parts)}";
            return files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            summary = "Scanned bundled font paths, but access was denied.";
            return Array.Empty<string>();
        }
        catch (IOException)
        {
            summary = "Scanned bundled font paths, but an IO error occurred.";
            return Array.Empty<string>();
        }
    }

    private static void BuildSearchDirectories(string? pluginAssemblyDirectory, string? pluginConfigDirectory)
    {
        FontSearchDirectories.Clear();

        static void AddIfValid(List<string> list, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var normalized = Path.GetFullPath(path);
            if (!list.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(normalized);
            }
        }

        var assemblyDir = string.IsNullOrWhiteSpace(pluginAssemblyDirectory)
            ? Path.GetDirectoryName(typeof(GameFontRegistry).Assembly.Location)
            : pluginAssemblyDirectory;
        var baseDir = AppContext.BaseDirectory;

        AddIfValid(FontSearchDirectories, Path.Combine(assemblyDir ?? string.Empty, "fonts"));
        AddIfValid(FontSearchDirectories, Path.Combine(baseDir, "fonts"));
        AddIfValid(FontSearchDirectories, Path.Combine(baseDir, "FFXIVHudPlugin", "fonts"));
        AddIfValid(FontSearchDirectories, Path.Combine(pluginConfigDirectory ?? string.Empty, "fonts"));
    }

    private static string GetFriendlyGameFontName(GameFontFamily family)
    {
        var raw = family.ToString();
        var mapped = raw switch
        {
            "Axis" => "Axis",
            "Jupiter" => "Jupiter",
            "JupiterNumeric" => "Jupiter Numeric",
            "Meidinger" => "Meidinger",
            "MeidingerMid" => "Meidinger Mid",
            "MiedingerMid" => "Miedinger Mid",
            "TrumpGothic" => "Trump Gothic",
            _ => raw,
        };

        return string.Concat(mapped.Select((ch, index) =>
            index > 0 && char.IsUpper(ch) && !char.IsUpper(mapped[index - 1])
                ? $" {ch}"
                : ch.ToString()));
    }

    private static bool IsHiddenGameFontFamily(GameFontFamily family)
    {
        return family is GameFontFamily.JupiterNumeric
            or GameFontFamily.Meidinger
            or GameFontFamily.TrumpGothic;
    }

    private static IEnumerable<(string DisplayName, string Path)> SelectBundledFontRepresentatives(IEnumerable<string> files)
    {
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var familyKey = GetBundledFamilyKey(file);
            if (!grouped.TryGetValue(familyKey, out var list))
            {
                list = new List<string>();
                grouped[familyKey] = list;
            }

            list.Add(file);
        }

        foreach (var pair in grouped.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var best = pair.Value
                .OrderBy(GetFontVariantScore)
                .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .First();
            yield return (pair.Key, best);
        }
    }

    private static int GetFontVariantScore(string path)
    {
        var file = Path.GetFileNameWithoutExtension(path);
        if (file.Contains("VariableFont", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (file.Contains("Regular", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (file.Contains("Medium", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (file.Contains("SemiBold", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (file.Contains("Bold", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (file.Contains("Italic", StringComparison.OrdinalIgnoreCase))
        {
            return 8;
        }

        return 4;
    }

    private static string GetBundledFamilyKey(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        var parent = Path.GetFileName(dir);
        if (string.Equals(parent, "static", StringComparison.OrdinalIgnoreCase))
        {
            parent = Path.GetFileName(Path.GetDirectoryName(dir) ?? string.Empty);
        }

        var fallback = Path.GetFileNameWithoutExtension(path);
        var raw = string.IsNullOrWhiteSpace(parent) || string.Equals(parent, "fonts", StringComparison.OrdinalIgnoreCase)
            ? fallback
            : parent;

        var normalized = raw.Replace('_', ' ').Trim();
        return normalized.Length == 0 ? fallback : normalized;
    }

    private sealed class FontScope : IDisposable
    {
        private readonly IFontHandle handle;
        private bool disposed;

        public FontScope(IFontHandle handle)
        {
            this.handle = handle;
            this.handle.Push();
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.handle.Pop();
        }
    }
}
