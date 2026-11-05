namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Services;

using ViewModels;
using Models;

public partial class RecurringExpensesPage : ContentPage
{
    private readonly RecurringExpensesViewModel _viewModel;

    /// <summary>Creates the page of recurring expenses (subscriptions and other schedules).</summary>
    public RecurringExpensesPage(RecurringExpensesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the data each time the page is shown, so it is current after returning from another page.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    /// <summary>Ignores the back button while something is being saved or loaded.</summary>
    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    // Picking an end date means the schedule has one: the box is ticked for the person.
    private void OnEndDateSelected(object? sender, EventArgs e) =>
        _viewModel.UseEndDate();

    /// <summary>Reloads the data from the database.</summary>
    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    /// <summary>Creates the expenses that are due until today.</summary>
    private async void OnGenerateClicked(object? sender, EventArgs e) =>
        await _viewModel.GenerateDueAsync();

    /// <summary>Saves the form.</summary>
    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    /// <summary>Leaves edit mode without saving.</summary>
    private void OnCancelEditClicked(object? sender, EventArgs e) =>
        _viewModel.CancelEdit();

    /// <summary>Loads the schedule of the clicked row into the form.</summary>
    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not BindableObject
            {
                BindingContext: RecurringExpenseListItem item
            })
        {
            return;
        }

        _viewModel.StartEdit(item.Id);
    }

    /// <summary>Clears the form for the next schedule.</summary>
    private void OnAddAnotherClicked(object? sender, EventArgs e) =>
        _viewModel.StartNewSchedule();
    
    /// <summary>Resumes an inactive schedule.</summary>
    private async void OnReactivateClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not BindableObject
            {
                BindingContext: RecurringExpenseListItem item
            })
        {
            return;
        }

        await _viewModel.ReactivateAsync(item.Id);
    }

    /// <summary>Asks for a confirmation and deletes an inactive schedule; its expenses stay.</summary>
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not BindableObject
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

    /// <summary>Stops a schedule; its expenses stay.</summary>
    private async void OnDeactivateClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.ShowResults
            || sender is not BindableObject
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