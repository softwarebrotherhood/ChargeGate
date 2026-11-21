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
            var iconUri = new Uri("pack://application:,,,/Resources/app.ico", UriKind.RelativeOrAbsolute);
            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        }
        catch { }

        try
        {
            TrayIcon.Icon = SystemIcons.Application;
        }
        catch { }

        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName is nameof(vm.BatteryPercentage) or nameof(vm.IsPluggedIn) or nameof(vm.IsCharging) or nameof(vm.SelectedThreshold))
                {
                    Dispatcher.Invoke(() => UpdateTrayIcon(vm.BatteryPercentage, vm.IsPluggedIn, vm.IsCharging, vm.SelectedThreshold));
                }
            };
            UpdateTrayIcon(vm.BatteryPercentage, vm.IsPluggedIn, vm.IsCharging, vm.SelectedThreshold);
        }
    }

    private void UpdateTrayIcon(int percentage, bool isPluggedIn, bool isCharging, int threshold)
    {
        try
        {
            using var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Color borderColor = (isPluggedIn && !isCharging)
                    ? Color.FromArgb(16, 185, 129) // Gated (Green)
                    : isCharging
                        ? Color.FromArgb(59, 130, 246) // Charging (Blue)
                        : percentage <= 20
                            ? Color.FromArgb(239, 68, 68) // Low (Red)
                            : Color.FromArgb(245, 158, 11); // Discharging (Amber)

                using var bgBrush = new SolidBrush(Color.FromArgb(220, 20, 20, 20));
                g.FillEllipse(bgBrush, 0, 0, 15, 15);

                using var pen = new Pen(borderColor, 1.4f);
                g.DrawEllipse(pen, 0.7f, 0.7f, 13.6f, 13.6f);

                string text = percentage > 0 ? percentage.ToString() : "CG";
                using var font = new System.Drawing.Font("Segoe UI", 6.5f, System.Drawing.FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);

                var size = g.MeasureString(text, font);
                float x = (16f - size.Width) / 2f + 0.3f;
                float y = (16f - size.Height) / 2f;
                g.DrawString(text, font, textBrush, x, y);
            }

            var hIcon = bitmap.GetHicon();
            var icon = System.Drawing.Icon.FromHandle(hIcon);
            TrayIcon.Icon = icon;
            TrayIcon.ToolTipText = $"ChargeGate: {percentage}% • Gated at {threshold}%";
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
