namespace ExpenseTracker.App.Controls;

public sealed record DonutSegment(string Name, double Value, Color Color);

/// <summary>
/// A ring split by category: segments in the category colors with a small gap, the total goes in the middle
/// as labels of the page, so it uses the app fonts. The segment under the pointer grows; a tap on a segment raises SegmentTapped.
/// </summary>
public sealed class DonutChart : GraphicsView, IDrawable
{
    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(
        nameof(Segments), typeof(IReadOnlyList<DonutSegment>), typeof(DonutChart), null,
        propertyChanged: (b, _, _) => ((DonutChart)b).Invalidate());

    /// <summary>Index of the highlighted segment, or -1. The legend sets it too.</summary>
    public static readonly BindableProperty HighlightIndexProperty = BindableProperty.Create(
        nameof(HighlightIndex), typeof(int), typeof(DonutChart), -1,
        propertyChanged: (b, _, _) => ((DonutChart)b).Invalidate());

    private const float Thickness = 24f;
    private const float GapDegrees = 1.6f;

    private float _cx, _cy, _radius;

    public DonutChart()
    {
        Drawable = this;
        WidthRequest = 196;
        HeightRequest = 196;
        HorizontalOptions = LayoutOptions.Center;

        var pointer = new PointerGestureRecognizer();
        pointer.PointerMoved += (_, e) =>
        {
            if (e.GetPosition(this) is { } point)
                HighlightIndex = HitTest(point);
        };
        pointer.PointerExited += (_, _) => HighlightIndex = -1;
        GestureRecognizers.Add(pointer);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, e) =>
        {
            if (e.GetPosition(this) is { } point && HitTest(point) is var index and >= 0)
                SegmentTapped?.Invoke(this, index);
        };
        GestureRecognizers.Add(tap);
    }

    public event EventHandler<int>? SegmentTapped;

    public IReadOnlyList<DonutSegment>? Segments
    {
        get => (IReadOnlyList<DonutSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public int HighlightIndex
    {
        get => (int)GetValue(HighlightIndexProperty);
        set => SetValue(HighlightIndexProperty, value);
    }

    private static Color Token(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height) - 8f; // room for the grown segment
        _radius = (size - Thickness) / 2f;
        _cx = rect.Center.X;
        _cy = rect.Center.Y;

        var arc = new RectF(_cx - _radius, _cy - _radius, _radius * 2, _radius * 2);

        canvas.StrokeLineCap = LineCap.Butt;
        canvas.StrokeSize = Thickness;
        canvas.StrokeColor = Token("Track", Colors.Gray);
        canvas.DrawEllipse(arc);

        var segments = Segments?.Where(x => x.Value > 0).ToArray() ?? [];
        var total = segments.Sum(x => x.Value);

        if (total > 0)
        {
            var start = 90f; // 90 degrees is the top; the ring runs clockwise

            for (var i = 0; i < segments.Length; i++)
            {
                var sweep = (float)(segments[i].Value / total * 360d);
                var drawn = segments.Length == 1 ? 359.9f : Math.Max(0.5f, sweep - GapDegrees);

                canvas.StrokeColor = segments[i].Color;
                canvas.StrokeSize = i == HighlightIndex ? Thickness + 6 : Thickness;
                canvas.DrawArc(arc, start - GapDegrees / 2, start - GapDegrees / 2 - drawn, true, false);

                start -= sweep;
            }
        }
    }

    /// <summary>The segment under the point, or -1.</summary>
    private int HitTest(Point point)
    {
        var segments = Segments?.Where(x => x.Value > 0).ToArray() ?? [];
        var total = segments.Sum(x => x.Value);
        if (total <= 0) return -1;

        var dx = point.X - _cx;
        var dy = point.Y - _cy;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < _radius - Thickness / 2 - 2 || distance > _radius + Thickness / 2 + 6) return -1;

        var angle = (Math.Atan2(dx, -dy) * 180 / Math.PI + 360) % 360; // from the top, clockwise
        var passed = 0d;

        for (var i = 0; i < segments.Length; i++)
        {
            passed += segments[i].Value / total * 360d;
            if (angle < passed) return i;
        }

        return -1;
    }
}
