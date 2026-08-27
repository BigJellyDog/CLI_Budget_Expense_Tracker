namespace CLI_Budget_Expense_Tracker_Test;
using CLI_Budget_Expense_Tracker;

public class Tests
{
    [Test]
    public void AddMoneyTest()
    {
        var currentAmount = new DataLogic();

        double result = currentAmount.AddMoneyToAccount(200);

        Assert.That(result, Is.EqualTo(200));
    }
    
    
}

