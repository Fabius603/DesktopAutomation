# Hotkeys-Konfiguration

Diese Datei beschreibt das **JSON-Format** zur Definition globaler Hotkeys. Die `GlobalHotkeyService` lädt diese Konfiguration, registriert die Hotkeys und feuert bei Tastendruck Events mit dem Objekt `ActionDefinition`, das Name und Steuerkommando enthält.

## Aufbau der JSON-Datei

Die Datei ist ein JSON-Array von Objekten. Jedes Objekt besitzt diese Felder:

| Feld             | Typ      | Beschreibung                                                                                                                           |
| ---------------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `Name`           | String   | Eindeutiger Bezeichner des Hotkeys, dient intern als Key.                                                                              |
| `Modifiers`      | Number   | Bitmaske für Modifier-Tasten (Strg=2, Alt=1, Shift=4, Windows=8). Kombination z. B. Strg+Shift = `2+4=6`.                                 |
| `VirtualKeyCode` | Number   | Windows Virtual-Key-Code (z. B. `83` für 'S', `70` für 'F', `82` für 'R').                                                             |
| `Action`         | Object   | Objekt mit:
|                  |          | - `Name`    (String): Name des Jobs oder Makros                                                                                        |
|                  |          | - `Command` (String): Steuerkommando aus `ActionCommand`-Enum (`Start`, `Stop`, `Toggle`), legt fest, ob Job gestartet, gestoppt oder umgeschaltet wird |

## Beispiel

```json
[
  {
    "Name": "SaveHotkey",
    "Modifiers": 2,
    "VirtualKeyCode": 83,
    "Action": { "Name": "SaveDocument", "Command": "Start" }
  },
  {
    "Name": "StopSave",
    "Modifiers": 2,
    "VirtualKeyCode": 83,
    "Action": { "Name": "SaveDocument", "Command": "Stop" }
  },
  {
    "Name": "ToggleRecord",
    "Modifiers": 6,
    "VirtualKeyCode": 82,
    "Action": { "Name": "RecordMacro", "Command": "Toggle" }
  }
]
```

- **SaveHotkey**: Strg+S (`2`) löst `Start` für `SaveDocument` aus.
- **StopSave**: Strg+S (`2`) löst `Stop` für `SaveDocument` aus.
- **ToggleRecord**: Strg+Shift+R (`2+4=6`) löst `Toggle` für `RecordMacro` aus.

## Verwendung

```csharp
GlobalHotkeyService.Initialize(workerCount: 4);
var hotkeyService = GlobalHotkeyService.Instance;
hotkeyService.HotkeyPressed += (s, e) =>
{
    var name = e.Action.Name;
    switch (e.Action.Command)
    {
        case ActionCommand.Start:  jobDispatcher.Start(name);  break;
        case ActionCommand.Stop:   jobDispatcher.Cancel(name); break;
        case ActionCommand.Toggle: jobDispatcher.Toggle(name); break;
    }
};
hotkeyService.LoadFromJson("hotkeys.json");

Console.ReadLine();
```

## Virtual-Key-Codes

https://cherrytree.at/misc/vk.htm
