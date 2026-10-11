namespace CLI_Budget_Expense_Tracker.Infrastructure.Exporting;

using Core.Models;
using System.Globalization;
using System.Text;

/// <summary>Exports selected active expenses with category names and exact decimal amounts.</summary>
public sealed class CsvExpenseExporter
{
    public void Export(IReadOnlyList<Expense> expenses, IReadOnlyDictionary<Guid, string> categoryNames, string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var writer = new StreamWriter(temporaryPath, false, new UTF8Encoding(true)))
            {
                writer.NewLine = "\r\n";
                writer.WriteLine("Id,Amount,Category,Date,Description");
                foreach (var expense in expenses.Where(x => !x.IsDeleted))
                {
                    writer.WriteLine(string.Join(",",
                        expense.Id,
                        expense.Amount.ToString("F2", CultureInfo.InvariantCulture),
                        Escape(categoryNames[expense.CategoryId]),
                        expense.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Escape(expense.Description)));
                }
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
