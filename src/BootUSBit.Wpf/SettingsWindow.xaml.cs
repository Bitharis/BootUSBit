using System.Windows;
using BootUSBit.Core.Diagnostics;

namespace BootUSBit.Wpf;

public partial class SettingsWindow : Window
{
    public SettingsWindow(LogLevel currentLevel)
    {
        InitializeComponent();
        var levels = Enum.GetValues<LogLevel>()
            .Select(level => new LogLevelOption(level, level.ToString()))
            .ToList();
        LogLevelComboBox.ItemsSource = levels;
        LogLevelComboBox.SelectedItem = levels.First(level => level.Value == currentLevel);
    }

    public LogLevel? SelectedLogLevel => (LogLevelComboBox.SelectedItem as LogLevelOption)?.Value;

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private sealed record LogLevelOption(LogLevel Value, string Name);
}
