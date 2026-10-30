namespace ExpenseTracker.App.Controls;

using Models;

/// <summary>A thin bar split by category: how the month's spending is distributed.</summary>
public sealed class StackBar : GraphicsView, IDrawable
{
    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(
        nameof(Segments), typeof(IReadOnlyList<BarSegment>), typeof(StackBar), null,
        propertyChanged: (b, _, _) => ((StackBar)b).Invalidate());

    public StackBar()
    {
        Drawable = this;
        HeightRequest = 8;
        InputTransparent = true;
    }

    public IReadOnlyList<BarSegment>? Segments
    {
        get => (IReadOnlyList<BarSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float gap = 2f;

        var resources = Application.Current?.Resources;
        var track = resources?.TryGetValue("Track", out var t) == true && t is Color c ? c : Colors.Gray;

        var segments = Segments?.Where(x => x.Value > 0).ToArray() ?? [];
        var total = segments.Sum(x => x.Value);

        if (total <= 0)
        {
            canvas.FillColor = track;
            canvas.FillRoundedRectangle(dirtyRect, dirtyRect.Height / 2);
            return;
        }

        var usable = dirtyRect.Width - gap * (segments.Length - 1);
        var x = dirtyRect.X;

        foreach (var segment in segments)
        {
            var width = (float)(segment.Value / total) * usable;

            canvas.FillColor = CategoryPalette.ColorFor(segment.CategoryName);
            canvas.FillRoundedRectangle(x, dirtyRect.Y, width, dirtyRect.Height, dirtyRect.Height / 2);

            x += width + gap;
        }
    }
}
