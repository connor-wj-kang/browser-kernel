#region

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SkiaSharp.Views.Desktop;

#endregion

namespace Browser;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public static float CanvasWidth;
    public static float CanvasHeight;
    public static BrowserData Browser = new();

    public static DispatcherTimer Timer = new() {
        Interval = TimeSpan.FromMilliseconds(3)
    };


    public MainWindow() {
        InitializeComponent();
        Browser.NewTab(new Uri("http://localhost:3000/count.html"));
        // Browser.NewTab(new Uri("https://example.com/"));
        CanvasElement.PaintSurface += OnPaintSurface;
        CanvasElement.Loaded += OnLoaded;
        CanvasElement.SizeChanged += OnSizeChanged;
        CanvasElement.MouseWheel += CanvasElementOnMouseWheel;
        CanvasElement.MouseDown += OnMouseDown;
        Timer.Tick += TimerOnTick;
        Timer.Start();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e) {
        var x = e.GetPosition(this).X;
        var y = e.GetPosition(this).Y;
        Browser.HandleClick((float)x, (float)y);
    }

    private void CanvasElementOnMouseWheel(object sender, MouseWheelEventArgs e) {
        var step = e.Delta;
        Browser.HandleDown(step);
    }

    private void TimerOnTick(object? sender, EventArgs e) {
        CanvasElement.InvalidateVisual();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        CanvasWidth = (float)CanvasElement.ActualWidth;
        CanvasHeight = (float)CanvasElement.ActualHeight;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        CanvasWidth = (float)e.NewSize.Width;
        CanvasHeight = (float)e.NewSize.Height;
        Browser.ActiveTab?.SetNeedsRender();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e) {
        var canvas = e.Surface.Canvas;
        Browser.RasterAndDraw(canvas);
        Browser.ScheduleAnimationFrame();
    }
}