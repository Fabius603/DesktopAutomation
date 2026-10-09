## Werte und Ergebnisse: display.primary

ID: display.primary
Website: /doku/werte/display.primary/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle display.primary im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.display_name

Systemname eines vorhandenen Displays, etwa \\.\DISPLAY2, nicht ein frei formulierter Fenstertitel.

### purpose

Wählt den vorhandenen Bildschirm mit display_name als Hauptbildschirm. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "display.primary",
  "name": "display.primary",
  "fields": [
    {
      "id": "display_name",
      "name": "Hauptbildschirm",
      "type": "Text",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

