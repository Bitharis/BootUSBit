using System.Text;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ProgressDialog? _progressDialog;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            ApplyWorkAreaBounds();
            UpdateWindowButtons();
        };
        StateChanged += (_, _) =>
        {
            ApplyWorkAreaBounds();
            UpdateWindowButtons();
        };
        Closing += MainWindow_Closing;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
            await vm.RefreshDrivesCommand.ExecuteAsync(null);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsBuildInProgress) ||
            sender is not MainViewModel vm)
        {
            return;
        }

        if (vm.IsBuildInProgress)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (vm.IsBuildInProgress)
                {
                    ShowProgressDialog(vm);
                }
            });
        }
        else
        {
            _progressDialog?.Close();
        }
    }

    private void ShowProgressDialog(MainViewModel vm)
    {
        if (_progressDialog is not null)
        {
            return;
        }

        var dialog = new ProgressDialog
        {
            Owner = this,
            DataContext = vm,
        };
        _progressDialog = dialog;
        dialog.ShowDialog();
        if (ReferenceEquals(_progressDialog, dialog))
        {
            _progressDialog = null;
        }
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        var logPath = AppSettings.LoadFileLoggerOptions().FilePath;
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, logPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{fullPath}\"")
        {
            UseShellExecute = true,
        });
    }

    private void LogLevel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag } || !Enum.TryParse<LogLevel>(tag, out var level) ||
            DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.SetMinimumLogLevel(level);
        UpdateLogLevelChecks();
    }

    private void WindowMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void WindowMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateWindowButtons();
    }

    private void WindowClose_Click(object sender, RoutedEventArgs e) => Close();

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = DataContext is MainViewModel { IsBuildInProgress: true };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowMaximize_Click(sender, e);
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void UpdateWindowButtons()
    {
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        UpdateLogLevelChecks();
    }

    private void ApplyWorkAreaBounds()
    {
        var workArea = SystemParameters.WorkArea;
        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;
    }

    private void UpdateLogLevelChecks()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        foreach (var item in LogLevelMenu.Items.OfType<MenuItem>())
        {
            item.IsChecked = item.Tag is string tag && Enum.TryParse<LogLevel>(tag, out var level) &&
                             level == vm.MinimumLogLevel;
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "BootUSBit\nMultiboot USB creator",
            "About BootUSBit",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}