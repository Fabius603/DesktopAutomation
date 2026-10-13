## Logs: LogQuery

ID: LogQuery
Website: /doku/logs/LogQuery/

### errors

State, Issues und IsComplete begrenzen die Aussagekraft. Ein leerer beziehungsweise nicht verfuegbarer Bestand ist kein Erfolgsbeleg. Fehlende ExecutionId oder Endzeitpunkte nicht erfinden.

### example

Filtere einen konkreten RunId und lese LogPage mit unveraenderter SnapshotSequence bis NextBeforeSequence=null. Ordne danach StepExecutionId, StepSummaries und Laufstatus gemeinsam zu. Der Log-Leitfaden zeigt ein synthetisches Ereignis im echten Serializerformat.

### field.AfterSequence

Untere exklusive Sequenzgrenze: nur spätere Ereignisse lesen. Zusammen mit SnapshotSequence eine stabile Abfrage begrenzen.

### field.Area

Textuelle Kontext-/Bereichsangabe. Für fachliche Filter zusätzlich die typisierte Category verwenden.

### field.BeforeSequence

Obere exklusive Sequenzgrenze für rückwärtige Seiten. Den NextBeforeSequence-Wert der vorherigen Seite übernehmen, statt Ereignisnummern selbst zu schaetzen.

### field.Category

Typisierter Fachbereich, zum Beispiel FileAccess, Capture oder Script. Daraus wird keine Ausführung anhand des Nachrichtentexts abgeleitet.

### field.Category.option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### field.Category.option.Capture

Bildschirm-/Kameraaufnahme.

### field.Category.option.Detection

Bild-/Text-/Punkterkennung und Auswertung.

### field.Category.option.Execution

Ausführung und Ablaufsteuerung.

### field.Category.option.FileAccess

Datei-/Ordnerzugriffe und ihre Diagnose.

### field.Category.option.General

Allgemeiner Log-Fachbereich ohne spezielleren Ausführungsbereich.

### field.Category.option.Process

Programmstart, Suche, Fokus und Beenden.

### field.Category.option.Script

Skriptausführung und zugehörige Hintergrundarbeit.

### field.Category.option.Video

Videoaufnahme und Finalisierung.

### field.Category.option.Windows

Windows-Taste als Modifikator (Bitmaske 8) beziehungsweise Windows-Fachbereich, wenn diese Option zu einem Log-Bereich gehört.

### field.From

Optionaler Anfang des Ereignis-Zeitbereichs als DateTimeOffset. Zeitpunkte mit Offset vergleichen.

### field.MinimumLevel

Mindestschweregrad für die Abfrage: Debug, Information, Warning oder Error. Höhere Grenzen koennen wichtige Diagnoseereignisse ausblenden.

### field.MinimumLevel.option.Debug

Technische Detailbeobachtung.

### field.MinimumLevel.option.Error

Fehlerbeobachtung; den tatsächlichen Laufabschluss zusätzlich über Outcome prüfen.

### field.MinimumLevel.option.Information

Normale Lebenszyklus-/Aktionsinformation.

### field.MinimumLevel.option.Warning

Problemhinweis, der nicht zwingend den Lauf beendet.

### field.OnlyProblems

Beschraenkt die Abfrage auf Problemereignisse. Fuer vollständige Ablaufanalyse auch normale Start-, Status- und Abschlussereignisse lesen.

### field.PageSize

Gewuenschte Anzahl Ereignisse je Seite. Fuer Folgeseiten dieselben Filter und SnapshotSequence verwenden.

### field.RunId

ID genau dieses Laufs. Verknüpft Starts, Steps, Probleme und Abschluss desselben Laufs.

### field.Search

Optionaler Suchtext für die vorhandene Logsuche. Suche ersetzt keine stabile Auswertung von Code, Context und Status.

### field.SnapshotSequence

Obere Snapshot-Sequenz der Abfrage. Bei Folgeseiten denselben Wert verwenden, damit neu angehängte Ereignisse den abgefragten Bestand nicht verschieben.

### field.Source

Ursprungsart Job, Automation, Application oder Makro.

### field.Source.option.Application

Ereignis stammt aus der Anwendung selbst.

### field.Source.option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### field.Source.option.Job

Quelle beziehungsweise Ziel ist ein Job.

### field.Source.option.Makro

Quelle beziehungsweise Ziel ist ein Makro.

### field.SourceId

Stabile ID des ursprünglichen Jobs/Makros/der Automation; sie ist nicht die Identität eines einzelnen Laufs.

### field.TriggerId

Identität der konkreten Ereignisbeobachtung, nicht nur der gespeicherten Triggerkonfiguration.

### field.Until

Optionales Ende des Ereignis-Zeitbereichs als DateTimeOffset. Kein Lauf-Endzeitpunkt und kein Beleg eines erfolgreichen Abschlusses.

### purpose

Filter- und Seitengrenzen einer strukturierten Logabfrage. Source, SourceId, RunId, TriggerId, Category, Zeitraum und MinimumLevel grenzen den Bestand ein. Sequenz-/Snapshotgrenzen halten die Seitenauswertung stabil.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogQuery",
  "name": "LogQuery",
  "options": [],
  "fields": [
    {
      "id": "AfterSequence",
      "name": "AfterSequence",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Area",
      "name": "Area",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BeforeSequence",
      "name": "BeforeSequence",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Category",
      "name": "Category",
      "type": "LogArea",
      "options": [
        "General",
        "Execution",
        "Automation",
        "FileAccess",
        "Capture",
        "Detection",
        "Process",
        "Script",
        "Video",
        "Windows"
      ],
      "defaultValue": null
    },
    {
      "id": "From",
      "name": "From",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "MinimumLevel",
      "name": "MinimumLevel",
      "type": "ExecutionLogLevel",
      "options": [
        "Debug",
        "Information",
        "Warning",
        "Error"
      ],
      "defaultValue": null
    },
    {
      "id": "OnlyProblems",
      "name": "OnlyProblems",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "PageSize",
      "name": "PageSize",
      "type": "Int32",
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
      "id": "Search",
      "name": "Search",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "SnapshotSequence",
      "name": "SnapshotSequence",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Source",
      "name": "Source",
      "type": "LogSource",
      "options": [
        "Job",
        "Automation",
        "Application",
        "Makro"
      ],
      "defaultValue": null
    },
    {
      "id": "SourceId",
      "name": "SourceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "TriggerId",
      "name": "TriggerId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Until",
      "name": "Until",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

