namespace ExpenseTracker.App.Services;

using System.Text.Json;
using CLI_Budget_Expense_Tracker.Core.Models;

public sealed record SettingsData(
    string Currency = "EUR",
    CsvImportProfile? LastCsvProfile = null,
    ThemeMode ThemeMode = ThemeMode.Black,
    string Accent = "Mint",
    string Language = "en",
    Dictionary<string, int>? CategoryColors = null,
    Dictionary<string, string>? CategoryHex = null,
    bool StandardCategoriesSeeded = false);

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

    /// <summary>Light, dark, or follow the system.</summary>
    public ThemeMode ThemeMode => _data.ThemeMode;

    /// <summary>Name of the accent palette, one of <see cref="ThemeService.Accents"/>.</summary>
    public string Accent => _data.Accent;

    /// <summary>Code of the language of the screens: en, de, ru or uk.</summary>
    public string Language => _data.Language;

    /// <summary>True once the standard categories were offered to this database (also when it had its own).</summary>
    public bool StandardCategoriesSeeded => _data.StandardCategoriesSeeded;

    /// <summary>The color slot (0-5) the person picked for a category, by category name.</summary>
    public int? CategoryColor(string name) =>
        _data.CategoryColors is { } map && map.TryGetValue(name.Trim().ToUpperInvariant(), out var slot) ? slot : null;

    /// <summary>A color the person mixed for a category, as #RRGGBB, or null.</summary>
    public string? CategoryCustomColor(string name) =>
        _data.CategoryHex is { } map && map.TryGetValue(name.Trim().ToUpperInvariant(), out var hex) ? hex : null;

    public static bool IsValidCurrency(string? code) =>
        code is { Length: 3 } && code.All(char.IsLetter);

    public void SetCurrency(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();

        if (!IsValidCurrency(code))
            throw new ArgumentException("Currency must be three letters.", nameof(code));

        Save(_data with { Currency = code });
    }

    public void SetAppearance(ThemeMode mode, string accent)
    {
        if (!ThemeService.Accents.Contains(accent))
            throw new ArgumentException("Unknown accent.", nameof(accent));

        Save(_data with { ThemeMode = mode, Accent = accent });
    }

    public void SetLanguage(string code)
    {
        if (!Loc.IsValid(code))
            throw new ArgumentException("Unknown language.", nameof(code));

        Save(_data with { Language = code });
    }

    /// <summary>All categories are gone: forgets their colors, so the standard ones can be offered again.</summary>
    public void ForgetCategories() =>
        Save(_data with { CategoryColors = null, CategoryHex = null, StandardCategoriesSeeded = false });

    public void MarkStandardCategoriesSeeded() => Save(_data with { StandardCategoriesSeeded = true });

    /// <summary>The category uses a color of the palette; a custom color it had is dropped.</summary>
    public void SetCategoryColor(string name, int slot)
    {
        var key = name.Trim().ToUpperInvariant();
        var map = new Dictionary<string, int>(_data.CategoryColors ?? []) { [key] = slot };
        var hex = new Dictionary<string, string>(_data.CategoryHex ?? []);
        hex.Remove(key);

        Save(_data with { CategoryColors = map, CategoryHex = hex });
    }

    /// <summary>The category uses its own color; the palette choice it had is dropped.</summary>
    public void SetCategoryCustomColor(string name, string hex)
    {
        var key = name.Trim().ToUpperInvariant();
        var map = new Dictionary<string, int>(_data.CategoryColors ?? []);
        map.Remove(key);
        var colors = new Dictionary<string, string>(_data.CategoryHex ?? []) { [key] = hex };

        Save(_data with { CategoryColors = map, CategoryHex = colors });
    }

    public void RenameCategoryColor(string oldName, string newName)
    {
        var map = new Dictionary<string, int>(_data.CategoryColors ?? []);
        var hex = new Dictionary<string, string>(_data.CategoryHex ?? []);
        var from = oldName.Trim().ToUpperInvariant();
        var to = newName.Trim().ToUpperInvariant();

        if (map.Remove(from, out var slot)) map[to] = slot;
        if (hex.Remove(from, out var custom)) hex[to] = custom;

        Save(_data with { CategoryColors = map, CategoryHex = hex });
    }

    public void RemoveCategoryColor(string name)
    {
        var map = new Dictionary<string, int>(_data.CategoryColors ?? []);

        if (map.Remove(name.Trim().ToUpperInvariant()))
            Save(_data with { CategoryColors = map });
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

            return data is not null
                   && IsValidCurrency(data.Currency)
                   && ThemeService.Accents.Contains(data.Accent)
                   && Loc.IsValid(data.Language)
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
