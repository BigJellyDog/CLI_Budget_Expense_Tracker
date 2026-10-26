namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

/// <summary>Builds spending analysis over a date range.</summary>
public sealed class ReportService(
    IExpenseRepository expenses,
    TimeProvider timeProvider)
{
    public const int DefaultTopCount = 5;

    public Task<PeriodReport> GetMonthAsync(
        DateOnly month,
        CancellationToken cancellationToken = default) =>
        GetPeriodAsync(
            new DateOnly(month.Year, month.Month, 1),
            new DateOnly(
                month.Year,
                month.Month,
                DateTime.DaysInMonth(month.Year, month.Month)),
            cancellationToken);

    /// <summary>
    /// Compares the range with the range of the same length right before it.
    /// </summary>
    public async Task<PeriodReport> GetPeriodAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default)
    {
        if (end < start)
        {
            throw new ArgumentException(
                "End date cannot be earlier than start date.");
        }

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        var length = end.DayNumber - start.DayNumber + 1;

        var current = await expenses.GetAsync(
            start, end, cancellationToken: cancellationToken);

        var previous = start > DateOnly.MinValue.AddDays(length)
            ? await expenses.GetAsync(
                start.AddDays(-length),
                start.AddDays(-1),
                cancellationToken: cancellationToken)
            : [];

        var currentTotals = Totals(current);
        var previousTotals = Totals(previous);

        var rows = currentTotals.Keys
            .Union(previousTotals.Keys)
            .Select(id => new CategoryPeriodRow(
                id,
                currentTotals.GetValueOrDefault(id),
                previousTotals.GetValueOrDefault(id)))
            .OrderByDescending(x => x.Total)
            .ThenByDescending(x => x.PreviousTotal)
            .ToArray();

        var total = current.Sum(x => x.Amount);

        // Days that already happened; future days carry no spending yet.
        var elapsed = today < start
            ? 0
            : Math.Min(today.DayNumber, end.DayNumber) - start.DayNumber + 1;

        var average = elapsed == 0
            ? 0m
            : decimal.Round(total / elapsed, 2);

        // A forecast only makes sense while the period is still running.
        decimal? forecast = elapsed > 0 && today < end
            ? decimal.Round(total / elapsed * length, 2)
            : null;

        var top = current
            .OrderByDescending(x => x.Amount)
            .ThenByDescending(x => x.Date)
            .ThenBy(x => x.Id)
            .Take(DefaultTopCount)
            .ToArray();

        return new PeriodReport(
            start,
            end,
            total,
            previous.Sum(x => x.Amount),
            rows,
            top,
            elapsed,
            average,
            forecast);
    }

    private static Dictionary<Guid, decimal> Totals(
        IReadOnlyList<Expense> items) =>
        items
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
}
