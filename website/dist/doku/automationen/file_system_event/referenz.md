## Automationen: Datei- oder Ordnerereignis

ID: file_system_event
Website: /doku/automationen/file_system_event/

Abbildung: [Ordnerüberwachung mit Beispieldateipfad und Filter für Textdateien. Der Beispielpfad muss vor Verwendung angepasst werden.](/automation-ordner.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Ereignis: Wählt die Art der Änderung aus, auf die der Trigger reagiert.
3. Dateifilter: Schränkt die überwachten Namen ein, beispielsweise auf *.txt.
4. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
5. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
6. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Sicherungsbeispiel: Created und *.txt im Rechnungsordner starten den Rückfrage-Job.

### field.directory_path

Vorhandener beobachteter Ordner des Datei-Triggers. Die App benötigt Zugriff auf ihn; ein nicht vorhandener Ordner ist keine gültige Automation.

### field.event_kind

Beobachtetes Fenster-, Datei- oder Systemereignis. Wähle nur die Optionen dieses Trigger-Typs.

### field.event_kind.option.Changed

Beobachteter Eintrag wurde geändert.

### field.event_kind.option.Created

Datei-/Ordnereintrag wurde erstellt.

### field.event_kind.option.Deleted

Beobachteter Eintrag wurde entfernt.

### field.event_kind.option.Renamed

Beobachteter Eintrag wurde umbenannt.

### field.filter

Optionales Dateimuster wie *.txt. Beim Ordner-Trigger begrenzt es die beobachteten Einträge; bei Dateiaktionen gilt es nur für die dafür angebotene Aktion.

### field.include_subdirectories

Beobachtet auch Unterordner unterhalb des angegebenen Pfads. Eine größere Baumstruktur kann mehr Ereignisse erzeugen.

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

### field.wait_until_ready

Wartet bei unterstützten Datei-Ereignissen auf Verwendbarkeit der Datei, bevor der Zielablauf gestartet wird. Es ersetzt keine Zugriffsrechte.

### purpose

Beobachtet einen vorhandenen Ordner mit Dateimuster und optionalen Unterordnern. Created, Changed, Deleted oder Renamed bestimmen die Ereignisart. Dateisystemereignisse können mehrfach auftreten; Readiness und RunPolicy begrenzen Starts, ersetzen aber keine fachliche Deduplizierung.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "file_system_event",
  "name": "Datei- oder Ordnerereignis",
  "fields": [
    {
      "id": "directory_path",
      "name": "directory_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "event_kind",
      "name": "event_kind",
      "type": "FileSystemAutomationEventKind",
      "options": [
        "Created",
        "Changed",
        "Deleted",
        "Renamed"
      ],
      "defaultValue": "Created"
    },
    {
      "id": "filter",
      "name": "filter",
      "type": "String",
      "options": [],
      "defaultValue": "*.*"
    },
    {
      "id": "include_subdirectories",
      "name": "include_subdirectories",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
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
      "defaultValue": "FileSystemEvent"
    },
    {
      "id": "wait_until_ready",
      "name": "wait_until_ready",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "$type": "file_system_event",
  "Kind": "FileSystemEvent",
  "event_kind": "Created",
  "directory_path": "",
  "filter": "*.*",
  "include_subdirectories": false,
  "wait_until_ready": true,
  "kind": "FileSystemEvent"
}
```

