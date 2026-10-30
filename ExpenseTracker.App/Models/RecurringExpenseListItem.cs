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
    string Currency = "")
{
    public bool HasEndDate => EndDate.HasValue;

    public string EndText => EndDate is { } end ? Loc.F("Ends: {0:dd.MM.yyyy}", end) : "";

    public bool IsInactive => !IsActive;

    public string Summary => $"{Amount:N2} {Currency}".TrimEnd() + $" · {Loc.T(Frequency.ToString())}";

    public string NextDueText => IsActive
        ? Loc.F("Next due: {0:dd.MM.yyyy}", NextDueDate)
        : Loc.T("Inactive");

    public string StartText => Loc.F("Start: {0:dd.MM.yyyy}", StartDate);
}