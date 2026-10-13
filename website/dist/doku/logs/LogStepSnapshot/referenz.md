## Logs: LogStepSnapshot

ID: LogStepSnapshot
Website: /doku/logs/LogStepSnapshot/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### field.Enabled

Ob der Step im Lauf-Snapshot aktiviert war. Disabled ist von einem aktivierten, aber nicht erreichten Step zu unterscheiden.

### field.Id

Eindeutige Identität des Ereignisses beziehungsweise Laufs. Ereignis-ID und RunId haben unterschiedliche Aufgaben.

### field.Phase

Bereich der Ausführung: Startphase, Hauptphase oder Endphase. Er beeinflusst die Bedeutung fehlender und übersprungener Steps.

### field.Position

Position des Steps beziehungsweise der Bedingung im gespeicherten Snapshot.

### field.TypeId

Kanonische Step-Typ-ID, zum Beispiel file_system_operation, unabhängig vom sichtbaren Label.

### purpose

Gespeicherte Step-Struktur dieses Laufs: ID, kanonischer Typ, Reihenfolge, Phase und Aktivierung. Spätere Änderungen am Job verändern den damaligen Snapshot nicht.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogStepSnapshot",
  "name": "LogStepSnapshot",
  "options": [],
  "fields": [
    {
      "id": "Enabled",
      "name": "Enabled",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Id",
      "name": "Id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Phase",
      "name": "Phase",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Position",
      "name": "Position",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "TypeId",
      "name": "TypeId",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

