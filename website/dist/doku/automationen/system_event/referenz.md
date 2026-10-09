## Automationen: Windows-Systemereignis

ID: system_event
Website: /doku/automationen/system_event/

Abbildung: [Detailansicht: Windows-Systemereignis. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-auto-system-event.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Systemereignis: Das ausgewählte Windows-Ereignis startet die Automation.
3. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
4. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
5. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.event_kind

Beobachtetes Fenster-, Datei- oder Systemereignis. Wähle nur die Optionen dieses Trigger-Typs.

### field.event_kind.option.Resume

Fortsetzen nach einem Energiesparzustand.

### field.event_kind.option.SessionLocked

Windows-Sitzung wurde gesperrt.

### field.event_kind.option.SessionUnlocked

Windows-Sitzung wurde entsperrt.

### field.event_kind.option.Suspend

Wechsel in den Energiespar-/Standbyzustand.

### field.event_kind.option.SystemShutdown

Herunterfahren wird beobachtet; verfügbare Ausführungszeit kann begrenzt sein.

### field.event_kind.option.UserLogoff

Abmeldung des Benutzers wird beobachtet.

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

Beobachtet unterstützte Sitzung-/Systemereignisse wie Sperren, Entsperren, Standby und Fortsetzen. Beim Herunterfahren/Abmelden kann die verbleibende Zeit kurz sein; verwende dafür kurze, bereits getestete Zielabläufe.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "system_event",
  "name": "Windows-Systemereignis",
  "fields": [
    {
      "id": "event_kind",
      "name": "event_kind",
      "type": "SystemAutomationEventKind",
      "options": [
        "SessionLocked",
        "SessionUnlocked",
        "UserLogoff",
        "SystemShutdown",
        "Suspend",
        "Resume"
      ],
      "defaultValue": "SessionLocked"
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
      "defaultValue": "SystemEvent"
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "$type": "system_event",
  "Kind": "SystemEvent",
  "event_kind": "SessionLocked",
  "kind": "SystemEvent"
}
```

