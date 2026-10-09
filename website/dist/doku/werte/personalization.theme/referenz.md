## Werte und Ergebnisse: personalization.theme

ID: personalization.theme
Website: /doku/werte/personalization.theme/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle personalization.theme im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.theme

Gewünschte Windows-Theme-Option aus der angebotenen Liste.

### field.theme.option.dark

Windows-Farbmodus Dunkel auswählen.

### field.theme.option.light

Windows-Farbmodus Hell auswählen.

### purpose

Ändert die unterstützte Windows-Farb-/Theme-Einstellung entsprechend theme. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "personalization.theme",
  "name": "personalization.theme",
  "fields": [
    {
      "id": "theme",
      "name": "Darstellung",
      "type": "Enum",
      "Required": true,
      "defaultValue": "dark",
      "options": [
        "light",
        "dark"
      ]
    }
  ],
  "result": null
}
```

