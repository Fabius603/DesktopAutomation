## Werte und Ergebnisse: WLAN-Verbindung wird aufgebaut

ID: network.wifi.connecting
Website: /doku/werte/network.wifi.connecting/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle network.wifi.connecting als Windows-Ereignis einer deaktivierten Automation und ergänze die angebotenen Filter; teste das Ziel zunächst manuell.

### field.ssid

Optionaler WLAN-Netzname zum Eingrenzen der dafür angebotenen WLAN-Ereignisse.

### purpose

Ereignis: WLAN-Verbindung wird aufgebaut. Die Automation reagiert auf eine beobachtete Änderung; ein Ereignis ersetzt keine aktuelle Zustandsabfrage. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "network.wifi.connecting",
  "name": "WLAN-Verbindung wird aufgebaut",
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

