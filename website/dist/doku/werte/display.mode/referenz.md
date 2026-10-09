## Werte und Ergebnisse: display.mode

ID: display.mode
Website: /doku/werte/display.mode/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle display.mode im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.mode

Konkreter Betriebs-/Projektions- beziehungsweise Nicht-stören-Modus; exakt eine der für diese Funktion angebotenen Optionen verwenden.

### field.mode.option.duplicate

Bildschirme duplizieren.

### field.mode.option.extend

Desktop über Bildschirme erweitern.

### field.mode.option.external

Nur den externen Bildschirm verwenden.

### field.mode.option.internal

Nur den internen Bildschirm verwenden.

### purpose

Ändert den Bildschirm-Betriebsmodus auf eine vom Katalog angebotene Projektionsart. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "display.mode",
  "name": "display.mode",
  "fields": [
    {
      "id": "mode",
      "name": "Bildschirmmodus",
      "type": "Enum",
      "Required": true,
      "defaultValue": "extend",
      "options": [
        "extend",
        "duplicate",
        "internal",
        "external"
      ]
    }
  ],
  "result": null
}
```

