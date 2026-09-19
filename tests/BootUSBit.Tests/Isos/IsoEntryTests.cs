using BootUSBit.Core.Isos;

namespace BootUSBit.Tests.Isos;

public class IsoEntryTests
{
    [Fact]
    public void FileName_ReturnsFileNameFromPath()
    {
        var entry = new IsoEntry(@"C:\isos\ubuntu-24.04.iso", "Ubuntu 24.04");

        Assert.Equal("ubuntu-24.04.iso", entry.FileName);
    }
}
