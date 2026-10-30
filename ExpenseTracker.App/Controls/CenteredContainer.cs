namespace ExpenseTracker.App.Controls;

/// <summary>Gives the page content the whole width next to the menu (or at most MaxContentWidth), so the side column stays at the right edge.</summary>
public sealed class CenteredContainer : ContentView
{
    public static readonly BindableProperty MaxContentWidthProperty = BindableProperty.Create(
        nameof(MaxContentWidth), typeof(double), typeof(CenteredContainer), double.MaxValue);

    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (Content is not View view || width <= 0) return;

        view.HorizontalOptions = LayoutOptions.Fill;
        view.WidthRequest = Math.Max(0, Math.Min(MaxContentWidth, width - Padding.HorizontalThickness));
    }
}
