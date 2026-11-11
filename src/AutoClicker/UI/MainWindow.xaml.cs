using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using AutoClicker.Native;

namespace AutoClicker.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyWindowChrome();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void ApplyWindowChrome()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int dark = 1;
        NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        int round = 2; // DWMWCP_ROUND (Windows 11; ignored on Windows 10)
        NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
    }

    private void CpsBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => Vm?.CommitCps();

    private void CpsBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Vm?.CommitCps();
        CpsBox.SelectAll();
        e.Handled = true;
    }

    private void CpsBox_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsDigit);
}
