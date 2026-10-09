## Automationen: Fensterereignis

ID: window_event
Website: /doku/automationen/window_event/

Abbildung: [Detailansicht: Fensterereignis. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-auto-window-event.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Ereignis: Wählt die zu beobachtende Fensteränderung.
3. Prozessname (optional): Beschränkt die Beobachtung auf den angegebenen Prozess.
4. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
5. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
6. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.delay_after_event

Verzögerung zwischen Ereignisbeobachtung und Startentscheidung als TimeSpan-Text, zum Beispiel 00:00:02.

### field.event_kind

Beobachtetes Fenster-, Datei- oder Systemereignis. Wähle nur die Optionen dieses Trigger-Typs.

### field.event_kind.option.Closed

Passendes Fenster wurde geschlossen.

### field.event_kind.option.Focused

Passendes Fenster wurde in den Vordergrund gewechselt.

### field.event_kind.option.Opened

Passendes Fenster wurde geöffnet.

### field.event_kind.option.TitleChanged

Titel des passenden Fensters wurde geändert.

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

### field.process_name

Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein.

### field.window_title_contains

Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht.

### purpose

Beobachtet Öffnen, Schließen, Fokuswechsel oder Titeländerungen passender Fenster. Ein Titelteil und Prozessname grenzen die Fenster ein; mehrere Ereignisse können kurz hintereinander auftreten.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "window_event",
  "name": "Fensterereignis",
  "fields": [
    {
      "id": "delay_after_event",
      "name": "delay_after_event",
      "type": "TimeSpan",
      "options": [],
      "defaultValue": "00:00:00"
    },
    {
      "id": "event_kind",
      "name": "event_kind",
      "type": "WindowAutomationEventKind",
      "options": [
        "Opened",
        "Closed",
        "Focused",
        "TitleChanged"
      ],
      "defaultValue": "Opened"
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
      "defaultValue": "WindowEvent"
    },
    {
      "id": "process_name",
      "name": "process_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "window_title_contains",
      "name": "window_title_contains",
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
  "$type": "window_event",
  "Kind": "WindowEvent",
  "event_kind": "Opened",
  "process_name": "",
  "window_title_contains": "",
  "delay_after_event": "00:00:00",
  "kind": "WindowEvent"
}
```

