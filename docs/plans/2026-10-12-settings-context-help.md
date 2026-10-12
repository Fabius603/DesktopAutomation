# Konzept: Kontexthilfe in den Einstellungen

Stand: 12.10.2026. Vorschlag für die Umsetzung, kein verbindlicher Entscheidungsrecord.
Die Anwendung wurde für dieses Konzept nicht geändert. Grundlage sind die aktuellen
Settings-Views, ihre ViewModels und die vorhandenen deutschen Beschreibungstexte.

## Ziel und Umfang

Jede bearbeitbare Eigenschaft in den Einstellungen erhält eine verständliche Erklärung
direkt am Feld. Nutzer sollen erkennen, was die Eigenschaft bewirkt, wann Änderungen
wirksam werden und welche Voraussetzungen gelten.

Umfang: Allgemein, Updates und Zugangsdaten einschließlich des Zugangsdaten-Editors.
Bei Aktionen wird die Wirkung erklärt, bei Statusangaben deren Bedeutung.
Navigationseinträge und selbsterklärende Standardaktionen wie Abbrechen benötigen
kein zusätzliches Info-Symbol.

## Empfohlene Bedienung

- Neben jeder Feldbezeichnung steht ein dauerhaft sichtbares, dezentes Info-Symbol.
  Symbolgröße etwa 16 px, interaktive Fläche mindestens 28 × 28 px.
- Hover über Bezeichnung oder Symbol zeigt nach etwa 500 ms eine Hilfekarte.
  Der Eingabebereich bleibt frei von automatisch aufklappender Hilfe.
- Die Karte bleibt sichtbar, solange der Mauszeiger über Auslöser oder Karte liegt.
  Sie lässt sich mit Escape schließen; erneutes Öffnen erfordert erneutes Betreten
  des Auslösers. Es gibt keinen zeitgesteuerten Abbruch während des Lesens.
- Klick auf das Symbol hält dieselbe Karte offen. Erneuter Klick, Escape oder ein
  Klick außerhalb schließt sie. Es ist höchstens eine Hilfekarte offen.
- Das Info-Symbol ist per Tab erreichbar; Enter und Leertaste öffnen und schließen
  die Karte. Ein Klick verändert niemals den Einstellungswert.
- Bei deaktivierten Eingabefeldern bleibt das Info-Symbol bedienbar. Die Hilfe nennt
  die Voraussetzung oder den aktuellen Grund der Sperre.
- Beim Wechsel des Einstellungsbereichs wird offene Hilfe geschlossen.

Die Karte steht nahe beim Symbol und weicht an Fensterkanten aus. Richtwert für die
Breite: 320–400 px, begrenzt durch den verfügbaren Platz; Text wird umgebrochen.
Die vorhandene kompakte Kartenstruktur, Typografie und Theme-Farben bleiben erhalten.

Bestehende kurze Beschreibungen bleiben zunächst sichtbar. Kritische Hinweise,
Validierungsfehler und aktueller Betriebsstatus bleiben immer direkt auf der Seite.
Sie dürfen nicht ausschließlich in der Hilfe stehen.

## Inhalt der Hilfe

Eine Hilfe enthält den Feldtitel und ein bis drei kurze Sätze. Sie erklärt Wirkung,
Geltungsbereich und gegebenenfalls Voraussetzung oder nächsten Schritt. Beispiele
werden nur ergänzt, wenn sie das Verständnis erleichtern. Keine internen Property-Namen.

Die folgenden Texte sind deutsche Entwürfe; vor der Umsetzung werden sie mit den
jeweiligen Verhaltensverantwortlichen abgeglichen und ins Englische übersetzt.

| Eigenschaft oder Aktion | Entwurf / benötigter Inhalt |
| --- | --- |
| Sprache | „Legt die Sprache der Benutzeroberfläche fest. Die Oberfläche wechselt direkt nach der Auswahl.“ |
| Design | „Legt das Erscheinungsbild der Anwendung fest. Bei ‚System‘ richtet es sich nach der Windows-Einstellung. Die Auswahl wird sofort angewendet.“ |
| Akzentfarbe | „Legt die Hervorhebungsfarbe für Schaltflächen, Auswahl und weitere Bedienelemente fest. Die Auswahl wird sofort angewendet.“ |
| Mit Windows starten | „Startet DesktopAutomation automatisch nach der Windows-Anmeldung.“ Bei nicht unterstützter Installation oder durch Windows deaktiviertem Autostart den vorhandenen Status ergänzen. |
| Im Hintergrund starten | „Startet die App bei der Windows-Anmeldung ohne geöffnetes Hauptfenster. Das Symbol im Infobereich bleibt erreichbar; Automationen und Trigger bleiben aktiv.“ Bei ausgeschaltetem Autostart: „Aktiviere zuerst ‚Mit Windows starten‘.“ |
| Sofort-Stopp-Taste | „Stoppt laufende Jobs und Makros. Die Taste bleibt auch bei blockierten Eingaben verfügbar. Klicke auf ‚Ändern‘ und drücke anschließend die gewünschte Taste.“ |
| Updates prüfen | Wirkung der manuellen Prüfung erklären. Hilfe und sichtbarer Status unterscheiden die bestehende Aktualisierung in der App, MSIX und lokale Builds; keine installierungsunabhängigen Updateversprechen. |
| Versionshinweise | „Zeigt die Änderungen der veröffentlichten Anwendungsversionen.“ |
| Zugangsdaten suchen | „Filtert die gespeicherten Zugangsdaten nach Name oder Beschreibung.“ |
| Name | „Bezeichnet den gespeicherten Eintrag, damit du ihn später wiederfindest. Der Name ist erforderlich.“ |
| Beschreibung | „Optionaler Hinweis zum Zweck oder zur Verwendung des Eintrags. Trage hier keinen geheimen Wert ein.“ |
| Geheimer Wert | „Der geschützte Wert wird nach dem Speichern nicht angezeigt. Verwende ‚Secret ersetzen‘, um einen neuen Wert zu hinterlegen.“ |
| Bearbeiten | „Ändert Name und Beschreibung. Der gespeicherte geheime Wert bleibt erhalten.“ |
| Secret ersetzen | „Ersetzt den gespeicherten geheimen Wert durch eine neue Eingabe.“ |
| Löschen | Entfernen des gespeicherten Eintrags und Folgen für Verwendungen erklären; genaue Folgen vor der Umsetzung am Secret-Verbrauch prüfen. Bestehende Löschbestätigung beibehalten. |
| Aktualisieren / zuletzt geändert | Neu laden der gespeicherten Liste beziehungsweise Zeitpunkt der letzten Änderung erklären. |

Bei Allgemein bleibt die automatische Übernahme der Änderungen erkennbar. Im
Zugangsdaten-Editor wird ausdrücklich erklärt, dass Änderungen erst mit Speichern
übernommen werden. Hilfe darf keine geheimen Werte enthalten oder aus Eingaben ableiten.

## Technische Umsetzung

Ein gemeinsames WPF-Control für Kontexthilfe übernimmt Symbol, Hilfekarte,
Hover-/Klickzustand, Tastaturbedienung und Schließen. Die Views liefern Titel,
lokalisierten Hilfetext und optional einen lokalisierten Zustandsgrund.
Hover und Klick verwenden denselben Inhalt und dieselbe Darstellung; keine getrennt
gepflegten Tooltip- und Popup-Texte.

Vorhandene Icon- und Theme-Ressourcen aus `DesktopAutomationApp/Styles/` verwenden.
Das bereits verwendete `InformationOutline` ist der visuelle Ausgangspunkt;
die lokale Darstellung in `JobStepsView.xaml` wird nicht unverändert kopiert,
insbesondere muss Tastaturfokus sichtbar bleiben.

Hilfetexte liegen unter semantischen Schlüsseln wie `Settings.Theme.Help` in
`Strings.resx` und `Strings.en.resx`. Vorhandene Beschreibungen werden wiederverwendet,
wenn sie inhaltlich ausreichen. Die Hilfekarte folgt Sprach- und Theme-Wechseln.

Die vorhandenen Preferences-, Startup-, Update- und Secret-Dienste bleiben die
Verantwortlichen für Verhalten und Verfügbarkeit. Hilfe verwendet deren bestehende
Zustände; sie implementiert keine zweite Regel für die Aktivierung von Feldern.
Gespeicherte Einstellungen und Dateiformate werden nicht erweitert.

Das Eingabefeld erhält einen aussagekräftigen Automation-Namen und HelpText.
Das Symbol erhält beispielsweise „Hilfe zu Design“, eine stabile AutomationId und
den geöffneten Zustand für assistive Technologien. Hilfe ist auch ohne Maus vollständig
lesbar. Beim Schließen bleibt beziehungsweise kehrt der Fokus sinnvoll zum Auslöser zurück.

## Umsetzung und Abnahme

1. Gemeinsame Hilfe-Komponente und vollständige Hilfe für Allgemein erstellen.
2. Updates und Zugangsdaten inklusive aller Editorfelder und besonderer Zustände ergänzen.
3. Deutsche und englische Texte prüfen; relevante Produktdokumentation und Screenshots
   nach Repository-Vorgaben aktualisieren, Release Notes zur sichtbaren Verbesserung ergänzen.

Abnahmebedingungen:

- Jedes Feld im beschriebenen Umfang besitzt Hilfe; Aktionen erklären ihre Wirkung.
- Hover, Wechsel vom Symbol zur Karte, Klick, Escape, Außenklick und Bereichswechsel
  funktionieren ohne gleichzeitige oder hängenbleibende Karten.
- Deaktivierte Felder erklären ihren aktuellen Zustand; Hilfe verändert keine Werte
  und löst keine Updateprüfung, Speicherung oder andere Fachaktion aus.
- Tastatur und Screenreader erhalten dieselben Informationen wie Mausnutzer.
- Deutsch und Englisch, Light/Dark/Black sowie schmale Fenster und 100/150/200 %
  Skalierung werden am tatsächlichen WPF-Rendering geprüft.
- Gezielte UI-/Interaktionstests decken das gemeinsame Control, Zustandsgründe und
  Bindings ab. Betroffene Projekte werden gebaut und Localization- sowie erforderliche
  Dokumentationsprüfungen müssen erfolgreich sein.

Für das vorliegende Konzept wurden die Quellen gelesen; es wurden keine Builds,
Tests oder visuellen Prüfungen der laufenden Anwendung durchgeführt.
