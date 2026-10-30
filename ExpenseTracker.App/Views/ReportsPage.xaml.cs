namespace ExpenseTracker.App.Views;

using ExpenseTracker.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Models;
using ViewModels;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;
    private readonly IServiceProvider _services;

    public ReportsPage(ReportsViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override bool OnBackButtonPressed() =>
        _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnPreviousClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(-1);

    private async void OnNextClicked(object? sender, EventArgs e) =>
        await _viewModel.MoveMonthAsync(1);

    private async void OnMonthPicked(object? sender, DateTime month) =>
        await _viewModel.SetMonthAsync(month);

    private async void OnRefreshClicked(object? sender, EventArgs e) =>
        await _viewModel.LoadAsync();

    private void OnAmountTapped(object? sender, TappedEventArgs e) => _viewModel.SetShowShare(false);

    private void OnShareTapped(object? sender, TappedEventArgs e) => _viewModel.SetShowShare(true);

    // A click on a category opens Expenses on that category and month.
    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryReportItem item })
            await OpenExpensesAsync(item.CategoryId);
    }

    private async void OnDonutSegmentTapped(object? sender, int index)
    {
        if (index >= 0 && index < _viewModel.Categories.Count)
            await OpenExpensesAsync(_viewModel.Categories[index].CategoryId);
    }

    private async Task OpenExpensesAsync(Guid categoryId)
    {
        _services.GetRequiredService<ExpenseFilterRequest>().Set(categoryId, _viewModel.Month);
        await Shell.Current.GoToAsync("//expenses");
    }
}
