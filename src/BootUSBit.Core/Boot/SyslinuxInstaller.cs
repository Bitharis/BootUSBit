using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BootUSBit.Core.Diagnostics;
using BootUSBit.Core.Processes;
using BootUSBit.Core.Security;
using Microsoft.Win32.SafeHandles;

namespace BootUSBit.Core.Boot;

/// <summary>Deploys the syslinux multiboot bootloader (BIOS + UEFI) onto a freshly formatted USB drive.</summary>
[SupportedOSPlatform("windows")]
public sealed class SyslinuxInstaller
{
    private const string DefaultConfigHeader = """
        UI menu.c32
        PROMPT 0
        TIMEOUT 300
        DEFAULT menu
        MENU TITLE BootUSBit Multiboot Menu

        LABEL menu
        MENU LABEL Boot menu
        KERNEL menu.c32
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
        InstallUefiBootFiles(driveLetter, configPath);
    }

    private static void InstallUefiBootFiles(char driveLetter, string configPath)
    {
        var efiBootDir = Path.Combine($"{driveLetter}:\\", "EFI", "BOOT");
        Directory.CreateDirectory(efiBootDir);

        File.Copy(BootloaderAssets.Efi64BootApp, Path.Combine(efiBootDir, "BOOTX64.EFI"), overwrite: true);
        File.Copy(BootloaderAssets.Efi64Ldlinux, Path.Combine(efiBootDir, "ldlinux.e64"), overwrite: true);
        CopyModules(BootloaderAssets.Efi64ModulesDir, efiBootDir);
        File.Copy(configPath, Path.Combine(efiBootDir, "syslinux.cfg"), overwrite: true);
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

    private static Task WriteMbrAsync(int diskNumber, CancellationToken cancellationToken)
    {
        // Physical disks require sector-aligned writes. Replace only the first 440 bytes in the
        // existing 512-byte MBR sector so the partition table and disk signature stay intact.
        const int BootCodeLength = 440;
        const int SectorLength = 512;

        var mbrBytes = File.ReadAllBytes(BootloaderAssets.MbrBin);
        if (mbrBytes.Length < BootCodeLength)
        {
            throw new InvalidOperationException("Bundled mbr.bin is smaller than expected; refusing to write it.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var physicalDrivePath = $@"\\.\PHYSICALDRIVE{diskNumber}";
        using var handle = CreateFile(
            physicalDrivePath,
            GenericRead | GenericWrite,
            FileShare.ReadWrite,
            IntPtr.Zero,
            FileMode.Open,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            throw new IOException(
                $"Could not open {physicalDrivePath} for MBR writing: {new Win32Exception(error).Message} (Win32 error {error}).");
        }

        using var disk = new FileStream(handle, FileAccess.ReadWrite, 4096, isAsync: false);
        var sector = new byte[SectorLength];
        var bytesRead = 0;
        while (bytesRead < SectorLength)
        {
            var read = disk.Read(sector, bytesRead, SectorLength - bytesRead);
            if (read == 0)
            {
                throw new IOException($"Could not read the complete MBR sector from {physicalDrivePath}.");
            }

            bytesRead += read;
        }

        Buffer.BlockCopy(mbrBytes, 0, sector, 0, BootCodeLength);
        disk.Position = 0;
        disk.Write(sector, 0, SectorLength);
        disk.Flush(flushToDisk: true);
        return Task.CompletedTask;
    }

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        FileShare shareMode,
        IntPtr securityAttributes,
        FileMode creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);
}
