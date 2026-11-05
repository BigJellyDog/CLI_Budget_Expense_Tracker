namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface IBudgetRepository
{
    /// <summary>The limit of a category for a month, or null.</summary>
    Task<BudgetLimit?> GetAsync(Guid categoryId, DateOnly month, CancellationToken cancellationToken = default);
    /// <summary>All limits of a month.</summary>
    Task<IReadOnlyList<BudgetLimit>> GetForMonthAsync(DateOnly month, CancellationToken cancellationToken = default);
    /// <summary>Saves a new limit.</summary>
    Task AddAsync(BudgetLimit limit, CancellationToken cancellationToken = default);
    /// <summary>Saves the changes of a limit.</summary>
    Task UpdateAsync(BudgetLimit limit, CancellationToken cancellationToken = default);
    /// <summary>Deletes the limit of a category for a month; false when there was none.</summary>
    Task<bool> DeleteAsync(Guid categoryId, DateOnly month, CancellationToken cancellationToken = default);
}
