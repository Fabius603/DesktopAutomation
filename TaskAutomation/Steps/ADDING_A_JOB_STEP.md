# Einen neuen Job-Step hinzufügen

Diese Anleitung beschreibt alle Stellen, die ein neuer `JobStep` im aktuellen
Projekt benötigt. Ein Step gilt erst als vollständig integriert, wenn
Serialisierung, Ausführung, Result-Vertrag, Backend-Validierung, Editor,
Lokalisierung, Release Notes und Tests umgesetzt sind.

Die Result-Verträge werden ergänzend in
[`RESULT_CONTRACTS.md`](RESULT_CONTRACTS.md) beschrieben.

## Frontendneutrale Step-Definitionen

Jeder neue Step beschreibt seine bearbeitbaren Felder und seine Darstellung über
eine `IStepDefinition` unter `TaskAutomation/Steps/Definitions/`.
Transportierbare Metadaten wie Feldtypen, Constraints, Editorabschnitte,
Zusammenfassung und Detailfelder liegen im plattformneutralen Projekt
`TaskAutomation.Contracts`.

Alle eingebauten Steps verwenden diesen Definitionspfad. Ihre WPF-Editoren
werden aus den Definitionen erzeugt; Laden, Bearbeiten, Validieren und Erstellen
laufen über einen frontendneutralen `StepDraft`. Felder, Abschnitte, Checkboxen,
Hinweise und eingeklappte erweiterte Einstellungen werden aus dem
Darstellungsvertrag erzeugt. Das persistierte Job-JSON und die Runtime-Handler
bleiben davon unabhängig.

Frontend-spezifische Komfortfunktionen werden über stabile `EditorHint`-Werte
angefordert. `monitor-picker`, `file-picker`, `directory-picker`, `file-or-folder-picker`, `camera-picker`, `visual-overlay`, `roi-picker`, `yolo-picker`, `condition-editor`, `windows-capability-picker`, `process-name-suggestions`,
`executable-path-suggestions`, `start-program-picker`, `macro-picker`,
`job-picker`, `process-target-picker`, `executable-process-target-picker`,
`result-binding-picker`, `screen-point-picker`, `user-choice-options`,
`point-entry-list`, `axis-expression-list`, `emoji-text` und `percentage`
beschreiben nur die gewünschte
Auswahlhilfe; WPF kann
dafür beispielsweise das Monitor-Overlay, einen Dateidialog, lokale
Vorschlagslisten, eine Auswahl vorhandener Bibliothekseinträge oder den Wechsel
zwischen Prozesssuche und vorherigem Prozessergebnis anbieten. Referenzen
tragen eine stabile ID und einen lesbaren Namen. Ein anderes Frontend darf eine
passende eigene Darstellung verwenden. Der persistierte fachliche Wert bleibt
davon unabhängig.

Ein neuer Step erhält keine eigenen Properties im `AddJobStepDialogViewModel`,
keinen eigenen Create-/Load-Switchzweig und kein eigenes WPF-Control. Komplexe
Eingaben werden als wiederverwendbare, frontendneutrale Editor-Hints
beschrieben. Nur der Adapter für einen neuen Editor-Hint ist
frontend-spezifisch; fachliche Werte und Validierung bleiben in `TaskAutomation`.

Zusammengesetzte Editor-Hints dürfen keine konkreten Step-Typen im Frontend
voraussetzen. `visual-overlay` deklariert deshalb seine Detection- und
Text-Input-Contracts sowie optionale Desktop-Platzierungsfunktionen über
`StepVisualOverlayEditorOptions`. Das Frontend erhält damit alle benötigten
Fähigkeiten aus dem Descriptor.

`roi-picker` beschreibt ueber `StepRoiPickerOptions` den Eingabevertrag einer
optionalen dynamischen ROI. Frontends koennen dazu eine statische Bereichsauswahl
und eine Capture-Hilfe anbieten. Dateifelder koennen ueber
`StepFilePickerOptions` den Dateityp und eine optionale Vorschau anfordern.
`yolo-picker` fordert eine zusammenhaengende Modell- und Klassenauswahl an und
kann ein Feld fuer die vom Modell empfohlene Konfidenz benennen.
`condition-editor` fordert einen Editor fuer eine beliebig kombinierbare Liste
von Bedingungen an. Das Frontend darf dafuer eine komfortable Ergebnis- und
Eigenschaftsauswahl anbieten; persistiert werden weiterhin nur stabile Step-,
Property- und Operatorwerte.
`windows-capability-picker` deklariert ueber
`StepWindowsCapabilityPickerOptions`, ob Windows-Zustaende abgefragt oder
Einstellungen geaendert werden. Die Capability-ID und ihre Parameter bleiben
frontendneutral; dynamische Geraete- und Profillisten sind reine Eingabehilfen.

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

Ein fester Step benötigt Laufzeit-Metadaten und einen Handler. Die UI- und
Konfigurations-Metadaten werden separat durch seine Step-Definition registriert.

### Pipeline-Metadaten

In `TaskAutomation/Steps/StepPipelineRegistry.cs`:

```csharp
[typeof(FileHashStep)] = new(
    ResultType: typeof(FileHashResult),
    IsConditionSource: true,
    DisplayName: "Datei-Hash berechnen"),
```

`IsConditionSource` wird aktiviert, wenn mindestens eine Result-Property in
If-/ElseIf-Bedingungen verwendet werden darf.

### Laufzeit-Handler

Den Handler in der `_stepHandlers`-Registry in
`TaskAutomation/Jobs/JobExecutor.cs` anlegen. Benötigt er Services, werden sie
aus den bereits in den `JobExecutor` injizierten Abhängigkeiten übergeben.

```csharp
{ typeof(FileHashStep), new FileHashStepHandler() },
```

Fehlt diese Registrierung, lässt sich der Step zwar speichern und anzeigen,
wird aber nicht ausgeführt.

Benötigt der Handler eine neue Laufzeitabhängigkeit, muss sie zusätzlich:

1. über den Konstruktor des `JobExecutor` beziehungsweise den passenden
   Pipeline-Kontext geführt werden,
2. in `DesktopAutomationApp/App.xaml.cs` für die Desktop-Anwendung registriert
   werden und
3. in Tests durch einen geeigneten Test-Double bereitgestellt werden.

Backend-Verträge dürfen dabei keine Abhängigkeit auf WPF oder
`DesktopAutomationApp` erhalten.

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

## 7. Step-Definition und Frontend integrieren

Für einen neuen einfachen Step sind normalerweise folgende Ergänzungen nötig:

1. Eine `StepDefinition<TStep>` unter `TaskAutomation/Steps/Definitions/`
   anlegen. Sie definiert stabile Feld-IDs, Feldtypen, Defaults, Constraints,
   Editorabschnitte, Zusammenfassung und Detailfelder.
2. `Read`, `Apply` und `ValidateDraft` implementieren. Allgemeine Regeln wie
   Pflichtfelder, Datentypen, Wertebereiche und erlaubte Auswahlwerte werden
   automatisch aus dem Descriptor validiert; `ValidateDraft` enthält nur
   zusätzliche fachliche Regeln.
3. Die Definition im `BuiltInStepDefinitions`-Katalog registrieren. Der Step
   erscheint dadurch in Auswahl, generischem Editor und Detaildarstellung.
4. Nur wenn eine Auswahlhilfe nötig ist, einen neutralen `EditorHint` im
   Contracts-Projekt ergänzen und im jeweiligen Frontend adaptieren.

Alle eingebauten Steps verwenden diesen Definitionspfad. Einen parallelen
statischen Editor-, Template- oder Typ-Switch-Pfad gibt es nicht mehr. Komplexe
Eingaben werden über wiederverwendbare Editor-Hints und frontend-spezifische
Adapter dargestellt.

Auswahlfelder werden als `StepValueKind.Enum` mit stabilen `Options` aus Wert
und Lokalisierungsschlüssel beschrieben. Mehrzeilige Texte und Farben verwenden
`MultilineText` beziehungsweise `Color`; das Frontend stellt dafür passende
Eingaben bereit. Abhängige Felder verwenden `VisibleWhen` für eine Regel oder
`VisibleWhenAll` für mehrere gemeinsam zu erfüllende Regeln. Eine
`StepVisibilityRule` kann mit `AnyOfValues` mehrere zulässige Werte desselben
Quellfelds beschreiben. Das Frontend wertet diese Regeln aus und blendet
beispielsweise Zieloptionen nur für Kopieren und Verschieben ein. Prozessziele verwenden
je nach manueller Eingabe `ProcessTargetPicker` oder
`ExecutableProcessTargetPicker`, ohne Windows-spezifische Picker in die
TaskAutomation-Definition zu ziehen.

Auswahlen mit eigenen untergeordneten Eingaben werden in `EditorNodes` als
`StepChoiceGroupDescriptor` beschrieben. Die Gruppe referenziert ein stabiles
Enum-Auswahlfeld und besitzt zwei oder mehr `StepChoiceBranchDescriptor`-Zweige.
Jeder Zweig enthält seinen stabilen Wert, einen lokalisierten Titel und beliebig
viele weitere Feld- oder Auswahlknoten. Dadurch können mehrere unabhängige
Gruppen und beliebig viele Auswahlwerte innerhalb eines Steps dargestellt
werden. `VisibleWhen` beziehungsweise `VisibleWhenAll` beschreibt nur zusätzliche
fachliche Bedingungen. Beim Umschalten dürfen inaktive Eingabewerte nicht
verworfen werden.

Allgemeine Result-Bindings verwenden `ResultBindingPicker` zusammen mit einer
stabilen `InputContractId`. Das Frontend ermittelt daraus den bereits im Backend
registrierten Eingabevertrag und bietet ausschließlich kompatible Ergebnisse an.

Ein einfacher Step erhält keine eigene `Show...`-Property, keine
duplizierten ViewModel-Felder, keine Create-/Load-Switchzweige und kein eigenes
WPF-Control. Ein spezialisierter Editor ist nur für zusammengesetzte Eingaben
wie Result-Bindings, interaktive ROI-Erfassung oder dynamische Collections
vorgesehen. Auch er soll fachliche Werte und Validierung aus derselben
Step-Definition beziehen.

`SummaryItems` werden direkt auf der kompakten Step-Karte dargestellt;
`DetailFieldIds` steuern die aufgeklappte Detailansicht. Beide Listen müssen
ausschließlich stabile Feld-IDs derselben Definition referenzieren.

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

## 9. Release Notes

Ein neuer Step ist eine sichtbare Funktion und benötigt deshalb einen kurzen
Eintrag in `DesktopAutomationApp/Resources/ReleaseNotes.json`:

- Die veröffentlichte Version immer aus Git `HEAD` lesen:
  `git show HEAD:DesktopAutomationApp/DesktopAutomationApp.csproj`.
- Für den neuen Eintrag genau die Patch-Komponente um eins erhöhen. Während
  normaler Entwicklungsarbeit die `<Version>` im Projekt nicht ändern.
- Je einen deutschen (`de`) und englischen (`en`) Text in einer bestehenden
  Kategorie `Added`, `Changed` oder `Fixed` ergänzen.
- Nur den wahrnehmbaren Nutzen beschreiben. Keine Handler, Registries,
  Verträge, Refactorings oder Tests erwähnen.
- Vor dem Ergänzen den gesamten unveröffentlichten Block prüfen und einen
  vorhandenen Eintrag zum selben Ergebnis erweitern, statt einen weiteren
  ähnlichen Bullet anzulegen.
- Die neue Version steht an erster Stelle; ältere Einträge bleiben unverändert.

Nach der Änderung muss die Datei als JSON parsebar sein. Die normale
Release-Build-Prüfung validiert zusätzlich die eingebettete Ressource.

## 10. Tests

Die folgenden Szenarien beschreiben die erforderliche Abdeckung des vollständigen
Steps. Sie verlangen weder einen eigenen Test pro Aufzählungspunkt oder Feld noch
eine Wiederholung bereits vorhandener Abdeckung. Generische Katalog-,
Serialisierungs-, Metadaten- und Binding-Tests sollen erweitert oder
parametrisiert werden, wenn sie den jeweiligen Vertrag bereits prüfen.

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

## 11. Abschlussprüfung

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
- [ ] Neue Laufzeitabhängigkeiten durch Konstruktor/Pipeline geführt und in der
      Desktop-DI registriert
- [ ] Eingabeverträge und `ResultBindingResolver` verwendet
- [ ] Backend-Validierung ergänzt
- [ ] `IStepDefinition` im `BuiltInStepDefinitions`-Katalog registriert
- [ ] Generischer Editor, Zusammenfassung und Detaildarstellung vollständig beschrieben
- [ ] Deutsche und englische Ressourcen ergänzt
- [ ] Zweisprachige Release Notes in der nächsten Patch-Version ergänzt oder
      mit einem passenden Eintrag zusammengeführt
- [ ] Serialisierungs-, Handler-, Vertrags- und Validierungstests ergänzt
- [ ] Testprojekt und WPF-App erfolgreich validiert
