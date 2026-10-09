## Automationen: Windows-Ereignis

ID: windows_event
Website: /doku/automationen/windows_event/

Abbildung: [Detailansicht: Windows-Ereignis. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-auto-windows-event.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Kategorie: Gruppiert die verfügbaren Windows-Ereignisse.
3. Windows-Funktion: Wählt das konkrete Ereignis aus der Kategorie.
4. Entprellung (Sekunden): Fasst schnell wiederholte Ereignisse innerhalb der eingestellten Zeit zusammen.
5. Verzögerung (Sekunden): Wartezeit zwischen dem beobachteten Ereignis und dem Start.
6. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.debounce

Mindestabstand zur Entprellung mehrfacher Trigger-Ereignisse als TimeSpan-Text, zum Beispiel 00:00:01. Dies ist nicht die allgemeine Ziel-Cooldown-Regel.

### field.delay_after_event

Verzögerung zwischen Ereignisbeobachtung und Startentscheidung als TimeSpan-Text, zum Beispiel 00:00:02.

### field.event_type

Stabile Kennung eines unterstützten Windows-Ereignisses, zum Beispiel network.availability.changed. Reine Abfrage-/Einstellungsfunktionen sind keine Ereignistrigger.

### field.filters

Benannte Filterwerte der Windows-Ereignisfunktion. Erforderliche Parameter des gewählten Ereignisses müssen vorhanden sein.

### field.kind

Abgeleitete Triggerart zur Anzeige. Für die polymorphe Deserialisierung ist $type maßgeblich; die kanonische Vorlage zeigt die zusätzlich geschriebenen Kind/kind-Werte.

### field.kind.option.FileSystemEvent

Start bei einem passenden Datei-/Ordnerereignis.

### field.kind.option.Hotkey

Start über Tastenkombination.

### field.kind.option.Interval

Start in einem positiven Wiederholungsintervall.

### field.kind.option.OnceAt

Einmaliger Start zu einem zukünftigen Zeitpunkt.

### field.kind.option.ProcessExited

Start, wenn ein passender Prozess beendet wird.

### field.kind.option.ProcessStarted

Start, wenn ein passender Prozess neu erscheint.

### field.kind.option.Schedule

Start zu einer Uhrzeit an ausgewählten Wochentagen.

### field.kind.option.SystemEvent

Start bei einem unterstützten Sitzungs-/Energie-/Systemereignis.

### field.kind.option.Webhook

Start durch einen authentifizierten HTTP-POST-Aufruf.

### field.kind.option.WindowEvent

Start bei einem passenden Fensterereignis.

### field.kind.option.WindowsEvent

Start über eine unterstützte Funktion des Windows-Ereigniskatalogs.

### purpose

Wähle eine Funktion mit SupportsEvents aus dem Windows-Katalog und ergänze deren erforderliche Filter. Die Funktion bestimmt die tatsächliche Ereignisquelle. Debounce und Startverzögerung sind getrennt von Cooldown und Verhalten bei laufendem Ziel.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "windows_event",
  "name": "Windows-Ereignis",
  "fields": [
    {
      "id": "debounce",
      "name": "debounce",
      "type": "TimeSpan",
      "options": [],
      "defaultValue": "00:00:01"
    },
    {
      "id": "delay_after_event",
      "name": "delay_after_event",
      "type": "TimeSpan",
      "options": [],
      "defaultValue": "00:00:00"
    },
    {
      "id": "event_type",
      "name": "event_type",
      "type": "String",
      "options": [],
      "defaultValue": "network.availability.changed"
    },
    {
      "id": "filters",
      "name": "filters",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "kind",
      "name": "kind",
      "type": "AutomationTriggerKind",
      "options": [
        "Hotkey",
        "OnceAt",
        "Schedule",
        "Interval",
        "ProcessStarted",
        "ProcessExited",
        "WindowEvent",
        "FileSystemEvent",
        "SystemEvent",
        "WindowsEvent",
        "Webhook"
      ],
      "defaultValue": "WindowsEvent"
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "$type": "windows_event",
  "Kind": "WindowsEvent",
  "event_type": "network.availability.changed",
  "filters": {},
  "debounce": "00:00:01",
  "delay_after_event": "00:00:00",
  "kind": "WindowsEvent"
}
```

