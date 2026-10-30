namespace ExpenseTracker.App.Views;

using Services;
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

    private void OnAppearanceChanged(object? sender, EventArgs e)
    {
        // Read the pickers directly: the bindings may not have copied the new item yet.
        if (ModePicker.SelectedItem is not ThemeMode mode
            || AccentPicker.SelectedItem is not string accent)
        {
            return;
        }

        _viewModel.SelectedMode = mode;
        _viewModel.SelectedAccent = accent;
        _viewModel.ApplyAppearance();
    }

    private void OnSaveClicked(object? sender, EventArgs e) =>
        _viewModel.Save();
}
