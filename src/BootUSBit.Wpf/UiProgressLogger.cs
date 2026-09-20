using System.Windows;
using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Wpf;

/// <summary>Marshals Info-and-above log messages from background work onto the UI thread; file logging handles the rest.</summary>
public sealed class UiProgressLogger : IProgressLogger
{
    private readonly Action<string> _append;

    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    public UiProgressLogger(Action<string> append)
    {
        _append = append;
    }

    public void Log(LogLevel level, string message)
    {
        if (level < MinimumLevel)
        {
            return;
        }

        var line = level >= LogLevel.Warn ? $"{level.ToString().ToUpperInvariant()}: {message}" : message;
        Application.Current.Dispatcher.Invoke(() => _append(line));
    }
}
