using System.Windows;
using Wpf.Ui.Appearance;

namespace ChargeGate.UI;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            string details = e.Exception.Message;
            if (e.Exception.InnerException != null)
            {
                details += $"\n\nInner: {e.Exception.InnerException.Message}";
            }
            MessageBox.Show(
                $"ChargeGate Error: {details}\n\nStack:\n{e.Exception.StackTrace}",
                "ChargeGate Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            ApplicationThemeManager.ApplySystemTheme();
        }
        catch
        {
            try
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            }
            catch { }
        }
    }
}
