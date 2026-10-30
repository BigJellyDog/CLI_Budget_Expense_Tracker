namespace ExpenseTracker.App.Services;

#if WINDOWS
using Microsoft.Maui.Platform;
#endif

public enum ThemeMode
{
    System,
    Light,
    Dark,

    /// <summary>Dark with a pure black background.</summary>
    Black
}

/// <summary>
/// Puts the StyleLavender tokens into the application resources. Pages and styles use them
/// with DynamicResource, so a change of mode or accent shows at once, without a restart.
/// </summary>
public sealed class ThemeService(AppSettings settings)
{
    public static readonly IReadOnlyList<string> Accents =
        ["Lavender", "Mint", "Sky", "Rose", "Amber"];

    private sealed record Palette(
        string Bg, string Surface, string Surface2, string Border,
        string Ink, string InkMuted,
        string Accent, string OnAccent, string AccentSoft, string Track);

    private sealed record Semantic(
        string Danger, string Success, string Warning,
        string CatFood, string CatFun, string CatPets, string CatHealth, string CatMove, string CatShop);

    private static readonly Semantic DarkSemantic = new(
        "#ff9b92", "#86e3b0", "#f5c76b",
        "#f5b77a", "#c9b6ff", "#7fd8e8", "#ff9fb6", "#9fc0ff", "#b5e06b");

    private static readonly Semantic LightSemantic = new(
        "#b42318", "#18794e", "#9a5b00",
        "#b45f0c", "#7a4fd0", "#0f7d90", "#c2335a", "#2f5fc4", "#4d7c0f");

    private static readonly Dictionary<string, (Palette Dark, Palette Light)> Palettes = new()
    {
        ["Lavender"] = (
            new("#15131c", "#1e1b28", "#2a2638", "#3a3550", "#f1eefa", "#aba5bf",
                "#b4a0f5", "#1a1330", "#352d55", "#2f2a40"),
            new("#f6f4fb", "#ffffff", "#eeeaf8", "#ddd7ee", "#1e1830", "#5f5878",
                "#6d4fd6", "#ffffff", "#e4dcfb", "#e4dff2")),
        ["Mint"] = (
            new("#0e1111", "#151a19", "#1c2322", "#2a3533", "#e8efed", "#93a5a0",
                "#3ddc97", "#052016", "#173a2c", "#26302e"),
            new("#f1f6f4", "#ffffff", "#e6efec", "#d0ddd9", "#14201c", "#566a64",
                "#11805a", "#ffffff", "#d5ede3", "#dbe7e3")),
        ["Sky"] = (
            new("#101722", "#17202e", "#1f2b3d", "#2f3d54", "#eef4fb", "#9fb0c6",
                "#60cdff", "#00253a", "#1f3a48", "#25324a"),
            new("#f2f6fb", "#ffffff", "#e7eef7", "#d3deec", "#14202e", "#52647a",
                "#0b6fb0", "#ffffff", "#d8eaf7", "#dde7f2")),
        ["Rose"] = (
            new("#1a1216", "#241a20", "#302229", "#47323c", "#fbeef2", "#c4a3b0",
                "#ff8fb1", "#3a0f1f", "#4a2635", "#3a2832"),
            new("#fbf3f6", "#ffffff", "#f6e6ec", "#ebd3dc", "#2a1520", "#755562",
                "#c2255c", "#ffffff", "#fadde7", "#f0dde4")),
        ["Amber"] = (
            new("#17130d", "#211b12", "#2d2518", "#453a26", "#f8f1e4", "#bfb095",
                "#f5b84b", "#2b1a00", "#44361a", "#3a3020"),
            new("#faf5ea", "#ffffff", "#f3ead6", "#e4d6b8", "#261d0e", "#6b5c40",
                "#a56200", "#ffffff", "#f6e6c4", "#eadfc6"))
    };

    private Application? _application;

    /// <summary>True while a dark palette (Dark, Black, or System following a dark system) is in use.</summary>
    public static bool IsDark { get; private set; } = true;

    /// <summary>Applies the saved choice and follows the system theme while the mode is System.</summary>
    public void Attach(Application application)
    {
        _application = application;
        application.RequestedThemeChanged += (_, _) =>
        {
            if (settings.ThemeMode == ThemeMode.System) Apply();
        };

        Apply();
    }

    public void Apply()
    {
        var application = _application;
        if (application is null) return;

        application.UserAppTheme = settings.ThemeMode switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark or ThemeMode.Black => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        var dark = settings.ThemeMode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark or ThemeMode.Black => true,
            _ => application.RequestedTheme != AppTheme.Light
        };

        IsDark = dark;

        if (!Palettes.TryGetValue(settings.Accent, out var pair))
            pair = Palettes["Lavender"];

        var palette = dark ? pair.Dark : pair.Light;

        if (settings.ThemeMode == ThemeMode.Black)
        {
            palette = palette with
            {
                Bg = "#000000",
                Surface = "#0c0c0f",
                Surface2 = "#17171c",
                Border = "#26262e",
                Track = "#1f1f27"
            };
        }

        var semantic = dark ? DarkSemantic : LightSemantic;
        var resources = application.Resources;

        void Set(string key, string hex) => resources[key] = Color.FromArgb(hex);

        Set("Bg", palette.Bg);
        Set("Surface", palette.Surface);
        Set("Surface2", palette.Surface2);
        Set("Border", palette.Border);
        Set("Ink", palette.Ink);
        Set("InkMuted", palette.InkMuted);
        Set("Accent", palette.Accent);
        Set("OnAccent", palette.OnAccent);
        Set("AccentSoft", palette.AccentSoft);
        Set("Track", palette.Track);

        Set("Danger", semantic.Danger);
        Set("Success", semantic.Success);
        Set("Warning", semantic.Warning);
        Set("CatFood", semantic.CatFood);
        Set("CatFun", semantic.CatFun);
        Set("CatPets", semantic.CatPets);
        Set("CatHealth", semantic.CatHealth);
        Set("CatMove", semantic.CatMove);
        Set("CatShop", semantic.CatShop);

#if WINDOWS
        ColorTitleBar(application, palette);
#endif
    }

#if WINDOWS
    private static void ColorTitleBar(Application application, Palette palette)
    {
        try
        {
            if (application.Windows.FirstOrDefault()?.Handler?.PlatformView
                    is not Microsoft.UI.Xaml.Window window
                || !Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                return;
            }

            var bar = window.AppWindow.TitleBar;
            var bg = Color.FromArgb(palette.Bg).ToWindowsColor();
            var ink = Color.FromArgb(palette.Ink).ToWindowsColor();
            var hover = Color.FromArgb(palette.Surface2).ToWindowsColor();

            bar.BackgroundColor = bg;
            bar.InactiveBackgroundColor = bg;
            bar.ForegroundColor = ink;
            bar.InactiveForegroundColor = Color.FromArgb(palette.InkMuted).ToWindowsColor();
            bar.ButtonBackgroundColor = bg;
            bar.ButtonInactiveBackgroundColor = bg;
            bar.ButtonForegroundColor = ink;
            bar.ButtonHoverBackgroundColor = hover;
            bar.ButtonHoverForegroundColor = ink;
            bar.ButtonPressedBackgroundColor = hover;
        }
        catch
        {
            // Coloring the title bar is cosmetic and must never stop the app.
        }
    }
#endif
}
