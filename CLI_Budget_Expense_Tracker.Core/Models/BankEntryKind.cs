namespace CLI_Budget_Expense_Tracker.Core.Models;

public enum BankEntryKind
{
    ExpenseCandidate = 1,
    Incoming = 2,
    InternalTransfer = 3,
    Ignored = 4,
    NeedsReview = 5
}