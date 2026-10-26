namespace CLI_Budget_Expense_Tracker.Infrastructure.Importing;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Models;
using Microsoft.VisualBasic.FileIO;

/// <summary>
/// Reads bank statements from any CSV file, using a profile that maps its columns.
/// Amounts are normalized so that money spent is always negative.
/// </summary>
public sealed class GenericCsvReader
{
    private static readonly char[] DelimiterCandidates = [',', ';', '\t'];

    /// <summary>Guesses the delimiter from the header line.</summary>
    public static char DetectDelimiter(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var firstLine = Decode(content).Split('\n', 2)[0];

        return DelimiterCandidates
            .OrderByDescending(c => firstLine.Count(x => x == c))
            .First();
    }

    /// <summary>Returns the column names of the first record.</summary>
    public IReadOnlyList<string> ReadHeaders(byte[] content, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var parser = CreateParser(content, delimiter);

        return ReadFields(parser, 1)
            .Select(x => x.Trim())
            .ToArray();
    }

    public IReadOnlyList<BankStatementEntry> Read(byte[] content, CsvImportProfile profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);

        var culture = CultureInfo.GetCultureInfo(profile.CultureName);
        var currency = profile.DefaultCurrency.Trim().ToUpperInvariant();

        using var parser = CreateParser(content, profile.Delimiter);

        var headers = ReadFields(parser, 1)
            .Select(x => x.Trim())
            .ToArray();

        var columns = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < headers.Length; index++)
            columns.TryAdd(headers[index], index);

        int Column(string name) =>
            columns.TryGetValue(name, out var index)
                ? index
                : throw new FormatException($"Column not found in the file: {name}.");

        var dateColumn = Column(profile.DateColumn);
        var descriptionColumn = Column(profile.DescriptionColumn);
        var amountColumn = Column(profile.AmountColumn);
        var currencyColumn = profile.CurrencyColumn is null
            ? (int?)null
            : Column(profile.CurrencyColumn);

        var entries = new List<BankStatementEntry>();

        // Count CSV records, including the header, rather than physical text lines.
        var rowNumber = 1;

        while (!parser.EndOfData)
        {
            rowNumber++;
            var fields = ReadFields(parser, rowNumber);

            if (fields.Length != headers.Length)
                throw new FormatException(
                    $"CSV record {rowNumber}: expected {headers.Length} fields, found {fields.Length}.");

            var amountText = new string(fields[amountColumn]
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());

            if (!IsWellFormedAmount(amountText, culture)
                || !decimal.TryParse(
                    amountText,
                    NumberStyles.AllowLeadingSign
                    | NumberStyles.AllowDecimalPoint
                    | NumberStyles.AllowThousands,
                    culture,
                    out var amount))
            {
                throw new FormatException(
                    $"CSV record {rowNumber}: '{fields[amountColumn].Trim()}' is not a valid amount for the chosen number format.");
            }

            if (!profile.ExpensesAreNegative) amount = -amount;

            var dateText = fields[dateColumn].Trim();

            if (!DateTime.TryParseExact(
                    dateText,
                    profile.DateFormat,
                    culture,
                    DateTimeStyles.None,
                    out var date))
            {
                throw new FormatException(
                    $"CSV record {rowNumber}: '{dateText}' does not match the date format {profile.DateFormat}.");
            }

            var rowCurrency = currencyColumn is { } index
                              && !string.IsNullOrWhiteSpace(fields[index])
                ? fields[index].Trim().ToUpperInvariant()
                : currency;

            entries.Add(new BankStatementEntry(
                RowNumber: rowNumber,
                Type: "CSV",
                Product: "",
                StartedAt: date,
                CompletedAt: date,
                Description: fields[descriptionColumn].Trim(),
                Amount: amount,
                Fee: 0m,
                Currency: rowCurrency,
                Status: "COMPLETED",
                Balance: null));
        }

        return entries;
    }

    /// <summary>
    /// Accepts only amounts written the way the culture writes them. Without this check
    /// a German "12.50" would be read as 1250, because "." groups thousands there.
    /// </summary>
    private static bool IsWellFormedAmount(string text, CultureInfo culture)
    {
        var format = culture.NumberFormat;

        text = text.Replace(format.NegativeSign, "-");

        var group = string.IsNullOrWhiteSpace(format.NumberGroupSeparator)
            ? null
            : Regex.Escape(format.NumberGroupSeparator);

        var decimals = Regex.Escape(format.NumberDecimalSeparator);

        var integer = group is null
            ? @"\d+"
            : $@"(?:\d{{1,3}}(?:{group}\d{{3}})+|\d+)";

        return Regex.IsMatch(text, $@"^[+-]?{integer}(?:{decimals}\d+)?$");
    }

    private static TextFieldParser CreateParser(byte[] content, char delimiter)
    {
        var parser = new TextFieldParser(new StringReader(Decode(content)))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };

        parser.SetDelimiters(delimiter.ToString());

        return parser;
    }

    /// <summary>Reads UTF-8, and falls back to Latin-1 for older bank exports.</summary>
    private static string Decode(byte[] content)
    {
        string text;

        try
        {
            text = new UTF8Encoding(false, true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            text = Encoding.Latin1.GetString(content);
        }

        return text.TrimStart('﻿');
    }

    private static string[] ReadFields(TextFieldParser parser, int rowNumber)
    {
        if (parser.EndOfData)
        {
            throw new FormatException(
                rowNumber == 1
                    ? "The file is empty."
                    : $"CSV record {rowNumber} is missing.");
        }

        try
        {
            return parser.ReadFields()
                ?? throw new FormatException($"CSV record {rowNumber} is missing.");
        }
        catch (MalformedLineException exception)
        {
            throw new FormatException(
                $"CSV record {rowNumber}: malformed quoted fields.",
                exception);
        }
    }
}
