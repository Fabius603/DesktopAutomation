## Werte und Ergebnisse: Prozess beendet

ID: process.exited
Website: /doku/werte/process.exited/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle process.exited als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.name

Prozessname (etwa notepad) beziehungsweise Laufwerksname (etwa C:), entsprechend der Windows-Funktion.

### purpose

Ereignis: Prozess beendet. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "process.exited",
  "name": "Prozess beendet",
  "fields": [
    {
      "id": "name",
      "name": "Prozessname",
      "type": "ProcessName",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

