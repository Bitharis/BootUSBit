namespace BootUSBit.Core.Isos;

/// <summary>An ISO file selected by the user, plus the label to show for it in the boot menu.</summary>
public sealed record IsoEntry(string IsoPath, string DisplayName)
{
    public string FileName => Path.GetFileName(IsoPath);
}
