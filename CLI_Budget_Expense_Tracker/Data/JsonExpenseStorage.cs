namespace CLI_Budget_Expense_Tracker.Data;
using System.Text.Json;

public class JsonExpenseStorage(string filePath)
{
    public void Save(IReadOnlyList<Expense> expenses)
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

    public IReadOnlyList<Expense> Load()
    {
        if (!File.Exists(filePath)) return [];

        var json = File.ReadAllText(filePath);

        var expenses = JsonSerializer.Deserialize<List<Expense>>(json);

        if (expenses is null)
        {
            throw new JsonException("The expense file must contain an array of expenses.");
        }

        return expenses;
    }
}