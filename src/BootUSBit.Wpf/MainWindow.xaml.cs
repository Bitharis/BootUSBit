using System.Text;
using System.IO;
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
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => UpdateWindowButtons();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.RefreshDrivesCommand.ExecuteAsync(null);
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