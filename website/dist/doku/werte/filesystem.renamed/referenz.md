## Werte und Ergebnisse: Datei oder Ordner umbenannt

ID: filesystem.renamed
Website: /doku/werte/filesystem.renamed/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle filesystem.renamed als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.include_subdirectories

Bei Dateisystemereignissen auch Unterordner beobachten.

### field.path

Lokaler Datei-/Ordnerpfad beziehungsweise vorhandene Hintergrundbilddatei, entsprechend der Funktion.

### purpose

Ereignis: Datei oder Ordner umbenannt. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "filesystem.renamed",
  "name": "Datei oder Ordner umbenannt",
  "fields": [
    {
      "id": "path",
      "name": "Datei oder Ordner",
      "type": "FilePath",
      "Required": true,
      "defaultValue": null,
      "options": []
    },
    {
      "id": "include_subdirectories",
      "name": "Unterordner",
      "type": "Boolean",
      "Required": false,
      "defaultValue": "false",
      "options": []
    }
  ],
  "result": null
}
```

