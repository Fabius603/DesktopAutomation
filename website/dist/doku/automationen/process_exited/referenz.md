## Automationen: Prozess beendet

ID: process_exited
Website: /doku/automationen/process_exited/

Abbildung: [Detailansicht: Prozess beendet. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-auto-process-exited.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Prozessname: Name des Prozesses, dessen Beenden beobachtet wird.
3. Fenstertitel enthält (optional): Grenzt den beobachteten Prozess auf einen passenden Fenstertitel ein.
4. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
5. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
6. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.delay_after_event

Verzögerung zwischen Ereignisbeobachtung und Startentscheidung als TimeSpan-Text, zum Beispiel 00:00:02.

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

Beobachtet das Ende passender Prozesse. Die Automation startet ihre eigene Zielaktion und garantiert nicht, dass ein zuvor beendetes Programm noch für Eingaben verfügbar ist.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "process_exited",
  "name": "Prozess beendet",
  "fields": [
    {
      "id": "delay_after_event",
      "name": "delay_after_event",
      "type": "TimeSpan",
      "options": [],
      "defaultValue": "00:00:00"
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
      "defaultValue": "ProcessExited"
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
  "$type": "process_exited",
  "Kind": "ProcessExited",
  "process_name": "",
  "window_title_contains": "",
  "delay_after_event": "00:00:00",
  "kind": "ProcessExited"
}
```

