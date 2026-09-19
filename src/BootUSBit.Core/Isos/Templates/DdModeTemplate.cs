using System.Runtime.Versioning;

namespace BootUSBit.Core.Isos.Templates;

/// <summary>
/// Fallback for ISOs that no multiboot template recognizes: writes the ISO byte-for-byte to the whole
/// physical disk (like Rufus's "DD image" mode). This destroys any existing partitions/multiboot menu
/// and only a single ISO can be on the drive afterwards, so callers must confirm this explicitly.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DdModeTemplate
{
    public static async Task WriteAsync(string isoPath, int diskNumber, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var physicalDrivePath = $@"\\.\PHYSICALDRIVE{diskNumber}";

        await using var source = File.OpenRead(isoPath);
        await using var dest = new FileStream(physicalDrivePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);

        var buffer = new byte[4 * 1024 * 1024];
        long totalWritten = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalWritten += read;
            progress?.Report((double)totalWritten / source.Length);
        }

        await dest.FlushAsync(cancellationToken);
    }
}
