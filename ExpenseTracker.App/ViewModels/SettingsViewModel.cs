namespace ExpenseTracker.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Services;

public sealed class SettingsViewModel(
    AppSettings settings,
    ThemeService theme,
    DemoDataService demoData,
    ILogger<SettingsViewModel> logger) : ObservableObject
{
    public IReadOnlyList<ThemeMode> ModeOptions { get; } = Enum.GetValues<ThemeMode>();

    public IReadOnlyList<string> AccentOptions => ThemeService.Accents;

    public IReadOnlyList<LanguageOption> LanguageOptions => Loc.Languages;

    public LanguageOption SelectedLanguage { get; set; } =
        Loc.Languages.First(x => x.Code == settings.Language);

    /// <summary>Saves the language. True when it changed and the screens must be rebuilt.</summary>
    public bool ApplyLanguage()
    {
        if (SelectedLanguage.Code == settings.Language)
            return false;

        try
        {
            settings.SetLanguage(SelectedLanguage.Code);
            Loc.SetLanguage(SelectedLanguage.Code);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save the language.");

            ErrorMessage = Loc.T("Could not save the settings. Try again.");
            SuccessMessage = "";
            NotifyState();
            return false;
        }
    }

    public ThemeMode SelectedMode { get; set; } = settings.ThemeMode;

    public string SelectedAccent { get; set; } = settings.Accent;

    public string CurrencyText { get; set; } = settings.Currency;

    /// <summary>Saves the look and shows it straight away.</summary>
    public void ApplyAppearance()
    {
        if (SelectedMode == settings.ThemeMode && SelectedAccent == settings.Accent)
            return;

        try
        {
            settings.SetAppearance(SelectedMode, SelectedAccent);
            theme.Apply();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save the appearance.");

            ErrorMessage = Loc.T("Could not save the appearance. Try again.");
            SuccessMessage = "";
            NotifyState();
        }
    }

    public bool IsBusy { get; private set; }

    public bool CanLoadDemo => !IsBusy;

    /// <summary>Adds a year of sample data. True when it worked.</summary>
    public async Task<bool> LoadDemoDataAsync()
    {
        if (IsBusy) return false;

        IsBusy = true;
        ErrorMessage = "";
        SuccessMessage = "";
        NotifyState();

        try
        {
            var result = await demoData.LoadAsync();

            SuccessMessage = Loc.F("Demo data added: {0} expenses, {1} schedules.", result.Expenses, result.Schedules);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load the demo data.");

            ErrorMessage = Loc.T("Could not load the demo data. Nothing was lost; a backup was made first.");
            return false;
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    public string DataFolder =>
        Path.GetDirectoryName(Path.GetFullPath(settings.FilePath)) ?? "";

    public string ErrorMessage { get; private set; } = "";

    public string SuccessMessage { get; private set; } = "";

    public bool HasError => ErrorMessage.Length > 0;

    public bool HasSuccess => SuccessMessage.Length > 0;

    public void Reload()
    {
        CurrencyText = settings.Currency;
        SelectedMode = settings.ThemeMode;
        SelectedAccent = settings.Accent;
        SelectedLanguage = Loc.Languages.First(x => x.Code == settings.Language);
        ErrorMessage = "";
        SuccessMessage = "";

        OnPropertyChanged(nameof(SelectedMode));
        OnPropertyChanged(nameof(SelectedAccent));
        OnPropertyChanged(nameof(SelectedLanguage));
        NotifyState();
    }

    public void Save()
    {
        var code = (CurrencyText ?? "").Trim().ToUpperInvariant();

        if (!AppSettings.IsValidCurrency(code))
        {
            ErrorMessage = Loc.T("Enter the currency as three letters, for example EUR.");
            SuccessMessage = "";
            NotifyState();
            return;
        }

        try
        {
            settings.SetCurrency(code);

            CurrencyText = code;
            ErrorMessage = "";
            SuccessMessage =
                Loc.F("Saved. Amounts are shown in {0}. Amounts already saved are not converted.", code);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save the settings.");

            ErrorMessage = Loc.T("Could not save the settings. Try again.");
            SuccessMessage = "";
        }

        NotifyState();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(CurrencyText));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanLoadDemo));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(SuccessMessage));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasSuccess));
    }
}
