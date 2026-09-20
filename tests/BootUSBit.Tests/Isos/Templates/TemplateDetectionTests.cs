using BootUSBit.Core.Isos.Templates;

namespace BootUSBit.Tests.Isos.Templates;

public class TemplateDetectionTests
{
    [Fact]
    public void CasperLiveTemplate_CanHandle_TrueForCasperLayout()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "casper"));
            Directory.CreateDirectory(Path.Combine(root, "isolinux"));
            File.WriteAllText(Path.Combine(root, "isolinux", "isolinux.cfg"), string.Empty);

            Assert.True(new CasperLiveTemplate().CanHandle(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CasperLiveTemplate_CanHandle_FalseWithoutCasperDir()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "isolinux"));
            File.WriteAllText(Path.Combine(root, "isolinux", "isolinux.cfg"), string.Empty);

            Assert.False(new CasperLiveTemplate().CanHandle(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CasperLiveTemplate_CanHandle_TrueForDebianLiveLayout()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "live"));
            File.WriteAllText(Path.Combine(root, "live", "vmlinuz"), string.Empty);
            File.WriteAllText(Path.Combine(root, "live", "initrd.img"), string.Empty);

            Assert.True(new CasperLiveTemplate().CanHandle(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DracutLiveTemplate_CanHandle_TrueForLiveOsLayout()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "LiveOS"));
            Directory.CreateDirectory(Path.Combine(root, "isolinux"));
            File.WriteAllText(Path.Combine(root, "isolinux", "vmlinuz"), string.Empty);
            File.WriteAllText(Path.Combine(root, "isolinux", "initrd.img"), string.Empty);

            Assert.True(new DracutLiveTemplate().CanHandle(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void MemtestTemplate_CanHandle_TrueWhenMemtestKernelPresent()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "isolinux"));
            File.WriteAllText(Path.Combine(root, "isolinux", "memtest"), string.Empty);

            Assert.True(new MemtestTemplate().CanHandle(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void IsoStaging_FindFirstExisting_ReturnsFirstMatchingPair()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "b"));
            File.WriteAllText(Path.Combine(root, "b", "kernel"), string.Empty);
            File.WriteAllText(Path.Combine(root, "b", "initrd"), string.Empty);

            var result = IsoStaging.FindFirstExisting(root,
            [
                ("a/kernel", "a/initrd"),
                ("b/kernel", "b/initrd"),
            ]);

            Assert.Equal(("b/kernel", "b/initrd"), result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
