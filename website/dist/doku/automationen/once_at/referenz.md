## Automationen: Einmalig

ID: once_at
Website: /doku/automationen/once_at/

Abbildung: [Detailansicht: Einmalig. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-auto-once-at.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Ausführung: Datum und Uhrzeit des einmaligen Starts.
3. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
4. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
5. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

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

### field.run_at

Zeitpunkt der einmaligen Ausführung mit Zeitzonenoffset, etwa 2030-01-01T08:00:00+01:00. Er muss für die Einrichtung in der Zukunft liegen.

### purpose

Startet das Ziel einmal zu run_at. Gib einen zukünftigen Zeitpunkt mit eindeutigem Zeitzonenoffset an. Nach dem Termin ist dies kein täglich wiederkehrender Zeitplan.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "once_at",
  "name": "Einmalig",
  "fields": [
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
      "defaultValue": "OnceAt"
    },
    {
      "id": "run_at",
      "name": "run_at",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "$type": "once_at",
  "Kind": "OnceAt",
  "run_at": "2030-01-01T08:00:00+00:00",
  "kind": "OnceAt"
}
```

