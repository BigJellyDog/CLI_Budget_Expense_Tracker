# Development guide

[Back to README](../../README.md) · [Deutsch](../de/ENTWICKLUNG.md)

## 1. Setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Install the MAUI workload: `dotnet workload install maui`.
3. Clone the repository and restore: `dotnet restore CLI_Budget_Expense_Tracker.sln`.

## 2. Build and run

```powershell
# build everything (what the CI does)
dotnet build CLI_Budget_Expense_Tracker.sln -c Release

# run the app
dotnet run --project ExpenseTracker.App -f net10.0-windows10.0.19041.0
```

In Rider or Visual Studio: open the solution, select `ExpenseTracker.App`, Run.

A running app locks `ExpenseTracker.App.exe`; stop it before building again, or the build ends with a "file is
being used by another process" error.

The app writes a harmless line such as `Error loading font 'ms-appx:///Nunito-….ttf'` to the log at start-up.
The fonts are registered correctly and render as expected.

## 3. Where the app keeps its data while you develop

The app looks for the `.sln` file above its output folder and keeps the database, backups, exports, logs and
`settings.json` next to it. All of them are ignored by Git. To start from a clean state, close the app and delete
`expense-tracker.db*`; it is created again on the next start.

## 4. Database migrations

Restore the EF Core tool from the local manifest in `.config` once: `dotnet tool restore`
(or install it globally with `dotnet tool install --global dotnet-ef`).

```powershell
# add a migration after changing a model or the DbContext
dotnet ef migrations add <Name> --project CLI_Budget_Expense_Tracker.Infrastructure --output-dir Data/Migrations
```

Migrations are applied when the app starts. If a database file already exists, a backup (`auto-…`) is made first.
When you change an entity, also keep the domain validation in the Core model and the database constraint in the
`DbContext` in agreement.

## 5. Conventions

- **Layering**: Core must not reference EF Core, MAUI or UI types. Infrastructure implements Core interfaces.
  The app talks to Core services, never to the `DbContext`.
- **Money**: `decimal` in code, whole cents in the database. Use `MoneyRules` for validation.
- **Time**: inject `TimeProvider`; do not call `DateTime.Now` or `DateTime.Today` in logic.
- **Parsing user input**: amounts are parsed with the invariant culture after replacing a comma with a dot.
  Do not parse amounts with the current culture; the UI language changes the culture.
- **MVVM**: state and commands in the view model; code-behind only for view concerns. Run data access on a
  background thread and change bound properties on the UI thread.
- **Errors**: catch at the operation level, log with `ILogger`, show a localized message. Never let an exception
  end the app.
- **Style**: `.editorconfig` defines formatting; keep comments short and explain *why*, not *what*.
- **Commits**: small, one topic each, with a message that says what and why (`feat:`, `fix:`, `style:`, `docs:`).

## 6. How to extend

### Add a screen

1. Create `ViewModels/<Name>ViewModel.cs` (derive from `ObservableObject`) and `Views/<Name>Page.xaml` + code-behind.
2. Register both in `MauiProgram` (`AddTransient`).
3. Add a `ShellContent` with a route in `AppShell.xaml` and an entry in `Controls/Sidebar.cs` (route, text, icon).
4. Use `controls:CenteredContainer` as the content root and the shared header pattern (title with a subtitle on the
   left, actions on the right).

### Add or change a text

Write the English text in code or XAML (`{loc:Tr Key='…'}`) and add a row to `Services/Translations.cs`:

```csharp
["Save changes"] = ["Änderungen speichern", "Сохранить изменения", "Зберегти зміни"],
```

The order is German, Russian, Ukrainian. A text without a row shows in English. For a text with values, use a
format string with placeholders (`Loc.F("{0} of {1}", a, b)`), and translate the whole format string.

### Add a language

Add the code to `Loc` (`Languages` and `Codes`), add a column to every row of `Translations` (and to the
`Loc.T` index logic), and offer it in Settings through `Loc.Languages`.

### Add an accent or a theme color

Accents are entries of the `Palettes` dictionary in `Services/ThemeService.cs` (a dark and a light palette each)
and are listed in `ThemeService.Accents`. A new color token must be defined in `Resources/Styles/Colors.xaml`
(default value) and set in `ThemeService.Apply`, then used with `{DynamicResource Token}`.

### Add a column or a table

Change the model in Core, map it in `ExpenseTrackerDbContext` (with constraints), add a migration (section 4), and
extend the repository and the service.

### Support another bank

Implement `IBankStatementReader` and `IBankStatementClassifier` in Infrastructure, register them, and add a
format option to `BankImportViewModel.Formats`. Banks with a plain table need no code: the generic reader maps
the columns in the UI.

## 7. Continuous integration

`.github/workflows/build.yml` builds the solution in Release on `windows-latest` for each push to `master` and each
pull request. It installs the MAUI workload first.

## 8. Status and roadmap

Done: the features in the [user guide](USER_GUIDE.md), error logging, backups, CI build, localization, themes
and the redesigned screens.

Open:

- [ ] Automated tests (Core services first: money rules, budget status, recurring generation, import hash and
      classification, rule matching) and running them in CI.
- [ ] Windows release build (packaging).
- [ ] Translating the messages that come from the Core project.

## 9. Building the release zip

`.\tools\pack-release.ps1 -Version 1.2.0` publishes the app self-contained into an `app` folder, builds a small native
starter (`tools/Launcher`, Native AOT, about 1.5 MB) as `ExpenseTracker.exe`, adds `info` (readme, license, notices) and
writes `artifacts/ExpenseTracker-v<version>-win-x64.zip`. The root of the zip then holds only the starter and two folders.
The starter needs the Visual Studio C++ tools (the "Desktop development with C++" workload) to link.
