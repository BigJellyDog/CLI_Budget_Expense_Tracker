namespace ExpenseTracker.App;

using Microsoft.Extensions.DependencyInjection;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this._services = services;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        var page = _services.GetRequiredService<MainPage>();

        return new Window(new NavigationPage(page))
        {
            Title = "Expense Tracker",
            Width = 1100,
            Height = 760
        };
    }
}