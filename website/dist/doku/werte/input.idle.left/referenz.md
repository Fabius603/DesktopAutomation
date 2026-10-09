## Werte und Ergebnisse: Leerlauf beendet

ID: input.idle.left
Website: /doku/werte/input.idle.left/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle input.idle.left als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.threshold_ms

Leerlaufgrenze in Millisekunden; 60000 entspricht einer Minute ohne Eingabe.

### purpose

Ereignis: Leerlauf beendet. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "input.idle.left",
  "name": "Leerlauf beendet",
  "fields": [
    {
      "id": "threshold_ms",
      "name": "Leerlaufgrenze (ms)",
      "type": "Duration",
      "Required": true,
      "defaultValue": "60000",
      "options": []
    }
  ],
  "result": null
}
```

