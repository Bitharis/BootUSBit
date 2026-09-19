using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Tests.Diagnostics;

public class FileProgressLoggerTests
{
    [Fact]
    public void Log_WritesLine_WhenAtOrAboveMinimumLevel()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var logFile = Path.Combine(dir, "test.log");
            IProgressLogger logger = new FileProgressLogger(new FileLoggerOptions { FilePath = logFile, MinimumLevel = LogLevel.Info });

            logger.Info("hello");

            var lines = File.ReadAllLines(logFile);
            Assert.Single(lines);
            Assert.Contains("[INFO ]", lines[0]);
            Assert.Contains("hello", lines[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Log_SkipsLine_WhenBelowMinimumLevel()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var logFile = Path.Combine(dir, "test.log");
            IProgressLogger logger = new FileProgressLogger(new FileLoggerOptions { FilePath = logFile, MinimumLevel = LogLevel.Info });

            logger.Debug("should not appear");

            Assert.False(File.Exists(logFile));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Log_DefaultMinimumLevel_IsInfo()
    {
        Assert.Equal(LogLevel.Info, new FileLoggerOptions().MinimumLevel);
    }

    [Fact]
    public void Log_RollsOverFile_WhenSizeLimitExceeded()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var logFile = Path.Combine(dir, "test.log");
            IProgressLogger logger = new FileProgressLogger(new FileLoggerOptions
            {
                FilePath = logFile,
                MinimumLevel = LogLevel.Trace,
                MaxFileSizeBytes = 10,
                RetainedFileCount = 2,
            });

            logger.Info("first message long enough to exceed limit");
            logger.Info("second message");

            Assert.True(File.Exists(logFile));
            Assert.True(File.Exists($"{logFile}.1"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
