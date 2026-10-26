namespace ExpenseTracker.App.Views;

using CLI_Budget_Expense_Tracker.Core.Models;
using ViewModels;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;

    private const string DeleteWithDataText = "Delete category and all its data";

    private bool _choosingDeleteAction;

    public CategoriesPage(CategoriesViewModel viewModel)
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

    private async void OnSaveClicked(object? sender, EventArgs e) =>
        await _viewModel.SaveAsync();

    private void OnCancelClicked(object? sender, EventArgs e) =>
        _viewModel.CancelEdit();

    private void OnRenameClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit
            || sender is not Button { BindingContext: Category category })
        {
            return;
        }

        _viewModel.StartRename(category);
        CategoryNameEntry.Focus();
    }
    
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_choosingDeleteAction
            || !_viewModel.CanEdit
            || sender is not Button { BindingContext: Category category })
        {
            return;
        }

        _choosingDeleteAction = true;

        try
        {
            var usage = await _viewModel.GetUsageAsync(category.Id);

            if (usage is null)
                return;

            if (!usage.IsUsed)
            {
                await _viewModel.DeleteAsync(
                    category.Id,
                    () => DisplayAlertAsync(
                        "Delete category?",
                        $"Delete \"{category.Name}\"?",
                        "Delete",
                        "Cancel"));

                return;
            }

            await MoveAndDeleteAsync(category, usage);
        }
        finally
        {
            _choosingDeleteAction = false;
        }
    }

    private async Task MoveAndDeleteAsync(Category category, CategoryUsage usage)
    {
        var targets = _viewModel.Items
            .Where(x => x.Id != category.Id)
            .ToArray();

        var history = usage.DeletedExpenses > 0
            ? $" (+{usage.DeletedExpenses} deleted, kept in history)"
            : "";

        var picked = await DisplayActionSheetAsync(
            $"\"{category.Name}\" is used by {usage.ActiveExpenses} expenses{history}, "
            + $"{usage.RecurringExpenses} schedules and {usage.BudgetLimits} budget limits. "
            + (targets.Length > 0
                ? "Move expenses and schedules to another category, or delete everything:"
                : "There is no other category to move them to. You can delete everything:"),
            "Cancel",
            DeleteWithDataText,
            targets.Select(x => x.Name).ToArray());

        if (picked == DeleteWithDataText)
        {
            await _viewModel.DeleteWithDataAsync(
                category.Id,
                () => DisplayAlertAsync(
                    "Delete everything?",
                    $"Permanently delete \"{category.Name}\" together with "
                    + $"{usage.ActiveExpenses} expenses{history}, "
                    + $"{usage.RecurringExpenses} schedules and {usage.BudgetLimits} budget limits?\n\n"
                    + "A backup is saved first, so you can undo this with Backup > Restore.",
                    "Delete everything",
                    "Cancel"));

            return;
        }

        var target = targets.FirstOrDefault(x => x.Name == picked);

        if (target is null)
            return;

        await _viewModel.MoveAndDeleteAsync(
            category.Id,
            target.Id,
            () => DisplayAlertAsync(
                "Move and delete?",
                $"Move the data of \"{category.Name}\" to \"{target.Name}\" and delete \"{category.Name}\"?\n\n"
                + (usage.BudgetLimits > 0
                    ? $"Its {usage.BudgetLimits} budget limits will be removed.\n\n"
                    : "")
                + "This cannot be undone.",
                "Move and delete",
                "Cancel"));
    }
}