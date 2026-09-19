using BootUSBit.Core.Isos;

namespace BootUSBit.Core.Isos.Templates;

/// <summary>
/// Template for memtest86+ ISOs. These are a single standalone kernel with no initrd and nothing to
/// loop-mount, so they don't fit the live-distro "iso-scan" pattern used by the other templates.
/// </summary>
public sealed class MemtestTemplate : IIsoTemplate
{
    public string Name => "memtest86+";

    private static readonly string[] KernelCandidates =
    [
        "isolinux/memtest",
        "BOOT/memtest",
        "memtest",
    ];

    public bool CanHandle(string mountedIsoRoot) =>
        KernelCandidates.Any(candidate => File.Exists(Path.Combine(mountedIsoRoot, candidate)));

    public Task<string> ApplyAsync(
        IsoEntry entry,
        string mountedIsoRoot,
        char usbDriveLetter,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var kernelRelative = KernelCandidates.First(candidate => File.Exists(Path.Combine(mountedIsoRoot, candidate)));

        var isoDir = IsoStaging.CreateIsoDirectory(usbDriveLetter, slug);
        File.Copy(Path.Combine(mountedIsoRoot, kernelRelative), Path.Combine(isoDir, "memtest"), overwrite: true);

        return Task.FromResult($"""
            LABEL {slug}
            MENU LABEL {entry.DisplayName}
            KERNEL /isos/{slug}/memtest

            """);
    }
}
