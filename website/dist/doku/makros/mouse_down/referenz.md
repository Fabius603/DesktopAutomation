## Makros: Maustaste drücken

ID: mouse_down
Website: /doku/makros/mouse_down/

Abbildung: [Detailansicht: Maustaste drücken. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-macro-mouse-down.detail.png)

1. Button: Wählt die Maustaste, die gedrückt oder losgelassen wird.
2. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
### errors

Ungültige Tasten-/Maustastenkennungen, doppelte IDs, fehlerhafte Gruppenreferenzen und negative/ungültige Zeitwerte vor Ausführung korrigieren. Eingaben wirken auf das aktive Fenster. Bei absoluten Bewegungen die Aufnahmeumgebung prüfen; ein Abbruch gibt die vom Executor gehaltenen Tasten/Maustasten frei, rekonstruiert aber keine bereits eingegebenen Texte.

### example

Füge diesen Befehl im Makro-Editor in einen Testablauf ein. Down-Befehle durch passende Up-Befehle abschließen; die JSON-Vorlage darunter zeigt type, id und die konkreten Wertefelder dieses Befehls.

### field.button

Maustaste als akzeptierter Name, zum Beispiel left, right oder middle. Down/Up-Paare müssen dieselbe Taste verwenden.

### field.delayBeforeUs

Wartezeit vor diesem Makrobefehl in Mikrosekunden; 1000 µs sind 1 ms. null kennzeichnet ältere oder manuell angelegte Befehle ohne aufgezeichnete Vorverzögerung.

### field.groupId

Optionale Referenz auf groups[].id. Gruppen ordnen zusammengehörige aufgezeichnete Befehle, ändern aber nicht ihre Reihenfolge in commands. Jede Gruppe muss Befehle enthalten; Befehle derselben Gruppe müssen einen zusammenhängenden Block bilden.

### field.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### purpose

Drückt die benannte Maustaste. Für einen Klick ein passendes mouse_up ergänzen; für Ziehen können dazwischen Bewegungsbefehle stehen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "mouse_down",
  "name": "Maustaste drücken",
  "fields": [
    {
      "id": "button",
      "name": "button",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
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
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "mouse_down",
  "button": "",
  "id": "57b76900-3086-9cc2-d4ab-dcc175ad9b46"
}
```

