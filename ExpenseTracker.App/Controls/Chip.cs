namespace ExpenseTracker.App.Controls;

using Microsoft.Maui.Controls.Shapes;

/// <summary>A small rounded status label: "Over the limit", "in 3 days". Tone is a color token name.</summary>
public sealed class Chip : Border
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(Chip), "",
        propertyChanged: (b, _, v) => ((Chip)b)._label.Text = (string)v);

    /// <summary>Danger, Success, Warning, Accent, or InkMuted (the default, a quiet chip).</summary>
    public static readonly BindableProperty ToneKeyProperty = BindableProperty.Create(
        nameof(ToneKey), typeof(string), typeof(Chip), "InkMuted",
        propertyChanged: (b, _, v) => ((Chip)b).SetDynamicResource(ToneProperty, (string)v));

    /// <summary>Path data of a small icon before the text, or empty.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(Chip), "",
        propertyChanged: (b, _, v) =>
        {
            var chip = (Chip)b;
            chip._icon.Data = (string)v;
            chip._icon.IsVisible = ((string)v).Length > 0;
        });

    // Follows the theme: it is bound to the color resource named by ToneKey.
    private static readonly BindableProperty ToneProperty = BindableProperty.Create(
        "Tone", typeof(Color), typeof(Chip), Colors.Gray,
        propertyChanged: (b, _, _) => ((Chip)b).UpdateLook());

    private readonly Label _label = new()
    {
        FontSize = 12,
        FontFamily = "NunitoBold",
        VerticalOptions = LayoutOptions.Center
    };

    private readonly IconView _icon = new() { WidthRequest = 12, HeightRequest = 12, IsVisible = false };

    public Chip()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 12 };
        HeightRequest = 24;
        Padding = new Thickness(10, 0);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Content = new HorizontalStackLayout { Spacing = 5, Children = { _icon, _label } };

        SetDynamicResource(ToneProperty, "InkMuted");
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string ToneKey
    {
        get => (string)GetValue(ToneKeyProperty);
        set => SetValue(ToneKeyProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    private void UpdateLook()
    {
        var tone = (Color)GetValue(ToneProperty);

        _label.TextColor = tone;
        _icon.Color = tone;
        BackgroundColor = tone.WithAlpha(ToneKey == "InkMuted" ? 0.16f : 0.14f);
    }
}
