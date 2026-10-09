## Makros: Text eingeben

ID: text_input
Website: /doku/makros/text_input/

Abbildung: [Der Textbefehl trägt die Beispielnummer RE-2026-042 ein; die Verzögerung davor wird separat eingestellt.](/makro-text.detail.png)

1. Text: Dieser Text wird in die aktive Anwendung eingegeben.
2. Verzögerung davor: Wartezeit seit dem vorherigen Befehl. Den Zahlenwert und die Einheit separat einstellen.
### errors

Ungültige Tasten-/Maustastenkennungen, doppelte IDs, fehlerhafte Gruppenreferenzen und negative/ungültige Zeitwerte vor Ausführung korrigieren. Eingaben wirken auf das aktive Fenster. Bei absoluten Bewegungen die Aufnahmeumgebung prüfen; ein Abbruch gibt die vom Executor gehaltenen Tasten/Maustasten frei, rekonstruiert aber keine bereits eingegebenen Texte.

### example

Das Makrobeispiel kombiniert text_input mit „Notiz aus dem Beispielmakro“ und anschließend timeout mit duration=200. Vor Ausführung einen leeren Editor fokussieren.

### field.delayBeforeUs

Wartezeit vor diesem Makrobefehl in Mikrosekunden; 1000 µs sind 1 ms. null kennzeichnet ältere oder manuell angelegte Befehle ohne aufgezeichnete Vorverzögerung.

### field.durationUs

Haltedauer beziehungsweise zusätzliche Dauer dieses Makrobefehls in Mikrosekunden. Sie ist nicht die Millisekunden-Dauer des timeout-Befehls.

### field.groupId

Optionale Referenz auf groups[].id. Gruppen ordnen zusammengehörige aufgezeichnete Befehle, ändern aber nicht ihre Reihenfolge in commands. Jede Gruppe muss Befehle enthalten; Befehle derselben Gruppe müssen einen zusammenhängenden Block bilden.

### field.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### field.text

Direkt angezeigter beziehungsweise eingegebener Text. Ein Makro schreibt ihn in das aktive Fenster; die Desktop-Anzeige stellt ihn nur dar.

### purpose

Sendet den angegebenen Unicode-Text an das aktive Eingabeziel. Dies ist keine OCR und keine garantierte Zwischenablagenoperation. Fokussiere vorher das gewünschte Textfeld.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "text_input",
  "name": "Text eingeben",
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
      "id": "text",
      "name": "text",
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
  "type": "text_input",
  "text": "",
  "durationUs": 0,
  "id": "8175bccd-480d-2892-ec75-d89f4337f976"
}
```

