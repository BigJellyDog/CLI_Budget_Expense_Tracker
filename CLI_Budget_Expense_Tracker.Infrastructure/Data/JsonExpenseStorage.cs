namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

using Core.Models;
using System.Text.Json;


public class JsonExpenseStorage(string filePath)
{
    public void Save(IReadOnlyList<LegacyExpense> expenses)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(expenses, options);
        
        File.WriteAllText(filePath, json);
    }

    public IReadOnlyList<LegacyExpense> Load()
    {
        if (!File.Exists(filePath)) return [];

        var json = File.ReadAllText(filePath);

        var expenses = JsonSerializer.Deserialize<List<LegacyExpense>>(json);

        if (expenses is null)
        {
            throw new JsonException("The expense file must contain an array of expenses.");
        }

        return expenses;
    }
}