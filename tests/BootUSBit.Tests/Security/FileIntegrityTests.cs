using BootUSBit.Core.Security;

namespace BootUSBit.Tests.Security;

public class FileIntegrityTests
{
    [Fact]
    public async Task VerifySha256Async_ReturnsTrue_ForMatchingHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "hello world");
            var expected = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(tempFile)));

            var result = await FileIntegrity.VerifySha256Async(tempFile, expected);

            Assert.True(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifySha256Async_ReturnsFalse_ForMismatchedHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "hello world");

            var result = await FileIntegrity.VerifySha256Async(tempFile, new string('0', 64));

            Assert.False(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyAllAsync_Throws_WhenManifestedFileIsMissing()
    {
        var assetsRoot = Directory.CreateTempSubdirectory().FullName;
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(assetsRoot, "manifest.sha256"),
                $"{new string('a', 64)}  missing-file.bin");

            await Assert.ThrowsAsync<FileNotFoundException>(() => FileIntegrity.VerifyAllAsync(assetsRoot));
        }
        finally
        {
            Directory.Delete(assetsRoot, recursive: true);
        }
    }

    [Fact]
    public async Task VerifyAllAsync_BundledSyslinuxAssets_MatchManifest()
    {
        await FileIntegrity.VerifyAllAsync(BundledAssetsRoot);
    }

    private static string BundledAssetsRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "bootloaders"));
}
