namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface ICategoryRuleRepository
{
    Task<IReadOnlyList<CategoryRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryRule?> GetByNormalizedPatternAsync(string normalizedPattern, CancellationToken cancellationToken = default);
    Task AddAsync(CategoryRule rule, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
