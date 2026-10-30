namespace ExpenseTracker.App.Controls;

/// <summary>A thin progress bar: a track and a filled part. The fill color is a token name.</summary>
public sealed class MeterBar : GraphicsView, IDrawable
{
    /// <summary>0 to 1.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(MeterBar), 0d,
        propertyChanged: (b, _, _) => ((MeterBar)b).Invalidate());

    public static readonly BindableProperty FillKeyProperty = BindableProperty.Create(
        nameof(FillKey), typeof(string), typeof(MeterBar), "Accent",
        propertyChanged: (b, _, v) => ((MeterBar)b).SetDynamicResource(FillProperty, (string)v));

    private static readonly BindableProperty FillProperty = BindableProperty.Create(
        "Fill", typeof(Color), typeof(MeterBar), Colors.Gray,
        propertyChanged: (b, _, _) => ((MeterBar)b).Invalidate());

    public MeterBar()
    {
        Drawable = this;
        HeightRequest = 8;
        InputTransparent = true;
        SetDynamicResource(FillProperty, "Accent");
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string FillKey
    {
        get => (string)GetValue(FillKeyProperty);
        set => SetValue(FillKeyProperty, value);
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var track = Application.Current?.Resources.TryGetValue("Track", out var t) == true && t is Color c ? c : Colors.Gray;
        var radius = rect.Height / 2;

        canvas.FillColor = track;
        canvas.FillRoundedRectangle(rect, radius);

        var width = (float)Math.Clamp(Value, 0, 1) * rect.Width;
        if (width <= 0) return;

        canvas.FillColor = (Color)GetValue(FillProperty);
        canvas.FillRoundedRectangle(new RectF(rect.X, rect.Y, Math.Max(width, rect.Height), rect.Height), radius);
    }
}
