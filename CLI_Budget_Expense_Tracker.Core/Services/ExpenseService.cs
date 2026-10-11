namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

public sealed class ExpenseService(
    IExpenseRepository expenses, ICategoryRepository categories, TimeProvider timeProvider)
{
    public DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    public async Task<Expense> AddAsync(
        decimal amount, Guid categoryId, DateOnly date, string description = "",
        CancellationToken cancellationToken = default)
    {
        if (date > Today)
            throw new ArgumentOutOfRangeException(nameof(date), "Expense date cannot be in the future.");
        var expense = new Expense(Guid.NewGuid(), amount, categoryId, date, description);
        if (await categories.GetByIdAsync(categoryId, cancellationToken) is null)
            throw new InvalidOperationException("Category not found.");
        await expenses.AddAsync(expense, cancellationToken);
        return expense;
    }

    public async Task<ExpenseReport> GetReportAsync(
        DateOnly? start = null, DateOnly? end = null, Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (start.HasValue && end.HasValue && end.Value < start.Value)
            throw new ArgumentException("End date cannot be earlier than start date.");
        var items = await expenses.GetAsync(start, end, categoryId, cancellationToken);
        // A SQL sum of integer cents could overflow Int64; sum decimal amounts after loading.
        return new ExpenseReport(items, items.Sum(x => x.Amount),
            items.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)));
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        expenses.DeleteAsync(id, cancellationToken);
}
