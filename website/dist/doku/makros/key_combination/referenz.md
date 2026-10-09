## Makros: Tastenkombination

ID: key_combination
Website: /doku/makros/key_combination/

Abbildung: [Gruppierte Maus- und Tastaturbefehle mit ausgewählter Tastenkombination.](/makro-editor.detail.png)

1. Key: Taste bzw. Tastenkombination für diesen Befehl.
2. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
### errors

Ungültige Tasten-/Maustastenkennungen, doppelte IDs, fehlerhafte Gruppenreferenzen und negative/ungültige Zeitwerte vor Ausführung korrigieren. Eingaben wirken auf das aktive Fenster. Bei absoluten Bewegungen die Aufnahmeumgebung prüfen; ein Abbruch gibt die vom Executor gehaltenen Tasten/Maustasten frei, rekonstruiert aber keine bereits eingegebenen Texte.

### example

Füge diesen Befehl im Makro-Editor in einen Testablauf ein. Down-Befehle durch passende Up-Befehle abschließen; die JSON-Vorlage darunter zeigt type, id und die konkreten Wertefelder dieses Befehls.

### field.delayBeforeUs

Wartezeit vor diesem Makrobefehl in Mikrosekunden; 1000 µs sind 1 ms. null kennzeichnet ältere oder manuell angelegte Befehle ohne aufgezeichnete Vorverzögerung.

### field.durationUs

Haltedauer beziehungsweise zusätzliche Dauer dieses Makrobefehls in Mikrosekunden. Sie ist nicht die Millisekunden-Dauer des timeout-Befehls.

### field.groupId

Optionale Referenz auf groups[].id. Gruppen ordnen zusammengehörige aufgezeichnete Befehle, ändern aber nicht ihre Reihenfolge in commands. Jede Gruppe muss Befehle enthalten; Befehle derselben Gruppe müssen einen zusammenhängenden Block bilden.

### field.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### field.keys

Geordnete Tastenkennungen einer Tastenkombination. Sie werden gedrückt und in umgekehrter Reihenfolge freigegeben; doppelte oder ungültige Kennungen vermeiden.

### purpose

Drückt eine geordnete Tastenkombination, hält sie für durationUs und gibt die selbst gedrückten Tasten in umgekehrter Reihenfolge frei. Verwende keys für Shortcuts wie Strg+A statt den Text „Strg+A“ als Texteingabe zu schreiben.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "key_combination",
  "name": "Tastenkombination",
  "fields": [
    {
      "id": "delayBeforeUs",
      "name": "delayBeforeUs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "durationUs",
      "name": "durationUs",
      "type": "Int64",
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
    },
    {
      "id": "keys",
      "name": "keys",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "key_combination",
  "keys": [],
  "durationUs": 0,
  "id": "13effec4-5e09-0872-eac1-b63a4eb7e8e5"
}
```

