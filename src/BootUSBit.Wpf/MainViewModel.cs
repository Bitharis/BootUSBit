using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using BootUSBit.Core;
using BootUSBit.Core.Diagnostics;
using BootUSBit.Core.Disks;
using BootUSBit.Core.Isos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BootUSBit.Wpf;

/// <summary>Backing view model for the main window: drive/ISO selection, build progress and logging.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly UsbBuilder _usbBuilder;
    private CancellationTokenSource? _buildCts;

    public ObservableCollection<UsbDriveInfo> Drives { get; } = [];

    public ObservableCollection<IsoEntry> Isos { get; } = [];

    public ObservableCollection<string> LogLines { get; } = [];

    [ObservableProperty]
    private UsbDriveInfo? _selectedDrive;

    [ObservableProperty]
    private IsoEntry? _selectedIso;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready.";

    /// <summary>When set, writes a single ISO raw to the whole disk instead of building a multiboot menu.</summary>
    [ObservableProperty]
    private bool _isRawImageMode;

    [ObservableProperty]
    private double _buildProgress;

    public MainViewModel()
    {
        var fileLogger = new FileProgressLogger(AppSettings.LoadFileLoggerOptions());
        var logger = new CompositeProgressLogger(new UiProgressLogger(AppendLog), fileLogger);
        _usbBuilder = new UsbBuilder(logger: logger);
        Isos.CollectionChanged += (_, _) => BuildCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task RefreshDrivesAsync()
    {
        IsBusy = true;
        try
        {
            var drives = await _usbBuilder.GetUsbDrivesAsync();
            Drives.Clear();
            foreach (var drive in drives)
            {
                Drives.Add(drive);
            }
            StatusText = $"Found {Drives.Count} USB drive(s).";
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddIso()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ISO images (*.iso)|*.iso",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var path in dialog.FileNames)
        {
            Isos.Add(new IsoEntry(path, Path.GetFileNameWithoutExtension(path)));
        }
    }

    [RelayCommand]
    private void RemoveIso(IsoEntry? iso)
    {
        if (iso is not null)
        {
            Isos.Remove(iso);
        }
    }

    private bool CanBuild() =>
        !IsBusy && SelectedDrive is not null && (IsRawImageMode ? Isos.Count == 1 : Isos.Count > 0);

    // Bound to the "Create USB" button; branches into either the multiboot flow or the raw dd-mode flow.
    [RelayCommand(CanExecute = nameof(CanBuild))]
    private async Task BuildAsync()
    {
        if (SelectedDrive is null)
        {
            return;
        }

        var confirmationMessage = IsRawImageMode
            ? $"This will ERASE ALL DATA on '{SelectedDrive.Model}' ({SelectedDrive.SizeGigabytes:F1} GB) and write " +
              $"'{Isos[0].DisplayName}' to it as a raw image (single ISO, no multiboot menu). Continue?"
            : $"This will ERASE ALL DATA on '{SelectedDrive.Model}' ({SelectedDrive.SizeGigabytes:F1} GB) and make it " +
              $"a multiboot USB with {Isos.Count} ISO(s). Continue?";

        var confirmed = MessageBox.Show(confirmationMessage, "Confirm USB wipe", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmed != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        BuildProgress = 0;
        _buildCts = new CancellationTokenSource();
        try
        {
            if (IsRawImageMode)
            {
                StatusText = "Writing raw ISO image...";
                var progress = new Progress<double>(p => BuildProgress = p * 100);
                await _usbBuilder.WriteRawIsoAsync(SelectedDrive.DiskNumber, Isos[0].IsoPath, progress, _buildCts.Token);
            }
            else
            {
                StatusText = "Building multiboot USB drive...";
                await _usbBuilder.BuildAsync(SelectedDrive.DiskNumber, [.. Isos], _buildCts.Token);
            }

            StatusText = "Done.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled.";
        }
        catch (UnsupportedIsoException ex)
        {
            AppendLog($"ERROR: {ex.Message}");
            StatusText = "Failed: unsupported ISO.";
            MessageBox.Show(
                $"{ex.Message}\n\nTip: enable 'Write raw ISO image' mode to write just this ISO directly (single ISO, no multiboot menu).",
                "Unsupported ISO",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
            StatusText = "Failed.";
        }
        finally
        {
            IsBusy = false;
            _buildCts.Dispose();
            _buildCts = null;
        }
    }

    [RelayCommand]
    private void CancelBuild() => _buildCts?.Cancel();

    private void AppendLog(string message) => LogLines.Add(message);

    partial void OnIsBusyChanged(bool value) => BuildCommand.NotifyCanExecuteChanged();

    partial void OnSelectedDriveChanged(UsbDriveInfo? value) => BuildCommand.NotifyCanExecuteChanged();

    partial void OnIsRawImageModeChanged(bool value) => BuildCommand.NotifyCanExecuteChanged();
}
