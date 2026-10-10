namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;
using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class ExpenseTrackerDbContextTests
{
    [Test]
    public async Task SaveChangesAsync_PreservesEntitiesAndStoresIntegerCents()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ExpenseTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ExpenseTrackerDbContext(options);

        await context.Database.MigrateAsync();

        var category = new Category("Food");
        var date = new DateOnly(2026, 10, 9);

        var expense = new Expense(
            Guid.NewGuid(),
            12.50m,
            category.Id,
            date,
            "Lunch");

        var budget = new BudgetLimit(
            Guid.NewGuid(),
            category.Id,
            new DateOnly(2026, 10, 1),
            100m);

        context.Categories.Add(category);
        context.Expenses.Add(expense);
        context.BudgetLimits.Add(budget);

        await context.SaveChangesAsync();

        // Force subsequent queries to materialize entities from the database.
        context.ChangeTracker.Clear();

        var savedCategory = await context.Categories.SingleAsync();
        var savedExpense = await context.Expenses.SingleAsync();
        var savedBudget = await context.BudgetLimits.SingleAsync();

        // Inspect the stored value without applying the EF money converter.
        await using var command = connection.CreateCommand();

        command.CommandText =
            """SELECT "AmountInCents" FROM "Expenses";""";

        var storedCents = await command.ExecuteScalarAsync();

        Assert.Multiple(() =>
        {
            Assert.That(savedCategory.Id, Is.EqualTo(category.Id));
            Assert.That(savedCategory.NormalizedName, Is.EqualTo("FOOD"));

            Assert.That(savedExpense.Id, Is.EqualTo(expense.Id));
            Assert.That(savedExpense.CategoryId, Is.EqualTo(category.Id));
            Assert.That(savedExpense.Amount, Is.EqualTo(12.50m));
            Assert.That(savedExpense.Date, Is.EqualTo(date));
            Assert.That(savedExpense.Description, Is.EqualTo("Lunch"));

            Assert.That(savedBudget.Id, Is.EqualTo(budget.Id));
            Assert.That(savedBudget.CategoryId, Is.EqualTo(category.Id));
            Assert.That(savedBudget.Month, Is.EqualTo(budget.Month));
            Assert.That(savedBudget.MonthlyAmount, Is.EqualTo(100m));

            Assert.That(storedCents, Is.TypeOf<long>());
            Assert.That(storedCents, Is.EqualTo(1250L));
        });
    }
}