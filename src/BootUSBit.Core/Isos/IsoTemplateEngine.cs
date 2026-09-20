using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BootUSBit.Core.Diagnostics;
using BootUSBit.Core.Isos.Templates;

namespace BootUSBit.Core.Isos;

/// <summary>Thrown when no <see cref="Templates.IIsoTemplate"/> recognizes a mounted ISO's layout.</summary>
public sealed class UnsupportedIsoException(string message) : Exception(message);

/// <summary>Picks the right <see cref="Templates.IIsoTemplate"/> for an ISO and appends its boot-menu entry.</summary>
[SupportedOSPlatform("windows")]
public sealed partial class IsoTemplateEngine
{
    private readonly IReadOnlyList<IIsoTemplate> _templates;
    private readonly IProgressLogger _log;

    public IsoTemplateEngine(IProgressLogger? logger = null, IEnumerable<IIsoTemplate>? templates = null)
    {
        _log = logger ?? NullProgressLogger.Instance;
        _templates = templates?.ToList() ?? [new MemtestTemplate(), new CasperLiveTemplate(), new DracutLiveTemplate()];
    }

    /// <summary>Mounts the ISO, applies the first matching template, appends its menu entry, then dismounts.</summary>
    public async Task AddIsoAsync(IsoEntry entry, char usbDriveLetter, CancellationToken cancellationToken = default)
    {
        _log.Info($"Mounting '{entry.FileName}'...");
        var mountedDrive = await IsoImageMounter.MountAsync(entry.IsoPath, cancellationToken);
        var mountedRoot = $"{mountedDrive}:\\";

        try
        {
            var template = _templates.FirstOrDefault(t => t.CanHandle(mountedRoot))
                ?? throw new UnsupportedIsoException(
                    $"No multiboot template recognizes '{entry.DisplayName}'. Use 'write raw ISO' mode instead (single ISO only).");

            _log.Info($"Using template '{template.Name}' for '{entry.DisplayName}'.");
            var slug = Slugify(entry.DisplayName);
            var menuBlock = await template.ApplyAsync(entry, mountedRoot, usbDriveLetter, slug, cancellationToken);

            var configPath = Path.Combine($"{usbDriveLetter}:\\", "syslinux", "syslinux.cfg");
            await File.AppendAllTextAsync(configPath, menuBlock, cancellationToken);

            var efiConfigPath = Path.Combine($"{usbDriveLetter}:\\", "EFI", "BOOT", "syslinux.cfg");
            if (File.Exists(efiConfigPath))
            {
                await File.AppendAllTextAsync(efiConfigPath, menuBlock, cancellationToken);
            }

            _log.Info($"Added '{entry.DisplayName}' to the boot menu.");
        }
        finally
        {
            await IsoImageMounter.DismountAsync(entry.IsoPath, cancellationToken);
        }
    }

    private static string Slugify(string displayName)
    {
        var lowered = displayName.ToLowerInvariant();
        var slug = NonAlphaNumeric().Replace(lowered, "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "iso" : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphaNumeric();
}
