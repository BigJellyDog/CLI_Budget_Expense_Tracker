namespace ExpenseTracker.App.Controls;

/// <summary>A color picker under a control: spectrum, brightness and a hex field.</summary>
public static class ColorPickerPopup
{
    /// <summary>Opens the color picker under the anchor and reports the chosen color. Windows only.</summary>
    public static void Show(VisualElement anchor, Color? initial, Action<Color> chosen)
    {
#if WINDOWS
        if (anchor.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement native) return;

        Platforms.Windows.PopupMenu.ShowColorPicker(native, initial, chosen);
#endif
    }
}
