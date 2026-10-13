## Logs: LogPage

ID: LogPage
Website: /doku/logs/LogPage/

### errors

State, Issues und IsComplete begrenzen die Aussagekraft. Ein leerer beziehungsweise nicht verfuegbarer Bestand ist kein Erfolgsbeleg. Fehlende ExecutionId oder Endzeitpunkte nicht erfinden.

### example

Filtere einen konkreten RunId und lese LogPage mit unveraenderter SnapshotSequence bis NextBeforeSequence=null. Ordne danach StepExecutionId, StepSummaries und Laufstatus gemeinsam zu. Der Log-Leitfaden zeigt ein synthetisches Ereignis im echten Serializerformat.

### field.AvailableCount

Anzahl der verfügbaren Ereignisse im betrachteten Abfragebestand. Nicht als Zahl aller jemals ausgefuehrten Steps interpretieren.

### field.Entries

Die auf dieser Seite gelieferten strukturierten LogEvent-Objekte. Fuer weitere Seiten NextBeforeSequence auswerten.

### field.Entries.Area

Textuelle Kontext-/Bereichsangabe. Für fachliche Filter zusätzlich die typisierte Category verwenden.

### field.Entries.BranchDecision

Strukturierte Entscheidung eines Bedingungsblocks mit Aktivität, Grund und Operatoren ohne rohe Vergleichswerte.

### field.Entries.BranchDecision.BranchActive

Ob der betreffende Zweig bei dieser Entscheidung gewählt wurde.

### field.Entries.BranchDecision.Conditions

Ausgewertete Bedingungen mit Position, Zustand und Operator; bewusst ohne sensible Rohvergleichswerte.

### field.Entries.BranchDecision.Conditions.Operator

Operator der ausgewerteten Bedingung, ohne deren rohen Vergleichsinhalt.

### field.Entries.BranchDecision.Conditions.Position

Position des Steps beziehungsweise der Bedingung im gespeicherten Snapshot.

### field.Entries.BranchDecision.Conditions.State

Zustand einer Bedingung/Beobachtung. Fehlende Belege nicht als false oder erfolgreich umdeuten.

### field.Entries.BranchDecision.MatchMode

Verknüpfung der Bedingungsliste, soweit diese Entscheidung einen Vergleich besitzt.

### field.Entries.BranchDecision.ParentActive

Ob der übergeordnete Block bei dieser Entscheidung aktiv war.

### field.Entries.BranchDecision.Reason

Stabiler Grund für Skip, Entscheidung oder Abschluss. Im Zusammenhang mit Code und Phase interpretieren.

### field.Entries.Category

Typisierter Fachbereich, zum Beispiel FileAccess, Capture oder Script. Daraus wird keine Ausführung anhand des Nachrichtentexts abgeleitet.

### field.Entries.Category.option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### field.Entries.Category.option.Capture

Bildschirm-/Kameraaufnahme.

### field.Entries.Category.option.Detection

Bild-/Text-/Punkterkennung und Auswertung.

### field.Entries.Category.option.Execution

Ausführung und Ablaufsteuerung.

### field.Entries.Category.option.FileAccess

Datei-/Ordnerzugriffe und ihre Diagnose.

### field.Entries.Category.option.General

Allgemeiner Log-Fachbereich ohne spezielleren Ausführungsbereich.

### field.Entries.Category.option.Process

Programmstart, Suche, Fokus und Beenden.

### field.Entries.Category.option.Script

Skriptausführung und zugehörige Hintergrundarbeit.

### field.Entries.Category.option.Video

Videoaufnahme und Finalisierung.

### field.Entries.Category.option.Windows

Windows-Taste als Modifikator (Bitmaske 8) beziehungsweise Windows-Fachbereich, wenn diese Option zu einem Log-Bereich gehört.

### field.Entries.Code

Stabiler Ereignistoken wie step.completed. Für Auswertungen diesen Token verwenden statt übersetzte Nachrichtentexte zu parsen.

### field.Entries.Context

Korrelationsobjekt mit Lauf-, Step-, Trigger- und gegebenenfalls Elternlauf-Identitäten.

### field.Entries.Context.AutomationId

ID der auslösenden Automation, falls der Lauf aus einer Automation stammt.

### field.Entries.Context.InstanceId

Dispatcher-Instanz-ID der Ausführung. Mehrere Ausführungen desselben Jobs besitzen unterschiedliche Instanzen.

### field.Entries.Context.ParentRunId

Übergeordneter Lauf eines Unterjobs. Ermöglicht die Navigation von besitzender Arbeit zu Kindläufen.

### field.Entries.Context.RunId

ID genau dieses Laufs. Verknüpft Starts, Steps, Probleme und Abschluss desselben Laufs.

### field.Entries.Context.StepExecutionId

Identität einer tatsächlichen Step-Ausführung. Start und terminales Ereignis teilen sie; eine Wiederholung erhält eine neue Identität.

### field.Entries.Context.StepId

Stabile ID des Job-Steps; derselbe Step kann in mehreren Durchläufen ausgeführt werden.

### field.Entries.Context.TriggerId

Identität der konkreten Ereignisbeobachtung, nicht nur der gespeicherten Triggerkonfiguration.

### field.Entries.Details

Optionale bereinigte Zusatzdiagnose. Eingabewerte, Skriptargumente und Secrets werden nicht vollständig hineinkopiert.

### field.Entries.DiagnosticCode

Stabiler Diagnosecode für bekannte Fehler-/Speicherfälle, soweit vorhanden.

### field.Entries.DurationMs

Gemessene Dauer in Millisekunden. Ein fehlender Wert ist keine Dauer von null.

### field.Entries.FlowEffect

Strukturierter Einfluss auf den Ablauf, etwa beendete Hauptphase oder nächster Durchlauf.

### field.Entries.FlowEffect.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.Entries.Id

Eindeutige Identität des Ereignisses beziehungsweise Laufs. Ereignis-ID und RunId haben unterschiedliche Aufgaben.

### field.Entries.Iteration

Nummer des Hauptdurchlaufs, soweit das Ereignis einem Durchlauf zugeordnet ist.

### field.Entries.Level

Schweregrad Debug, Information, Warning oder Error. Eine Warning ist nicht automatisch ein abgebrochener Lauf.

### field.Entries.Level.option.Debug

Technische Detailbeobachtung.

### field.Entries.Level.option.Error

Fehlerbeobachtung; den tatsächlichen Laufabschluss zusätzlich über Outcome prüfen.

### field.Entries.Level.option.Information

Normale Lebenszyklus-/Aktionsinformation.

### field.Entries.Level.option.Warning

Problemhinweis, der nicht zwingend den Lauf beendet.

### field.Entries.Message

Lesbare, bereinigte Nachricht. Sie ist eine Anzeige und keine stabile Programmierschnittstelle.

### field.Entries.Parameters

Explizit erlaubte, bereinigte Diagnosewerte. Keine vollständige Kopie der Step-settings oder frei verfügbarer Eingabeinhalte.

### field.Entries.Paths

Explizit erlaubte strukturierte Aktionspfade. Werden getrennt von beliebigen Eingabetexten erfasst und bei Export erneut bereinigt.

### field.Entries.Paths.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.Entries.Paths.Value

Wert eines strukturierten erlaubten Pfad-/Diagnoseeintrags, entsprechend seinem umgebenden Typ.

### field.Entries.Phase

Bereich der Ausführung: Startphase, Hauptphase oder Endphase. Er beeinflusst die Bedeutung fehlender und übersprungener Steps.

### field.Entries.ProblemId

Gemeinsame Problemidentität für mehrere Diagnosen desselben Vorfalls; dadurch werden Probleme nicht anhand ihrer Textzeilen doppelt gezählt.

### field.Entries.RelatedInstanceIds

Instanzen, die an einer Automationsentscheidung beteiligt waren, zum Beispiel bereits laufende Ziele bei Restart/Ignore.

### field.Entries.SchemaVersion

Version des strukturierten Logformats; der aktuelle Repository-Vertrag ist Schema 2.

### field.Entries.Sequence

Repositoryweit aufsteigende Ereignissequenz für Reihenfolge und Seitencursor, unabhängig von gleichzeitigen Zeitstempeln.

### field.Entries.Source

Ursprungsart Job, Automation, Application oder Makro.

### field.Entries.Source.option.Application

Ereignis stammt aus der Anwendung selbst.

### field.Entries.Source.option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### field.Entries.Source.option.Job

Quelle beziehungsweise Ziel ist ein Job.

### field.Entries.Source.option.Makro

Quelle beziehungsweise Ziel ist ein Makro.

### field.Entries.SourceId

Stabile ID des ursprünglichen Jobs/Makros/der Automation; sie ist nicht die Identität eines einzelnen Laufs.

### field.Entries.SourceName

Zum Ereigniszeitpunkt gespeicherter Anzeigename der Quelle.

### field.Entries.Timestamp

Zeitpunkt des Ereignisses mit Zeitzonenoffset. Für Vergleiche UTC verwenden.

### field.Entries.Trigger

Snapshot der auslösenden Ereignisart und Zielzuordnung, soweit vorhanden.

### field.Entries.Trigger.EventKind

Konkrete Ereignisart des Trigger-Snapshots.

### field.Entries.Trigger.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.Entries.Trigger.TargetId

Stabile ID des von der Automation angesprochenen Jobs/Makros.

### field.Entries.Trigger.TargetKind

Art des Automationsziels: Job oder Makro.

### field.Entries.Trigger.TargetName

Gespeicherter Anzeigename des Ziels zum Zeitpunkt der Beobachtung.

### field.Entries.Trigger.WatchedDirectory

Explizit erlaubter beobachteter Ordnerpfad für eine Datei-Automation.

### field.Issues

Bekannte Probleme beim Lesen oder Speichern. Lesbare Teile koennen vorliegen; fehlende Belege nicht rekonstruieren.

### field.MatchCount

Anzahl passender Ereignisse für die gesetzten Filter. Nicht gleich der Anzahl von Jobläufen oder aggregierten Step-Ausfuehrungen.

### field.NextBeforeSequence

Optionale obere Grenze der nächsten rückwärtigen Seite. null bedeutet keine weitere Seite; vorhandene Leseluecken bleiben trotzdem möglich.

### field.SnapshotSequence

Obere Snapshot-Sequenz der Abfrage. Bei Folgeseiten denselben Wert verwenden, damit neu angehängte Ereignisse den abgefragten Bestand nicht verschieben.

### field.State

LogReadState dieser Abfrage: Available, Empty, Partial oder Unavailable. Issues und verfügbare Entries gemeinsam auswerten.

### field.State.option.Available

Logdaten sind verfügbar; zusätzlich IsComplete und Issues prüfen.

### field.State.option.Empty

Der abgefragte Logbereich enthält keine Ereignisse; das beweist keine erfolgreiche Ausführung.

### field.State.option.Partial

Daten sind teilweise lesbar; Fehler und bekannte Lücken bleiben sichtbar.

### field.State.option.Unavailable

Logdaten sind nicht verfügbar; Abwesenheit von Fehlern ist dann keine Erfolgsaussage.

### purpose

Eine Seite einer Logabfrage mit Ereignissen, Zählern, Folgeseitengrenze und Lesequalitaet. Teilweise lesbare Daten bleiben nutzbar, ihre bekannten Probleme bleiben jedoch Teil des Ergebnisses.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogPage",
  "name": "LogPage",
  "options": [],
  "fields": [
    {
      "id": "AvailableCount",
      "name": "AvailableCount",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries",
      "name": "Entries",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Area",
      "name": "Area",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision",
      "name": "BranchDecision",
      "type": "LogBranchDecision",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.BranchActive",
      "name": "BranchActive",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.Conditions",
      "name": "Conditions",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.Conditions.Operator",
      "name": "Operator",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.Conditions.Position",
      "name": "Position",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.Conditions.State",
      "name": "State",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.MatchMode",
      "name": "MatchMode",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.ParentActive",
      "name": "ParentActive",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.BranchDecision.Reason",
      "name": "Reason",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Category",
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
      "id": "Entries.Code",
      "name": "Code",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context",
      "name": "Context",
      "type": "LogContext",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.AutomationId",
      "name": "AutomationId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.InstanceId",
      "name": "InstanceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.ParentRunId",
      "name": "ParentRunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.RunId",
      "name": "RunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.StepExecutionId",
      "name": "StepExecutionId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.StepId",
      "name": "StepId",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Context.TriggerId",
      "name": "TriggerId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Details",
      "name": "Details",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.DiagnosticCode",
      "name": "DiagnosticCode",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.DurationMs",
      "name": "DurationMs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.FlowEffect",
      "name": "FlowEffect",
      "type": "LogFlowEffect",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.FlowEffect.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Id",
      "name": "Id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Iteration",
      "name": "Iteration",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Level",
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
      "id": "Entries.Message",
      "name": "Message",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Parameters",
      "name": "Parameters",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Paths",
      "name": "Paths",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Paths.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Paths.Value",
      "name": "Value",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Phase",
      "name": "Phase",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.ProblemId",
      "name": "ProblemId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.RelatedInstanceIds",
      "name": "RelatedInstanceIds",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.SchemaVersion",
      "name": "SchemaVersion",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Sequence",
      "name": "Sequence",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Source",
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
      "id": "Entries.SourceId",
      "name": "SourceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.SourceName",
      "name": "SourceName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Timestamp",
      "name": "Timestamp",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger",
      "name": "Trigger",
      "type": "LogTriggerSnapshot",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger.EventKind",
      "name": "EventKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger.TargetId",
      "name": "TargetId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger.TargetKind",
      "name": "TargetKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger.TargetName",
      "name": "TargetName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Entries.Trigger.WatchedDirectory",
      "name": "WatchedDirectory",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Issues",
      "name": "Issues",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "MatchCount",
      "name": "MatchCount",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "NextBeforeSequence",
      "name": "NextBeforeSequence",
      "type": "Int64",
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
      "id": "State",
      "name": "State",
      "type": "LogReadState",
      "options": [
        "Available",
        "Empty",
        "Partial",
        "Unavailable"
      ],
      "defaultValue": null
    }
  ]
}
```

