namespace BootUSBit.Core.Disks;

/// <summary>A physical USB disk as reported by WMI, identified by its Windows disk number (e.g. \\.\PHYSICALDRIVE1).</summary>
public sealed record UsbDriveInfo(
    int DiskNumber,
    string Model,
    ulong SizeBytes,
    string PnpDeviceId,
    string VolumeLabel,
    char? DriveLetter)
{
    public double SizeGigabytes => SizeBytes / 1_000_000_000d;

    public string DisplayName =>
        $"{(DriveLetter is null ? "?:" : $"{DriveLetter}:")} - " +
        (string.IsNullOrWhiteSpace(VolumeLabel) ? "Unlabeled USB drive" : VolumeLabel);
}
