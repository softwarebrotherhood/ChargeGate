using System.ComponentModel;
using System.Drawing;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Wpf.Ui.Controls;

namespace ChargeGate.UI;

public partial class MainWindow : FluentWindow
{
    private bool _isExplicitExit = false;

    public MainWindow()
    {
        ExtendsContentIntoTitleBar = true;
        WindowBackdropType = WindowBackdropType.None;
        InitializeComponent();

        try
        {
            TrayIcon.Icon = SystemIcons.Application;
        }
        catch { }
    }

    private void OnTrayDoubleClick(object sender, RoutedEventArgs e)
    {
        RestoreFromTray();
    }

    private void OnTrayOpenClick(object sender, RoutedEventArgs e)
    {
        RestoreFromTray();
    }

    private void OnTrayExitClick(object sender, RoutedEventArgs e)
    {
        _isExplicitExit = true;
        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void RestoreFromTray()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            this.Hide();
            TrayIcon.ShowBalloonTip("ChargeGate", "ChargeGate is minimizing to the system tray to protect your battery in the background.", BalloonIcon.Info);
        }
        else
        {
            base.OnClosing(e);
        }
    }
}
