namespace CLI_Budget_Expense_Tracker.Core.Services;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Models;

/// <summary>
/// Builds stable transaction hashes so the same statement row is never imported twice.
/// </summary>
public static class BankImportHasher
{
    /// <summary>
    /// Hashes every record of one statement. Identical records inside the file
    /// get an increasing ordinal, so two equal purchases stay distinct.
    /// </summary>
    public static IReadOnlyDictionary<int, string> ComputeAll(
        IReadOnlyList<BankStatementEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var seen = new Dictionary<string, int>();
        var result = new Dictionary<int, string>();

        foreach (var entry in entries)
        {
            var key = Describe(entry);
            seen[key] = seen.GetValueOrDefault(key) + 1;

            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes($"{key}|#{seen[key]}"));

            result[entry.RowNumber] = Convert.ToHexString(bytes);
        }

        return result;
    }

    private static string Describe(BankStatementEntry entry) =>
        string.Join('|',
            entry.Type,
            entry.Product,
            entry.StartedAt.ToString("O", CultureInfo.InvariantCulture),
            entry.CompletedAt?.ToString("O", CultureInfo.InvariantCulture),
            entry.Description,
            entry.Amount.ToString(CultureInfo.InvariantCulture),
            entry.Fee.ToString(CultureInfo.InvariantCulture),
            entry.Currency,
            entry.Balance?.ToString(CultureInfo.InvariantCulture));
}
