using System.Runtime.Versioning;
using BootUSBit.Core.Boot;
using BootUSBit.Core.Diagnostics;
using BootUSBit.Core.Disks;
using BootUSBit.Core.Isos;
using BootUSBit.Core.Isos.Templates;

namespace BootUSBit.Core;

/// <summary>Top-level entry point that ties disk prep, bootloader install and ISO staging into one operation.</summary>
[SupportedOSPlatform("windows")]
public sealed class UsbBuilder
{
    private readonly IDiskService _diskService;
    private readonly SyslinuxInstaller _syslinuxInstaller;
    private readonly IsoTemplateEngine _isoTemplateEngine;
    private readonly IProgressLogger _log;

    public UsbBuilder(
        IDiskService? diskService = null,
        SyslinuxInstaller? syslinuxInstaller = null,
        IsoTemplateEngine? isoTemplateEngine = null,
        IProgressLogger? logger = null)
    {
        _log = logger ?? NullProgressLogger.Instance;
        _diskService = diskService ?? new DiskService(_log);
        _syslinuxInstaller = syslinuxInstaller ?? new SyslinuxInstaller(_log);
        _isoTemplateEngine = isoTemplateEngine ?? new IsoTemplateEngine(_log);
    }

    /// <summary>Lists candidate USB drives the user can pick as the build target.</summary>
    public Task<IReadOnlyList<UsbDriveInfo>> GetUsbDrivesAsync(CancellationToken cancellationToken = default) =>
        _diskService.GetUsbDrivesAsync(cancellationToken);

    /// <summary>Wipes the drive, installs the multiboot bootloader, then adds every ISO to the boot menu in order.</summary>
    public async Task BuildAsync(int diskNumber, IReadOnlyList<IsoEntry> isos, CancellationToken cancellationToken = default)
    {
        if (isos.Count == 0)
        {
            throw new ArgumentException("At least one ISO must be selected.", nameof(isos));
        }

        var driveLetter = await _diskService.WipeAndPrepareAsync(diskNumber, cancellationToken);
        await _syslinuxInstaller.InstallAsync(driveLetter, diskNumber, cancellationToken);

        foreach (var iso in isos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _isoTemplateEngine.AddIsoAsync(iso, driveLetter, cancellationToken);
        }

        _log.Info("USB drive is ready.");
    }

    /// <summary>
    /// Writes a single ISO byte-for-byte to the whole disk (Rufus-style "DD image" mode), for ISOs no
    /// multiboot template recognizes. This erases any existing partitions/multiboot menu on the drive.
    /// </summary>
    public async Task WriteRawIsoAsync(int diskNumber, string isoPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await _diskService.EnsureUsbDriveAsync(diskNumber, cancellationToken);
        _log.Info($"Writing raw ISO image to disk {diskNumber}...");
        await DdModeTemplate.WriteAsync(isoPath, diskNumber, progress, cancellationToken);
        _log.Info("USB drive is ready.");
    }
}
