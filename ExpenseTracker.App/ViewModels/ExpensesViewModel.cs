namespace ExpenseTracker.App.ViewModels;

using System.Globalization;
using CLI_Budget_Expense_Tracker.Core.Services;
using CLI_Budget_Expense_Tracker.Infrastructure.Backup;
using CLI_Budget_Expense_Tracker.Infrastructure.Data;
using CLI_Budget_Expense_Tracker.Infrastructure.Exporting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Controls;
using Models;
using Services;

public sealed class ExpensesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ExportLocation _exportLocation;
    private readonly DatabaseBackupService _backups;
    private readonly AppSettings _settings;
    private readonly ILogger<ExpensesViewModel> _logger;

    private DateOnly _month;
    private bool _loaded;
    private decimal _total;
    private decimal? _previousTotal;
    private int _budgetsOver;
    private int _budgetsTotal;
    private decimal _donutTotal;
    private readonly TimeProvider _timeProvider;

    public ExpensesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        ExportLocation exportLocation,
        DatabaseBackupService backups,
        AppSettings settings,
        ILogger<ExpensesViewModel> logger)
    {
        this._settings = settings;
        this._timeProvider = timeProvider;
        this._backups = backups;
        this._exportLocation = exportLocation;
        this._scopeFactory = scopeFactory;
        this._initializer = initializer;
        this._logger = logger;

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        _month = new DateOnly(today.Year, today.Month, 1);

        SelectedPeriod = PeriodOptions[0];
        SelectedSort = SortOptions[0];
        CustomFrom = today.ToDateTime(TimeOnly.MinValue);
        CustomTo = CustomFrom;

        RefreshCommand = new AsyncRelayCommand(
            RefreshAsync,
            () => !IsBusy);

        PreviousMonthCommand = new AsyncRelayCommand(
            () => MoveMonthAsync(-1),
            () => !IsBusy && IsMonthPeriod && _month > DateOnly.MinValue);

        NextMonthCommand = new AsyncRelayCommand(
            () => MoveMonthAsync(1),
            () => !IsBusy && IsMonthPeriod && _month < new DateOnly(9999, 12, 1));
    }

    public IReadOnlyList<ExpenseListItem> Items { get; private set; } = [];

    /// <summary>The same rows grouped by day (only when sorted by date).</summary>
    public IReadOnlyList<ExpenseDayGroup> Groups { get; private set; } = [];

    public IReadOnlyList<BarSegment> Segments { get; private set; } = [];

    public string SpentTitle => IsMonthPeriod ? Loc.F("Spent in {0}", _month.ToString("MMMM")) : Loc.T("Spent");

    public string SpentText => $"{_total:N2} {_settings.Currency}";

    public string DeltaTitle => IsMonthPeriod ? Loc.F("Compared with {0}", _month.AddMonths(-1).ToString("MMMM")) : Loc.T("Compared with last month");

    public string DeltaPercentText
    {
        get
        {
            if (_previousTotal is not { } previous || !IsMonthPeriod) return "–";
            if (previous == 0) return "–";

            var percent = (_total - previous) / previous * 100m;
            return $"{(percent > 0 ? "+" : "")}{percent:0.#}%";
        }
    }

    public string DeltaAmountText
    {
        get
        {
            if (_previousTotal is not { } previous || !IsMonthPeriod) return "";

            var diff = _total - previous;
            var word = diff > 0 ? Loc.T("more") : diff < 0 ? Loc.T("less") : "";

            return $"{(diff > 0 ? "+" : diff < 0 ? "−" : "")}{Math.Abs(diff):N2} {_settings.Currency} {word}".TrimEnd();
        }
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private bool IsCurrentMonth => IsMonthPeriod && _month.Year == Today.Year && _month.Month == Today.Month;

    /// <summary>Average spending per day: over the days passed in this month, or all of a past month.</summary>
    private decimal? AveragePerDay
    {
        get
        {
            if (!IsMonthPeriod) return null;

            var days = IsCurrentMonth ? Today.Day : _month < Today ? DateTime.DaysInMonth(_month.Year, _month.Month) : 0;
            return days > 0 ? _total / days : null;
        }
    }

    public string AveragePerDayText =>
        AveragePerDay is { } average ? Loc.F("Ø {0:N2} {1} per day", average, _settings.Currency) : "";

    public string ForecastTitle => IsMonthPeriod ? Loc.F("Forecast for {0}", _month.ToString("MMMM")) : Loc.T("Forecast");

    public string ForecastText =>
        IsCurrentMonth && AveragePerDay is { } average
            ? $"{average * DateTime.DaysInMonth(_month.Year, _month.Month):N2} {_settings.Currency}"
            : "–";

    public string ForecastCaption => IsCurrentMonth ? Loc.T("at the current pace") : "";

    /// <summary>The month that is open, as a date, for the month picker.</summary>
    public DateTime Month => _month.ToDateTime(TimeOnly.MinValue);

    /// <summary>Shows one category of one month (a click in Reports).</summary>
    public async Task OpenAsync(Guid categoryId, DateTime month)
    {
        if (IsBusy) return;

        _month = new DateOnly(month.Year, month.Month, 1);
        SelectedPeriod = PeriodOptions[0];
        SelectedCategoryFilter = CategoryFilters.FirstOrDefault(x => x.CategoryId == categoryId)
                                 ?? new ExpenseCategoryFilter(categoryId, "");

        OnPropertyChanged(nameof(SelectedPeriod));
        OnPropertyChanged(nameof(SelectedCategoryFilter));

        await RefreshAsync();
    }

    /// <summary>Opens a month chosen by hand; the list goes back to the month view.</summary>
    public async Task SetMonthAsync(DateTime month)
    {
        if (IsBusy) return;

        _month = new DateOnly(month.Year, month.Month, 1);

        if (!IsMonthPeriod)
        {
            SelectedPeriod = PeriodOptions[0];
            OnPropertyChanged(nameof(SelectedPeriod));
        }

        await RefreshAsync();
    }

    public string Subtitle => Loc.F("{0} · {1} entries", PeriodTitle, Items.Count);

    public IReadOnlyList<DonutSegment> DonutSegments { get; private set; } = [];

    public IReadOnlyList<LegendItem> Legend { get; private set; } = [];

    public bool HasLegend => Legend.Count > 0;

    public bool HasNoLegend => Legend.Count == 0;

    public string DonutTotalTitle => Loc.T("Total");

    public string DonutTotalText => $"{_donutTotal:N2}";

    public IReadOnlyList<BudgetBarItem> BudgetBars { get; private set; } = [];

    public bool HasBudgetBars => IsMonthPeriod && BudgetBars.Count > 0;

    public string BudgetsChipText =>
        _budgetsOver > 0 ? Loc.F("{0} of {1} over the limit", _budgetsOver, _budgetsTotal) : Loc.T("All within limits");

    public string BudgetsChipTone => _budgetsOver > 0 ? "Danger" : "Success";

    public bool IsFilterOpen { get; set; }

    /// <summary>How many of the hidden filters are in use: period, amount range, custom dates.</summary>
    public int ActiveFilterCount =>
        (SelectedPeriod.Period != ExpensePeriod.Month ? 1 : 0)
        + (string.IsNullOrWhiteSpace(MinAmountText) ? 0 : 1)
        + (string.IsNullOrWhiteSpace(MaxAmountText) ? 0 : 1);

    public bool HasActiveFilters => ActiveFilterCount > 0;

    public string ActiveFilterCountText => ActiveFilterCount.ToString();

    public void ToggleFilters()
    {
        IsFilterOpen = !IsFilterOpen;
        OnPropertyChanged(nameof(IsFilterOpen));
    }

    /// <summary>Shows only one category (a tap on the chart or the legend); the same category again shows all.</summary>
    public async Task SelectCategoryAsync(Guid categoryId)
    {
        if (IsBusy) return;

        var target = SelectedCategoryFilter?.CategoryId == categoryId
            ? CategoryFilters.FirstOrDefault()
            : CategoryFilters.FirstOrDefault(x => x.CategoryId == categoryId);

        if (target is null) return;

        SelectedCategoryFilter = target;
        OnPropertyChanged(nameof(SelectedCategoryFilter));
        await RefreshAsync();
    }

    /// <summary>Spending went up compared with the previous month.</summary>
    public bool DeltaIsUp => _previousTotal is { } previous && IsMonthPeriod && _total > previous;

    public string BudgetsText => IsMonthPeriod && _budgetsTotal > 0 ? Loc.F("{0} of {1}", _budgetsOver, _budgetsTotal) : "–";

    public string BudgetsCaption =>
        !IsMonthPeriod ? "" : _budgetsTotal == 0 ? Loc.T("no limits set") : Loc.T("over the limit");

    public bool BudgetsAreOver => _budgetsOver > 0;
    
    public IReadOnlyList<ExpenseCategoryFilter> CategoryFilters { get; private set; } = [];

    public ExpenseCategoryFilter? SelectedCategoryFilter { get; set; }

    public IReadOnlyList<PeriodOption> PeriodOptions { get; } =
    [
        new(ExpensePeriod.Month, Loc.T("Month")),
        new(ExpensePeriod.AllTime, Loc.T("All time")),
        new(ExpensePeriod.Custom, Loc.T("Custom range"))
    ];

    public PeriodOption SelectedPeriod { get; set; }

    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new(ExpenseSort.DateNewestFirst, Loc.T("Date: newest first")),
        new(ExpenseSort.DateOldestFirst, Loc.T("Date: oldest first")),
        new(ExpenseSort.AmountHighestFirst, Loc.T("Amount: highest first")),
        new(ExpenseSort.AmountLowestFirst, Loc.T("Amount: lowest first"))
    ];

    public SortOption SelectedSort { get; set; }

    public DateTime? CustomFrom { get; set; }

    public DateTime? CustomTo { get; set; }

    public string SearchText { get; set; } = "";

    public string MinAmountText { get; set; } = "";

    public string MaxAmountText { get; set; } = "";

    public string FilterError { get; private set; } = "";

    public bool HasFilterError => FilterError.Length > 0;

    public bool IsMonthPeriod => SelectedPeriod.Period == ExpensePeriod.Month;

    public bool IsCustomPeriod => SelectedPeriod.Period == ExpensePeriod.Custom;

    public bool CanChangeCategoryFilter => ShowResults;

    public string PeriodTitle => SelectedPeriod.Period switch
    {
        ExpensePeriod.AllTime => Loc.T("All time"),
        ExpensePeriod.Custom when CustomFrom is { } from && CustomTo is { } to =>
            $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}",
        ExpensePeriod.Custom => Loc.T("Custom range"),
        _ => _month.ToString("MMMM yyyy")
    };

    public string Summary =>
        Loc.F("Expenses: {0} · Total: {1:N2} {2}", Items.Count, _total, _settings.Currency);

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

        if (!TryBuildFilter(out var filter)) return;

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
                    new(null, Loc.T("All categories")),
                    .. categories.Select(x =>
                        new ExpenseCategoryFilter(x.Id, x.Name))
                ];

                var selectedFilter = filters.FirstOrDefault(
                    x => x.CategoryId == filter.CategoryId) ?? filters[0];

                var report = await services
                    .GetRequiredService<ExpenseService>()
                    .GetReportAsync(
                        filter.Start,
                        filter.End,
                        selectedFilter.CategoryId);

                var shown = filter.Query.Apply(report.Expenses);

                // The chart shows every category even while one is picked in the list.
                var everything = selectedFilter.CategoryId is null
                    ? report
                    : await services.GetRequiredService<ExpenseService>()
                        .GetReportAsync(filter.Start, filter.End, null);

                var names = categories.ToDictionary(
                    x => x.Id,
                    x => x.Name);

                var byCategory = filter.Query.Apply(everything.Expenses)
                    .GroupBy(x => x.CategoryId)
                    .Select(g => (
                        Id: g.Key,
                        Name: names.GetValueOrDefault(g.Key, Loc.T("Unknown category")),
                        Amount: g.Sum(x => x.Amount)))
                    .OrderByDescending(x => x.Amount)
                    .ToArray();

                IReadOnlyList<(string Name, decimal Spent, decimal Limit)> bars = [];
                decimal? previousTotal = null;
                var budgetsOver = 0;
                var budgetsTotal = 0;

                if (filter.IsMonth && filter.Start is { } monthStart)
                {
                    var previousStart = monthStart.AddMonths(-1);
                    var previousEnd = monthStart.AddDays(-1);

                    var previous = await services
                        .GetRequiredService<ExpenseService>()
                        .GetReportAsync(previousStart, previousEnd, selectedFilter.CategoryId);

                    previousTotal = filter.Query.Apply(previous.Expenses).Sum(x => x.Amount);

                    var statuses = await services
                        .GetRequiredService<BudgetService>()
                        .GetMonthlyStatusesAsync(monthStart);

                    budgetsTotal = statuses.Count;
                    budgetsOver = statuses.Count(x => x.Overspend > 0);

                    bars = statuses
                        .Where(x => x.Limit is > 0)
                        .OrderByDescending(x => x.Spent / x.Limit!.Value)
                        .Take(3)
                        .Select(x => (
                            names.GetValueOrDefault(x.CategoryId, Loc.T("Unknown category")),
                            x.Spent,
                            x.Limit!.Value))
                        .ToArray();
                }

                IReadOnlyList<ExpenseListItem> rows = shown
                    .Select(x => new ExpenseListItem(
                        x.Id,
                        x.Date,
                        x.Amount,
                        names.GetValueOrDefault(
                            x.CategoryId, Loc.T("Unknown category")),
                        x.Description,
                        _settings.Currency))
                    .ToArray();

                return (
                    Rows: rows,
                    Total: shown.Sum(x => x.Amount),
                    Filters: filters,
                    SelectedFilter: selectedFilter,
                    PreviousTotal: previousTotal,
                    BudgetsOver: budgetsOver,
                    BudgetsTotal: budgetsTotal,
                    ByCategory: byCategory,
                    Bars: bars);
            });

            // Colors come from the theme, which belongs to the UI thread.
            CategoryPalette.Assign(result.Filters
                .Where(x => x.CategoryId is not null)
                .Select(x => x.Name));
            Items = result.Rows
                .Select(x => x with { CategoryColor = CategoryPalette.ColorFor(x.CategoryName) })
                .ToArray();
            _total = result.Total;
            _previousTotal = result.PreviousTotal;
            _budgetsOver = result.BudgetsOver;
            _budgetsTotal = result.BudgetsTotal;

            _donutTotal = result.ByCategory.Sum(x => x.Amount);
            DonutSegments = result.ByCategory
                .Select(x => new DonutSegment(x.Name, (double)x.Amount, CategoryPalette.ColorFor(x.Name)))
                .ToArray();

            var picked = result.SelectedFilter.CategoryId;
            Legend = result.ByCategory
                .Select(x => new LegendItem(
                    x.Id,
                    x.Name,
                    $"{x.Amount:N2} {_settings.Currency}",
                    _donutTotal > 0 ? $"{Math.Round(x.Amount / _donutTotal * 100):0} %" : "",
                    CategoryPalette.ColorFor(x.Name),
                    picked == x.Id))
                .ToArray();

            BudgetBars = result.Bars
                .Select(x =>
                {
                    var ratio = x.Limit > 0 ? (double)(x.Spent / x.Limit) : 0;
                    var key = ratio > 1 ? "Danger" : ratio >= 0.9 ? "Warning" : "Success";

                    return new BudgetBarItem(x.Name, $"{Math.Round(ratio * 100):0} %", Math.Min(ratio, 1), key, key);
                })
                .ToArray();

            BuildGroups();

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
                "Failed to load expenses for {Period} and category {CategoryId}.",
                filter.Title,
                filter.CategoryId);

            ErrorMessage = Loc.T("Could not load expenses. Try Refresh.");
            _loaded = false;
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    /// <summary>Puts the search, amount range, sort order, period and category back to their defaults.</summary>
    public void ResetFilters()
    {
        if (IsBusy) return;

        SearchText = "";
        MinAmountText = "";
        MaxAmountText = "";
        SelectedSort = SortOptions[0];
        SelectedPeriod = PeriodOptions[0];
        SelectedCategoryFilter = CategoryFilters.FirstOrDefault();
        FilterError = "";

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(MinAmountText));
        OnPropertyChanged(nameof(MaxAmountText));
        OnPropertyChanged(nameof(SelectedSort));
        OnPropertyChanged(nameof(SelectedPeriod));
        OnPropertyChanged(nameof(SelectedCategoryFilter));

        NotifyState();
    }

    private sealed record ViewFilter(
        DateOnly? Start,
        DateOnly? End,
        Guid? CategoryId,
        ExpenseListQuery Query,
        string Title,
        string FileLabel)
    {
        public bool IsMonth { get; init; }

        public bool IsNarrowed =>
            !string.IsNullOrWhiteSpace(Query.Text)
            || Query.MinAmount is not null
            || Query.MaxAmount is not null;
    }

    /// <summary>Reads the filter inputs. On bad input it shows a message and returns false.</summary>
    private bool TryBuildFilter(out ViewFilter filter)
    {
        filter = null!;

        DateOnly? start = null;
        DateOnly? end = null;
        string title;
        string fileLabel;

        switch (SelectedPeriod.Period)
        {
            case ExpensePeriod.AllTime:
                title = "All expenses";
                fileLabel = "all";
                break;

            case ExpensePeriod.Custom:
                if (CustomFrom is not { } from || CustomTo is not { } to)
                {
                    ShowFilterError(Loc.T("Choose both dates."));
                    return false;
                }

                start = DateOnly.FromDateTime(from);
                end = DateOnly.FromDateTime(to);

                if (end < start)
                {
                    ShowFilterError(Loc.T("The end date cannot be earlier than the start date."));
                    return false;
                }

                title = $"{Loc.T("Expenses")} {start:dd.MM.yyyy} – {end:dd.MM.yyyy}";
                fileLabel = $"{start:yyyyMMdd}-{end:yyyyMMdd}";
                break;

            default:
                start = _month;
                end = new DateOnly(
                    _month.Year,
                    _month.Month,
                    DateTime.DaysInMonth(_month.Year, _month.Month));

                title = $"{Loc.T("Expenses")} {_month:MMMM yyyy}";
                fileLabel = $"{_month:yyyy-MM}";
                break;
        }

        if (!TryParseAmount(MinAmountText, out var min)
            || !TryParseAmount(MaxAmountText, out var max))
        {
            ShowFilterError(Loc.T("Enter amounts as numbers, for example 12.50."));
            return false;
        }

        if (min is { } lowest && max is { } highest && highest < lowest)
        {
            ShowFilterError(Loc.T("The maximum amount cannot be less than the minimum."));
            return false;
        }

        filter = new ViewFilter(
            start,
            end,
            SelectedCategoryFilter?.CategoryId,
            new ExpenseListQuery(SearchText, min, max, SelectedSort.Sort),
            title,
            fileLabel)
        {
            IsMonth = SelectedPeriod.Period == ExpensePeriod.Month
        };

        FilterError = "";
        OnPropertyChanged(nameof(FilterError));
        OnPropertyChanged(nameof(HasFilterError));

        return true;
    }

    private static bool TryParseAmount(string? text, out decimal? amount)
    {
        amount = null;

        var value = (text ?? "").Trim().Replace(',', '.');

        if (value.Length == 0) return true;

        if (!decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var parsed)
            || parsed < 0)
        {
            return false;
        }

        amount = parsed;
        return true;
    }

    private void ShowFilterError(string message)
    {
        FilterError = message;
        OnPropertyChanged(nameof(FilterError));
        OnPropertyChanged(nameof(HasFilterError));
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
            BuildGroups();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete expense {ExpenseId}.",
                id);

            ErrorMessage = Loc.T("Could not delete the expense. Try Refresh.");
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    /// <summary>Exports the expenses currently shown, with the same filters. Returns the file path, or null on failure.</summary>
    public async Task<string?> ExportAsync(ExportFormat format)
    {
        if (IsBusy || !_loaded) return null;

        if (!TryBuildFilter(out var filter)) return null;

        var categoryName = SelectedCategoryFilter?.CategoryId is null
            ? null
            : SelectedCategoryFilter.Name;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            return await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var services = scope.ServiceProvider;

                var categories = await services
                    .GetRequiredService<CategoryService>()
                    .GetAllAsync();

                var report = await services
                    .GetRequiredService<ExpenseService>()
                    .GetReportAsync(filter.Start, filter.End, filter.CategoryId);

                var shown = filter.Query.Apply(report.Expenses);

                var names = categories.ToDictionary(x => x.Id, x => x.Name);
                var baseName = $"expenses-{filter.FileLabel}"
                               + (categoryName is null ? "" : "-" + SafeFileName(categoryName))
                               + (filter.IsNarrowed ? "-filtered" : "");

                if (format == ExportFormat.Csv)
                {
                    var path = Path.Combine(_exportLocation.Directory, baseName + ".csv");
                    new CsvExpenseExporter().Export(shown, names, path);
                    return path;
                }
                else
                {
                    var path = Path.Combine(_exportLocation.Directory, baseName + ".md");
                    var title = filter.Title
                                + (categoryName is null ? "" : $" · {categoryName}")
                                + (filter.IsNarrowed ? Loc.T(" (filtered)") : "");
                    new MarkdownExpenseExporter().Export(shown, names, title, path);
                    return path;
                }
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to export expenses for {Period} and category {CategoryId}.",
                filter.Title,
                filter.CategoryId);

            ErrorMessage = Loc.T("Could not export expenses.");
            return null;
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    /// <summary>Creates a database backup. Returns its path, or null on failure.</summary>
    public async Task<string?> CreateBackupAsync()
    {
        if (IsBusy) return null;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            return await Task.Run(() => _backups.CreateBackup());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create a database backup.");

            ErrorMessage = Loc.T("Could not create the backup.");
            return null;
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    public IReadOnlyList<BackupInfo> GetBackups() => _backups.List();

    /// <summary>Replaces the database with a backup and reloads the list.</summary>
    public async Task<bool> RestoreBackupAsync(string fileName)
    {
        if (IsBusy) return false;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        var restored = false;

        try
        {
            await Task.Run(async () =>
            {
                _backups.Restore(fileName);

                // An older backup may need newer migrations.
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<DatabaseInitializer>()
                    .InitializeAsync();
            });

            restored = true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to restore backup {FileName}.", fileName);

            ErrorMessage = Loc.T("Could not restore the backup. The current data was not changed.");
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }

        if (restored) await RefreshAsync();

        return restored;
    }

    private static string SafeFileName(string value) =>
        string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    private async Task MoveMonthAsync(int offset)
    {
        if (IsBusy) return;

        _month = _month.AddMonths(offset);
        await RefreshAsync();
    }

    private void BuildGroups()
    {
        Segments = Items
            .GroupBy(x => x.CategoryName)
            .Select(g => new BarSegment(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Value)
            .ToArray();

        var byDate = SelectedSort.Sort is ExpenseSort.DateNewestFirst or ExpenseSort.DateOldestFirst;

        Groups = byDate
            ? Items
                .GroupBy(x => x.Date)
                .Select(g => new ExpenseDayGroup(g.Key.ToString("ddd, d MMMM"), g, _settings.Currency))
                .ToArray()
            : [new ExpenseDayGroup("", Items, _settings.Currency)];
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(Segments));
        OnPropertyChanged(nameof(SpentTitle));
        OnPropertyChanged(nameof(SpentText));
        OnPropertyChanged(nameof(DeltaTitle));
        OnPropertyChanged(nameof(DeltaPercentText));
        OnPropertyChanged(nameof(DeltaAmountText));
        OnPropertyChanged(nameof(DeltaIsUp));
        OnPropertyChanged(nameof(BudgetsText));
        OnPropertyChanged(nameof(BudgetsCaption));
        OnPropertyChanged(nameof(BudgetsAreOver));
        OnPropertyChanged(nameof(AveragePerDayText));
        OnPropertyChanged(nameof(ForecastTitle));
        OnPropertyChanged(nameof(ForecastText));
        OnPropertyChanged(nameof(ForecastCaption));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(Month));
        OnPropertyChanged(nameof(DonutSegments));
        OnPropertyChanged(nameof(Legend));
        OnPropertyChanged(nameof(HasLegend));
        OnPropertyChanged(nameof(HasNoLegend));
        OnPropertyChanged(nameof(DonutTotalTitle));
        OnPropertyChanged(nameof(DonutTotalText));
        OnPropertyChanged(nameof(BudgetBars));
        OnPropertyChanged(nameof(HasBudgetBars));
        OnPropertyChanged(nameof(BudgetsChipText));
        OnPropertyChanged(nameof(BudgetsChipTone));
        OnPropertyChanged(nameof(ActiveFilterCount));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ActiveFilterCountText));
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(PeriodTitle));
        OnPropertyChanged(nameof(IsMonthPeriod));
        OnPropertyChanged(nameof(IsCustomPeriod));
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