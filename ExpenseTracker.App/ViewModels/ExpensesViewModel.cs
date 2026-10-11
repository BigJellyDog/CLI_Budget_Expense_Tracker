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

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        var selectedMonth = _month;

        try
        {
            // SQLite work runs off the UI thread.
            // Results are applied after awaiting.
            (Items, _total) = await Task.Run(async () =>
            {
                await _initializer.InitializeAsync();

                await using var scope = _scopeFactory.CreateAsyncScope();
                var services = scope.ServiceProvider;

                var end = new DateOnly(
                    selectedMonth.Year,
                    selectedMonth.Month,
                    DateTime.DaysInMonth(
                        selectedMonth.Year,
                        selectedMonth.Month));

                var report = await services
                    .GetRequiredService<ExpenseService>()
                    .GetReportAsync(selectedMonth, end);

                var categories = await services
                    .GetRequiredService<CategoryService>()
                    .GetAllAsync();

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

                return (rows, report.Total);
            });

            _loaded = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to load expenses for {Month}.",
                selectedMonth);

            ErrorMessage = "Could not load expenses. Try Refresh.";
            _loaded = false;
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

        RefreshCommand.NotifyCanExecuteChanged();
        PreviousMonthCommand.NotifyCanExecuteChanged();
        NextMonthCommand.NotifyCanExecuteChanged();
    }
}