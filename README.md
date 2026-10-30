# Expense Tracker

A personal budget and expense tracker for Windows, built with C#, .NET 10 and .NET MAUI.
It keeps all data in a local SQLite file next to the project, so the whole app and its data
live in one folder.

## Features

- **Expenses**: add, edit and delete; search by description; filter by category, month, custom
  date range and amount range; sort by date or amount.
- **Categories**: create and rename. Deleting a category that is in use lets you move its
  expenses to another category, or delete the category together with all its data.
- **Monthly budgets**: a limit per category and month, with a warning when a saved expense
  goes over the limit.
- **Recurring expenses**: daily, weekly, monthly or yearly schedules with an optional end date.
  Due expenses are generated at start-up. Schedules can be edited, deactivated, reactivated
  and deleted.
- **Reports**: month against the previous month, top expenses, average per day and a forecast
  for the whole month.
- **Bank CSV import** with a preview before anything is saved:
  - Revolut statements (German column names).
  - Any other bank: you choose the delimiter, number format, date format and columns.
    The choices are remembered for the next import.
  - A category per record, and auto-categorization rules ("description contains LIDL → Food").
  - Records that were imported before are skipped, so importing a file twice is safe.
- **Export** of the expenses on screen to CSV or Markdown.
- **Backup and restore** of the database. A backup is also made automatically before a
  database upgrade and before a category is deleted together with its data.
- **Settings**: the currency shown next to amounts, and the look: mode (System, Light, Dark, Black) and accent (Lavender, Mint, Sky, Rose, Amber), applied at once.
- **Error log**: unexpected errors are written to a log file instead of crashing the app.

## Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- The .NET MAUI workload: `dotnet workload install maui`
- Optional: JetBrains Rider or Visual Studio 2022+ with the .NET MAUI workload

## Run

```powershell
dotnet run --project ExpenseTracker.App -f net10.0-windows10.0.19041.0
```

Or open `CLI_Budget_Expense_Tracker.sln` in Rider or Visual Studio, select the
`ExpenseTracker.App` project and press Run.

The database is created and upgraded automatically on the first start.

## Where the data is

The app looks for the `.sln` file above the folder it runs from and keeps everything next to it.
If no solution file is found, the database goes to the application data folder instead.

| Path | Content |
|------|---------|
| `expense-tracker.db` | the SQLite database |
| `backups/` | database backups (`backup-…` made by you, `auto-…` made by the app) |
| `exports/` | exported CSV and Markdown files |
| `logs/` | daily log files, kept for 14 days |
| `settings.json` | currency and the last CSV column choices |

All of these are ignored by Git.

## Importing a bank statement

1. Open **Add CSV** on the main screen.
2. Pick the file format. Choose **Other bank CSV** for anything that is not Revolut, then
   choose the file and tell the app how it is written (delimiter, number format, date format,
   which column is the date, the description and the amount, and whether money spent is
   negative or positive).
3. Press **Show preview**. Tick the records to import and check the category of each one.
   Categories picked by a rule say which rule did it.
4. Press **Import selected**.

Rules are managed under **Categories → Auto-categorization rules**. When several rules fit a
description, the longest text wins.

## Project structure

| Project | Purpose |
|---------|---------|
| `CLI_Budget_Expense_Tracker.Core` | models, services and interfaces. It knows nothing about EF Core or the UI. |
| `CLI_Budget_Expense_Tracker.Infrastructure` | EF Core and SQLite, migrations, repositories, CSV import and export, backups |
| `ExpenseTracker.App` | the .NET MAUI application: views, view models (MVVM with CommunityToolkit.Mvvm) |

Money is always `decimal` and is stored in the database as whole cents. Time comes from
`TimeProvider`, never from `DateTime.Now`.

## Database migrations

Install the EF Core tool once (`dotnet tool install --global dotnet-ef`), then:

```powershell
dotnet ef migrations add <Name> --project CLI_Budget_Expense_Tracker.Infrastructure --output-dir Data/Migrations
```

Migrations are applied automatically when the app starts. If a database already exists, a backup
is made first.

## Status

The app works end to end, but it is still being developed. Not done yet: automated tests,
charts by category, a Windows release build and a new visual design. The plan is in
[`ExpenseTracker.App/ExpenseTracker-TZ-v3.md`](ExpenseTracker.App/ExpenseTracker-TZ-v3.md).
