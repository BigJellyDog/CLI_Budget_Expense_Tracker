namespace ExpenseTracker.App.Models;

using ExpenseTracker.App.Services;

using CLI_Budget_Expense_Tracker.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

public enum ImportFormat
{
    Revolut,
    Custom
}

public sealed record ImportFormatOption(ImportFormat Format, string Name);

public sealed record DelimiterOption(char Value, string Name);

public sealed record CultureOption(string Name, string Label);

public sealed record SignOption(bool ExpensesAreNegative, string Label);

/// <summary>How the user says a bank CSV file is written: delimiter, number and date format, columns.</summary>
public sealed class CustomCsvSettings : ObservableObject
{
    public const string NoColumn = "(none)";

    private DelimiterOption _selectedDelimiter;
    private CultureOption _selectedCulture;
    private SignOption _selectedSign;
    private string _dateFormat = "yyyy-MM-dd";
    private string _defaultCurrency = "EUR";
    private IReadOnlyList<string> _headers = [];
    private IReadOnlyList<string> _currencyHeaders = [NoColumn];
    private string? _dateColumn;
    private string? _descriptionColumn;
    private string? _amountColumn;
    private string? _currencyColumn = NoColumn;

    public CustomCsvSettings()
    {
        _selectedDelimiter = DelimiterOptions[0];
        _selectedCulture = CultureOptions[0];
        _selectedSign = SignOptions[0];
    }

    public IReadOnlyList<DelimiterOption> DelimiterOptions { get; } =
    [
        new(',', Loc.T("Comma ,")),
        new(';', Loc.T("Semicolon ;")),
        new('\t', "Tab")
    ];

    public IReadOnlyList<CultureOption> CultureOptions { get; } =
    [
        new("en-US", Loc.T("Numbers: 1,234.56")),
        new("de-DE", Loc.T("Numbers: 1.234,56")),
        new("ru-RU", Loc.T("Numbers: 1 234,56"))
    ];

    public IReadOnlyList<SignOption> SignOptions { get; } =
    [
        new(true, Loc.T("Money spent is a negative number")),
        new(false, Loc.T("Money spent is a positive number"))
    ];

    public DelimiterOption SelectedDelimiter
    {
        get => _selectedDelimiter;
        set => SetProperty(ref _selectedDelimiter, value);
    }

    public CultureOption SelectedCulture
    {
        get => _selectedCulture;
        set => SetProperty(ref _selectedCulture, value);
    }

    public SignOption SelectedSign
    {
        get => _selectedSign;
        set => SetProperty(ref _selectedSign, value);
    }

    public string DateFormat
    {
        get => _dateFormat;
        set => SetProperty(ref _dateFormat, value ?? "");
    }

    public string DefaultCurrency
    {
        get => _defaultCurrency;
        set => SetProperty(ref _defaultCurrency, value ?? "");
    }

    public IReadOnlyList<string> Headers
    {
        get => _headers;
        private set => SetProperty(ref _headers, value);
    }

    public IReadOnlyList<string> CurrencyHeaders
    {
        get => _currencyHeaders;
        private set => SetProperty(ref _currencyHeaders, value);
    }

    // A picker briefly writes null while its list is being replaced; ignore that.
    public string? SelectedDateColumn
    {
        get => _dateColumn;
        set { if (value is not null) SetProperty(ref _dateColumn, value); }
    }

    public string? SelectedDescriptionColumn
    {
        get => _descriptionColumn;
        set { if (value is not null) SetProperty(ref _descriptionColumn, value); }
    }

    public string? SelectedAmountColumn
    {
        get => _amountColumn;
        set { if (value is not null) SetProperty(ref _amountColumn, value); }
    }

    public string? SelectedCurrencyColumn
    {
        get => _currencyColumn;
        set { if (value is not null) SetProperty(ref _currencyColumn, value); }
    }

    public void UseDelimiter(char delimiter) =>
        SelectedDelimiter = DelimiterOptions.FirstOrDefault(x => x.Value == delimiter)
                            ?? DelimiterOptions[0];

    /// <summary>Fills the column pickers and preselects the columns that look right.</summary>
    public void SetHeaders(IReadOnlyList<string> headers)
    {
        var names = headers
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Headers = names;
        CurrencyHeaders = [NoColumn, .. names];

        SelectedDateColumn = Guess(names, "date", "datum", "buchungstag", "дата")
                             ?? names.FirstOrDefault();

        SelectedDescriptionColumn = Guess(
                                        names, "description", "beschreibung", "verwendungszweck",
                                        "payee", "memo", "details", "name", "описание", "назначение")
                                    ?? (names.Length > 1 ? names[1] : names.FirstOrDefault());

        SelectedAmountColumn = Guess(names, "amount", "betrag", "value", "сумма")
                               ?? names.LastOrDefault();

        SelectedCurrencyColumn = Guess(names, "currency", "währung", "валюта") ?? NoColumn;
    }

    /// <summary>Reuses the number, date and column choices from the last import.</summary>
    public void ApplySaved(CsvImportProfile profile)
    {
        SelectedCulture = CultureOptions.FirstOrDefault(x => x.Name == profile.CultureName)
                          ?? SelectedCulture;
        SelectedSign = SignOptions.First(x => x.ExpensesAreNegative == profile.ExpensesAreNegative);
        DateFormat = profile.DateFormat;
        DefaultCurrency = profile.DefaultCurrency;

        if (Headers.Contains(profile.DateColumn)) SelectedDateColumn = profile.DateColumn;
        if (Headers.Contains(profile.DescriptionColumn)) SelectedDescriptionColumn = profile.DescriptionColumn;
        if (Headers.Contains(profile.AmountColumn)) SelectedAmountColumn = profile.AmountColumn;

        if (profile.CurrencyColumn is { } currency && Headers.Contains(currency))
            SelectedCurrencyColumn = currency;
        else if (profile.CurrencyColumn is null)
            SelectedCurrencyColumn = NoColumn;
    }

    public bool TryBuildProfile(out CsvImportProfile profile, out string error)
    {
        profile = null!;

        if (string.IsNullOrWhiteSpace(DateFormat))
        {
            error = Loc.T("Enter the date format, for example yyyy-MM-dd.");
            return false;
        }

        if (SelectedDateColumn is not { } date
            || SelectedDescriptionColumn is not { } description
            || SelectedAmountColumn is not { } amount)
        {
            error = Loc.T("Choose the date, description and amount columns.");
            return false;
        }

        if (new[] { date, description, amount }.Distinct().Count() < 3)
        {
            error = Loc.T("Choose three different columns for date, description and amount.");
            return false;
        }

        var currency = DefaultCurrency.Trim().ToUpperInvariant();

        if (currency.Length != 3 || !currency.All(char.IsLetter))
        {
            error = Loc.T("Enter the currency as three letters, for example EUR.");
            return false;
        }

        profile = new CsvImportProfile(
            SelectedDelimiter.Value,
            SelectedCulture.Name,
            DateFormat.Trim(),
            date,
            description,
            amount,
            SelectedCurrencyColumn is { } column && column != NoColumn ? column : null,
            currency,
            SelectedSign.ExpensesAreNegative);

        error = "";
        return true;
    }

    private static string? Guess(IReadOnlyList<string> headers, params string[] hints) =>
        headers.FirstOrDefault(h => hints.Any(k => h.Contains(k, StringComparison.OrdinalIgnoreCase)));
}
