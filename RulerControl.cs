using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Diagnostics;
using Avalonia.VisualTree;
using Avalonia.Media.TextFormatting;

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

    // 常量定义
    private const double RulerHeight = 50;
    private const double HandleRadius = 8;

    // 旋转手柄相关
    private bool _isDraggingHandle;
    private double _handleStartAngleRad; // 按下手柄时鼠标相对旋转中心的角度（弧度）
    private double _startAngle; // 按下手柄时的初始角度

    // 整体拖拽相关
    private bool _isDragging;
    private Point _dragStartCanvasPos; // 拖拽开始时鼠标在画布的坐标
    private double _dragStartLeft;     // 拖拽开始时的Left
    private double _dragStartTop;      // 拖拽开始时的Top

    private const double TextFontSize = 10;
    private readonly Typeface _textTypeface = new Typeface("Arial");

    static RulerControl()
    {
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
        // 旋转中心：左边缘中点
        RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative);
        RenderTransform = new RotateTransform(Angle);
    }

    private void OnAngleChanged()
    {
        if (RenderTransform is RotateTransform rotateTransform)
            rotateTransform.Angle = Angle;
        else
            RenderTransform = new RotateTransform(Angle);
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double pixelLength = (LengthInCm / 2.54) * Dpi; // 厘米→英寸→像素
        return new Size(pixelLength, RulerHeight);
    }

    public override void Render(DrawingContext context)
    {
        double width = Bounds.Width;
        double height = Bounds.Height;

        // 背景
        context.FillRectangle(Brushes.LightGray, new Rect(0, 0, width, height));
        context.DrawRectangle(new Pen(Brushes.Black, 1), new Rect(0, 0, width, height));

        // 绘制刻度
        double mmToPixels = width / (LengthInCm * 10); // 每毫米像素数
        int totalMM = (int)(LengthInCm * 10);
        for (int mm = 0; mm <= totalMM; mm++)
        {
            double x = mm * mmToPixels;
            double tickHeight = mm % 10 == 0 ? height * 0.6 : (mm % 5 == 0 ? height * 0.4 : height * 0.2);
            context.DrawLine(new Pen(Brushes.Black, 1), new Point(x, 0), new Point(x, tickHeight));
        }

        // 绘制旋转手柄
        Point handleCenter = new Point(width - HandleRadius, HandleRadius);
        context.DrawEllipse(Brushes.Red, null, handleCenter, HandleRadius, HandleRadius);
    }

    // // 绘制垂直于屏幕的厘米数字（核心逻辑：抵消尺子旋转，让文本保持水平）
    // private void DrawCentimeterNumber(DrawingContext context, double x, double tickHeight, double cmValue)
    // {
    //     // 1. 转换刻度本地坐标为屏幕全局坐标（抵消尺子旋转的关键）
    //     var localPoint = new Point(x, tickHeight + 2); // 数字在大刻度下方2像素
    //     var globalPoint = PointToScreen(localPoint);   // 转屏幕坐标
    //     var renderPoint = ScreenToClient(globalPoint); // 转回控件绘制坐标（已抵消旋转）

    //     // 2. 生成数字文本（仅显示非0的厘米数，0刻度可省略）
    //     string text = cmValue == 0 ? "" : $"{cmValue:F0}cm";

    //     // 3. 绘制文本（保持垂直于屏幕，无旋转）
    //     var textLayout = new TextLayout(text:text, _textTypeface,TextFontSize,Brushes.Black);
    //     context.DrawText(
    //         brush: Brushes.Black,
    //         origin: new Point(renderPoint.X - textLayout.Width / 2, renderPoint.Y), // 文本居中
    //         textLayout: textLayout
    //     );
    // }

    // // 【最终修正：适配所有Avalonia版本的HitTestCore】
    // // 1. 正确签名：返回Visual，参数是PointHitTestParameters（来自Avalonia.Input）
    // // 2. Control继承自Visual，所以能重写这个方法
    // protected override Visual? HitTestCore(PointHitTestParameters hitTestParameters)
    // {
    //     // 强制整个Bounds区域可点击（即使无背景）
    //     if (Bounds.Contains(hitTestParameters.HitPoint))
    //     {
    //         return this;
    //     }
    //     // 调用Visual基类的HitTestCore（Control的父类）
    //     return base.HitTestCore(hitTestParameters);
    // }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        Point localPoint = e.GetPosition(this);
        double width = Bounds.Width;
        double height = Bounds.Height;
        Point handleCenter = new Point(width - HandleRadius, HandleRadius);

        // 点击旋转手柄
        if (Vector.Distance(localPoint, handleCenter) <= HandleRadius)
        {
            _isDraggingHandle = true;
            _startAngle = Angle;
            Point rotateCenter = new Point(0, height / 2); // 旋转中心（左边缘中点）
            double dx = localPoint.X - rotateCenter.X;
            double dy = localPoint.Y - rotateCenter.Y;
            _handleStartAngleRad = Math.Atan2(dy, dx); // 鼠标相对旋转中心的初始角度
            e.Pointer.Capture(this);
            e.Handled = true;
        }
        // 点击尺子本体（拖拽）
        else if (Parent is Canvas canvas)
        {
            _isDragging = true;
            _dragStartCanvasPos = e.GetPosition(canvas); // 记录画布坐标
            _dragStartLeft = Canvas.GetLeft(this);       // 记录初始Left
            _dragStartTop = Canvas.GetTop(this);         // 记录初始Top
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // 处理旋转
        if (_isDraggingHandle)
        {
            double height = Bounds.Height;
            Point localPoint = e.GetPosition(this);
            Point rotateCenter = new Point(0, height / 2);

            // 计算当前鼠标相对旋转中心的角度
            double dx = localPoint.X - rotateCenter.X;
            double dy = localPoint.Y - rotateCenter.Y;
            double currentAngleRad = Math.Atan2(dy, dx);

            // 角度差 = 当前角度 - 初始角度，转换为角度值
            double angleDelta = (currentAngleRad - _handleStartAngleRad) * 180 / Math.PI;
            Angle = _startAngle + angleDelta; // 直接叠加差值，无累积错误
            e.Handled = true;
        }
        // 处理拖拽
        else if (_isDragging && Parent is Canvas canvas)
        {
            Point currentCanvasPos = e.GetPosition(canvas);
            // 计算画布坐标偏移量
            double deltaX = currentCanvasPos.X - _dragStartCanvasPos.X;
            double deltaY = currentCanvasPos.Y - _dragStartCanvasPos.Y;

            // 新位置 = 初始位置 + 偏移量（边界限制）
            double newLeft = Math.Max(0, Math.Min(_dragStartLeft + deltaX, canvas.Bounds.Width - Bounds.Width));
            double newTop = Math.Max(0, Math.Min(_dragStartTop + deltaY, canvas.Bounds.Height - Bounds.Height));

            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
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
        else if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}