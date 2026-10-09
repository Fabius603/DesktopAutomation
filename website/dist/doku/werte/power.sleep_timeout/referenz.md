## Werte und Ergebnisse: power.sleep_timeout

ID: power.sleep_timeout
Website: /doku/werte/power.sleep_timeout/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle power.sleep_timeout im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.minutes

Zeit bis zur Energieaktion in Minuten. Die vom Katalog angegebenen Grenzen und die gewählte Energiequelle beachten.

### field.power_source

Energiequelle, für die die Einstellung gilt, gemäß den angebotenen Optionen.

### field.power_source.option.ac

Die Änderung gilt bei Netzstrom.

### field.power_source.option.both

Die Änderung gilt bei Netzstrom und Akkubetrieb.

### field.power_source.option.dc

Die Änderung gilt bei Akkubetrieb.

### purpose

Ändert die Standby-Zeit in Minuten für die angegebene Energiequelle. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "power.sleep_timeout",
  "name": "power.sleep_timeout",
  "fields": [
    {
      "id": "minutes",
      "name": "Standby-Timeout (Minuten)",
      "type": "Integer",
      "Required": true,
      "defaultValue": "30",
      "options": []
    },
    {
      "id": "power_source",
      "name": "Stromquelle",
      "type": "Enum",
      "Required": true,
      "defaultValue": "both",
      "options": [
        "both",
        "ac",
        "dc"
      ]
    }
  ],
  "result": null
}
```

