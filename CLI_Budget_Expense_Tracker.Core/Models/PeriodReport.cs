namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>One category's spending compared with the previous period.</summary>
public sealed record CategoryPeriodRow(
    Guid CategoryId,
    decimal Total,
    decimal PreviousTotal)
{
    /// <summary>Null when there is nothing to compare with.</summary>
    public decimal? ChangePercent => PeriodReport.PercentChange(Total, PreviousTotal);
}

/// <summary>
/// Spending analysis for a date range and the equally long range before it.
/// </summary>
public sealed record PeriodReport(
    DateOnly Start,
    DateOnly End,
    decimal Total,
    decimal PreviousTotal,
    IReadOnlyList<CategoryPeriodRow> Categories,
    IReadOnlyList<Expense> TopExpenses,
    int DaysElapsed,
    decimal AveragePerDay,
    decimal? Forecast)
{
    public int DaysInPeriod => End.DayNumber - Start.DayNumber + 1;

    public decimal? ChangePercent => PercentChange(Total, PreviousTotal);

    internal static decimal? PercentChange(decimal current, decimal previous) =>
        previous == 0
            ? null
            : decimal.Round((current - previous) / previous * 100m, 1);
}
