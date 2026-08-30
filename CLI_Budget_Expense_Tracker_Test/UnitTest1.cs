using NUnit.Framework.Constraints;

namespace CLI_Budget_Expense_Tracker_Test;
using CLI_Budget_Expense_Tracker;

public class Tests
{
    [Test]
    // Test for adding money
    public void AddMoneyTest()
    {
        var addedAmount = new DataLogic(0, Categories.Test, new DateTime());
        var result = addedAmount.MoneyAdded(200);
        Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(200));
                Assert.That(result, Is.GreaterThan(0));
                Console.WriteLine(addedAmount);
            }
        );
    }

    [Test]
    // Test for removing money
    public void SpendMoneyTest()
    {
        var removedAmount = new DataLogic(0, Categories.Test, new DateTime());
        var result = removedAmount.MoneySpend(200);
        Assert.That(result, Is.EqualTo(-200));
    }
    
    
    [Test]
    public void CheckIfDateAndCategoryArePresent()
    {
        var newAmount = new DataLogic(0, Categories.Test, new DateTime());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(newAmount.Date, Is.TypeOf<DateTime>());
            Assert.That(newAmount.Category, Is.EqualTo(Categories.Test));
        }
    }
}

