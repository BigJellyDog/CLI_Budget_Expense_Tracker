namespace ExpenseTracker.App.ViewModels;

using System.Globalization;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;
using Category = CLI_Budget_Expense_Tracker.Core.Models.Category;

public sealed class BudgetsViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<BudgetsViewModel> _logger;

    private DateOnly _month;
    private readonly AppSettings _settings;

    private Category? _selectedCategory;
    private bool _loaded;

    public BudgetsViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        AppSettings settings,
        ILogger<BudgetsViewModel> logger)
    {
        _settings = settings;
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        _month = new DateOnly(today.Year, today.Month, 1);
    }

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public IReadOnlyList<BudgetListItem> Items { get; private set; } = [];

    public string MonthTitle => _month.ToString("MMMM yyyy");

    public string LimitText { get; set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasSuccess => SuccessMessage.Length > 0;

    public bool CanGoBack => !IsBusy;

    public bool CanRefresh => !IsBusy;

    public bool CanEdit => _loaded && !IsBusy;

    public bool CanSave => CanEdit && SelectedCategory is not null;

    public bool ShowResults => _loaded && !IsBusy;

    public bool ShowNoCategories =>
        ShowResults && Categories.Count == 0;

    public bool CanPreviousMonth =>
        !IsBusy && _month > DateOnly.MinValue;

    public bool CanNextMonth =>
        !IsBusy && _month < new DateOnly(9999, 12, 1);

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value) || IsBusy)
                return;

            UseSelectedLimit();
            ErrorMessage = "";
            SuccessMessage = "";
            NotifyState();
        }
    }

    public Task LoadAsync() => RunAsync(
        ReloadAsync,
        () => Loc.T("Could not load budgets. Try Refresh."));

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

    public void SelectBudget(BudgetListItem item)
    {
        if (!CanEdit) return;

        SelectedCategory = Categories.SingleOrDefault(
            x => x.Id == item.CategoryId);

        UseSelectedLimit();
        ErrorMessage = "";
        SuccessMessage = "";
        NotifyState();
    }

    public async Task SaveAsync()
    {
        if (!CanSave || SelectedCategory is not { } category)
            return;

        var text = (LimitText ?? "").Trim().Replace(',', '.');
        var separator = text.IndexOf('.');

        if ((separator >= 0 && text.Length - separator - 1 > 2)
            || !decimal.TryParse(
                text,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount)
            || amount < 0
            || amount > long.MaxValue / 100m)
        {
            ErrorMessage =
                Loc.T("Enter an amount of 0 or more with up to 2 decimal places.");

            SuccessMessage = "";
            NotifyState();
            return;
        }

        var month = _month;
        var saved = false;

        await RunAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<BudgetService>()
                    .SetMonthlyLimitAsync(category.Id, month, amount);
            });

            saved = true;
            SuccessMessage = Loc.T("Budget saved.");

            await ReloadAsync();
        }, () => saved
            ? Loc.T("The budget was saved, but the list could not be reloaded. Click Refresh.")
            : Loc.T("Could not save the budget. Click Refresh and try again."));
    }

    public async Task RemoveLimitAsync(
        Guid categoryId,
        Func<Task<bool>> confirmRemoval)
    {
        if (!CanEdit || Items.All(x => x.CategoryId != categoryId))
            return;

        var month = _month;

        await RunAsync(async () =>
        {
            // Keep the selected month fixed while confirmation is open.
            if (!await confirmRemoval())
                return;

            var removed = await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                return await scope.ServiceProvider
                    .GetRequiredService<BudgetService>()
                    .DeleteMonthlyLimitAsync(categoryId, month);
            });

            Items = Items
                .Where(x => x.CategoryId != categoryId)
                .ToArray();

            if (SelectedCategory?.Id == categoryId)
                UseSelectedLimit();

            SuccessMessage = removed
                ? Loc.T("Budget limit removed.")
                : Loc.T("The limit was already removed. The list has been updated.");
        }, () => Loc.T("Could not remove the budget limit. Try Refresh."));
    }
    
    private async Task ReloadAsync()
    {
        _loaded = false;

        var categoryId = SelectedCategory?.Id;
        var month = _month;

        var result = await Task.Run(async () =>
        {
            await _initializer.InitializeAsync();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var categories = await services
                .GetRequiredService<CategoryService>()
                .GetAllAsync();

            var statuses = await services
                .GetRequiredService<BudgetService>()
                .GetMonthlyStatusesAsync(month);

            var names = categories.ToDictionary(
                x => x.Id,
                x => x.Name);

            IReadOnlyList<BudgetListItem> rows = statuses
                .Where(x => x.Limit.HasValue)
                .Select(x => new BudgetListItem(
                    x.CategoryId,
                    names.GetValueOrDefault(
                        x.CategoryId, Loc.T("Unknown category")),
                    x.Limit!.Value,
                    x.Spent,
                    _settings.Currency))
                .OrderBy(x => x.CategoryName)
                .ToArray();

            return (Categories: categories, Items: rows);
        });

        Items = result.Items;
        Categories = result.Categories;
        OnPropertyChanged(nameof(Categories));

        // Restore selection after the Picker receives its new ItemsSource.
        SelectedCategory = Categories.SingleOrDefault(
            x => x.Id == categoryId);

        UseSelectedLimit();
        _loaded = true;
    }

    private void UseSelectedLimit()
    {
        var item = Items.SingleOrDefault(
            x => x.CategoryId == SelectedCategory?.Id);

        LimitText = item?.Limit.ToString(
            "0.00",
            CultureInfo.InvariantCulture) ?? "";

        OnPropertyChanged(nameof(LimitText));
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
                "Budget operation failed.");

            ErrorMessage = failureMessage();
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ShowResults));
        OnPropertyChanged(nameof(ShowNoCategories));
        OnPropertyChanged(nameof(CanPreviousMonth));
        OnPropertyChanged(nameof(CanNextMonth));
    }
}