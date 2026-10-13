## Logs: LogReadState

ID: LogReadState
Website: /doku/logs/LogReadState/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### option.Available

Logdaten sind verfügbar; zusätzlich IsComplete und Issues prüfen.

### option.Empty

Der abgefragte Logbereich enthält keine Ereignisse; das beweist keine erfolgreiche Ausführung.

### option.Partial

Daten sind teilweise lesbar; Fehler und bekannte Lücken bleiben sichtbar.

### option.Unavailable

Logdaten sind nicht verfügbar; Abwesenheit von Fehlern ist dann keine Erfolgsaussage.

### purpose

Qualität des Leseergebnisses: vollständig, teilweise oder nicht verfügbar gemäß den konkreten Enum-Werten. Teilweise lesbare Daten werden nicht verworfen, aber verlorene Daten nicht rekonstruiert.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogReadState",
  "name": "LogReadState",
  "options": [
    "Available",
    "Empty",
    "Partial",
    "Unavailable"
  ],
  "fields": []
}
```

