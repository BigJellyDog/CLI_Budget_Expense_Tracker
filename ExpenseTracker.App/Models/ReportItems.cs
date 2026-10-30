namespace ExpenseTracker.App.Models;

using ExpenseTracker.App.Services;

using System.Globalization;

public sealed record CategoryReportItem(
    string CategoryName,
    decimal Total,
    decimal PreviousTotal,
    decimal? ChangePercent,
    double Share,
    string Currency = "")
{
    public string TotalText => $"{Total:N2} {Currency}".TrimEnd();

    public string PreviousText => Loc.F("Previous month: {0:N2} {1}", PreviousTotal, Currency).TrimEnd();

    public string ChangeText => ReportFormat.Change(ChangePercent, Total, PreviousTotal);
}

public sealed record TopExpenseItem(
    DateOnly Date,
    string CategoryName,
    string Description,
    decimal Amount,
    string Currency = "")
{
    public string DateText => Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();

    public string Title => Description.Length > 0
        ? $"{CategoryName} · {Description}"
        : CategoryName;
}

public static class ReportFormat
{
    public static string Change(decimal? percent, decimal current, decimal previous)
    {
        if (percent is { } value)
            return $"{(value > 0 ? "+" : "")}{value.ToString("0.#", CultureInfo.InvariantCulture)}%";

        return current > 0 && previous == 0 ? Loc.T("new") : "—";
    }
}
