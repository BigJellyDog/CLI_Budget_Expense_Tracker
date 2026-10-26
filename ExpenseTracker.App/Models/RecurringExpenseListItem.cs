namespace ExpenseTracker.App.Models;

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
    DateOnly? EndDate)
{
    public bool HasEndDate => EndDate.HasValue;

    public string EndText => EndDate is { } end ? $"Ends: {end:dd.MM.yyyy}" : "";

    public bool IsInactive => !IsActive;

    public string Summary => $"{Amount:N2} · {Frequency}";

    public string NextDueText => IsActive
        ? $"Next due: {NextDueDate:dd.MM.yyyy}"
        : "Inactive";
}