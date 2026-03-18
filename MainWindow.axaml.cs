using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Diagnostics;

namespace TestAvalonia;

public partial class MainWindow : Window
{
    private Control? _draggedRuler;
    private Point _dragOffset;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void AddRuler_Click(object sender, RoutedEventArgs e)
    {
        var ruler = new RulerControl
        {
            Dpi = 91.79,                // 可根据实际屏幕 DPI 设置
            LengthInCm = 20,
        };

        // 初始位置（画布中央）
        double left = (DrawingCanvas.Bounds.Width - ruler.Bounds.Width) / 2;
        double top = (DrawingCanvas.Bounds.Height - ruler.Bounds.Height) / 2;
        Canvas.SetLeft(ruler, left);
        Canvas.SetTop(ruler, top);

        // 附加整体拖拽事件
        ruler.PointerPressed += Ruler_PointerPressed;
        ruler.PointerMoved += Ruler_PointerMoved;
        ruler.PointerReleased += Ruler_PointerReleased;

        DrawingCanvas.Children.Add(ruler);
    }

    private void Ruler_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 如果内部手柄已处理（e.Handled == true），则不再响应整体拖拽
        if (e.Handled) return;
        if (sender is Control ruler)
        {
            _draggedRuler = ruler;
            var point = e.GetPosition(ruler);
            _dragOffset = new Point(point.X, point.Y);
            e.Pointer.Capture(ruler);
            e.Handled = true;
        }
    }

    private void Ruler_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedRuler != null && sender == _draggedRuler)
        {
            var currentPos = e.GetPosition(DrawingCanvas);
            double left = currentPos.X - _dragOffset.X;
            double top = currentPos.Y - _dragOffset.Y;

            // 可选：边界限制（防止完全拖出画布）
            left = Math.Max(0, Math.Min(left, DrawingCanvas.Bounds.Width - _draggedRuler.Bounds.Width));
            top = Math.Max(0, Math.Min(top, DrawingCanvas.Bounds.Height - _draggedRuler.Bounds.Height));

            Canvas.SetLeft(_draggedRuler, left);
            Canvas.SetTop(_draggedRuler, top);
            e.Handled = true;
        }
    }

    private void Ruler_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedRuler != null && sender == _draggedRuler)
        {
            e.Pointer.Capture(null);
            _draggedRuler = null;
            e.Handled = true;
        }
    }
}