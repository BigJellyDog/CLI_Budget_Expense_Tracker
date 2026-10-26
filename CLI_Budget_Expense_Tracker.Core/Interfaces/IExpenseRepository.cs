namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface IExpenseRepository
{
    
    Task<Expense?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only when the active expense still matches the original values.
    /// </summary>
    Task<bool> UpdateAsync(
        Expense original,
        Expense updated,
        CancellationToken cancellationToken = default);
    
    /// <summary>Returns active expenses within inclusive optional date bounds.</summary>
    Task<IReadOnlyList<Expense>> GetAsync(
        DateOnly? start = null, DateOnly? end = null, Guid? categoryId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);

    /// <summary>Saves all expenses in one transaction.</summary>
    Task AddRangeAsync(
        IReadOnlyCollection<Expense> expenses,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the given hashes that already exist, including deleted expenses.
    /// </summary>
    Task<IReadOnlySet<string>> GetExistingImportHashesAsync(
        IReadOnlyCollection<string> hashes,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
