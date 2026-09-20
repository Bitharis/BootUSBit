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
    private readonly IProgressLogger _logger;
    private readonly FileLoggerOptions _fileLoggerOptions;
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
    private bool _isBuildInProgress;

    [ObservableProperty]
    private string _statusText = "Ready.";

    /// <summary>When set, writes a single ISO raw to the whole disk instead of building a multiboot menu.</summary>
    [ObservableProperty]
    private bool _isRawImageMode;

    [ObservableProperty]
    private double _buildProgress;

    public MainViewModel()
    {
        _fileLoggerOptions = AppSettings.LoadFileLoggerOptions();
        var fileLogger = new FileProgressLogger(_fileLoggerOptions);
        _logger = new CompositeProgressLogger(new UiProgressLogger(AppendLog), fileLogger);
        _usbBuilder = new UsbBuilder(logger: _logger);
        Isos.CollectionChanged += (_, _) => BuildCommand.NotifyCanExecuteChanged();
    }

    public LogLevel MinimumLogLevel => _fileLoggerOptions.MinimumLevel;

    public void SetMinimumLogLevel(LogLevel level)
    {
        _fileLoggerOptions.MinimumLevel = level;
        AppSettings.SaveFileLoggerOptions(_fileLoggerOptions);
        OnPropertyChanged(nameof(MinimumLogLevel));
    }

    [RelayCommand]
    private async Task RefreshDrivesAsync()
    {
        IsBusy = true;
        try
        {
            var drives = await Task.Run(() => _usbBuilder.GetUsbDrivesAsync());
            Drives.Clear();
            foreach (var drive in drives)
            {
                Drives.Add(drive);
            }
            StatusText = $"Found {Drives.Count} USB drive(s).";
            StatusIndicator = "READY";
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
            _logger.Error($"Drive refresh failed: {ex}");
            StatusText = "Failed. See the activity log for details.";
            StatusIndicator = "FAILED";
            MessageBox.Show(
                ex.Message,
                "USB creation failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
        StatusIndicator = "WORKING";
        _buildCts = new CancellationTokenSource();
        IsBuildInProgress = true;
        try
        {
            var progress = new Progress<double>(p => BuildProgress = p * 100);
            if (IsRawImageMode)
            {
                StatusText = "Writing raw ISO image...";
                await Task.Run(
                    () => _usbBuilder.WriteRawIsoAsync(SelectedDrive.DiskNumber, Isos[0].IsoPath, progress, _buildCts.Token));
            }
            else
            {
                StatusText = "Building multiboot USB drive...";
                await Task.Run(
                    () => _usbBuilder.BuildAsync(SelectedDrive.DiskNumber, [.. Isos], progress, _buildCts.Token));
            }

            BuildProgress = 100;
            StatusText = "Completed successfully.";
            StatusIndicator = "SUCCESS";
            _logger.Info("USB creation completed successfully.");
        }
        catch (OperationCanceledException)
        {
            StatusText = "Creation cancelled.";
            StatusIndicator = "CANCELLED";
            _logger.Warn("USB creation was cancelled.");
        }
        catch (UnsupportedIsoException ex)
        {
            _logger.Error(ex.ToString());
            StatusText = "Failed: unsupported ISO.";
            StatusIndicator = "FAILED";
            MessageBox.Show(
                $"{ex.Message}\n\nTip: enable 'Write raw ISO image' mode to write just this ISO directly (single ISO, no multiboot menu).",
                "Unsupported ISO",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
            StatusText = "Creation failed. See the activity log.";
            StatusIndicator = "FAILED";
        }
        finally
        {
            _buildCts.Dispose();
            _buildCts = null;
            IsBusy = false;
            IsBuildInProgress = false;
        }
    }

    [RelayCommand]
    private void CancelBuild() => _buildCts?.Cancel();

    private void AppendLog(string message)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            LogLines.Add(message);
            return;
        }

        Application.Current.Dispatcher.Invoke(() => LogLines.Add(message));
    }

    [ObservableProperty]
    private string _statusIndicator = "READY";

    partial void OnIsBusyChanged(bool value) => BuildCommand.NotifyCanExecuteChanged();

    partial void OnSelectedDriveChanged(UsbDriveInfo? value) => BuildCommand.NotifyCanExecuteChanged();

    partial void OnIsRawImageModeChanged(bool value) => BuildCommand.NotifyCanExecuteChanged();
}
