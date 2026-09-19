namespace BootUSBit.Core.Diagnostics;

/// <summary>Fans a single log call out to multiple sinks (e.g. UI + file) so callers only depend on one IProgressLogger.</summary>
public sealed class CompositeProgressLogger : IProgressLogger
{
    private readonly IReadOnlyList<IProgressLogger> _loggers;

    public CompositeProgressLogger(params IReadOnlyList<IProgressLogger> loggers)
    {
        _loggers = loggers;
    }

    public void Log(LogLevel level, string message)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(level, message);
        }
    }
}
