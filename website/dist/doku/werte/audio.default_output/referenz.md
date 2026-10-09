## Werte und Ergebnisse: audio.default_output

ID: audio.default_output
Website: /doku/werte/audio.default_output/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle audio.default_output im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.device_name

Name eines auf dem Zielrechner vorhandenen Audiogeräts. Die dynamische Auswahl des Editors berücksichtigt Ausgabe- beziehungsweise Eingabegeräte.

### purpose

Wählt ein vorhandenes Standard-Ausgabegerät über dessen Gerätenamen. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "audio.default_output",
  "name": "audio.default_output",
  "fields": [
    {
      "id": "device_name",
      "name": "Ausgabegerät",
      "type": "Text",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

