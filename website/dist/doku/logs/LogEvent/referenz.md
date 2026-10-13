## Logs: LogEvent

ID: LogEvent
Website: /doku/logs/LogEvent/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### field.Area

Textuelle Kontext-/Bereichsangabe. Für fachliche Filter zusätzlich die typisierte Category verwenden.

### field.BranchDecision

Strukturierte Entscheidung eines Bedingungsblocks mit Aktivität, Grund und Operatoren ohne rohe Vergleichswerte.

### field.BranchDecision.BranchActive

Ob der betreffende Zweig bei dieser Entscheidung gewählt wurde.

### field.BranchDecision.Conditions

Ausgewertete Bedingungen mit Position, Zustand und Operator; bewusst ohne sensible Rohvergleichswerte.

### field.BranchDecision.Conditions.Operator

Operator der ausgewerteten Bedingung, ohne deren rohen Vergleichsinhalt.

### field.BranchDecision.Conditions.Position

Position des Steps beziehungsweise der Bedingung im gespeicherten Snapshot.

### field.BranchDecision.Conditions.State

Zustand einer Bedingung/Beobachtung. Fehlende Belege nicht als false oder erfolgreich umdeuten.

### field.BranchDecision.MatchMode

Verknüpfung der Bedingungsliste, soweit diese Entscheidung einen Vergleich besitzt.

### field.BranchDecision.ParentActive

Ob der übergeordnete Block bei dieser Entscheidung aktiv war.

### field.BranchDecision.Reason

Stabiler Grund für Skip, Entscheidung oder Abschluss. Im Zusammenhang mit Code und Phase interpretieren.

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

### field.Code

Stabiler Ereignistoken wie step.completed. Für Auswertungen diesen Token verwenden statt übersetzte Nachrichtentexte zu parsen.

### field.Context

Korrelationsobjekt mit Lauf-, Step-, Trigger- und gegebenenfalls Elternlauf-Identitäten.

### field.Context.AutomationId

ID der auslösenden Automation, falls der Lauf aus einer Automation stammt.

### field.Context.InstanceId

Dispatcher-Instanz-ID der Ausführung. Mehrere Ausführungen desselben Jobs besitzen unterschiedliche Instanzen.

### field.Context.ParentRunId

Übergeordneter Lauf eines Unterjobs. Ermöglicht die Navigation von besitzender Arbeit zu Kindläufen.

### field.Context.RunId

ID genau dieses Laufs. Verknüpft Starts, Steps, Probleme und Abschluss desselben Laufs.

### field.Context.StepExecutionId

Identität einer tatsächlichen Step-Ausführung. Start und terminales Ereignis teilen sie; eine Wiederholung erhält eine neue Identität.

### field.Context.StepId

Stabile ID des Job-Steps; derselbe Step kann in mehreren Durchläufen ausgeführt werden.

### field.Context.TriggerId

Identität der konkreten Ereignisbeobachtung, nicht nur der gespeicherten Triggerkonfiguration.

### field.Details

Optionale bereinigte Zusatzdiagnose. Eingabewerte, Skriptargumente und Secrets werden nicht vollständig hineinkopiert.

### field.DiagnosticCode

Stabiler Diagnosecode für bekannte Fehler-/Speicherfälle, soweit vorhanden.

### field.DurationMs

Gemessene Dauer in Millisekunden. Ein fehlender Wert ist keine Dauer von null.

### field.FlowEffect

Strukturierter Einfluss auf den Ablauf, etwa beendete Hauptphase oder nächster Durchlauf.

### field.FlowEffect.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.Id

Eindeutige Identität des Ereignisses beziehungsweise Laufs. Ereignis-ID und RunId haben unterschiedliche Aufgaben.

### field.Iteration

Nummer des Hauptdurchlaufs, soweit das Ereignis einem Durchlauf zugeordnet ist.

### field.Level

Schweregrad Debug, Information, Warning oder Error. Eine Warning ist nicht automatisch ein abgebrochener Lauf.

### field.Level.option.Debug

Technische Detailbeobachtung.

### field.Level.option.Error

Fehlerbeobachtung; den tatsächlichen Laufabschluss zusätzlich über Outcome prüfen.

### field.Level.option.Information

Normale Lebenszyklus-/Aktionsinformation.

### field.Level.option.Warning

Problemhinweis, der nicht zwingend den Lauf beendet.

### field.Message

Lesbare, bereinigte Nachricht. Sie ist eine Anzeige und keine stabile Programmierschnittstelle.

### field.Parameters

Explizit erlaubte, bereinigte Diagnosewerte. Keine vollständige Kopie der Step-settings oder frei verfügbarer Eingabeinhalte.

### field.Paths

Explizit erlaubte strukturierte Aktionspfade. Werden getrennt von beliebigen Eingabetexten erfasst und bei Export erneut bereinigt.

### field.Paths.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.Paths.Value

Wert eines strukturierten erlaubten Pfad-/Diagnoseeintrags, entsprechend seinem umgebenden Typ.

### field.Phase

Bereich der Ausführung: Startphase, Hauptphase oder Endphase. Er beeinflusst die Bedeutung fehlender und übersprungener Steps.

### field.ProblemId

Gemeinsame Problemidentität für mehrere Diagnosen desselben Vorfalls; dadurch werden Probleme nicht anhand ihrer Textzeilen doppelt gezählt.

### field.RelatedInstanceIds

Instanzen, die an einer Automationsentscheidung beteiligt waren, zum Beispiel bereits laufende Ziele bei Restart/Ignore.

### field.SchemaVersion

Version des strukturierten Logformats; der aktuelle Repository-Vertrag ist Schema 2.

### field.Sequence

Repositoryweit aufsteigende Ereignissequenz für Reihenfolge und Seitencursor, unabhängig von gleichzeitigen Zeitstempeln.

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

### field.SourceName

Zum Ereigniszeitpunkt gespeicherter Anzeigename der Quelle.

### field.Timestamp

Zeitpunkt des Ereignisses mit Zeitzonenoffset. Für Vergleiche UTC verwenden.

### field.Trigger

Snapshot der auslösenden Ereignisart und Zielzuordnung, soweit vorhanden.

### field.Trigger.EventKind

Konkrete Ereignisart des Trigger-Snapshots.

### field.Trigger.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.Trigger.TargetId

Stabile ID des von der Automation angesprochenen Jobs/Makros.

### field.Trigger.TargetKind

Art des Automationsziels: Job oder Makro.

### field.Trigger.TargetName

Gespeicherter Anzeigename des Ziels zum Zeitpunkt der Beobachtung.

### field.Trigger.WatchedDirectory

Explizit erlaubter beobachteter Ordnerpfad für eine Datei-Automation.

### purpose

Ein einzelnes strukturiertes Ereignis des Schema-2-Verlaufs. Nutze Code, Context und Sequence für maschinelle Auswertungen; Message und Details sind bereinigte Anzeigen. Einzelereignisse und aggregierte StepSummaries ergänzen sich.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogEvent",
  "name": "LogEvent",
  "options": [],
  "fields": [
    {
      "id": "Area",
      "name": "Area",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision",
      "name": "BranchDecision",
      "type": "LogBranchDecision",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.BranchActive",
      "name": "BranchActive",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.Conditions",
      "name": "Conditions",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.Conditions.Operator",
      "name": "Operator",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.Conditions.Position",
      "name": "Position",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.Conditions.State",
      "name": "State",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.MatchMode",
      "name": "MatchMode",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.ParentActive",
      "name": "ParentActive",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "BranchDecision.Reason",
      "name": "Reason",
      "type": "String",
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
      "id": "Code",
      "name": "Code",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context",
      "name": "Context",
      "type": "LogContext",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.AutomationId",
      "name": "AutomationId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.InstanceId",
      "name": "InstanceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.ParentRunId",
      "name": "ParentRunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.RunId",
      "name": "RunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.StepExecutionId",
      "name": "StepExecutionId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.StepId",
      "name": "StepId",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Context.TriggerId",
      "name": "TriggerId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Details",
      "name": "Details",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "DiagnosticCode",
      "name": "DiagnosticCode",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "DurationMs",
      "name": "DurationMs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "FlowEffect",
      "name": "FlowEffect",
      "type": "LogFlowEffect",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "FlowEffect.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Id",
      "name": "Id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Iteration",
      "name": "Iteration",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Level",
      "name": "Level",
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
      "id": "Message",
      "name": "Message",
      "type": "String",
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
      "id": "Paths",
      "name": "Paths",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Paths.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Paths.Value",
      "name": "Value",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Phase",
      "name": "Phase",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "ProblemId",
      "name": "ProblemId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "RelatedInstanceIds",
      "name": "RelatedInstanceIds",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "SchemaVersion",
      "name": "SchemaVersion",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Sequence",
      "name": "Sequence",
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
      "id": "SourceName",
      "name": "SourceName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Timestamp",
      "name": "Timestamp",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger",
      "name": "Trigger",
      "type": "LogTriggerSnapshot",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger.EventKind",
      "name": "EventKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger.TargetId",
      "name": "TargetId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger.TargetKind",
      "name": "TargetKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger.TargetName",
      "name": "TargetName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Trigger.WatchedDirectory",
      "name": "WatchedDirectory",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

