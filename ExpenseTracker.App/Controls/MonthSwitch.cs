namespace ExpenseTracker.App.Controls;

using Microsoft.Maui.Controls.Shapes;

/// <summary>
/// The month control of a page header: arrows to step, and a tap on the month opens a small
/// calendar to pick any month of any year.
/// </summary>
public sealed class MonthSwitch : Border
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(MonthSwitch), "",
        propertyChanged: (b, _, v) => ((MonthSwitch)b)._title.Text = (string)v);

    /// <summary>The month that is shown now (any day of it).</summary>
    public static readonly BindableProperty MonthProperty = BindableProperty.Create(
        nameof(Month), typeof(DateTime), typeof(MonthSwitch), DateTime.Today);

    public static readonly BindableProperty CanGoPreviousProperty = BindableProperty.Create(
        nameof(CanGoPrevious), typeof(bool), typeof(MonthSwitch), true,
        propertyChanged: (b, _, v) => ((MonthSwitch)b)._previous.IsEnabled = (bool)v);

    public static readonly BindableProperty CanGoNextProperty = BindableProperty.Create(
        nameof(CanGoNext), typeof(bool), typeof(MonthSwitch), true,
        propertyChanged: (b, _, v) => ((MonthSwitch)b)._next.IsEnabled = (bool)v);

    private readonly IconButton _previous = new() { Icon = Icons.ChevronLeft };
    private readonly IconButton _next = new() { Icon = Icons.ChevronRight };
    private readonly Label _title = new()
    {
        FontFamily = "NunitoBold",
        WidthRequest = 140,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalOptions = LayoutOptions.Center
    };

    public MonthSwitch()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 20 };
        Padding = new Thickness(4, 0);
        HeightRequest = 40;
        VerticalOptions = LayoutOptions.Center;
        this.SetDynamicResource(BackgroundColorProperty, "Surface2");

        _previous.Clicked += (_, e) => PreviousClicked?.Invoke(this, e);
        _next.Clicked += (_, e) => NextClicked?.Invoke(this, e);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OpenPicker();
        _title.GestureRecognizers.Add(tap);

        Content = new HorizontalStackLayout { Spacing = 0, Children = { _previous, _title, _next } };
    }

    public event EventHandler? PreviousClicked;

    public event EventHandler? NextClicked;

    /// <summary>The person picked a month in the calendar; the value is the first day of it.</summary>
    public event EventHandler<DateTime>? MonthPicked;

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public DateTime Month
    {
        get => (DateTime)GetValue(MonthProperty);
        set => SetValue(MonthProperty, value);
    }

    public bool CanGoPrevious
    {
        get => (bool)GetValue(CanGoPreviousProperty);
        set => SetValue(CanGoPreviousProperty, value);
    }

    public bool CanGoNext
    {
        get => (bool)GetValue(CanGoNextProperty);
        set => SetValue(CanGoNextProperty, value);
    }

    private void OpenPicker()
    {
#if WINDOWS
        if (Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement anchor) return;

        Platforms.Windows.PopupMenu.ShowMonths(
            anchor,
            Month,
            picked => MonthPicked?.Invoke(this, new DateTime(picked.Year, picked.Month, 1)));
#endif
    }
}
