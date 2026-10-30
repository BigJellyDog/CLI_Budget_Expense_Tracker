namespace ExpenseTracker.App.Controls;

/// <summary>A bar for this month with a thin vertical mark for the month before; both on one scale (0 to 1).</summary>
public sealed class ComparisonBar : GraphicsView, IDrawable
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(ComparisonBar), 0d,
        propertyChanged: (b, _, _) => ((ComparisonBar)b).Invalidate());

    public static readonly BindableProperty MarkerProperty = BindableProperty.Create(
        nameof(Marker), typeof(double), typeof(ComparisonBar), 0d,
        propertyChanged: (b, _, _) => ((ComparisonBar)b).Invalidate());

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(ComparisonBar), Colors.Gray,
        propertyChanged: (b, _, _) => ((ComparisonBar)b).Invalidate());

    public ComparisonBar()
    {
        Drawable = this;
        HeightRequest = 14;
        InputTransparent = true;
        VerticalOptions = LayoutOptions.Center;
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Marker
    {
        get => (double)GetValue(MarkerProperty);
        set => SetValue(MarkerProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    private static Color Token(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color c ? c : Colors.Gray;

    public void Draw(ICanvas canvas, RectF rect)
    {
        const float barHeight = 8f;
        var top = rect.Y + (rect.Height - barHeight) / 2;
        var track = new RectF(rect.X, top, rect.Width, barHeight);

        canvas.FillColor = Token("Track");
        canvas.FillRoundedRectangle(track, barHeight / 2);

        var width = (float)Math.Clamp(Value, 0, 1) * rect.Width;
        if (width > 0)
        {
            canvas.FillColor = FillColor;
            canvas.FillRoundedRectangle(new RectF(rect.X, top, Math.Max(width, barHeight), barHeight), barHeight / 2);
        }

        if (Marker <= 0) return;

        var x = rect.X + (float)Math.Clamp(Marker, 0, 1) * rect.Width;
        canvas.FillColor = Token("InkMuted");
        canvas.FillRoundedRectangle(new RectF(Math.Min(x, rect.Right - 2), top - 3, 2, barHeight + 6), 1);
    }
}
