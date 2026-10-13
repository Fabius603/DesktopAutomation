## Logs: ExecutionLogKind

ID: ExecutionLogKind
Website: /doku/logs/ExecutionLogKind/

### errors

State, Issues und IsComplete begrenzen die Aussagekraft. Ein leerer beziehungsweise nicht verfuegbarer Bestand ist kein Erfolgsbeleg. Fehlende ExecutionId oder Endzeitpunkte nicht erfinden.

### example

Filtere einen konkreten RunId und lese LogPage mit unveraenderter SnapshotSequence bis NextBeforeSequence=null. Ordne danach StepExecutionId, StepSummaries und Laufstatus gemeinsam zu. Der Log-Leitfaden zeigt ein synthetisches Ereignis im echten Serializerformat.

### option.Job

Quelle beziehungsweise Ziel ist ein Job.

### option.Makro

Quelle beziehungsweise Ziel ist ein Makro.

### purpose

Art eines ausfuehrbaren Laufziels: Job oder Makro. Anders als LogSource umfasst dieser Vertrag weder Application noch Automation.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "ExecutionLogKind",
  "name": "ExecutionLogKind",
  "options": [
    "Job",
    "Makro"
  ],
  "fields": []
}
```

