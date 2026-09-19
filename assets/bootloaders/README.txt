This folder contains third-party bootloader binaries that BootUSBit verifies (by SHA-256) and uses to make
a USB drive bootable, for BOTH legacy BIOS and UEFI firmware.

The required files below have already been extracted into syslinux/ from the bundled syslinux-6.03/ source
release (bios/win64, bios/mbr, bios/com32/*, and the prebuilt efi64/efi + efi64/com32/*), and their checksums
are recorded in manifest.sha256. FileIntegrity.VerifyAllAsync() re-checks these hashes before every disk
write, so if you replace any file you must regenerate manifest.sha256 (see the command at the top of that
file) or the app will refuse to run.

UEFI boot uses syslinux's own EFI64 application (bootx64.efi) rather than GRUB2: its BIOS and EFI builds
read the identical syslinux.cfg config format and search paths, so one config drives both boot modes with
no separate GRUB config needed.

Required files (from the official syslinux release, e.g. syslinux-x.xx/bios and .../efi64):
  syslinux/syslinux64.exe      - Windows installer for the syslinux BIOS boot sector
  syslinux/mbr.bin             - generic MBR boot code (bios/mbr/mbr.bin in the syslinux release)
  syslinux/modules/ldlinux.c32
  syslinux/modules/libcom32.c32
  syslinux/modules/libutil.c32
  syslinux/modules/menu.c32
  syslinux/modules/vesamenu.c32
  syslinux/efi64/bootx64.efi   - UEFI application, staged to \EFI\BOOT\BOOTX64.EFI on the USB drive
  syslinux/efi64/ldlinux.e64
  syslinux/efi64/modules/libcom32.c32
  syslinux/efi64/modules/libutil.c32
  syslinux/efi64/modules/menu.c32
  syslinux/efi64/modules/vesamenu.c32

After placing the files, generate manifest.sha256 next to this file, one line per file:
  <sha256-hex>  syslinux/syslinux64.exe
  <sha256-hex>  syslinux/mbr.bin
  <sha256-hex>  syslinux/modules/ldlinux.c32
  ...

FileIntegrity.VerifyAllAsync() refuses to proceed if any listed file is missing or its hash does not match,
so a corrupted or tampered download will be rejected instead of silently used.
