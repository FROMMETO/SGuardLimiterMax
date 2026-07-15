using System.IO;
using System.Text.Json;
using SGuardLimiterMax.Models;

namespace SGuardLimiterMax.Services;

/// <summary>
/// Manages portable Config.json stored alongside the executable.
/// No AppData, no Registry — fully portable.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        AppContext.BaseDirectory, "Config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Loads config from disk. Creates a default file if none exists.
    /// </summary>
    public static AppConfig Load() => Load(ConfigPath);

    /// <summary>
    /// Loads config from the specified path. Creates a default file if none exists.
    /// Exposed for unit testing.
    /// </summary>
    internal static AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = new AppConfig();
            Save(defaults, path);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            var defaults = new AppConfig();
            Save(defaults, path);
            return defaults;
        }
    }

    /// <summary>
    /// Persists the current config state to disk.
    /// </summary>
    public static void Save(AppConfig config) => Save(config, ConfigPath);

    /// <summary>
    /// Persists the config to the specified path.
    /// Exposed for unit testing.
    /// </summary>
    internal static void Save(AppConfig config, string path)
    {
        try
        {
            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
