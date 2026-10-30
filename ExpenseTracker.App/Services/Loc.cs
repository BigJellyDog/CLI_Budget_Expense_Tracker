namespace ExpenseTracker.App.Services;

using System.Globalization;

public sealed record LanguageOption(string Code, string Name);

/// <summary>
/// Texts of the screens in four languages. English text is the key, so a text with no
/// translation simply shows in English. Changing the language rebuilds the shell, so every
/// page reads its texts again.
/// </summary>
public static class Loc
{
    public static readonly IReadOnlyList<LanguageOption> Languages =
    [
        new("en", "English"),
        new("de", "Deutsch"),
        new("ru", "Русский"),
        new("uk", "Українська")
    ];

    private static readonly string[] Codes = ["en", "de", "ru", "uk"];

    private static int _index;

    public static string Code => Codes[_index];

    public static bool IsValid(string? code) => code is not null && Codes.Contains(code);

    /// <summary>Also sets the culture, so month and day names follow the language.</summary>
    public static void SetLanguage(string code)
    {
        _index = Math.Max(0, Array.IndexOf(Codes, code));

        var culture = CultureInfo.GetCultureInfo(Code);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string T(string text)
    {
        if (_index == 0 || !Translations.All.TryGetValue(text, out var row)) return text;

        var translated = row[_index - 1];
        return string.IsNullOrEmpty(translated) ? text : translated;
    }

    /// <summary>Translates the format, then fills it in.</summary>
    public static string F(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(format), args);
}

/// <summary>{loc:Tr 'Save'} in XAML.</summary>
[ContentProperty(nameof(Key))]
public sealed class TrExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
}

/// <summary>Shows an enum value or a plain name in the chosen language.</summary>
public sealed class TrConverter : IValueConverter
{
    public static readonly TrConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Loc.T(value?.ToString() ?? "");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
