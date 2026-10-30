namespace ExpenseTracker.App.Controls;

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

/// <summary>A rounded button with an optional icon. The primary one is filled with the accent.</summary>
public sealed class PillButton : Border
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(PillButton), "",
        propertyChanged: (b, _, v) =>
        {
            var button = (PillButton)b;
            var text = (string?)v ?? "";
            button._label.Text = text;
            button._label.IsVisible = text.Length > 0;
        });

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(PillButton), "",
        propertyChanged: (b, _, v) =>
        {
            var button = (PillButton)b;
            button._icon.Data = (string)v;
            button._icon.IsVisible = ((string)v).Length > 0;
        });

    public static readonly BindableProperty IsPrimaryProperty = BindableProperty.Create(
        nameof(IsPrimary), typeof(bool), typeof(PillButton), false,
        propertyChanged: (b, _, _) => ((PillButton)b).UpdateLook());

    /// <summary>A smaller button (32 high) for the actions of a card.</summary>
    public static readonly BindableProperty IsCompactProperty = BindableProperty.Create(
        nameof(IsCompact), typeof(bool), typeof(PillButton), false,
        propertyChanged: (b, _, _) => ((PillButton)b).UpdateLook());

    /// <summary>Name of a color resource for the text and icon, for example Danger. Empty: the usual colors.</summary>
    public static readonly BindableProperty ToneKeyProperty = BindableProperty.Create(
        nameof(ToneKey), typeof(string), typeof(PillButton), "",
        propertyChanged: (b, _, _) => ((PillButton)b).UpdateLook());

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(PillButton), null);

    private readonly IconView _icon = new() { IsVisible = false, WidthRequest = 16, HeightRequest = 16 };
    private readonly Label _label = new() { VerticalOptions = LayoutOptions.Center, FontFamily = "ManropeBold", IsVisible = false };
    private bool _hover;

    public PillButton()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 20 };
        HeightRequest = 40;
        Padding = new Thickness(16, 0);
        VerticalOptions = LayoutOptions.Center;

        Content = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { _icon, _label }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, e) =>
        {
            if (!IsEnabled) return;

            Clicked?.Invoke(this, e);
            if (Command?.CanExecute(null) == true) Command.Execute(null);
        };
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

    public bool IsPrimary
    {
        get => (bool)GetValue(IsPrimaryProperty);
        set => SetValue(IsPrimaryProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public string ToneKey
    {
        get => (string)GetValue(ToneKeyProperty);
        set => SetValue(ToneKeyProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.5;
    }

    private void UpdateLook()
    {
        var fill = IsPrimary ? "Accent" : "Surface2";
        var ink = ToneKey.Length > 0 ? ToneKey : IsPrimary ? "OnAccent" : "Ink";

        HeightRequest = IsCompact ? 32 : 40;
        Padding = IsCompact ? new Thickness(12, 0) : new Thickness(16, 0);
        _label.FontSize = IsCompact ? 13 : 14;

        this.SetDynamicResource(BackgroundColorProperty, fill);
        _label.SetDynamicResource(Label.TextColorProperty, ink);
        _icon.SetDynamicResource(IconView.ColorProperty, ink);

        // Hover is a light change of the fill, so it works in every theme.
        Opacity = !IsEnabled ? 0.5 : _hover ? 0.88 : 1;
    }
}
