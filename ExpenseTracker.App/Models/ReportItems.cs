namespace ExpenseTracker.App.Models;

using System.Globalization;
using ExpenseTracker.App.Controls;
using ExpenseTracker.App.Services;

/// <summary>One row of the "By category" table: this month, last month and how they compare.</summary>
public sealed record CategoryReportItem(
    Guid CategoryId,
    string CategoryName,
    decimal Total,
    decimal PreviousTotal,
    decimal? ChangePercent,
    double BarValue,
    double MarkerValue,
    Color Color,
    string Currency = "")
{
    public string TotalText => $"{Total:N2} {Currency}".TrimEnd();

    public string PreviousText => $"{PreviousTotal:N2} {Currency}".TrimEnd();

    public string ChangeText => ReportFormat.Change(ChangePercent, Total, PreviousTotal);

    /// <summary>Red when more was spent, green when less, gray when there is nothing to compare.</summary>
    public string ChangeTone => ChangePercent is > 0 ? "Danger" : ChangePercent is < 0 ? "Success" : "InkMuted";

    public string ChangeIcon => ChangePercent is > 0
        ? Icons.ArrowUpRight
        : ChangePercent is < 0 ? Icons.ArrowDownRight : "";
}

public sealed record TopExpenseItem(
    int Rank,
    DateOnly Date,
    string CategoryName,
    string Description,
    decimal Amount,
    Color Color,
    string Currency = "")
{
    public Color Tint => Color.WithAlpha(0.16f);

    public string RankText => Rank.ToString();

    public string Title => Description.Length > 0 ? Description : CategoryName;

    public string Subtitle => $"{CategoryName} · {Date:dd.MM.}";

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();
}

public static class ReportFormat
{
    public static string Change(decimal? percent, decimal current, decimal previous)
    {
        if (percent is { } value)
            return $"{(value > 0 ? "+" : "")}{value.ToString("0.#", CultureInfo.InvariantCulture)}%";

        return "–";
    }
}
