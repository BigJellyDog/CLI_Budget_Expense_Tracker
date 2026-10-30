namespace ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// The categories every new database starts with, named in the language of the screens.
/// When the language changes, the ones that still carry their standard name are renamed to match.
/// A category the person renamed is left alone.
/// </summary>
public sealed class StandardCategories(
    IServiceScopeFactory scopeFactory,
    AppSettings settings,
    ILogger<StandardCategories> logger)
{
    /// <summary>English names, which are also the keys of their translations.</summary>
    public static readonly IReadOnlyList<string> Names =
    [
        "Food", "Restaurants", "Transport", "Housing", "Utilities", "Health", "Entertainment", "Shopping",
        "Clothing", "Education", "Travel", "Gifts", "Pets", "Personal care", "Insurance", "Sports",
        "Subscriptions", "Other"
    ];

    /// <summary>Adds the standard categories once, to a database that has none yet.</summary>
    public async Task SeedAsync()
    {
        if (settings.StandardCategoriesSeeded) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<CategoryService>();

        if ((await service.GetAllAsync()).Count == 0)
        {
            for (var i = 0; i < Names.Count; i++)
            {
                var name = Loc.T(Names[i]);
                await service.AddAsync(name);
                settings.SetCategoryColor(name, i % Controls.CategoryPalette.Count);
            }
        }

        // Also for a database that already had categories: the person's own set stays as it is.
        settings.MarkStandardCategoriesSeeded();
    }

    /// <summary>Renames the standard categories from one language to another.</summary>
    public async Task ChangeLanguageAsync(string fromCode, string toCode)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var categories = await service.GetAllAsync();

        foreach (var key in Names)
        {
            var from = Loc.In(fromCode, key);
            var to = Loc.In(toCode, key);

            if (string.Equals(from, to, StringComparison.CurrentCultureIgnoreCase)) continue;

            var category = categories.FirstOrDefault(x => string.Equals(x.Name, from, StringComparison.CurrentCultureIgnoreCase));

            if (category is null) continue;

            try
            {
                await service.RenameAsync(category.Id, to);
                settings.RenameCategoryColor(category.Name, to);
            }
            catch (Exception exception)
            {
                // For example the person already has a category with the new name; this one keeps its name.
                logger.LogWarning(exception, "Could not rename the standard category {Name}.", category.Name);
            }
        }
    }
}
