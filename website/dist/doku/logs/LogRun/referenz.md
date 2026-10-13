## Logs: LogRun

ID: LogRun
Website: /doku/logs/LogRun/

### errors

Eine Datenlücke, nicht verfügbare Datei oder IsComplete=false begrenzt die Aussagekraft. Ein Start-/Dispatch-Ereignis beweist keinen Abschluss. Nicht ausgeführte, deaktivierte und übersprungene Steps getrennt behandeln; übersetzte Message-Texte nicht als stabile Statuscodes parsen.

### example

Öffne Logs, filtere die Quelle und den Zeitraum, öffne einen Lauf und verknüpfe die Ereignisse über Context.RunId. Für eine technische Untersuchung den Bericht exportieren und runs.json zusammen mit den JSONL-Ereignissen lesen. Einzelereignisse und StepSummaries gemeinsam berücksichtigen.

### field.CatalogSequence

Checkpoint der Lauf-Liste zur stabilen Seitennavigation; nicht mit der Sequenz jedes Einzelereignisses verwechseln.

### field.CompletedIterations

Anzahl abgeschlossener Hauptdurchläufe dieses Laufs.

### field.CompletionReason

Grund des Laufabschlusses; im Zusammenhang mit Outcome auswerten.

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

### field.DurationMs

Gemessene Dauer in Millisekunden. Ein fehlender Wert ist keine Dauer von null.

### field.EndedAt

Gespeicherter Endzeitpunkt, falls ein Abschluss belegt ist. Bei nach Neustart unterbrochenen Läufen wird kein Enddatum erfunden.

### field.ErrorCount

Erfasste Fehleranzahl des Laufs nach gemeinsamen Problemregeln; nicht einfach alle Nachrichtenzeilen zählen.

### field.ExecutionNumber

Fortlaufender Ausführungszähler der Quelle. Er bleibt nach Aufbewahrungsbereinigung von Ereignissen erhalten.

### field.Id

Eindeutige Identität des Ereignisses beziehungsweise Laufs. Ereignis-ID und RunId haben unterschiedliche Aufgaben.

### field.InstanceId

Dispatcher-Instanz-ID der Ausführung. Mehrere Ausführungen desselben Jobs besitzen unterschiedliche Instanzen.

### field.IsComplete

Ob die aufbewahrten Daten als vollständig gelten. false verlangt, Grenzen der Diagnose ausdrücklich zu berücksichtigen.

### field.IterationDurationMs

Gesamtdauer abgeschlossener Hauptdurchläufe in Millisekunden.

### field.LostEntries

Bekannte Anzahl verlorener beziehungsweise abgelehnter Logeinträge. 0 beweist keine Rekonstruktion nie erfasster Vorgänge.

### field.Name

Im Lauf-Snapshot gespeicherter Quellenname; spätere Umbenennung ändert alte Ereignisse nicht.

### field.Origin

Startursprung des Laufs, zum Beispiel manuell, Automation oder Unterjob.

### field.OriginId

Identität des Startursprungs, falls vorhanden.

### field.OriginName

Zum Start gespeicherter Anzeigename des Ursprungs.

### field.Outcome

Lebenszyklus und Qualität des Laufs. Successful, WithWarnings und WithErrors unterscheiden Abschluss von Problemfreiheit.

### field.Outcome.option.Failed

Lauf wegen Fehler fehlgeschlagen.

### field.Outcome.option.Interrupted

Lauf hatte beim Neustart keinen belegten Abschluss und gilt als unterbrochen/unvollständig.

### field.Outcome.option.Paused

Lauf ist pausiert; das ist kein erfolgreicher Abschluss.

### field.Outcome.option.Running

Lauf ist noch aktiv und hat keinen belegten Abschluss.

### field.Outcome.option.Stopped

Lauf wurde ausdrücklich gestoppt/abgebrochen.

### field.Outcome.option.Successful

Lauf erfolgreich beendet, ohne die Qualität als Warnung/Fehler einzustufen.

### field.Outcome.option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

### field.Outcome.option.WithErrors

Lauf abgeschlossen, aber Fehler beeinträchtigen seine Qualität; nicht als problemfrei auswerten.

### field.Outcome.option.WithWarnings

Lauf beendet, dabei wurden Warnungen erfasst.

### field.PrimaryProblemId

Hauptproblem des Laufs zur direkten Problemnavigation.

### field.SchemaVersion

Version des strukturierten Logformats; der aktuelle Repository-Vertrag ist Schema 2.

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

### field.StartedAt

Gespeicherter Startzeitpunkt des Laufs mit Offset.

### field.StepSummaries

Aggregierte erfolgreiche/übersprungene Wiederholungen der Hauptphase; Problemereignisse bleiben einzeln nachvollziehbar.

### field.StepSummaries.Code

Stabiler Ereignistoken wie step.completed. Für Auswertungen diesen Token verwenden statt übersetzte Nachrichtentexte zu parsen.

### field.StepSummaries.Count

Anzahl der in dieser Zusammenfassung enthaltenen Beobachtungen.

### field.StepSummaries.IterationCoverageComplete

Ob die Zuordnung aller zusammengefassten Durchläufe vollständig belegt ist.

### field.StepSummaries.Iterations

Zusammengefasste Durchlaufbereiche dieser Beobachtung; anhand First und Last lesen.

### field.StepSummaries.Iterations.First

Erster Durchlauf eines zusammengefassten Bereichs.

### field.StepSummaries.Iterations.Last

Letzter Durchlauf eines zusammengefassten Bereichs, einschließlich dieser Grenze.

### field.StepSummaries.LastEvent

Letzte bereinigte Beobachtung der Zusammenfassung. Sie ist nicht automatisch ein zusätzliches individuelles Ereignis im JSONL-Strom.

### field.StepSummaries.LastEvent.Area

Textuelle Kontext-/Bereichsangabe. Für fachliche Filter zusätzlich die typisierte Category verwenden.

### field.StepSummaries.LastEvent.BranchDecision

Strukturierte Entscheidung eines Bedingungsblocks mit Aktivität, Grund und Operatoren ohne rohe Vergleichswerte.

### field.StepSummaries.LastEvent.BranchDecision.BranchActive

Ob der betreffende Zweig bei dieser Entscheidung gewählt wurde.

### field.StepSummaries.LastEvent.BranchDecision.Conditions

Ausgewertete Bedingungen mit Position, Zustand und Operator; bewusst ohne sensible Rohvergleichswerte.

### field.StepSummaries.LastEvent.BranchDecision.Conditions.Operator

Operator der ausgewerteten Bedingung, ohne deren rohen Vergleichsinhalt.

### field.StepSummaries.LastEvent.BranchDecision.Conditions.Position

Position des Steps beziehungsweise der Bedingung im gespeicherten Snapshot.

### field.StepSummaries.LastEvent.BranchDecision.Conditions.State

Zustand einer Bedingung/Beobachtung. Fehlende Belege nicht als false oder erfolgreich umdeuten.

### field.StepSummaries.LastEvent.BranchDecision.MatchMode

Verknüpfung der Bedingungsliste, soweit diese Entscheidung einen Vergleich besitzt.

### field.StepSummaries.LastEvent.BranchDecision.ParentActive

Ob der übergeordnete Block bei dieser Entscheidung aktiv war.

### field.StepSummaries.LastEvent.BranchDecision.Reason

Stabiler Grund für Skip, Entscheidung oder Abschluss. Im Zusammenhang mit Code und Phase interpretieren.

### field.StepSummaries.LastEvent.Category

Typisierter Fachbereich, zum Beispiel FileAccess, Capture oder Script. Daraus wird keine Ausführung anhand des Nachrichtentexts abgeleitet.

### field.StepSummaries.LastEvent.Category.option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### field.StepSummaries.LastEvent.Category.option.Capture

Bildschirm-/Kameraaufnahme.

### field.StepSummaries.LastEvent.Category.option.Detection

Bild-/Text-/Punkterkennung und Auswertung.

### field.StepSummaries.LastEvent.Category.option.Execution

Ausführung und Ablaufsteuerung.

### field.StepSummaries.LastEvent.Category.option.FileAccess

Datei-/Ordnerzugriffe und ihre Diagnose.

### field.StepSummaries.LastEvent.Category.option.General

Allgemeiner Log-Fachbereich ohne spezielleren Ausführungsbereich.

### field.StepSummaries.LastEvent.Category.option.Process

Programmstart, Suche, Fokus und Beenden.

### field.StepSummaries.LastEvent.Category.option.Script

Skriptausführung und zugehörige Hintergrundarbeit.

### field.StepSummaries.LastEvent.Category.option.Video

Videoaufnahme und Finalisierung.

### field.StepSummaries.LastEvent.Category.option.Windows

Windows-Taste als Modifikator (Bitmaske 8) beziehungsweise Windows-Fachbereich, wenn diese Option zu einem Log-Bereich gehört.

### field.StepSummaries.LastEvent.Code

Stabiler Ereignistoken wie step.completed. Für Auswertungen diesen Token verwenden statt übersetzte Nachrichtentexte zu parsen.

### field.StepSummaries.LastEvent.Context

Korrelationsobjekt mit Lauf-, Step-, Trigger- und gegebenenfalls Elternlauf-Identitäten.

### field.StepSummaries.LastEvent.Context.AutomationId

ID der auslösenden Automation, falls der Lauf aus einer Automation stammt.

### field.StepSummaries.LastEvent.Context.InstanceId

Dispatcher-Instanz-ID der Ausführung. Mehrere Ausführungen desselben Jobs besitzen unterschiedliche Instanzen.

### field.StepSummaries.LastEvent.Context.ParentRunId

Übergeordneter Lauf eines Unterjobs. Ermöglicht die Navigation von besitzender Arbeit zu Kindläufen.

### field.StepSummaries.LastEvent.Context.RunId

ID genau dieses Laufs. Verknüpft Starts, Steps, Probleme und Abschluss desselben Laufs.

### field.StepSummaries.LastEvent.Context.StepExecutionId

Identität einer tatsächlichen Step-Ausführung. Start und terminales Ereignis teilen sie; eine Wiederholung erhält eine neue Identität.

### field.StepSummaries.LastEvent.Context.StepId

Stabile ID des Job-Steps; derselbe Step kann in mehreren Durchläufen ausgeführt werden.

### field.StepSummaries.LastEvent.Context.TriggerId

Identität der konkreten Ereignisbeobachtung, nicht nur der gespeicherten Triggerkonfiguration.

### field.StepSummaries.LastEvent.Details

Optionale bereinigte Zusatzdiagnose. Eingabewerte, Skriptargumente und Secrets werden nicht vollständig hineinkopiert.

### field.StepSummaries.LastEvent.DiagnosticCode

Stabiler Diagnosecode für bekannte Fehler-/Speicherfälle, soweit vorhanden.

### field.StepSummaries.LastEvent.DurationMs

Gemessene Dauer in Millisekunden. Ein fehlender Wert ist keine Dauer von null.

### field.StepSummaries.LastEvent.FlowEffect

Strukturierter Einfluss auf den Ablauf, etwa beendete Hauptphase oder nächster Durchlauf.

### field.StepSummaries.LastEvent.FlowEffect.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.StepSummaries.LastEvent.Id

Eindeutige Identität des Ereignisses beziehungsweise Laufs. Ereignis-ID und RunId haben unterschiedliche Aufgaben.

### field.StepSummaries.LastEvent.Iteration

Nummer des Hauptdurchlaufs, soweit das Ereignis einem Durchlauf zugeordnet ist.

### field.StepSummaries.LastEvent.Level

Schweregrad Debug, Information, Warning oder Error. Eine Warning ist nicht automatisch ein abgebrochener Lauf.

### field.StepSummaries.LastEvent.Level.option.Debug

Technische Detailbeobachtung.

### field.StepSummaries.LastEvent.Level.option.Error

Fehlerbeobachtung; den tatsächlichen Laufabschluss zusätzlich über Outcome prüfen.

### field.StepSummaries.LastEvent.Level.option.Information

Normale Lebenszyklus-/Aktionsinformation.

### field.StepSummaries.LastEvent.Level.option.Warning

Problemhinweis, der nicht zwingend den Lauf beendet.

### field.StepSummaries.LastEvent.Message

Lesbare, bereinigte Nachricht. Sie ist eine Anzeige und keine stabile Programmierschnittstelle.

### field.StepSummaries.LastEvent.Parameters

Explizit erlaubte, bereinigte Diagnosewerte. Keine vollständige Kopie der Step-settings oder frei verfügbarer Eingabeinhalte.

### field.StepSummaries.LastEvent.Paths

Explizit erlaubte strukturierte Aktionspfade. Werden getrennt von beliebigen Eingabetexten erfasst und bei Export erneut bereinigt.

### field.StepSummaries.LastEvent.Paths.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.StepSummaries.LastEvent.Paths.Value

Wert eines strukturierten erlaubten Pfad-/Diagnoseeintrags, entsprechend seinem umgebenden Typ.

### field.StepSummaries.LastEvent.Phase

Bereich der Ausführung: Startphase, Hauptphase oder Endphase. Er beeinflusst die Bedeutung fehlender und übersprungener Steps.

### field.StepSummaries.LastEvent.ProblemId

Gemeinsame Problemidentität für mehrere Diagnosen desselben Vorfalls; dadurch werden Probleme nicht anhand ihrer Textzeilen doppelt gezählt.

### field.StepSummaries.LastEvent.RelatedInstanceIds

Instanzen, die an einer Automationsentscheidung beteiligt waren, zum Beispiel bereits laufende Ziele bei Restart/Ignore.

### field.StepSummaries.LastEvent.SchemaVersion

Version des strukturierten Logformats; der aktuelle Repository-Vertrag ist Schema 2.

### field.StepSummaries.LastEvent.Sequence

Repositoryweit aufsteigende Ereignissequenz für Reihenfolge und Seitencursor, unabhängig von gleichzeitigen Zeitstempeln.

### field.StepSummaries.LastEvent.Source

Ursprungsart Job, Automation, Application oder Makro.

### field.StepSummaries.LastEvent.Source.option.Application

Ereignis stammt aus der Anwendung selbst.

### field.StepSummaries.LastEvent.Source.option.Automation

Triggerbeobachtung und Automationsentscheidungen.

### field.StepSummaries.LastEvent.Source.option.Job

Quelle beziehungsweise Ziel ist ein Job.

### field.StepSummaries.LastEvent.Source.option.Makro

Quelle beziehungsweise Ziel ist ein Makro.

### field.StepSummaries.LastEvent.SourceId

Stabile ID des ursprünglichen Jobs/Makros/der Automation; sie ist nicht die Identität eines einzelnen Laufs.

### field.StepSummaries.LastEvent.SourceName

Zum Ereigniszeitpunkt gespeicherter Anzeigename der Quelle.

### field.StepSummaries.LastEvent.Timestamp

Zeitpunkt des Ereignisses mit Zeitzonenoffset. Für Vergleiche UTC verwenden.

### field.StepSummaries.LastEvent.Trigger

Snapshot der auslösenden Ereignisart und Zielzuordnung, soweit vorhanden.

### field.StepSummaries.LastEvent.Trigger.EventKind

Konkrete Ereignisart des Trigger-Snapshots.

### field.StepSummaries.LastEvent.Trigger.Kind

Art des Triggers beziehungsweise der strukturierten Beobachtung.

### field.StepSummaries.LastEvent.Trigger.TargetId

Stabile ID des von der Automation angesprochenen Jobs/Makros.

### field.StepSummaries.LastEvent.Trigger.TargetKind

Art des Automationsziels: Job oder Makro.

### field.StepSummaries.LastEvent.Trigger.TargetName

Gespeicherter Anzeigename des Ziels zum Zeitpunkt der Beobachtung.

### field.StepSummaries.LastEvent.Trigger.WatchedDirectory

Explizit erlaubter beobachteter Ordnerpfad für eine Datei-Automation.

### field.StepSummaries.Phase

Bereich der Ausführung: Startphase, Hauptphase oder Endphase. Er beeinflusst die Bedeutung fehlender und übersprungener Steps.

### field.StepSummaries.Reason

Stabiler Grund für Skip, Entscheidung oder Abschluss. Im Zusammenhang mit Code und Phase interpretieren.

### field.StepSummaries.StepId

Stabile ID des Job-Steps; derselbe Step kann in mehreren Durchläufen ausgeführt werden.

### field.StepSummaries.TotalDurationMs

Summe der gemessenen Dauer der zusammengefassten Beobachtungen in Millisekunden.

### field.Steps

Unveränderlicher Step-Snapshot mit ID, Typ, Position, Phase und Aktivierung. Enthält keine vollständigen settings oder Eingabewerte.

### field.Steps.Enabled

Ob der Step im Lauf-Snapshot aktiviert war. Disabled ist von einem aktivierten, aber nicht erreichten Step zu unterscheiden.

### field.Steps.Id

Eindeutige Identität des Ereignisses beziehungsweise Laufs. Ereignis-ID und RunId haben unterschiedliche Aufgaben.

### field.Steps.Phase

Bereich der Ausführung: Startphase, Hauptphase oder Endphase. Er beeinflusst die Bedeutung fehlender und übersprungener Steps.

### field.Steps.Position

Position des Steps beziehungsweise der Bedingung im gespeicherten Snapshot.

### field.Steps.TypeId

Kanonische Step-Typ-ID, zum Beispiel file_system_operation, unabhängig vom sichtbaren Label.

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

### field.WarningCount

Erfasste Warnungsanzahl des Laufs.

### purpose

Unveränderliche Quellen-/Step-Snapshots und fortgeschriebene Lebenszyklus-/Qualitätsdaten eines Laufs. Der Status ist nicht allein aus einer Startanforderung oder Nachricht abzuleiten. IsComplete und LostEntries zeigen bekannte Grenzen der Belege.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "LogRun",
  "name": "LogRun",
  "options": [],
  "fields": [
    {
      "id": "CatalogSequence",
      "name": "CatalogSequence",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "CompletedIterations",
      "name": "CompletedIterations",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "CompletionReason",
      "name": "CompletionReason",
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
      "id": "DurationMs",
      "name": "DurationMs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "EndedAt",
      "name": "EndedAt",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "ErrorCount",
      "name": "ErrorCount",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "ExecutionNumber",
      "name": "ExecutionNumber",
      "type": "Int64",
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
      "id": "InstanceId",
      "name": "InstanceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "IsComplete",
      "name": "IsComplete",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "IterationDurationMs",
      "name": "IterationDurationMs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "LostEntries",
      "name": "LostEntries",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Name",
      "name": "Name",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Origin",
      "name": "Origin",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "OriginId",
      "name": "OriginId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "OriginName",
      "name": "OriginName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Outcome",
      "name": "Outcome",
      "type": "LogOutcome",
      "options": [
        "Running",
        "Paused",
        "Successful",
        "WithWarnings",
        "WithErrors",
        "Failed",
        "Stopped",
        "Interrupted",
        "Unknown"
      ],
      "defaultValue": null
    },
    {
      "id": "PrimaryProblemId",
      "name": "PrimaryProblemId",
      "type": "Guid",
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
      "id": "StartedAt",
      "name": "StartedAt",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Steps",
      "name": "Steps",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Steps.Enabled",
      "name": "Enabled",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Steps.Id",
      "name": "Id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Steps.Phase",
      "name": "Phase",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Steps.Position",
      "name": "Position",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "Steps.TypeId",
      "name": "TypeId",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries",
      "name": "StepSummaries",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Code",
      "name": "Code",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Count",
      "name": "Count",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.IterationCoverageComplete",
      "name": "IterationCoverageComplete",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Iterations",
      "name": "Iterations",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Iterations.First",
      "name": "First",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Iterations.Last",
      "name": "Last",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent",
      "name": "LastEvent",
      "type": "LogEvent",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Area",
      "name": "Area",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision",
      "name": "BranchDecision",
      "type": "LogBranchDecision",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.BranchActive",
      "name": "BranchActive",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.Conditions",
      "name": "Conditions",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.Conditions.Operator",
      "name": "Operator",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.Conditions.Position",
      "name": "Position",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.Conditions.State",
      "name": "State",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.MatchMode",
      "name": "MatchMode",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.ParentActive",
      "name": "ParentActive",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.BranchDecision.Reason",
      "name": "Reason",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Category",
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
      "id": "StepSummaries.LastEvent.Code",
      "name": "Code",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context",
      "name": "Context",
      "type": "LogContext",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.AutomationId",
      "name": "AutomationId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.InstanceId",
      "name": "InstanceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.ParentRunId",
      "name": "ParentRunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.RunId",
      "name": "RunId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.StepExecutionId",
      "name": "StepExecutionId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.StepId",
      "name": "StepId",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Context.TriggerId",
      "name": "TriggerId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Details",
      "name": "Details",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.DiagnosticCode",
      "name": "DiagnosticCode",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.DurationMs",
      "name": "DurationMs",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.FlowEffect",
      "name": "FlowEffect",
      "type": "LogFlowEffect",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.FlowEffect.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Id",
      "name": "Id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Iteration",
      "name": "Iteration",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Level",
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
      "id": "StepSummaries.LastEvent.Message",
      "name": "Message",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Parameters",
      "name": "Parameters",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Paths",
      "name": "Paths",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Paths.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Paths.Value",
      "name": "Value",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Phase",
      "name": "Phase",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.ProblemId",
      "name": "ProblemId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.RelatedInstanceIds",
      "name": "RelatedInstanceIds",
      "type": "IReadOnlyList`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.SchemaVersion",
      "name": "SchemaVersion",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Sequence",
      "name": "Sequence",
      "type": "Int64",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Source",
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
      "id": "StepSummaries.LastEvent.SourceId",
      "name": "SourceId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.SourceName",
      "name": "SourceName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Timestamp",
      "name": "Timestamp",
      "type": "DateTimeOffset",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger",
      "name": "Trigger",
      "type": "LogTriggerSnapshot",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger.EventKind",
      "name": "EventKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger.Kind",
      "name": "Kind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger.TargetId",
      "name": "TargetId",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger.TargetKind",
      "name": "TargetKind",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger.TargetName",
      "name": "TargetName",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.LastEvent.Trigger.WatchedDirectory",
      "name": "WatchedDirectory",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Phase",
      "name": "Phase",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.Reason",
      "name": "Reason",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.StepId",
      "name": "StepId",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "StepSummaries.TotalDurationMs",
      "name": "TotalDurationMs",
      "type": "Int64",
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
    },
    {
      "id": "WarningCount",
      "name": "WarningCount",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

