namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Controls;
using Models;
using Services;

public sealed class ReportsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<ReportsViewModel> _logger;

    private readonly AppSettings _settings;

    private DateOnly _month;
    private bool _loaded;

    public ReportsViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        AppSettings settings,
        ILogger<ReportsViewModel> logger)
    {
        _settings = settings;
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        _month = new DateOnly(today.Year, today.Month, 1);
    }

    public IReadOnlyList<CategoryReportItem> Categories { get; private set; } = [];

    public IReadOnlyList<TopExpenseItem> TopExpenses { get; private set; } = [];

    public IReadOnlyList<DonutSegment> DonutSegments { get; private set; } = [];

    public string MonthTitle => _month.ToString("MMMM yyyy");

    public DateTime Month => _month.ToDateTime(TimeOnly.MinValue);

    public string Subtitle => Loc.F("{0} compared with {1}", _month.ToString("MMMM yyyy"), _month.AddMonths(-1).ToString("MMMM yyyy"));

    public string TotalText { get; private set; } = "";

    public string TotalDeltaText { get; private set; } = "";

    public string TotalDeltaTone { get; private set; } = "InkMuted";

    public string AverageText { get; private set; } = "";

    public string AverageCaption { get; private set; } = "";

    public string ForecastTitle => Loc.F("Forecast for {0}", _month.ToString("MMMM"));

    public string ForecastText { get; private set; } = "";

    public string ForecastCaption { get; private set; } = "";

    public string TopCategoryText { get; private set; } = "";

    public string TopCategoryCaption { get; private set; } = "";

    public string DonutTotalText { get; private set; } = "";

    public string DonutTotalTitle => Loc.T("Total");

    public string ComparisonTitle => Loc.F("{0} vs {1}", _month.ToString("MMMM"), _month.AddMonths(-1).ToString("MMMM"));

    public string CurrentMonthName => _month.ToString("MMMM");

    public string PreviousMonthName => _month.AddMonths(-1).ToString("MMMM");

    /// <summary>The bars show shares of the total instead of amounts.</summary>
    public bool ShowShare { get; private set; }

    public bool ShowAmount => !ShowShare;

    public void SetShowShare(bool share)
    {
        if (ShowShare == share) return;

        ShowShare = share;
        BuildRows();
        OnPropertyChanged(nameof(ShowShare));
        OnPropertyChanged(nameof(ShowAmount));
        OnPropertyChanged(nameof(Categories));
    }

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool CanGoBack => !IsBusy;

    public bool CanNavigate => !IsBusy;

    public bool CanPreviousMonth => !IsBusy && _month > new DateOnly(1, 2, 1);

    public bool CanNextMonth => !IsBusy && _month < new DateOnly(9999, 11, 1);

    public bool ShowResults => _loaded && !IsBusy;

    public bool ShowEmpty => ShowResults && Categories.Count == 0;

    public bool HasData => ShowResults && Categories.Count > 0;

    public async Task SetMonthAsync(DateTime month)
    {
        if (IsBusy) return;

        _month = new DateOnly(month.Year, month.Month, 1);
        await LoadAsync();
    }

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
            ErrorMessage = Loc.T("Could not build the report. Try again.");
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    private PeriodReport? _report;
    private Dictionary<Guid, string> _names = new();

    private void Apply(PeriodReport report, Dictionary<Guid, string> names)
    {
        _report = report;
        _names = names;

        CategoryPalette.Assign(names.Values);

        var currency = _settings.Currency;

        TotalText = $"{report.Total:N2} {currency}";

        if (report.ChangePercent is { } percent)
        {
            TotalDeltaText = Loc.F("{0} vs {1:N2} {2}", ReportFormat.Change(percent, report.Total, report.PreviousTotal), report.PreviousTotal, currency);
            TotalDeltaTone = percent > 0 ? "Danger" : percent < 0 ? "Success" : "InkMuted";
        }
        else
        {
            TotalDeltaText = report.PreviousTotal == 0 ? Loc.T("Nothing to compare with") : "";
            TotalDeltaTone = "InkMuted";
        }

        AverageText = report.DaysElapsed == 0 ? "–" : $"{report.AveragePerDay:N2} {currency}";
        AverageCaption = report.DaysElapsed == 0 ? "" : Loc.F("over {0} days", report.DaysElapsed);

        ForecastText = report.Forecast is { } forecast ? $"{forecast:N2} {currency}" : "–";
        ForecastCaption = report.Forecast is null ? "" : Loc.T("at the current pace");

        var top = report.Categories.OrderByDescending(x => x.Total).FirstOrDefault();

        TopCategoryText = top is null ? "–" : NameOf(top.CategoryId);
        TopCategoryCaption = top is null || report.Total <= 0
            ? ""
            : Loc.F("{0:N2} {1} · {2} % of total", top.Total, currency, Math.Round(top.Total / report.Total * 100));

        DonutTotalText = $"{report.Total:N2}";

        BuildRows();

        TopExpenses = report.TopExpenses
            .Take(5)
            .Select((x, index) => new TopExpenseItem(
                index + 1,
                x.Date,
                NameOf(x.CategoryId),
                x.Description,
                x.Amount,
                CategoryPalette.ColorFor(NameOf(x.CategoryId)),
                currency))
            .ToArray();
    }

    private string NameOf(Guid id) => _names.GetValueOrDefault(id, Loc.T("Unknown category"));

    /// <summary>The bars of the table: amounts, or shares of the total, on one scale for all rows.</summary>
    private void BuildRows()
    {
        if (_report is not { } report) return;

        var rows = report.Categories.OrderByDescending(x => x.Total).ToArray();

        double Value(decimal amount, decimal sum) => ShowShare ? (sum > 0 ? (double)(amount / sum) : 0) : (double)amount;

        var scale = rows
            .SelectMany(x => new[] { Value(x.Total, report.Total), Value(x.PreviousTotal, report.PreviousTotal) })
            .DefaultIfEmpty(0)
            .Max();

        Categories = rows
            .Select(x => new CategoryReportItem(
                x.CategoryId,
                NameOf(x.CategoryId),
                x.Total,
                x.PreviousTotal,
                x.ChangePercent,
                scale > 0 ? Value(x.Total, report.Total) / scale : 0,
                scale > 0 ? Value(x.PreviousTotal, report.PreviousTotal) / scale : 0,
                CategoryPalette.ColorFor(NameOf(x.CategoryId)),
                _settings.Currency))
            .ToArray();

        DonutSegments = rows
            .Select(x => new DonutSegment(NameOf(x.CategoryId), (double)x.Total, CategoryPalette.ColorFor(NameOf(x.CategoryId))))
            .ToArray();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Categories));
        OnPropertyChanged(nameof(TopExpenses));
        OnPropertyChanged(nameof(DonutSegments));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(Month));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(TotalDeltaText));
        OnPropertyChanged(nameof(TotalDeltaTone));
        OnPropertyChanged(nameof(AverageText));
        OnPropertyChanged(nameof(AverageCaption));
        OnPropertyChanged(nameof(ForecastTitle));
        OnPropertyChanged(nameof(ForecastText));
        OnPropertyChanged(nameof(ForecastCaption));
        OnPropertyChanged(nameof(TopCategoryText));
        OnPropertyChanged(nameof(TopCategoryCaption));
        OnPropertyChanged(nameof(DonutTotalText));
        OnPropertyChanged(nameof(DonutTotalTitle));
        OnPropertyChanged(nameof(ComparisonTitle));
        OnPropertyChanged(nameof(CurrentMonthName));
        OnPropertyChanged(nameof(PreviousMonthName));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanNavigate));
        OnPropertyChanged(nameof(CanPreviousMonth));
        OnPropertyChanged(nameof(CanNextMonth));
        OnPropertyChanged(nameof(ShowResults));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(HasData));
    }
}
