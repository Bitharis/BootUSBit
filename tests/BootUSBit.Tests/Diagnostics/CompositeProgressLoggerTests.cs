using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Tests.Diagnostics;

public class CompositeProgressLoggerTests
{
    private sealed class RecordingLogger : IProgressLogger
    {
        public readonly List<(LogLevel Level, string Message)> Entries = [];

        public void Log(LogLevel level, string message) => Entries.Add((level, message));
    }

    [Fact]
    public void Log_ForwardsToAllInnerLoggers()
    {
        var a = new RecordingLogger();
        var b = new RecordingLogger();
        IProgressLogger composite = new CompositeProgressLogger(a, b);

        composite.Warn("careful");

        Assert.Single(a.Entries);
        Assert.Single(b.Entries);
        Assert.Equal(LogLevel.Warn, a.Entries[0].Level);
        Assert.Equal("careful", b.Entries[0].Message);
    }
}
