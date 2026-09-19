# BootUSBit

A YUMI/Ventoy-style multiboot USB creator for Windows: pick one or more ISOs, pick a USB drive, and it
formats the drive, installs a bootloader that works on both legacy BIOS and UEFI firmware, and builds a
boot menu that chains into each ISO.

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/) to build/run from source
- Administrator privileges at runtime (the app writes raw disk sectors and runs `diskpart`/`syslinux`)

## Solution layout

| Project | Purpose |
|---|---|
| `src/BootUSBit.Core` | Disk enumeration/formatting, bootloader install, ISO mounting + boot-menu templates, orchestration (`UsbBuilder`) |
| `src/BootUSBit.Wpf` | WPF desktop UI |
| `tests/BootUSBit.Tests` | xUnit tests |

## Building and testing

```powershell
dotnet build BootUSBit.sln
dotnet test tests/BootUSBit.Tests/BootUSBit.Tests.csproj
```

Run the app (elevation prompt is expected — it's required by the app manifest):

```powershell
dotnet run --project src/BootUSBit.Wpf/BootUSBit.Wpf.csproj
```

## CI/CD

GitHub Actions workflows live in [.github/workflows](.github/workflows):

- **`build-test-sbom.yml`** — reusable workflow: restores/builds/tests the solution, checks for known-
  vulnerable NuGet packages (`dotnet list package --vulnerable`), generates a CycloneDX SBOM, and scans it
  with Grype (fails on high/critical vulnerabilities; results are also uploaded to the repo's Code Scanning
  tab).
- **`ci.yml`** — runs the above on every push/PR to `main`.
- **`release.yml`** — runs the above, then on a `v*.*.*` tag push (or manual dispatch) publishes a
  self-contained `win-x64` build, zips it with its SBOM, and creates a GitHub Release with the zip attached
  so it's directly downloadable from the repo's Releases page.

To cut a release: `git tag v1.0.0 && git push origin v1.0.0`.

## Bundled bootloader binaries

`assets/bootloaders/` ships the syslinux binaries the app needs to make a drive bootable (BIOS boot sector,
MBR code, and syslinux's own UEFI application). Every file is checksum-verified against
`assets/bootloaders/manifest.sha256` before each disk write — see
[assets/bootloaders/README.txt](assets/bootloaders/README.txt) for what's there and how to regenerate the
manifest if you replace a file.

## How booting works

- **BIOS**: `syslinux64.exe` installs the boot sector on the USB's FAT32 partition; a generic MBR
  (`mbr.bin`) is written to the disk's first 440 bytes.
- **UEFI**: syslinux's own prebuilt EFI64 application is staged at `\EFI\BOOT\BOOTX64.EFI` on the same
  partition. Since syslinux's BIOS and EFI builds read the identical `syslinux.cfg` format/search path, one
  config drives both boot modes — no GRUB2 needed.
- **ISO templates** (`src/BootUSBit.Core/Isos/Templates`) copy the kernel/initrd (and, for live distros,
  the whole ISO for `iso-scan/filename`) onto the drive and append a menu entry. Built-in templates cover
  Ubuntu/Debian (`casper`), Fedora (`dracut`), and memtest86+. ISOs none of these recognize can instead be
  written raw to the whole disk (single ISO, no multiboot menu) via the "Write raw ISO image" option.

## Logging

The app logs to both the UI and a rolling file, configured in
[src/BootUSBit.Wpf/appsettings.json](src/BootUSBit.Wpf/appsettings.json):

```json
{
  "Logging": {
    "MinimumLevel": "Info",
    "FilePath": "logs/bootusbit.log",
    "MaxFileSizeBytes": 5242880,
    "RetainedFileCount": 5
  }
}
```

`MinimumLevel` accepts `Trace`, `Debug`, `Info` (default), `Warn`, `Error`, or `Fatal`.

## Safety notes

- Only USB-attached, removable disks are ever listed or written to; the app re-verifies a disk is still a
  USB drive immediately before wiping it.
- All external process calls (`diskpart`, `syslinux64.exe`, PowerShell disk-image cmdlets) use explicit
  argument lists or environment variables — never string-concatenated shell commands — to avoid injection
  via file paths.
- Bundled bootloader binaries are checksum-verified before use; the app refuses to proceed if a file is
  missing or doesn't match `manifest.sha256`.

## Known limitations

- Distro template coverage is currently limited to Ubuntu/Debian (casper), Fedora (dracut), and
  memtest86+; other distros will fall back to raw/DD mode unless a matching template is added.
- Not yet verified against real hardware/VM boots — treat as unverified until you've tested it on your own
  target machines.
