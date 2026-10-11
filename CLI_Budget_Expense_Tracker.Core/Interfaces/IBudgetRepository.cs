namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface IBudgetRepository
{
    Task<BudgetLimit?> GetAsync(Guid categoryId, DateOnly month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetLimit>> GetForMonthAsync(DateOnly month, CancellationToken cancellationToken = default);
    Task AddAsync(BudgetLimit limit, CancellationToken cancellationToken = default);
    Task UpdateAsync(BudgetLimit limit, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid categoryId, DateOnly month, CancellationToken cancellationToken = default);
}
