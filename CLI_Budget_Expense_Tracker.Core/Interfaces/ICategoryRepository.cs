namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

/// <summary>
/// Defines persistence operations for expense categories.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Returns a legacyCategory by its identifier, or null if it does not exist.
    /// </summary>
    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a legacyCategory by its normalized name, or null if none exists.
    /// </summary>
    Task<Category?> GetByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all categories ordered by normalized name.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new legacyCategory.
    /// </summary>
    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing legacyCategory.
    /// </summary>
    Task UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether expenses, budget limits, or recurring rules
    /// reference the legacyCategory, including soft-deleted expenses.
    /// </summary>
    Task<bool> IsInUseAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an unreferenced legacyCategory.
    /// Returns false if the legacyCategory does not exist.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The legacyCategory is referenced by another record.
    /// </exception>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}