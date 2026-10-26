namespace CLI_Budget_Expense_Tracker.Infrastructure.Exporting;

using Core.Models;
using System.Globalization;
using System.Text;

/// <summary>Exports active expenses as a Markdown table with a total row.</summary>
public sealed class MarkdownExpenseExporter
{
    public void Export(
        IReadOnlyList<Expense> expenses,
        IReadOnlyDictionary<Guid, string> categoryNames,
        string title,
        string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var active = expenses.Where(x => !x.IsDeleted).ToArray();

            using (var writer = new StreamWriter(temporaryPath, false, new UTF8Encoding(false)))
            {
                writer.NewLine = "\n";
                writer.WriteLine("# " + Escape(title));
                writer.WriteLine();
                writer.WriteLine("| Date | Category | Description | Amount |");
                writer.WriteLine("|------|----------|-------------|-------:|");
                foreach (var expense in active)
                {
                    writer.WriteLine(
                        $"| {expense.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} " +
                        $"| {Escape(categoryNames[expense.CategoryId])} " +
                        $"| {Escape(expense.Description)} " +
                        $"| {expense.Amount.ToString("F2", CultureInfo.InvariantCulture)} |");
                }

                writer.WriteLine();
                writer.WriteLine(
                    $"**Total:** {active.Sum(x => x.Amount).ToString("F2", CultureInfo.InvariantCulture)} ({active.Length} expenses)");
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static string Escape(string value) => value
        .Replace("|", "\\|")
        .Replace("\r\n", " ")
        .Replace('\n', ' ')
        .Replace('\r', ' ');
}
