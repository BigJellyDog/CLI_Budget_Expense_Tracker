namespace ExpenseTracker.App.Controls;

using Microsoft.Maui.Controls.Shapes;

/// <summary>
/// A field that shows a date and opens a calendar under itself. It replaces the DatePicker,
/// whose native calendar is drawn by the system in its own style.
/// </summary>
public sealed class DateField : Border
{
    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date), typeof(DateTime?), typeof(DateField),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((DateField)b).ShowDate());

    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(
        nameof(MinimumDate), typeof(DateTime?), typeof(DateField));

    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(
        nameof(MaximumDate), typeof(DateTime?), typeof(DateField));

    public static readonly BindableProperty FormatProperty = BindableProperty.Create(
        nameof(Format), typeof(string), typeof(DateField), "dd.MM.yyyy",
        propertyChanged: (b, _, _) => ((DateField)b).ShowDate());

    private readonly Label _label;

    public DateField()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 20 };
        HeightRequest = 40;
        MinimumWidthRequest = 130;
        HorizontalOptions = LayoutOptions.Start;
        Padding = new Thickness(14, 0, 12, 0);
        this.SetDynamicResource(BackgroundColorProperty, "Surface2");

        _label = new Label { VerticalOptions = LayoutOptions.Center };

        var icon = new IconView { Data = Icons.Calendar, WidthRequest = 16, HeightRequest = 16 };
        icon.SetDynamicResource(IconView.ColorProperty, "InkMuted");

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12
        };
        grid.Add(_label, 0, 0);
        grid.Add(icon, 1, 0);
        Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Open();
        GestureRecognizers.Add(tap);

        ShowDate();
    }

    /// <summary>Raised when the person picks a day (not when the value is set from code).</summary>
    public event EventHandler? DateSelected;

    public DateTime? Date
    {
        get => (DateTime?)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public DateTime? MinimumDate
    {
        get => (DateTime?)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public DateTime? MaximumDate
    {
        get => (DateTime?)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsEnabled))
            Opacity = IsEnabled ? 1 : 0.5;
    }

    private void ShowDate()
    {
        if (_label is null) return;

        _label.Text = Date?.ToString(Format) ?? "–";
        _label.SetDynamicResource(Label.TextColorProperty, Date is null ? "InkMuted" : "Ink");
    }

    private void Open()
    {
        if (!IsEnabled) return;

#if WINDOWS
        if (Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement anchor) return;

        Platforms.Windows.PopupMenu.ShowCalendar(
            anchor,
            Date ?? DateTime.Today,
            MinimumDate,
            MaximumDate,
            picked =>
            {
                Date = picked;
                DateSelected?.Invoke(this, EventArgs.Empty);
            });
#endif
    }
}
