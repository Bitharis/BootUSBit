using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Wpf;

/// <summary>Loads the "Logging" section of appsettings.json into <see cref="FileLoggerOptions"/>.</summary>
public static class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static FileLoggerOptions LoadFileLoggerOptions()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
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

    private sealed class AppSettingsDocument
    {
        public FileLoggerOptions? Logging { get; set; }
    }
}
