# Pflege der Produktreferenz

Die Produktdokumentation umfasst UI-Anleitungen, exakte JSON-Verträge und eine vollständige Feld-/Options-/Ergebnisreferenz. Website und Agenten-Downloads werden aus denselben Quellen erzeugt; es gibt keinen offenen Erklärungsvorrat mehr.

## Benutzer und Entwickler getrennt führen

`/doku/` bietet zwei Einstiege: `/doku/benutzer/` für die Bedienung und
`/doku/entwickler/` für Textdateien, Verträge und Agenten. Auf Funktionsseiten stehen
markierte App-Bilder, Bedienung, Ergebnisbedeutung, Beispiele und Fehlerhinweise zuerst.
Technische Feldnamen, Typen, Standardwerte, Eingabe-/Ergebnisverträge und JSON-Vorlagen
stehen gesammelt in einem geschlossenen Entwicklerbereich am Seitenende.

Gemischte Anleitungen ordnen ihre technischen Abschnitte über `DEVELOPER_SECTIONS` im
Generator zu. Neue Abschnitte passend zuordnen; neue JSON-/Agenten-Anleitungen in
`DEVELOPER_GUIDES` aufnehmen. Bestehende URLs und Feldanker erhalten: Direktlinks öffnen
die zugehörigen Details automatisch. Markdown und JSON behalten die vollständige Referenz
ohne einklappbare Teile. Beide Zielgruppen werden weiterhin aus denselben Quellen gepflegt.

## Inhaltsquellen

- `metadata.json`: Export der bestehenden Step-, Ergebnis-, Windows-, Automations-, Makro- und Logverträge sowie Hashes der relevanten Implementierungen. Nicht von Hand bearbeiten.
- `content.json`: gepflegte Erklärungen je stabiler Referenz-ID. Jede Seite hat `purpose`, `example` und `errors`; weitere Schlüssel beginnen mit `field.`, `schema.`, `input.`, `result.` oder `option.`. Enum-Optionen besitzen jeweils eigene Erklärungsschlüssel. Die Seiten rendern diese Texte sicher als Text, einschließlich Verweisen auf Werte mit stabilen Ankern.
- `pending.json`: geschlossener Aufschubbestand mit leerer Eintragsliste. Nicht zur Umgehung fehlender Erklärungen erweitern.
- `authoring.json`: direkt vom kanonischen Serializer erzeugte Dateihüllen und Defaultvorlagen, aktuelle Formatversionen, zusammengesetzte Binding-Schemas und deren Feldzuordnung. Zusätzlich ein mit dem App-Validator geprüftes vollständiges Werteverbindungsbeispiel. Defaultvorlagen sind strukturell und können erforderliche Ziele/Werte vermissen; diese Unterscheidung bleibt sichtbar.
- `../tools/guide_content.py`: redaktionelle Anleitungen für UI und JSON, einschließlich Dateiformaten, Werteverbindungen, Makros, Automationen, Logs, Windows-Funktionen, Fehlerbehebung und Agenten. Prosa, Tabellen und Code werden gemeinsam als HTML und lesbarer Text ausgegeben.
- Generierte Agenten-Zugänge: `dist/llms.txt`, `dist/llms-full.txt`, `dist/doku/referenz.md` und `dist/doku/vertragsdaten.json`. `dist/doku/referenz-index.json` erschließt einzelne Markdown- und JSON-Referenzen pro Funktion für kleine Agenten-Kontexte. Der JSON-Datensatz ist eine Referenz, kein JSON-Schema und kein Ersatz für die App-Validatoren.
- `examples/`: kanonisch erzeugte Job-, Automations- und Makrodateien. Geändert werden die Fixture-Definitionen in `../tools/Exporter/Examples.cs`, nicht die JSON-Ausgabe.
- `examples.json`: vollständige Ablauf- und Einrichtungstexte der drei Beispiele.
- `generated-files.json`: Manifest der erzeugten Referenzseiten, Suchdaten und Pakete. Veraltete URLs benötigen eine Weiterleitung oder eine Kompatibilitätsseite.

## Arbeiten mit Änderungen

Nach einer Codeänderung aus dem Repository-Stamm:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\website\Build.ps1
```

Vor dem Erzeugen die betroffenen Inhalte in `content.json`, die UI-/JSON-Anleitungen in `guide_content.py` und gegebenenfalls Beispiele aktualisieren. Neue Referenzen und neue Feld-/Optionsschlüssel müssen gezielt ergänzt und ausgefüllt werden. Ein fehlender Schlüssel führt zum Fehler; der Generator schreibt keine ungeprüften Erklärungen. Die Initialisierung `--init-content` ist ausschließlich für einen noch nicht vorhandenen Inhaltsbestand gedacht.

Bei geänderten sichtbaren App-Ansichten zusätzlich `-Render` verwenden, danach die erzeugten PNGs prüfen. Der Renderer nutzt die vorhandenen Test-Renderressourcen und die echte WPF-Anwendung mit isolierten Fixtures. Er führt keine Jobs aus, aktiviert keine Automationen und schreibt nicht in App-Profile.

Nach reinen Textänderungen genügt `python website/tools/build_site.py`. Dieser Aufruf akzeptiert keinen veralteten Implementierungsstand: zuerst muss gegebenenfalls der Export aktualisiert werden.

Prüfen:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Focused -Checks website-docs,documentation,skills
```

`website-docs` ist auch im vollständigen CI-/Release-Check enthalten. Er baut und führt den Exporter aus, prüft mit den App-Validatoren die Beispielkonfigurationen, vergleicht Export und Ausgabe und prüft Abdeckung, Verweise sowie Regressionstests des Dokumentationsablaufs einschließlich Agenten-Downloads, HTML-Escaping und unveränderter JSON-Vorlagen. Ordner-Trigger werden mit einem isolierten existierenden Testordner validiert; das ersetzt die Einrichtung auf dem Zielrechner nicht. Job-Ausführung auf einem echten Desktop ist nicht Teil dieser Prüfung.

Der verbindliche Website-Check verwendet `website/Build.ps1 -Check -Strict` und scheitert, sobald ein Erklärungstext fehlt. Durch den leeren Aufschubbestand akzeptiert auch der normale Build keine fehlenden Erklärungen. Die Generatoren prüfen strukturelle Aktualität, keine vollständige semantische Richtigkeit; diese bleibt Aufgabe der Agenten und des Reviews. Bei Produktänderungen beide Zugänge pflegen: konkrete Bedienung und genaue Dateiverträge mit Einheiten, Typen und vollständigen Beispielen.

## Version und Veröffentlichung

Die vollständigen Referenzen beschreiben ausdrücklich den aktuellen Arbeitsbaum. Referenz- und Beispiel-Detailseiten behalten `noindex,follow`, solange diese Versionszuordnung nicht als Release-Dokumentation veröffentlicht wird. Vor einer Veröffentlichung den passenden App-Stand auswählen und die Versionszuordnung prüfen. Veröffentlichung bleibt ein gesonderter Auftrag.

## Bilder pflegen

Der [Screenshot-Ablauf](screenshots.md) ergänzt den Dokumentationsprozess: Katalog `screenshots.json`, gezielte WPF-Generierung, explizite Sichtprüfung und gespeicherte Quellen-/PNG-Hashes in `screenshots-state.json`. Die Doku-Zuordnung wird automatisch gerendert. `website-docs` prüft auch die Bildpflege und führt deren Regressionstests aus.

Die Benutzerreferenz erläutert auch Auswahloptionen, Sichtbarkeitsregeln und echte Unterfelder zusammengesetzter Einstellungen. Beispielwerte stehen neben der Erklärung; technische Binding-Mitglieder bleiben im Entwicklerbereich. Bei Änderungen beide Darstellungen und die vollständigen Feld-Ausschnitte prüfen. Modellstandards sind keine ausführbare Beispielkonfiguration.

Der Exporter übernimmt `uiFieldIds` aus den kanonischen Editorabschnitten. Einstellungen, die nur noch zum kompatiblen Dateivertrag gehören, bleiben in der Entwicklerreferenz und werden nicht als vorhandene UI-Steuerelemente beschrieben.
