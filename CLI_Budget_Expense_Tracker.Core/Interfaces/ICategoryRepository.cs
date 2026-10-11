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
}
