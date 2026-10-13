## Logs: LogArea

ID: LogArea
Website: /doku/logs/LogArea/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### option.Capture

Bildschirm-/Kameraaufnahme.

### option.Detection

Bild-/Text-/Punkterkennung und Auswertung.

### option.Execution

Ausführung und Ablaufsteuerung.

### option.FileAccess

Datei-/Ordnerzugriffe und ihre Diagnose.

### option.General

Allgemeiner Log-Fachbereich ohne spezielleren Ausführungsbereich.

### option.Process

Programmstart, Suche, Fokus und Beenden.

### option.Script

Skriptausführung und zugehörige Hintergrundarbeit.

### option.Video

Videoaufnahme und Finalisierung.

### option.Windows

Windows-Taste als Modifikator (Bitmaske 8) beziehungsweise Windows-Fachbereich, wenn diese Option zu einem Log-Bereich gehört.

### purpose

Typisierter Fachbereich für Diagnose und Filter. Er beschreibt die fachliche Herkunft und wird nicht aus frei formulierten Nachrichtentexten erraten.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogArea",
  "name": "LogArea",
  "options": [
    "General",
    "Execution",
    "Automation",
    "FileAccess",
    "Capture",
    "Detection",
    "Process",
    "Script",
    "Video",
    "Windows"
  ],
  "fields": []
}
```

