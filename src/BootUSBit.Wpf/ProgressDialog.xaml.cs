using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace BootUSBit.Wpf;

public partial class ProgressDialog : Window
{
    public ProgressDialog()
    {
        InitializeComponent();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = DataContext is MainViewModel { IsBuildInProgress: true };
    }

    private void WindowMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void WindowClose_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}