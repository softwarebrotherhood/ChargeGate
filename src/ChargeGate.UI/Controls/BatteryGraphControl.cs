using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ChargeGate.Core.Models;

namespace ChargeGate.UI.Controls;

public class BatteryGraphControl : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(
            nameof(Points),
            typeof(IEnumerable<BatteryTelemetryPoint>),
            typeof(BatteryGraphControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GraphModeProperty =
        DependencyProperty.Register(
            nameof(GraphMode),
            typeof(string),
            typeof(BatteryGraphControl),
            new FrameworkPropertyMetadata("Watts", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThresholdPercentageProperty =
        DependencyProperty.Register(
            nameof(ThresholdPercentage),
            typeof(int),
            typeof(BatteryGraphControl),
            new FrameworkPropertyMetadata(80, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<BatteryTelemetryPoint>? Points
    {
        get => (IEnumerable<BatteryTelemetryPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public string GraphMode
    {
        get => (string)GetValue(GraphModeProperty);
        set => SetValue(GraphModeProperty, value);
    }

    public int ThresholdPercentage
    {
        get => (int)GetValue(ThresholdPercentageProperty);
        set => SetValue(ThresholdPercentageProperty, value);
    }

    private Point? _hoverPoint;
    private BatteryTelemetryPoint? _hoverTelemetry;

    public BatteryGraphControl()
    {
        ClipToBounds = true;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        _hoverPoint = e.GetPosition(this);
        InvalidateVisual();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _hoverPoint = null;
        _hoverTelemetry = null;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double width = ActualWidth;
        double height = ActualHeight;

        if (width <= 60 || height <= 40) return;

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var boldTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        // Chart Area Padding
        double marginLeft = 44;
        double marginRight = 16;
        double marginTop = 18;
        double marginBottom = 22;

        double plotW = Math.Max(10, width - marginLeft - marginRight);
        double plotH = Math.Max(10, height - marginTop - marginBottom);

        // Grid brush and text brush
        var textBrush = new SolidColorBrush(Color.FromArgb(160, 160, 160, 160));
        textBrush.Freeze();
        var subtleGridPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)), 1);
        subtleGridPen.Freeze();

        var pointsList = Points?.ToList() ?? new List<BatteryTelemetryPoint>();
        if (pointsList.Count == 0)
        {
            var noDataText = new FormattedText("Waiting for battery telemetry...", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, 11, textBrush, dpi);
            dc.DrawText(noDataText, new Point((width - noDataText.Width) / 2, (height - noDataText.Height) / 2));
            return;
        }

        bool isWattsMode = GraphMode == "Watts";

        if (isWattsMode)
        {
            RenderWattsMode(dc, pointsList, marginLeft, marginTop, plotW, plotH, dpi, typeface, boldTypeface, textBrush, subtleGridPen);
        }
        else
        {
            RenderPercentageMode(dc, pointsList, marginLeft, marginTop, plotW, plotH, dpi, typeface, boldTypeface, textBrush, subtleGridPen);
        }

        // Render Hover Tooltip if cursor is inside plot area
        if (_hoverPoint.HasValue && _hoverTelemetry != null && _hoverPoint.Value.X >= marginLeft && _hoverPoint.Value.X <= marginLeft + plotW)
        {
            RenderHoverTooltip(dc, _hoverPoint.Value, _hoverTelemetry, marginLeft, plotW, plotH, marginTop, dpi, boldTypeface, textBrush);
        }
    }

    private void RenderWattsMode(DrawingContext dc, List<BatteryTelemetryPoint> pts,
        double left, double top, double w, double h, double dpi,
        Typeface tf, Typeface boldTf, Brush textBrush, Pen gridPen)
    {
        // 1. Determine dynamic Y range for Watts
        double maxW = 35.0;
        double minW = -25.0;

        foreach (var p in pts)
        {
            if (p.RateWatts > maxW) maxW = p.RateWatts + 5.0;
            if (p.RateWatts < minW) minW = p.RateWatts - 5.0;
        }

        double range = Math.Max(20.0, maxW - minW);
        double zeroY = top + ((maxW - 0) / range) * h;
        zeroY = Math.Clamp(zeroY, top, top + h);

        // 2. Horizontal Gridlines & Y-Axis Labels
        var zeroPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 16, 185, 129)), 1.5)
        {
            DashStyle = DashStyles.Dash
        };
        zeroPen.Freeze();

        // 0 W Passthrough Line
        dc.DrawLine(zeroPen, new Point(left, zeroY), new Point(left + w, zeroY));
        var zeroText = new FormattedText("0 W", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, textBrush, dpi);
        dc.DrawText(zeroText, new Point(left - zeroText.Width - 6, zeroY - 6));

        // +Top Grid Line
        double topWatts = Math.Round(maxW / 10.0) * 10.0;
        double topGridY = top + ((maxW - topWatts) / range) * h;
        if (topGridY > top && Math.Abs(topGridY - zeroY) > 20)
        {
            dc.DrawLine(gridPen, new Point(left, topGridY), new Point(left + w, topGridY));
            var topText = new FormattedText($"+{topWatts:F0}W", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, textBrush, dpi);
            dc.DrawText(topText, new Point(left - topText.Width - 6, topGridY - 6));
        }

        // -Bottom Grid Line (Drain)
        double botWatts = Math.Round(minW / 10.0) * 10.0;
        double botGridY = top + ((maxW - botWatts) / range) * h;
        if (botGridY < top + h && Math.Abs(botGridY - zeroY) > 20)
        {
            dc.DrawLine(gridPen, new Point(left, botGridY), new Point(left + w, botGridY));
            var botText = new FormattedText($"{botWatts:F0}W", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, textBrush, dpi);
            dc.DrawText(botText, new Point(left - botText.Width - 6, botGridY - 6));
        }

        // 3. Compute screen points
        int count = pts.Count;
        var screenCoords = new Point[count];
        for (int i = 0; i < count; i++)
        {
            double px = count == 1 ? left + w : left + ((double)i / (count - 1)) * w;
            double py = top + ((maxW - pts[i].RateWatts) / range) * h;
            screenCoords[i] = new Point(px, Math.Clamp(py, top, top + h));
        }

        // Track closest point to cursor
        if (_hoverPoint.HasValue)
        {
            int closestIdx = 0;
            double minDistance = double.MaxValue;
            for (int i = 0; i < count; i++)
            {
                double dist = Math.Abs(screenCoords[i].X - _hoverPoint.Value.X);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestIdx = i;
                }
            }
            _hoverTelemetry = pts[closestIdx];
        }

        // 4. Draw Filled Gradient Area under curve
        var fillGeometry = new StreamGeometry();
        using (var ctx = fillGeometry.Open())
        {
            ctx.BeginFigure(new Point(screenCoords[0].X, zeroY), true, true);
            foreach (var p in screenCoords)
            {
                ctx.LineTo(p, true, true);
            }
            ctx.LineTo(new Point(screenCoords[count - 1].X, zeroY), true, true);
        }
        fillGeometry.Freeze();

        var areaBrush = new LinearGradientBrush(
            Color.FromArgb(60, 16, 185, 129), // Emerald
            Color.FromArgb(15, 245, 158, 11), // Amber
            new Point(0, 0),
            new Point(0, 1));
        areaBrush.Freeze();
        dc.DrawGeometry(areaBrush, null, fillGeometry);

        // 5. Draw Curve Line
        var linePen = new Pen(new SolidColorBrush(Color.FromRgb(16, 185, 129)), 2);
        linePen.Freeze();
        for (int i = 0; i < count - 1; i++)
        {
            var p1 = screenCoords[i];
            var p2 = screenCoords[i + 1];
            // If discharging, tint amber/orange
            Color segColor = pts[i + 1].RateWatts < 0 ? Color.FromRgb(245, 158, 11) : Color.FromRgb(16, 185, 129);
            var segPen = new Pen(new SolidColorBrush(segColor), 2);
            segPen.Freeze();
            dc.DrawLine(segPen, p1, p2);
        }

        // 6. Draw Current Live Dot
        var lastPt = screenCoords[count - 1];
        Color dotColor = pts[count - 1].RateWatts < 0 ? Color.FromRgb(245, 158, 11) : Color.FromRgb(16, 185, 129);
        var haloBrush = new SolidColorBrush(Color.FromArgb(60, dotColor.R, dotColor.G, dotColor.B));
        haloBrush.Freeze();
        var dotBrush = new SolidColorBrush(dotColor);
        dotBrush.Freeze();

        dc.DrawEllipse(haloBrush, null, lastPt, 6, 6);
        dc.DrawEllipse(dotBrush, null, lastPt, 3.5, 3.5);

        // 7. X-Axis Time Markers
        DrawTimeMarkers(dc, left, top + h, w, tf, textBrush, dpi);
    }

    private void RenderPercentageMode(DrawingContext dc, List<BatteryTelemetryPoint> pts,
        double left, double top, double w, double h, double dpi,
        Typeface tf, Typeface boldTf, Brush textBrush, Pen gridPen)
    {
        // 1. Gridlines at 100%, 75%, 50%, 25%, 0%
        int[] gridPercentages = { 100, 75, 50, 25 };
        foreach (int p in gridPercentages)
        {
            double gy = top + ((100.0 - p) / 100.0) * h;
            dc.DrawLine(gridPen, new Point(left, gy), new Point(left + w, gy));
            var pText = new FormattedText($"{p}%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, textBrush, dpi);
            dc.DrawText(pText, new Point(left - pText.Width - 6, gy - 6));
        }

        // 2. Active Threshold Dashed Line (e.g. 80%)
        if (ThresholdPercentage > 0 && ThresholdPercentage <= 100)
        {
            double thY = top + ((100.0 - ThresholdPercentage) / 100.0) * h;
            var thPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 16, 185, 129)), 1.5)
            {
                DashStyle = DashStyles.Dash
            };
            thPen.Freeze();
            dc.DrawLine(thPen, new Point(left, thY), new Point(left + w, thY));

            var thText = new FormattedText($"{ThresholdPercentage}% Gate", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, boldTf, 9.5, new SolidColorBrush(Color.FromRgb(16, 185, 129)), dpi);
            dc.DrawText(thText, new Point(left + w - thText.Width - 4, thY - 14));
        }

        // 3. Compute screen points
        int count = pts.Count;
        var screenCoords = new Point[count];
        for (int i = 0; i < count; i++)
        {
            double px = count == 1 ? left + w : left + ((double)i / (count - 1)) * w;
            double py = top + ((100.0 - pts[i].Percentage) / 100.0) * h;
            screenCoords[i] = new Point(px, Math.Clamp(py, top, top + h));
        }

        if (_hoverPoint.HasValue)
        {
            int closestIdx = 0;
            double minDistance = double.MaxValue;
            for (int i = 0; i < count; i++)
            {
                double dist = Math.Abs(screenCoords[i].X - _hoverPoint.Value.X);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestIdx = i;
                }
            }
            _hoverTelemetry = pts[closestIdx];
        }

        // 4. Fill Area under Curve
        double botY = top + h;
        var fillGeometry = new StreamGeometry();
        using (var ctx = fillGeometry.Open())
        {
            ctx.BeginFigure(new Point(screenCoords[0].X, botY), true, true);
            foreach (var p in screenCoords)
            {
                ctx.LineTo(p, true, true);
            }
            ctx.LineTo(new Point(screenCoords[count - 1].X, botY), true, true);
        }
        fillGeometry.Freeze();

        var areaBrush = new LinearGradientBrush(
            Color.FromArgb(70, 16, 185, 129),
            Color.FromArgb(5, 16, 185, 129),
            new Point(0, 0),
            new Point(0, 1));
        areaBrush.Freeze();
        dc.DrawGeometry(areaBrush, null, fillGeometry);

        // 5. Draw Percentage Curve Line
        var linePen = new Pen(new SolidColorBrush(Color.FromRgb(16, 185, 129)), 2);
        linePen.Freeze();
        for (int i = 0; i < count - 1; i++)
        {
            dc.DrawLine(linePen, screenCoords[i], screenCoords[i + 1]);
        }

        // 6. Current Dot
        var lastPt = screenCoords[count - 1];
        var haloBrush = new SolidColorBrush(Color.FromArgb(60, 16, 185, 129));
        haloBrush.Freeze();
        var dotBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        dotBrush.Freeze();

        dc.DrawEllipse(haloBrush, null, lastPt, 6, 6);
        dc.DrawEllipse(dotBrush, null, lastPt, 3.5, 3.5);

        // 7. Time markers
        DrawTimeMarkers(dc, left, top + h, w, tf, textBrush, dpi);
    }

    private void DrawTimeMarkers(DrawingContext dc, double left, double y, double w,
        Typeface tf, Brush textBrush, double dpi)
    {
        var startText = new FormattedText("10m ago", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9, textBrush, dpi);
        dc.DrawText(startText, new Point(left, y + 4));

        var midText = new FormattedText("5m ago", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9, textBrush, dpi);
        dc.DrawText(midText, new Point(left + (w - midText.Width) / 2, y + 4));

        var nowText = new FormattedText("Now", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9, textBrush, dpi);
        dc.DrawText(nowText, new Point(left + w - nowText.Width, y + 4));
    }

    private void RenderHoverTooltip(DrawingContext dc, Point mousePos, BatteryTelemetryPoint p,
        double left, double plotW, double plotH, double top, double dpi, Typeface boldTf, Brush textBrush)
    {
        // Draw vertical crosshair line
        var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 1)
        {
            DashStyle = DashStyles.Dash
        };
        crossPen.Freeze();
        dc.DrawLine(crossPen, new Point(mousePos.X, top), new Point(mousePos.X, top + plotH));

        // Format tooltip label
        string rateStr = p.RateWatts > 0 ? $"+{p.RateWatts:F1}W" : $"{p.RateWatts:F1}W";
        string timeStr = p.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        string tip = $"{timeStr} • {p.Percentage}% • {rateStr}";

        var tipFormatted = new FormattedText(tip, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            boldTf, 10, Brushes.White, dpi);

        double tipW = tipFormatted.Width + 16;
        double tipH = tipFormatted.Height + 8;
        double tipX = Math.Clamp(mousePos.X - tipW / 2, left, left + plotW - tipW);
        double tipY = top + 4;

        var tipBg = new SolidColorBrush(Color.FromArgb(220, 24, 24, 27));
        tipBg.Freeze();
        var tipBorder = new Pen(new SolidColorBrush(Color.FromArgb(120, 16, 185, 129)), 1);
        tipBorder.Freeze();

        dc.DrawRoundedRectangle(tipBg, tipBorder, new Rect(tipX, tipY, tipW, tipH), 6, 6);
        dc.DrawText(tipFormatted, new Point(tipX + 8, tipY + 4));
    }
}
