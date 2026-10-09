## Werte und Ergebnisse: audio.mute

ID: audio.mute
Website: /doku/werte/audio.mute/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle audio.mute im Step „Windows-Einstellung ändern“, wähle ein vorhandenes Systemziel und prüfe danach previous_value und applied_value.

### field.state

Gewünschte Stummschaltung: true aktiviert sie, false hebt sie auf.

### field.state.option.off

Den angegebenen Schalter ausschalten.

### field.state.option.on

Den angegebenen Schalter einschalten.

### field.state.option.toggle

Den aktuellen Schalterzustand umkehren.

### purpose

Schaltet die Audioausgabe über state stumm oder hebt die Stummschaltung auf. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "audio.mute",
  "name": "audio.mute",
  "fields": [
    {
      "id": "state",
      "name": "Stummschaltung",
      "type": "Enum",
      "Required": true,
      "defaultValue": "on",
      "options": [
        "on",
        "off",
        "toggle"
      ]
    }
  ],
  "result": null
}
```

