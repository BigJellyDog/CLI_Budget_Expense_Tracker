namespace ExpenseTracker.App.Models;

using CLI_Budget_Expense_Tracker.Core.Models;

public sealed record BankImportListItem(ClassifiedBankEntry Entry)
{
    public string Kind => Entry.Kind switch
    {
        BankEntryKind.ExpenseCandidate => "Expense candidate",
        BankEntryKind.Incoming => "Incoming",
        BankEntryKind.InternalTransfer => "Internal transfer",
        BankEntryKind.Ignored => "Ignored",
        _ => "Needs review"
    };

    public string Description => Entry.Source.Description;

    public string Amount =>
        $"{Entry.Source.Amount:N2} {Entry.Source.Currency}";

    public string Date => Entry.Source.CompletedAt is { } completedAt
        ? $"Completed: {completedAt:dd.MM.yyyy HH:mm}"
        : "Not completed";

    public string Details =>
        $"Row {Entry.Source.RowNumber} · {Entry.Source.Type} · " +
        $"{Entry.Source.Product} · {Entry.Source.Status} · " +
        $"Fee: {Entry.Source.Fee:N2} {Entry.Source.Currency}";

    public string Reason => Entry.Reason;
}