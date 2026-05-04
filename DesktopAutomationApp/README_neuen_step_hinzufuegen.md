# Neuen Job-Step hinzufügen – Frontend (DesktopAutomationApp)

Diese Anleitung beschreibt alle Stellen im Frontend, die angepasst werden müssen, wenn ein neuer Step-Typ hinzugefügt wird.

> Voraussetzung: Die Backend-Schritte aus `TaskAutomation/Steps/README_neuen_step_hinzufuegen.md` wurden abgearbeitet.

---

## Übersicht der beteiligten Dateien

| Datei | Aufgabe |
|---|---|
| `ViewModels/Jobs/AddJobStepDialogViewModel.cs` | Properties, Validierung, Fabrik-Methode, Beschreibungstext |
| `Views/Jobs/AddJobStepDialog.xaml` | Konfigurationspanel für den neuen Step |
| `ViewModels/Jobs/JobStepsViewModel.cs` | `Prefill`-Methode (Felder beim Bearbeiten vorausfüllen) |

---

## Schritt-für-Schritt-Anleitung

### 1. `AddJobStepDialogViewModel.cs`

#### a) Show-Property hinzufügen

Im Abschnitt der `ShowXxx`-Properties:

```csharp
public bool ShowMeinStep => SelectedType == "MeinStep";
```

#### b) `OnChange`-Benachrichtigung in `SelectedType.set` eintragen

```csharp
OnChange(nameof(ShowMeinStep));
```

#### c) Eintrag in `CreateStepTypeItems()`

Wähle eine passende Kategorie (z. B. `"Automatisierung"`, `"Erfassung"`, `"Erkennung"`, `"Interaktion"`, `"Ausgabe"`, `"Ablaufsteuerung"`):

```csharp
new("MeinStep", "Automatisierung", "Mein Step (Anzeigename)"),
```

#### d) Beschreibungstext in `StepTypeDescription`

```csharp
"MeinStep" => "Kurze Beschreibung, was dieser Step macht.",
```

#### e) `CanConfirm`-Validierung

```csharp
"MeinStep" => !string.IsNullOrWhiteSpace(MeinStep_MeinParameter),
```

#### f) Felder als Properties deklarieren

```csharp
// ===== MeinStep Felder =====
private string _meinStep_MeinParameter = string.Empty;
public string MeinStep_MeinParameter
{
    get => _meinStep_MeinParameter;
    set { _meinStep_MeinParameter = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
}
```

#### g) Fabrik-Methode `CreateStep()`

```csharp
"MeinStep" => new MeinStep
{
    Settings = new MeinStepSettings
    {
        MeinParameter = MeinStep_MeinParameter
    }
},
```

#### h) Optionale Commands

Falls der Step einen Datei-Dialog oder einen anderen Hilfsbefehl benötigt:

1. `ICommand`-Property deklarieren:
   ```csharp
   public ICommand BrowseMeinParameterCommand { get; }
   ```
2. Im Konstruktor initialisieren:
   ```csharp
   BrowseMeinParameterCommand = new RelayCommand(BrowseMeinParameter);
   ```
3. Private Methode implementieren.

---

### 2. `AddJobStepDialog.xaml`

Füge innerhalb des `<StackPanel>` für dynamische Felder (nach dem letzten vorhandenen Step-Panel) ein neues Panel ein:

```xml
<!-- ── MeinStep ── -->
<StackPanel Visibility="{Binding ShowMeinStep, Converter={StaticResource BooleanToVisibilityConverter}}">
    <Grid Margin="0,0,0,8">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="140"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Column="0" Text="Mein Parameter" VerticalAlignment="Center"
                   Foreground="{StaticResource App.Brush.TextSecondary}"/>
        <TextBox Grid.Column="1"
                 Text="{Binding MeinStep_MeinParameter, UpdateSourceTrigger=PropertyChanged}"/>
    </Grid>
</StackPanel>
```

Konventionen:
- Beschriftungs-Spalte: `Width="140"`
- Abstände zwischen Zeilen: `Margin="0,0,0,8"`
- Brush für Labels: `{StaticResource App.Brush.TextSecondary}`
- Wasserzeichen-Text: `mah:TextBoxHelper.Watermark="…"`

---

### 3. `JobStepsViewModel.cs` – `Prefill`-Methode

Damit Felder beim **Bearbeiten** eines vorhandenen Steps vorausgefüllt werden, muss der `switch`-Block in der `Prefill`-Methode ergänzt werden:

```csharp
case MeinStep ms:
    vm.SelectedType = "MeinStep";
    vm.MeinStep_MeinParameter = ms.Settings.MeinParameter;
    break;
```

---

## Checkliste

- [ ] `ShowMeinStep`-Property in `AddJobStepDialogViewModel.cs` hinzugefügt
- [ ] `OnChange(nameof(ShowMeinStep))` in `SelectedType.set` eingetragen
- [ ] Eintrag in `CreateStepTypeItems()` hinzugefügt
- [ ] Beschreibungstext in `StepTypeDescription` eingetragen
- [ ] `CanConfirm`-Fall hinzugefügt
- [ ] Felder als Properties deklariert
- [ ] `CreateStep()`-Fall hinzugefügt
- [ ] XAML-Panel in `AddJobStepDialog.xaml` hinzugefügt
- [ ] `Prefill`-Fall in `JobStepsViewModel.cs` hinzugefügt
