namespace ExpenseTracker.App.Models;

public sealed record ExpenseListItem(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string CategoryName,
    string Description);