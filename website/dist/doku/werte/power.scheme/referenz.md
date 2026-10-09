## Werte und Ergebnisse: power.scheme

ID: power.scheme
Website: /doku/werte/power.scheme/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle power.scheme im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.scheme

Kennung eines vorhandenen Energiesparplans; keine beliebige Bezeichnung eines neuen Plans.

### field.scheme.option.balanced

Den vorhandenen ausgewogenen Energiesparplan aktivieren.

### field.scheme.option.high_performance

Den vorhandenen Hochleistungsplan aktivieren.

### field.scheme.option.power_saver

Den vorhandenen Energiesparplan aktivieren.

### purpose

Aktiviert den angegebenen vorhandenen Energiesparplan. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "power.scheme",
  "name": "power.scheme",
  "fields": [
    {
      "id": "scheme",
      "name": "Energieschema",
      "type": "Enum",
      "Required": true,
      "defaultValue": "balanced",
      "options": [
        "balanced",
        "high_performance",
        "power_saver"
      ]
    }
  ],
  "result": null
}
```

