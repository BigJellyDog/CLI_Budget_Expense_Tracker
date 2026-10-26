namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models;
using Services;

public sealed class RulesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<RulesViewModel> _logger;

    private bool _loaded;

    public RulesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        ILogger<RulesViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;
    }

    public IReadOnlyList<RuleListItem> Items { get; private set; } = [];

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public Category? SelectedCategory { get; set; }

    public string PatternText { get; set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasSuccess => SuccessMessage.Length > 0;

    public bool CanRefresh => !IsBusy;

    public bool CanGoBack => !IsBusy;

    public bool CanEdit => _loaded && !IsBusy;

    public bool ShowResults => _loaded && !IsBusy;

    public bool ShowNoCategories => ShowResults && Categories.Count == 0;

    public Task LoadAsync() => RunAsync(
        ReloadAsync,
        () => "Could not load the rules. Try Refresh.");

    public async Task AddAsync()
    {
        if (!CanEdit) return;

        var pattern = (PatternText ?? "").Trim();

        if (pattern.Length == 0)
        {
            ShowError("Enter the text to look for.");
            return;
        }

        if (SelectedCategory is not { } category)
        {
            ShowError("Choose a category.");
            return;
        }

        if (Items.Any(x => string.Equals(
                x.Pattern, pattern, StringComparison.OrdinalIgnoreCase)))
        {
            ShowError("A rule with this text already exists.");
            return;
        }

        var saved = false;

        await RunAsync(async () =>
        {
            try
            {
                await Task.Run(async () =>
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    await scope.ServiceProvider
                        .GetRequiredService<CategoryRuleService>()
                        .AddAsync(pattern, category.Id);
                });
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Adding a rule was rejected.");

                ErrorMessage = exception.Message;
                return;
            }

            saved = true;
            PatternText = "";
            SuccessMessage = "Rule added.";

            await ReloadAsync();
        }, () => saved
            ? "The rule was added, but the list could not be reloaded. Click Refresh."
            : "Could not add the rule. Try again.");
    }

    public async Task DeleteAsync(
        Guid ruleId,
        Func<Task<bool>> confirmDeletion)
    {
        if (!CanEdit) return;

        await RunAsync(async () =>
        {
            if (!await confirmDeletion())
                return;

            await Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<CategoryRuleService>()
                    .DeleteAsync(ruleId);
            });

            Items = Items.Where(x => x.Id != ruleId).ToArray();

            SuccessMessage = "Rule deleted.";
        }, () => "Could not delete the rule. Try Refresh.");
    }

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

            var rules = await services
                .GetRequiredService<CategoryRuleService>()
                .GetAllAsync();

            var names = categories.ToDictionary(x => x.Id, x => x.Name);

            IReadOnlyList<RuleListItem> rows = rules
                .Select(x => new RuleListItem(
                    x.Id,
                    x.Pattern,
                    names.GetValueOrDefault(x.CategoryId, "Unknown category")))
                .ToArray();

            return (Categories: categories, Items: rows);
        });

        Items = result.Items;
        Categories = result.Categories;
        OnPropertyChanged(nameof(Categories));

        SelectedCategory = Categories.SingleOrDefault(x => x.Id == categoryId);
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
            _logger.LogError(exception, "Rule operation failed.");

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
        OnPropertyChanged(nameof(PatternText));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(ShowResults));
        OnPropertyChanged(nameof(ShowNoCategories));
    }
}
