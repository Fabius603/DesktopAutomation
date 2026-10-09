## Makros: Maus relativ bewegen

ID: mouse_move_relative
Website: /doku/makros/mouse_move_relative/

Abbildung: [Detailansicht: Maus relativ bewegen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-macro-mouse-move-relative.detail.png)

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

Bewegt den Mauszeiger um deltaX/deltaY relativ zur aktuellen Position. Die Endposition hängt deshalb vom Ausgangspunkt und von der Eingabeverarbeitung des Zielprogramms ab.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "mouse_move_relative",
  "name": "Maus relativ bewegen",
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
  "type": "mouse_move_relative",
  "deltaX": 0,
  "deltaY": 0,
  "id": "318c6a13-2d74-5343-dfaf-cf44e5cc1875"
}
```

