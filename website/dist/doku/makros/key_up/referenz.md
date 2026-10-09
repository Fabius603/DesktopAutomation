## Makros: Taste loslassen

ID: key_up
Website: /doku/makros/key_up/

Abbildung: [Detailansicht: Taste loslassen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-macro-key-up.detail.png)

1. Key: Taste bzw. Tastenkombination für diesen Befehl.
2. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
### errors

Ungültige Tasten-/Maustastenkennungen, doppelte IDs, fehlerhafte Gruppenreferenzen und negative/ungültige Zeitwerte vor Ausführung korrigieren. Eingaben wirken auf das aktive Fenster. Bei absoluten Bewegungen die Aufnahmeumgebung prüfen; ein Abbruch gibt die vom Executor gehaltenen Tasten/Maustasten frei, rekonstruiert aber keine bereits eingegebenen Texte.

### example

Füge diesen Befehl im Makro-Editor in einen Testablauf ein. Down-Befehle durch passende Up-Befehle abschließen; die JSON-Vorlage darunter zeigt type, id und die konkreten Wertefelder dieses Befehls.

### field.delayBeforeUs

Wartezeit vor diesem Makrobefehl in Mikrosekunden; 1000 µs sind 1 ms. null kennzeichnet ältere oder manuell angelegte Befehle ohne aufgezeichnete Vorverzögerung.

### field.groupId

Optionale Referenz auf groups[].id. Gruppen ordnen zusammengehörige aufgezeichnete Befehle, ändern aber nicht ihre Reihenfolge in commands. Jede Gruppe muss Befehle enthalten; Befehle derselben Gruppe müssen einen zusammenhängenden Block bilden.

### field.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### field.key

Windows-Tastenkennung des Makrobefehls, zum Beispiel A oder CONTROL. Der Executor muss die Kennung in einen Virtual-Key-Code übersetzen können.

### purpose

Gibt die benannte Taste frei. Verwende dieselbe Kennung wie beim vorherigen key_down.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "key_up",
  "name": "Taste loslassen",
  "fields": [
    {
      "id": "delayBeforeUs",
      "name": "delayBeforeUs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
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
      "id": "key",
      "name": "key",
      "type": "String",
      "options": [],
      "defaultValue": ""
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "key_up",
  "key": "",
  "id": "e471f899-41c4-c89c-4e3b-8d51c98356c1"
}
```

