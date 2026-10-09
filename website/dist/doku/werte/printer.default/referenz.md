## Werte und Ergebnisse: printer.default

ID: printer.default
Website: /doku/werte/printer.default/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle printer.default im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.printer_name

Name eines auf diesem Rechner installierten Druckers.

### purpose

Setzt den installierten Drucker mit printer_name als Standarddrucker. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "printer.default",
  "name": "printer.default",
  "fields": [
    {
      "id": "printer_name",
      "name": "Standarddrucker",
      "type": "Text",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

