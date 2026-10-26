namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;

public sealed class ReportsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<ReportsViewModel> _logger;

    private DateOnly _month;
    private bool _loaded;

    public ReportsViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        ILogger<ReportsViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        _month = new DateOnly(today.Year, today.Month, 1);
    }

    public IReadOnlyList<CategoryReportItem> Categories { get; private set; } = [];

    public IReadOnlyList<TopExpenseItem> TopExpenses { get; private set; } = [];

    public string MonthTitle => _month.ToString("MMMM yyyy");

    public string TotalText { get; private set; } = "";

    public string ComparisonText { get; private set; } = "";

    public string AverageText { get; private set; } = "";

    public string ForecastText { get; private set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasForecast => ForecastText.Length > 0;

    public bool CanGoBack => !IsBusy;

    public bool CanNavigate => !IsBusy;

    public bool CanPreviousMonth => !IsBusy && _month > new DateOnly(1, 2, 1);

    public bool CanNextMonth => !IsBusy && _month < new DateOnly(9999, 11, 1);

    public bool ShowResults => _loaded && !IsBusy;

    public bool ShowEmpty => ShowResults && Categories.Count == 0;

    public async Task MoveMonthAsync(int offset)
    {
        if (offset != -1 && offset != 1)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if ((offset < 0 && !CanPreviousMonth)
            || (offset > 0 && !CanNextMonth))
        {
            return;
        }

        _month = _month.AddMonths(offset);
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            var month = _month;

            var (report, names) = await Task.Run(async () =>
            {
                await _initializer.InitializeAsync();

                await using var scope = _scopeFactory.CreateAsyncScope();
                var services = scope.ServiceProvider;

                var report = await services
                    .GetRequiredService<ReportService>()
                    .GetMonthAsync(month);

                var names = (await services
                        .GetRequiredService<CategoryService>()
                        .GetAllAsync())
                    .ToDictionary(x => x.Id, x => x.Name);

                return (report, names);
            });

            Apply(report, names);
            _loaded = true;
        }
        catch (Exception exception)
        {
            _loaded = false;

            _logger.LogError(exception, "Failed to build the monthly report.");
            ErrorMessage = "Could not build the report. Try again.";
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    private void Apply(PeriodReport report, Dictionary<Guid, string> names)
    {
        string Name(Guid id) => names.GetValueOrDefault(id, "Unknown category");

        var max = report.Categories.Select(x => x.Total).DefaultIfEmpty(0).Max();

        Categories = report.Categories
            .Select(x => new CategoryReportItem(
                Name(x.CategoryId),
                x.Total,
                x.PreviousTotal,
                x.ChangePercent,
                max > 0 ? (double)(x.Total / max) : 0d))
            .ToArray();

        TopExpenses = report.TopExpenses
            .Select(x => new TopExpenseItem(
                x.Date, Name(x.CategoryId), x.Description, x.Amount))
            .ToArray();

        TotalText = $"Total: {report.Total:N2}";

        ComparisonText =
            $"Previous month: {report.PreviousTotal:N2} · Change: " +
            ReportFormat.Change(report.ChangePercent, report.Total, report.PreviousTotal);

        AverageText = report.DaysElapsed == 0
            ? "Average per day: no days have passed yet"
            : $"Average per day: {report.AveragePerDay:N2} (over {report.DaysElapsed} days)";

        ForecastText = report.Forecast is { } forecast
            ? $"Forecast for the whole month: {forecast:N2}"
            : "";
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Categories));
        OnPropertyChanged(nameof(TopExpenses));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(ComparisonText));
        OnPropertyChanged(nameof(AverageText));
        OnPropertyChanged(nameof(ForecastText));
        OnPropertyChanged(nameof(HasForecast));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanNavigate));
        OnPropertyChanged(nameof(CanPreviousMonth));
        OnPropertyChanged(nameof(CanNextMonth));
        OnPropertyChanged(nameof(ShowResults));
        OnPropertyChanged(nameof(ShowEmpty));
    }
}
