namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface ICategoryRuleRepository
{
    /// <summary>All rules, ordered by pattern.</summary>
    Task<IReadOnlyList<CategoryRule>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>The rule with the normalized pattern, or null.</summary>
    Task<CategoryRule?> GetByNormalizedPatternAsync(string normalizedPattern, CancellationToken cancellationToken = default);
    /// <summary>Saves a new rule.</summary>
    Task AddAsync(CategoryRule rule, CancellationToken cancellationToken = default);
    /// <summary>Deletes a rule; false when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
