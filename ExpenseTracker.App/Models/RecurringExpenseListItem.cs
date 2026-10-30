namespace ExpenseTracker.App.Models;

using ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Core.Models;

public sealed record RecurringExpenseListItem(
    Guid Id,
    string CategoryName,
    decimal Amount,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly NextDueDate,
    bool IsActive,
    string Description,
    Guid CategoryId,
    DateOnly? EndDate,
    string Currency = "",
    Color? Color = null)
{
    /// <summary>Today, set by the view model, so the "in 3 days" chip is right.</summary>
    public DateOnly Today { get; init; }

    public bool HasEndDate => EndDate.HasValue;

    public string EndText => EndDate is { } end ? Loc.F("Ends: {0:dd.MM.yyyy}", end) : "";

    public bool IsInactive => !IsActive;

    public string Summary => $"{Amount:N2} {Currency}".TrimEnd() + $" · {Loc.T(Frequency.ToString())}";

    public string NextDueText => IsActive
        ? Loc.F("Next due: {0:dd.MM.yyyy}", NextDueDate)
        : Loc.T("Inactive");

    public Color Tint => (Color ?? Colors.Gray).WithAlpha(0.16f);

    public string Title => string.IsNullOrWhiteSpace(Description) ? CategoryName : Description;

    public string SinceText => Loc.F("{0} · since {1:dd.MM.yyyy}", CategoryName, StartDate);

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();

    public string FrequencyText => Loc.T(Frequency.ToString()).ToLower();

    public string NextDueChipText => IsActive
        ? Loc.F("Next due: {0:dd.MM.yyyy}", NextDueDate)
        : Loc.T("Inactive");

    private int DaysUntil => NextDueDate.DayNumber - Today.DayNumber;

    public bool ShowDueIn => IsActive;

    public string DueInText => DaysUntil switch
    {
        < 0 => Loc.T("overdue"),
        0 => Loc.T("today"),
        1 => Loc.T("tomorrow"),
        _ => Loc.F("in {0} days", DaysUntil)
    };

    /// <summary>Danger when overdue, Success within a week, a quiet gray beyond.</summary>
    public string DueInTone => DaysUntil < 0 ? "Danger" : DaysUntil <= 7 ? "Success" : "InkMuted";

    public string StartText => Loc.F("Start: {0:dd.MM.yyyy}", StartDate);
}

/// <summary>An occurrence in the "coming due" list.</summary>
public sealed record UpcomingItem(DateOnly Date, string Name, decimal Amount, string Currency, Color Color)
{
    public string DayText => Date.ToString("dd.MM.");

    public string WeekdayText => Date.ToString("ddd");

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();
}
