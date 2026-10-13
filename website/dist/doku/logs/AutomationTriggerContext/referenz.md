## Logs: AutomationTriggerContext

ID: AutomationTriggerContext
Website: /doku/logs/AutomationTriggerContext/

### errors

State, Issues und IsComplete begrenzen die Aussagekraft. Ein leerer beziehungsweise nicht verfuegbarer Bestand ist kein Erfolgsbeleg. Fehlende ExecutionId oder Endzeitpunkte nicht erfinden.

### example

Filtere einen konkreten RunId und lese LogPage mit unveraenderter SnapshotSequence bis NextBeforeSequence=null. Ordne danach StepExecutionId, StepSummaries und Laufstatus gemeinsam zu. Der Log-Leitfaden zeigt ein synthetisches Ereignis im echten Serializerformat.

### field.AutomationId

ID der auslösenden Automation, falls der Lauf aus einer Automation stammt.

### field.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.ObservedAt

Zeitpunkt der Triggerbeobachtung mit Offset. Er ist noch kein Zeitpunkt eines erfolgreichen Zielstarts oder Zielabschlusses.

### field.Parameters

Explizit erlaubte, bereinigte Diagnosewerte. Keine vollständige Kopie der Step-settings oder frei verfügbarer Eingabeinhalte.

### field.TriggerId

Identität der konkreten Ereignisbeobachtung, nicht nur der gespeicherten Triggerkonfiguration.

### purpose

Identitaet und Zeitpunkt einer konkreten Triggerbeobachtung. AutomationId referenziert die Definition, TriggerId diese Beobachtung; Parameters enthaelt deren strukturierte Angaben.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "AutomationTriggerContext",
  "name": "AutomationTriggerContext",
  "options": [],
  "fields": [
    {
      "id": "AutomationId",
      "name": "AutomationId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "ObservedAt",
      "name": "ObservedAt",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Parameters",
      "name": "Parameters",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "TriggerId",
      "name": "TriggerId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

