using System.Text.Json;
using CLI_Budget_Expense_Tracker.Models;


/// <summary>
/// Saves and loads category budget limits using a JSON file.
/// </summary>
/// <param name="filePath">The path to the budget limits file.</param>
public class JsonBudgetStorage(string filePath)
{
    /// <summary>
    /// Saves the supplied budget limits to the file.
    /// Replaces the existing file contents.
    /// </summary>
    /// <param name="limits">The budget limits to save.</param>
    public void Save(IReadOnlyList<BudgetLimit> limits)
    {
        var directory = Path.GetDirectoryName(filePath);

        // Create the storage directory if it does not exist yet.
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(limits, options);

        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads budget limits from the file.
    /// </summary>
    /// <returns>
    /// The stored budget limits, or an empty collection if the file is missing.
    /// </returns>
    /// <exception cref="JsonException">
    /// The file contains invalid JSON, incompatible data, or a null root value.
    /// </exception>
    public IReadOnlyList<BudgetLimit> Load()
    {
        // A missing file is expected on the first run.
        if (!File.Exists(filePath)) return [];

        var json = File.ReadAllText(filePath);

        var limits = JsonSerializer.Deserialize<List<BudgetLimit>>(json);

        // A JSON null value is not a valid collection of budget limits.
        if (limits is null)
        {
            throw new JsonException(
                "The budget file must contain an array of budget limits.");
        }

        return limits;
    }
}