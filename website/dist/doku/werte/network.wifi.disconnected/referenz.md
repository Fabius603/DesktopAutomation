## Werte und Ergebnisse: WLAN-Verbindung getrennt

ID: network.wifi.disconnected
Website: /doku/werte/network.wifi.disconnected/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle network.wifi.disconnected als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.ssid

Optionaler WLAN-Netzname zum Eingrenzen der dafür angebotenen WLAN-Ereignisse.

### purpose

Ereignis: WLAN-Verbindung getrennt. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "network.wifi.disconnected",
  "name": "WLAN-Verbindung getrennt",
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

