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

    // 测量线位置(厘米)
    public double MeasurePosition
    {
        get => GetValue(MeasurePositionProperty);
        set => SetValue(MeasurePositionProperty, value);
    }

    // Constants
    private const double RulerHeight = 50;
    private const double HandleRadius = 8;
    // The measure line extends RulerHeight/2 above and below the ruler body,
    // giving a total visible length of RulerHeight (body) + RulerHeight/2 + RulerHeight/2 = 2 * RulerHeight.
    private const double MeasureLineExtend = RulerHeight / 2;
    private const double MeasureLineHitThreshold = 12.0;

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

    private double _mm2Pixcel;

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
                tickH = height * 0.6;
                DrawCMNumber(context, x, tickH, mm / 10.0);
            }
            else if (mm % 5 == 0)
                tickH = height * 0.4;
            else
                tickH = height * 0.2;

            context.DrawLine(new Pen(Brushes.Black, 1), new Point(x, 0), new Point(x, tickH));
        }

        // Measure line (drawn before handle so handle renders on top)
        DrawMeasureLine(context, width, height);

        // Rotation handle (red circle, top-right corner)
        Point handleCenter = new Point(width - HandleRadius, HandleRadius);
        context.DrawEllipse(Brushes.Red, null, handleCenter, HandleRadius, HandleRadius);
    }

    private void DrawMeasuredArea(DrawingContext context, double width, double height)
    {
        var measureLineX = MeasurePosition * 10 * _mm2Pixcel;
        double lineX = Math.Clamp(measureLineX, 0, width);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1A22C55E")), new Rect(0, 0, lineX, height));
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
        context.DrawLine(dashedPen, new Point(lineX, lineTop), new Point(lineX, lineBottom));

        // Cm label — positioned just above the top of the measure line
        double cmValue = lineX / width * LengthInCm;
        string label = $"{cmValue:F1} cm";
        var ft = new FormattedText(label, CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, Typeface.Default, 11, Brushes.Green);

        double labelX = lineX - ft.Width / 2;   // horizontally centred on the line
        double labelY = lineTop - ft.Height - 3; // a few pixels above the line top

        // Keep the label upright regardless of the ruler's rotation angle.
        // Rotate around the label's visual centre.
        DrawUpright(context, lineX, labelY + ft.Height / 2,
            () => context.DrawText(ft, new Point(labelX, labelY)));
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
            20,
            new SolidColorBrush(Colors.Black));

        double xPos = x - ft.Width / 2;

        // Keep the text upright — rotate around the text's visual centre.
        DrawUpright(context, x, tickHeight + ft.Height / 2,
            () => context.DrawText(ft, new Point(xPos, tickHeight)));
    }

    /// <summary>
    /// Applies a counter-rotation of -Angle around (cx, cy) before calling <paramref name="draw"/>.
    /// Because the control itself is rotated by +Angle, the net rotation of the drawn content
    /// is zero, making it appear upright in screen space.
    /// </summary>
    private void DrawUpright(DrawingContext context, double cx, double cy, Action draw)
    {
        double rad = -Angle * Math.PI / 180.0;

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
        Point handleCenter = new Point(width - HandleRadius, HandleRadius);

        var measureLineX = MeasurePosition * 10 * _mm2Pixcel;

        if (Vector.Distance(local, handleCenter) <= HandleRadius)
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
}