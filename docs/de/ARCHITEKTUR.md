# Architektur

[Zurück zur README](../../README.de.md) · [English](../en/ARCHITECTURE.md)

## 1. Überblick

Die Lösung besteht aus drei Projekten mit Abhängigkeiten in nur einer Richtung:

```
ExpenseTracker.App  -->  Infrastructure  -->  Core
        |                                       ^
        +---------------------------------------+
```

| Projekt | Verantwortung | Hängt ab von |
|---------|---------------|--------------|
| `CLI_Budget_Expense_Tracker.Core` | Domänenmodelle, Geschäftsregeln, Dienste, Repository-Schnittstellen | nichts |
| `CLI_Budget_Expense_Tracker.Infrastructure` | EF Core + SQLite, Migrationen, Repository-Implementierungen, CSV-Import/-Export, Sicherungen, Generator für wiederkehrende Ausgaben | Core |
| `ExpenseTracker.App` | .NET-MAUI-Oberfläche (Windows): Views, ViewModels, Steuerelemente, App-Dienste | Core, Infrastructure |

Das Core-Projekt verweist weder auf EF Core noch auf MAUI oder eine Oberfläche. Es lässt sich ohne Datenbank testen,
indem man die Repository-Schnittstellen ersetzt.

## 2. Core

**Modelle** (`Models/`): `Expense`, `Category`, `BudgetLimit`, `RecurringExpense`, `CategoryRule`; Werte- und
Ergebnistypen wie `BudgetStatus`, `CategoryUsage`, `ExpenseReport`, `PeriodReport`, `BankStatementEntry`,
`ClassifiedBankEntry`, `BankImportResult`, `CsvImportProfile` sowie `MoneyRules`.

Die Entitäten prüfen sich in ihren Konstruktoren und Methoden selbst (zum Beispiel ein positiver Betrag mit höchstens
zwei Nachkommastellen, ein nicht leerer Name, ein Enddatum nicht vor dem Startdatum) und werfen bei ungültiger Eingabe
eine Ausnahme. Ein Wiederholungsplan kann sein nächstes Fälligkeitsdatum selbst weiterschieben.

**Dienste** (`Services/`) enthalten die Anwendungsfälle. Jeder erhält Repository-Schnittstellen und einen `TimeProvider`:

| Dienst | Anwendungsfälle |
|--------|-----------------|
| `ExpenseService` | hinzufügen, ändern, weich löschen, mit Filtern abfragen, einen `ExpenseReport` bilden |
| `CategoryService` | anlegen, umbenennen, Nutzungsstatistik, löschen, verschieben und löschen, samt Daten löschen |
| `BudgetService` | Monatslimit setzen, Status einer Kategorie, Status eines Monats, Limit löschen |
| `RecurringExpenseService` | anlegen (und erzeugen), ändern, deaktivieren, reaktivieren, löschen |
| `ReportService` | Monat gegen Vormonat, größte Ausgaben, Durchschnitt pro Tag, Prognose |
| `BankImportService` | die gewählten Einträge eines Auszugs speichern und Duplikate überspringen |
| `CategoryRuleService`, `CategoryRuleMatcher` | Regeln („enthält Text → Kategorie“); der längste Treffer gewinnt |
| `BankImportHasher` | stabiler SHA-256-Hash je Auszugszeile (gleiche Zeilen erhalten eine Ordnungszahl) |
| `ExpenseListQuery` | Text, Betragsspanne und Sortierung auf eine Liste anwenden |

**Schnittstellen** (`Interfaces/`): `IExpenseRepository`, `ICategoryRepository`, `IBudgetRepository`,
`IRecurringExpenseRepository`, `ICategoryRuleRepository`, `IRecurringExpenseGenerator` sowie die
Import-Abstraktionen `IBankStatementReader` und `IBankStatementClassifier`.

## 3. Infrastructure

**Persistenz.** `ExpenseTrackerDbContext` bildet die Entitäten auf SQLite ab. Entwurfsentscheidungen der Abbildung:

- **Geld wird als ganze Cent gespeichert** (`long`, Spalte `…InCents`) über einen Wertekonverter, es gibt also keine
  Rundungsfehler durch Gleitkommazahlen. Im Code ist Geld immer `decimal`.
- **Datenbank-Constraints sichern die Domänenregeln zusätzlich ab**: `CHECK`-Constraints für positive Beträge (ein
  Budget darf 0 sein), ein eindeutiger Index auf den normalisierten Kategorienamen, ein eindeutiger Index auf den
  Import-Hash einer Ausgabe (gelöschte Ausgaben behalten ihren Hash, ein erneuter Import kann sie also nicht
  zurückbringen), ein eindeutiger Index je Kategorie und Monat für Budgetlimits und ein eindeutiger Index auf das
  normalisierte Regelmuster.
- **Weiches Löschen**: Eine Ausgabe hat `IsDeleted`; ein globaler Abfragefilter blendet gelöschte Zeilen aus. Sie
  bleiben für den Verlauf und für das Verschieben von Daten zwischen Kategorien erhalten.
- **Indizes** für die häufigen Abfragen: nach Datum, nach Kategorie und Datum sowie Wiederholungspläne nach
  Aktivität und nächstem Fälligkeitsdatum.

**Migrationen** (`Data/Migrations`): InitialCreate, AddRecurringExpenses, LinkExpensesToRecurringSchedules,
AddExpenseImportHash, AddRecurringEndDate, AddCategoryRules. `DatabaseInitializer` wendet ausstehende Migrationen beim
Start an und erstellt zuvor eine Sicherung, wenn bereits eine Datenbank existiert.

**Repositories** (`Repositories/Sqlite*Repository`) setzen die Core-Schnittstellen mit EF-Core-Abfragen um.

**Import** (`Importing/`): `RevolutCsvReader` und `GenericCsvReader` lesen einen Auszug in `BankStatementEntry`-Einträge
(der generische Leser erhält ein `CsvImportProfile`: Trennzeichen, Kultur, Datumsformat, Spalten, Vorzeichenregel,
Währung). `RevolutStatementClassifier` und `GenericStatementClassifier` ordnen jeden Eintrag als Ausgabenkandidat,
Eingang, interne Überweisung, ignoriert oder „zu prüfen“ ein.

**Export** (`Exporting/`): `CsvExpenseExporter`, `MarkdownExpenseExporter`.

**Sicherung** (`Backup/DatabaseBackupService`): konsistente Schnappschüsse mit der Online-Backup-API von SQLite;
manuelle (`backup-…`) und automatische (`auto-…`) Dateien; eine Wiederherstellung sichert zuerst die aktuelle Datenbank.

**Generator für Wiederholungen** (`Services/SqliteRecurringExpenseGenerator`): erzeugt für alle aktiven Pläne jede bis
heute fällige Ausgabe und schiebt deren nächstes Fälligkeitsdatum weiter. Er läuft in einer Transaktion und verknüpft
jede erzeugte Ausgabe mit ihrem Plan; ein eindeutiger Index auf Plan und Datum macht den Lauf idempotent.

**Zusammenstellung**: `AddExpenseTrackerSqlite(connectionString)` registriert alles oben Genannte im DI-Container.

## 4. Anwendung (MAUI)

### 4.1 Aufbau

```
ExpenseTracker.App/
+-- AppShell.xaml        Shell mit festem Flyout (das Menü) und einer Route je Bildschirm
+-- MainPage.xaml        der Bildschirm „Ausgaben“
+-- Views/               die übrigen Bildschirme (XAML + Code-Behind)
+-- ViewModels/          ein ViewModel je Bildschirm (MVVM, CommunityToolkit.Mvvm)
+-- Models/              Listeneinträge und Anzeige-Records für die Oberfläche
+-- Controls/            wiederverwendbare Steuerelemente (siehe 4.4)
+-- Services/            App-Dienste (siehe 4.2)
+-- Platforms/Windows/   native Windows-Teile: Gestaltung nativer Steuerelemente, Popups
+-- Resources/           Stile, Farb-Token, Nunito-Schriften
```

Das Muster ist **MVVM**. Ein ViewModel hält Zustand und Befehle eines Bildschirms, liest und schreibt über
Core-Dienste und stellt anzeigefertige Werte bereit (formatierte Beträge, lokalisierte Texte, Farben). Das Code-Behind
enthält nur view-spezifischen Code: Dialoge öffnen, Hover-Effekte, Navigation und Handler, die Steuerelemente brauchen.

**Datenzugriff aus einem ViewModel.** Jede Operation erzeugt einen DI-Scope (`IServiceScopeFactory`), löst den
Core-Dienst auf, läuft in einem Hintergrund-Thread (`Task.Run`) und liefert einfache Daten zurück. Die UI-Eigenschaften
werden danach im UI-Thread geändert. Jeder Aufruf ist von einer Fehlerbehandlung umgeben, die die Ausnahme protokolliert
und eine lokalisierte Meldung zeigt; die Oberfläche zeigt einen Beschäftigt-Zustand und sperrt Aktionen, solange gearbeitet wird.

**Navigation.** `Shell` mit `FlyoutBehavior="Locked"`; das eigene `Sidebar` ist der Flyout-Inhalt und navigiert über
Routen (`//expenses`, `//budgets`, …). Der Bildschirm „Berichte“ bittet „Ausgaben“ über den Singleton
`ExpenseFilterRequest`, sich auf eine Kategorie und einen Monat zu öffnen.

### 4.2 Anwendungsdienste

| Dienst | Aufgabe |
|--------|---------|
| `AppInitializer` | einmal pro Lauf: Datenbank initialisieren (Migrationen), danach fällige Wiederholungen erzeugen; wiederholbar nach Fehlern |
| `AppSettings` | `settings.json` neben der Datenbank: Währung, Sprache, Design, Akzent, Kategoriefarben, letztes CSV-Profil. Geschrieben wird zuerst in eine temporäre Datei, die dann die alte ersetzt, ein Absturz kann die Datei also nicht beschädigen |
| `ThemeService` | setzt zur Laufzeit die Farb-Token für den gewählten Modus und Akzent |
| `Loc`, `TrExtension`, `Translations` | Lokalisierung (siehe 4.5) |
| `FileLoggerProvider` | `ILogger`-Provider, der `logs/app-YYYYMMDD.log` schreibt und Dateien löscht, die älter als 14 Tage sind |
| `GlobalErrorHandler` | fängt unbehandelte Ausnahmen, schreibt sie ins Protokoll und zeigt eine Meldung statt abzustürzen |
| `ExportLocation` | der Ordner `exports` |
| `ExpenseFilterRequest` | Übergabe von „Berichte“ an „Ausgaben“ |

### 4.3 Gestaltung (Themes)

Alle Farben sind **Token** (`Bg`, `Surface`, `Surface2`, `Border`, `Ink`, `InkMuted`, `Accent`, `OnAccent`,
`AccentSoft`, `Danger`, `Success`, `Warning`, `Track` und sechs Kategoriefarben). `ThemeService` schreibt die Werte in
die Anwendungsressourcen; XAML und Steuerelemente verwenden sie mit `DynamicResource`, ein Wechsel von Modus oder
Akzent wirkt also sofort ohne Neustart.

- Modus: System, Hell, Dunkel, Schwarz (rein schwarzer Hintergrund `#000000`).
- Akzent: Lavendel, Minze, Himmel, Rose, Bernstein; jeder hat eine dunkle und eine helle Palette.
- **Native Steuerelemente**: Unter Windows zeichnen Steuerelemente wie `Entry` ihre Hover- und Fokusfarben aus
  Systempinseln. `Platforms/Windows/NativeControlTheme` speist über Handler-Mapper die Token-Farben in diese Pinsel
  ein; die Titelleiste wird über die `TitleBar` des Fensters gefärbt.

### 4.4 Steuerelemente

Die Oberfläche besteht aus wenigen wiederverwendbaren Steuerelementen statt aus den Plattform-Vorgaben:

| Steuerelement | Zweck |
|---------------|-------|
| `Sidebar`, `NavItem` | das Menü |
| `PillButton`, `IconButton`, `Chip` | Schaltflächen und Statusmarken |
| `DropDown`, `DateField`, `MonthSwitch` | Auswahlfelder; jedes öffnet unter sich ein gestaltetes Popup |
| `DonutChart`, `ComparisonBar`, `MeterBar`, `StackBar` | Diagramme, gezeichnet mit `GraphicsView` und `IDrawable` (keine Diagrammbibliothek) |
| `IconView`, `Icons` | Linien-Icons aus SVG-Pfaddaten, vom Design eingefärbt |
| `CenteredContainer` | Inhaltsbereich, der die Breite neben dem Menü nutzt |
| `CategoryPalette` | die Farbe einer Kategorie (Palettenplatz oder eine eigene Farbe aus den Einstellungen) |
| `MenuPopup`, `ColorPickerPopup` | kleine Menüs und die Farbauswahl |

`Platforms/Windows/PopupMenu` baut die Popups als WinUI-Flyouts, deren Inhalt aus den Token gefärbt wird: die Liste
einer Auswahl, der Kalender, das Monatsraster und die Farbauswahl. Sie passen zum Design und öffnen sich immer unter
dem Feld, was die System-Steuerelemente `Picker` und `DatePicker` nicht tun.

### 4.5 Lokalisierung

Der englische Text ist der Schlüssel. `Loc.T("Save")` liefert die Übersetzung in der aktuellen Sprache oder den
englischen Text, wenn es keine gibt. `Loc.F("{0} entries", n)` übersetzt einen Formatstring und füllt ihn. In XAML:
`Text="{loc:Tr Key='Save'}"`. Die Übersetzungen (Deutsch, Russisch, Ukrainisch) stehen in `Services/Translations.cs`.

Beim Sprachwechsel wird die Sprache gespeichert, die Kultur gesetzt (damit Monats- und Tagesnamen folgen) und die
`AppShell` neu aufgebaut, sodass jede Seite ihre Texte neu liest. Zahlen werden mit der invarianten Kultur gelesen,
die Sprache hat also keinen Einfluss darauf, wie Beträge eingegeben oder gespeichert werden.

Texte aus dem Core-Projekt (zum Beispiel Prüfmeldungen der Entitäten) werden nicht übersetzt.

## 5. Qualitätsentscheidungen

- **`decimal` für Geld**, ganze Cent in der Datenbank, Constraints in der Datenbank als zweite Verteidigungslinie.
- **`TimeProvider` statt `DateTime.Now`**, damit zeitabhängige Logik (Fälligkeiten, „nicht in der Zukunft“) testbar ist.
- **Duplikatsicherer Import** durch stabile Transaktions-Hashes und einen eindeutigen Index.
- **Kein stiller Datenverlust**: Sicherungen vor Migrationen und destruktiven Aktionen; das Löschen von Daten braucht
  eine zweite Bestätigung; weiches Löschen für Ausgaben.
- **Fehler werden protokolliert, nicht als Absturz gezeigt**; jeder Bildschirm hat einen Fehlerzustand mit erneutem Versuch.
- **Ein Ordner hält alles** (Datenbank, Sicherungen, Exporte, Protokolle, Einstellungen), das macht die App portabel
  und leicht zu sichern.
- **Einheitlicher Codestil** durch `.editorconfig`; die CI baut die gesamte Lösung im Release-Modus.

## 6. Bekannte Grenzen

- Nur Windows: Das MAUI-Projekt zielt auf `net10.0-windows10.0.19041.0`, und die Popups nutzen WinUI-APIs.
- Derzeit keine automatischen Tests (die früheren Tests wurden entfernt, als die Konsolenversion entfiel, und stehen
  in der Git-Historie). Das Core-Projekt ist so aufgebaut, dass sie sich einfach schreiben lassen.
- Texte aus dem Core-Projekt sind englisch.
- Die Ausgabenliste wird jeweils für einen Zeitraum geladen; es gibt keine Seitenweise-Ladung.
