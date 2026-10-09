## Automationen: AutomationAction

ID: AutomationAction
Website: /doku/automationen/AutomationAction/

### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.action_type

Job oder Makro bestimmt die Art des Ziels. Fülle entsprechend job_id oder makro_id aus.

### field.action_type.option.Job

Quelle beziehungsweise Ziel ist ein Job.

### field.action_type.option.Makro

Quelle beziehungsweise Ziel ist ein Makro.

### field.job_id

GUID des auszuführenden Jobs. Ein Anzeigename alleine reicht für die Zuordnung nicht aus.

### field.makro_id

GUID des auszuführenden Makros. Beim Duplizieren eines Makros die Referenzen auf dessen neue ID anpassen.

### field.name

Anzeigename für Auswahl und Verlauf. Beziehungen verwenden die stabile ID, nicht diesen frei änderbaren Namen.

### purpose

Ordnet den Trigger einem bestehenden Job oder Makro zu. action_type bestimmt, welche GUID verwendet wird. Ein gespeicherter Anzeigename dient der Anzeige und ersetzt die Ziel-ID nicht.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "AutomationAction",
  "name": "AutomationAction",
  "fields": [
    {
      "id": "action_type",
      "name": "action_type",
      "type": "AutomationActionTarget",
      "options": [
        "Job",
        "Makro"
      ],
      "defaultValue": "Job"
    },
    {
      "id": "job_id",
      "name": "job_id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "makro_id",
      "name": "makro_id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "name",
      "name": "name",
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
  "name": "",
  "job_id": null,
  "makro_id": null,
  "action_type": "Job"
}
```

