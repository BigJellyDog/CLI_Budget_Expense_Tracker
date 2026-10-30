# Expense Tracker

[![Build](https://github.com/BigJellyDog/CLI_Budget_Expense_Tracker/actions/workflows/build.yml/badge.svg)](https://github.com/BigJellyDog/CLI_Budget_Expense_Tracker/actions/workflows/build.yml)

**English** · [Deutsch](README.de.md)

A personal budget and expense tracker for Windows, built with **C#, .NET 10 and .NET MAUI**.
All data stays in a local SQLite file next to the project: no account, no server, no cloud.

![Expenses](docs/images/en/expenses.png)

## Highlights

- **Complete expense workflow**: add, edit, delete; search, filter by category / month / date range /
  amount range; sort; export to CSV and Markdown.
- **Monthly budgets** with status per category (within limit, almost reached, over the limit) and a
  warning when a new expense exceeds a limit.
- **Recurring expenses** (daily, weekly, monthly, yearly) that are generated automatically at start-up.
- **Bank CSV import** with preview: Revolut and any other bank (you map the columns), automatic
  categorization by rules, and duplicate protection through transaction hashes.
- **Reports**: month against the previous month, share of total, top expenses, average per day, forecast.
- **Safe data handling**: backups before migrations and destructive actions, restore, soft delete,
  money stored as whole cents.
- **Four languages** (English, German, Russian, Ukrainian) and **themes**: System / Light / Dark / Black
  with five accent colors, switched at runtime.
- **Clean architecture**: a framework-free domain layer, an infrastructure layer for EF Core and SQLite,
  and an MVVM application layer.

## Screens

| | |
|---|---|
| ![Budgets](docs/images/en/budgets.png) | ![Categories](docs/images/en/categories.png) |
| ![Recurring](docs/images/en/recurring.png) | ![Reports](docs/images/en/reports.png) |
| ![Import](docs/images/en/import.png) | ![Settings](docs/images/en/settings.png) |

## Documentation

| Document | Content |
|----------|---------|
| [User guide](docs/en/USER_GUIDE.md) | every screen and workflow, data locations, backups |
| [Architecture](docs/en/ARCHITECTURE.md) | layers, data model, key design decisions, UI system |
| [Development](docs/en/DEVELOPMENT.md) | build, run, migrations, conventions, how to extend, status |

The German versions are in [`docs/de`](docs/de).

## Requirements

- Windows 10 or 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- The .NET MAUI workload: `dotnet workload install maui`
- Optional: JetBrains Rider or Visual Studio 2022+ with the .NET MAUI workload

## Download

A ready-to-run Windows build (64-bit, no installation, no .NET needed) is on the
[Releases page](https://github.com/BigJellyDog/CLI_Budget_Expense_Tracker/releases/latest): download the zip,
extract it and start `ExpenseTracker.App.exe`.

## Quick start (from source)

```powershell
dotnet run --project ExpenseTracker.App -f net10.0-windows10.0.19041.0
```

Or open `CLI_Budget_Expense_Tracker.sln` in Rider or Visual Studio, select `ExpenseTracker.App` and run.
The database is created and upgraded automatically on the first start.

## Technology

| Area | Choice |
|------|--------|
| Language / runtime | C# 14, .NET 10 |
| UI | .NET MAUI (Windows), XAML, Shell navigation, MVVM with CommunityToolkit.Mvvm |
| Data | EF Core + SQLite, code-first migrations |
| Drawing | `GraphicsView` / `IDrawable` for icons, donut chart and bars (no chart library) |
| Font | Manrope (five weights, bundled) |
| CI | GitHub Actions (build on `windows-latest`) |

## Project structure

| Project | Purpose |
|---------|---------|
| `CLI_Budget_Expense_Tracker.Core` | models, services and interfaces; knows nothing about EF Core or the UI |
| `CLI_Budget_Expense_Tracker.Infrastructure` | EF Core and SQLite, migrations, repositories, CSV import and export, backups |
| `ExpenseTracker.App` | the .NET MAUI application: views, view models, controls, services |

## Status

The application works end to end. Not done yet: automated tests, a Windows release build and demo data
for screenshots. The detailed plan is in
[`ExpenseTracker.App/ExpenseTracker-TZ-v3.md`](ExpenseTracker.App/ExpenseTracker-TZ-v3.md) (in Russian).
