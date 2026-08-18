using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SillyPutty;
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => WriteCrashLog(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
        TaskScheduler.UnobservedTaskException += (_, e) => { WriteCrashLog(e.Exception); e.SetObserved(); };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var path = WriteCrashLog(e.Exception);
        MessageBox.Show($"SillyPutty encountered an unexpected error.\n\nA diagnostic log was written to:\n{path}", "SillyPutty error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; Shutdown(-1);
    }

    private static string WriteCrashLog(Exception exception)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SillyPutty", "CrashLogs"); Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
            var text = new StringBuilder().AppendLine($"SillyPutty crash at {DateTimeOffset.Now:O}").AppendLine($"Version: {typeof(App).Assembly.GetName().Version}").AppendLine($"OS: {Environment.OSVersion}").AppendLine().AppendLine(exception.ToString()).ToString();
            File.WriteAllText(path, text); return path;
        }
        catch { return "(crash log could not be written)"; }
    }
}
