using BootUSBit.Core.Isos;

namespace BootUSBit.Core.Isos.Templates;

/// <summary>
/// Template for Ubuntu casper and Debian Live ISOs. Copies the kernel, initrd and the full ISO
/// (needed for "iso-scan/filename") onto the USB drive.
/// </summary>
public sealed class CasperLiveTemplate : IIsoTemplate
{
    public string Name => "Ubuntu/Debian live";

    private static readonly string[] IsolinuxConfigCandidates =
    [
        "isolinux/isolinux.cfg",
        "boot/isolinux/isolinux.cfg",
    ];

    private static readonly (string Kernel, string Initrd)[] KernelInitrdCandidates =
    [
        ("casper/vmlinuz", "casper/initrd.lz"),
        ("casper/vmlinuz", "casper/initrd"),
        ("casper/vmlinuz", "casper/initrd.gz"),
        ("isolinux/vmlinuz", "isolinux/initrd.gz"),
        ("live/vmlinuz", "live/initrd.img"),
    ];

    public bool CanHandle(string mountedIsoRoot)
    {
        var isCasperLayout = Directory.Exists(Path.Combine(mountedIsoRoot, "casper")) &&
                             IsolinuxConfigCandidates.Any(candidate => File.Exists(Path.Combine(mountedIsoRoot, candidate)));
        var isDebianLiveLayout = IsoStaging.FindFirstExisting(mountedIsoRoot,
            [("live/vmlinuz", "live/initrd.img")]) is not null;

        return isCasperLayout || isDebianLiveLayout;
    }

    public async Task<string> ApplyAsync(
        IsoEntry entry,
        string mountedIsoRoot,
        char usbDriveLetter,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var kernelInitrd = IsoStaging.FindFirstExisting(mountedIsoRoot, KernelInitrdCandidates)
            ?? throw new InvalidOperationException(
                $"'{entry.DisplayName}' looks like a supported live ISO but no known kernel/initrd layout was found.");

        var isDebianLive = kernelInitrd.Kernel.StartsWith("live/", StringComparison.OrdinalIgnoreCase);
        var isoDir = IsoStaging.CreateIsoDirectory(usbDriveLetter, slug);
        File.Copy(Path.Combine(mountedIsoRoot, kernelInitrd.Kernel), Path.Combine(isoDir, "vmlinuz"), overwrite: true);
        File.Copy(Path.Combine(mountedIsoRoot, kernelInitrd.Initrd), Path.Combine(isoDir, "initrd.img"), overwrite: true);
        await IsoStaging.CopyFullIsoAsync(entry, usbDriveLetter, slug, cancellationToken);

        var bootParameters = isDebianLive
            ? $"boot=live components findiso=/isos/{slug}.iso quiet splash"
            : $"boot=casper iso-scan/filename=/isos/{slug}.iso quiet splash ---";

        return $"""
            LABEL {slug}
            MENU LABEL {entry.DisplayName}
            KERNEL /isos/{slug}/vmlinuz
            APPEND initrd=/isos/{slug}/initrd.img {bootParameters}

            """;
    }
}
