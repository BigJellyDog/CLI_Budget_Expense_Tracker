namespace ExpenseTracker.App.Controls;

using ExpenseTracker.App.Services;

/// <summary>
/// Gives each category a color from the theme, so the list, the chart and the budgets agree.
/// The person can pick a color per category; the choice is kept in the settings file.
/// </summary>
public static class CategoryPalette
{
    public const int Count = 6;

    // The order of the swatches in the "new category" row of the design.
    private static readonly string[] Keys = ["CatFood", "CatFun", "CatPets", "CatHealth", "CatMove", "CatShop"];

    private static Dictionary<string, int> _slots = new();
    private static AppSettings? _settings;

    public static void Use(AppSettings settings) => _settings = settings;

    /// <summary>
    /// Hands out the colors in alphabetical order to the categories that have no choice yet,
    /// so that the first few categories never share a color.
    /// </summary>
    public static void Assign(IEnumerable<string> categoryNames) =>
        _slots = categoryNames
            .Distinct()
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index % Count);

    /// <summary>The color the person mixed for the category, or null when it uses the palette.</summary>
    public static Color? CustomColorOf(string categoryName)
    {
        if (_settings?.CategoryCustomColor(categoryName) is not { } hex) return null;

        try { return Color.FromArgb(hex); }
        catch { return null; }
    }

    public static int SlotOf(string categoryName)
    {
        if (_settings?.CategoryColor(categoryName) is { } chosen && chosen is >= 0 and < Count)
            return chosen;

        if (_slots.TryGetValue(categoryName, out var slot))
            return slot;

        var hash = 17;
        foreach (var c in categoryName) hash = unchecked(hash * 31 + c);
        return (hash & int.MaxValue) % Count;
    }

    public static Color ColorOfSlot(int slot) =>
        Application.Current?.Resources.TryGetValue(Keys[slot % Count], out var value) == true && value is Color color
            ? color
            : Colors.Gray;

    public static Color ColorFor(string categoryName) =>
        CustomColorOf(categoryName) ?? ColorOfSlot(SlotOf(categoryName));

    /// <summary>The slot that the fewest of the given categories use: a sensible default for a new one.</summary>
    public static int LeastUsedSlot(IEnumerable<string> categoryNames)
    {
        var used = categoryNames.Select(SlotOf).ToArray();

        return Enumerable.Range(0, Count)
            .OrderBy(slot => used.Count(x => x == slot))
            .ThenBy(slot => slot)
            .First();
    }
}
