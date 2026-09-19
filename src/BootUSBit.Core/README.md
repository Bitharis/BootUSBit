# BootUSBit.Core

Platform logic for BootUSBit, with no UI dependencies. See the [repo README](../../README.md) for the
overall architecture; this project contains:

- `Disks/` — USB drive enumeration and wipe/partition/format (`DiskService`)
- `Boot/` — syslinux (BIOS + UEFI) bootloader install (`SyslinuxInstaller`, `BootloaderAssets`)
- `Isos/` — ISO mounting and per-distro boot-menu templates (`IsoTemplateEngine`, `Templates/`)
- `Security/` — checksum verification of bundled bootloader binaries (`FileIntegrity`)
- `Processes/` — safe external-process execution (`ProcessRunner`)
- `Diagnostics/` — logging abstraction (`IProgressLogger`, `FileProgressLogger`, `CompositeProgressLogger`)
- `UsbBuilder.cs` — orchestrates the above into the two top-level operations: `BuildAsync` (multiboot) and
  `WriteRawIsoAsync` (single ISO, whole-disk DD mode)

Windows-only (`net10.0-windows`, `[SupportedOSPlatform("windows")]`) since it relies on WMI, `diskpart`,
and raw physical-disk access.
