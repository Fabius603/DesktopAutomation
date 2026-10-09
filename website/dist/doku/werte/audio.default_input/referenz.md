## Werte und Ergebnisse: audio.default_input

ID: audio.default_input
Website: /doku/werte/audio.default_input/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle audio.default_input im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.device_name

Name eines auf dem Zielrechner vorhandenen Audiogeräts. Die dynamische Auswahl des Editors berücksichtigt Ausgabe- beziehungsweise Eingabegeräte.

### purpose

Wählt ein vorhandenes Standard-Mikrofon über dessen Gerätenamen. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "audio.default_input",
  "name": "audio.default_input",
  "fields": [
    {
      "id": "device_name",
      "name": "Mikrofon",
      "type": "Text",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

