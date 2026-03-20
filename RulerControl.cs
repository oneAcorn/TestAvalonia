using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TestAvalonia;

public class RulerControl : Control
{
    // Styled properties
    public static readonly StyledProperty<double> DpiProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(Dpi), 96.0);

    public static readonly StyledProperty<double> LengthInCmProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(LengthInCm), 20.0);

    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(Angle), 0.0);

    public static readonly StyledProperty<double> MeasurePositionProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(MeasurePosition), 0.0);

    public static readonly StyledProperty<bool> DragAngleEnableProperty =
        AvaloniaProperty.Register<RulerControl, bool>(nameof(DragAngleEnable), true);

    public static readonly StyledProperty<bool> SubMeasureLineEnableProperty =
        AvaloniaProperty.Register<RulerControl, bool>(nameof(SubMeasureLineEnable), false);

    // public static readonly StyledProperty<bool> SubMeasureLineFixedProperty =
    //     AvaloniaProperty.Register<RulerControl, bool>(nameof(SubMeasureLineFixed), false);

    public static readonly StyledProperty<double> SubMeasurePositionProperty =
        AvaloniaProperty.Register<RulerControl, double>(nameof(SubMeasurePosition), 2.0);

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

    /// <summary>
    /// 测量线位置(厘米)
    /// </summary>
    public double MeasurePosition
    {
        get => GetValue(MeasurePositionProperty);
        set => SetValue(MeasurePositionProperty, value);
    }

    /// <summary>
    /// 是否允许拖拽角度
    /// </summary>
    public bool DragAngleEnable
    {
        get => GetValue(DragAngleEnableProperty);
        set => SetValue(DragAngleEnableProperty, value);
    }

    /// <summary>
    /// 是否显示子测量线
    /// </summary>
    public bool SubMeasureLineEnable
    {
        get => GetValue(SubMeasureLineEnableProperty);
        set => SetValue(SubMeasureLineEnableProperty, value);
    }

    // /// <summary>
    // /// 子测量线是否固定,跟随测量线.
    // /// </summary>
    // public bool SubMeasureLineFixed
    // {
    //     get => GetValue(SubMeasureLineFixedProperty);
    //     set => SetValue(SubMeasureLineFixedProperty, value);
    // }

    public double SubMeasurePosition
    {
        //TODO 设置合法范围
        get => GetValue(SubMeasurePositionProperty);
        set => SetValue(SubMeasurePositionProperty, value);
    }

    public double ArrowHeadLength { get; set; } = 10;
    public double ArrowHeadWidth { get; set; } = 6;

    // Constants
    private const double RulerHeight = 100;
    private const double HandleRadius = 8;
    // The measure line extends RulerHeight/2 above and below the ruler body,
    // giving a total visible length of RulerHeight (body) + RulerHeight/2 + RulerHeight/2 = 2 * RulerHeight.
    private const double MeasureLineExtend = RulerHeight / 2;
    private const double MeasureLineHitThreshold = 18.0;

    // Rotation handle state
    private bool _isDraggingHandle;
    private double _handleStartAngleRad;
    private double _startAngle;

    // Body drag state
    private bool _isDragging;
    private Point _dragStartCanvasPos;
    private double _dragStartLeft;
    private double _dragStartTop;

    // Measure line state
    // private double _measureLineX;           // current X position in local ruler coordinates
    private bool _isDraggingMeasureLine;
    private double _measureLineDragStartLocalX;
    private double _measureLineStartX;
    private double _measureTextPadding = 5;
    private Typeface boldTf = new Typeface(FontFamily.Default, weight: FontWeight.Bold);

    private double _mm2Pixcel;

    private SolidColorBrush _scaleBrush = new SolidColorBrush(Color.Parse("#FFBEBFCA"));
    private SolidColorBrush _measureAreaBrush = new SolidColorBrush(Color.Parse("#1A22C55E"));
    private SolidColorBrush _arrowBrush = new SolidColorBrush(Color.Parse("#FF1FB255"));
    private SolidColorBrush _totalMeasureValueBrush = new SolidColorBrush(Color.Parse("#FF22C55E"));

    static RulerControl()
    {
        AngleProperty.Changed.AddClassHandler<RulerControl>((c, _) => c.OnAngleChanged());
        DpiProperty.Changed.AddClassHandler<RulerControl>((c, _) =>
        {
            c.InvalidateMeasure();
            c.InvalidateVisual();
        });
        LengthInCmProperty.Changed.AddClassHandler<RulerControl>((c, _) =>
        {
            c.InvalidateMeasure();
            c.InvalidateVisual();
        });

        MeasurePositionProperty.Changed.AddClassHandler<RulerControl>((c, _) =>
        {
            c.InvalidateVisual();
        });
    }

    public RulerControl()
    {
        // Rotation origin: left-edge midpoint
        RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative);
        RenderTransform = new RotateTransform(Angle);
        // Allow the measure line and its label to render outside the control's layout bounds
        ClipToBounds = false;
    }

    private void OnAngleChanged()
    {
        if (RenderTransform is RotateTransform rt)
            rt.Angle = Angle;
        else
            RenderTransform = new RotateTransform(Angle);
        // Console.WriteLine($"curAngle:{Angle} transform:{RenderTransform}");
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double pixelLength = (LengthInCm / 2.54) * Dpi; // cm → inch → pixels
        _mm2Pixcel = pixelLength / (LengthInCm * 10);
        return new Size(pixelLength, RulerHeight);
    }

    public override void Render(DrawingContext context)
    {
        double width = Bounds.Width;
        double height = Bounds.Height;

        // Tick marks and cm labels
        // double mmToPixels = width / (LengthInCm * 10);

        // Background and border
        // context.FillRectangle(Brushes.LightGray, new Rect(0, 0, width, height));
        // context.DrawRectangle(new Pen(Brushes.Black, 1), new Rect(0, 0, width, height));
        DrawMeasuredArea(context, width, height);

        int totalMM = (int)(LengthInCm * 10);
        for (int mm = 0; mm <= totalMM; mm++)
        {
            double x = mm * _mm2Pixcel;
            double tickH;
            if (mm % 10 == 0)
            {
                tickH = height * 0.26;
                DrawCMNumber(context, x, tickH, mm / 10.0);
            }
            else if (mm % 5 == 0)
                tickH = height * 0.18;
            else
                tickH = height * 0.12;

            context.DrawLine(new Pen(_scaleBrush, 1), new Point(x, 0), new Point(x, tickH));
        }

        // Measure line (drawn before handle so handle renders on top)
        DrawMeasureLine(context, width, height);

        // Rotation handle (red circle, top-right corner)
        if (DragAngleEnable)
        {
            Point handleCenter = new Point(width - HandleRadius, HandleRadius + height / 2.0);
            context.DrawEllipse(Brushes.Red, null, handleCenter, HandleRadius, HandleRadius);
        }
    }

    private void DrawMeasuredArea(DrawingContext context, double width, double height)
    {
        var measureLineX = MeasurePosition * 10 * _mm2Pixcel;
        double lineX = Math.Clamp(measureLineX, 0, width);
        context.FillRectangle(_measureAreaBrush, new Rect(0, 0, lineX, height));
    }

    private void DrawArrow(DrawingContext context, Point start, Point end, IBrush fill, Pen pen)
    {
        Vector direction = end - start;
        double length = direction.Length;
        if (length < double.Epsilon) return; // 忽略零长度向量

        // 手动计算归一化方向向量
        Vector normalized = direction / length;

        // 计算垂直向量（用于箭头两侧）
        Vector perpendicular = new Vector(-normalized.Y, normalized.X);

        Point arrowTip = end;

        // 三角形底边中点（在箭头尖端后方）
        Point baseMid = end - normalized * ArrowHeadLength;

        // 三角形底边两个端点
        Point baseLeft = baseMid + perpendicular * (ArrowHeadWidth / 2);
        Point baseRight = baseMid - perpendicular * (ArrowHeadWidth / 2);

        // 绘制直线（只画到三角形底边中点，避免箭头覆盖直线）
        context.DrawLine(pen, start, baseMid);

        // 构建并绘制箭头三角形
        var arrowHead = new StreamGeometry();
        using (var ctx = arrowHead.Open())
        {
            ctx.BeginFigure(arrowTip, true); // 起点为箭头尖端，并填充
            ctx.LineTo(baseLeft);
            ctx.LineTo(baseRight);
            ctx.EndFigure(true); // 闭合图形
        }
        context.DrawGeometry(fill, null, arrowHead);
    }

    /// <summary>
    /// Draws the green dashed measure line and its upright cm label.
    /// The line is perpendicular to the ruler (vertical in local space) with a total height
    /// of 2 × RulerHeight: it extends MeasureLineExtend above and below the ruler body.
    /// </summary>
    private void DrawMeasureLine(DrawingContext context, double width, double height)
    {
        if (width <= 0) return;

        var measureLineX = MeasurePosition * 10 * _mm2Pixcel;
        double lineX = Math.Clamp(measureLineX, 0, width);
        double lineTop = -MeasureLineExtend;          // above the ruler
        double lineBottom = height + MeasureLineExtend;  // below the ruler
        // total = height + 2 * MeasureLineExtend = RulerHeight + RulerHeight = 2 * RulerHeight ✓

        var dashedPen = new Pen(Brushes.Green, 2,
            new DashStyle(new double[] { 6, 4 }, 0));
        //起始线
        context.DrawLine(dashedPen, new Point(0.0, lineTop), new Point(0.0, lineBottom));
        //测量线
        context.DrawLine(dashedPen, new Point(lineX, lineTop), new Point(lineX, lineBottom));

        // Cm label — positioned just above the top of the measure line
        var isSubMeasureEnable = SubMeasureLineEnable && SubMeasurePosition > 0.0 && (MeasurePosition - SubMeasurePosition) > 0.0;

        var mainLineX = lineX;
        var mainPosition = MeasurePosition;
        if (isSubMeasureEnable)
        {
            mainPosition = MeasurePosition - SubMeasurePosition;
            var mainMeasureLineX = mainPosition * 10 * _mm2Pixcel;
            mainLineX = Math.Clamp(mainMeasureLineX, 0, width);

            //绘制sub line 
            context.DrawLine(dashedPen, new Point(mainLineX, lineTop), new Point(mainLineX, height + 3));
            var subFt = new FormattedText($"{SubMeasurePosition:F1} cm", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, boldTf, 10, _arrowBrush);
            var subTextX = mainLineX + ((lineX - mainLineX) / 2.0) - subFt.Width / 2.0;
            var subTextY = height - subFt.Height / 2.0;
            DrawByHV(context, mainLineX + ((lineX - mainLineX) / 2.0), height,
                () => context.DrawText(subFt, new Point(subTextX, subTextY)));

            //绘制总数
            var totalFt = new FormattedText($"{MeasurePosition:F1} cm", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, boldTf, 24, _arrowBrush);
            var totalTextX = lineX / 2 - totalFt.Width / 2.0;
            var totalTextY = lineBottom - totalFt.Height;
            context.DrawText(totalFt, new Point(totalTextX, totalTextY));
        }
        var arrowThickness = 2;
        string label = $"{mainPosition:F1} cm";
        var ft = new FormattedText(label, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, boldTf, 11, _arrowBrush);

        double labelX = mainLineX / 2.0 - ft.Width / 2.0;   // horizontally centred on the line
        double labelY = height - ft.Height / 2.0; // a few pixels above the line top

        // Keep the label upright regardless of the ruler's rotation angle.
        // Rotate around the label's visual centre.
        DrawByHV(context, mainLineX / 2.0, labelY + ft.Height / 2,
            () => context.DrawText(ft, new Point(labelX, labelY)));
        // context.DrawText(ft, new Point(labelX, labelY));

        DrawArrow(context,
            new Point(labelX - _measureTextPadding, height),
            new Point(0.0, height), _arrowBrush,
            new Pen(_arrowBrush, arrowThickness)
            );
        DrawArrow(context,
            new Point(labelX + ft.Width + _measureTextPadding, height),
            new Point(mainLineX, height), _arrowBrush,
            new Pen(_arrowBrush, arrowThickness)
            );
    }

    /// <summary>
    /// Draws a cm number at (x, tickHeight) that stays upright regardless of the ruler angle.
    /// </summary>
    private void DrawCMNumber(DrawingContext context, double x, double tickHeight, double cmValue)
    {
        if (cmValue == 0.0)
        {
            return;
        }
        var ft = new FormattedText(
            cmValue.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            _scaleBrush);

        double xPos = x - ft.Width / 2;

        // Keep the text upright — rotate around the text's visual centre.
        DrawByHV(context, x, tickHeight + ft.Height / 2,
            () => context.DrawText(ft, new Point(xPos, tickHeight)));
    }

    /// <summary>
    /// Applies a counter-rotation of -Angle around (cx, cy) before calling <paramref name="draw"/>.
    /// Because the control itself is rotated by +Angle, the net rotation of the drawn content
    /// is zero, making it appear upright in screen space.
    /// </summary>
    private void DrawUpright(DrawingContext context, double cx, double cy, Action draw)
    {
        DrawByAngle(context, -Angle, cx, cy, draw);
    }

    /// <summary>
    /// 根据当前角度决定是水平还是垂直绘制
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cx"></param>
    /// <param name="cy"></param>
    /// <param name="draw"></param>
    /// <returns>true:水平绘制; false:垂直绘制</returns>
    private bool DrawByHV(DrawingContext context, double cx, double cy, Action draw)
    {
        var absAngle = Math.Abs(Angle);
        var isHorizontal = false;
        if (Angle >= -135 && Angle < -45)
        {
            DrawByAngle(context, 90, cx, cy, draw);
        }
        else if (Angle >= -45 && Angle < 45)
        {
            isHorizontal = true;
            DrawByAngle(context, 0, cx, cy, draw);
        }
        else if (Angle >= 45 && Angle < 135)
        {
            DrawByAngle(context, -90, cx, cy, draw);
        }
        else
        {
            isHorizontal = true;
            DrawByAngle(context, 180, cx, cy, draw);
        }
        return isHorizontal;
    }

    private void DrawByAngle(DrawingContext context, double angle, double cx, double cy, Action draw)
    {
        double rad = angle * Math.PI / 180.0;

        // Matrix that rotates by `rad` around the point (cx, cy):
        //   translate so (cx,cy) → origin, rotate, translate back.
        // In Avalonia's row-vector convention (point × matrix), left-to-right
        // composition means the leftmost matrix is applied first.
        var matrix = Matrix.CreateTranslation(-cx, -cy)
                   * Matrix.CreateRotation(rad)
                   * Matrix.CreateTranslation(cx, cy);

        using (context.PushTransform(matrix))
            draw();
    }

    // ── Pointer handling ────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        Point local = e.GetPosition(this);
        double width = Bounds.Width;
        double height = Bounds.Height;
        Point handleCenter = new Point(width - HandleRadius, HandleRadius + height / 2.0);

        var measureLineX = MeasurePosition * 10 * _mm2Pixcel;

        if (DragAngleEnable && Vector.Distance(local, handleCenter) <= HandleRadius)
        {
            // ── Rotation handle ──
            _isDraggingHandle = true;
            _startAngle = Angle;
            Point rotCenter = new Point(0, height / 2);
            _handleStartAngleRad = Math.Atan2(local.Y - rotCenter.Y, local.X - rotCenter.X);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
        else if (Math.Abs(local.X - measureLineX) <= MeasureLineHitThreshold)
        {
            // ── Measure line drag ──
            // `local` is already in the ruler's own coordinate system, so local.X
            // directly represents position along the ruler axis — correct for any angle.
            _isDraggingMeasureLine = true;
            _measureLineDragStartLocalX = local.X;
            _measureLineStartX = measureLineX;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
        else if (Parent is Canvas canvas)
        {
            // ── Body drag ──
            _isDragging = true;
            _dragStartCanvasPos = e.GetPosition(canvas);
            _dragStartLeft = Canvas.GetLeft(this);
            _dragStartTop = Canvas.GetTop(this);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDraggingHandle)
        {
            // Compute new angle from current mouse position relative to rotation centre
            Point local = e.GetPosition(this);
            Point rotCenter = new Point(0, Bounds.Height / 2);
            double currentRad = Math.Atan2(local.Y - rotCenter.Y, local.X - rotCenter.X);
            double angleDelta = (currentRad - _handleStartAngleRad) * 180.0 / Math.PI;
            Angle = _startAngle + angleDelta;
            e.Handled = true;
        }
        else if (_isDraggingMeasureLine)
        {
            // `GetPosition(this)` returns local ruler coordinates, so X is along the ruler axis.
            Point local = e.GetPosition(this);
            double deltaX = local.X - _measureLineDragStartLocalX;
            var measureLineX = Math.Clamp(_measureLineStartX + deltaX, 0, Bounds.Width);
            MeasurePosition = measureLineX / 10 / _mm2Pixcel;
            InvalidateVisual();
            e.Handled = true;
        }
        else if (_isDragging && Parent is Canvas canvas)
        {
            Point current = e.GetPosition(canvas);
            double deltaX = current.X - _dragStartCanvasPos.X;
            double deltaY = current.Y - _dragStartCanvasPos.Y;

            double newLeft = _dragStartLeft + deltaX;
            double newTop = _dragStartTop + deltaY;

            // 计算期望位置的外接矩形
            Rect bbox = GetBoundingBox(newLeft, newTop);

            // 根据 Canvas 边界修正位置
            if (bbox.Left < 0)
                newLeft += -bbox.Left;
            else if (bbox.Right > canvas.Bounds.Width)
                newLeft -= (bbox.Right - canvas.Bounds.Width);

            if (bbox.Top < 0)
                newTop += -bbox.Top;
            else if (bbox.Bottom > canvas.Bounds.Height)
                newTop -= (bbox.Bottom - canvas.Bounds.Height);

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
        else if (_isDraggingMeasureLine)
        {
            _isDraggingMeasureLine = false;
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

    // 计算控件旋转后的外接矩形（相对于 Canvas 坐标系）
    private Rect GetBoundingBox(double left, double top)
    {
        double width = Bounds.Width;
        double height = Bounds.Height;
        double angleRad = Angle * Math.PI / 180.0;
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);

        // 旋转中心在 Canvas 中的坐标 (RenderTransformOrigin = (0, 0.5))
        double centerX = left;
        double centerY = top + height / 2.0;

        // 四个角点相对于旋转中心的偏移量
        (double dx, double dy)[] offsets = new[]
        {
        (0, -height / 2),
        (width, -height / 2),
        (width, height / 2),
        (0, height / 2)
    };

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        foreach (var (dx, dy) in offsets)
        {
            // 旋转偏移
            double rx = dx * cos - dy * sin;
            double ry = dx * sin + dy * cos;
            double x = centerX + rx;
            double y = centerY + ry;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}