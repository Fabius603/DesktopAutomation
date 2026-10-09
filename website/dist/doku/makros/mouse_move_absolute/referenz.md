## Makros: Maus absolut bewegen

ID: mouse_move_absolute
Website: /doku/makros/mouse_move_absolute/

Abbildung: [Detailansicht: Maus absolut bewegen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-macro-mouse-move-absolute.detail.png)

1. X: Horizontale Zielposition des Mauszeigers in Pixeln.
2. Y: Vertikale Zielposition des Mauszeigers in Pixeln.
3. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
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

### field.x

Horizontale Pixelkoordinate. Bei Bildschirmwerten den Monitorversatz beziehungsweise bei Makros den virtuellen Desktop berücksichtigen.

### field.y

Vertikale Pixelkoordinate. Der Ursprung liegt üblicherweise links oben; Bild- und Desktopkoordinaten dürfen nicht ungeprüft vermischt werden.

### purpose

Bewegt den Mauszeiger zu x/y in Pixeln des virtuellen Desktops. Monitoranordnung, Auflösung und Skalierung müssen zum aufgezeichneten Ziel passen; negative Koordinaten sind bei zusätzlichen Monitoren möglich.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "mouse_move_absolute",
  "name": "Maus absolut bewegen",
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
      "id": "x",
      "name": "x",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "y",
      "name": "y",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "mouse_move_absolute",
  "x": 0,
  "y": 0,
  "id": "743a6baa-76dc-d235-0599-ad83c3203476"
}
```

