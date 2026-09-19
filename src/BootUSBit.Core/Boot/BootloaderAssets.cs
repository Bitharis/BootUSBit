namespace BootUSBit.Core.Boot;

/// <summary>Resolves paths to the bundled, checksum-pinned syslinux binaries shipped with the app.</summary>
public static class BootloaderAssets
{
    public static string AssetsRoot => Path.Combine(AppContext.BaseDirectory, "assets", "bootloaders");

    private static string SyslinuxRoot => Path.Combine(AssetsRoot, "syslinux");

    // BIOS/MBR boot chain
    public static string SyslinuxExe => Path.Combine(SyslinuxRoot, "syslinux64.exe");

    public static string MbrBin => Path.Combine(SyslinuxRoot, "mbr.bin");

    public static string BiosModulesDir => Path.Combine(SyslinuxRoot, "modules");

    // UEFI boot chain (same syslinux.cfg config format as BIOS, no GRUB needed)
    public static string Efi64Dir => Path.Combine(SyslinuxRoot, "efi64");

    public static string Efi64BootApp => Path.Combine(Efi64Dir, "bootx64.efi");

    public static string Efi64Ldlinux => Path.Combine(Efi64Dir, "ldlinux.e64");

    public static string Efi64ModulesDir => Path.Combine(Efi64Dir, "modules");
}
