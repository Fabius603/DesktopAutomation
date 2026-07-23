# Einen neuen Job-Step hinzufügen

Diese Anleitung beschreibt alle Stellen, die ein neuer `JobStep` im aktuellen
Projekt benötigt. Ein Step gilt erst als vollständig integriert, wenn
Serialisierung, Ausführung, Result-Vertrag, Backend-Validierung, Editor,
Lokalisierung und Tests umgesetzt sind.

Die Result-Verträge werden ergänzend in
[`RESULT_CONTRACTS.md`](RESULT_CONTRACTS.md) beschrieben.

## 1. Step und Settings modellieren

Der persistierte Step gehört nach `TaskAutomation/Jobs/StepData.cs`.

```csharp
public sealed class FileHashStep : JobStep
{
    [JsonPropertyName("settings")]
    public FileHashSettings Settings { get; set; } = new();
}

public sealed class FileHashSettings
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}
```

Für persistierte Properties gelten folgende Regeln:

- Explizite `JsonPropertyName`-Attribute verwenden.
- Sinnvolle Defaults setzen, damit alte oder unvollständige Job-Dateien
  weiterhin geladen werden können.
- Enums mit `JsonStringEnumConverter` als stabile Namen speichern.
- Persistierte Namen nicht nachträglich ändern. Falls eine Umbenennung nötig
  ist, muss eine Abwärtskompatibilität oder Migration ergänzt werden.

Anschließend den Typ an `JobStep` registrieren:

```csharp
[JsonDerivedType(typeof(FileHashStep), "file_hash")]
public abstract class JobStep
```

Der Discriminator wie `file_hash` ist Bestandteil des Dateiformats und darf
nach der Veröffentlichung nicht mehr geändert oder wiederverwendet werden.

## 2. Result-Typ definieren

Jeder ausführbare Step gibt genau ein von `StepResultBase` abgeleitetes Objekt
zurück. Dieses Objekt darf mehrere auswählbare Werte enthalten.

```csharp
public sealed record FileHashResult : StepResultBase
{
    [ResultProperty("hash")]
    public string Hash { get; init; } = string.Empty;

    [ResultProperty("file_size")]
    public long FileSize { get; init; }

    [ResultProperty("calculated_at")]
    public DateTime CalculatedAt { get; init; }

    public static readonly FileHashResult Default = new();
}
```

Für Result-Properties gelten folgende Regeln:

- Jede für andere Steps oder Bedingungen auswählbare Property benötigt ein
  explizites `[ResultProperty("stabile_id")]`.
- Die ID wird in Jobs persistiert. Sie bleibt deshalb auch dann gleich, wenn
  der C#-Propertyname später geändert wird.
- IDs innerhalb eines Result-Vertrags müssen eindeutig sein.
- Technische Werte, die nicht auswählbar sein sollen, erhalten
  `[ResultHidden]`.
- Der Datentyp wird normalerweise aus dem CLR-Typ abgeleitet. Nur bei einer
  bewusst abweichenden Semantik wird `DataType = ResultValueKind...` gesetzt.
- Verschachtelte Objekte und Collection-Elemente benötigen ebenfalls
  annotierte Properties. Für Collections stellt das Metadatenmodell zusätzlich
  `Count` bereit.
- Neue Datentypen müssen zentral in `ResultValueKind`, Metadatenableitung,
  Kompatibilitätsregeln, UI-Darstellung und Tests ergänzt werden.

Stabile IDs sollten `snake_case` verwenden und die fachliche Bedeutung
beschreiben. Eine vorhandene ID wird niemals für eine andere Bedeutung
recycelt.

## 3. Handler implementieren

Steps mit einem festen Result-Typ erben von
`JobStepHandler<TStep, TResult>`.

```csharp
public sealed class FileHashStepHandler
    : JobStepHandler<FileHashStep, FileHashResult>
{
    protected override async Task<FileHashResult> ExecuteCoreAsync(
        FileHashStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(step.Settings.Path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);

        return new FileHashResult
        {
            WasExecuted = true,
            Hash = Convert.ToHexString(hash),
            FileSize = stream.Length,
            CalculatedAt = DateTime.UtcNow
        };
    }

    protected override FileHashResult CreateDefault() => FileHashResult.Default;
}
```

Die Basisklasse:

- prüft den Step-Typ,
- ruft `ExecuteCoreAsync` auf,
- speichert das Result automatisch unter der Step-ID im `IJobResultStore`.

Der Handler soll deshalb nicht selbst am Result-Store vorbeischreiben.
Abhängigkeiten werden über den Konstruktor eingebracht. Fehler sollen entweder
als fachlich definiertes Result modelliert oder als Exception ausgelöst werden;
ein Fehler darf nicht als normaler `false`-Wert getarnt werden.

## 4. Backend registrieren

Ein fester Step benötigt derzeit zwei Registrierungen.

### Pipeline-Metadaten

In `TaskAutomation/Steps/StepPipelineRegistry.cs`:

```csharp
[typeof(FileHashStep)] = new(
    Prerequisites: [],
    ResultType: typeof(FileHashResult),
    IsConditionSource: true,
    DisplayName: "Datei-Hash berechnen"),
```

`IsConditionSource` wird aktiviert, wenn mindestens eine Result-Property in
If-/ElseIf-Bedingungen verwendet werden darf.

Falls das String-Mapping `GetByName` verwendet wird, muss zusätzlich ein
Eintrag in `_nameMap` ergänzt werden:

```csharp
["FileHash"] = typeof(FileHashStep),
```

### Laufzeit-Handler

Den Handler in der `_stepHandlers`-Registry in
`TaskAutomation/Jobs/JobExecutor.cs` anlegen. Benötigt er Services, werden sie
aus den bereits in den `JobExecutor` injizierten Abhängigkeiten übergeben.

```csharp
{ typeof(FileHashStep), new FileHashStepHandler() },
```

Fehlt diese Registrierung, lässt sich der Step zwar speichern und anzeigen,
wird aber nicht ausgeführt.

## 5. Eingaben aus vorherigen Results

Wenn ein Step ein Ergebnis eines vorherigen Steps konsumiert:

1. Eine `ResultBinding`-Property in seinen Settings anlegen.
2. Einen Backend-Vertrag in
   `TaskAutomation/Steps/StepInputContractRegistry.cs` registrieren.
3. Im Handler ausschließlich über `ResultBindingResolver` auflösen.
4. Im Frontend einen `ResultBindingPickerViewModel` mit demselben Contract-Key
   verwenden.

Beispiel für einen verpflichtenden einzelnen Textwert:

```csharp
[typeof(MyConsumerStep)] =
[
    Required(
        "input",
        CollectionConsumptionMode.NotApplicable,
        new AcceptedResultShape(
            ResultValueKind.Text,
            ResultCardinality.Single,
            ResultCardinality.OptionalSingle))
],
```

Auflösung im Handler:

```csharp
var resolved = ResultBindingResolver.Resolve<string>(
    context.Results,
    step.Settings.Input);

if (!resolved.IsSuccess)
    throw new InvalidOperationException(resolved.Error);
```

Typen, Kardinalitäten, Pflichtangabe, Verhalten bei fehlenden Werten und
Collection-Verarbeitung werden im Backend-Vertrag festgelegt. Das Frontend
filtert nur anhand dieses Vertrags und erfindet keine eigenen
Kompatibilitätsregeln.

## 6. Konfigurationsabhängige Result-Typen

Dieser Weg ist nur nötig, wenn verschiedene Konfigurationen desselben Steps
unterschiedliche Result-Schemas besitzen. Der WindowsStatus-Step ist das
Referenzbeispiel.

Für jede fachlich unterschiedliche Variante wird ein eigener konkreter
Result-Record erstellt:

```csharp
public sealed record TextQueryResult : QueryResultBase
{
    [ResultProperty("text")]
    public string Text { get; init; } = string.Empty;
}

public sealed record NumberQueryResult : QueryResultBase
{
    [ResultProperty("number")]
    public double Number { get; init; }
}
```

Danach:

1. Einen `IStepResultContractProvider` implementieren, der anhand des vollständig
   konfigurierten Steps den konkreten Vertrag zurückgibt.
2. Den Provider in `StepResultContractRegistry.DynamicProviders` registrieren.
3. Den Handler von `DynamicJobStepHandler<TStep>` ableiten.
4. Zur Laufzeit genau den vom Provider angekündigten konkreten Result-Typ
   zurückgeben.

`DynamicJobStepHandler` prüft den tatsächlichen Rückgabetyp gegen den
konfigurierten Vertrag und bricht bei einer Abweichung ab.

Die Basisklasse eines dynamischen Result-Typs darf nur wirklich gemeinsame
Properties enthalten. Variantenabhängige Felder gehören in die konkreten
Records, nicht in ein allgemeines Union- oder Snapshot-Objekt.

## 7. WPF-Editor integrieren

Der aktuelle Step-Dialog wird in
`DesktopAutomationApp/ViewModels/Jobs/AddJobStepDialogViewModel.cs` und
`DesktopAutomationApp/Views/JobsView/AddJobStepDialog.xaml` zusammengesetzt.

Für einen neuen Step sind normalerweise folgende Ergänzungen nötig:

1. Einen Eintrag in `CreateStepTypeItems` mit stabilem UI-Namen, Kategorie und
   Beschreibung ergänzen.
2. Eine `Show...`-Property für die Sichtbarkeit des Editors anlegen.
3. Editierbare ViewModel-Properties mit Defaults ergänzen.
4. Beim Bearbeiten eines vorhandenen Steps dessen Settings in das ViewModel
   laden.
5. Die lokale Eingabeprüfung in `IsInputValid` ergänzen.
6. In `CreateStep` den neuen Step samt Settings erzeugen.
7. Bei Result-Eingaben einen Picker mit dem Backend-Contract-Key erzeugen.
8. Ein fokussiertes Editor-Control unter
   `DesktopAutomationApp/Controls/Jobs/Editors/<Kategorie>/` anlegen.
9. Das Editor-Control in `AddJobStepDialog.xaml` einfügen.
10. Falls nötig die kompakte Detaildarstellung in
    `DesktopAutomationApp/Services/Jobs/JobStepDetailsProvider.cs` ergänzen.

Die endgültige fachliche Prüfung gehört immer zusätzlich in
`TaskAutomation/Jobs/JobValidation.cs`. Eine reine WPF-Validierung reicht
nicht, weil Jobs auch aus JSON oder anderen Aufrufern stammen können.

## 8. Lokalisierung

Alle sichtbaren Texte müssen in beiden Dateien vorhanden sein:

- `DesktopAutomationApp/Resources/Strings.resx`
- `DesktopAutomationApp/Resources/Strings.en.resx`

Mindestens üblich sind:

```text
Step.Type.FileHash
Step.Description.FileHash
Step.ResultProperty.Hash
Step.ResultProperty.FileSize
Step.ResultProperty.CalculatedAt
```

Zusätzliche Feldbezeichnungen, Auswahlwerte und Fehlermeldungen erhalten
ebenfalls lokalisierte Schlüssel. In XAML wird `loc:Translate` verwendet; in
C# `Loc.Get`, `Loc.Format` oder `StepLocalization`.

Die stabile Result-ID und der Lokalisierungsschlüssel sind unterschiedliche
Konzepte: Die ID wird persistiert, die Übersetzung darf geändert werden.

## 9. Tests

Für einen neuen Step sollen mindestens folgende Szenarien abgedeckt werden:

- JSON-Roundtrip inklusive Discriminator und Settings.
- Handler-Erfolg mit vollständigem Result.
- Abbruch über `CancellationToken`, falls der Handler wartet oder I/O ausführt.
- Fehler- und ungültige Eingabefälle.
- Speicherung des Results unter der konkreten Step-ID.
- Alle auswählbaren Result-Properties besitzen eindeutige stabile IDs.
- Result-Bindings werden nur für kompatible Typen und Kardinalitäten akzeptiert.
- Fehlende Pflicht-Bindings werden vom Backend abgelehnt.
- Ein dynamischer Step liefert für jede Konfiguration den passenden konkreten
  Vertrag und Result-Typ.
- UI-Erzeugung und Bearbeitung verlieren keine Settings.

Der bestehende Test
`StepResultMetadataTests.EveryResultContract_HasUniqueStablePropertyIds`
prüft automatisch alle bekannten Result-Verträge. Fehlende
`ResultProperty`-Attribute führen bereits beim Aufbau der Metadaten zu einem
Fehler.

## 10. Abschlussprüfung

Für Änderungen an Backend und UI werden beide Prüfungen ausgeführt:

```powershell
dotnet test tests\TaskAutomation.Tests\TaskAutomation.Tests.csproj `
  --configuration Release --no-restore

dotnet build DesktopAutomationApp\DesktopAutomationApp.csproj `
  --configuration Release --no-restore
```

Abschließende Checkliste:

- [ ] `JobStep` und Settings mit stabilen JSON-Namen angelegt
- [ ] `JsonDerivedType` mit stabilem Discriminator registriert
- [ ] Result-Record mit expliziten stabilen Property-IDs angelegt
- [ ] Fester oder dynamischer Handler implementiert
- [ ] `StepPipelineRegistry` ergänzt
- [ ] Handler im `JobExecutor` registriert
- [ ] Eingabeverträge und `ResultBindingResolver` verwendet
- [ ] Backend-Validierung ergänzt
- [ ] Step-Auswahl, ViewModel und Editor vollständig integriert
- [ ] Deutsche und englische Ressourcen ergänzt
- [ ] Serialisierungs-, Handler-, Vertrags- und Validierungstests ergänzt
- [ ] Testprojekt und WPF-App erfolgreich validiert
