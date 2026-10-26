namespace ExpenseTracker.App.Services;

using System.Text.Json;
using CLI_Budget_Expense_Tracker.Core.Models;

public sealed record SettingsData(
    string Currency = "EUR",
    CsvImportProfile? LastCsvProfile = null);

/// <summary>
/// User settings kept in a small JSON file next to the database.
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly object _gate = new();

    private SettingsData _data;

    public AppSettings(string filePath)
    {
        FilePath = filePath;
        _data = Read();
    }

    public string FilePath { get; }

    /// <summary>Three-letter code shown next to amounts. Saved amounts are never converted.</summary>
    public string Currency => _data.Currency;

    /// <summary>The column choices of the last CSV import of another bank.</summary>
    public CsvImportProfile? LastCsvProfile => _data.LastCsvProfile;

    public static bool IsValidCurrency(string? code) =>
        code is { Length: 3 } && code.All(char.IsLetter);

    public void SetCurrency(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();

        if (!IsValidCurrency(code))
            throw new ArgumentException("Currency must be three letters.", nameof(code));

        Save(_data with { Currency = code });
    }

    public void SetLastCsvProfile(CsvImportProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        Save(_data with { LastCsvProfile = profile });
    }

    private SettingsData Read()
    {
        try
        {
            if (!File.Exists(FilePath)) return new SettingsData();

            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath));

            return data is not null && IsValidCurrency(data.Currency)
                ? data
                : new SettingsData();
        }
        catch
        {
            // A damaged settings file must not stop the app; defaults are used.
            return new SettingsData();
        }
    }

    private void Save(SettingsData data)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);

            var temporaryPath = FilePath + ".tmp";

            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, JsonOptions));
                File.Move(temporaryPath, FilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }

            _data = data;
        }
    }
}
