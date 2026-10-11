namespace CLI_Budget_Expense_Tracker.Infrastructure.Importing;

using Core.Interfaces;
using Core.Models;

/// <summary>
/// Classifies German Revolut records for an expense import preview.
/// </summary>
public sealed class RevolutStatementClassifier : IBankStatementClassifier
{
    public IReadOnlyList<ClassifiedBankEntry> Classify(
        IReadOnlyList<BankStatementEntry> entries,
        string currency)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        currency = currency.Trim().ToUpperInvariant();

        bool HasCurrency(BankStatementEntry entry) =>
            string.Equals(
                entry.Currency,
                currency,
                StringComparison.OrdinalIgnoreCase);

        var internalTransfers = new HashSet<BankStatementEntry>();
        var ambiguousTransfers = new HashSet<BankStatementEntry>();

        var transferGroups = entries
            .Where(x => x.Type == "Transfer"
                && x.Status == "ABGESCHLOSSEN"
                && x.CompletedAt.HasValue
                && x.CompletedAt.Value >= x.StartedAt
                && HasCurrency(x)
                && x.Amount != 0
                && !string.IsNullOrWhiteSpace(x.Product)
                && !string.IsNullOrWhiteSpace(x.Description))
            .GroupBy(x => new
            {
                x.StartedAt,
                x.CompletedAt,
                x.Description,
                Amount = Math.Abs(x.Amount)
            });

        foreach (var group in transferGroups)
        {
            var matches = group.ToArray();

            if (!matches.Any(x => x.Amount < 0)
                || !matches.Any(x => x.Amount > 0))
            {
                continue;
            }

            if (matches.Length == 2
                && matches[0].Product != matches[1].Product
                && matches.All(x => x.Fee == 0))
            {
                internalTransfers.UnionWith(matches);
            }
            else
            {
                ambiguousTransfers.UnionWith(matches);
            }
        }

        return entries.Select(entry =>
        {
            ClassifiedBankEntry Result(
                BankEntryKind kind,
                string reason) =>
                new(entry, kind, reason);

            if (entry.Status != "ABGESCHLOSSEN")
                return Result(
                    BankEntryKind.Ignored,
                    "Transaction is not completed.");

            if (!HasCurrency(entry))
                return Result(
                    BankEntryKind.NeedsReview,
                    "Currency differs from the import currency.");

            if (!entry.CompletedAt.HasValue
                || entry.CompletedAt.Value < entry.StartedAt)
            {
                return Result(
                    BankEntryKind.NeedsReview,
                    "Missing or inconsistent completion date.");
            }

            if (string.IsNullOrWhiteSpace(entry.Product))
                return Result(
                    BankEntryKind.NeedsReview,
                    "Missing Revolut product.");

            if (entry.Fee != 0)
                return Result(
                    BankEntryKind.NeedsReview,
                    "Transaction contains a separate fee.");

            if (internalTransfers.Contains(entry))
                return Result(
                    BankEntryKind.InternalTransfer,
                    "Matching transfer between Revolut products.");

            if (ambiguousTransfers.Contains(entry))
                return Result(
                    BankEntryKind.NeedsReview,
                    "Transfer has ambiguous matching records.");

            if (entry.Type is not
                ("Kartenbezahlung" or "Transfer" or "Einzahlung" or "Zinsen"))
            {
                return Result(
                    BankEntryKind.NeedsReview,
                    "Unrecognized transaction type.");
            }

            if (entry.Type == "Transfer"
                && string.IsNullOrWhiteSpace(entry.Description))
            {
                return Result(
                    BankEntryKind.NeedsReview,
                    "Transfer description is missing.");
            }

            if (entry.Amount > 0)
                return Result(
                    BankEntryKind.Incoming,
                    "Incoming amount; not an expense.");

            if (entry.Amount == 0)
                return Result(
                    BankEntryKind.Ignored,
                    "Zero amount without a fee.");

            return entry.Type switch
            {
                "Kartenbezahlung" => Result(
                    BankEntryKind.ExpenseCandidate,
                    "Outgoing card payment."),

                "Transfer" => Result(
                    BankEntryKind.ExpenseCandidate,
                    "Outgoing transfer; confirm it is an expense."),

                _ => Result(
                    BankEntryKind.NeedsReview,
                    "Unexpected outgoing amount for this transaction type.")
            };
        }).ToArray();
    }
}