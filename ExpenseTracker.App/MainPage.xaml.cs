namespace ExpenseTracker.App;

using Microsoft.Extensions.DependencyInjection;
using ViewModels;
using Views;

public partial class MainPage : ContentPage
{
    private readonly ExpensesViewModel _viewModel;
    private readonly IServiceProvider _services;

    private bool _openingExpense;

    public MainPage(
        ExpensesViewModel viewModel,
        IServiceProvider services)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _services = services;

        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnAddExpenseClicked(object? sender, EventArgs e)
    {
        if (_openingExpense || _viewModel.IsBusy) return;

        _openingExpense = true;

        try
        {
            var page = _services.GetRequiredService<AddExpensePage>();
            await Navigation.PushAsync(page);
        }
        finally
        {
            _openingExpense = false;
        }
    }
}