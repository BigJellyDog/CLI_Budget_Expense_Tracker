namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Services;

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

    // Picking an end date means the schedule has one: the box is ticked for the person.
    private void OnEndDateSelected(object? sender, EventArgs e) =>
        _viewModel.UseEndDate();

    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private async void OnGenerateClicked(object? sender, EventArgs e) =>
        await _viewModel.GenerateDueAsync();

    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    private void OnCancelEditClicked(object? sender, EventArgs e) =>
        _viewModel.CancelEdit();

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not Button
            {
                BindingContext: RecurringExpenseListItem item
            })
        {
            return;
        }

        _viewModel.StartEdit(item.Id);
    }

    private void OnAddAnotherClicked(object? sender, EventArgs e) =>
        _viewModel.StartNewSchedule();
    
    private async void OnReactivateClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not Button
            {
                BindingContext: RecurringExpenseListItem item
            })
        {
            return;
        }

        await _viewModel.ReactivateAsync(item.Id);
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not Button
            {
                BindingContext: RecurringExpenseListItem item
            })
        {
            return;
        }

        await _viewModel.DeleteAsync(
            item.Id,
            () => DisplayAlertAsync(
                Loc.T("Delete schedule?"),
                $"{item.CategoryName}\n{item.Summary}\n{item.Description}\n\n"
                + Loc.T("Delete this schedule? Expenses it already created will be kept."),
                Loc.T("Delete"),
                Loc.T("Cancel")));
    }

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
                Loc.T("Deactivate schedule?"),
                $"{item.CategoryName}\n{item.Summary}\n{item.Description}\n\n"
                + Loc.T("Stop generating expenses for this schedule? Existing expenses will be kept."),
                Loc.T("Deactivate"),
                Loc.T("Cancel")));
    }
}