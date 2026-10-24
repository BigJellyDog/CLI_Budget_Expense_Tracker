namespace ExpenseTracker.App.Views;

using ViewModels;
using Models;

public partial class RecurringExpensesPage : ContentPage
{
    private readonly RecurringExpensesViewModel _viewModel;

    public RecurringExpensesPage(RecurringExpensesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnGenerateClicked(object? sender, EventArgs e) =>
        await _viewModel.GenerateDueAsync();

    private async void OnCreateClicked(object? sender, EventArgs e) =>
        await _viewModel.CreateAsync();

    private void OnAddAnotherClicked(object? sender, EventArgs e) =>
        _viewModel.StartNewSchedule();
    
    private async void OnDeactivateClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not Button
            {
                BindingContext: RecurringExpenseListItem item
            })
        {
            return;
        }

        await _viewModel.DeactivateAsync(
            item.Id,
            () => DisplayAlertAsync(
                "Deactivate schedule?",
                $"{item.CategoryName}\n{item.Summary}\n{item.Description}\n\n"
                + "Stop generating expenses for this schedule? Existing expenses will be kept.",
                "Deactivate",
                "Cancel"));
    }
}