## Logs: LogTriggerSnapshot

ID: LogTriggerSnapshot
Website: /doku/logs/LogTriggerSnapshot/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### field.EventKind

Konkrete Ereignisart des Trigger-Snapshots.

### field.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.TargetId

Stabile ID des von der Automation angesprochenen Jobs/Makros.

### field.TargetKind

Art des Automationsziels: Job oder Makro.

### field.TargetName

Gespeicherter Anzeigename des Ziels zum Zeitpunkt der Beobachtung.

### field.WatchedDirectory

Explizit erlaubter beobachteter Ordnerpfad für eine Datei-Automation.

### purpose

Gespeicherte Ereignis- und Zielzuordnung einer Automation. Ermöglicht die Frage, welches Ereignis welche Aktion angefordert hat, ohne mutable aktuelle Konfigurationen als historische Wahrheit zu verwenden.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogTriggerSnapshot",
  "name": "LogTriggerSnapshot",
  "options": [],
  "fields": [
    {
      "id": "EventKind",
      "name": "EventKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "TargetId",
      "name": "TargetId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "TargetKind",
      "name": "TargetKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "TargetName",
      "name": "TargetName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "WatchedDirectory",
      "name": "WatchedDirectory",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

