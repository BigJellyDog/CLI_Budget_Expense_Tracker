namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

public sealed class ExpenseService(
    IExpenseRepository expenses,
    ICategoryRepository categories,
    TimeProvider timeProvider)
{
    public DateOnly Today =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    public Task<Expense?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        expenses.GetByIdAsync(id, cancellationToken);

    /// <summary>
    /// Preserves identity and detects changes made since the expense was loaded.
    /// Returns false when the expense was changed or deleted.
    /// </summary>
    public async Task<bool> UpdateAsync(
        Expense original,
        decimal amount,
        Guid categoryId,
        DateOnly date,
        string description = "",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (original.IsDeleted)
            return false;

        if (date > Today)
        {
            throw new ArgumentOutOfRangeException(
                nameof(date),
                "Expense date cannot be in the future.");
        }

        if (original.RecurringExpenseId.HasValue
            && date != original.Date)
        {
            throw new InvalidOperationException(
                "The date of a recurring expense occurrence cannot be changed.");
        }

        var updated = new Expense(
            original.Id,
            amount,
            categoryId,
            date,
            description,
            original.RecurringExpenseId);

        if (await categories.GetByIdAsync(
                categoryId, cancellationToken) is null)
        {
            throw new InvalidOperationException(
                "Category not found.");
        }

        return await expenses.UpdateAsync(
            original,
            updated,
            cancellationToken);
    }

    public async Task<Expense> AddAsync(
        decimal amount,
        Guid categoryId,
        DateOnly date,
        string description = "",
        CancellationToken cancellationToken = default)
    {
        if (date > Today)
        {
            throw new ArgumentOutOfRangeException(
                nameof(date),
                "Expense date cannot be in the future.");
        }

        var expense = new Expense(
            Guid.NewGuid(),
            amount,
            categoryId,
            date,
            description);

        if (await categories.GetByIdAsync(
                categoryId, cancellationToken) is null)
        {
            throw new InvalidOperationException(
                "Category not found.");
        }

        await expenses.AddAsync(expense, cancellationToken);

        return expense;
    }

    public async Task<ExpenseReport> GetReportAsync(
        DateOnly? start = null,
        DateOnly? end = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (start.HasValue
            && end.HasValue
            && end.Value < start.Value)
        {
            throw new ArgumentException(
                "End date cannot be earlier than start date.");
        }

        var items = await expenses.GetAsync(
            start,
            end,
            categoryId,
            cancellationToken);

        // Sum decimal amounts after loading to avoid Int64 overflow in SQL.
        return new ExpenseReport(
            items,
            items.Sum(x => x.Amount),
            items
                .GroupBy(x => x.CategoryId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(x => x.Amount)));
    }

    public Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        expenses.DeleteAsync(id, cancellationToken);
}