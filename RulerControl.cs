using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Diagnostics;

namespace TestAvalonia;

public class RulerControl : Control
{
    // 依赖属性
    public static readonly StyledProperty<double> DpiProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(Dpi), 96.0);

    public static readonly StyledProperty<double> LengthInCmProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(LengthInCm), 20.0);

    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(Angle), 0.0);

    public double Dpi
    {
        get => GetValue(DpiProperty);
        set => SetValue(DpiProperty, value);
    }

    public double LengthInCm
    {
        get => GetValue(LengthInCmProperty);
        set => SetValue(LengthInCmProperty, value);
    }

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    private const double RulerHeight = 50;
    private const double HandleRadius = 8;

    private bool _isDraggingHandle;
    private double _handleLocalAngle; // 弧度

    static RulerControl()
    {
        Console.WriteLine("ruler init");
        AngleProperty.Changed.AddClassHandler<RulerControl>((control, e) => control.OnAngleChanged());
        DpiProperty.Changed.AddClassHandler<RulerControl>((control, e) =>
        {
            control.InvalidateMeasure();
            control.InvalidateVisual();
        });
        LengthInCmProperty.Changed.AddClassHandler<RulerControl>((control, e) =>
        {
            control.InvalidateMeasure();
            control.InvalidateVisual();
        });
    }

    public RulerControl()
    {
        RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative);
        RenderTransform = new RotateTransform(Angle);
    }

    private void OnAngleChanged()
    {
        if (RenderTransform is RotateTransform rotateTransform)
            rotateTransform.Angle = Angle;
        else
            RenderTransform = new RotateTransform(Angle);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double pixelLength = (LengthInCm / 2.54) * Dpi; // 厘米 → 英寸 → 像素
        Console.WriteLine($"cm:{LengthInCm},dpi:{Dpi},pxLen:{pixelLength}");
        return new Size(pixelLength, RulerHeight);
    }

    public override void Render(DrawingContext context)
    {
        double width = Bounds.Width;
        double height = Bounds.Height;
        // Console.WriteLine($"Ruler Render size:({width},{height})");

        // 背景
        context.FillRectangle(Brushes.LightGray, new Rect(0, 0, width, height));
        context.DrawRectangle(new Pen(Brushes.Black, 1), new Rect(0, 0, width, height));

        // 绘制刻度
        double mmToPixels = width / (LengthInCm * 10); // 每毫米像素数
        int totalMM = (int)(LengthInCm * 10);
        for (int mm = 0; mm <= totalMM; mm++)
        {
            double x = mm * mmToPixels;
            double tickHeight;
            if (mm % 10 == 0)
                tickHeight = height * 0.6; // 大刻度
            else if (mm % 5 == 0)
                tickHeight = height * 0.4; // 中刻度
            else
                tickHeight = height * 0.2; // 小刻度

            context.DrawLine(new Pen(Brushes.Black, 1), new Point(x, 0), new Point(x, tickHeight));
        }

        // 绘制旋转手柄（红色圆）
        Point handleCenter = new Point(width - HandleRadius, HandleRadius);
        context.DrawEllipse(Brushes.Red, null, handleCenter, HandleRadius, HandleRadius);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Point p = e.GetPosition(this);
            double width = Bounds.Width;
            double height = Bounds.Height;
            Point handleCenter = new Point(width - HandleRadius, HandleRadius);
            // 修正2：使用 Vector.Distance 计算两点距离
            double dist = Vector.Distance(p, handleCenter);
            if (dist <= HandleRadius)
            {
                _isDraggingHandle = true;
                double dx = handleCenter.X - 0;
                double dy = handleCenter.Y - height / 2;
                _handleLocalAngle = Math.Atan2(dy, dx);
                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDraggingHandle)
        {
            Point p = e.GetPosition(this);
            double height = Bounds.Height;
            double dx = p.X - 0;
            double dy = p.Y - height / 2;
            double mouseAngle = Math.Atan2(dy, dx);
            double newAngleRad = mouseAngle - _handleLocalAngle;
            double newAngleDeg = newAngleRad * 180 / Math.PI;
            Angle = newAngleDeg;
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDraggingHandle)
        {
            _isDraggingHandle = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}