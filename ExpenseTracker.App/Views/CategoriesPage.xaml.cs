namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Controls;
using ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Models;
using ViewModels;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;

    private static string DeleteWithDataText => Loc.T("Delete category and all its data");

    private bool _choosingDeleteAction;

    private readonly IServiceProvider _services;

    public CategoriesPage(CategoriesViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    private async void OnRulesClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit) return;

        await Navigation.PushAsync(_services.GetRequiredService<RulesPage>());
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

    private async void OnMonthPicked(object? sender, DateTime month) =>
        await _viewModel.SetMonthAsync(month);

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryTile tile })
            _viewModel.Select(tile.Category.Id);
    }

    private void OnSwatchTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not VisualElement { BindingContext: SwatchItem swatch } view)
            return;

        if (!swatch.IsCustom)
        {
            _viewModel.SetSelectedColor(swatch.Slot);
            return;
        }

        // The last swatch opens a color picker for a color of the person's own.
        ColorPickerPopup.Show(view, _viewModel.Selected?.Color, _viewModel.SetSelectedCustomColor);
    }

    private static void ShowRowHover(object? sender, bool hovered)
    {
        if (sender is not Border { BindingContext: CategoryTile { IsSelected: false } } row) return;

        if (hovered)
            row.SetDynamicResource(BackgroundColorProperty, "Surface2");
        else
            row.BackgroundColor = Colors.Transparent;
    }

    private void OnRowPointerEntered(object? sender, PointerEventArgs e) => ShowRowHover(sender, true);

    private void OnRowPointerExited(object? sender, PointerEventArgs e) => ShowRowHover(sender, false);

    private async void OnNewClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit) return;

        _viewModel.CancelEdit();

        var name = await DisplayPromptAsync(
            Loc.T("New category"),
            Loc.T("Category name"),
            Loc.T("Create category"),
            Loc.T("Cancel"),
            Loc.T("Category name"));

        if (string.IsNullOrWhiteSpace(name)) return;

        _viewModel.NameText = name;
        await _viewModel.SaveAsync();
    }

    private async void OnRenameClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit || _viewModel.Selected?.Category is not { } category) return;

        _viewModel.StartRename(category);

        var name = await DisplayPromptAsync(
            Loc.T("Rename category"),
            Loc.T("Category name"),
            Loc.T("Save changes"),
            Loc.T("Cancel"),
            Loc.T("Category name"),
            initialValue: category.Name);

        if (string.IsNullOrWhiteSpace(name) || name.Trim() == category.Name)
        {
            _viewModel.CancelEdit();
            return;
        }

        _viewModel.NameText = name;
        await _viewModel.SaveAsync();
        _viewModel.CancelEdit();
    }

    private async void OnShowEntriesClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Selected?.Category is not { } category) return;

        _services.GetRequiredService<ExpenseFilterRequest>().Set(category.Id, _viewModel.Month);
        await Shell.Current.GoToAsync("//expenses");
    }

    private async void OnBudgetClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//budgets");

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_choosingDeleteAction
            || !_viewModel.CanEdit
            || _viewModel.Selected?.Category is not { } category)
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
                        Loc.T("Delete category?"),
                        $"Delete \"{category.Name}\"?",
                        Loc.T("Delete"),
                        Loc.T("Cancel")));

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
            ? Loc.F(" (+{0} deleted, kept in history)", usage.DeletedExpenses)
            : "";

        var picked = await DisplayActionSheetAsync(
            Loc.F("\"{0}\" is used by {1} expenses{2}, {3} schedules and {4} budget limits. ",
                category.Name, usage.ActiveExpenses, history, usage.RecurringExpenses, usage.BudgetLimits)
            + (targets.Length > 0
                ? Loc.T("Move expenses and schedules to another category, or delete everything:")
                : Loc.T("There is no other category to move them to. You can delete everything:")),
            Loc.T("Cancel"),
            DeleteWithDataText,
            targets.Select(x => x.Name).ToArray());

        if (picked == DeleteWithDataText)
        {
            await _viewModel.DeleteWithDataAsync(
                category.Id,
                () => DisplayAlertAsync(
                    Loc.T("Delete everything?"),
                    Loc.F("Permanently delete \"{0}\" together with {1} expenses{2}, {3} schedules and {4} budget limits?",
                        category.Name, usage.ActiveExpenses, history, usage.RecurringExpenses, usage.BudgetLimits)
                    + "\n\n"
                    + Loc.T("A backup is saved first, so you can undo this with Backup > Restore."),
                    Loc.T("Delete everything"),
                    Loc.T("Cancel")));

            return;
        }

        var target = targets.FirstOrDefault(x => x.Name == picked);

        if (target is null)
            return;

        await _viewModel.MoveAndDeleteAsync(
            category.Id,
            target.Id,
            () => DisplayAlertAsync(
                Loc.T("Move and delete?"),
                Loc.F("Move the data of \"{0}\" to \"{1}\" and delete \"{0}\"?", category.Name, target.Name)
                + "\n\n"
                + (usage.BudgetLimits > 0
                    ? Loc.F("Its {0} budget limits will be removed.", usage.BudgetLimits) + "\n\n"
                    : "")
                + Loc.T("This cannot be undone."),
                Loc.T("Move and delete"),
                Loc.T("Cancel")));
    }
}