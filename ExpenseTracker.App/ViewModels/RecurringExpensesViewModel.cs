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

    private readonly AppSettings _settings;

    private bool _loaded;
    private Guid? _editingScheduleId;

    public RecurringExpensesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        TimeProvider timeProvider,
        AppSettings settings,
        ILogger<RecurringExpensesViewModel> logger)
    {
        _settings = settings;
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _timeProvider = timeProvider;
        _logger = logger;

        SelectedStartDate = timeProvider.GetLocalNow().Date;
        SelectedEndDate = SelectedStartDate;
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

    public bool HasEndDate { get; set; }

    public void UseEndDate()
    {
        HasEndDate = true;
        OnPropertyChanged(nameof(HasEndDate));
    }

    public DateTime? SelectedEndDate { get; set; }

    public bool IsEditing => _editingScheduleId.HasValue;

    public string FormTitle => IsEditing ? Loc.T("Edit schedule") : Loc.T("New schedule");

    public string SaveButtonText => IsEditing ? Loc.T("Save changes") : Loc.T("Create schedule");

    public bool CanChangeStartDate => !IsEditing;

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
        () => Loc.T("Could not load recurring expenses. Try Refresh."));

    public async Task SaveAsync()
    {
        if (_editingScheduleId is { } id)
        {
            await UpdateAsync(id);
        }
        else
        {
            await CreateAsync();
        }
    }

    public async Task CreateAsync()
    {
        if (!CanEdit) return;

        if (!TryReadForm(out var form)) return;

        if (SelectedStartDate is not { } selectedDate)
        {
            ShowError(Loc.T("Choose a start date."));
            return;
        }

        var startDate = DateOnly.FromDateTime(selectedDate);
        var (amount, category, frequency, description, endDate) =
            (form.Amount, form.Category, form.Frequency, form.Description, form.EndDate);

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
                        description,
                        endDate);
            });

            // The schedule is saved even if generation or reloading fails.
            saved = true;
            IsSaved = true;
            _loaded = false;
            SuccessMessage = Loc.T("Schedule created.");

            var count = await GenerateCoreAsync();

            generated = true;
            SuccessMessage =
                Loc.F("Schedule created. Due expenses added: {0}.", count);

            await ReloadAsync();
        }, () => !saved
            ? Loc.T("Could not create the schedule. Check the category and try again.")
            : !generated
                ? Loc.T("The schedule was saved, but due expenses could not be generated. Click Generate due expenses to retry.")
                : Loc.T("The schedule was saved and generation completed, but the list could not be reloaded. Try Refresh."));
    }

    private async Task UpdateAsync(Guid scheduleId)
    {
        if (!CanEdit) return;

        if (!TryReadForm(out var form)) return;

        var saved = false;

        await RunAsync(async () =>
        {
            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<RecurringExpenseService>()
                    .UpdateAsync(
                        scheduleId,
                        form.Amount,
                        form.Category.Id,
                        form.Frequency,
                        form.Description,
                        form.EndDate);
            });

            saved = true;
            ResetForm();
            SuccessMessage = Loc.T("Schedule updated. Expenses created earlier were not changed.");

            await ReloadAsync();
        }, () => saved
            ? Loc.T("The schedule was updated, but the list could not be reloaded. Try Refresh.")
            : Loc.T("Could not update the schedule. Check the values and try again."));
    }

    public void StartEdit(Guid scheduleId)
    {
        if (!ShowResults) return;

        var item = Items.FirstOrDefault(x => x.Id == scheduleId);

        if (item is null) return;

        _editingScheduleId = scheduleId;
        IsSaved = false;

        SelectedCategory = Categories.FirstOrDefault(x => x.Id == item.CategoryId);
        SelectedFrequency = item.Frequency;
        SelectedStartDate = item.StartDate.ToDateTime(TimeOnly.MinValue);
        AmountText = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        Description = item.Description;
        HasEndDate = item.EndDate.HasValue;
        SelectedEndDate = (item.EndDate ?? item.NextDueDate).ToDateTime(TimeOnly.MinValue);
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyForm();
        NotifyState();
    }

    public void CancelEdit()
    {
        if (IsBusy || !IsEditing) return;

        ResetForm();
        NotifyState();
    }

    private sealed record FormValues(
        decimal Amount,
        Category Category,
        RecurrenceFrequency Frequency,
        string Description,
        DateOnly? EndDate);

    private bool TryReadForm(out FormValues values)
    {
        values = null!;

        if (SelectedCategory is not { } category)
        {
            ShowError(Loc.T("Choose a category."));
            return false;
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
                Loc.T("Enter a positive amount with up to 2 decimal places."));

            return false;
        }

        if (SelectedFrequency is not { } frequency
            || !Enum.IsDefined(frequency))
        {
            ShowError(Loc.T("Choose a frequency."));
            return false;
        }

        DateOnly? endDate = null;

        if (HasEndDate)
        {
            if (SelectedEndDate is not { } selectedEnd)
            {
                ShowError(Loc.T("Choose an end date."));
                return false;
            }

            endDate = DateOnly.FromDateTime(selectedEnd);

            DateOnly? start = null;

            if (_editingScheduleId is { } id)
            {
                start = Items.FirstOrDefault(x => x.Id == id)?.StartDate;
            }
            else if (SelectedStartDate is { } selectedStart)
            {
                start = DateOnly.FromDateTime(selectedStart);
            }

            if (start is { } startDate && endDate < startDate)
            {
                ShowError(Loc.T("End date cannot be before the start date."));
                return false;
            }
        }

        values = new FormValues(
            amount,
            category,
            frequency,
            (Description ?? "").Trim(),
            endDate);

        return true;
    }

    private void ResetForm()
    {
        var today = _timeProvider.GetLocalNow().Date;

        _editingScheduleId = null;
        AmountText = "";
        Description = "";
        HasEndDate = false;
        SelectedStartDate = today;
        SelectedEndDate = today;
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyForm();
    }

    private void NotifyForm()
    {
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedFrequency));
        OnPropertyChanged(nameof(AmountText));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(SelectedStartDate));
        OnPropertyChanged(nameof(HasEndDate));
        OnPropertyChanged(nameof(SelectedEndDate));
    }

    public async Task GenerateDueAsync()
    {
        var generated = false;

        await RunAsync(async () =>
        {
            _loaded = false;

            var count = await GenerateCoreAsync();

            generated = true;
            SuccessMessage = Loc.F("Due expenses added: {0}.", count);

            await ReloadAsync();
        }, () => generated
            ? Loc.T("Generation completed, but the list could not be reloaded. Try Refresh.")
            : Loc.T("Could not generate due expenses. Try again."));
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

            SuccessMessage = Loc.T("Schedule deactivated.");
        }, () => Loc.T("Could not deactivate the schedule. Try Refresh."));
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
            try
            {
                await Task.Run(async () =>
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    await scope.ServiceProvider
                        .GetRequiredService<RecurringExpenseService>()
                        .ReactivateAsync(scheduleId, today);
                });
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Reactivation was rejected for schedule {ScheduleId}.",
                    scheduleId);

                ErrorMessage = exception.Message;
                return;
            }

            SuccessMessage = Loc.T("Schedule reactivated.");

            await ReloadAsync();
        }, () => Loc.T("Could not reactivate the schedule. Try Refresh."));
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

            SuccessMessage = Loc.T("Schedule deleted. Its expenses were kept.");
        }, () => Loc.T("Could not delete the schedule. Try Refresh."));
    }

    public void StartNewSchedule()
    {
        if (!CanStartNew) return;

        IsSaved = false;
        ResetForm();
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
                        x.CategoryId, Loc.T("Unknown category")),
                    x.Amount,
                    x.Frequency,
                    x.StartDate,
                    x.NextDueDate,
                    x.IsActive,
                    x.Description,
                    x.CategoryId,
                    x.EndDate,
                    _settings.Currency))
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
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CanChangeStartDate));
    }
}