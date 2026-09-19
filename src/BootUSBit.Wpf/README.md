# BootUSBit.Wpf

WPF desktop UI for BootUSBit. See the [repo README](../../README.md) for the overall architecture and how
to build/run.

- `MainWindow.xaml` / `MainWindow.xaml.cs` — main window (drive picker, ISO list, log, progress)
- `MainViewModel.cs` — view model: drive/ISO selection, build/cancel commands, raw-image-mode toggle
- `UiProgressLogger.cs` — marshals Info-and-above log messages onto the UI thread
- `AppSettings.cs` / `appsettings.json` — loads the `Logging` section (file logger configuration)
- `app.manifest` — requires administrator elevation (needed for raw disk writes and `diskpart`)

Requires elevation to run; Windows will show a UAC prompt on launch.
