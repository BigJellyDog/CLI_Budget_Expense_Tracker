namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Describes how to read the records of an arbitrary bank CSV file.
/// </summary>
/// <param name="Delimiter">Character that separates the fields.</param>
/// <param name="CultureName">Culture used for numbers and dates, for example "de-DE".</param>
/// <param name="DateFormat">Exact date format, for example "dd.MM.yyyy".</param>
/// <param name="DateColumn">Header of the date column.</param>
/// <param name="DescriptionColumn">Header of the description column.</param>
/// <param name="AmountColumn">Header of the amount column.</param>
/// <param name="CurrencyColumn">Header of the currency column, or null to use the default currency.</param>
/// <param name="DefaultCurrency">Currency of records that have no currency column.</param>
/// <param name="ExpensesAreNegative">True when money spent is written as a negative number.</param>
public sealed record CsvImportProfile(
    char Delimiter,
    string CultureName,
    string DateFormat,
    string DateColumn,
    string DescriptionColumn,
    string AmountColumn,
    string? CurrencyColumn,
    string DefaultCurrency,
    bool ExpensesAreNegative);
