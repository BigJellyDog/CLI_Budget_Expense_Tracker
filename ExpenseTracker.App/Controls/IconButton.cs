namespace ExpenseTracker.App.Controls;

using Microsoft.Maui.Controls.Shapes;

/// <summary>A round button with only an icon, for actions like edit and delete.</summary>
public sealed class IconButton : Border
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(IconButton), "",
        propertyChanged: (b, _, v) => ((IconButton)b)._icon.Data = (string)v);

    public static readonly BindableProperty IconColorKeyProperty = BindableProperty.Create(
        nameof(IconColorKey), typeof(string), typeof(IconButton), "InkMuted",
        propertyChanged: (b, _, _) => ((IconButton)b).UpdateLook());

    private readonly IconView _icon = new() { WidthRequest = 16, HeightRequest = 16 };
    private bool _hover;

    public IconButton()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 18 };
        WidthRequest = 36;
        HeightRequest = 36;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;

        _icon.HorizontalOptions = LayoutOptions.Center;
        Content = _icon;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, e) => Clicked?.Invoke(this, e);
        GestureRecognizers.Add(tap);

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => { _hover = true; UpdateLook(); };
        pointer.PointerExited += (_, _) => { _hover = false; UpdateLook(); };
        GestureRecognizers.Add(pointer);

        UpdateLook();
    }

    public event EventHandler? Clicked;

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Name of the color resource for the icon, for example InkMuted or Danger.</summary>
    public string IconColorKey
    {
        get => (string)GetValue(IconColorKeyProperty);
        set => SetValue(IconColorKeyProperty, value);
    }

    private void UpdateLook()
    {
        if (_hover)
            this.SetDynamicResource(BackgroundColorProperty, "Surface2");
        else
            BackgroundColor = Colors.Transparent;

        _icon.SetDynamicResource(IconView.ColorProperty, IconColorKey);
    }
}
