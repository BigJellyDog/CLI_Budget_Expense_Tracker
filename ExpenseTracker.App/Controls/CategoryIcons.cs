namespace ExpenseTracker.App.Controls;

using ExpenseTracker.App.Services;

/// <summary>
/// The icons a category can wear (same 24 x 24 line style as <see cref="Icons"/>).
/// The person picks one per category; the choice is kept in the settings file.
/// </summary>
public static class CategoryIcons
{
    public const string DefaultKey = "tag";

    /// <summary>Prefix of a key that holds the path data of an icon the person imported.</summary>
    public const string CustomPrefix = "svg:";

    /// <summary>Key and SVG path data of the built-in icons, in the order the picker shows them.</summary>
    public static IReadOnlyList<(string Key, string Path)> Catalog => LucideIcons.All;

    /// <summary>The icon each standard category starts with, by its English name.</summary>
    private static readonly Dictionary<string, string> StandardKeys = new()
    {
        ["Food"] = "shopping-cart", ["Restaurants"] = "utensils", ["Transport"] = "car", ["Housing"] = "house",
        ["Utilities"] = "zap", ["Health"] = "heart-pulse", ["Entertainment"] = "film", ["Shopping"] = "shopping-bag",
        ["Clothing"] = "shirt", ["Education"] = "graduation-cap", ["Travel"] = "plane", ["Gifts"] = "gift",
        ["Pets"] = "paw-print", ["Personal care"] = "sparkles", ["Insurance"] = "shield-check", ["Sports"] = "dumbbell",
        ["Subscriptions"] = "repeat", ["Other"] = "ellipsis"
    };

    private static AppSettings? _settings;

    /// <summary>Gives the class the settings that hold the icon choices.</summary>
    public static void Use(AppSettings settings) => _settings = settings;

    /// <summary>The icon a standard category starts with, or null for any other name.</summary>
    public static string? StandardKeyOf(string englishName) =>
        StandardKeys.GetValueOrDefault(englishName);

    /// <summary>The icon of the category: the one picked, else the standard one for its name, else a tag.</summary>
    public static string KeyOf(string categoryName)
    {
        if (_settings?.CategoryIcon(categoryName) is { } chosen
            && (chosen.StartsWith(CustomPrefix, StringComparison.Ordinal) || Catalog.Any(x => x.Key == chosen)))
            return chosen;

        // A standard category from before icons existed, in the current language.
        foreach (var (english, key) in StandardKeys)
        {
            if (string.Equals(Loc.T(english), categoryName.Trim(), StringComparison.CurrentCultureIgnoreCase))
                return key;
        }

        return DefaultKey;
    }

    /// <summary>Path data of an icon key: a catalog icon, an imported one, or the tag as fallback.</summary>
    public static string PathOf(string key) =>
        key.StartsWith(CustomPrefix, StringComparison.Ordinal)
            ? key[CustomPrefix.Length..]
            : Catalog.FirstOrDefault(x => x.Key == key).Path ?? Icons.Tag;

    /// <summary>Path data of the icon of a category.</summary>
    public static string PathFor(string categoryName) => PathOf(KeyOf(categoryName));
}
