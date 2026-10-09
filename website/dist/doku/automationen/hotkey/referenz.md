## Automationen: Hotkey

ID: hotkey
Website: /doku/automationen/hotkey/

Abbildung: [Strg+Alt+U als Auslöser für den Job Arbeitsunterlagen vorbereiten.](/automation-hotkey.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Hotkey: Die angezeigte Tastenkombination löst die Automation aus.
3. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
4. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
5. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Unterlagenbeispiel: Strg+Alt+U startet den ausgewählten Job.

### field.debounce

Mindestabstand zur Entprellung mehrfacher Trigger-Ereignisse als TimeSpan-Text, zum Beispiel 00:00:01. Dies ist nicht die allgemeine Ziel-Cooldown-Regel.

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

### field.modifiers

Bitmaske der Modifikatortasten: None=0, Alt=1, Control=2, Shift=4, Windows=8; kombinierte Werte addieren, etwa 3 für Strg+Alt.

### field.modifiers.option.Alt

Alt-Modifikator; numerische Bitmaske 1.

### field.modifiers.option.Control

Strg-Modifikator; numerische Bitmaske 2.

### field.modifiers.option.None

Keine Modifikatortaste; numerische Bitmaske 0.

### field.modifiers.option.Shift

Umschalt-Modifikator; numerische Bitmaske 4.

### field.modifiers.option.Windows

Windows-Taste als Modifikator (Bitmaske 8) beziehungsweise Windows-Fachbereich, wenn diese Option zu einem Log-Bereich gehört.

### field.virtual_key_code

Windows-Virtual-Key-Code der Trigger-Taste als Zahl, zum Beispiel 85 für U. Mit modifiers kombinieren und auf bestehende Tastenkombinationen prüfen.

### purpose

Startet das gewählte Ziel über eine Tastenkombination. Wähle Taste und Modifikatoren, prüfe Konflikte und aktiviere erst nach einem manuellen Test. debounce entprellt den Trigger, delay_after_event verschiebt seine Startentscheidung.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "hotkey",
  "name": "Hotkey",
  "fields": [
    {
      "id": "debounce",
      "name": "debounce",
      "type": "TimeSpan",
      "options": [],
      "defaultValue": "00:00:00"
    },
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
      "defaultValue": "Hotkey"
    },
    {
      "id": "modifiers",
      "name": "modifiers",
      "type": "KeyModifiers",
      "options": [
        "None",
        "Alt",
        "Control",
        "Shift",
        "Windows"
      ],
      "defaultValue": "None"
    },
    {
      "id": "virtual_key_code",
      "name": "virtual_key_code",
      "type": "UInt32",
      "options": [],
      "defaultValue": 0
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "$type": "hotkey",
  "Kind": "Hotkey",
  "modifiers": 0,
  "virtual_key_code": 0,
  "debounce": "00:00:00",
  "delay_after_event": "00:00:00",
  "kind": "Hotkey"
}
```

