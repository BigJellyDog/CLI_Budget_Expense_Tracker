namespace CLI_Budget_Expense_Tracker.Test;
using Infrastructure.Data;
using Core.Models;



/// <summary>
/// Verifies expense persistence using JSON files.
/// </summary>
[TestFixture]
public class JsonExpenseStorageTests
{
    private string _testDirectory = string.Empty;
    private string _filePath = string.Empty;

    /// <summary>
    /// Creates an isolated temporary directory before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"ExpenseTrackerTests_{Guid.NewGuid()}");

        Directory.CreateDirectory(_testDirectory);

        _filePath = Path.Combine(_testDirectory, "expenses.json");
    }

    /// <summary>
    /// Removes the temporary directory and its files after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that loading a missing file returns an empty collection.
    /// </summary>
    [Test]
    public void Load_WhenFileDoesNotExist_ReturnsEmptyCollection()
    {
        // Arrange: use a path where no expense file has been created.
        var storage = new JsonExpenseStorage(_filePath);

        // Act: attempt to load expenses.
        var result = storage.Load();

        // Assert: a missing file should represent an empty expense collection.
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Verifies that saving and loading preserves every expense field.
    /// </summary>
    [Test]
    public void SaveAndLoad_WithExpenses_PreservesAllFields()
    {
        // Arrange: create expenses with different values and fixed timestamps.
        var storage = new JsonExpenseStorage(_filePath);

        IReadOnlyList<Expense> expenses =
        [
            new Expense(12.50m, Categories.Food, "Lunch")
            {
                Date = new DateTime(2026, 10, 5, 14, 30, 15)
            },
            new Expense(5.75m, Categories.Transportation, "Билет в Köln")
            {
                Date = new DateTime(2026, 10, 6, 9, 10, 20)
            }
        ];

        // Act: save expenses and load them through a new storage instance.
        storage.Save(expenses);

        var restoredStorage = new JsonExpenseStorage(_filePath);
        var result = restoredStorage.Load();

        // Check the count before accessing individual elements.
        Assert.That(result, Has.Count.EqualTo(expenses.Count));

        // Assert: compare every stored field for each expense.
        Assert.Multiple(() =>
        {
            for (var i = 0; i < expenses.Count; i++)
            {
                var expected = expenses[i];
                var actual = result[i];

                Assert.That(actual.Id, Is.EqualTo(expected.Id));
                Assert.That(actual.Amount, Is.EqualTo(expected.Amount));
                Assert.That(actual.Category, Is.EqualTo(expected.Category));
                Assert.That(actual.Date, Is.EqualTo(expected.Date));
                Assert.That(actual.Description, Is.EqualTo(expected.Description));
            }
        });
    }
}