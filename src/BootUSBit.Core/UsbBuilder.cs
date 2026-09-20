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

    public Task BuildAsync(
        int diskNumber,
        IReadOnlyList<IsoEntry> isos,
        CancellationToken cancellationToken) =>
        BuildAsync(diskNumber, isos, progress: null, cancellationToken);

    /// <summary>Wipes the drive, installs the multiboot bootloader, then adds every ISO to the boot menu in order.</summary>
    public async Task BuildAsync(
        int diskNumber,
        IReadOnlyList<IsoEntry> isos,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (isos.Count == 0)
        {
            throw new ArgumentException("At least one ISO must be selected.", nameof(isos));
        }

        const long Fat32MaximumFileSize = 4L * 1024 * 1024 * 1024;
        foreach (var iso in isos)
        {
            var size = new FileInfo(iso.IsoPath).Length;
            if (size > Fat32MaximumFileSize)
            {
                throw new UnsupportedIsoException(
                    $"'{iso.DisplayName}' is {size / (1024d * 1024 * 1024):F1} GB, which exceeds FAT32's 4 GB per-file limit. " +
                    "Enable 'Write raw ISO image' mode to write this ISO directly (single ISO only).");
            }
        }

        _log.Info($"Starting multiboot build for disk {diskNumber} with {isos.Count} ISO(s).");
        progress?.Report(0.05);
        var driveLetter = await _diskService.WipeAndPrepareAsync(diskNumber, cancellationToken);
        progress?.Report(0.25);
        await _syslinuxInstaller.InstallAsync(driveLetter, diskNumber, cancellationToken);
        progress?.Report(0.45);

        for (var index = 0; index < isos.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var iso = isos[index];
            _log.Info($"Processing ISO {index + 1} of {isos.Count}: '{iso.DisplayName}'.");
            await _isoTemplateEngine.AddIsoAsync(iso, driveLetter, cancellationToken);
            progress?.Report(0.45 + 0.5 * (index + 1) / isos.Count);
        }

        progress?.Report(1);
        _log.Info("USB drive is ready.");
    }

    /// <summary>
    /// Writes a single ISO byte-for-byte to the whole disk (Rufus-style "DD image" mode), for ISOs no
    /// multiboot template recognizes. This erases any existing partitions/multiboot menu on the drive.
    /// </summary>
    public async Task WriteRawIsoAsync(int diskNumber, string isoPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        using var volumeLocks = await _diskService.LockUsbVolumesAsync(diskNumber, cancellationToken);
        _log.Info($"Writing raw ISO image to disk {diskNumber}...");
        await DdModeTemplate.WriteAsync(isoPath, diskNumber, progress, cancellationToken);
        _log.Info("USB drive is ready.");
    }
}
