namespace ExpenseTracker.App.Views;

using Models;
using ViewModels;

public partial class BudgetsPage : ContentPage
{
    private readonly BudgetsViewModel _viewModel;

    public BudgetsPage(BudgetsViewModel viewModel)
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

    private async void OnPreviousClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(-1);

    private async void OnNextClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(1);

    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit
            || sender is not Button { BindingContext: BudgetListItem item })
        {
            return;
        }

        _viewModel.SelectBudget(item);
        LimitEntry.Focus();
    }
}