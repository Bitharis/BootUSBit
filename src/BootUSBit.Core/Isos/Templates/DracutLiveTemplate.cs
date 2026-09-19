using BootUSBit.Core.Isos;

namespace BootUSBit.Core.Isos.Templates;

/// <summary>
/// Template for dracut-based live ISOs (Fedora and derivatives). Uses the same "iso-scan/filename" trick as
/// the casper template — dracut's built-in iso-scan module finds and loop-mounts the ISO copy at boot.
/// </summary>
public sealed class DracutLiveTemplate : IIsoTemplate
{
    public string Name => "Fedora live (dracut)";

    private static readonly (string Kernel, string Initrd)[] KernelInitrdCandidates =
    [
        ("isolinux/vmlinuz", "isolinux/initrd.img"),
        ("images/pxeboot/vmlinuz", "images/pxeboot/initrd.img"),
    ];

    public bool CanHandle(string mountedIsoRoot) =>
        Directory.Exists(Path.Combine(mountedIsoRoot, "LiveOS")) &&
        IsoStaging.FindFirstExisting(mountedIsoRoot, KernelInitrdCandidates) is not null;

    public async Task<string> ApplyAsync(
        IsoEntry entry,
        string mountedIsoRoot,
        char usbDriveLetter,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var kernelInitrd = IsoStaging.FindFirstExisting(mountedIsoRoot, KernelInitrdCandidates)
            ?? throw new InvalidOperationException(
                $"'{entry.DisplayName}' looks dracut-based but no known kernel/initrd layout was found.");

        var isoDir = IsoStaging.CreateIsoDirectory(usbDriveLetter, slug);
        File.Copy(Path.Combine(mountedIsoRoot, kernelInitrd.Kernel), Path.Combine(isoDir, "vmlinuz"), overwrite: true);
        File.Copy(Path.Combine(mountedIsoRoot, kernelInitrd.Initrd), Path.Combine(isoDir, "initrd.img"), overwrite: true);
        await IsoStaging.CopyFullIsoAsync(entry, usbDriveLetter, slug, cancellationToken);

        return $"""
            LABEL {slug}
            MENU LABEL {entry.DisplayName}
            KERNEL /isos/{slug}/vmlinuz
            APPEND initrd=/isos/{slug}/initrd.img root=live:CDLABEL=ANACONDA rd.live.image iso-scan/filename=/isos/{slug}.iso quiet

            """;
    }
}
