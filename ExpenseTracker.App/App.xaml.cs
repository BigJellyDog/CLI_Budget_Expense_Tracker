namespace ExpenseTracker.App;

using Microsoft.Extensions.DependencyInjection;
using Services;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this._services = services;

        services.GetRequiredService<ThemeService>().Attach(this);
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            Title = "Expense Tracker",
            Width = 1440,
            Height = 860
        };

        var titleBar = new TitleBar { Title = "Expense Tracker" };
        titleBar.SetDynamicResource(VisualElement.BackgroundColorProperty, "Bg");
        titleBar.SetDynamicResource(TitleBar.ForegroundColorProperty, "Ink");
        window.TitleBar = titleBar;

        // The title bar belongs to the window, so it is colored once the window exists.
        var theme = _services.GetRequiredService<ThemeService>();
        window.Created += (_, _) => theme.Apply();
        window.HandlerChanged += (_, _) => theme.Apply();
        window.Activated += (_, _) => theme.Apply();

        return window;
    }
}
