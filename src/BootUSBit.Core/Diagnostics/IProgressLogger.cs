namespace BootUSBit.Core.Diagnostics;

/// <summary>Severity of a log message, ordered from most to least verbose.</summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    Fatal = 5,
}

/// <summary>Sink for human-readable progress/log messages emitted during long-running USB operations.</summary>
public interface IProgressLogger
{
    void Log(LogLevel level, string message);

    void Trace(string message) => Log(LogLevel.Trace, message);

    void Debug(string message) => Log(LogLevel.Debug, message);

    void Info(string message) => Log(LogLevel.Info, message);

    void Warn(string message) => Log(LogLevel.Warn, message);

    void Error(string message) => Log(LogLevel.Error, message);

    void Fatal(string message) => Log(LogLevel.Fatal, message);
}

public sealed class NullProgressLogger : IProgressLogger
{
    public static readonly NullProgressLogger Instance = new();

    public void Log(LogLevel level, string message) { }
}
