## Logs: LogAction

ID: LogAction
Website: /doku/logs/LogAction/

### errors

State, Issues und IsComplete begrenzen die Aussagekraft. Ein leerer beziehungsweise nicht verfuegbarer Bestand ist kein Erfolgsbeleg. Fehlende ExecutionId oder Endzeitpunkte nicht erfinden.

### example

Filtere einen konkreten RunId und lese LogPage mit unveraenderter SnapshotSequence bis NextBeforeSequence=null. Ordne danach StepExecutionId, StepSummaries und Laufstatus gemeinsam zu. Der Log-Leitfaden zeigt ein synthetisches Ereignis im echten Serializerformat.

### field.Kind

Art der angebotenen Diagnoseaktion. RunId, StepId oder Path sind nur im Kontext dieser Art relevant; keine Trigger-Art und kein Step-type.

### field.Path

Optionaler strukturierter Pfad einer Diagnoseaktion. Nur im beschriebenen Aktionskontext verwenden; ein vorhandener Pfad beweist weder Existenz noch erfolgreiche Dateioperation.

### field.RunId

ID genau dieses Laufs. Verknüpft Starts, Steps, Probleme und Abschluss desselben Laufs.

### field.StepId

Stabile ID des Job-Steps; derselbe Step kann in mehreren Durchläufen ausgeführt werden.

### purpose

Eine konkrete Hilfs- beziehungsweise Navigationsaktion der Diagnose. Kind bestimmt die Bedeutung der optionalen RunId, StepId und Path; dieses Objekt ist kein Auftrag zur Jobausfuehrung.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogAction",
  "name": "LogAction",
  "options": [],
  "fields": [
    {
      "id": "Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Path",
      "name": "Path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "RunId",
      "name": "RunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepId",
      "name": "StepId",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

