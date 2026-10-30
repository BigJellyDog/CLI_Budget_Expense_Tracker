namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CLI_Budget_Expense_Tracker.Infrastructure.Backup;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Controls;
using Models;
using Services;

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly DatabaseBackupService _backups;
    private readonly ILogger<CategoriesViewModel> _logger;

    private readonly AppSettings _settings;

    private Guid? _editingCategoryId;
    private Guid? _selectedId;
    private DateOnly _month;
    private string _searchText = "";
    private bool _loaded;
    private Dictionary<Guid, (int Count, decimal Amount)> _stats = new();
    private Dictionary<Guid, decimal> _limits = new();
    private Dictionary<Guid, int> _ruleCounts = new();

    public CategoriesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        DatabaseBackupService backups,
        TimeProvider timeProvider,
        AppSettings settings,
        ILogger<CategoriesViewModel> logger)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        _month = new DateOnly(today.Year, today.Month, 1);
        _settings = settings;
        _backups = backups;
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;
    }

    public IReadOnlyList<Category> Items { get; private set; } = [];

    public IReadOnlyList<CategoryTile> Tiles { get; private set; } = [];

    /// <summary>Categories with spending in the month that match the search.</summary>
    public IReadOnlyList<CategoryTile> UsedRows { get; private set; } = [];

    /// <summary>Categories without spending in the month that match the search.</summary>
    public IReadOnlyList<CategoryTile> UnusedRows { get; private set; } = [];

    public IReadOnlyList<SwatchItem> Swatches { get; private set; } = [];

    /// <summary>The palette slot of the category being created or renamed, or -1 for a custom color.</summary>
    public int SelectedSlot { get; private set; }

    /// <summary>The custom color of the category being created or renamed, if any.</summary>
    public Color? CustomColor { get; private set; }

    public CategoryTile? Selected { get; private set; }

    public bool HasSelection => Selected is not null;

    public bool IsEmpty => ShowResults && Items.Count == 0;

    public bool HasUsed => UsedRows.Count > 0;

    public bool HasUnused => UnusedRows.Count > 0;

    public bool NothingFound => ShowResults && Items.Count > 0 && UsedRows.Count + UnusedRows.Count == 0;

    public string UsedCaption => Loc.F("Used this month · {0}", UsedRows.Count);

    public string UnusedCaption => Loc.F("Unused · {0}", UnusedRows.Count);

    public string SearchPlaceholder => Loc.F("Search {0} categories", Items.Count);

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value ?? "")) return;

            BuildTiles();
            NotifyLists();
        }
    }

    public DateTime Month => _month.ToDateTime(TimeOnly.MinValue);

    public string MonthTitle => _month.ToString("MMMM yyyy");

    public bool CanPreviousMonth => !IsBusy && _month > DateOnly.MinValue;

    public bool CanNextMonth => !IsBusy && _month < new DateOnly(9999, 12, 1);

    public string Subtitle => Loc.F("{0} categories · {1} in {2}",
        Items.Count, $"{_stats.Values.Sum(x => x.Amount):N2} {_settings.Currency}".TrimEnd(), _month.ToString("MMMM"));

    public string NameText { get; set; } = ""; 

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasSuccess => SuccessMessage.Length > 0;

    public bool CanRefresh => !IsBusy;

    public bool CanGoBack => !IsBusy;

    public bool CanEdit => _loaded && !IsBusy;

    public bool ShowResults => _loaded && !IsBusy;

    public Task LoadAsync() => RunAsync(async () =>
    {
        _loaded = false;
        Items = await ReadCategoriesAsync();
        _loaded = true;

        if (_editingCategoryId is null)
        {
            CustomColor = null;
            SelectedSlot = CategoryPalette.LeastUsedSlot(Items.Select(x => x.Name));
        }

        if (_editingCategoryId is { } id
            && Items.All(x => x.Id != id))
        {
            _editingCategoryId = null;
            NameText = "";

            ErrorMessage =
                Loc.T("The category being edited no longer exists.");
        }
    }, () => Loc.T("Could not load categories. Try Refresh."));

    public void StartRename(Category category)
    {
        if (!CanEdit) return;

        _editingCategoryId = category.Id;
        NameText = category.Name;
        CustomColor = CategoryPalette.CustomColorOf(category.Name);
        SelectedSlot = CustomColor is null ? CategoryPalette.SlotOf(category.Name) : -1;
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyState();
    }

    public void CancelEdit()
    {
        if (IsBusy) return;

        _editingCategoryId = null;
        NameText = "";
        CustomColor = null;
        SelectedSlot = CategoryPalette.LeastUsedSlot(Items.Select(x => x.Name));
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyState();
    }

    public void Select(Guid categoryId)
    {
        if (IsBusy) return;

        _selectedId = categoryId;
        BuildTiles();
        NotifyLists();
    }

    /// <summary>Gives the selected category a color of the palette.</summary>
    public void SetSelectedColor(int slot)
    {
        if (!CanEdit || Selected is not { } tile) return;

        SaveSelectedColor(() => _settings.SetCategoryColor(tile.Name, slot));
    }

    /// <summary>Gives the selected category a color of the person's own.</summary>
    public void SetSelectedCustomColor(Color color)
    {
        if (!CanEdit || Selected is not { } tile) return;

        SaveSelectedColor(() => _settings.SetCategoryCustomColor(tile.Name, ToHex(color)));
    }

    public async Task SetMonthAsync(DateTime month)
    {
        if (IsBusy) return;

        _month = new DateOnly(month.Year, month.Month, 1);
        await LoadAsync();
    }

    public async Task MoveMonthAsync(int offset)
    {
        if ((offset < 0 && !CanPreviousMonth) || (offset > 0 && !CanNextMonth)) return;

        _month = _month.AddMonths(offset);
        await LoadAsync();
    }

    private void SaveSelectedColor(Action save)
    {
        try
        {
            save();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not save the color of a category.");
            ShowError(Loc.T("Could not save the color."));
            return;
        }

        NotifyState();
    }

    private static string ToHex(Color color) =>
        $"#{(int)Math.Round(color.Red * 255):X2}{(int)Math.Round(color.Green * 255):X2}{(int)Math.Round(color.Blue * 255):X2}";

    public async Task SaveAsync()
    {
        if (!CanEdit) return;

        var name = (NameText ?? "").Trim();

        if (name.Length == 0)
        {
            ShowError(Loc.T("Enter a category name."));
            return;
        }

        var categoryId = _editingCategoryId;
        var normalizedName = name.ToUpperInvariant();

        if (Items.Any(x =>
                x.Id != categoryId
                && x.NormalizedName == normalizedName))
        {
            ShowError(Loc.T("A category with this name already exists."));
            return;
        }

        var saved = false;

        await RunAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<CategoryService>();

                if (categoryId is { } id)
                {
                    await service.RenameAsync(id, name);
                }
                else
                {
                    await service.AddAsync(name);
                }
            });

            saved = true;
            SaveColor(categoryId, name);
            _editingCategoryId = null;
            NameText = "";

            SuccessMessage = categoryId.HasValue
                ? Loc.T("Category renamed.")
                : Loc.T("Category created.");

            // Saving succeeded; a reload failure must not look like a save failure.
            _loaded = false;
            Items = await ReadCategoriesAsync();
            _loaded = true;
            _selectedId = Items.FirstOrDefault(x => x.NormalizedName == normalizedName)?.Id ?? _selectedId;
            CustomColor = null;
            SelectedSlot = CategoryPalette.LeastUsedSlot(Items.Select(x => x.Name));
        }, () => saved
            ? Loc.T("The category was saved, but the list could not be reloaded. Click Refresh.")
            : Loc.T("Could not save the category. Click Refresh and try again."));
    }

    public async Task DeleteAsync(
        Guid categoryId,
        Func<Task<bool>> confirmDeletion)
    {
        if (!CanEdit) return;

        await RunAsync(async () =>
        {
            // Keep other actions disabled while confirmation is open.
            if (!await confirmDeletion())
                return;

            bool deleted;

            try
            {
                deleted = await Task.Run(async () =>
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    return await scope.ServiceProvider
                        .GetRequiredService<CategoryService>()
                        .DeleteAsync(categoryId);
                });
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Deletion was rejected for category {CategoryId}.",
                    categoryId);

                ErrorMessage = exception.Message;
                return;
            }

            Items = Items
                .Where(x => x.Id != categoryId)
                .ToArray();

            if (_editingCategoryId == categoryId)
            {
                _editingCategoryId = null;
                NameText = "";
            }

            SuccessMessage = deleted
                ? Loc.T("Category deleted.")
                : Loc.T("The category was already deleted. The list has been updated.");
        }, () => Loc.T("Could not delete the category. Try Refresh."));
    }
    
    /// <summary>Counts the records that use a category. Returns null if it could not be read.</summary>
    public async Task<CategoryUsage?> GetUsageAsync(Guid categoryId)
    {
        if (!CanEdit) return null;

        try
        {
            return await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                return await scope.ServiceProvider
                    .GetRequiredService<CategoryService>()
                    .GetUsageAsync(categoryId);
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to read usage of category {CategoryId}.",
                categoryId);

            ShowError(Loc.T("Could not check where the category is used. Try Refresh."));
            return null;
        }
    }

    public async Task MoveAndDeleteAsync(
        Guid categoryId,
        Guid targetCategoryId,
        Func<Task<bool>> confirmMove)
    {
        if (!CanEdit) return;

        await RunAsync(async () =>
        {
            if (!await confirmMove())
                return;

            CategoryUsage moved;

            try
            {
                moved = await Task.Run(async () =>
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    return await scope.ServiceProvider
                        .GetRequiredService<CategoryService>()
                        .MoveAndDeleteAsync(categoryId, targetCategoryId);
                });
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Moving and deleting was rejected for category {CategoryId}.",
                    categoryId);

                ErrorMessage = exception.Message;
                return;
            }

            Items = Items
                .Where(x => x.Id != categoryId)
                .ToArray();

            if (_editingCategoryId == categoryId)
            {
                _editingCategoryId = null;
                NameText = "";
            }

            SuccessMessage =
                Loc.F("Category deleted. Moved: {0} expenses, {1} schedules. Budget limits removed: {2}.",
                    moved.ActiveExpenses + moved.DeletedExpenses, moved.RecurringExpenses, moved.BudgetLimits);
        }, () => Loc.T("Could not move and delete the category. Nothing was changed. Try Refresh."));
    }

    public async Task DeleteWithDataAsync(
        Guid categoryId,
        Func<Task<bool>> confirmDeletion)
    {
        if (!CanEdit) return;

        await RunAsync(async () =>
        {
            if (!await confirmDeletion())
                return;

            CategoryUsage deleted;

            try
            {
                deleted = await Task.Run(async () =>
                {
                    // Safety copy, so this permanent delete can be undone with Restore.
                    _backups.CreateBackup(DatabaseBackupService.AutomaticPrefix);

                    await using var scope = _scopeFactory.CreateAsyncScope();

                    return await scope.ServiceProvider
                        .GetRequiredService<CategoryService>()
                        .DeleteWithDataAsync(categoryId);
                });
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Deleting category {CategoryId} with its data was rejected.",
                    categoryId);

                ErrorMessage = exception.Message;
                return;
            }

            Items = Items
                .Where(x => x.Id != categoryId)
                .ToArray();

            if (_editingCategoryId == categoryId)
            {
                _editingCategoryId = null;
                NameText = "";
            }

            SuccessMessage =
                Loc.F("Category deleted with {0} expenses, {1} schedules and {2} budget limits. A backup was saved first.",
                    deleted.ActiveExpenses + deleted.DeletedExpenses, deleted.RecurringExpenses, deleted.BudgetLimits);
        }, () => Loc.T("Could not delete the category. Nothing was changed. Try Refresh."));
    }

    /// <summary>Loads the categories and, for the month, what was spent in each, their limits and rules.</summary>
    private async Task<IReadOnlyList<Category>> ReadCategoriesAsync()
    {
        var start = _month;
        var end = new DateOnly(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month));

        var result = await Task.Run(async () =>
        {
            await _initializer.InitializeAsync();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var categories = await services.GetRequiredService<CategoryService>().GetAllAsync();

            var report = await services
                .GetRequiredService<ExpenseService>()
                .GetReportAsync(start, end, null);

            var stats = report.Expenses
                .GroupBy(x => x.CategoryId)
                .ToDictionary(g => g.Key, g => (g.Count(), g.Sum(x => x.Amount)));

            var limits = (await services.GetRequiredService<BudgetService>().GetMonthlyStatusesAsync(start))
                .Where(x => x.Limit.HasValue)
                .ToDictionary(x => x.CategoryId, x => x.Limit!.Value);

            var rules = (await services.GetRequiredService<CategoryRuleService>().GetAllAsync())
                .GroupBy(x => x.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            return (Categories: categories, Stats: stats, Limits: limits, Rules: rules);
        });

        _stats = result.Stats;
        _limits = result.Limits;
        _ruleCounts = result.Rules;

        // Colors come from the theme, which belongs to the UI thread.
        CategoryPalette.Assign(result.Categories.Select(x => x.Name));

        return result.Categories;
    }

    /// <summary>Keeps the color picked in the form with the category, also across a rename.</summary>
    private void SaveColor(Guid? categoryId, string name)
    {
        try
        {
            if (categoryId is { } id && Items.FirstOrDefault(x => x.Id == id) is { } old)
                _settings.RenameCategoryColor(old.Name, name);

            if (SelectedSlot < 0 && CustomColor is { } custom)
                _settings.SetCategoryCustomColor(name, ToHex(custom));
            else
                _settings.SetCategoryColor(name, Math.Max(0, SelectedSlot));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not save the color of a category.");
        }
    }

    private void BuildTiles()
    {
        var total = _stats.Values.Sum(x => x.Amount);

        if (_selectedId is null || Items.All(x => x.Id != _selectedId))
            _selectedId = Items.OrderByDescending(x => _stats.GetValueOrDefault(x.Id).Amount).FirstOrDefault()?.Id;

        Tiles = Items
            .Select(c =>
            {
                var (count, amount) = _stats.GetValueOrDefault(c.Id);

                return new CategoryTile(
                    c,
                    count,
                    amount,
                    total > 0 ? amount / total : 0,
                    _settings.Currency,
                    CategoryPalette.ColorFor(c.Name),
                    _limits.TryGetValue(c.Id, out var limit) ? limit : null,
                    _ruleCounts.GetValueOrDefault(c.Id),
                    c.Id == _selectedId);
            })
            .ToArray();

        Selected = Tiles.FirstOrDefault(x => x.IsSelected);

        var query = SearchText.Trim();
        var shown = Tiles
            .Where(x => query.Length == 0 || x.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();

        UsedRows = shown.Where(x => !x.IsUnused).OrderByDescending(x => x.Amount).ThenBy(x => x.Name).ToArray();
        UnusedRows = shown.Where(x => x.IsUnused).OrderBy(x => x.Name).ToArray();

        var current = Selected is null
            ? -2
            : CategoryPalette.CustomColorOf(Selected.Name) is null ? CategoryPalette.SlotOf(Selected.Name) : -1;

        Swatches =
        [
            .. Enumerable.Range(0, CategoryPalette.Count)
                .Select(slot => new SwatchItem(slot, CategoryPalette.ColorOfSlot(slot), slot == current)),
            new SwatchItem(
                -1,
                Selected is null ? Colors.Gray : CategoryPalette.CustomColorOf(Selected.Name) ?? Colors.Gray,
                current == -1,
                IsCustom: true,
                ShowPlus: current != -1)
        ];
    }

    private async Task RunAsync(
        Func<Task> action,
        Func<string> failureMessage)
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = "";
        SuccessMessage = "";
        NotifyState();

        try
        {
            await action();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Category operation failed.");

            ErrorMessage = failureMessage();
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = "";
        NotifyState();
    }

    private void NotifyState()
    {
        BuildTiles();

        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(NameText));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(Month));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(CanPreviousMonth));
        OnPropertyChanged(nameof(CanNextMonth));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(ShowResults));
        NotifyLists();
    }

    private void NotifyLists()
    {
        OnPropertyChanged(nameof(Tiles));
        OnPropertyChanged(nameof(UsedRows));
        OnPropertyChanged(nameof(UnusedRows));
        OnPropertyChanged(nameof(UsedCaption));
        OnPropertyChanged(nameof(UnusedCaption));
        OnPropertyChanged(nameof(HasUsed));
        OnPropertyChanged(nameof(HasUnused));
        OnPropertyChanged(nameof(NothingFound));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(Selected));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(Swatches));
    }
}
