# Screenshot-Ablauf für Website und Doku

Der Katalog `screenshots.json` ist die einzige Zuordnung von App-Ansichten, PNG-Dateien,
Abmessungen, Bildbeschreibungen, Doku-Seiten und Quelldateien. Der WPF-Renderer verwendet echte
Views mit isolierten Beispieldaten und gesperrten Ausführungsdiensten. Standard: Light.Blue,
de-DE, 96 DPI und die Fenstergröße der jeweiligen Katalogzeile.

## Schnellbefehle

Aus dem Repository-Stamm in PowerShell:

```powershell
./website/Build.ps1 -ListScreenshots
./website/Build.ps1 -Screenshots "verlauf,automationen"
./website/Build.ps1 -ChangedScreenshots
./website/Build.ps1 -Render
```

`-Screenshots` rendert ausschließlich die genannten IDs. `-ChangedScreenshots` rendert Bilder,
deren Quellen oder PNGs vom gespeicherten Stand abweichen. Unbekannte IDs scheitern vor dem
Rendererstart. Der Exporter stellt die aktuellen internen Beispielkonfigurationen bereit; es
werden keine Jobs ausgeführt. `-Check` ist schreibgeschützt und lässt sich nicht mit Rendering
kombinieren. `-ListScreenshots` benötigt keinen .NET-Build.

## Sichtprüfung und Abschluss

Nach dem Rendern sind die ausgewählten Bilder noch nicht visuell freigegeben. Öffne die PNGs
und die betroffenen Doku-Seiten: richtige Funktion und Auswahl, Lightmode, lesbare und nicht
abgeschnittene Felder, passende Bildbeschreibung, keine persönlichen Daten. Danach:

```powershell
python website/tools/screenshots.py approve --ids "verlauf,automationen"
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/verify.ps1 -Mode Focused -Checks website-docs,documentation,skills
```

Der Renderer speichert Quellen- und Bildhashes in `screenshots-state.json`. Der strenge
Website-Check scheitert bei veränderten Quellen, fehlenden/falschen PNGs, fehlender Sichtprüfung
oder fehlenden Bildern auf zugeordneten Doku-Seiten. Er rendert und bestätigt nichts selbst.
Ein gezielter Renderlauf darf andere Bilder noch offenlassen; der Abschlusscheck prüft alle.
Hashes sind kein Beweis für eine korrekte Darstellung. `approve` dokumentiert die tatsächliche
Sichtprüfung des Agenten und darf sie nicht ersetzen.

## Neue Funktion bebildern

Der Katalog enthält 67 Aufnahmen für alle 39 Step-Arten, zehn Makro-Befehle, elf Triggerarten
und grundlegende Bedienabläufe. Die Doku verwendet deren zugeschnittene `.detail.png`-Ansichten.
Der vollständige Mitschnitt bleibt nur als Quell-/Galerieasset erhalten. `crop` gibt den Bereich
als `[x, y, breite, maximaleHöhe]` an. Die tatsächliche Höhe folgt den sichtbaren Markierungen. Mit `fieldDetails: true` erzeugt der Renderer stattdessen eine ausdrücklich beschriebene Zusammenstellung vollständiger nativer Feld-Ausschnitte. Sie enthält auch unterhalb des Scrollbereichs angeordnete Einstellungen; ihre Höhe darf deshalb größer als die Fensteraufnahme sein.
`markLabels` benennt tatsächliche UI-Beschriftungen. Der Renderer sucht sie im gerenderten
Visual Tree, berücksichtigt Scroll-Clipping und zeichnet Rahmen, Verbindungslinien und Zahlen.
`markNotes` erklärt die markierten Werte. Die erzeugte `.detail.json` enthält die tatsächlich
gefundenen Beschriftungen und Nummern. Feld-Zusammenstellungen enthalten außerdem `mode: fields` und die vollständigen Rahmenkoordinaten und tatsächlich angezeigten Eingabewerte je Feld. Leere erfasste Eingaben werden vom Check abgelehnt. Der Check prüft, dass jeder Rahmen innerhalb des Detailbilds liegt. Detail-PNG und Zuordnung werden ebenfalls gehasht.

Prüfe jeden Ausschnitt: Beschriftung und zugehörige Eingabe gemeinsam eingerahmt, alle sichtbaren Eingaben mit sinnvollen Beispielwerten (einschließlich ausgeschalteter Häkchen und gültiger Nullen), relevante Einstellung sichtbar, passender Editor ausgewählt, keine
abgeschnittenen Ziele oder leeren Fensterflächen, jede Nummer mit passender Erklärung.
Bei verschachtelten oder bedingt sichtbaren Feldern zeigen zusätzliche Schemaübersichten die
exakten Feldnamen, Typen, Standards, Optionen und Ergebnisse; sie sind als Schema gekennzeichnet
und stellen keine Bedienoberfläche dar. Schemaübersichten existieren für jede Produktreferenz
und für die einzelnen Anleitungsabschnitte. Markierungen und Ausschnittdateien stehen auch in
der Markdown-/JSON-Agentenreferenz. Keine echte Kamera oder Ausführung ist für eine Fixture nötig;
eine Aufnahme ohne angeschlossenes Gerät darf nicht als echte Kameraaufnahme ausgegeben werden.

1. Eine Fixture im bestehenden Renderer ergänzen und über eine neue stabile ID auswählbar
   machen. Gemeinsamen Capture-Code verwenden; keine Eingabeaktionen oder Live-Daten ausführen.
2. Im Katalog eine Zeile mit `id`, `file`, `width`, `height`, `title`, `caption`, `pages` und
   `sources` hinzufügen. Jede Quelle muss existieren; Globs berücksichtigen auch neue Dateien.
3. `sources` um View, Code-behind, ViewModel, Editorressourcen und Fixture-Daten ergänzen.
   `commonSources` enthält Abhängigkeiten aller Bilder. Änderungen an der Beobachtungsliste
   verlangen ebenfalls einen neuen Renderlauf.
4. In `pages` die passenden bestehenden `/doku/.../`-Adressen eintragen. Der Generator bindet
   Bilder mit Beschreibung, Originalgrößen-Link und Lazy Loading automatisch ein. Markdown-
   und JSON-Referenzen bekommen die Bildinformationen ebenfalls.
5. Gezielt rendern, visuell prüfen, bestätigen und die Abschlusschecks ausführen.

Der Repository-Skill `.agents/skills/maintain-website-screenshots/SKILL.md` und die Agentenregeln
machen diese Pflege zum Bestandteil betroffener Änderungen. Noch nicht bebilderte Funktionen
bekommen bei Bedarf eigene Ansichten; der Katalog behauptet keine vollständige Bildabdeckung
aller Step-Felder. Aktualisierte Dateien liegen lokal in `website/dist`; Deployment ist ein
eigener Auftrag.
