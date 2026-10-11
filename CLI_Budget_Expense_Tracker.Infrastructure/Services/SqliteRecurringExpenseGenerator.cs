namespace CLI_Budget_Expense_Tracker.Infrastructure.Services;

using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Generates expenses and advances schedules in a single transaction.
/// Use a fresh service scope for each generation operation.
/// </summary>
public sealed class SqliteRecurringExpenseGenerator(
    ExpenseTrackerDbContext context,
    IRecurringExpenseRepository recurringExpenses,
    TimeProvider timeProvider) : IRecurringExpenseGenerator
{
    public async Task<int> GenerateAsync(
        DateOnly? throughDate = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        var endDate = throughDate ?? today;

        if (endDate > today)
        {
            throw new ArgumentOutOfRangeException(
                nameof(throughDate),
                "Cannot generate expenses for future dates.");
        }

        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken);

        var schedules = await recurringExpenses.GetDueAsync(
            endDate,
            cancellationToken);

        var generatedCount = 0;

        foreach (var schedule in schedules)
        {
            // Deleted expenses still count as previously generated occurrences.
            var existingDates = (await context.Expenses
                .IgnoreQueryFilters()
                .Where(x => x.RecurringExpenseId == schedule.Id
                    && x.Date >= schedule.NextDueDate
                    && x.Date <= endDate)
                .Select(x => x.Date)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            while (schedule.IsActive && schedule.NextDueDate <= endDate)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dueDate = schedule.NextDueDate;

                if (existingDates.Add(dueDate))
                {
                    context.Expenses.Add(new Expense(
                        Guid.NewGuid(),
                        schedule.Amount,
                        schedule.CategoryId,
                        dueDate,
                        schedule.Description,
                        schedule.Id));

                    generatedCount++;
                }

                schedule.AdvanceNextDueDate();
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return generatedCount;
    }
}