namespace ExpenseTracker.App.ViewModels;

using System.Globalization;
using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;

public sealed class ExpenseEditorViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExpenseEditorViewModel> _logger;

    private Guid? _expenseId;
    private Expense? _originalExpense;
    private bool _loaded;

    public ExpenseEditorViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        ILogger<ExpenseEditorViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _timeProvider = timeProvider;
        _logger = logger;

        SelectedDate = MaximumDate;
    }

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public Category? SelectedCategory { get; set; }

    public string NewCategoryName { get; set; } = "";

    public string AmountText { get; set; } = "";

    public string Description { get; set; } = "";

    public DateTime? SelectedDate { get; set; }

    public DateTime? MaximumDate => _timeProvider.GetLocalNow().Date;

    public bool IsEditing => _expenseId.HasValue;

    public string PageTitle =>
        IsEditing ? "Edit expense" : "Add expense";

    public string SaveButtonText =>
        IsEditing ? "Save changes" : "Save expense";

    public string SavedMessage =>
        IsEditing ? "Changes saved." : "Expense saved.";

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool IsSaved { get; private set; }

    public string LimitWarning { get; private set; } = "";

    public bool HasLimitWarning => IsSaved && LimitWarning.Length > 0;

    public bool HasError => ErrorMessage.Length > 0;

    public bool CanGoBack => !IsBusy;

    public bool CanReload => !IsBusy && !IsSaved;

    public bool CanEdit => _loaded && CanReload;

    public bool HasFixedDate =>
        _originalExpense?.RecurringExpenseId.HasValue == true;

    public bool CanEditDate => CanEdit && !HasFixedDate;

    public bool ShowAddAnother => IsSaved && !IsEditing;

    public void ConfigureForEdit(Guid expenseId)
    {
        if (expenseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Expense ID must not be empty.",
                nameof(expenseId));
        }

        if (IsBusy || _loaded || _expenseId.HasValue)
        {
            throw new InvalidOperationException(
                "Configure the editor before loading it.");
        }

        _expenseId = expenseId;
        NotifyState();
    }

    public Task<bool> LoadAsync() => RunAsync(async () =>
    {
        _loaded = false;

        var categoryId = SelectedCategory?.Id;
        var loadExpense = IsEditing && _originalExpense is null;

        var result = await Task.Run(async () =>
        {
            await _initializer.InitializeAsync();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var categories = await services
                .GetRequiredService<CategoryService>()
                .GetAllAsync();

            var expense = loadExpense
                ? await services
                    .GetRequiredService<ExpenseService>()
                    .GetByIdAsync(_expenseId!.Value)
                : null;

            return (Categories: categories, Expense: expense);
        });

        if (loadExpense)
        {
            if (result.Expense is null)
            {
                return SetError(
                    "This expense no longer exists. Return to the expense list.");
            }

            _originalExpense = result.Expense;

            AmountText = result.Expense.Amount.ToString(
                "0.00",
                CultureInfo.InvariantCulture);

            Description = result.Expense.Description;
            SelectedDate = result.Expense.Date.ToDateTime(TimeOnly.MinValue);
            categoryId = result.Expense.CategoryId;
        }

        SetCategories(result.Categories, categoryId);
        NotifyInputs();

        _loaded = true;
        return true;
    }, "Could not load the form. Try Reload categories.");

    public async Task<bool> AddCategoryAsync()
    {
        if (!CanEdit) return false;

        var name = NewCategoryName.Trim();

        if (name.Length == 0)
            return SetError("Enter a category name.");

        if (Categories.Any(
                x => x.NormalizedName == name.ToUpperInvariant()))
        {
            return SetError("A category with this name already exists.");
        }

        return await RunAsync(async () =>
        {
            var category = await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                return await scope.ServiceProvider
                    .GetRequiredService<CategoryService>()
                    .AddAsync(name);
            });

            SetCategories(
                Categories
                    .Append(category)
                    .OrderBy(x => x.Name)
                    .ToArray(),
                category.Id);

            NewCategoryName = "";
            OnPropertyChanged(nameof(NewCategoryName));

            return true;
        }, "Could not create the category. Try Reload categories.");
    }

    public async Task<bool> SaveAsync()
    {
        if (!CanEdit) return false;

        if (SelectedCategory is null)
            return SetError("Choose a category.");

        var text = AmountText.Trim().Replace(',', '.');
        var separator = text.IndexOf('.');

        if ((separator >= 0 && text.Length - separator - 1 > 2)
            || !decimal.TryParse(
                text,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount)
            || amount <= 0
            || amount > long.MaxValue / 100m)
        {
            return SetError(
                "Enter a positive amount with up to 2 decimal places, for example 12.50.");
        }

        if (SelectedDate is not { } selectedDate)
            return SetError("Choose a date.");

        var date = DateOnly.FromDateTime(selectedDate);
        var today = DateOnly.FromDateTime(
            _timeProvider.GetLocalNow().DateTime);

        if (date > today)
            return SetError("Expense date cannot be in the future.");

        if (HasFixedDate && date != _originalExpense!.Date)
        {
            return SetError(
                "The date of an automatically generated expense is fixed.");
        }

        var categoryId = SelectedCategory.Id;
        var description = Description.Trim();
        var original = _originalExpense;

        return await RunAsync(async () =>
        {
            var (saved, warning) = await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<ExpenseService>();

                var ok = original is null
                    ? await service.AddAsync(
                        amount,
                        categoryId,
                        date,
                        description) is not null
                    : await service.UpdateAsync(
                        original,
                        amount,
                        categoryId,
                        date,
                        description);

                return (ok, ok
                    ? await GetLimitWarningAsync(scope, categoryId, date)
                    : "");
            });

            if (!saved)
            {
                return SetError(
                    "This expense was changed or deleted. Reopen it from the list to load its current values.");
            }

            LimitWarning = warning;
            IsSaved = true;
            return true;
        }, "Could not save the expense. Check the category and try again.");
    }

    public void StartNewExpense()
    {
        if (IsBusy || !IsSaved || IsEditing) return;

        IsSaved = false;
        LimitWarning = "";
        AmountText = "";
        Description = "";
        NewCategoryName = "";
        ErrorMessage = "";

        NotifyInputs();
        NotifyState();
    }

    private void SetCategories(
        IReadOnlyList<Category> categories,
        Guid? selectedId)
    {
        Categories = categories;
        OnPropertyChanged(nameof(Categories));

        // A Picker may clear its selection when ItemsSource changes.
        SelectedCategory = categories.SingleOrDefault(
            x => x.Id == selectedId);

        OnPropertyChanged(nameof(SelectedCategory));
    }

    private async Task<string> GetLimitWarningAsync(
        AsyncServiceScope scope,
        Guid categoryId,
        DateOnly date)
    {
        // The expense is already saved, so a failed check must not report a save error.
        try
        {
            var status = await scope.ServiceProvider
                .GetRequiredService<BudgetService>()
                .GetStatusAsync(categoryId, date);

            if (status.Limit is not { } limit || status.Overspend <= 0)
                return "";

            return string.Create(
                CultureInfo.InvariantCulture,
                $"Over budget for {date:MMMM yyyy}: spent {status.Spent:0.00} of {limit:0.00} (over by {status.Overspend:0.00}).");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not check the monthly limit.");
            return "";
        }
    }

    private async Task<bool> RunAsync(
        Func<Task<bool>> action,
        string failureMessage)
    {
        if (!CanReload) return false;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Expense editor operation failed.");

            ErrorMessage = failureMessage;
            return false;
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    private bool SetError(string message)
    {
        ErrorMessage = message;
        NotifyState();
        return false;
    }

    private void NotifyInputs()
    {
        OnPropertyChanged(nameof(NewCategoryName));
        OnPropertyChanged(nameof(AmountText));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(MaximumDate));
        OnPropertyChanged(nameof(SelectedDate));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(SavedMessage));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(LimitWarning));
        OnPropertyChanged(nameof(HasLimitWarning));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsSaved));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanReload));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(HasFixedDate));
        OnPropertyChanged(nameof(CanEditDate));
        OnPropertyChanged(nameof(ShowAddAnother));
    }
}