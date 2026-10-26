namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>A bank record chosen for import together with its category.</summary>
public sealed record BankImportCandidate(
    ClassifiedBankEntry Entry,
    string Hash,
    Guid CategoryId);

public sealed record BankImportFailure(int RowNumber, string Reason);

/// <summary>Outcome of saving bank records as expenses.</summary>
public sealed record BankImportResult(
    int Added,
    int Duplicates,
    IReadOnlyList<BankImportFailure> Failures);
