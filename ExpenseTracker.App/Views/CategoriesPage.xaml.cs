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

    /// <summary>Creates the categories page: the list and the card of the selected category.</summary>
    public CategoriesPage(CategoriesViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    /// <summary>Opens the category rules.</summary>
    private async void OnRulesClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit) return;

        await Navigation.PushAsync(_services.GetRequiredService<RulesPage>());
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

    /// <summary>Shows the previous month.</summary>
    private async void OnPreviousClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(-1);

    /// <summary>Shows the next month.</summary>
    private async void OnNextClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(1);

    /// <summary>Shows the month chosen in the month picker.</summary>
    private async void OnMonthPicked(object? sender, DateTime month) =>
        await _viewModel.SetMonthAsync(month);

    /// <summary>Selects the tapped category.</summary>
    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryTile tile })
            _viewModel.Select(tile.Category.Id);
    }

    /// <summary>Gives the selected category the tapped color, or opens the picker for a custom one.</summary>
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

    /// <summary>Lets the person pick an SVG file and uses it as the icon of the selected category.</summary>
    private async void OnImportIconClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanEdit) return;

        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = Loc.T("Import SVG"),
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = [".svg"]
                })
            });

            if (file is null) return;

            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var svg = await reader.ReadToEndAsync();

            if (!_viewModel.SetSelectedIconFromSvg(svg))
            {
                await DisplayAlertAsync(
                    Loc.T("Import SVG"),
                    Loc.T("This SVG cannot be used. Use a simple line icon on a 24 x 24 grid."),
                    Loc.T("OK"));
            }
        }
        catch (Exception)
        {
            await DisplayAlertAsync(Loc.T("Import SVG"), Loc.T("The file could not be read."), Loc.T("OK"));
        }
    }

    /// <summary>A tapped icon of the picker becomes the icon of the selected category.</summary>
    private void OnIconTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: IconChoice choice })
            _viewModel.SetSelectedIcon(choice.Key);
    }

    /// <summary>Tints a category row while the pointer is over it (not the selected one).</summary>
    private static void ShowRowHover(object? sender, bool hovered)
    {
        if (sender is not Border { BindingContext: CategoryTile { IsSelected: false } } row) return;

        if (hovered)
            row.SetDynamicResource(BackgroundColorProperty, "Surface2");
        else
            row.BackgroundColor = Colors.Transparent;
    }

    /// <summary>Tints the row under the pointer.</summary>
    private void OnRowPointerEntered(object? sender, PointerEventArgs e) => ShowRowHover(sender, true);

    /// <summary>Removes the tint of the row.</summary>
    private void OnRowPointerExited(object? sender, PointerEventArgs e) => ShowRowHover(sender, false);

    /// <summary>Asks for a name and creates a category.</summary>
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

    /// <summary>Asks for a new name and renames the selected category.</summary>
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

    /// <summary>Opens the expenses list filtered by the selected category and month.</summary>
    private async void OnShowEntriesClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Selected?.Category is not { } category) return;

        _services.GetRequiredService<ExpenseFilterRequest>().Set(category.Id, _viewModel.Month);
        await Shell.Current.GoToAsync("//expenses");
    }

    /// <summary>Opens the budgets page.</summary>
    private async void OnBudgetClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//budgets");

    /// <summary>Deletes the selected category; if it has expenses, offers to move them or delete them too.</summary>
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

    /// <summary>Asks where to move the expenses of the category, then deletes it.</summary>
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