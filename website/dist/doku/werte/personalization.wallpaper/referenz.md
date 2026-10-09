## Werte und Ergebnisse: personalization.wallpaper

ID: personalization.wallpaper
Website: /doku/werte/personalization.wallpaper/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle personalization.wallpaper im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.path

Lokaler Datei-/Ordnerpfad beziehungsweise vorhandene Hintergrundbilddatei, entsprechend der Funktion.

### purpose

Setzt die vorhandene Bilddatei aus path als Desktop-Hintergrund. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "personalization.wallpaper",
  "name": "personalization.wallpaper",
  "fields": [
    {
      "id": "path",
      "name": "Hintergrundbild",
      "type": "FilePath",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

