namespace CLI_Budget_Expense_Tracker.Infrastructure.Importing;

using System.Globalization;
using System.Text;
using Core.Interfaces;
using Core.Models;
using Microsoft.VisualBasic.FileIO;

/// <summary>
/// Reads Revolut CSV statements with German column names.
/// </summary>
public sealed class RevolutCsvReader : IBankStatementReader
{
    private static readonly string[] RequiredColumns =
    [
        "Art", "Produkt", "Datum des Beginns", "Datum des Abschlusses",
        "Beschreibung", "Betrag", "Gebühr", "Währung", "Status", "Kontostand"
    ];

    public IReadOnlyList<BankStatementEntry> Read(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var parser = new TextFieldParser(
            filePath, new UTF8Encoding(false, true))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };

        parser.SetDelimiters(",");

        var headers = ReadFields(parser, 1);
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < headers.Length; index++)
        {
            var name = headers[index].Trim();

            if (name.Length == 0 || !columns.TryAdd(name, index))
                throw new FormatException(
                    "CSV contains an empty or duplicate column name.");
        }

        foreach (var name in RequiredColumns)
        {
            if (!columns.ContainsKey(name))
                throw new FormatException(
                    $"Required Revolut column is missing: {name}.");
        }

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

            string Field(string name) => fields[columns[name]];

            decimal Amount(string name)
            {
                const NumberStyles styles =
                    NumberStyles.AllowLeadingSign |
                    NumberStyles.AllowDecimalPoint;

                if (decimal.TryParse(
                    Field(name).Trim(),
                    styles,
                    CultureInfo.InvariantCulture,
                    out var value))
                {
                    return value;
                }

                throw new FormatException(
                    $"CSV record {rowNumber}: invalid number in '{name}'.");
            }

            DateTime Date(string name)
            {
                if (DateTime.TryParseExact(
                    Field(name).Trim(),
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var value))
                {
                    return value;
                }

                throw new FormatException(
                    $"CSV record {rowNumber}: invalid date in '{name}'.");
            }

            entries.Add(new BankStatementEntry(
                RowNumber: rowNumber,
                Type: Field("Art").Trim(),
                Product: Field("Produkt").Trim(),
                StartedAt: Date("Datum des Beginns"),
                CompletedAt:
                    string.IsNullOrWhiteSpace(Field("Datum des Abschlusses"))
                        ? null
                        : Date("Datum des Abschlusses"),
                Description: Field("Beschreibung"),
                Amount: Amount("Betrag"),
                Fee: Amount("Gebühr"),
                Currency: Field("Währung").Trim(),
                Status: Field("Status").Trim(),
                Balance:
                    string.IsNullOrWhiteSpace(Field("Kontostand"))
                        ? null
                        : Amount("Kontostand")));
        }

        return entries;
    }

    private static string[] ReadFields(
        TextFieldParser parser,
        int rowNumber)
    {
        try
        {
            return parser.ReadFields()
                ?? throw new FormatException(
                    $"CSV record {rowNumber} is missing.");
        }
        catch (MalformedLineException exception)
        {
            throw new FormatException(
                $"CSV record {rowNumber}: malformed quoted fields.",
                exception);
        }
    }
}