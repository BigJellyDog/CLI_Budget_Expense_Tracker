namespace ExpenseTracker.App.Controls;

using Microsoft.Maui.Controls.Shapes;

/// <summary>One entry of the left menu: icon and text on a rounded pill.</summary>
public sealed class NavItem : Border
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NavItem), "",
        propertyChanged: (b, _, v) => ((NavItem)b)._label.Text = (string)v);

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(NavItem), "",
        propertyChanged: (b, _, v) => ((NavItem)b)._icon.Data = (string)v);

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(NavItem), false,
        propertyChanged: (b, _, _) => ((NavItem)b).UpdateLook());

    private readonly IconView _icon = new();
    private readonly Label _label = new() { VerticalOptions = LayoutOptions.Center };
    private bool _hover;

    public NavItem()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 20 };
        HeightRequest = 40;
        Padding = new Thickness(14, 0);

        _label.FontFamily = "NunitoSemiBold";

        Content = new HorizontalStackLayout
        {
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
            Children = { _icon, _label }
        };

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

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private void UpdateLook()
    {
        if (IsActive)
            this.SetDynamicResource(BackgroundColorProperty, "AccentSoft");
        else if (_hover)
            this.SetDynamicResource(BackgroundColorProperty, "Surface2");
        else
            BackgroundColor = Colors.Transparent;

        _label.SetDynamicResource(Label.TextColorProperty, IsActive || _hover ? "Ink" : "InkMuted");
        _icon.SetDynamicResource(IconView.ColorProperty, IsActive ? "Accent" : "InkMuted");
    }
}
