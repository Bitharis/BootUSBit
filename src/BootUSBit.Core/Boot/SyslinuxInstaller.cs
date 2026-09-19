using System.Runtime.Versioning;
using BootUSBit.Core.Diagnostics;
using BootUSBit.Core.Processes;
using BootUSBit.Core.Security;

namespace BootUSBit.Core.Boot;

/// <summary>Deploys the syslinux multiboot bootloader (BIOS + UEFI) onto a freshly formatted USB drive.</summary>
[SupportedOSPlatform("windows")]
public sealed class SyslinuxInstaller
{
    private const string DefaultConfigHeader = """
        UI vesamenu.c32
        PROMPT 0
        TIMEOUT 300
        DEFAULT menu
        MENU TITLE BootUSBit Multiboot Menu

        LABEL menu
        MENU LABEL Boot menu
        KERNEL vesamenu.c32
        APPEND syslinux.cfg

        """;

    private readonly IProgressLogger _log;

    public SyslinuxInstaller(IProgressLogger? logger = null)
    {
        _log = logger ?? NullProgressLogger.Instance;
    }

    /// <summary>
    /// Installs syslinux as the boot sector of <paramref name="driveLetter"/> for legacy BIOS boot, writes a
    /// generic MBR to the physical disk, and also stages syslinux's own UEFI (EFI64) application under
    /// \EFI\BOOT so the same drive boots on UEFI-only firmware too. Both boot paths share the same
    /// \syslinux\syslinux.cfg config file — syslinux's BIOS and EFI builds use identical config syntax and
    /// search paths, so no separate GRUB config/format is needed.
    /// </summary>
    public async Task InstallAsync(char driveLetter, int diskNumber, CancellationToken cancellationToken = default)
    {
        _log.Info("Verifying bundled bootloader assets...");
        await FileIntegrity.VerifyAllAsync(BootloaderAssets.AssetsRoot, cancellationToken: cancellationToken);

        var drive = $"{driveLetter}:";
        var syslinuxDir = Path.Combine($"{drive}\\", "syslinux");
        Directory.CreateDirectory(syslinuxDir);

        _log.Info("Copying syslinux (BIOS) modules...");
        CopyModules(BootloaderAssets.BiosModulesDir, syslinuxDir);

        var configPath = Path.Combine(syslinuxDir, "syslinux.cfg");
        if (!File.Exists(configPath))
        {
            await File.WriteAllTextAsync(configPath, DefaultConfigHeader, cancellationToken);
        }

        _log.Info($"Installing syslinux boot sector to {drive}...");
        var installResult = await ProcessRunner.RunAsync(
            BootloaderAssets.SyslinuxExe,
            ["-maf", drive],
            cancellationToken: cancellationToken);

        if (!installResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"syslinux installation failed (exit {installResult.ExitCode}): {installResult.StandardError}");
        }

        _log.Info("Writing MBR boot code...");
        await WriteMbrAsync(diskNumber, cancellationToken);

        _log.Info("Staging UEFI boot files (\\EFI\\BOOT)...");
        InstallUefiBootFiles(driveLetter);
    }

    private static void InstallUefiBootFiles(char driveLetter)
    {
        var efiBootDir = Path.Combine($"{driveLetter}:\\", "EFI", "BOOT");
        Directory.CreateDirectory(efiBootDir);

        File.Copy(BootloaderAssets.Efi64BootApp, Path.Combine(efiBootDir, "BOOTX64.EFI"), overwrite: true);
        File.Copy(BootloaderAssets.Efi64Ldlinux, Path.Combine(efiBootDir, "ldlinux.e64"), overwrite: true);
        CopyModules(BootloaderAssets.Efi64ModulesDir, efiBootDir);
    }

    private static void CopyModules(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir))
        {
            var destination = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static async Task WriteMbrAsync(int diskNumber, CancellationToken cancellationToken)
    {
        // Only the first 440 bytes (boot code) are replaced; the partition table and signature that
        // follow are left untouched so the partition diskpart already created stays intact.
        const int BootCodeLength = 440;

        var mbrBytes = await File.ReadAllBytesAsync(BootloaderAssets.MbrBin, cancellationToken);
        if (mbrBytes.Length < BootCodeLength)
        {
            throw new InvalidOperationException("Bundled mbr.bin is smaller than expected; refusing to write it.");
        }

        var physicalDrivePath = $@"\\.\PHYSICALDRIVE{diskNumber}";
        await using var disk = new FileStream(physicalDrivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        await disk.WriteAsync(mbrBytes.AsMemory(0, BootCodeLength), cancellationToken);
        await disk.FlushAsync(cancellationToken);
    }
}
