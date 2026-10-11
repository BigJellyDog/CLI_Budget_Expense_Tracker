namespace ExpenseTracker.App;

using Microsoft.Extensions.DependencyInjection;
using Models;
using ViewModels;
using Views;

public partial class MainPage : ContentPage
{
    private readonly ExpensesViewModel _viewModel;
    private readonly IServiceProvider _services;

    private bool _openingExpense;
    private bool _confirmingDelete;

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
        if (_openingExpense || _confirmingDelete || _viewModel.IsBusy)
            return;

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

    private async void OnDeleteExpenseClicked(object? sender, EventArgs e)
    {
        if (_confirmingDelete || _openingExpense || _viewModel.IsBusy
            || sender is not Button { BindingContext: ExpenseListItem expense })
        {
            return;
        }

        _confirmingDelete = true;

        try
        {
            var confirmed = await DisplayAlertAsync(
                "Delete expense?",
                $"{expense.CategoryName}\n{expense.Amount:N2} · {expense.Date:dd.MM.yyyy}",
                "Delete",
                "Cancel");

            if (confirmed)
                await _viewModel.DeleteAsync(expense.Id);
        }
        finally
        {
            _confirmingDelete = false;
        }
    }
}