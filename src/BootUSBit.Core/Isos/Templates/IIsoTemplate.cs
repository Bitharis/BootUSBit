using BootUSBit.Core.Isos;

namespace BootUSBit.Core.Isos.Templates;

/// <summary>Copies the parts of an ISO needed to boot it from a syslinux multiboot menu.</summary>
public interface IIsoTemplate
{
    string Name { get; }

    /// <summary>Returns true if this template knows how to boot the ISO mounted at <paramref name="mountedIsoRoot"/>.</summary>
    bool CanHandle(string mountedIsoRoot);

    /// <summary>Copies files to the USB drive and returns the syslinux "LABEL" menu block to append to syslinux.cfg.</summary>
    Task<string> ApplyAsync(
        IsoEntry entry,
        string mountedIsoRoot,
        char usbDriveLetter,
        string slug,
        CancellationToken cancellationToken = default);
}
