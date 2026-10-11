namespace CLI_Budget_Expense_Tracker.Test;

using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using UI;

public sealed class ConsoleApplicationTests
{
    [Test]
    public async Task Menu_CreatesCategoryAndExpenseAndHandlesDuplicateName()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        using var input = new StringReader("1\n10\nFood\n10\n food \n1\n1\n12,50\n\nLunch\n0\n");
        using var output = new StringWriter();
        var app = new ConsoleApplication(database.Services.GetRequiredService<IServiceScopeFactory>(),
            new AppPaths(database.DirectoryPath), input, output);
        await app.RunAsync();
        Assert.That(output.ToString(), Does.Contain("No categories yet").And.Contain("already exists").And.Contain("Expense saved"));
        await using var scope = database.Services.CreateAsyncScope();
        var report = await scope.ServiceProvider.GetRequiredService<ExpenseService>().GetReportAsync();
        Assert.That(report.Total, Is.EqualTo(12.50m));
        Assert.That(report.Expenses.Single().Date, Is.EqualTo(new DateOnly(2026, 10, 15)));
    }

    [Test]
    public async Task Menu_EndOfInputDuringExpenseEntry_ExitsWithoutPartialExpense()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        using var input = new StringReader("10\nFood\n1\n1\n10\n");
        using var output = new StringWriter();
        var app = new ConsoleApplication(database.Services.GetRequiredService<IServiceScopeFactory>(),
            new AppPaths(database.DirectoryPath), input, output);
        await app.RunAsync();
        await using var scope = database.Services.CreateAsyncScope();
        Assert.That((await scope.ServiceProvider.GetRequiredService<ExpenseService>().GetReportAsync()).Expenses, Is.Empty);
    }

    [TestCase("12,50\n", "12.50")]
    [TestCase("12.50\n", "12.50")]
    [TestCase("1,234\n1.234\n-1\n0\n7.25\n", "7.25")]
    public void AmountInput_AcceptsEitherDecimalSeparatorAndRejectsAmbiguousThousands(string text, string expected)
    {
        using var reader = new StringReader(text);
        using var writer = new StringWriter();
        var amount = new ConsoleInput(reader, writer).ReadAmount(false);
        Assert.That(amount, Is.EqualTo(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)));
    }
}
