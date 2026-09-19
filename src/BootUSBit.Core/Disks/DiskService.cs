using System.Management;
using System.Runtime.Versioning;
using BootUSBit.Core.Diagnostics;
using BootUSBit.Core.Processes;

namespace BootUSBit.Core.Disks;

/// <summary>Finds USB drives via WMI and prepares them (wipe/partition/format) via diskpart.</summary>
[SupportedOSPlatform("windows")]
public sealed class DiskService : IDiskService
{
    private readonly IProgressLogger _log;

    public DiskService(IProgressLogger? logger = null)
    {
        _log = logger ?? NullProgressLogger.Instance;
    }

    public Task<IReadOnlyList<UsbDriveInfo>> GetUsbDrivesAsync(CancellationToken cancellationToken = default)
    {
        var drives = new List<UsbDriveInfo>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceID, Model, Size, PNPDeviceID, InterfaceType FROM Win32_DiskDrive WHERE InterfaceType = 'USB'");

        foreach (ManagementBaseObject disk in searcher.Get())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deviceId = (string?)disk["DeviceID"] ?? string.Empty;
            var diskNumber = ParseDiskNumber(deviceId);
            if (diskNumber is null)
            {
                continue;
            }

            var size = disk["Size"] is null ? 0UL : Convert.ToUInt64(disk["Size"]);
            drives.Add(new UsbDriveInfo(
                diskNumber.Value,
                (string?)disk["Model"] ?? "Unknown USB drive",
                size,
                (string?)disk["PNPDeviceID"] ?? string.Empty));
        }

        return Task.FromResult<IReadOnlyList<UsbDriveInfo>>(drives);
    }

    public async Task<char> WipeAndPrepareAsync(int diskNumber, CancellationToken cancellationToken = default)
    {
        // Re-verify the disk is still a USB drive right before wiping, to guard against a stale/spoofed disk number.
        await EnsureUsbDriveAsync(diskNumber, cancellationToken);

        var scriptPath = Path.Combine(Path.GetTempPath(), $"bootusbit-{Guid.NewGuid():N}.diskpart.txt");
        var script = string.Join(Environment.NewLine,
        [
            $"select disk {diskNumber}",
            "clean",
            "convert mbr",
            "create partition primary",
            "active",
            "format fs=fat32 quick label=BOOTUSB",
            "assign",
        ]);

        await File.WriteAllTextAsync(scriptPath, script, cancellationToken);
        try
        {
            _log.Info($"Partitioning disk {diskNumber} (diskpart)...");
            var result = await ProcessRunner.RunAsync(
                "diskpart.exe",
                ["/s", scriptPath],
                cancellationToken: cancellationToken);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"diskpart failed (exit {result.ExitCode}):{Environment.NewLine}{result.StandardOutput}{result.StandardError}");
            }

            _log.Info(result.StandardOutput.Trim());
        }
        finally
        {
            File.Delete(scriptPath);
        }

        var driveLetter = await FindDriveLetterForDiskAsync(diskNumber, cancellationToken);
        if (driveLetter is null)
        {
            throw new InvalidOperationException($"Could not determine the drive letter assigned to disk {diskNumber}.");
        }

        return driveLetter.Value;
    }

    public async Task EnsureUsbDriveAsync(int diskNumber, CancellationToken cancellationToken = default)
    {
        var current = await GetUsbDrivesAsync(cancellationToken);
        if (!current.Any(d => d.DiskNumber == diskNumber))
        {
            throw new InvalidOperationException(
                $"Disk {diskNumber} is not a currently-attached USB drive; refusing to write to it.");
        }
    }

    private static async Task<char?> FindDriveLetterForDiskAsync(int diskNumber, CancellationToken cancellationToken)
    {
        // Formatting can take a moment to surface a drive letter via WMI; poll briefly.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var searcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='\\\\.\\PHYSICALDRIVE{diskNumber}'}} " +
                "WHERE AssocClass = Win32_DiskDriveToDiskPartition");

            foreach (ManagementBaseObject partition in searcher.Get())
            {
                using var logicalSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} " +
                    "WHERE AssocClass = Win32_LogicalDiskToPartition");

                foreach (ManagementBaseObject logicalDisk in logicalSearcher.Get())
                {
                    var name = (string?)logicalDisk["DeviceID"];
                    if (!string.IsNullOrEmpty(name) && name.Length >= 2 && name[1] == ':')
                    {
                        return char.ToUpperInvariant(name[0]);
                    }
                }
            }

            await Task.Delay(500, cancellationToken);
        }

        return null;
    }

    private static int? ParseDiskNumber(string deviceId)
    {
        // deviceId looks like \\.\PHYSICALDRIVE2
        var marker = "PHYSICALDRIVE";
        var index = deviceId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var numberPart = deviceId[(index + marker.Length)..];
        return int.TryParse(numberPart, out var number) ? number : null;
    }
}
