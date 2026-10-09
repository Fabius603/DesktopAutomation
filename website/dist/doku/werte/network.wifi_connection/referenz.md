## Werte und Ergebnisse: network.wifi_connection

ID: network.wifi_connection
Website: /doku/werte/network.wifi_connection/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle network.wifi_connection im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.action

connect verbindet das bestehende Profil/die Verbindung, disconnect trennt sie.

### field.action.option.connect

Mit dem angegebenen vorhandenen Profil verbinden.

### field.action.option.disconnect

Die angegebene Verbindung trennen.

### field.profile

Name eines bereits vorhandenen WLAN-Profils. Die Funktion legt nicht automatisch ein neues Profil mit Zugangsdaten an.

### purpose

Verbindet ein bestehendes WLAN-Profil oder trennt die Verbindung entsprechend action. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "network.wifi_connection",
  "name": "network.wifi_connection",
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
      "id": "profile",
      "name": "WLAN-Profil",
      "type": "Text",
      "Required": false,
      "defaultValue": null,
      "options": []
    }
  ],
  "result": null
}
```

