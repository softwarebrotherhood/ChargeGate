using Wpf.Ui.Controls;

namespace ChargeGate.UI;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        ExtendsContentIntoTitleBar = true;
        WindowBackdropType = WindowBackdropType.None;
        InitializeComponent();
    }
}
