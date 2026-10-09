## Werte und Ergebnisse: Farben geändert

ID: system.settings.colors_changed
Website: /doku/werte/system.settings.colors_changed/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle system.settings.colors_changed als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### purpose

Ereignis: Farben geändert. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "system.settings.colors_changed",
  "name": "Farben geändert",
  "fields": [],
  "result": null
}
```

