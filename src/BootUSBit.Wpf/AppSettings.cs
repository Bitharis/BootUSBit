using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Wpf;

/// <summary>Loads the "Logging" section of appsettings.json into <see cref="FileLoggerOptions"/>.</summary>
public static class AppSettings
{
    private static string UserSettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BootUSBit",
        "appsettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static FileLoggerOptions LoadFileLoggerOptions()
    {
        var path = File.Exists(UserSettingsPath)
            ? UserSettingsPath
            : Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new FileLoggerOptions();
        }

        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<AppSettingsDocument>(stream, JsonOptions);
            return document?.Logging ?? new FileLoggerOptions();
        }
        catch (JsonException)
        {
            // Malformed appsettings.json shouldn't prevent the app from starting; fall back to defaults.
            return new FileLoggerOptions();
        }
    }

    public static void SaveFileLoggerOptions(FileLoggerOptions options)
    {
        var directory = Path.GetDirectoryName(UserSettingsPath)!;
        Directory.CreateDirectory(directory);
        var document = new AppSettingsDocument { Logging = options };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(UserSettingsPath, json);
    }

    private sealed class AppSettingsDocument
    {
        public FileLoggerOptions? Logging { get; set; }
    }
}
