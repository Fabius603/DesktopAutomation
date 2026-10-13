## Logs: StepLogOutcome

ID: StepLogOutcome
Website: /doku/logs/StepLogOutcome/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### option.Cancelled

Eine begonnene Step-Ausführung wurde abgebrochen.

### option.Failed

Lauf wegen Fehler fehlgeschlagen.

### option.NotExecuted

Der Step wurde in diesem Ablauf nicht ausgeführt.

### option.Pending

Noch kein terminales Step-Ergebnis vorhanden.

### option.Running

Lauf ist noch aktiv und hat keinen belegten Abschluss.

### option.Skipped

Der Step wurde übersprungen; Reason für deaktiviert, inaktiven Zweig oder fehlende Eingabe prüfen.

### option.Successful

Lauf erfolgreich beendet, ohne die Qualität als Warnung/Fehler einzustufen.

### option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

### option.Warning

Problemhinweis, der nicht zwingend den Lauf beendet.

### purpose

Status einer Step-Beobachtung beziehungsweise Timeline-Projektion. Disabled, inactive, skipped und noch nicht ausgeführt haben unterschiedliche Ursachen; fehlende Belege nicht als Ausführung zählen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "StepLogOutcome",
  "name": "StepLogOutcome",
  "options": [
    "Running",
    "Successful",
    "Warning",
    "Failed",
    "Skipped",
    "Cancelled",
    "NotExecuted",
    "Pending",
    "Unknown"
  ],
  "fields": []
}
```

