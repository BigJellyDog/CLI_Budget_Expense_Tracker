# Benutzerhandbuch

[Zurück zur README](../../README.de.md) · [English](../en/USER_GUIDE.md)

Expense Tracker ist eine Desktop-App, um Ausgaben zu erfassen, Monatslimits zu planen und zu verstehen, wohin das
Geld fließt. Dieses Handbuch beschreibt jeden Bildschirm. Die Screenshots zeigen das Design „Schwarz“ mit dem
Akzent „Minze“ und eine englische Oberfläche; in den Einstellungen lässt sich die App auf Deutsch umstellen.

## Inhalt

1. [Orientierung](#1-orientierung)
2. [Ausgaben](#2-ausgaben)
3. [CSV-Import](#3-csv-import)
4. [Kategorien](#4-kategorien)
5. [Budgets](#5-budgets)
6. [Berichte](#6-berichte)
7. [Wiederkehrende Ausgaben](#7-wiederkehrende-ausgaben)
8. [Einstellungen](#8-einstellungen)
9. [Ihre Daten: Speicherort, Sicherung, Wiederherstellung](#9-ihre-daten)
10. [Fehlerbehebung](#10-fehlerbehebung)

## 1. Orientierung

Das Menü links ist immer sichtbar. Es enthält einen Eintrag je Bildschirm: **Ausgaben**, **CSV-Import**,
**Kategorien**, **Budgets**, **Berichte**, **Wiederkehrend** und unten **Einstellungen**. Der geöffnete Bildschirm
ist hervorgehoben.

Gemeinsame Elemente:

- **Monatsauswahl**: Die Pfeile wechseln um einen Monat; ein Klick auf den Monatsnamen öffnet einen kleinen
  Kalender, in dem Sie jeden Monat jedes Jahres wählen. Sie erscheint bei Ausgaben, Budgets und Berichten.
- **Runder Aktualisieren-Knopf** neben der Monatsauswahl lädt den Bildschirm neu.
- **Auswahllisten und Datumsfelder** öffnen eine Liste oder einen Kalender direkt unter dem Feld.
- Gescrollt wird mit dem Mausrad; die Bildlaufleisten sind absichtlich ausgeblendet.

## 2. Ausgaben

![Ausgaben](../images/expenses.png)

Der Hauptbildschirm. Er zeigt jeweils einen Monat.

**Kopfzeile.** Titel mit der Anzahl der Einträge, Monatsauswahl, Aktualisieren, **Exportieren** und
**Ausgabe hinzufügen**.

**Drei Kennzahlen.**

| Karte | Bedeutung |
|-------|-----------|
| Ausgegeben im *Monat* | Summe der angezeigten Ausgaben und der Durchschnitt pro Tag |
| Im Vergleich zum *Vormonat* | Prozent und Betragsdifferenz (rot = mehr, grün = weniger). Gab es im Vormonat keine Ausgaben, steht beim Prozentwert „–“. |
| Prognose für *Monat* | Durchschnitt pro Tag mal Anzahl der Tage des Monats (nur für den laufenden Monat) |

**Liste.** Die Ausgaben sind nach Tagen gruppiert (bei Sortierung nach Datum), rechts neben jeder Tagesüberschrift
steht die Tagessumme. Fahren Sie mit der Maus über eine Zeile, um die Symbole **Bearbeiten** und **Löschen**
anzuzeigen. Das Löschen verlangt eine Bestätigung.

**Suche und Filter.**

- Das Suchfeld durchsucht die Beschreibung. Mit Enter wird angewendet.
- *Alle Kategorien* und die Sortierung (neueste/älteste zuerst, höchster/niedrigster Betrag) wirken sofort.
- Der Knopf **Filter** öffnet ein Feld mit dem Zeitraum (Monat, gesamter Zeitraum, eigener Zeitraum), den Daten
  eines eigenen Zeitraums sowie einem Mindest- und Höchstbetrag. Eine Marke am Knopf zeigt, wie viele dieser Filter
  aktiv sind. Mit **Anwenden** und **Zurücksetzen** steuern Sie sie.

**Diagramm und Budgets (rechte Spalte).**

- Das **Ringdiagramm** teilt den Zeitraum nach Kategorien auf. Es zeigt immer alle Kategorien, auch wenn die Liste
  gefiltert ist. Fahren Sie über ein Segment, um es zu vergrößern. Ein Klick auf ein Segment oder eine Zeile der
  Legende filtert die Liste auf diese Kategorie; ein zweiter Klick hebt den Filter auf. **Bericht** öffnet den
  Bildschirm Berichte.
- Die Karte **Budgets** zeigt die drei Kategorien, die ihrem Limit am nächsten sind oder es überschreiten, und wie
  viele überschritten sind.

**Exportieren.** Das Menü bietet *CSV*, *Markdown* und *Sicherung*. CSV und Markdown enthalten genau die aktuell
angezeigten Ausgaben (Zeitraum, Kategorie, Suche und Betragsspanne). Die Dateien liegen im Ordner `exports`.

**Ausgabe hinzufügen oder bearbeiten.** Wählen Sie eine Kategorie (oder legen Sie sofort eine neue an), geben Sie
den Betrag ein (Punkt oder Komma, höchstens zwei Nachkommastellen), das Datum (nicht in der Zukunft) und optional
eine Beschreibung. Überschreitet die Ausgabe das Monatslimit der Kategorie, zeigt eine Warnung die Beträge. Eine
Ausgabe, die ein Wiederholungsplan erzeugt hat, behält ihr Datum; die übrigen Felder lassen sich für diesen einen
Termin ändern.

## 3. CSV-Import

![Import](../images/import.png)

Importiert einen Kontoauszug. Vor Ihrer Bestätigung wird nichts gespeichert.

1. **Format wählen.** *Revolut (deutsche Spaltennamen)* funktioniert sofort. Für jede andere Bank wählen Sie
   *CSV einer anderen Bank*.
2. **Datei wählen** mit **CSV wählen**. Bei einer anderen Bank beschreiben Sie, wie die Datei aufgebaut ist:
   Trennzeichen, Zahlenformat (zum Beispiel `1,234.56` oder `1.234,56`), Datumsformat (zum Beispiel `dd.MM.yyyy`),
   die Spalten für Datum, Beschreibung und Betrag, eine optionale Währungsspalte und ob ausgegebenes Geld eine
   negative oder positive Zahl ist. Diese Angaben werden für den nächsten Import gemerkt. Dann **Vorschau anzeigen**.
3. **Einträge prüfen.** Die Tabelle zeigt Datum, Beschreibung, Betrag und Kategorie.
   - Importierbare Einträge sind angehakt. Entfernen Sie, was Sie nicht brauchen; das Kästchen in der Kopfzeile
     wählt alle.
   - Eine Marke **Regel** bedeutet, dass eine Regel zur automatischen Kategorisierung die Kategorie gesetzt hat.
     Einträge ohne Regel nutzen die **Standardkategorie** (linke Spalte) und haben einen gelben Rand; ändern Sie
     sie in der Auswahlliste.
   - Eingänge und interne Überweisungen sind grau und werden übersprungen.
   - Die Marken über der Tabelle zählen die Regeltreffer, die Einträge mit Standardkategorie und die
     übersprungenen Einträge.
4. **Importieren.** Die Leiste unten zeigt immer *N von M ausgewählt* und die Summe. **N Einträge importieren**
   speichert sie. Bereits importierte Einträge werden an einem Hash der Transaktion erkannt und übersprungen; eine
   Datei zweimal zu importieren ist also sicher. Das Ergebnis zeigt, wie viele hinzugefügt, als Duplikat übersprungen
   oder nicht importiert wurden.

**Regeln zur automatischen Kategorisierung** (Link auf diesem Bildschirm oder **Kategorien → Regeln zur
Auto-Kategorisierung**): Eine Regel sagt „enthält die Beschreibung *Text*, dann *Kategorie*“. Groß- und
Kleinschreibung spielt keine Rolle. Passen mehrere Regeln, gewinnt der längste Text.

## 4. Kategorien

![Kategorien](../images/categories.png)

Jede Kategorie ist eine Kachel mit ihrer Farbe, der Anzahl der Einträge in diesem Monat, dem Betrag und ihrem
Anteil am Monat.

- **Neue Kategorie**: Name eingeben, Farbe wählen und **Kategorie erstellen**. Die Zeile bietet sechs Farben der
  Palette und ein **+**, das eine Farbauswahl für eine eigene Farbe öffnet. Namen müssen eindeutig sein (ohne
  Beachtung der Groß- und Kleinschreibung).
- **Umbenennen** mit dem Stift: Das Formular wechselt in den Umbenennen-Modus; auch die Farbe lässt sich ändern.
- **Löschen** mit dem Papierkorb. Wird die Kategorie nicht verwendet, wird sie nach einer Bestätigung gelöscht.
  Wird sie verwendet, können Sie entweder **die Ausgaben und Pläne in eine andere Kategorie verschieben** oder
  **die Kategorie samt allen Daten löschen**. Die zweite Möglichkeit erstellt zuerst eine Sicherung und fragt ein
  zweites Mal nach.

Die Kategoriefarben sind auf jedem Bildschirm gleich (Listen, Ringdiagramm, Budgets, Berichte).

## 5. Budgets

![Budgets](../images/budgets.png)

Ein Ausgabenlimit pro Kategorie und Monat.

- Oben: **Gesamtlimit**, **ausgegeben (mit Limit)** und **wie viele Kategorien überschritten sind**.
- **Limit für *Monat* festlegen**: Kategorie wählen, Betrag eingeben und **Speichern**. Ein Limit von 0 bedeutet,
  dass für diese Kategorie nichts eingeplant ist. Erneutes Speichern ändert das Limit.
- Jede Kategorie mit Limit ist eine Karte: ausgegebener Betrag, Limit, Fortschrittsbalken und der Restbetrag oder
  die Überschreitung. Der Status ist **Im Rahmen**, **Fast erreicht** (ab 90 %) oder **Überschritten**.
- Der **Stift** übernimmt die Kategorie ins Formular; das Menü **⋮** entfernt das Limit (Ihre Ausgaben bleiben).
- Kategorien ohne Limit erscheinen als gestrichelte Karten mit dem Knopf **Limit festlegen**.

Limits gehören zu einem Monat; jeder Monat hat also eigene Limits.

## 6. Berichte

![Berichte](../images/reports.png)

Eine Auswertung eines Monats im Vergleich zum Vormonat.

- Vier Kennzahlen: **Insgesamt ausgegeben** (mit der Änderung), **Durchschnitt pro Tag**, **Prognose** und die
  **Top-Kategorie**.
- **Nach Kategorie**: je Kategorie ein Balken für diesen Monat und eine dünne Marke für den Vormonat auf derselben
  Skala, beide Beträge und eine Marke für die **Änderung** (roter Pfeil nach oben = mehr, grüner Pfeil nach unten =
  weniger). Der Schalter **Betrag / Anteil** stellt die Balken auf Anteile am Gesamten um.
- **Anteil am Gesamten**: das Ringdiagramm. **Größte Ausgaben**: die fünf größten Einzelausgaben.
- Ein Klick auf eine Kategorie (in der Tabelle oder im Ring) öffnet die Ausgaben für diese Kategorie und diesen Monat.

## 7. Wiederkehrende Ausgaben

![Wiederkehrend](../images/recurring.png)

Für Zahlungen, die sich wiederholen: Miete, Abonnements, Versicherungen.

- **Neuer Plan**: Kategorie, Betrag pro Termin, Häufigkeit (täglich, wöchentlich, monatlich, jährlich), Startdatum,
  optionales Enddatum (zum Eingeben einschalten) und Beschreibung.
- Beim Erstellen eines Plans werden auch **die bereits fälligen Ausgaben bis heute erzeugt**; ein Startdatum in der
  Vergangenheit erzeugt die verpassten Termine. Auch bei jedem Start der App werden fällige Ausgaben erzeugt.
  **Fällige Ausgaben erzeugen** erledigt das auf Wunsch.
- Jeder Plan ist eine Karte mit Betrag, Häufigkeit, nächstem Fälligkeitsdatum und einer Marke wie „in 3 Tagen“.
  **Bearbeiten** ändert nur künftige Ausgaben; früher erzeugte bleiben unverändert, und eine neue Häufigkeit zählt
  ab der nächsten Fälligkeit. **Deaktivieren** stoppt die Erzeugung; **Reaktivieren** setzt sie fort; **Löschen**
  entfernt den (inaktiven) Plan und behält die bereits erzeugten Ausgaben.
- **Demnächst fällig** listet die Fälligkeiten der nächsten 30 Tage samt Summe.

## 8. Einstellungen

![Einstellungen](../images/settings.png)

- **Sprache**: Englisch, Deutsch, Russisch oder Ukrainisch. Beim Wechsel werden die Bildschirme neu geladen. Monats-
  und Tagesnamen folgen der Sprache.
- **Darstellung**: der *Modus* (System folgt Windows, Hell, Dunkel, Schwarz mit rein schwarzem Hintergrund) und der
  *Akzent* (Lavendel, Minze, Himmel, Rose, Bernstein). Änderungen wirken sofort, auch in der Titelleiste.
- **Währung**: ein dreibuchstabiger Code (EUR, USD, CHF …) neben den Beträgen. Gespeicherte Beträge werden nicht
  umgerechnet.
- **Wo Ihre Daten liegen**: der Ordner mit Datenbank, Sicherungen, Exporten, Protokollen und Einstellungen.

## 9. Ihre Daten

Die App sucht oberhalb ihres Ausführungsordners nach der Lösungsdatei (`.sln`) und legt alles daneben ab. Gibt es
keine, liegen die Daten im Anwendungsdatenordner.

| Pfad | Inhalt |
|------|--------|
| `expense-tracker.db` | die SQLite-Datenbank |
| `backups/` | Sicherungen: `backup-…` von Ihnen, `auto-…` von der App |
| `exports/` | exportierte CSV- und Markdown-Dateien |
| `logs/` | eine Protokolldatei pro Tag, 14 Tage aufbewahrt |
| `settings.json` | Währung, Sprache, Design, Akzent, Kategoriefarben, letzte CSV-Spaltenwahl |

**Sicherung.** *Exportieren → Sicherung → Sicherung erstellen* legt einen konsistenten Schnappschuss der Datenbank an
(mit der SQLite-Backup-API). Die App erstellt außerdem automatisch eine Sicherung vor einem Datenbank-Upgrade und
bevor eine Kategorie samt Daten gelöscht wird.

**Wiederherstellung.** *Exportieren → Sicherung → Aus Sicherung wiederherstellen*, eine Datei wählen (neueste zuerst)
und bestätigen. Die aktuelle Datenbank wird zuvor gesichert, eine Wiederherstellung lässt sich also durch eine
weitere Wiederherstellung rückgängig machen.

Gelöschte Ausgaben werden als gelöscht markiert und nicht aus der Datei entfernt (weiches Löschen); eine Sicherung ist
der Weg, eine gelöschte Kategorie samt Daten zurückzuholen.

## 10. Fehlerbehebung

| Problem | Was tun |
|---------|---------|
| Eine Meldung sagt, etwas sei schiefgelaufen | Der Fehler steht im Protokoll; öffnen Sie den Ordner `logs` und lesen Sie die heutige Datei |
| Eine Liste wirkt veraltet | den runden Aktualisieren-Knopf drücken |
| Ein Import lässt sich nicht lesen | Trennzeichen, Zahlenformat, Datumsformat und Spalten prüfen; die Meldung nennt das Problem |
| Eine Kategorie lässt sich nicht anlegen | der Name existiert bereits (ohne Beachtung der Groß- und Kleinschreibung) |
| Ein Betrag wird abgelehnt | eine positive Zahl mit höchstens zwei Nachkommastellen verwenden; Punkt oder Komma sind möglich |
| Das Fenster wirkt zu schmal | die rechte Spalte der Ausgaben weicht unter etwa 900 px Inhaltsbreite aus; Fenster verbreitern |
