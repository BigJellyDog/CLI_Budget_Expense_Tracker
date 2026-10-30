namespace ExpenseTracker.App.Controls;

/// <summary>Gives each category a stable color from the theme, so the list and the bar agree.</summary>
public static class CategoryPalette
{
    private static readonly string[] Keys = ["CatFood", "CatFun", "CatPets", "CatHealth", "CatMove"];

    private static Dictionary<string, int> _slots = new();

    /// <summary>
    /// Hands out the colors in alphabetical order, so that the first five categories never
    /// share a color. A name that is not known yet gets a color from its text.
    /// </summary>
    public static void Assign(IEnumerable<string> categoryNames) =>
        _slots = categoryNames
            .Distinct()
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index % Keys.Length);

    public static Color ColorFor(string categoryName)
    {
        if (!_slots.TryGetValue(categoryName, out var slot))
        {
            var hash = 17;
            foreach (var c in categoryName) hash = unchecked(hash * 31 + c);
            slot = (hash & int.MaxValue) % Keys.Length;
        }

        return Application.Current?.Resources.TryGetValue(Keys[slot], out var value) == true && value is Color color
            ? color
            : Colors.Gray;
    }
}
