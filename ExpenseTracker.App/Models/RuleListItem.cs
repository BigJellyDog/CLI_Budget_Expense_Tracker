namespace ExpenseTracker.App.Models;

public sealed record RuleListItem(
    Guid Id,
    string Pattern,
    string CategoryName);
