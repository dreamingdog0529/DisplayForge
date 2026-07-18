using System.Text.Json;
using System.Text.Json.Serialization;
using DisplayForge.Core.Models;

namespace DisplayForge.Core.Services;

public sealed class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _profilesPath;
    private readonly string _settingsPath;

    public JsonProfileStore(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DisplayForge");
        Directory.CreateDirectory(DataDirectory);
        _profilesPath = Path.Combine(DataDirectory, "profiles.json");
        _settingsPath = Path.Combine(DataDirectory, "settings.json");
    }

    public string DataDirectory { get; }

    public ProfileCollection LoadProfiles()
    {
        if (!File.Exists(_profilesPath))
            return new ProfileCollection();

        try
        {
            var json = File.ReadAllText(_profilesPath);
            return JsonSerializer.Deserialize<ProfileCollection>(json, JsonOptions)
                   ?? new ProfileCollection();
        }
        catch
        {
            return new ProfileCollection();
        }
    }

    public void SaveProfiles(ProfileCollection collection)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(collection, JsonOptions);
        var temp = _profilesPath + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _profilesPath, overwrite: true);
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        Directory.CreateDirectory(DataDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var temp = _settingsPath + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _settingsPath, overwrite: true);
    }
}
