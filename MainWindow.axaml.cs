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
            Angle = 90,
            MeasurePosition = 4.8
        };

        // 初始位置
        double left = (DrawingCanvas.Bounds.Width - ruler.Bounds.Width) / 2;
        double top = (DrawingCanvas.Bounds.Height - ruler.Bounds.Height) * 0.2;
        Canvas.SetLeft(ruler, left);
        Canvas.SetTop(ruler, top);

        // 无需绑定拖拽事件（逻辑已移到RulerControl内部）
        DrawingCanvas.Children.Add(ruler);
    }
}