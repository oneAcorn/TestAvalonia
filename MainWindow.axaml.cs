using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace TestAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void AddRuler_Click(object sender, RoutedEventArgs e)
    {
        var ruler = new RulerControl
        {
            Dpi = 91.79,
            LengthInCm = 10.8,
            Angle = 0,
            MeasurePosition = 4.8,
            DragAngleEnable = true,
            SubMeasureLineEnable = false
        };

        // 初始位置
        double left = (DrawingCanvas.Bounds.Width - ruler.Bounds.Width) * 0.3;
        double top = (DrawingCanvas.Bounds.Height - ruler.Bounds.Height) * 0.1;
        Canvas.SetLeft(ruler, left);
        Canvas.SetTop(ruler, top);

        DrawingCanvas.Children.Add(ruler);
    }

    private void AddRuler_Click2(object sender, RoutedEventArgs e)
    {
        var ruler = new RulerControl
        {
            Dpi = 91.79,
            LengthInCm = 10.8,
            Angle = 90,
            MeasurePosition = 4.8,
            DragAngleEnable = false,
            SubMeasureLineEnable = true,
            SubMeasurePosition = 2.0
        };

        // 初始位置
        double left = (DrawingCanvas.Bounds.Width - ruler.Bounds.Width) / 2;
        double top = (DrawingCanvas.Bounds.Height - ruler.Bounds.Height) * 0.2;
        Canvas.SetLeft(ruler, left);
        Canvas.SetTop(ruler, top);

        DrawingCanvas.Children.Add(ruler);
    }

    private void AddRuler_Click3(object sender, RoutedEventArgs e)
    {
        var ruler = new RulerControl
        {
            Dpi = 91.79,
            LengthInCm = 10.8,
            Angle = 90,
            MeasurePosition = 4.8,
            DragAngleEnable = true,
            SubMeasureLineEnable = true,
            SubMeasurePosition = 2.0
        };

        // 初始位置
        double left = (DrawingCanvas.Bounds.Width - ruler.Bounds.Width) * 0.75;
        double top = (DrawingCanvas.Bounds.Height - ruler.Bounds.Height) * 0.1;
        Canvas.SetLeft(ruler, left);
        Canvas.SetTop(ruler, top);

        DrawingCanvas.Children.Add(ruler);
    }
}