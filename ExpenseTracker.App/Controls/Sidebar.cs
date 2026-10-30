namespace ExpenseTracker.App.Controls;

using ExpenseTracker.App.Services;

using Microsoft.Maui.Controls.Shapes;

/// <summary>The left menu, shown on every screen. Highlights the section that is open.</summary>
public sealed class Sidebar : ContentView
{
    private readonly Dictionary<string, NavItem> _items = new();

    public Sidebar()
    {
        var items = new VerticalStackLayout { Spacing = 4 };

        void Add(string route, string text, string icon, Layout target)
        {
            var item = new NavItem { Text = Loc.T(text), Icon = icon };
            item.Clicked += async (_, _) => await Shell.Current.GoToAsync($"//{route}");
            _items[route] = item;
            target.Add(item);
        }

        Add("expenses", "Expenses", Icons.List, items);
        Add("import", Loc.T("Import CSV"), Icons.Upload, items);
        Add("categories", "Categories", Icons.Tag, items);
        Add("budgets", "Budgets", Icons.Wallet, items);
        Add("reports", "Reports", Icons.Chart, items);
        Add("recurring", "Recurring", Icons.Repeat, items);

        var settings = new VerticalStackLayout();
        Add("settings", "Settings", Icons.Sliders, settings);

        var mark = new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 9 },
            Content = new Label
            {
                Text = "€",
                FontFamily = "ManropeExtraBold",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        mark.SetDynamicResource(BackgroundColorProperty, "Accent");
        ((Label)mark.Content).SetDynamicResource(Label.TextColorProperty, "OnAccent");

        var title = new Label
        {
            Text = Loc.T("Expenses"),
            FontSize = 16,
            FontFamily = "ManropeExtraBold",
            VerticalOptions = LayoutOptions.Center
        };

        var logo = new HorizontalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(10, 0, 10, 16),
            Children = { mark, title }
        };

        var grid = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            Padding = new Thickness(10, 18)
        };
        grid.Add(logo, 0, 0);
        grid.Add(items, 0, 1);
        grid.Add(settings, 0, 2);

        var panel = new Border
        {
            Margin = new Thickness(12),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = grid
        };
        panel.SetDynamicResource(BackgroundColorProperty, "Surface");

        // The menu pane of the Shell has its own background; this fills it with the page color.
        this.SetDynamicResource(BackgroundColorProperty, "Bg");

        Content = panel;

        HandlerChanged += (_, _) =>
        {
            if (Handler is null || Shell.Current is not { } shell) return;

            shell.Navigated -= OnNavigated;
            shell.Navigated += OnNavigated;
            Highlight(shell.CurrentState?.Location.OriginalString);
        };
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs e) =>
        Highlight(e.Current?.Location.OriginalString);

    private void Highlight(string? location)
    {
        var route = (location ?? "").TrimStart('/').Split('/')[0];

        foreach (var (key, item) in _items)
            item.IsActive = key == route;
    }
}
