namespace ExpenseTracker.App.Controls;

/// <summary>
/// Draws a line icon from SVG path data in the given color. The color is a bindable
/// property, so it follows DynamicResource and changes with the theme.
/// </summary>
public sealed class IconView : GraphicsView, IDrawable
{
    public static readonly BindableProperty DataProperty = BindableProperty.Create(
        nameof(Data), typeof(string), typeof(IconView), "",
        propertyChanged: (b, _, _) => ((IconView)b).Update());

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(IconView), Colors.Gray,
        propertyChanged: (b, _, _) => ((IconView)b).Invalidate());

    private PathF? _path;

    public IconView()
    {
        Drawable = this;
        WidthRequest = 18;
        HeightRequest = 18;
        InputTransparent = true;
        VerticalOptions = LayoutOptions.Center;
    }

    public string Data
    {
        get => (string)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    private void Update()
    {
        _path = string.IsNullOrWhiteSpace(Data) ? null : PathBuilder.Build(Data);
        Invalidate();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_path is null) return;

        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);

        canvas.SaveState();
        canvas.Scale(size / 24f, size / 24f);
        canvas.StrokeColor = Color;
        canvas.StrokeSize = 1.8f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(_path);
        canvas.RestoreState();
    }
}
