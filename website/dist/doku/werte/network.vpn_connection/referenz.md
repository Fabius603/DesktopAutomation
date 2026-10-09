## Werte und Ergebnisse: network.vpn_connection

ID: network.vpn_connection
Website: /doku/werte/network.vpn_connection/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle network.vpn_connection im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.action

connect verbindet das bestehende Profil/die Verbindung, disconnect trennt sie.

### field.action.option.connect

Mit dem angegebenen vorhandenen Profil verbinden.

### field.action.option.disconnect

Die angegebene Verbindung trennen.

### field.connection_name

Name einer bereits eingerichteten VPN-Verbindung.

### purpose

Verbindet oder trennt eine vorhandene VPN-Verbindung über connection_name. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "network.vpn_connection",
  "name": "network.vpn_connection",
  "fields": [
    {
      "id": "action",
      "name": "Aktion",
      "type": "Enum",
      "Required": true,
      "defaultValue": "connect",
      "options": [
        "connect",
        "disconnect"
      ]
    },
    {
      "id": "connection_name",
      "name": "VPN-Verbindung",
      "type": "Text",
      "Required": true,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

