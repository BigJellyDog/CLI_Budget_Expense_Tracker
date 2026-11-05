namespace ExpenseTracker.App.Views;

using Services;
using ViewModels;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>Creates the settings page.</summary>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the data each time the page is shown, so it is current after returning from another page.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Reload();
    }

    /// <summary>Applies the theme and accent right away.</summary>
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

    /// <summary>Switches the language of the screens.</summary>
    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (LanguagePicker.SelectedItem is not LanguageOption language)
            return;

        _viewModel.SelectedLanguage = language;

        if (!await _viewModel.ApplyLanguageAsync())
            return;

        // Every page reads its texts when it is built, so the shell is built again.
        Dispatcher.Dispatch(() =>
        {
            if (Window is null) return;

            // The culture belongs to the thread; set it again here so month names and numbers follow at once.
            Loc.SetLanguage(language.Code);

            var shell = new AppShell();
            Window.Page = shell;
            shell.Dispatcher.Dispatch(async () => await Shell.Current.GoToAsync("//settings"));
        });
    }

    /// <summary>Adds the demo data after a confirmation.</summary>
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

    /// <summary>Deletes all data after a confirmation.</summary>
    private async void OnClearClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanClear) return;

        var confirmed = await DisplayAlertAsync(
            Loc.T("Delete all data?"),
            Loc.T("Deletes every expense, budget, schedule, rule and category, and starts again with the standard categories. A backup is saved first, so you can undo this with Backup > Restore."),
            Loc.T("Delete everything"),
            Loc.T("Cancel"));

        if (confirmed)
            await _viewModel.ClearAllDataAsync();
    }

    /// <summary>Saves the form.</summary>
    private void OnSaveClicked(object? sender, EventArgs e) =>
        _viewModel.Save();
}
