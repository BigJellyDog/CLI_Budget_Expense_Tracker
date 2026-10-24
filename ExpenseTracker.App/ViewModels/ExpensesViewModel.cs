namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;

public sealed class ExpensesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<ExpensesViewModel> _logger;

    private DateOnly _month;
    private bool _loaded;
    private decimal _total;

    public ExpensesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        ILogger<ExpensesViewModel> logger)
    {
        this._scopeFactory = scopeFactory;
        this._initializer = initializer;
        this._logger = logger;

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        _month = new DateOnly(today.Year, today.Month, 1);

        RefreshCommand = new AsyncRelayCommand(
            RefreshAsync,
            () => !IsBusy);

        PreviousMonthCommand = new AsyncRelayCommand(
            () => MoveMonthAsync(-1),
            () => !IsBusy && _month > DateOnly.MinValue);

        NextMonthCommand = new AsyncRelayCommand(
            () => MoveMonthAsync(1),
            () => !IsBusy && _month < new DateOnly(9999, 12, 1));
    }

    public IReadOnlyList<ExpenseListItem> Items { get; private set; } = [];
    
    public IReadOnlyList<ExpenseCategoryFilter> CategoryFilters { get; private set; } = [];

    public ExpenseCategoryFilter? SelectedCategoryFilter { get; set; }

    public bool CanChangeCategoryFilter => ShowResults;

    public string MonthTitle => _month.ToString("MMMM yyyy");

    public string Summary =>
        $"Expenses: {Items.Count} · Total: {_total:N2}";

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool ShowResults => _loaded && !IsBusy && !HasError;

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand PreviousMonthCommand { get; }

    public IAsyncRelayCommand NextMonthCommand { get; }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;

        var selectedMonth = _month;
        var selectedCategoryId = SelectedCategoryFilter?.CategoryId;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            var result = await Task.Run(async () =>
            {
                await _initializer.InitializeAsync();

                await using var scope = _scopeFactory.CreateAsyncScope();
                var services = scope.ServiceProvider;

                var categories = await services
                    .GetRequiredService<CategoryService>()
                    .GetAllAsync();

                IReadOnlyList<ExpenseCategoryFilter> filters =
                [
                    new(null, "All categories"),
                    .. categories.Select(x =>
                        new ExpenseCategoryFilter(x.Id, x.Name))
                ];

                var selectedFilter = filters.FirstOrDefault(
                    x => x.CategoryId == selectedCategoryId) ?? filters[0];

                var end = new DateOnly(
                    selectedMonth.Year,
                    selectedMonth.Month,
                    DateTime.DaysInMonth(
                        selectedMonth.Year,
                        selectedMonth.Month));

                var report = await services
                    .GetRequiredService<ExpenseService>()
                    .GetReportAsync(
                        selectedMonth,
                        end,
                        selectedFilter.CategoryId);

                var names = categories.ToDictionary(
                    x => x.Id,
                    x => x.Name);

                IReadOnlyList<ExpenseListItem> rows = report.Expenses
                    .Select(x => new ExpenseListItem(
                        x.Id,
                        x.Date,
                        x.Amount,
                        names.GetValueOrDefault(
                            x.CategoryId, "Unknown category"),
                        x.Description))
                    .ToArray();

                return (
                    Rows: rows,
                    Total: report.Total,
                    Filters: filters,
                    SelectedFilter: selectedFilter);
            });

            Items = result.Rows;
            _total = result.Total;

            CategoryFilters = result.Filters;
            OnPropertyChanged(nameof(CategoryFilters));

            SelectedCategoryFilter = result.SelectedFilter;
            OnPropertyChanged(nameof(SelectedCategoryFilter));

            _loaded = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load expenses for {Month} and category {CategoryId}.",
                selectedMonth,
                selectedCategoryId);

            ErrorMessage = "Could not load expenses. Try Refresh.";
            _loaded = false;
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }
    
    public async Task DeleteAsync(Guid id)
    {
        if (IsBusy || Items.All(x => x.Id != id)) return;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<ExpenseService>()
                    .DeleteAsync(id);
            });

            // Also remove a stale row if it was already deleted elsewhere.
            Items = Items.Where(x => x.Id != id).ToArray();
            _total = Items.Sum(x => x.Amount);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete expense {ExpenseId}.",
                id);

            ErrorMessage = "Could not delete the expense. Try Refresh.";
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    private async Task MoveMonthAsync(int offset)
    {
        if (IsBusy) return;

        _month = _month.AddMonths(offset);
        await RefreshAsync();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowResults));

        OnPropertyChanged(nameof(CanChangeCategoryFilter));
        RefreshCommand.NotifyCanExecuteChanged();
        PreviousMonthCommand.NotifyCanExecuteChanged();
        NextMonthCommand.NotifyCanExecuteChanged();
    }
}