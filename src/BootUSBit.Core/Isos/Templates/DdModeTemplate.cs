using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

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
        using var handle = CreateFile(
            physicalDrivePath,
            GenericRead | GenericWrite,
            FileShare.ReadWrite,
            IntPtr.Zero,
            FileMode.Open,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            throw new IOException(
                $"Could not open {physicalDrivePath} for raw image writing: {new Win32Exception(error).Message} (Win32 error {error}).");
        }

        using var dest = new FileStream(handle, FileAccess.ReadWrite, 4 * 1024 * 1024, isAsync: false);

        var buffer = new byte[4 * 1024 * 1024];
        long totalWritten = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            dest.Write(buffer, 0, read);
            totalWritten += read;
            progress?.Report((double)totalWritten / source.Length);
        }

        dest.Flush(flushToDisk: true);
    }

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        FileShare shareMode,
        IntPtr securityAttributes,
        FileMode creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);
}
