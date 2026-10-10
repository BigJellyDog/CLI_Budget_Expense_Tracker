namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Represents an expense legacyCategory with a stable identifier.
/// </summary>
public sealed class Category
{
    /// <summary>
    /// Gets the unique legacyCategory identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the legacyCategory name displayed to the user.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the normalized name used for uniqueness checks.
    /// </summary>
    public string NormalizedName { get; private set; }

    /// <summary>
    /// Creates a legacyCategory with a valid, trimmed name.
    /// </summary>
    public Category(string name)
    {
        Name = ValidateName(name);
        NormalizedName = Name.ToUpperInvariant();
    }

    /// <summary>
    /// Renames the legacyCategory while preserving its identifier.
    /// </summary>
    public void Rename(string name)
    {
        // Validate before changing the existing legacyCategory.
        var validatedName = ValidateName(name);

        Name = validatedName;
        NormalizedName = validatedName.ToUpperInvariant();
    }

    /// <summary>
    /// Rejects missing or blank names and removes surrounding whitespace.
    /// </summary>
    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return name.Trim();
    }
}