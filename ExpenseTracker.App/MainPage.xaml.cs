namespace ExpenseTracker.App;

using Microsoft.Extensions.DependencyInjection;
using Models;
using ViewModels;
using Views;

public partial class MainPage : ContentPage
{
    private readonly ExpensesViewModel _viewModel;
    private readonly IServiceProvider _services;

    private bool _openingPage;
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

    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await OpenPageAsync<ExpenseEditorPage>();
    
    private async void OnEditExpenseClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: ExpenseListItem expense })
            return;

        await OpenPageAsync<ExpenseEditorPage>(
            page => page.ConfigureForEdit(expense.Id));
    }

    private async void OnBankImportClicked(object? sender, EventArgs e) =>
        await OpenPageAsync<BankImportPage>();

    private async Task OpenPageAsync<TPage>(
        Action<TPage>? configure = null)
        where TPage : Page
    {
        if (_openingPage || _confirmingDelete || _viewModel.IsBusy)
            return;

        _openingPage = true;

        try
        {
            var page = _services.GetRequiredService<TPage>();

            configure?.Invoke(page);

            await Navigation.PushAsync(page);
        }
        finally
        {
            _openingPage = false;
        }
    }

    private async void OnDeleteExpenseClicked(object? sender, EventArgs e)
    {
        if (_confirmingDelete || _openingPage || _viewModel.IsBusy
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