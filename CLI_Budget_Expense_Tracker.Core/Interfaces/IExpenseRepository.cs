namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface IExpenseRepository
{
    /// <summary>Returns active expenses within inclusive optional date bounds.</summary>
    Task<IReadOnlyList<Expense>> GetAsync(
        DateOnly? start = null, DateOnly? end = null, Guid? categoryId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
