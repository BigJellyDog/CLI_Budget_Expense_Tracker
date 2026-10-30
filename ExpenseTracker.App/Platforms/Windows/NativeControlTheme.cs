namespace ExpenseTracker.App.Platforms.Windows;

using ExpenseTracker.App.Services;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// WinUI controls paint their own hover, focus and pressed colors from system brushes, which
/// ignore the colors set in MAUI: a button turns grey on hover, a focused field turns white.
/// This feeds the colors of the MAUI control into those brushes, for every state.
/// </summary>
public static class NativeControlTheme
{
    // The keys are IView property names: the colors are themed again whenever one of them changes.
    private static readonly string[] ColorKeys = ["Background", "TextColor"];
    private static readonly string[] TextKeys = ["Background", "TextColor", "PlaceholderColor"];

    public static void Register()
    {
        foreach (var key in ColorKeys)
        {
            ButtonHandler.Mapper.AppendToMapping(key, (handler, view) => ThemeButton(handler.PlatformView, view));
            PickerHandler.Mapper.AppendToMapping(key, (handler, view) => ThemePicker(handler.PlatformView, view));
            DatePickerHandler.Mapper.AppendToMapping(key, (handler, view) => ThemeDatePicker(handler.PlatformView, view));
        }

        PickerHandler.Mapper.AppendToMapping("Title", (handler, view) => ThemePickerTitle(handler.PlatformView, view));
        PickerHandler.Mapper.AppendToMapping("TitleColor", (handler, view) => ThemePickerTitle(handler.PlatformView, view));

        foreach (var key in TextKeys)
        {
            EntryHandler.Mapper.AppendToMapping(key, (handler, view) =>
            {
                if (view is Microsoft.Maui.Controls.Entry entry)
                    ThemeTextBox(handler.PlatformView, entry.BackgroundColor, entry.TextColor, entry.PlaceholderColor, 20);
            });

            EditorHandler.Mapper.AppendToMapping(key, (handler, view) =>
            {
                if (view is Microsoft.Maui.Controls.Editor editor)
                    ThemeTextBox(handler.PlatformView, editor.BackgroundColor, editor.TextColor, editor.PlaceholderColor, 14);
            });
        }
    }

    private static void ThemePickerTitle(ComboBox native, object view)
    {
        if (view is not Microsoft.Maui.Controls.Picker picker) return;

        // The title is a hint inside the box, not a heading above it.
        native.Header = null;
        native.HeaderTemplate = null;
        native.PlaceholderText = picker.Title ?? "";
        native.PlaceholderForeground = new SolidColorBrush(Token("InkMuted").ToWindowsColor());
    }

    private static void ThemeButton(FrameworkElement native, object view)
    {
        if (view is not Microsoft.Maui.Controls.Button button) return;

        var back = button.BackgroundColor ?? Microsoft.Maui.Graphics.Colors.Transparent;
        var text = button.TextColor ?? Microsoft.Maui.Graphics.Colors.White;

        // A little shift toward the text color makes the hover and press visible.
        var hover = Mix(back, text, 0.10f);
        var pressed = Mix(back, text, 0.18f);
        var none = Microsoft.Maui.Graphics.Colors.Transparent;

        SetAll(native, "Button", "Background", back, hover, pressed, back);
        SetAll(native, "Button", "Foreground", text, text, text, text);
        SetAll(native, "Button", "BorderBrush", none, none, none, none);
    }

    private static void ThemePicker(FrameworkElement native, object view)
    {
        if (view is not Microsoft.Maui.Controls.Picker picker) return;

        var back = picker.BackgroundColor ?? Microsoft.Maui.Graphics.Colors.Transparent;
        var text = picker.TextColor ?? Microsoft.Maui.Graphics.Colors.White;
        var hover = Mix(back, text, 0.08f);
        var none = Microsoft.Maui.Graphics.Colors.Transparent;

        SetAll(native, "ComboBox", "Background", back, hover, hover, back);
        SetAll(native, "ComboBox", "Foreground", text, text, text, text);
        SetAll(native, "ComboBox", "BorderBrush", none, none, none, none);

        ThemeDropDown(native);

        if (native is Microsoft.UI.Xaml.Controls.Control control)
        {
            control.VerticalContentAlignment = VerticalAlignment.Center;
            control.CornerRadius = new CornerRadius(20);
        }
    }

    /// <summary>The list that opens under a Picker: the app surface, rounded, with the accent on the chosen row.</summary>
    private static void ThemeDropDown(FrameworkElement native)
    {
        var surface = Token("Surface");
        var line = Token("Border");
        var ink = Token("Ink");
        var soft = Token("AccentSoft");
        var accent = Token("Accent");
        var hover = Token("Surface2");
        var none = Microsoft.Maui.Graphics.Colors.Transparent;

        Set(native, "ComboBoxDropDownBackground", surface);
        Set(native, "ComboBoxDropDownBorderBrush", line);

        Set(native, "ComboBoxItemBackground", none);
        Set(native, "ComboBoxItemBackgroundPointerOver", hover);
        Set(native, "ComboBoxItemBackgroundPressed", hover);
        Set(native, "ComboBoxItemBackgroundSelected", soft);
        Set(native, "ComboBoxItemBackgroundSelectedPointerOver", soft);
        Set(native, "ComboBoxItemBackgroundSelectedPressed", soft);

        foreach (var state in new[] { "", "PointerOver", "Pressed", "Selected", "SelectedPointerOver", "SelectedPressed" })
            Set(native, "ComboBoxItemForeground" + state, ink);

        Set(native, "ComboBoxItemPillFillBrush", accent);
        Set(native, "ComboBoxItemPillFillBrushPressed", accent);

        native.Resources["OverlayCornerRadius"] = new CornerRadius(16);
        native.RequestedTheme = ThemeService.IsDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    private static Microsoft.Maui.Graphics.Color Token(string key) =>
        Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out var value) == true
        && value is Microsoft.Maui.Graphics.Color color
            ? color
            : Microsoft.Maui.Graphics.Colors.Gray;

    private static void ThemeDatePicker(FrameworkElement native, object view)
    {
        if (view is not Microsoft.Maui.Controls.DatePicker picker) return;

        var back = picker.BackgroundColor ?? Microsoft.Maui.Graphics.Colors.Transparent;
        var text = picker.TextColor ?? Microsoft.Maui.Graphics.Colors.White;
        var hover = Mix(back, text, 0.08f);
        var none = Microsoft.Maui.Graphics.Colors.Transparent;

        SetAll(native, "CalendarDatePicker", "Background", back, hover, hover, back);
        SetAll(native, "CalendarDatePicker", "Foreground", text, text, text, text);
        SetAll(native, "CalendarDatePicker", "BorderBrush", none, none, none, none);

        native.RequestedTheme = ThemeService.IsDark ? ElementTheme.Dark : ElementTheme.Light;

        if (native is Microsoft.UI.Xaml.Controls.Control control)
        {
            control.VerticalContentAlignment = VerticalAlignment.Center;
            control.CornerRadius = new CornerRadius(20);
        }
    }

    private static void ThemeTextBox(
        TextBox box,
        Microsoft.Maui.Graphics.Color? backColor,
        Microsoft.Maui.Graphics.Color? textColor,
        Microsoft.Maui.Graphics.Color? placeholderColor,
        double radius)
    {
        var back = backColor ?? Microsoft.Maui.Graphics.Colors.Transparent;
        var text = textColor ?? Microsoft.Maui.Graphics.Colors.White;
        var hover = back.Alpha == 0 ? back : Mix(back, text, 0.06f);
        var none = Microsoft.Maui.Graphics.Colors.Transparent;

        // A field with no fill sits inside a rounded panel of its own: it gets no ring either.
        var ring = back.Alpha == 0 ? none : Token("Accent");

        SetAll(box, "TextControl", "Background", back, hover, back, back);
        SetAll(box, "TextControl", "Foreground", text, text, text, text);
        SetAll(box, "TextControl", "BorderBrush", none, none, ring, none);

        // The default is a square box with a heavy line under it while typing.
        box.Resources["TextControlBorderThemeThickness"] = new Thickness(0);
        box.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(back.Alpha == 0 ? 0 : 1.5);
        box.BorderThickness = new Thickness(0);
        box.CornerRadius = new CornerRadius(radius);

        if (placeholderColor is not null)
            SetAll(box, "TextControl", "PlaceholderForeground",
                placeholderColor, placeholderColor, placeholderColor, placeholderColor);
    }

    /// <summary>Sets the Normal, PointerOver, Pressed/Focused and Disabled variants of a WinUI brush.</summary>
    private static void SetAll(
        FrameworkElement element,
        string control,
        string part,
        Microsoft.Maui.Graphics.Color normal,
        Microsoft.Maui.Graphics.Color pointerOver,
        Microsoft.Maui.Graphics.Color third,
        Microsoft.Maui.Graphics.Color disabled)
    {
        Set(element, control + part, normal);
        Set(element, control + part + "PointerOver", pointerOver);
        Set(element, control + part + "Pressed", third);
        Set(element, control + part + "Focused", third);
        Set(element, control + part + "Disabled", disabled);
    }

    private static void Set(FrameworkElement element, string key, Microsoft.Maui.Graphics.Color color)
    {
        var windowsColor = color.ToWindowsColor();

        // Change the brush that is already there: states already built keep pointing at it.
        if (element.Resources.TryGetValue(key, out var existing) && existing is SolidColorBrush brush)
        {
            try
            {
                brush.Color = windowsColor;
                return;
            }
            catch (UnauthorizedAccessException)
            {
                // The brush is locked while the control is being built; a new one replaces it.
            }
        }

        element.Resources[key] = new SolidColorBrush(windowsColor);
    }

    private static Microsoft.Maui.Graphics.Color Mix(
        Microsoft.Maui.Graphics.Color from,
        Microsoft.Maui.Graphics.Color to,
        float amount) =>
        new(
            from.Red + (to.Red - from.Red) * amount,
            from.Green + (to.Green - from.Green) * amount,
            from.Blue + (to.Blue - from.Blue) * amount,
            Math.Max(from.Alpha, 1f - (1f - from.Alpha) * (1f - amount)));
}
