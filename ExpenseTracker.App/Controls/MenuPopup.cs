namespace ExpenseTracker.App.Controls;

/// <summary>A small menu under a button: "Export" (CSV, Markdown, Backup), the "more" menu of a card.</summary>
public static class MenuPopup
{
    public static void Show(VisualElement anchor, IReadOnlyList<string> texts, Action<int> chosen)
    {
#if WINDOWS
        if (anchor.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement native) return;

        Platforms.Windows.PopupMenu.ShowList(native, texts, -1, chosen, 200);
#endif
    }
}
