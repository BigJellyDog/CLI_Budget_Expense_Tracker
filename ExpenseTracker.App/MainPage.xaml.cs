namespace ExpenseTracker.App;

using Microsoft.Extensions.DependencyInjection;
using Controls;
using Models;
using Services;
using ViewModels;
using Views;

public partial class MainPage : ContentPage
{
    private readonly ExpensesViewModel _viewModel;
    private readonly IServiceProvider _services;

    private bool _openingPage;
    private bool _confirmingDelete;

    /// <summary>Creates the expenses page: the month, filters, list and the summary cards.</summary>
    public MainPage(
        ExpensesViewModel viewModel,
        IServiceProvider services)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _services = services;

        BindingContext = viewModel;
    }

    /// <summary>Loads the data each time the page is shown, so it is current after returning from another page.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reports can ask for one category of one month.
        if (_services.GetRequiredService<ExpenseFilterRequest>().Take() is { } request)
            await _viewModel.OpenAsync(request.CategoryId, request.Month);
        else
            await _viewModel.RefreshAsync();
    }

    /// <summary>Shows the previous month.</summary>
    private void OnPreviousClicked(object? sender, EventArgs e) =>
        _viewModel.PreviousMonthCommand.Execute(null);

    /// <summary>Shows the next month.</summary>
    private void OnNextClicked(object? sender, EventArgs e) =>
        _viewModel.NextMonthCommand.Execute(null);

    /// <summary>Shows the month chosen in the month picker.</summary>
    private async void OnMonthPicked(object? sender, DateTime month) =>
        await _viewModel.SetMonthAsync(month);

    /// <summary>Reloads the data from the database.</summary>
    private void OnRefreshClicked(object? sender, EventArgs e) =>
        _viewModel.RefreshCommand.Execute(null);

    /// <summary>Opens the editor for a new expense.</summary>
    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await OpenPageAsync<ExpenseEditorPage>();
    
    /// <summary>Opens the editor for the expense of the clicked row.</summary>
    private async void OnEditExpenseClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: ExpenseListItem expense })
            return;

        await OpenPageAsync<ExpenseEditorPage>(
            page => page.ConfigureForEdit(expense.Id));
    }

private async void OnCategoriesClicked(object? sender, EventArgs e) =>
        await OpenPageAsync<CategoriesPage>();
    
private async void OnReportsClicked(object? sender, EventArgs e) =>
        await OpenPageAsync<ReportsPage>();

private async void OnRecurringExpensesClicked(object? sender, EventArgs e) =>
        await OpenPageAsync<RecurringExpensesPage>();

    /// <summary>Opens the export menu: CSV, Markdown, backup and the export folder.</summary>
    private void OnExportClicked(object? sender, EventArgs e) =>
        MenuPopup.Show(
            ExportButton,
            [Loc.T("CSV"), Loc.T("Markdown"), Loc.T("Backup"), Loc.T("Open export folder")],
            async index =>
            {
                switch (index)
                {
                    case 0: await ExportAsync(ExportFormat.Csv); break;
                    case 1: await ExportAsync(ExportFormat.Markdown); break;
                    case 2: await BackupAsync(); break;
                    default: await OpenExportFolderAsync(); break;
                }
            });

    /// <summary>Shows or hides the filter panel.</summary>
    private void OnToggleFiltersTapped(object? sender, EventArgs e) =>
        _viewModel.ToggleFilters();

    // Narrow window: the side column would squeeze the list, so it steps aside.
    private void OnBodySizeChanged(object? sender, EventArgs e)
    {
        var wide = Body.Width >= 900;

        SideColumn.IsVisible = wide;
        Body.ColumnDefinitions[1].Width = wide ? new GridLength(360) : new GridLength(0);
        Body.ColumnSpacing = wide ? 20 : 0;
    }

    // The edit and delete icons of a row show only under the pointer.
    private static void ShowRowActions(object? sender, bool visible)
    {
        if (sender is not Grid row) return;

        if (visible)
            row.SetDynamicResource(BackgroundColorProperty, "Surface2");
        else
            row.BackgroundColor = Colors.Transparent;

        if (row.Children.OfType<HorizontalStackLayout>().FirstOrDefault() is { } actions)
            actions.Opacity = visible ? 1 : 0;
    }

    /// <summary>Shows the edit and delete icons of the row under the pointer.</summary>
    private void OnRowPointerEntered(object? sender, PointerEventArgs e) => ShowRowActions(sender, true);

    /// <summary>Hides the icons of the row again.</summary>
    private void OnRowPointerExited(object? sender, PointerEventArgs e) => ShowRowActions(sender, false);

    /// <summary>Opens the reports page.</summary>
    private async void OnReportLinkTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("//reports");

    /// <summary>Filters the list by the category of the clicked donut segment.</summary>
    private async void OnDonutSegmentTapped(object? sender, int index)
    {
        if (index >= 0 && index < _viewModel.Legend.Count)
            await _viewModel.SelectCategoryAsync(_viewModel.Legend[index].CategoryId);
    }

    /// <summary>Filters the list by the category of the clicked legend row.</summary>
    private async void OnLegendTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: LegendItem item })
            await _viewModel.SelectCategoryAsync(item.CategoryId);
    }

    /// <summary>Highlights the donut segment of the legend row under the pointer.</summary>
    private void OnLegendPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is BindableObject { BindingContext: LegendItem item })
            Donut.HighlightIndex = _viewModel.Legend.ToList().IndexOf(item);
    }

    /// <summary>Removes the donut highlight.</summary>
    private void OnLegendPointerExited(object? sender, PointerEventArgs e) =>
        Donut.HighlightIndex = -1;

    /// <summary>Exports the shown expenses and offers to open the export folder.</summary>
    private async Task ExportAsync(ExportFormat format)
    {
        if (_openingPage || _confirmingDelete || _viewModel.IsBusy)
            return;

        var path = await _viewModel.ExportAsync(format);

        if (path is not null
            && await DisplayAlertAsync(Loc.T("Export done"), path, Loc.T("Open folder"), Loc.T("OK")))
            await OpenExportFolderAsync();
    }

    /// <summary>Opens the export folder, telling the person when Windows cannot open it.</summary>
    private async Task OpenExportFolderAsync()
    {
        if (!_viewModel.OpenExportFolder())
            await DisplayAlertAsync(Loc.T("Export"), Loc.T("Could not open the export folder."), Loc.T("OK"));
    }

    /// <summary>Asks whether to create a backup or restore one.</summary>
    private async Task BackupAsync()
    {
        if (_openingPage || _confirmingDelete || _viewModel.IsBusy)
            return;

        _confirmingDelete = true;

        try
        {
            var choice = await DisplayActionSheetAsync(
                Loc.T("Backup"),
                Loc.T("Cancel"),
                null,
                Loc.T("Create backup"),
                Loc.T("Restore from backup"));

            if (choice == Loc.T("Create backup"))
            {
                var path = await _viewModel.CreateBackupAsync();

                if (path is not null)
                    await DisplayAlertAsync(Loc.T("Backup created"), path, Loc.T("OK"));
            }
            else if (choice == Loc.T("Restore from backup"))
            {
                await RestoreAsync();
            }
        }
        finally
        {
            _confirmingDelete = false;
        }
    }

    /// <summary>Lets the person pick one of the latest backups and restores it after a confirmation.</summary>
    private async Task RestoreAsync()
    {
        var names = _viewModel.GetBackups()
            .Take(15)
            .Select(x => x.FileName)
            .ToArray();

        if (names.Length == 0)
        {
            await DisplayAlertAsync(Loc.T("No backups"), Loc.T("Create a backup first."), Loc.T("OK"));
            return;
        }

        var picked = await DisplayActionSheetAsync(
            Loc.T("Choose a backup (newest first)"),
            Loc.T("Cancel"),
            null,
            names);

        if (picked is null || !names.Contains(picked))
            return;

        var confirmed = await DisplayAlertAsync(
            Loc.T("Restore backup?"),
            $"{picked}\n\nCurrent data will be replaced. A copy of the current database is saved first.",
            Loc.T("Restore"),
            Loc.T("Cancel"));

        if (confirmed && await _viewModel.RestoreBackupAsync(picked))
            await DisplayAlertAsync(Loc.T("Restored"), picked, Loc.T("OK"));
    }

    /// <summary>Applies the category chosen in the filter.</summary>
    private async void OnCategoryFilterChanged(object? sender, EventArgs e)
    {
        if (!_viewModel.CanChangeCategoryFilter
            || sender is not DropDown
            {
                SelectedItem: ExpenseCategoryFilter filter
            })
        {
            return;
        }

        _viewModel.SelectedCategoryFilter = filter;
        await _viewModel.RefreshAsync();
    }

    /// <summary>Reloads the list after the period or the sort order changed.</summary>
    private async void OnPeriodOrSortChanged(object? sender, EventArgs e)
    {
        if (!_viewModel.CanChangeCategoryFilter)
            return;

        await _viewModel.RefreshAsync();
    }

    /// <summary>Applies the filters.</summary>
    private async void OnApplyFiltersClicked(object? sender, EventArgs e) =>
        await _viewModel.RefreshAsync();

    /// <summary>Clears the filters and reloads the list.</summary>
    private async void OnResetFiltersClicked(object? sender, EventArgs e)
    {
        _viewModel.ResetFilters();
        await _viewModel.RefreshAsync();
    }
    
    /// <summary>Opens a page on top of this one, unless another dialog or page is already opening.</summary>
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

    /// <summary>Asks for a confirmation and deletes the expense of the clicked row.</summary>
    private async void OnDeleteExpenseClicked(object? sender, EventArgs e)
    {
        if (_confirmingDelete || _openingPage || _viewModel.IsBusy
            || sender is not BindableObject { BindingContext: ExpenseListItem expense })
        {
            return;
        }

        _confirmingDelete = true;

        try
        {
            var confirmed = await DisplayAlertAsync(
                Loc.T("Delete expense?"),
                $"{expense.CategoryName}\n{expense.Amount:N2} · {expense.Date:dd.MM.yyyy}",
                Loc.T("Delete"),
                Loc.T("Cancel"));

            if (confirmed)
                await _viewModel.DeleteAsync(expense.Id);
        }
        finally
        {
            _confirmingDelete = false;
        }
    }
}