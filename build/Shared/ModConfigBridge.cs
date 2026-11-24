using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Sts2Mods.Shared;

/// <summary>
/// Zero-dependency JSON configuration storage with soft detection of ModConfig-STS2.
/// </summary>
public static class ModConfigBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly ConcurrentDictionary<string, string> PendingJson = new();
    private static readonly ConcurrentDictionary<string, int> ScheduledFlags = new();

    public static bool IsModConfigLoaded()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = asm.GetName().Name;
            if (!string.IsNullOrEmpty(name) && name.Contains("ModConfig", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static string GetConfigPath(string modId)
    {
        var dir = Path.Combine(OS.GetUserDataDir(), "mod_configs");
        return Path.Combine(dir, $"{modId}.json");
    }

    public static T LoadJsonConfig<T>(string modId, T defaultValue) where T : class
    {
        try
        {
            var path = GetConfigPath(modId);
            if (!File.Exists(path))
                return defaultValue;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public static void SaveJsonConfig<T>(string modId, T config) where T : class
    {
        var path = GetConfigPath(modId);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static void SaveJsonConfigDeferred<T>(string modId, T config) where T : class
    {
        try
        {
            var path = GetConfigPath(modId);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            PendingJson[path] = json;

            if (ScheduledFlags.TryAdd(path, 1))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                        if (PendingJson.TryRemove(path, out var latest))
                        {
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(dir))
                                Directory.CreateDirectory(dir);
                            File.WriteAllText(path, latest);
                        }
                    }
                    catch
                    {
                        // Ignore background save errors
                    }
                    finally
                    {
                        ScheduledFlags.TryRemove(path, out _);
                        // If a newer write arrived right before removal, flush it immediately
                        if (PendingJson.TryRemove(path, out var trailing))
                        {
                            try { File.WriteAllText(path, trailing); } catch { }
                        }
                    }
                });
            }
        }
        catch
        {
            // Ignore serialization errors
        }
    }
}
