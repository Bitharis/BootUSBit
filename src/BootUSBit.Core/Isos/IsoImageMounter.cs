using System.Runtime.Versioning;
using BootUSBit.Core.Processes;

namespace BootUSBit.Core.Isos;

/// <summary>Mounts/dismounts ISO files as virtual DVD drives via the Windows disk-image PowerShell cmdlets.</summary>
[SupportedOSPlatform("windows")]
public static class IsoImageMounter
{
    // The ISO path is passed via an environment variable (not interpolated into the script text)
    // so a path containing quotes or PowerShell metacharacters can never break out of the command.
    private const string MountScript =
        "$d = Mount-DiskImage -ImagePath $env:BOOTUSBIT_ISO_PATH -PassThru; " +
        "($d | Get-Volume).DriveLetter";

    private const string DismountScript =
        "Dismount-DiskImage -ImagePath $env:BOOTUSBIT_ISO_PATH";

    /// <summary>Mounts the ISO and returns the drive letter Windows assigned to it.</summary>
    public static async Task<char> MountAsync(string isoPath, CancellationToken cancellationToken = default)
    {
        var result = await ProcessRunner.RunAsync(
            "powershell.exe",
            ["-NoProfile", "-NonInteractive", "-Command", MountScript],
            new Dictionary<string, string> { ["BOOTUSBIT_ISO_PATH"] = isoPath },
            cancellationToken: cancellationToken);

        var driveLetter = result.StandardOutput.Trim();
        if (!result.Succeeded || driveLetter.Length != 1)
        {
            throw new InvalidOperationException($"Failed to mount ISO '{isoPath}': {result.StandardError}");
        }

        return char.ToUpperInvariant(driveLetter[0]);
    }

    /// <summary>Dismounts a previously-mounted ISO; best-effort, so callers can call it unconditionally in a finally block.</summary>
    public static async Task DismountAsync(string isoPath, CancellationToken cancellationToken = default)
    {
        await ProcessRunner.RunAsync(
            "powershell.exe",
            ["-NoProfile", "-NonInteractive", "-Command", DismountScript],
            new Dictionary<string, string> { ["BOOTUSBIT_ISO_PATH"] = isoPath },
            cancellationToken: cancellationToken);
    }
}
