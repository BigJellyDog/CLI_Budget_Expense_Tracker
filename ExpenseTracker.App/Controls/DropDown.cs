namespace ExpenseTracker.App.Controls;

using System.Collections;
using ExpenseTracker.App.Services;
using Microsoft.Maui.Controls.Shapes;

/// <summary>
/// A field that shows the chosen item and opens a list of items under itself. It replaces the
/// Picker, whose native list is drawn by the system in its own style.
/// </summary>
public sealed class DropDown : Border
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(DropDown),
        propertyChanged: (b, _, _) => ((DropDown)b).ShowSelected());

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(object), typeof(DropDown),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((DropDown)b).OnSelectedChanged());

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(DropDown), "",
        propertyChanged: (b, _, _) => ((DropDown)b).ShowSelected());

    /// <summary>Name of the property that holds the text of an item. Empty: the item itself.</summary>
    public static readonly BindableProperty DisplayMemberProperty = BindableProperty.Create(
        nameof(DisplayMember), typeof(string), typeof(DropDown), "",
        propertyChanged: (b, _, _) => ((DropDown)b).ShowSelected());

    /// <summary>Shows the text of an item in the chosen language.</summary>
    public static readonly BindableProperty TranslateProperty = BindableProperty.Create(
        nameof(Translate), typeof(bool), typeof(DropDown), false,
        propertyChanged: (b, _, _) => ((DropDown)b).ShowSelected());

    private readonly Label _label;

    public DropDown()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 20 };
        HeightRequest = 40;
        Padding = new Thickness(14, 0, 12, 0);
        this.SetDynamicResource(BackgroundColorProperty, "Surface2");

        _label = new Label
        {
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var chevron = new IconView { Data = Icons.ChevronDown, WidthRequest = 16, HeightRequest = 16 };
        chevron.SetDynamicResource(IconView.ColorProperty, "InkMuted");

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8
        };
        grid.Add(_label, 0, 0);
        grid.Add(chevron, 1, 0);
        Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Open();
        GestureRecognizers.Add(tap);

        ShowSelected();
    }

    public event EventHandler? SelectedIndexChanged;

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string DisplayMember
    {
        get => (string)GetValue(DisplayMemberProperty);
        set => SetValue(DisplayMemberProperty, value);
    }

    public bool Translate
    {
        get => (bool)GetValue(TranslateProperty);
        set => SetValue(TranslateProperty, value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsEnabled))
            Opacity = IsEnabled ? 1 : 0.5;
    }

    private string Display(object? item)
    {
        if (item is null) return "";

        object? value = item;

        if (!string.IsNullOrEmpty(DisplayMember))
            value = item.GetType().GetProperty(DisplayMember)?.GetValue(item);

        var text = value?.ToString() ?? "";
        return Translate ? Loc.T(text) : text;
    }

    private void OnSelectedChanged()
    {
        ShowSelected();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowSelected()
    {
        if (_label is null) return;

        var hasItem = SelectedItem is not null;
        _label.Text = hasItem ? Display(SelectedItem) : Title;
        _label.SetDynamicResource(Label.TextColorProperty, hasItem ? "Ink" : "InkMuted");
    }

    private void Open()
    {
        if (!IsEnabled || ItemsSource is null) return;

#if WINDOWS
        if (Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement anchor) return;

        var items = ItemsSource.Cast<object>().ToList();
        if (items.Count == 0) return;

        Platforms.Windows.PopupMenu.ShowList(
            anchor,
            items.Select(Display).ToList(),
            items.IndexOf(SelectedItem!),
            index => SelectedItem = items[index]);
#endif
    }
}
