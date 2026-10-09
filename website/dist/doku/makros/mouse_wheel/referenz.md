## Makros: Mausrad bewegen

ID: mouse_wheel
Website: /doku/makros/mouse_wheel/

Abbildung: [Detailansicht: Mausrad bewegen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-macro-mouse-wheel.detail.png)

1. DeltaX: Horizontaler Versatz: Bei Mausbewegungen in Pixeln, beim Mausrad als Scroll-Delta.
2. DeltaY: Vertikaler Versatz: Bei Mausbewegungen in Pixeln, beim Mausrad als Scroll-Delta.
3. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
### errors

Ungültige Tasten-/Maustastenkennungen, doppelte IDs, fehlerhafte Gruppenreferenzen und negative/ungültige Zeitwerte vor Ausführung korrigieren. Eingaben wirken auf das aktive Fenster. Bei absoluten Bewegungen die Aufnahmeumgebung prüfen; ein Abbruch gibt die vom Executor gehaltenen Tasten/Maustasten frei, rekonstruiert aber keine bereits eingegebenen Texte.

### example

Füge diesen Befehl im Makro-Editor in einen Testablauf ein. Down-Befehle durch passende Up-Befehle abschließen; die JSON-Vorlage darunter zeigt type, id und die konkreten Wertefelder dieses Befehls.

### field.delayBeforeUs

Wartezeit vor diesem Makrobefehl in Mikrosekunden; 1000 µs sind 1 ms. null kennzeichnet ältere oder manuell angelegte Befehle ohne aufgezeichnete Vorverzögerung.

### field.deltaX

Relative horizontale Mausbewegung beziehungsweise horizontales Mausrad-Delta. Die Bedeutung hängt vom Befehlstyp ab.

### field.deltaY

Relative vertikale Mausbewegung beziehungsweise vertikales Mausrad-Delta. Positive/negative Werte bestimmen die Richtung.

### field.groupId

Optionale Referenz auf groups[].id. Gruppen ordnen zusammengehörige aufgezeichnete Befehle, ändern aber nicht ihre Reihenfolge in commands. Jede Gruppe muss Befehle enthalten; Befehle derselben Gruppe müssen einen zusammenhängenden Block bilden.

### field.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### purpose

Sendet horizontale/vertikale Mausrad-Deltas. Das fokussierte beziehungsweise unter dem Zeiger befindliche Programm bestimmt deren Wirkung.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "mouse_wheel",
  "name": "Mausrad bewegen",
  "fields": [
    {
      "id": "delayBeforeUs",
      "name": "delayBeforeUs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "deltaX",
      "name": "deltaX",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "deltaY",
      "name": "deltaY",
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
  "type": "mouse_wheel",
  "deltaX": 0,
  "deltaY": 0,
  "id": "3015398d-f01d-5b06-9899-4110bf157815"
}
```

