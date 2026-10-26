namespace ExpenseTracker.App.ViewModels;

using System.Globalization;
using CLI_Budget_Expense_Tracker.Core.Interfaces;
using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;

public sealed class RecurringExpensesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecurringExpensesViewModel> _logger;

    private bool _loaded;

    public RecurringExpensesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        ILogger<RecurringExpensesViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _timeProvider = timeProvider;
        _logger = logger;

        SelectedStartDate = timeProvider.GetLocalNow().Date;
    }

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public IReadOnlyList<RecurringExpenseListItem> Items { get; private set; } = [];

    public IReadOnlyList<RecurrenceFrequency> Frequencies { get; } =
        Enum.GetValues<RecurrenceFrequency>();

    public Category? SelectedCategory { get; set; }

    public RecurrenceFrequency? SelectedFrequency { get; set; } =
        RecurrenceFrequency.Monthly;

    public DateTime? SelectedStartDate { get; set; }

    public string AmountText { get; set; } = "";

    public string Description { get; set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool IsSaved { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasSuccess => SuccessMessage.Length > 0;

    public bool CanGoBack => !IsBusy;

    public bool CanRefresh => !IsBusy;

    public bool CanEdit => _loaded && !IsBusy && !IsSaved;

    public bool CanStartNew => _loaded && !IsBusy && IsSaved;

    public bool ShowResults => _loaded && !IsBusy;

    public bool ShowNoCategories =>
        ShowResults && Categories.Count == 0;

    public Task LoadAsync() => RunAsync(
        ReloadAsync,
        () => "Could not load recurring expenses. Try Refresh.");

    public async Task CreateAsync()
    {
        if (!CanEdit) return;

        if (SelectedCategory is not { } category)
        {
            ShowError("Choose a category.");
            return;
        }

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
            ShowError(
                "Enter a positive amount with up to 2 decimal places.");

            return;
        }

        if (SelectedFrequency is not { } frequency
            || !Enum.IsDefined(frequency))
        {
            ShowError("Choose a frequency.");
            return;
        }

        if (SelectedStartDate is not { } selectedDate)
        {
            ShowError("Choose a start date.");
            return;
        }

        var startDate = DateOnly.FromDateTime(selectedDate);
        var description = (Description ?? "").Trim();

        var saved = false;
        var generated = false;

        await RunAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<RecurringExpenseService>()
                    .AddAsync(
                        amount,
                        category.Id,
                        frequency,
                        startDate,
                        description);
            });

            // The schedule is saved even if generation or reloading fails.
            saved = true;
            IsSaved = true;
            _loaded = false;
            SuccessMessage = "Schedule created.";

            var count = await GenerateCoreAsync();

            generated = true;
            SuccessMessage =
                $"Schedule created. Due expenses added: {count}.";

            await ReloadAsync();
        }, () => !saved
            ? "Could not create the schedule. Check the category and try again."
            : !generated
                ? "The schedule was saved, but due expenses could not be generated. Click Generate due expenses to retry."
                : "The schedule was saved and generation completed, but the list could not be reloaded. Try Refresh.");
    }

    public async Task GenerateDueAsync()
    {
        var generated = false;

        await RunAsync(async () =>
        {
            _loaded = false;

            var count = await GenerateCoreAsync();

            generated = true;
            SuccessMessage = $"Due expenses added: {count}.";

            await ReloadAsync();
        }, () => generated
            ? "Generation completed, but the list could not be reloaded. Try Refresh."
            : "Could not generate due expenses. Try again.");
    }

    public async Task DeactivateAsync(
        Guid scheduleId,
        Func<Task<bool>> confirmDeactivation)
    {
        if (!ShowResults
            || !Items.Any(x => x.Id == scheduleId && x.IsActive))
        {
            return;
        }

        await RunAsync(async () =>
        {
            if (!await confirmDeactivation())
                return;

            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<RecurringExpenseService>()
                    .DeactivateAsync(scheduleId);
            });

            Items = Items
                .Select(x => x.Id == scheduleId
                    ? x with { IsActive = false }
                    : x)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.NextDueDate)
                .ThenBy(x => x.Id)
                .ToArray();

            SuccessMessage = "Schedule deactivated.";
        }, () => "Could not deactivate the schedule. Try Refresh.");
    }
    
    public async Task ReactivateAsync(Guid scheduleId)
    {
        if (!ShowResults
            || !Items.Any(x => x.Id == scheduleId && !x.IsActive))
        {
            return;
        }

        var today = DateOnly.FromDateTime(
            _timeProvider.GetLocalNow().DateTime);

        await RunAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<RecurringExpenseService>()
                    .ReactivateAsync(scheduleId, today);
            });

            SuccessMessage = "Schedule reactivated.";

            await ReloadAsync();
        }, () => "Could not reactivate the schedule. Try Refresh.");
    }

    public async Task DeleteAsync(
        Guid scheduleId,
        Func<Task<bool>> confirmDeletion)
    {
        if (!ShowResults
            || !Items.Any(x => x.Id == scheduleId && !x.IsActive))
        {
            return;
        }

        await RunAsync(async () =>
        {
            if (!await confirmDeletion())
                return;

            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<RecurringExpenseService>()
                    .DeleteAsync(scheduleId);
            });

            Items = Items.Where(x => x.Id != scheduleId).ToArray();

            SuccessMessage = "Schedule deleted. Its expenses were kept.";
        }, () => "Could not delete the schedule. Try Refresh.");
    }

    public void StartNewSchedule()
    {
        if (!CanStartNew) return;

        IsSaved = false;
        AmountText = "";
        Description = "";
        SelectedStartDate = _timeProvider.GetLocalNow().Date;
        ErrorMessage = "";
        SuccessMessage = "";

        OnPropertyChanged(nameof(AmountText));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(SelectedStartDate));

        NotifyState();
    }

    private Task<int> GenerateCoreAsync() => Task.Run(async () =>
    {
        await _initializer.InitializeAsync();

        await using var scope = _scopeFactory.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<IRecurringExpenseGenerator>()
            .GenerateAsync();
    });

    private async Task ReloadAsync()
    {
        _loaded = false;

        var categoryId = SelectedCategory?.Id;

        var result = await Task.Run(async () =>
        {
            await _initializer.InitializeAsync();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var categories = await services
                .GetRequiredService<CategoryService>()
                .GetAllAsync();

            var schedules = await services
                .GetRequiredService<RecurringExpenseService>()
                .GetAllAsync();

            var names = categories.ToDictionary(
                x => x.Id,
                x => x.Name);

            IReadOnlyList<RecurringExpenseListItem> rows = schedules
                .Select(x => new RecurringExpenseListItem(
                    x.Id,
                    names.GetValueOrDefault(
                        x.CategoryId, "Unknown category"),
                    x.Amount,
                    x.Frequency,
                    x.StartDate,
                    x.NextDueDate,
                    x.IsActive,
                    x.Description))
                .ToArray();

            return (Categories: categories, Items: rows);
        });

        Items = result.Items;
        Categories = result.Categories;
        OnPropertyChanged(nameof(Categories));

        SelectedCategory = Categories.SingleOrDefault(
            x => x.Id == categoryId);

        OnPropertyChanged(nameof(SelectedCategory));

        _loaded = true;
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
                "Recurring expense operation failed.");

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
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsSaved));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanStartNew));
        OnPropertyChanged(nameof(ShowResults));
        OnPropertyChanged(nameof(ShowNoCategories));
    }
}