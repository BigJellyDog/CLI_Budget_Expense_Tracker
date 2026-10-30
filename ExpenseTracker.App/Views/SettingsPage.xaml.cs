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

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (LanguagePicker.SelectedItem is not LanguageOption language)
            return;

        _viewModel.SelectedLanguage = language;

        if (!_viewModel.ApplyLanguage())
            return;

        // Every page reads its texts when it is built, so the shell is built again.
        Dispatcher.Dispatch(() =>
        {
            if (Window is null) return;

            var shell = new AppShell();
            Window.Page = shell;
            shell.Dispatcher.Dispatch(async () => await Shell.Current.GoToAsync("//settings"));
        });
    }

    private async void OnLoadDemoClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanLoadDemo) return;

        var confirmed = await DisplayAlertAsync(
            Loc.T("Load demo data?"),
            Loc.T("Adds a year of sample expenses, budgets, schedules and rules, counted back from today. Your data stays; a backup is made first. Loading it twice adds more expenses."),
            Loc.T("Load"),
            Loc.T("Cancel"));

        if (confirmed)
            await _viewModel.LoadDemoDataAsync();
    }

    private void OnSaveClicked(object? sender, EventArgs e) =>
        _viewModel.Save();
}
