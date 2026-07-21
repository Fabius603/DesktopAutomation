# Neuen Job-Step hinzufügen – Backend (TaskAutomation)

Diese Anleitung beschreibt alle Schritte, die nötig sind, um einen neuen Step-Typ im Backend zu registrieren und auszuführen.

---

## Übersicht der beteiligten Dateien

| Datei | Aufgabe |
|---|---|
| `TaskAutomation/Jobs/StepData.cs` | Datenhaltungsklassen (Step + Settings) |
| `TaskAutomation/Steps/StepResults.cs` | Ergebnistypen der Steps |
| `TaskAutomation/Steps/StepResultMetadata.cs` | Metadaten für Bedingungsauswertung |
| `TaskAutomation/Steps/StepPipelineRegistry.cs` | Pipeline-Validierung & Name-Mapping |
| `TaskAutomation/Steps/<MeinStep>StepHandler.cs` | Implementierung der Ausführungslogik |
| `TaskAutomation/Jobs/JobExecutor.cs` | Handler-Registrierung |

---

## Schritt-für-Schritt-Anleitung

### 1. Datenhaltungsklassen erstellen (`StepData.cs`)

a) Füge einen neuen `[JsonDerivedType]`-Attribute über der abstrakten `JobStep`-Klasse hinzu:

```csharp
[JsonDerivedType(typeof(MeinStep), "mein_step")]
```

b) Definiere die Step-Klasse und ihre Settings-Klasse am Ende der Datei:

```csharp
// ---- MeinStep ----
public sealed class MeinStep : JobStep
{
    [JsonPropertyName("settings")]
    public MeinStepSettings Settings { get; set; } = new();
}

public sealed class MeinStepSettings
{
    [JsonPropertyName("mein_parameter")]
    public string MeinParameter { get; set; } = string.Empty;
}
```

### 2. Ergebnistyp definieren (`StepResults.cs`)

Jeder ausführbare Step besitzt einen eigenen Ergebnistyp nach dem Muster `<Stepname>Result`:

```csharp
public sealed record MeinResult : StepResultBase
{
    public string MeinWert { get; init; } = string.Empty;

    public static readonly MeinResult Default = new() { WasExecuted = false };
}
```

> Auch ein Step ohne auswählbare Nutzdaten erhält einen eigenen Ergebnistyp. Technischer Ausführungsstatus wird nicht im Picker veröffentlicht.

### 3. Metadaten festlegen (`StepResultMetadata.cs`)

Konkrete Ergebnistypen und ihre öffentlichen Eigenschaften werden automatisch entdeckt.
Interne Laufzeitdaten und `WasExecuted` gehören nicht zum auswählbaren Schema.

Eigenschaften werden standardmäßig veröffentlicht. `WasExecuted`, `Success` und
`ErrorMessage` sind technische Ausführungswerte und bleiben im Picker verborgen.
Weitere technische Felder können ausdrücklich mit `[ResultHidden]` markiert werden.

### 4. Pipeline registrieren (`StepPipelineRegistry.cs`)

a) In `_map` (Voraussetzungen und Ausgabe angeben):

```csharp
[typeof(MeinStep)] = new(
    Prerequisites: [],          // Leer = keine Voraussetzungen
    ResultType:    typeof(MeinResult)
),
```

b) In `_nameMap` (Name-zu-Typ-Mapping):

```csharp
["MeinStep"] = typeof(MeinStep),
```

### 5. Handler implementieren

Erstelle eine neue Datei `MeinStepHandler.cs` im Ordner `TaskAutomation/Steps/`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public sealed class MeinStepHandler : JobStepHandler<MeinStep, MeinResult>
    {
        protected override async Task<MeinResult> ExecuteCoreAsync(
            MeinStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            // Implementierung hier
            ctx.Logger.LogInformation("MeinStepHandler wird ausgeführt.");
            return new MeinResult { WasExecuted = true, MeinWert = "Ergebnis" };
        }

        protected override MeinResult CreateDefault() => MeinResult.Default;
    }
}
```

### 6. Handler registrieren (`JobExecutor.cs`)

Im `_stepHandlers`-Dictionary in `JobExecutor.cs` eintragen:

```csharp
{ typeof(MeinStep), new MeinStepHandler() },
```

---

## Checkliste

- [ ] `[JsonDerivedType]` in `StepData.cs` eingetragen
- [ ] `MeinStep` + `MeinStepSettings` in `StepData.cs` definiert
- [ ] Eigenen Ergebnistyp `<Stepname>Result` in `StepResults.cs` definiert
- [ ] Nur die gewünschten Picker-Eigenschaften in `StepResultMetadata` veröffentlicht
- [ ] `StepPipelineRegistry._map` aktualisiert
- [ ] `StepPipelineRegistry._nameMap` aktualisiert
- [ ] Handler-Klasse erstellt
- [ ] Handler in `JobExecutor._stepHandlers` registriert
- [ ] Frontend-Schritte aus `DesktopAutomationApp/README_neuen_step_hinzufuegen.md` abarbeiten
