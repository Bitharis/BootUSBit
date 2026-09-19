namespace BootUSBit.Core.Diagnostics;

/// <summary>
/// Writes leveled log lines to a file, filtering out anything below <see cref="FileLoggerOptions.MinimumLevel"/>
/// and rolling the file over once it exceeds <see cref="FileLoggerOptions.MaxFileSizeBytes"/>.
/// </summary>
public sealed class FileProgressLogger : IProgressLogger
{
    private readonly object _sync = new();
    private readonly FileLoggerOptions _options;
    private readonly string _fullPath;

    public FileProgressLogger(FileLoggerOptions options)
    {
        _options = options;
        _fullPath = Path.IsPathRooted(options.FilePath)
            ? options.FilePath
            : Path.Combine(AppContext.BaseDirectory, options.FilePath);

        var directory = Path.GetDirectoryName(_fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Log(LogLevel level, string message)
    {
        if (level < _options.MinimumLevel)
        {
            return;
        }

        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level.ToString().ToUpperInvariant(),-5}] {message}";

        lock (_sync)
        {
            RollOverIfNeeded();
            File.AppendAllLines(_fullPath, [line]);
        }
    }

    private void RollOverIfNeeded()
    {
        var info = new FileInfo(_fullPath);
        if (!info.Exists || info.Length < _options.MaxFileSizeBytes)
        {
            return;
        }

        for (var i = _options.RetainedFileCount; i >= 1; i--)
        {
            var source = i == 1 ? _fullPath : $"{_fullPath}.{i - 1}";
            var destination = $"{_fullPath}.{i}";

            if (i == _options.RetainedFileCount)
            {
                File.Delete(destination);
            }

            if (File.Exists(source))
            {
                File.Move(source, destination, overwrite: true);
            }
        }
    }
}
