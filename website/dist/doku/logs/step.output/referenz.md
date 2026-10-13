## Logs: StepOutput

ID: step.output
Website: /doku/logs/step.output/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### purpose

Bereinigte Beobachtung einer Step-/Skriptausgabe. Der Verlauf erfasst beispielsweise Stream und Zeichenanzahl statt kompletten Eingabetext.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "step.output",
  "name": "StepOutput"
}
```

