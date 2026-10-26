namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

/// <summary>Saves chosen bank statement records as expenses.</summary>
public sealed class BankImportService(
    IExpenseRepository expenses,
    ICategoryRepository categories,
    TimeProvider timeProvider)
{
    public async Task<BankImportResult> ImportAsync(
        IReadOnlyList<BankImportCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var today = DateOnly.FromDateTime(
            timeProvider.GetLocalNow().DateTime);

        var existing = await expenses.GetExistingImportHashesAsync(
            candidates.Select(x => x.Hash).ToArray(),
            cancellationToken);

        var knownCategories = (await categories.GetAllAsync(cancellationToken))
            .Select(x => x.Id)
            .ToHashSet();

        var toAdd = new List<Expense>();
        var queued = new HashSet<string>();
        var failures = new List<BankImportFailure>();
        var duplicates = 0;

        foreach (var candidate in candidates)
        {
            var source = candidate.Entry.Source;

            if (existing.Contains(candidate.Hash)
                || queued.Contains(candidate.Hash))
            {
                duplicates++;
                continue;
            }

            var amount = -source.Amount;
            var date = DateOnly.FromDateTime(
                (source.CompletedAt ?? source.StartedAt).Date);

            string? reason = null;

            if (!knownCategories.Contains(candidate.CategoryId))
                reason = "Category not found.";
            else if (amount <= 0)
                reason = "Only outgoing payments can be imported as expenses.";
            else if (decimal.Round(amount, 2) != amount)
                reason = "Amount is not a whole number of cents.";
            else if (date > today)
                reason = "Transaction date is in the future.";

            if (reason is not null)
            {
                failures.Add(new BankImportFailure(source.RowNumber, reason));
                continue;
            }

            queued.Add(candidate.Hash);

            toAdd.Add(new Expense(
                Guid.NewGuid(),
                amount,
                candidate.CategoryId,
                date,
                source.Description,
                importHash: candidate.Hash));
        }

        if (toAdd.Count > 0)
            await expenses.AddRangeAsync(toAdd, cancellationToken);

        return new BankImportResult(toAdd.Count, duplicates, failures);
    }
}
