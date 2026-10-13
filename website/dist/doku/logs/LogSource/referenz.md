## Logs: LogSource

ID: LogSource
Website: /doku/logs/LogSource/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### option.Application

Ereignis stammt aus der Anwendung selbst.

### option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### option.Job

Quelle beziehungsweise Ziel ist ein Job.

### option.Makro

Quelle beziehungsweise Ziel ist ein Makro.

### purpose

Ursprungsart des Ereignisses: Job, Automation, Anwendung oder Makro. Zusammen mit SourceId und RunId werden einzelne Läufe eingegrenzt.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogSource",
  "name": "LogSource",
  "options": [
    "Job",
    "Automation",
    "Application",
    "Makro"
  ],
  "fields": []
}
```

