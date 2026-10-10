namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

using Core.Models;
using System.Globalization;
using System.Text;




/// <summary>
/// Exports expenses to a CSV file.
/// </summary>
public class CsvExpenseExporter
{
    /// <summary>
    /// Writes the supplied expenses to a CSV file, replacing existing contents.
    /// </summary>
    /// <param name="expenses">The expenses selected for export.</param>
    /// <param name="filePath">The destination file path.</param>
    public void Export(IReadOnlyList<Expense> expenses, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // A UTF-8 BOM helps spreadsheet applications detect the encoding.
        using var writer = new StreamWriter(
            filePath,
            append: false,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        writer.NewLine = "\r\n";
        writer.WriteLine("Id,Amount,Category,Date,Description");

        foreach (var expense in expenses)
        {
            var line = string.Join(",",
                expense.Id.ToString(),
                expense.Amount.ToString(CultureInfo.InvariantCulture),
                expense.Category.ToString(),
                expense.Date.ToString("O", CultureInfo.InvariantCulture),
                EscapeCsvField(expense.Description));

            writer.WriteLine(line);
        }
    }

    /// <summary>
    /// Quotes a CSV text field and doubles any embedded quotation marks.
    /// </summary>
    /// <param name="value">The text to escape. Null is treated as empty text.</param>
    /// <returns>A quoted CSV field.</returns>
    private static string EscapeCsvField(string? value)
    {
        // Embedded quotes must be doubled inside a quoted CSV field.
        var escapedValue = (value ?? "").Replace("\"", "\"\"");

        return $"\"{escapedValue}\"";
    }
}