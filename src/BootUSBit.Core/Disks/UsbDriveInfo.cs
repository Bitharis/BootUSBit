namespace BootUSBit.Core.Disks;

/// <summary>A physical USB disk as reported by WMI, identified by its Windows disk number (e.g. \\.\PHYSICALDRIVE1).</summary>
public sealed record UsbDriveInfo(
    int DiskNumber,
    string Model,
    ulong SizeBytes,
    string PnpDeviceId)
{
    public double SizeGigabytes => SizeBytes / 1_000_000_000d;
}
