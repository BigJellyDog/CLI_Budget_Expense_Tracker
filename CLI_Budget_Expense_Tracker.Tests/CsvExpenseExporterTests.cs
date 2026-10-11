namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;
using Infrastructure.Exporting;
using System.Text;

public sealed class CsvExpenseExporterTests
{
    [Test]
    public void Export_EscapesCategoryAndMultilineDescriptionAndSkipsDeletedExpenses()
    {
        var directory = Path.Combine(Path.GetTempPath(), "expense-csv-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "expenses.csv");
            var category = new Category("Food, \"кафе\"");
            var expense = new Expense(Guid.NewGuid(), 12.5m, category.Id, new DateOnly(2026, 10, 9), "Line 1\nLine \"2\"");
            var deleted = new Expense(Guid.NewGuid(), 1m, category.Id, expense.Date);
            deleted.MarkAsDeleted();
            new CsvExpenseExporter().Export([expense, deleted], new Dictionary<Guid, string> { [category.Id] = category.Name }, path);
            var expected = "Id,Amount,Category,Date,Description\r\n"
                + $"{expense.Id},12.50,\"Food, \"\"кафе\"\"\",2026-10-09,\"Line 1\nLine \"\"2\"\"\"\r\n";
            Assert.That(File.ReadAllText(path), Is.EqualTo(expected));
            Assert.That(File.ReadAllBytes(path).Take(3), Is.EqualTo(Encoding.UTF8.GetPreamble()));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void Export_WhenRenderingFails_PreservesPreviousReportAndRemovesTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "expense-csv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "expenses.csv");
            File.WriteAllText(path, "Previous report");
            var expense = new Expense(Guid.NewGuid(), 1m, Guid.NewGuid(), new DateOnly(2026, 10, 9));
            Assert.Throws<KeyNotFoundException>(() => new CsvExpenseExporter().Export([expense], new Dictionary<Guid, string>(), path));
            Assert.That(File.ReadAllText(path), Is.EqualTo("Previous report"));
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
        }
        finally { Directory.Delete(directory, true); }
    }
}
