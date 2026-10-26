namespace ExpenseTracker.App.Views;

using ViewModels;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Reload();
    }

    private void OnSaveClicked(object? sender, EventArgs e) =>
        _viewModel.Save();
}
