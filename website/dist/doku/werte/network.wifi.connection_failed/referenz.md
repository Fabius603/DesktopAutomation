## Werte und Ergebnisse: WLAN-Verbindung fehlgeschlagen

ID: network.wifi.connection_failed
Website: /doku/werte/network.wifi.connection_failed/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle network.wifi.connection_failed als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.ssid

Optionaler WLAN-Netzname zum Eingrenzen der dafür angebotenen WLAN-Ereignisse.

### purpose

Ereignis: WLAN-Verbindung fehlgeschlagen. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "network.wifi.connection_failed",
  "name": "WLAN-Verbindung fehlgeschlagen",
  "fields": [
    {
      "id": "ssid",
      "name": "WLAN-Name (SSID)",
      "type": "Text",
      "Required": false,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

