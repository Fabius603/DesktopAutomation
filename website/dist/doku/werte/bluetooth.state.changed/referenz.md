## Werte und Ergebnisse: Bluetooth-Geräteliste geändert

ID: bluetooth.state.changed
Website: /doku/werte/bluetooth.state.changed/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle bluetooth.state.changed als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.filter_value

Optionaler Textfilter für die angebotene Geräteabfrage beziehungsweise Ereignisquelle. Seine konkrete Wirkung hängt vom Geräteprovider ab.

### purpose

Ereignis: Bluetooth-Geräteliste geändert. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "bluetooth.state.changed",
  "name": "Bluetooth-Geräteliste geändert",
  "fields": [
    {
      "id": "filter_value",
      "name": "Filter",
      "type": "Text",
      "Required": false,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

