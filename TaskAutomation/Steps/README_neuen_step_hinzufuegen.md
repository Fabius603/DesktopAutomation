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

Falls dein Step einen neuen Ergebnistyp liefert (z. B. nicht `TaskResult`), definiere ihn hier:

```csharp
public sealed record MeinResult : StepResultBase
{
    public string MeinWert { get; init; } = string.Empty;

    public static readonly MeinResult Default = new() { WasExecuted = false };
}
```

> Wenn dein Step nur `TaskResult` (`Success = true/false`) liefert, kannst du diesen Schritt überspringen.

### 3. Metadaten registrieren (`StepResultMetadata.cs`)

a) In `Properties`:

```csharp
["MeinStep"] =
[
    new("Success",     "Erfolgreich",      ResultPropertyType.Bool),
    new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
],
```

b) In `FriendlyNames`:

```csharp
["MeinStep"] = "Mein Step (Anzeigename)",
```

c) Falls du einen **neuen Ergebnistyp** (nicht `TaskResult`/`CaptureResult`/…) erstellt hast, trage ihn auch in `ResultTypes` ein:

```csharp
new ResultTypeDescriptor("MeinResult", "MeinResult",
[
    new ResultPropertyDescriptor("MeinWert",    "Mein Wert",        ResultPropertyType.String),
    new ResultPropertyDescriptor("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
]),
```

### 4. Pipeline registrieren (`StepPipelineRegistry.cs`)

a) In `_map` (Voraussetzungen und Ausgabe angeben):

```csharp
[typeof(MeinStep)] = new(
    Prerequisites: [],          // Leer = keine Voraussetzungen
    Output:        "TaskResult" // oder Name deines neuen Ergebnistyps
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
    public sealed class MeinStepHandler : JobStepHandler<MeinStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            MeinStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            // Implementierung hier
            ctx.Logger.LogInformation("MeinStepHandler wird ausgeführt.");
            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
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
- [ ] Ergebnistyp (falls neu) in `StepResults.cs` definiert
- [ ] Eintrag in `StepResultMetadata.Properties` hinzugefügt
- [ ] Eintrag in `StepResultMetadata.FriendlyNames` hinzugefügt
- [ ] Neuen Ergebnistyp ggf. in `StepResultMetadata.ResultTypes` registriert
- [ ] `StepPipelineRegistry._map` aktualisiert
- [ ] `StepPipelineRegistry._nameMap` aktualisiert
- [ ] Handler-Klasse erstellt
- [ ] Handler in `JobExecutor._stepHandlers` registriert
- [ ] Frontend-Schritte aus `DesktopAutomationApp/README_neuen_step_hinzufuegen.md` abarbeiten
