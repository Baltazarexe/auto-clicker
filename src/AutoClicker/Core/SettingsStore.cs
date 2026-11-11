using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoClicker.Core;

public sealed class SettingsStore(string path)
{
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoClicker", "settings.json");

    public string FilePath { get; } = path;

    public AppSettings Load()
    {
        var d = AppSettings.Default;
        JsonObject? obj;
        try
        {
            if (!File.Exists(FilePath)) return d;
            obj = JsonNode.Parse(File.ReadAllText(FilePath)) as JsonObject;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return d;
        }
        if (obj is null) return d;

        try
        {
            int cps = TryGet(obj, "cps", out long c) ? AppSettings.ClampCps(c) : d.Cps;
            var button = Enum.TryParse<ClickButton>(Str(obj, "button"), out var b) && Enum.IsDefined(b) ? b : d.Button;
            var mode = Enum.TryParse<ClickMode>(Str(obj, "mode"), out var m) && Enum.IsDefined(m) ? m : d.Mode;
            var hotkey = Hotkey.TryParse(Str(obj, "hotkey"), out var h) && h.IsAllowedAsHotkey ? h : d.Hotkey;
            return new AppSettings(cps, button, mode, hotkey);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException)
        {
            // E.g. a duplicate key in the JSON makes the indexer throw ArgumentException.
            return d;
        }
    }

    public void Save(AppSettings s)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
            var obj = new JsonObject
            {
                ["cps"] = s.Cps,
                ["button"] = s.Button.ToString(),
                ["mode"] = s.Mode.ToString(),
                ["hotkey"] = s.Hotkey.ToString(),
            };
            File.WriteAllText(FilePath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Saving is best-effort: a disk failure must not crash the app.
        }
    }

    private static string? Str(JsonObject o, string key)
    {
        try { return o[key]?.GetValue<string>(); } catch (InvalidOperationException) { return null; } catch (FormatException) { return null; }
    }

    private static bool TryGet(JsonObject o, string key, out long value)
    {
        value = 0;
        try
        {
            if (o[key] is JsonValue v && v.TryGetValue(out long l)) { value = l; return true; }
            if (o[key] is JsonValue v2 && v2.TryGetValue(out double dbl)) { value = (long)Math.Clamp(dbl, long.MinValue, long.MaxValue); return true; }
        }
        catch (InvalidOperationException) { }
        return false;
    }
}
