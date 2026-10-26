namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Puts bank records whose description contains a text into a category.
/// </summary>
public sealed class CategoryRule
{
    public Guid Id { get; private set; }

    /// <summary>The text to look for, as the user typed it.</summary>
    public string Pattern { get; private set; }

    /// <summary>The upper-case pattern used for uniqueness and matching.</summary>
    public string NormalizedPattern { get; private set; }

    public Guid CategoryId { get; private set; }

    public CategoryRule(Guid id, string pattern, Guid categoryId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Rule ID must not be empty.",
                nameof(id));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must not be empty.",
                nameof(categoryId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        Id = id;
        Pattern = pattern.Trim();
        NormalizedPattern = Pattern.ToUpperInvariant();
        CategoryId = categoryId;
    }
}
