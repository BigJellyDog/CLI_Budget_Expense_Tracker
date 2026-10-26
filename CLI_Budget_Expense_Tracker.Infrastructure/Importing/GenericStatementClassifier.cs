namespace CLI_Budget_Expense_Tracker.Infrastructure.Importing;

using Core.Interfaces;
using Core.Models;

/// <summary>
/// Classifies records from a generic CSV by the sign of their amount.
/// Money spent is negative (see <see cref="GenericCsvReader"/>).
/// </summary>
public sealed class GenericStatementClassifier : IBankStatementClassifier
{
    public IReadOnlyList<ClassifiedBankEntry> Classify(
        IReadOnlyList<BankStatementEntry> entries,
        string currency)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        currency = currency.Trim().ToUpperInvariant();

        return entries.Select(entry =>
        {
            ClassifiedBankEntry Result(BankEntryKind kind, string reason) =>
                new(entry, kind, reason);

            if (!string.Equals(entry.Currency, currency, StringComparison.OrdinalIgnoreCase))
                return Result(
                    BankEntryKind.NeedsReview,
                    "Currency differs from the import currency.");

            if (entry.Amount < 0)
                return Result(BankEntryKind.ExpenseCandidate, "Outgoing amount.");

            if (entry.Amount > 0)
                return Result(BankEntryKind.Incoming, "Incoming amount; not an expense.");

            return Result(BankEntryKind.Ignored, "Zero amount.");
        }).ToArray();
    }
}
