namespace BootUSBit.Core.Diagnostics;

/// <summary>Configuration for <see cref="FileProgressLogger"/>, typically bound from appsettings.json.</summary>
public sealed class FileLoggerOptions
{
    /// <summary>Minimum level written to the log file. Defaults to Info.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    /// <summary>Path to the log file. Relative paths are resolved against the app's base directory.</summary>
    public string FilePath { get; set; } = "logs/bootusbit.log";

    /// <summary>When the file exceeds this size, it is rolled over to "&lt;name&gt;.1.log" etc.</summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>How many rolled-over files to keep, oldest deleted first.</summary>
    public int RetainedFileCount { get; set; } = 5;
}
