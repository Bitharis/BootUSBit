using BootUSBit.Core.Isos;

namespace BootUSBit.Core.Isos.Templates;

/// <summary>Shared file-staging helpers so each <see cref="IIsoTemplate"/> doesn't duplicate directory/copy logic.</summary>
public static class IsoStaging
{
    /// <summary>Creates (if needed) and returns usb:\isos\&lt;slug&gt;\ for a template to copy kernel/initrd into.</summary>
    public static string CreateIsoDirectory(char usbDriveLetter, string slug)
    {
        var dir = Path.Combine($"{usbDriveLetter}:\\", "isos", slug);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>The on-USB path for the full ISO copy, e.g. usb:\isos\&lt;slug&gt;.iso.</summary>
    public static string GetIsoCopyPath(char usbDriveLetter, string slug) =>
        Path.Combine($"{usbDriveLetter}:\\", "isos", $"{slug}.iso");

    /// <summary>Copies the whole ISO file onto the drive — needed by boot params like "iso-scan/filename".</summary>
    public static Task CopyFullIsoAsync(IsoEntry entry, char usbDriveLetter, string slug, CancellationToken cancellationToken = default) =>
        Task.Run(() => File.Copy(entry.IsoPath, GetIsoCopyPath(usbDriveLetter, slug), overwrite: true), cancellationToken);

    public static (string Kernel, string Initrd)? FindFirstExisting(
        string mountedIsoRoot,
        IEnumerable<(string Kernel, string Initrd)> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(mountedIsoRoot, candidate.Kernel)) &&
                File.Exists(Path.Combine(mountedIsoRoot, candidate.Initrd)))
            {
                return candidate;
            }
        }

        return null;
    }
}
