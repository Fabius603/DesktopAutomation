## Werte und Ergebnisse: Laufwerk eingebunden

ID: storage.drive.mounted
Website: /doku/werte/storage.drive.mounted/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle storage.drive.mounted als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.name

Prozessname (etwa notepad) beziehungsweise Laufwerksname (etwa C:), entsprechend der Windows-Funktion.

### purpose

Ereignis: Laufwerk eingebunden. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "storage.drive.mounted",
  "name": "Laufwerk eingebunden",
  "fields": [
    {
      "id": "name",
      "name": "Laufwerk",
      "type": "Drive",
      "Required": false,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

