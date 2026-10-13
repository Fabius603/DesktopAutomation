## Logs: LogOutcome

ID: LogOutcome
Website: /doku/logs/LogOutcome/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### option.Failed

Lauf wegen Fehler fehlgeschlagen.

### option.Interrupted

Lauf hatte beim Neustart keinen belegten Abschluss und gilt als unterbrochen/unvollständig.

### option.Paused

Lauf ist pausiert; das ist kein erfolgreicher Abschluss.

### option.Running

Lauf ist noch aktiv und hat keinen belegten Abschluss.

### option.Stopped

Lauf wurde ausdrücklich gestoppt/abgebrochen.

### option.Successful

Lauf erfolgreich beendet, ohne die Qualität als Warnung/Fehler einzustufen.

### option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

### option.WithErrors

Lauf abgeschlossen, aber Fehler beeinträchtigen seine Qualität; nicht als problemfrei auswerten.

### option.WithWarnings

Lauf beendet, dabei wurden Warnungen erfasst.

### purpose

Lebenszyklus und Qualität eines Laufs. Ein abgeschlossener Lauf kann Warnungen oder Fehler enthalten; ein unterbrochener Lauf hat keinen erfundenen erfolgreichen Abschluss.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogOutcome",
  "name": "LogOutcome",
  "options": [
    "Running",
    "Paused",
    "Successful",
    "WithWarnings",
    "WithErrors",
    "Failed",
    "Stopped",
    "Interrupted",
    "Unknown"
  ],
  "fields": []
}
```

