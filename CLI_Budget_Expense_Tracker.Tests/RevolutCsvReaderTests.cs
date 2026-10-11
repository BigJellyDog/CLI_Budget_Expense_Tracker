namespace CLI_Budget_Expense_Tracker.Test;

using System.Globalization;
using System.Text;
using Infrastructure.Importing;

public sealed class RevolutCsvReaderTests
{
    private const string Header =
        "Art,Produkt,Datum des Beginns,Datum des Abschlusses,Beschreibung,Betrag,Gebühr,Währung,Status,Kontostand";

    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(
            Path.GetTempPath(),
            $"revolut-reader-{Guid.NewGuid():N}.csv");
    }

    [TearDown]
    public void TearDown() => File.Delete(_path);

    [Test]
    public void Read_PreservesSignedAmountsFeesAndRecordNumbers()
    {
        const string records = """
            Kartenbezahlung,Giro,2026-09-30 10:20:30,2026-10-01 10:20:30,Groceries,-12.34,0.20,EUR,ABGESCHLOSSEN,100.01
            Zinsen,Einzahlung,2026-10-02 08:00:00,2026-10-02 08:00:01,"Zinsen für Oct 2, 2026",0.01,0.01,EUR,ABGESCHLOSSEN,100.02
            """;

        File.WriteAllText(_path, Header + "\n" + records);

        var entries = new RevolutCsvReader().Read(_path);

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(2));

            Assert.That(
                entries.Select(x => x.RowNumber),
                Is.EqualTo(new[] { 2, 3 }));

            Assert.That(
                entries.Select(x => x.Amount),
                Is.EqualTo(new[] { -12.34m, 0.01m }));

            Assert.That(
                entries.Select(x => x.Fee),
                Is.EqualTo(new[] { 0.20m, 0.01m }));

            Assert.That(entries[0].Type, Is.EqualTo("Kartenbezahlung"));
            Assert.That(entries[0].Product, Is.EqualTo("Giro"));

            Assert.That(
                entries[1].Description,
                Is.EqualTo("Zinsen für Oct 2, 2026"));

            Assert.That(entries.All(x => x.Currency == "EUR"), Is.True);
        });
    }

    [Test]
    public void Read_ReorderedHeadersAndQuotedMultilineText_WorksWithGermanCultureAndBom()
    {
        // The fixed test header contains no quoted fields.
        var headers = Header.Split(',').Reverse().Append("Extra");

        var description = "Café, \"Отложил\"\nsecond line";
        var fields = ValidFields();
        fields[4] = description;

        File.WriteAllText(
            _path,
            string.Join(',', headers) + "\r\n"
                + string.Join(
                    ',',
                    fields.Reverse().Append("ignored").Select(Quote)),
            new UTF8Encoding(true));

        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture =
                CultureInfo.GetCultureInfo("de-DE");

            var entry = new RevolutCsvReader().Read(_path).Single();

            Assert.Multiple(() =>
            {
                Assert.That(entry.Description, Is.EqualTo(description));
                Assert.That(entry.Amount, Is.EqualTo(-12.34m));
                Assert.That(entry.Fee, Is.EqualTo(0.20m));

                Assert.That(
                    entry.StartedAt,
                    Is.EqualTo(new DateTime(2026, 9, 30, 10, 20, 30)));

                Assert.That(
                    entry.CompletedAt,
                    Is.EqualTo(new DateTime(2026, 10, 1, 10, 20, 30)));

                Assert.That(
                    entry.StartedAt.Kind,
                    Is.EqualTo(DateTimeKind.Unspecified));

                Assert.That(entry.Balance, Is.EqualTo(100.01m));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void Read_EmptyCompletionAndBalance_PreservesNullsAndPendingStatus()
    {
        var fields = ValidFields();
        fields[3] = "";
        fields[8] = "AUSSTEHEND";
        fields[9] = "";

        WriteFields(fields);

        var entry = new RevolutCsvReader().Read(_path).Single();

        Assert.Multiple(() =>
        {
            Assert.That(entry.CompletedAt, Is.Null);
            Assert.That(entry.Balance, Is.Null);
            Assert.That(entry.Status, Is.EqualTo("AUSSTEHEND"));
        });
    }

    [TestCase(5, "12,34", "Betrag")]
    [TestCase(5, "1,234.56", "Betrag")]
    [TestCase(6, "", "Gebühr")]
    [TestCase(2, "2026-02-30 10:20:30", "Datum des Beginns")]
    public void Read_InvalidValue_ReportsRecordAndColumn(
        int column,
        string value,
        string name)
    {
        var fields = ValidFields();
        fields[column] = value;

        WriteFields(fields);

        var error = Assert.Throws<FormatException>(
            () => new RevolutCsvReader().Read(_path));

        Assert.That(
            error!.Message,
            Does.Contain("record 2").And.Contain(name));
    }

    [Test]
    public void Read_MalformedQuotedFieldAfterValidRecord_ThrowsInsteadOfReturningPartialResult()
    {
        WriteFields(ValidFields());
        File.AppendAllText(_path, "\n\"unterminated");

        var error = Assert.Throws<FormatException>(
            () => new RevolutCsvReader().Read(_path));

        Assert.That(
            error!.Message,
            Does.Contain("record 3").And.Contain("malformed"));
    }

    [TestCase("missing")]
    [TestCase("duplicate")]
    [TestCase("empty")]
    public void Read_InvalidHeader_Throws(string defect)
    {
        var header = defect switch
        {
            "missing" => Header.Replace("Gebühr", "Fee"),
            "duplicate" => Header.Replace("Gebühr", "Betrag"),
            _ => ""
        };

        File.WriteAllText(_path, header);

        Assert.Throws<FormatException>(
            () => new RevolutCsvReader().Read(_path));
    }

    [Test]
    public void Read_WrongFieldCount_ReportsRecordNumber()
    {
        WriteFields(ValidFields()[..^1]);

        var error = Assert.Throws<FormatException>(
            () => new RevolutCsvReader().Read(_path));

        Assert.That(
            error!.Message,
            Does.Contain("record 2")
                .And.Contain("expected 10 fields, found 9"));
    }

    [Test]
    public void Read_HeaderOnly_ReturnsNoEntries()
    {
        File.WriteAllText(_path, Header);

        Assert.That(new RevolutCsvReader().Read(_path), Is.Empty);
    }

    private void WriteFields(string[] fields)
    {
        File.WriteAllText(
            _path,
            Header + "\n" + string.Join(',', fields.Select(Quote)),
            new UTF8Encoding(false));
    }

    private static string Quote(string value) =>
        "\"" + value.Replace("\"", "\"\"") + "\"";

    private static string[] ValidFields() =>
    [
        "Kartenbezahlung", "Giro",
        "2026-09-30 10:20:30", "2026-10-01 10:20:30",
        "Coffee", "-12.34", "0.20", "EUR", "ABGESCHLOSSEN", "100.01"
    ];
}