## Makros: Wartezeit

ID: timeout
Website: /doku/makros/timeout/

Abbildung: [Eine Wartezeit von 500 ms innerhalb der Eingabegruppe, zusätzlich zur Verzögerung vor dem Befehl.](/makro-wartezeit.detail.png)

1. Dauer: Dauer der zusätzlichen Pause. Sie ist unabhängig von der Verzögerung davor.
2. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
### errors

Ungültige Tasten-/Maustastenkennungen, doppelte IDs, fehlerhafte Gruppenreferenzen und negative/ungültige Zeitwerte vor Ausführung korrigieren. Eingaben wirken auf das aktive Fenster. Bei absoluten Bewegungen die Aufnahmeumgebung prüfen; ein Abbruch gibt die vom Executor gehaltenen Tasten/Maustasten frei, rekonstruiert aber keine bereits eingegebenen Texte.

### example

Das Makrobeispiel kombiniert text_input mit „Notiz aus dem Beispielmakro“ und anschließend timeout mit duration=200. Vor Ausführung einen leeren Editor fokussieren.

### field.delayBeforeUs

Wartezeit vor diesem Makrobefehl in Mikrosekunden; 1000 µs sind 1 ms. null kennzeichnet ältere oder manuell angelegte Befehle ohne aufgezeichnete Vorverzögerung.

### field.duration

Dauer des Makro-timeout in Millisekunden. 200 bedeutet 0,2 Sekunden; nicht mit durationUs verwechseln.

### field.groupId

Optionale Referenz auf groups[].id. Gruppen ordnen zusammengehörige aufgezeichnete Befehle, ändern aber nicht ihre Reihenfolge in commands. Jede Gruppe muss Befehle enthalten; Befehle derselben Gruppe müssen einen zusammenhängenden Block bilden.

### field.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### purpose

Wartet die in duration gespeicherten Millisekunden auf der Makro-Zeitachse. durationUs und delayBeforeUs anderer Befehle verwenden dagegen Mikrosekunden.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "timeout",
  "name": "Wartezeit",
  "fields": [
    {
      "id": "delayBeforeUs",
      "name": "delayBeforeUs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "duration",
      "name": "duration",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "groupId",
      "name": "groupId",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "id",
      "name": "id",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "timeout",
  "duration": 0,
  "id": "f8468467-aabe-4960-2611-1a0bcd68497c"
}
```

