# Architecture

[Back to README](../../README.md) · [Deutsch](../de/ARCHITEKTUR.md)

## 1. Overview

The solution has three projects with one-way dependencies:

```
ExpenseTracker.App  ──►  Infrastructure  ──►  Core
        │                                       ▲
        └───────────────────────────────────────┘
```

| Project | Responsibility | Depends on |
|---------|----------------|------------|
| `CLI_Budget_Expense_Tracker.Core` | domain models, business rules, services, repository interfaces | nothing |
| `CLI_Budget_Expense_Tracker.Infrastructure` | EF Core + SQLite, migrations, repository implementations, CSV import/export, backups, recurring generator | Core |
| `ExpenseTracker.App` | .NET MAUI UI (Windows): views, view models, controls, app services | Core, Infrastructure |

The Core project has no reference to EF Core, MAUI or any UI. It can be tested without a database by
replacing the repository interfaces.

## 2. Core

**Models** (`Models/`): `Expense`, `Category`, `BudgetLimit`, `RecurringExpense`, `CategoryRule`; value and
result types such as `BudgetStatus`, `CategoryUsage`, `ExpenseReport`, `PeriodReport`, `BankStatementEntry`,
`ClassifiedBankEntry`, `BankImportResult`, `CsvImportProfile`; and `MoneyRules`.

Entities validate themselves in their constructors and methods (for example a positive amount with at most two
decimals, a non-empty name, an end date not before the start date) and throw on invalid input. A recurring
schedule knows how to advance its next due date.

**Services** (`Services/`) hold the use cases. Each takes repository interfaces and a `TimeProvider`:

| Service | Use cases |
|---------|-----------|
| `ExpenseService` | add, update, soft-delete, query with filters, build an `ExpenseReport` |
| `CategoryService` | create, rename, usage statistics, delete, move-and-delete, delete-with-data |
| `BudgetService` | set a monthly limit, status of one category, statuses of a month, delete a limit |
| `RecurringExpenseService` | create (and generate), update, deactivate, reactivate, delete |
| `ReportService` | month against the previous month, top expenses, average per day, forecast |
| `BankImportService` | save the selected records of a statement, skipping duplicates |
| `CategoryRuleService`, `CategoryRuleMatcher` | rules ("contains text → category"); longest match wins |
| `BankImportHasher` | stable SHA-256 hash per statement row (equal rows get an ordinal) |
| `ExpenseListQuery` | text, amount range and sort order applied to a list |

**Interfaces** (`Interfaces/`): `IExpenseRepository`, `ICategoryRepository`, `IBudgetRepository`,
`IRecurringExpenseRepository`, `ICategoryRuleRepository`, `IRecurringExpenseGenerator`, and the import
abstractions `IBankStatementReader` and `IBankStatementClassifier`.

## 3. Infrastructure

**Persistence.** `ExpenseTrackerDbContext` maps the entities to SQLite. Design decisions in the mapping:

- **Money is stored as whole cents** (`long`, column `…InCents`) through a value converter, so there is no
  floating-point rounding. In code, money is always `decimal`.
- **Database constraints back up the domain rules**: `CHECK` constraints for positive amounts (a budget may be
  0), a unique index on the normalized category name, a unique index on the import hash of an expense (deleted
  expenses keep their hash, so a re-import cannot bring them back), a unique
  index per category and month for budget limits, and a unique index on the normalized rule pattern.
- **Soft delete**: an expense has `IsDeleted`; a global query filter hides deleted rows. Deleted rows stay for
  history and for moving data between categories.
- **Indexes** for the common queries: by date, by category and date, and recurring schedules by activity and
  next due date.

**Migrations** (`Data/Migrations`): InitialCreate, AddRecurringExpenses, LinkExpensesToRecurringSchedules,
AddExpenseImportHash, AddRecurringEndDate, AddCategoryRules. `DatabaseInitializer` applies pending migrations at
start-up and makes a backup first when a database already exists.

**Repositories** (`Repositories/Sqlite*Repository`) implement the Core interfaces with EF Core queries.

**Import** (`Importing/`): `RevolutCsvReader` and `GenericCsvReader` parse a statement into
`BankStatementEntry` records (the generic reader takes a `CsvImportProfile`: delimiter, culture, date format,
columns, sign convention, currency). `RevolutStatementClassifier` and `GenericStatementClassifier` classify
each record as an expense candidate, incoming payment, internal transfer, ignored or needs review.

**Export** (`Exporting/`): `CsvExpenseExporter`, `MarkdownExpenseExporter`.

**Backup** (`Backup/DatabaseBackupService`): consistent snapshots with SQLite's online backup API; manual
(`backup-…`) and automatic (`auto-…`) files; a restore saves the current database first.

**Recurring generator** (`Services/SqliteRecurringExpenseGenerator`): creates every expense that is due up to
today for all active schedules and advances their next due date. It runs inside a transaction and links each
generated expense to its schedule; a unique index on schedule and date makes the run idempotent.

**Composition**: `AddExpenseTrackerSqlite(connectionString)` registers everything above in the DI container.

## 4. Application (MAUI)

### 4.1 Structure

```
ExpenseTracker.App/
├── AppShell.xaml        Shell with a locked flyout (the sidebar) and one route per screen
├── MainPage.xaml        the Expenses screen
├── Views/               the other screens (XAML + code-behind)
├── ViewModels/          one view model per screen (MVVM, CommunityToolkit.Mvvm)
├── Models/              list items and display records for the UI
├── Controls/            reusable controls (see 4.4)
├── Services/            app services (see 4.2)
├── Platforms/Windows/   native Windows code: theming of native controls, popups
└── Resources/           styles, color tokens, Manrope fonts
```

The pattern is **MVVM**. A view model holds the state and the commands of a screen, reads and writes through
Core services, and exposes display-ready values (formatted amounts, localized texts, colors). Code-behind
contains only view-specific code: opening dialogs, hover effects, navigation and handlers that need controls.

**Data access from a view model.** Each operation creates a DI scope (`IServiceScopeFactory`), resolves the
Core service, runs on a background thread (`Task.Run`) and returns plain data. UI properties are changed on the
UI thread afterwards. Every call is wrapped in error handling that logs the exception and shows a localized
message; the UI shows a busy state and disables actions while it is working.

**Navigation.** `Shell` with `FlyoutBehavior="Locked"`; the custom `Sidebar` is the flyout content and
navigates with routes (`//expenses`, `//budgets`, …). The Reports screen asks Expenses to open on one category
and month through the singleton `ExpenseFilterRequest`.

### 4.2 Application services

| Service | Role |
|---------|------|
| `AppInitializer` | once per run: database initialization (migrations), then generation of due recurring expenses; retry-safe |
| `AppSettings` | `settings.json` next to the database: currency, language, theme, accent, category colors, last CSV profile. Writes go to a temporary file first and replace the old file, so a crash cannot corrupt it |
| `ThemeService` | applies the color tokens for the chosen mode and accent at runtime |
| `Loc`, `TrExtension`, `Translations` | localization (see 4.5) |
| `FileLoggerProvider` | `ILogger` provider that writes `logs/app-YYYYMMDD.log` and deletes files older than 14 days |
| `GlobalErrorHandler` | catches unhandled exceptions, writes them to the log, shows a message instead of crashing |
| `ExportLocation` | the `exports` folder |
| `ExpenseFilterRequest` | hand-over from Reports to Expenses |

### 4.3 Theming

All colors are **tokens** (`Bg`, `Surface`, `Surface2`, `Border`, `Ink`, `InkMuted`, `Accent`, `OnAccent`,
`AccentSoft`, `Danger`, `Success`, `Warning`, `Track` and six category colors). `ThemeService` writes the values
into the application resources, and the XAML and controls use them with `DynamicResource`, so a change of mode
or accent shows at once without a restart.

- Mode: System, Light, Dark, Black (pure `#000000` background).
- Accent: Lavender, Mint, Sky, Rose, Amber; each has a dark and a light palette.
- **Native controls**: on Windows, controls such as `Entry` paint their own hover and focus colors from system
  brushes. `Platforms/Windows/NativeControlTheme` feeds the token colors into those brushes through handler
  mappers, and the title bar is colored through the window's `TitleBar`.

### 4.4 Controls

The design is built from a small set of reusable controls instead of the platform defaults:

| Control | Purpose |
|---------|---------|
| `Sidebar`, `NavItem` | the menu |
| `PillButton`, `IconButton`, `Chip` | buttons and status labels |
| `DropDown`, `DateField`, `MonthSwitch` | selection fields; each opens a themed popup below itself |
| `DonutChart`, `ComparisonBar`, `MeterBar`, `StackBar` | charts, drawn with `GraphicsView` and `IDrawable` (no chart library) |
| `IconView`, `Icons` | line icons drawn from SVG path data, colored by the theme |
| `CenteredContainer` | content area that takes the width next to the menu |
| `CategoryPalette` | the color of a category (palette slot or a custom color kept in the settings) |
| `MenuPopup`, `ColorPickerPopup` | small menus and the color picker |

`Platforms/Windows/PopupMenu` builds the popups as WinUI flyouts whose content is colored from the tokens: the
list of a drop-down, the calendar, the month grid and the color picker. They match the theme and always open
below the field, which the system `Picker` and `DatePicker` do not do.

### 4.5 Localization

English text is the key. `Loc.T("Save")` returns the translation in the current language, or the English text
when there is none. `Loc.F("{0} entries", n)` translates a format string and fills it in. In XAML:
`Text="{loc:Tr Key='Save'}"`. The translations (German, Russian, Ukrainian) live in `Services/Translations.cs`.

Changing the language saves it, sets the culture (so month and day names follow) and rebuilds the `AppShell`, so
every page reads its texts again. Numbers are parsed with the invariant culture, so the language does not
affect how amounts are entered or stored.

Texts that come from the Core project (for example validation messages of entities) are not translated.

## 5. Quality decisions

- **`decimal` for money**, whole cents in the database, constraints in the database as a second line of defense.
- **`TimeProvider` instead of `DateTime.Now`**, so time-dependent logic (due dates, "not in the future") is
  testable.
- **Duplicate-safe import** through stable transaction hashes and a unique index.
- **No silent data loss**: backups before migrations and destructive actions; deleting data needs a second
  confirmation; soft delete for expenses.
- **Errors are logged, not shown as crashes**; each screen has an error state with a retry.
- **One folder holds everything** (database, backups, exports, logs, settings), which makes the app portable and
  easy to back up.
- **Consistent code style** through `.editorconfig`; CI builds the whole solution in Release mode.

## 6. Known limits

- Windows only: the MAUI project targets `net10.0-windows10.0.19041.0`, and the popups use WinUI APIs.
- No automated tests at the moment (the earlier tests were removed when the console version was dropped and are
  in the Git history). The Core project is designed to make them straightforward.
- Texts from the Core project are in English.
- The expense list is loaded for one period at a time; there is no paging.
