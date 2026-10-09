## Automationen: AutomationRunPolicy

ID: AutomationRunPolicy
Website: /doku/automationen/AutomationRunPolicy/

Abbildung: [Zeitplan und Ausführungsregeln einer Automation.](/automationen.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Uhrzeit und Tage: Legt die Uhrzeit und Wochentage der wiederkehrenden Ausführung fest.
3. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
4. Wenn bereits aktiv: Bestimmt die Startentscheidung, falls dieses Ziel bereits läuft.
5. Cooldown: Mindestabstand zwischen angenommenen Starts; die Einheit ist Sekunden.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.already_running_behavior

Regel bei bereits laufendem Ziel: parallel starten, stoppen, ignorieren oder neu starten. Ihre Entscheidung wird im Automationsverlauf sichtbar.

### field.already_running_behavior.option.Ignore

Das neue Ereignis ignorieren, wenn das Ziel bereits läuft.

### field.already_running_behavior.option.Restart

Laufendes Ziel stoppen und erneut starten.

### field.already_running_behavior.option.StartParallel

Trotz bereits laufendem Ziel eine weitere Ausführung anfordern; parallele Wirkungen bewusst berücksichtigen.

### field.already_running_behavior.option.Stop

Bereits laufendes Ziel stoppen, ohne den neuen Lauf wie bei Restart erneut anzufordern.

### field.cooldown

Mindestpause zwischen akzeptierten Starts als TimeSpan. Ein während der Sperrfrist beobachtetes Ereignis ist kein Beweis für einen gestarteten Job.

### field.enabled_from

Beginn des aktiven täglichen Uhrzeitfensters. Gemeinsam mit enabled_until setzen oder beide leer lassen.

### field.enabled_until

Ende des aktiven täglichen Uhrzeitfensters. Der Partner enabled_from muss ebenfalls gesetzt sein.

### purpose

Entscheidet, ob ein beobachtetes Ereignis ein Ziel starten darf: aktives Uhrzeitfenster, Cooldown und Verhalten bei laufendem Ziel werden gemeinsam berücksichtigt. Die Entscheidung und beteiligte Instanzen sind im Automationsverlauf nachvollziehbar.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "AutomationRunPolicy",
  "name": "AutomationRunPolicy",
  "fields": [
    {
      "id": "already_running_behavior",
      "name": "already_running_behavior",
      "type": "AutomationAlreadyRunningBehavior",
      "options": [
        "StartParallel",
        "Stop",
        "Ignore",
        "Restart"
      ],
      "defaultValue": "Ignore"
    },
    {
      "id": "cooldown",
      "name": "cooldown",
      "type": "TimeSpan",
      "options": [],
      "defaultValue": "00:00:00"
    },
    {
      "id": "enabled_from",
      "name": "enabled_from",
      "type": "TimeOnly",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "enabled_until",
      "name": "enabled_until",
      "type": "TimeOnly",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "already_running_behavior": "Ignore",
  "cooldown": "00:00:00",
  "enabled_from": null,
  "enabled_until": null
}
```

