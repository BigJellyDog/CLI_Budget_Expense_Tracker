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
    public static void ShowList(
        FrameworkElement anchor, IReadOnlyList<string> texts, int selected, Action<int> chosen, double minWidth = 0)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.Bottom,
            FlyoutPresenterStyle = PresenterStyle(Math.Max(anchor.ActualWidth, minWidth), 6)
        };

        var panel = new StackPanel { Spacing = 2, RequestedTheme = Theme };

        var soft = Brush("AccentSoft");
        var none = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        for (var i = 0; i < texts.Count; i++)
        {
            var index = i;
            var isSelected = i == selected;

            // A plain border, not a Button: the Button template paints its own hover color.
            var text = new TextBlock
            {
                Text = texts[i],
                Foreground = Brush(isSelected ? "Accent" : "Ink"),
                FontFamily = new FontFamily("Nunito"),
                FontSize = 14,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };

            var item = new Border
            {
                Child = text,
                Background = none,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 9, 14, 9),
                MinHeight = 36
            };

            // Only the row under the pointer is green; the chosen row differs by its text.
            item.PointerEntered += (_, _) => item.Background = soft;
            item.PointerExited += (_, _) => item.Background = none;
            item.Tapped += (_, _) =>
            {
                flyout.Hide();
                chosen(index);
            };

            panel.Children.Add(item);
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
            HoverBorderBrush = accent,
            PressedBorderBrush = accent,
            OutOfScopeForeground = muted,
            OutOfScopeBackground = none,
            DayItemFontFamily = new FontFamily("Nunito"),
            MonthYearItemFontFamily = new FontFamily("Nunito"),
            FirstOfMonthLabelFontFamily = new FontFamily("Nunito")
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

    /// <summary>A year with arrows and its twelve months; calls back with the chosen month.</summary>
    public static void ShowMonths(FrameworkElement anchor, DateTime current, Action<DateTime> chosen)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.Bottom,
            FlyoutPresenterStyle = PresenterStyle(Math.Max(anchor.ActualWidth, 300), 12)
        };

        var year = current.Year;
        var soft = Brush("AccentSoft");
        var none = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        var names = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames;
        var shortNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;

        var root = new StackPanel { Spacing = 10, RequestedTheme = Theme };
        var title = new TextBlock
        {
            FontFamily = new FontFamily("Nunito"),
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("Ink"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid { ColumnSpacing = 6, RowSpacing = 6 };
        for (var c = 0; c < 3; c++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var r = 0; r < 4; r++) grid.RowDefinitions.Add(new RowDefinition());

        TextBlock Arrow(string glyph) => new()
        {
            Text = glyph,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("InkMuted"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Border Hit(UIElement child, Action click, double size = 36)
        {
            var box = new Border
            {
                Child = (FrameworkElement)child,
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(size / 2),
                Background = none
            };

            box.PointerEntered += (_, _) => box.Background = soft;
            box.PointerExited += (_, _) => box.Background = none;
            box.Tapped += (_, _) => click();
            return box;
        }

        void Build()
        {
            title.Text = year.ToString();
            grid.Children.Clear();

            for (var m = 1; m <= 12; m++)
            {
                var month = m;
                var isSelected = year == current.Year && month == current.Month;
                var label = !string.IsNullOrEmpty(shortNames[m - 1]) ? shortNames[m - 1] : names[m - 1];

                var cell = new Border
                {
                    Child = new TextBlock
                    {
                        Text = label,
                        FontFamily = new FontFamily("Nunito"),
                        FontSize = 14,
                        FontWeight = isSelected ? FontWeights.Bold : FontWeights.Medium,
                        Foreground = Brush(isSelected ? "OnAccent" : "Ink"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    Background = isSelected ? Brush("Accent") : none,
                    CornerRadius = new CornerRadius(12),
                    Height = 40
                };

                if (!isSelected)
                {
                    cell.PointerEntered += (_, _) => cell.Background = soft;
                    cell.PointerExited += (_, _) => cell.Background = none;
                }

                cell.Tapped += (_, _) =>
                {
                    flyout.Hide();
                    chosen(new DateTime(year, month, 1));
                };

                Grid.SetColumn(cell, (m - 1) % 3);
                Grid.SetRow(cell, (m - 1) / 3);
                grid.Children.Add(cell);
            }
        }

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var back = Hit(Arrow("\u2039"), () => { year = Math.Max(1, year - 1); Build(); });
        var forward = Hit(Arrow("\u203A"), () => { year = Math.Min(9999, year + 1); Build(); });

        Grid.SetColumn(title, 1);
        Grid.SetColumn(forward, 2);
        header.Children.Add(back);
        header.Children.Add(title);
        header.Children.Add(forward);

        root.Children.Add(header);
        root.Children.Add(grid);
        Build();

        flyout.Content = root;
        flyout.ShowAt(anchor);
    }

    /// <summary>The WinUI color picker (spectrum, sliders, hex) in a flyout under the anchor.</summary>
    public static void ShowColorPicker(FrameworkElement anchor, Microsoft.Maui.Graphics.Color? initial, Action<Microsoft.Maui.Graphics.Color> chosen)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.Bottom,
            FlyoutPresenterStyle = PresenterStyle(0, 12)
        };

        var picker = new ColorPicker
        {
            RequestedTheme = Theme,
            IsAlphaEnabled = false,
            IsMoreButtonVisible = false,
            IsColorChannelTextInputVisible = false,
            IsHexInputVisible = true,
            Color = (initial ?? Microsoft.Maui.Graphics.Colors.Gray).ToWindowsColor()
        };

        var done = new Microsoft.UI.Xaml.Controls.Button
        {
            Content = Loc.T("Choose"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(18, 8, 18, 8),
            Background = Brush("Accent"),
            Foreground = Brush("OnAccent"),
            FontFamily = new FontFamily("Nunito"),
            FontWeight = FontWeights.Bold
        };

        done.Resources["ButtonBackgroundPointerOver"] = Brush("Accent");
        done.Resources["ButtonBackgroundPressed"] = Brush("Accent");
        done.Resources["ButtonForegroundPointerOver"] = Brush("OnAccent");
        done.Resources["ButtonForegroundPressed"] = Brush("OnAccent");
        done.Click += (_, _) =>
        {
            flyout.Hide();
            var c = picker.Color;
            chosen(Microsoft.Maui.Graphics.Color.FromRgb(c.R, c.G, c.B));
        };

        var panel = new StackPanel { RequestedTheme = Theme };
        panel.Children.Add(picker);
        panel.Children.Add(done);

        flyout.Content = panel;
        flyout.ShowAt(anchor);
    }
}
