using System.Security.Cryptography;

namespace BootUSBit.Core.Security;

/// <summary>Verifies bundled third-party binaries (syslinux, etc.) against a pinned SHA-256 manifest before use.</summary>
public static class FileIntegrity
{
    public static async Task<bool> VerifySha256Async(string filePath, string expectedHexHash, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        var actual = Convert.ToHexString(hashBytes);
        return string.Equals(actual, expectedHexHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Loads a "hash  relative/path" per-line manifest, as produced by `sha256sum`.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> LoadManifestAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = await File.ReadAllLinesAsync(manifestPath, cancellationToken);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            map[parts[1].TrimStart('*')] = parts[0];
        }

        return map;
    }

    /// <summary>Verifies every file listed in the manifest against the files on disk, throwing on the first mismatch or missing file.</summary>
    public static async Task VerifyAllAsync(string assetsRoot, string manifestFileName = "manifest.sha256", CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(assetsRoot, manifestFileName);
        var manifest = await LoadManifestAsync(manifestPath, cancellationToken);

        foreach (var (relativePath, expectedHash) in manifest)
        {
            var fullPath = Path.Combine(assetsRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Bundled bootloader asset missing: {relativePath}", fullPath);
            }

            if (!await VerifySha256Async(fullPath, expectedHash, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Checksum mismatch for bundled bootloader asset '{relativePath}'. The file may be corrupted or tampered with; refusing to use it.");
            }
        }
    }
}
