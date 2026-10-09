## Werte und Ergebnisse: Windows-Sicherheit geändert

ID: security.state.changed
Website: /doku/werte/security.state.changed/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle security.state.changed als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### purpose

Ereignis: Windows-Sicherheit geändert. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Administratorrechte sind erforderlich.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "security.state.changed",
  "name": "Windows-Sicherheit geändert",
  "fields": [],
  "result": null
}
```

