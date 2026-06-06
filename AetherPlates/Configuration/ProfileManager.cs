using System.Text.Json;

namespace FFXIVHudPlugin.AetherPlates.Configuration;

public sealed class ProfileManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string Export(NameplateProfile profile)
    {
        return JsonSerializer.Serialize(profile, JsonOptions);
    }

    public bool TryImport(string json, out NameplateProfile? profile)
    {
        profile = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            profile = JsonSerializer.Deserialize<NameplateProfile>(json, JsonOptions);
            return profile is not null;
        }
        catch
        {
            return false;
        }
    }

    public NameplateProfile Duplicate(NameplateProfile source, string newId, string newDisplayName)
    {
        var cloneJson = this.Export(source);
        if (!this.TryImport(cloneJson, out var clone) || clone is null)
        {
            clone = NameplateProfile.CreateDefault();
        }

        clone.Id = newId;
        clone.DisplayName = newDisplayName;
        return clone;
    }

    public NameplateProfile ResetToDefault()
    {
        return NameplateProfile.CreateDefault();
    }
}
