namespace CLI_Budget_Expense_Tracker.UI;

using System.Globalization;

public sealed class ConsoleInput(TextReader reader, TextWriter writer)
{
    public string Read(string prompt)
    {
        writer.Write(prompt);
        return reader.ReadLine() ?? throw new EndOfStreamException();
    }

    public string ReadName(string prompt)
    {
        while (true)
        {
            var value = Read(prompt).Trim();
            if (value.Length > 0) return value;
            writer.WriteLine("Enter a non-empty name.");
        }
    }

    public int ReadSelection(int count)
    {
        while (true)
        {
            if (int.TryParse(Read("Number (0 to cancel): "), out var number) && number >= 0 && number <= count)
                return number;
            writer.WriteLine($"Choose a number from 0 to {count}.");
        }
    }

    public decimal ReadAmount(bool allowZero)
    {
        while (true)
        {
            // Accept either decimal separator, but never interpret it as a thousands separator.
            var text = Read(allowZero ? "Limit (0 or greater): " : "Amount: ").Trim().Replace(',', '.');
            if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var amount)
                && amount >= 0 && (allowZero || amount > 0)
                && amount <= long.MaxValue / 100m && decimal.Round(amount, 2) == amount)
                return amount;
            writer.WriteLine("Enter a valid amount with at most two decimal places (for example 12.50 or 12,50).");
        }
    }

    public DateOnly ReadDate(string prompt, DateOnly? defaultDate = null)
    {
        while (true)
        {
            var hint = defaultDate.HasValue ? $", Enter = {defaultDate:dd.MM.yyyy}" : "";
            var text = Read($"{prompt} (dd.MM.yyyy{hint}): ").Trim();
            if (text.Length == 0 && defaultDate.HasValue) return defaultDate.Value;
            if (DateOnly.TryParseExact(text, ["dd.MM.yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                return date;
            writer.WriteLine("Enter a valid date, for example 09.10.2026.");
        }
    }

    public DateOnly ReadMonth()
    {
        while (true)
        {
            var text = Read("Month (MM.yyyy): ").Trim();
            if (DateOnly.TryParseExact("01." + text, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var month))
                return month;
            writer.WriteLine("Enter a valid month, for example 10.2026.");
        }
    }

    public (DateOnly Start, DateOnly End) ReadPeriod()
    {
        var start = ReadDate("Start date");
        while (true)
        {
            var end = ReadDate("End date");
            if (end >= start) return (start, end);
            writer.WriteLine("End date cannot be earlier than start date.");
        }
    }
}
