namespace ExpenseTracker.App.ViewModels;

using System.Globalization;
using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;

public sealed class AddExpenseViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AddExpenseViewModel> _logger;

    private bool _loaded;

    public AddExpenseViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        ILogger<AddExpenseViewModel> logger)
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

    public string ErrorMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool IsSaved { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool CanReload => !IsBusy && !IsSaved;

    public bool CanEdit => _loaded && CanReload;

    public Task<bool> LoadAsync() => RunAsync(async () =>
    {
        _loaded = false;
        var selectedId = SelectedCategory?.Id;

        Categories = await Task.Run(async () =>
        {
            await _initializer.InitializeAsync();

            await using var scope = _scopeFactory.CreateAsyncScope();

            return await scope.ServiceProvider
                .GetRequiredService<CategoryService>()
                .GetAllAsync();
        });

        OnPropertyChanged(nameof(Categories));

        SelectedCategory = Categories.FirstOrDefault(
            x => x.Id == selectedId);

        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(MaximumDate));

        _loaded = true;
    }, "Could not load categories. Try Reload categories.");

    public Task<bool> AddCategoryAsync()
    {
        if (!CanEdit) return Task.FromResult(false);

        var name = (NewCategoryName ?? "").Trim();

        if (name.Length == 0)
            return Task.FromResult(
                SetError("Enter a category name."));

        if (Categories.Any(
                x => x.NormalizedName == name.ToUpperInvariant()))
        {
            return Task.FromResult(
                SetError("A category with this name already exists."));
        }

        return RunAsync(async () =>
        {
            var category = await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                return await scope.ServiceProvider
                    .GetRequiredService<CategoryService>()
                    .AddAsync(name);
            });

            Categories = Categories
                .Append(category)
                .OrderBy(
                    x => x.Name,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            OnPropertyChanged(nameof(Categories));

            SelectedCategory = category;
            OnPropertyChanged(nameof(SelectedCategory));

            NewCategoryName = "";
            OnPropertyChanged(nameof(NewCategoryName));
        }, "Could not add the category. Try reloading categories.");
    }

    public Task<bool> SaveAsync()
    {
        if (!CanEdit) return Task.FromResult(false);

        var category = SelectedCategory;

        if (category is null)
            return Task.FromResult(
                SetError("Select or create a category."));

        var text = (AmountText ?? "").Trim().Replace(',', '.');
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
            return Task.FromResult(SetError(
                "Enter a positive amount with up to 2 decimal places, for example 12.50."));
        }

        if (SelectedDate is not { } selectedDate)
            return Task.FromResult(
                SetError("Select a date."));

        var date = DateOnly.FromDateTime(selectedDate);
        var today = DateOnly.FromDateTime(
            _timeProvider.GetLocalNow().DateTime);

        if (date > today)
            return Task.FromResult(
                SetError("Expense date cannot be in the future."));

        var description = (Description ?? "").Trim();

        return RunAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<ExpenseService>()
                    .AddAsync(
                        amount,
                        category.Id,
                        date,
                        description);
            });

            IsSaved = true;
        }, "Could not save the expense. Your input has been kept.");
    }

    private async Task<bool> RunAsync(
        Func<Task> action,
        string failureMessage)
    {
        if (!CanReload) return false;

        IsBusy = true;
        ErrorMessage = "";
        NotifyState();

        try
        {
            await action();
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Expense form operation failed.");

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

    private void NotifyState()
    {
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsSaved));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanReload));
    }
}