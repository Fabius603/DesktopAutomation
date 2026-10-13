## Logs: StepSkipped

ID: step.skipped
Website: /doku/logs/step.skipped/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### purpose

Der Step wurde bewusst nicht ausgeführt, zum Beispiel deaktiviert, inaktiv oder ohne verfügbare optionale Eingabe. Reason erklärt den Unterschied.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "step.skipped",
  "name": "StepSkipped"
}
```

