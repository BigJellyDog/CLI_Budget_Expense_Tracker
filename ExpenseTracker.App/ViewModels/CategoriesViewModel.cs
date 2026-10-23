namespace ExpenseTracker.App.ViewModels;

using CLI_Budget_Expense_Tracker.Core.Models;
using CLI_Budget_Expense_Tracker.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppInitializer _initializer;
    private readonly ILogger<CategoriesViewModel> _logger;

    private Guid? _editingCategoryId;
    private bool _loaded;

    public CategoriesViewModel(
        IServiceScopeFactory scopeFactory,
        AppInitializer initializer,
        ILogger<CategoriesViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _initializer = initializer;
        _logger = logger;
    }

    public IReadOnlyList<Category> Items { get; private set; } = [];

    public string NameText { get; set; } = "";

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool IsBusy { get; private set; }

    public bool IsEditing => _editingCategoryId.HasValue;

    public string FormTitle =>
        IsEditing ? "Rename category" : "New category";

    public string SaveButtonText =>
        IsEditing ? "Save changes" : "Create category";

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

        if (_editingCategoryId is { } id
            && Items.All(x => x.Id != id))
        {
            _editingCategoryId = null;
            NameText = "";

            ErrorMessage =
                "The category being edited no longer exists.";
        }
    }, () => "Could not load categories. Try Refresh.");

    public void StartRename(Category category)
    {
        if (!CanEdit) return;

        _editingCategoryId = category.Id;
        NameText = category.Name;
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyState();
    }

    public void CancelEdit()
    {
        if (IsBusy) return;

        _editingCategoryId = null;
        NameText = "";
        ErrorMessage = "";
        SuccessMessage = "";

        NotifyState();
    }

    public async Task SaveAsync()
    {
        if (!CanEdit) return;

        var name = (NameText ?? "").Trim();

        if (name.Length == 0)
        {
            ShowError("Enter a category name.");
            return;
        }

        var categoryId = _editingCategoryId;
        var normalizedName = name.ToUpperInvariant();

        if (Items.Any(x =>
                x.Id != categoryId
                && x.NormalizedName == normalizedName))
        {
            ShowError("A category with this name already exists.");
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
            _editingCategoryId = null;
            NameText = "";

            SuccessMessage = categoryId.HasValue
                ? "Category renamed."
                : "Category created.";

            // Saving succeeded; a reload failure must not look like a save failure.
            _loaded = false;
            Items = await ReadCategoriesAsync();
            _loaded = true;
        }, () => saved
            ? "The category was saved, but the list could not be reloaded. Click Refresh."
            : "Could not save the category. Click Refresh and try again.");
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
                ? "Category deleted."
                : "The category was already deleted. The list has been updated.";
        }, () => "Could not delete the category. Try Refresh.");
    }
    
    private Task<IReadOnlyList<Category>> ReadCategoriesAsync() =>
        Task.Run(async () =>
        {
            await _initializer.InitializeAsync();

            await using var scope = _scopeFactory.CreateAsyncScope();

            return await scope.ServiceProvider
                .GetRequiredService<CategoryService>()
                .GetAllAsync();
        });

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
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(NameText));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(ShowResults));
    }
}