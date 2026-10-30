# Entwicklungsleitfaden

[Zurück zur README](../../README.de.md) · [English](../en/DEVELOPMENT.md)

## 1. Einrichtung

1. Das [.NET 10 SDK](https://dotnet.microsoft.com/download) installieren.
2. Die MAUI-Workload installieren: `dotnet workload install maui`.
3. Repository klonen und wiederherstellen: `dotnet restore CLI_Budget_Expense_Tracker.sln`.

## 2. Bauen und starten

```powershell
# alles bauen (das macht auch die CI)
dotnet build CLI_Budget_Expense_Tracker.sln -c Release

# die App starten
dotnet run --project ExpenseTracker.App -f net10.0-windows10.0.19041.0
```

In Rider oder Visual Studio: Lösung öffnen, `ExpenseTracker.App` wählen, Run.

Eine laufende App sperrt `ExpenseTracker.App.exe`; beenden Sie sie vor dem nächsten Build, sonst endet der Build mit
der Meldung „Datei wird von einem anderen Prozess verwendet“.

Beim Start schreibt die App eine harmlose Zeile wie `Error loading font 'ms-appx:///Nunito-….ttf'` ins Protokoll.
Die Schriften sind korrekt registriert und werden wie erwartet dargestellt.

## 3. Wo die App während der Entwicklung ihre Daten ablegt

Die App sucht oberhalb ihres Ausgabeordners nach der `.sln`-Datei und legt Datenbank, Sicherungen, Exporte, Protokolle
und `settings.json` daneben ab. Alle sind von Git ausgeschlossen. Für einen sauberen Neustart die App schließen und
`expense-tracker.db*` löschen; sie wird beim nächsten Start neu angelegt.

## 4. Datenbankmigrationen

Das EF-Core-Werkzeug einmalig aus dem lokalen Manifest in `.config` wiederherstellen: `dotnet tool restore`
(oder global installieren mit `dotnet tool install --global dotnet-ef`).

```powershell
# eine Migration nach der Änderung eines Modells oder des DbContext anlegen
dotnet ef migrations add <Name> --project CLI_Budget_Expense_Tracker.Infrastructure --output-dir Data/Migrations
```

Migrationen werden beim Start der App angewendet. Existiert bereits eine Datenbankdatei, wird zuvor eine Sicherung
(`auto-…`) erstellt. Halten Sie bei der Änderung einer Entität die Domänenprüfung im Core-Modell und den
Datenbank-Constraint im `DbContext` im Einklang.

## 5. Konventionen

- **Schichten**: Core darf weder EF Core noch MAUI noch UI-Typen referenzieren. Infrastructure implementiert die
  Core-Schnittstellen. Die App spricht mit Core-Diensten, nie mit dem `DbContext`.
- **Geld**: `decimal` im Code, ganze Cent in der Datenbank. Zur Prüfung `MoneyRules` verwenden.
- **Zeit**: `TimeProvider` injizieren; in der Logik kein `DateTime.Now` und kein `DateTime.Today` aufrufen.
- **Benutzereingaben lesen**: Beträge werden mit der invarianten Kultur gelesen, nachdem ein Komma durch einen Punkt
  ersetzt wurde. Beträge nicht mit der aktuellen Kultur lesen; die Oberflächensprache ändert die Kultur.
- **MVVM**: Zustand und Befehle im ViewModel; Code-Behind nur für Belange der View. Datenzugriff in einem
  Hintergrund-Thread, gebundene Eigenschaften im UI-Thread ändern.
- **Fehler**: auf Ebene der Operation abfangen, mit `ILogger` protokollieren, eine lokalisierte Meldung zeigen.
  Eine Ausnahme darf die App nie beenden.
- **Stil**: `.editorconfig` legt die Formatierung fest; Kommentare kurz halten und das *Warum* erklären, nicht das *Was*.
- **Commits**: klein, je ein Thema, mit einer Nachricht, die sagt, was und warum (`feat:`, `fix:`, `style:`, `docs:`).

## 6. Erweiterung

### Einen Bildschirm hinzufügen

1. `ViewModels/<Name>ViewModel.cs` (von `ObservableObject` ableiten) sowie `Views/<Name>Page.xaml` + Code-Behind anlegen.
2. Beide in `MauiProgram` registrieren (`AddTransient`).
3. Ein `ShellContent` mit Route in `AppShell.xaml` und einen Eintrag in `Controls/Sidebar.cs` (Route, Text, Icon) ergänzen.
4. `controls:CenteredContainer` als Wurzel des Inhalts und das gemeinsame Kopfmuster verwenden (Titel mit Untertitel
   links, Aktionen rechts).

### Einen Text hinzufügen oder ändern

Den englischen Text im Code oder in XAML schreiben (`{loc:Tr Key='…'}`) und eine Zeile in `Services/Translations.cs`
ergänzen:

```csharp
["Save changes"] = ["Änderungen speichern", "Сохранить изменения", "Зберегти зміни"],
```

Die Reihenfolge ist Deutsch, Russisch, Ukrainisch. Ein Text ohne Zeile erscheint auf Englisch. Für Texte mit Werten
einen Formatstring mit Platzhaltern verwenden (`Loc.F("{0} of {1}", a, b)`) und den ganzen Formatstring übersetzen.

### Eine Sprache hinzufügen

Den Code in `Loc` ergänzen (`Languages` und `Codes`), jeder Zeile von `Translations` eine Spalte hinzufügen (und die
Indexlogik in `Loc.T` anpassen) und die Sprache in den Einstellungen über `Loc.Languages` anbieten.

### Einen Akzent oder eine Designfarbe hinzufügen

Akzente sind Einträge des Wörterbuchs `Palettes` in `Services/ThemeService.cs` (je eine dunkle und eine helle Palette)
und stehen in `ThemeService.Accents`. Ein neuer Farb-Token muss in `Resources/Styles/Colors.xaml` (Standardwert)
definiert und in `ThemeService.Apply` gesetzt werden und wird dann mit `{DynamicResource Token}` verwendet.

### Eine Spalte oder Tabelle hinzufügen

Das Modell in Core ändern, im `ExpenseTrackerDbContext` abbilden (mit Constraints), eine Migration anlegen
(Abschnitt 4) und Repository sowie Dienst erweitern.

### Eine weitere Bank unterstützen

`IBankStatementReader` und `IBankStatementClassifier` in Infrastructure implementieren, registrieren und eine
Formatoption in `BankImportViewModel.Formats` ergänzen. Banken mit einfacher Tabelle brauchen keinen Code: Der
generische Leser ordnet die Spalten in der Oberfläche zu.

## 7. Kontinuierliche Integration

`.github/workflows/build.yml` baut die Lösung bei jedem Push auf `master` und bei jedem Pull Request im Release-Modus
auf `windows-latest`. Zuvor wird die MAUI-Workload installiert.

## 8. Stand und Planung

Erledigt: die Funktionen aus dem [Benutzerhandbuch](BENUTZERHANDBUCH.md), Fehlerprotokoll, Sicherungen, CI-Build,
Lokalisierung, Designs und die neu gestalteten Bildschirme.

Offen:

- [ ] Automatische Tests (zuerst die Core-Dienste: Geldregeln, Budgetstatus, Erzeugung von Wiederholungen,
      Import-Hash und -Klassifizierung, Regeltreffer) und deren Ausführung in der CI.
- [ ] Windows-Release-Build (Paketierung).
- [ ] Übersetzung der Meldungen aus dem Core-Projekt.

## 9. Release-Zip bauen

`.\tools\pack-release.ps1 -Version 1.2.0` veröffentlicht die App eigenständig in einen Ordner `app`, baut einen kleinen
nativen Starter (`tools/Launcher`, Native AOT, etwa 1,5 MB) als `ExpenseTracker.exe`, fügt `info` (Readme, Lizenz,
Hinweise) hinzu und schreibt `artifacts/ExpenseTracker-v<Version>-win-x64.zip`. Im Hauptordner des Zips liegen dann nur
der Starter und zwei Ordner. Zum Linken braucht der Starter die C++-Werkzeuge von Visual Studio (Workload
„Desktopentwicklung mit C++“).
