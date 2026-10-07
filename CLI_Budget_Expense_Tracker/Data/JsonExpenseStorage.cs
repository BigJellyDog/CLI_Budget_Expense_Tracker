namespace CLI_Budget_Expense_Tracker.Data;
using System.Text.Json;

public class JsonExpenseStorage(string filePath)
{
    private readonly string _filePath = filePath;

    public void Save(IReadOnlyList<Expense> expenses)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize((expenses, options));
        
        File.WriteAllText(_filePath, json);
    }
}