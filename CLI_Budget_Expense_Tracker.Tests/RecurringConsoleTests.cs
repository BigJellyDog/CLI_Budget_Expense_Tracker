namespace CLI_Budget_Expense_Tracker.Test;

using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UI;

public sealed class RecurringConsoleTests
{
    [Test]
    public async Task Menu_CreatesGeneratesAndDeactivatesSchedule()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();

        using var input = new StringReader("""
            10
            Subscriptions
            16
            1
            3
            15,99
            01.10.2026
            Netflix
            17
            19
            generate
            18
            1
            17
            18
            0
            """);

        using var output = new StringWriter();

        var app = new ConsoleApplication(
            database.Services.GetRequiredService<IServiceScopeFactory>(),
            new AppPaths(database.DirectoryPath),
            input,
            output);

        await app.RunAsync();

        Assert.That(output.ToString(),
            Does.Contain("Recurring expense saved.")
                .And.Contain("Generated 1 recurring expense(s).")
                .And.Contain("Generated 0 recurring expense(s).")
                .And.Contain("Inactive")
                .And.Contain("No active recurring expenses."));

        Assert.That(output.ToString(), Does.Not.Contain("Error:"));

        await using var scope = database.Services.CreateAsyncScope();

        var schedule = (await scope.ServiceProvider
            .GetRequiredService<RecurringExpenseService>()
            .GetAllAsync()).Single();

        var report = await scope.ServiceProvider
            .GetRequiredService<ExpenseService>()
            .GetReportAsync();

        Assert.Multiple(() =>
        {
            Assert.That(schedule.IsActive, Is.False);
            Assert.That(
                schedule.NextDueDate,
                Is.EqualTo(new DateOnly(2026, 11, 1)));

            Assert.That(report.Expenses, Has.Count.EqualTo(1));
            Assert.That(report.Total, Is.EqualTo(15.99m));
        });

        Assert.That(
            report.Expenses.Single().RecurringExpenseId,
            Is.EqualTo(schedule.Id));
    }

    [Test]
    public async Task Menu_CancelOrEndOfInput_DoesNotSavePartialSchedule()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();

        using var input = new StringReader("""
            10
            Subscriptions
            16
            1
            0
            16
            1
            3
            10
            """);

        using var output = new StringWriter();

        var app = new ConsoleApplication(
            database.Services.GetRequiredService<IServiceScopeFactory>(),
            new AppPaths(database.DirectoryPath),
            input,
            output);

        await app.RunAsync();

        await using var scope = database.Services.CreateAsyncScope();

        var schedules = await scope.ServiceProvider
            .GetRequiredService<RecurringExpenseService>()
            .GetAllAsync();

        Assert.That(schedules, Is.Empty);
    }
}