namespace ExpenseTracker.App.Platforms.Windows;

using ExpenseTracker.App.Services;
using Microsoft.Maui.Platform;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// The panels that open under the app's drop-downs and date fields. They are plain WinUI
/// flyouts filled with controls that are colored from the app tokens, so they always match
/// the theme and always open below the field.
/// </summary>
public static class PopupMenu
{
    private static global::Windows.UI.Color Token(string key)
    {
        var color = Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out var value) == true
                    && value is Microsoft.Maui.Graphics.Color c
            ? c
            : Microsoft.Maui.Graphics.Colors.Gray;

        return color.ToWindowsColor();
    }

    private static SolidColorBrush Brush(string key) => new(Token(key));

    private static Style PresenterStyle(double minWidth, double padding)
    {
        var style = new Style(typeof(FlyoutPresenter));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("Surface")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("Border")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(16)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(padding)));
        style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, minWidth));
        style.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden));
        return style;
    }

    private static ElementTheme Theme => ThemeService.IsDark ? ElementTheme.Dark : ElementTheme.Light;

    /// <summary>Lists the texts under the anchor; calls back with the index that was chosen.</summary>
    public static void ShowList(FrameworkElement anchor, IReadOnlyList<string> texts, int selected, Action<int> chosen)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.Bottom,
            FlyoutPresenterStyle = PresenterStyle(anchor.ActualWidth, 6)
        };

        var panel = new StackPanel { Spacing = 2, RequestedTheme = Theme };

        var hover = Brush("Surface2");
        var soft = Brush("AccentSoft");
        var none = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        for (var i = 0; i < texts.Count; i++)
        {
            var index = i;
            var isSelected = i == selected;
            var fill = isSelected ? soft : none;

            var button = new Microsoft.UI.Xaml.Controls.Button
            {
                Content = texts[i],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(14, 9, 14, 9),
                MinHeight = 36,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(0),
                Background = fill,
                Foreground = Brush("Ink"),
                FontFamily = new FontFamily("Manrope"),
                FontSize = 14,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Medium
            };

            button.Resources["ButtonBackground"] = fill;
            button.Resources["ButtonBackgroundPointerOver"] = isSelected ? soft : hover;
            button.Resources["ButtonBackgroundPressed"] = isSelected ? soft : hover;
            button.Resources["ButtonForeground"] = Brush("Ink");
            button.Resources["ButtonForegroundPointerOver"] = Brush("Ink");
            button.Resources["ButtonForegroundPressed"] = Brush("Ink");
            button.Resources["ButtonBorderBrush"] = none;
            button.Resources["ButtonBorderBrushPointerOver"] = none;
            button.Resources["ButtonBorderBrushPressed"] = none;

            button.Click += (_, _) =>
            {
                flyout.Hide();
                chosen(index);
            };

            panel.Children.Add(button);
        }

        flyout.Content = new ScrollViewer
        {
            Content = panel,
            MaxHeight = 320,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
        };

        flyout.ShowAt(anchor);
    }

    /// <summary>A month calendar under the anchor; calls back with the chosen day.</summary>
    public static void ShowCalendar(
        FrameworkElement anchor, DateTime current, DateTime? minimum, DateTime? maximum, Action<DateTime> chosen)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.Bottom,
            FlyoutPresenterStyle = PresenterStyle(0, 8)
        };

        var accent = Brush("Accent");
        var ink = Brush("Ink");
        var muted = Brush("InkMuted");
        var onAccent = Brush("OnAccent");
        var none = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        var calendar = new CalendarView
        {
            SelectionMode = CalendarViewSelectionMode.Single,
            RequestedTheme = Theme,
            Language = Loc.Code,
            Background = none,
            BorderThickness = new Thickness(0),
            Foreground = ink,
            CalendarItemForeground = ink,
            CalendarItemBackground = none,
            CalendarItemBorderBrush = none,
            TodayForeground = onAccent,
            SelectedForeground = ink,
            SelectedBorderBrush = accent,
            SelectedHoverBorderBrush = accent,
            SelectedPressedBorderBrush = accent,
            HoverBorderBrush = muted,
            PressedBorderBrush = accent,
            OutOfScopeForeground = muted,
            OutOfScopeBackground = none,
            DayItemFontFamily = new FontFamily("Manrope"),
            MonthYearItemFontFamily = new FontFamily("Manrope"),
            FirstOfMonthLabelFontFamily = new FontFamily("Manrope")
        };

        // The fill of "today" and the ring of the chosen day come from the accent brushes.
        foreach (var key in new[] { "AccentFillColorDefaultBrush", "AccentFillColorSecondaryBrush", "AccentFillColorTertiaryBrush" })
            calendar.Resources[key] = accent;

        foreach (var key in new[] { "SystemControlHighlightAccentBrush", "SystemControlBackgroundAccentBrush", "CalendarViewTodayBackground", "CalendarViewSelectedBackground" })
            calendar.Resources[key] = accent;

        foreach (var key in new[] { "SystemAccentColor", "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3", "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3" })
            calendar.Resources[key] = Token("Accent");

        calendar.Resources["TextOnAccentFillColorPrimaryBrush"] = onAccent;
        calendar.Resources["TextOnAccentFillColorSelectedTextBrush"] = onAccent;
        calendar.Resources["CalendarViewSelectedBorderBrush"] = accent;
        calendar.Resources["CalendarViewTodayForeground"] = onAccent;

        if (minimum is { } min) calendar.MinDate = new DateTimeOffset(min);
        if (maximum is { } max) calendar.MaxDate = new DateTimeOffset(max);

        calendar.SetDisplayDate(new DateTimeOffset(current));
        calendar.SelectedDates.Add(new DateTimeOffset(current));

        calendar.SelectedDatesChanged += (_, args) =>
        {
            if (args.AddedDates.Count == 0) return;

            flyout.Hide();
            chosen(args.AddedDates[0].DateTime.Date);
        };

        flyout.Content = calendar;
        flyout.ShowAt(anchor);
    }
}
