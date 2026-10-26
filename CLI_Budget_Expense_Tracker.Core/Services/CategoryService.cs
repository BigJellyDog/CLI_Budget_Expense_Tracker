namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

public sealed class CategoryService(ICategoryRepository categories)
{
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        categories.GetAllAsync(cancellationToken);

    public async Task<Category> AddAsync(string name, CancellationToken cancellationToken = default)
    {
        var category = new Category(name);
        await EnsureUniqueAsync(category.NormalizedName, category.Id, cancellationToken);
        await categories.AddAsync(category, cancellationToken);
        return category;
    }

    public async Task RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Category not found.");
        var candidate = new Category(name);
        await EnsureUniqueAsync(candidate.NormalizedName, id, cancellationToken);
        category.Rename(candidate.Name);
        await categories.UpdateAsync(category, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        categories.DeleteAsync(id, cancellationToken);

    public Task<CategoryUsage> GetUsageAsync(Guid id, CancellationToken cancellationToken = default) =>
        categories.GetUsageAsync(id, cancellationToken);

    public async Task<CategoryUsage> MoveAndDeleteAsync(Guid id, Guid targetId, CancellationToken cancellationToken = default)
    {
        if (id == targetId)
            throw new InvalidOperationException("Choose a different category to move the data to.");

        _ = await categories.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Category not found.");
        _ = await categories.GetByIdAsync(targetId, cancellationToken)
            ?? throw new InvalidOperationException("Target category not found.");

        return await categories.MoveAndDeleteAsync(id, targetId, cancellationToken);
    }

    private async Task EnsureUniqueAsync(string name, Guid id, CancellationToken cancellationToken)
    {
        var existing = await categories.GetByNormalizedNameAsync(name, cancellationToken);
        if (existing is not null && existing.Id != id)
            throw new InvalidOperationException("A category with this name already exists.");
    }
}
