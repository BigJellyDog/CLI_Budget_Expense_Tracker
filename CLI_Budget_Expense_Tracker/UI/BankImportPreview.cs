namespace CLI_Budget_Expense_Tracker.UI;

using Core.Interfaces;
using Core.Models;

/// <summary>
/// Displays a Revolut statement preview without saving expenses.
/// </summary>
public sealed class BankImportPreview(
    IBankStatementReader statementReader,
    IBankStatementClassifier classifier,
    ConsoleInput input,
    TextWriter writer)
{
    public void Run()
    {
        const string currency = "EUR";

        var path = input.Read(
            "Revolut CSV path (Enter to cancel): ").Trim();

        if (path.Length >= 2 && path[0] == '"' && path[^1] == '"')
            path = path[1..^1];

        if (path.Length == 0)
        {
            writer.WriteLine("Preview cancelled.");
            return;
        }

        var entries = statementReader.Read(path);

        if (entries.Count == 0)
        {
            writer.WriteLine("The statement contains no transactions.");
            return;
        }

        var classified = classifier.Classify(entries, currency);

        var total = classified
            .Where(x => x.Kind == BankEntryKind.ExpenseCandidate)
            .Sum(x => -x.Source.Amount);

        writer.WriteLine($"Revolut import preview ({currency})");
        writer.WriteLine($"Records: {classified.Count}");
        writer.WriteLine("Dates below are transaction completion dates.");

        WriteGroup(
            "Expense candidates",
            BankEntryKind.ExpenseCandidate,
            classified);

        WriteGroup(
            "Needs review",
            BankEntryKind.NeedsReview,
            classified);

        WriteGroup(
            "Internal transfer records",
            BankEntryKind.InternalTransfer,
            classified);

        WriteGroup(
            "Incoming records",
            BankEntryKind.Incoming,
            classified);

        WriteGroup(
            "Ignored records",
            BankEntryKind.Ignored,
            classified);

        writer.WriteLine();
        writer.WriteLine(
            $"Candidate total (fees excluded): {total:F2} {currency}");

        writer.WriteLine("Preview only. No expenses were saved.");
    }

    private void WriteGroup(
        string title,
        BankEntryKind kind,
        IReadOnlyList<ClassifiedBankEntry> entries)
    {
        var items = entries.Where(x => x.Kind == kind).ToArray();

        writer.WriteLine();
        writer.WriteLine($"{title}: {items.Length}");

        foreach (var item in items)
        {
            var entry = item.Source;
            var date = entry.CompletedAt?.ToString("dd.MM.yyyy") ?? "-";

            writer.WriteLine(
                $"#{entry.RowNumber} | {date} | " +
                $"{entry.Amount:F2} {OneLine(entry.Currency)} | " +
                $"Fee: {entry.Fee:F2} | " +
                $"{OneLine(entry.Type)} / {OneLine(entry.Product)} | " +
                OneLine(entry.Description));

            writer.WriteLine($"  {OneLine(item.Reason)}");
        }
    }

    private static string OneLine(string value) =>
        new(value.Select(character =>
            char.IsControl(character) ? ' ' : character).ToArray());
}