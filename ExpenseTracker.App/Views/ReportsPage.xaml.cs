namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Models;
using ViewModels;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;
    private readonly IServiceProvider _services;

    /// <summary>Creates the reports page: the month by category and the comparison with the month before.</summary>
    public ReportsPage(ReportsViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    /// <summary>Loads the data each time the page is shown, so it is current after returning from another page.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    /// <summary>Ignores the back button while something is being saved or loaded.</summary>
    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    /// <summary>Shows the previous month.</summary>
    private async void OnPreviousClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(-1);

    /// <summary>Shows the next month.</summary>
    private async void OnNextClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(1);

    /// <summary>Shows the month chosen in the month picker.</summary>
    private async void OnMonthPicked(object? sender, DateTime month) =>
        await _viewModel.SetMonthAsync(month);

    /// <summary>Reloads the data from the database.</summary>
    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    /// <summary>Shows the amounts of the categories.</summary>
    private void OnAmountTapped(object? sender, TappedEventArgs e) => _viewModel.SetShowShare(false);

    /// <summary>Shows the shares of the categories.</summary>
    private void OnShareTapped(object? sender, TappedEventArgs e) => _viewModel.SetShowShare(true);

    // A click on a category opens Expenses on that category and month.
    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryReportItem item })
            await OpenExpensesAsync(item.CategoryId);
    }

    /// <summary>Opens the expenses of the clicked category.</summary>
    private async void OnDonutSegmentTapped(object? sender, int index)
    {
        if (index >= 0 && index < _viewModel.Categories.Count)
            await OpenExpensesAsync(_viewModel.Categories[index].CategoryId);
    }

    /// <summary>Opens the expenses page filtered by the category and the shown month.</summary>
    private async Task OpenExpensesAsync(Guid categoryId)
    {
        _services.GetRequiredService<ExpenseFilterRequest>().Set(categoryId, _viewModel.Month);
        await Shell.Current.GoToAsync("//expenses");
    }
}
