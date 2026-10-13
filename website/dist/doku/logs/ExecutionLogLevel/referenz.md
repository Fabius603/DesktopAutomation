## Logs: ExecutionLogLevel

ID: ExecutionLogLevel
Website: /doku/logs/ExecutionLogLevel/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### option.Debug

Technische Detailbeobachtung.

### option.Error

Fehlerbeobachtung; den tatsächlichen Laufabschluss zusätzlich über Outcome prüfen.

### option.Information

Normale Lebenszyklus-/Aktionsinformation.

### option.Warning

Problemhinweis, der nicht zwingend den Lauf beendet.

### purpose

Schweregrad eines einzelnen Ereignisses. Filtern über MinimumLevel kann technische Details verbergen; bei Diagnose zusätzlich Run-Status und Vollständigkeit prüfen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "ExecutionLogLevel",
  "name": "ExecutionLogLevel",
  "options": [
    "Debug",
    "Information",
    "Warning",
    "Error"
  ],
  "fields": []
}
```

