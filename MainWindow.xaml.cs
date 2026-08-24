using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FontsQuickView;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        Title = "字体速览";

        RootGrid.DataContext = _viewModel;

        RootGrid.Loaded += (_, _) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _viewModel.LoadFonts();
            });
        };

        SystemBackdrop = new MicaBackdrop();
        SetWindowSize(1000, 720);
    }

    private void SetWindowSize(int width, int height)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        if (Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId) is { } appWindow)
        {
            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        }
    }
}