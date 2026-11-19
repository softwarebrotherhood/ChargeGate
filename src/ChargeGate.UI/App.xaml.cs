using System.Windows;
using Wpf.Ui.Appearance;

namespace ChargeGate.UI;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show(
                $"ChargeGate Error: {e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}",
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
