namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetByNormalizedNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    /// <summary>Deletes an unused category; throws if any expense or budget references it.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryUsage> GetUsageAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Moves expenses and recurring schedules to another category, removes the budget limits
    /// of the old one, then deletes it. Returns what the old category held.
    /// </summary>
    Task<CategoryUsage> MoveAndDeleteAsync(Guid id, Guid targetId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Permanently deletes a category together with its expenses, recurring schedules and budget limits.
    /// Returns what the category held.
    /// </summary>
    Task<CategoryUsage> DeleteWithDataAsync(Guid id, CancellationToken cancellationToken = default);
}
