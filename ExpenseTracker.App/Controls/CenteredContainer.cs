namespace ExpenseTracker.App.Controls;

/// <summary>Keeps the content no wider than MaxContentWidth and next to the menu, so rows never stretch across a big monitor.</summary>
public sealed class CenteredContainer : ContentView
{
    public static readonly BindableProperty MaxContentWidthProperty = BindableProperty.Create(
        nameof(MaxContentWidth), typeof(double), typeof(CenteredContainer), 1200d);

    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (Content is not View view || width <= 0) return;

        view.HorizontalOptions = LayoutOptions.Start;
        view.WidthRequest = Math.Max(0, Math.Min(MaxContentWidth, width - Padding.HorizontalThickness));
    }
}
