namespace BootUSBit.Core.Disks;

/// <summary>Enumerates and prepares USB drives; never touches non-removable disks.</summary>
public interface IDiskService
{
    /// <summary>Lists only removable, USB-attached disks — internal disks are never returned or touched.</summary>
    Task<IReadOnlyList<UsbDriveInfo>> GetUsbDrivesAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws if <paramref name="diskNumber"/> is not a currently-attached USB drive.</summary>
    Task EnsureUsbDriveAsync(int diskNumber, CancellationToken cancellationToken = default);

    /// <summary>Locks and dismounts mounted USB volumes so raw writes can safely access the physical disk.</summary>
    Task<IDisposable> LockUsbVolumesAsync(int diskNumber, CancellationToken cancellationToken = default);

    /// <summary>Wipes the disk, creates a single active MBR partition and formats it FAT32, returning the assigned drive letter.</summary>
    Task<char> WipeAndPrepareAsync(int diskNumber, CancellationToken cancellationToken = default);
}
