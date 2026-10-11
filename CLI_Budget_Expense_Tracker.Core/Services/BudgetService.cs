namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

public sealed class BudgetService(
    IBudgetRepository budgets, IExpenseRepository expenses, ICategoryRepository categories)
{
    public async Task SetMonthlyLimitAsync(
        Guid categoryId, DateOnly date, decimal amount, CancellationToken cancellationToken = default)
    {
        var candidate = new BudgetLimit(Guid.NewGuid(), categoryId, date, amount);
        if (await categories.GetByIdAsync(categoryId, cancellationToken) is null)
            throw new InvalidOperationException("Category not found.");
        var existing = await budgets.GetAsync(categoryId, candidate.Month, cancellationToken);
        if (existing is null)
            await budgets.AddAsync(candidate, cancellationToken);
        else
        {
            existing.SetMonthlyAmount(amount);
            await budgets.UpdateAsync(existing, cancellationToken);
        }
    }

    public async Task<BudgetStatus> GetStatusAsync(
        Guid categoryId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var month = FirstDay(date);
        var limit = await budgets.GetAsync(categoryId, month, cancellationToken);
        var items = await expenses.GetAsync(month, LastDay(date), categoryId, cancellationToken);
        return new BudgetStatus(categoryId, month, limit?.MonthlyAmount, items.Sum(x => x.Amount));
    }

    public async Task<IReadOnlyList<BudgetStatus>> GetMonthlyStatusesAsync(
        DateOnly date, CancellationToken cancellationToken = default)
    {
        var month = FirstDay(date);
        var limits = await budgets.GetForMonthAsync(month, cancellationToken);
        var items = await expenses.GetAsync(month, LastDay(date), cancellationToken: cancellationToken);
        var totals = items.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        return limits.Select(x => new BudgetStatus(
            x.CategoryId, month, x.MonthlyAmount, totals.GetValueOrDefault(x.CategoryId))).ToArray();
    }

    public Task<bool> DeleteMonthlyLimitAsync(
        Guid categoryId, DateOnly date, CancellationToken cancellationToken = default) =>
        budgets.DeleteAsync(categoryId, FirstDay(date), cancellationToken);

    private static DateOnly FirstDay(DateOnly date) => new(date.Year, date.Month, 1);
    private static DateOnly LastDay(DateOnly date) =>
        new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
}
