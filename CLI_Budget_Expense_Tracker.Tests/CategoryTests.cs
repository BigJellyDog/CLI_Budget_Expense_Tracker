namespace CLI_Budget_Expense_Tracker.Test;

using Core.Models;

public class CategoryTests
{
    /// <summary>
    /// Verifies trimming and consistent normalization of category names.
    /// </summary>
    [TestCase("  Food  ", "Food")]
    [TestCase("food", "food")]
    [TestCase("FOOD", "FOOD")]
    public void Constructor_WithValidName_TrimsAndNormalizes(
        string name,
        string expectedName)
    {
        var category = new Category(name);

        Assert.Multiple(() =>
        {
            Assert.That(category.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(category.Name, Is.EqualTo(expectedName));
            Assert.That(category.NormalizedName, Is.EqualTo("FOOD"));
        });
    }

    /// <summary>
    /// Verifies that missing or blank names are rejected.
    /// </summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\r\n")]
    public void Constructor_WithInvalidName_Throws(string? name)
    {
        Assert.That(
            () => new Category(name!),
            Throws.InstanceOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that renaming updates both names and preserves identity.
    /// </summary>
    [Test]
    public void Rename_WithValidName_UpdatesNamesAndPreservesId()
    {
        var category = new Category("Food");
        var originalId = category.Id;

        category.Rename("  Transport  ");

        Assert.Multiple(() =>
        {
            Assert.That(category.Id, Is.EqualTo(originalId));
            Assert.That(category.Name, Is.EqualTo("Transport"));
            Assert.That(category.NormalizedName, Is.EqualTo("TRANSPORT"));
        });
    }

    /// <summary>
    /// Verifies that an invalid rename leaves the category unchanged.
    /// </summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\r\n")]
    public void Rename_WithInvalidName_ThrowsAndPreservesCategory(string? name)
    {
        var category = new Category("Food");
        var originalId = category.Id;

        Assert.That(
            () => category.Rename(name!),
            Throws.InstanceOf<ArgumentException>());

        Assert.Multiple(() =>
        {
            Assert.That(category.Id, Is.EqualTo(originalId));
            Assert.That(category.Name, Is.EqualTo("Food"));
            Assert.That(category.NormalizedName, Is.EqualTo("FOOD"));
        });
    }
}