## Steps: Prozess starten

ID: start_process
Website: /doku/steps/start_process/

Abbildung: [Notepad als Beispielprogramm: ausführbare Datei und Startoptionen im Editor. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/step-programmstart.detail.png)

1. Programmpfad: Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.
2. Auf Beendigung warten: Mit Häkchen wartet der Job auf das Ende des gestarteten Programms. Ohne Häkchen folgt der nächste Step nach dem Start; für einen geöffneten Editor ist das meist die passende Wahl.
3. Argumente: Zusätzliche Startargumente des Programms, etwa der Pfad zu einer Textdatei für den Editor. Pfade mit Leerzeichen in Anführungszeichen setzen. Leer lassen, wenn keine Argumente nötig sind.
4. Arbeitsverzeichnis: Arbeitsverzeichnis des gestarteten Programms. Relative Pfade des Programms können davon abhängen.
5. Monitor: Nullbasierter Monitor für Programmfenster oder Anzeige. Er bezieht sich auf die aktuelle Bildschirmkonfiguration.
6. Fenstermodus: Fensterdarstellung: Voreinstellung des Programms, Normal oder Maximiert, soweit der jeweilige Step diese Option unterstützt.
7. Position: „Zentriert“ positioniert das Programmfenster mittig auf dem gewählten Monitor; „Benutzerdefiniert“ verwendet die konfigurierten Versätze.
8. Versatz X (px): Bei Position „Benutzerdefiniert“: Abstand in Pixeln vom linken Rand des Arbeitsbereichs des gewählten Monitors zur linken Fensterkante. Beispiel 120 setzt das Fenster 120 px nach rechts.
9. Versatz Y (px): Bei Position „Benutzerdefiniert“: Abstand in Pixeln vom oberen Rand des Arbeitsbereichs des gewählten Monitors zur oberen Fensterkante. Beispiel 80 setzt das Fenster 80 px nach unten.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Der Beispieljob „Arbeitsplatz nach Zeitplan“ startet notepad nur bei Bedarf und öffnet anschließend calc.

### field.action

Wähle „Starten“, um ein Programm über seinen Pfad zu öffnen, oder „Beenden“, um die ausgewählte Prozessreferenz zu schließen. Es werden nur die Felder der gewählten Aktion eingeblendet.

### field.action.option.Start

Startet das konfigurierte Programm.

### field.action.option.Terminate

Beendet die gewählte Programminstanz beziehungsweise die unterstützte Zielsuche.

### field.arguments

Zusätzliche Startargumente des Programms, etwa der Pfad zu einer Textdatei für den Editor. Pfade mit Leerzeichen in Anführungszeichen setzen. Leer lassen, wenn keine Argumente nötig sind.

### field.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### field.monitor_index

Nullbasierter Monitor für Programmfenster oder Anzeige. Er bezieht sich auf die aktuelle Bildschirmkonfiguration.

### field.offset_x

Bei Position „Benutzerdefiniert“: Abstand in Pixeln vom linken Rand des Arbeitsbereichs des gewählten Monitors zur linken Fensterkante. Beispiel 120 setzt das Fenster 120 px nach rechts.

### field.offset_y

Bei Position „Benutzerdefiniert“: Abstand in Pixeln vom oberen Rand des Arbeitsbereichs des gewählten Monitors zur oberen Fensterkante. Beispiel 80 setzt das Fenster 80 px nach unten.

### field.placement_mode

„Zentriert“ positioniert das Programmfenster mittig auf dem gewählten Monitor; „Benutzerdefiniert“ verwendet die konfigurierten Versätze.

### field.placement_mode.option.Centered

Positioniert das Fenster mittig auf dem gewählten Monitor.

### field.placement_mode.option.Custom

Verwendet die angegebenen Positionsversätze.

### field.process_target

Im Editor eine kompatible Prozessreferenz wählen. Suchkriterien einer älteren settings.target-Konfiguration sind kein Ersatz für eine beliebige Textquelle im typisierten Prozesseingang.

### field.wait_for_exit

Mit Häkchen wartet der Job auf das Ende des gestarteten Programms. Ohne Häkchen folgt der nächste Step nach dem Start; für einen geöffneten Editor ist das meist die passende Wahl.

### field.window_mode

Fensterdarstellung: Voreinstellung des Programms, Normal oder Maximiert, soweit der jeweilige Step diese Option unterstützt.

### field.window_mode.option.ApplicationDefault

Überlässt den anfänglichen Fenstermodus dem Programm.

### field.window_mode.option.Maximized

Maximiert das Fenster, soweit es dies unterstützt.

### field.window_mode.option.Normal

Verwendet ein normales, nicht maximiertes Fenster.

### field.working_directory

Arbeitsverzeichnis des gestarteten Programms. Relative Pfade des Programms können davon abhängen.

### input.process

konkrete Prozessreferenz. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Starte ein Programm über seinen ausführbaren Pfad. Argumente und Arbeitsverzeichnis bestimmen den Startkontext. Fenstermodus und Position werden angewendet, soweit das Programm ein steuerbares Fenster bereitstellt. Zum Beenden eines Programms verwende den Step „Prozess beenden“.

### result.process

Konkrete Prozessreferenz mit Identität und optionalen Programmdaten. Bei keinem Treffer kann sie fehlen; nicht nur process_name als Instanzidentität verwenden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.process.executable_path

Ermittelter Pfad des Programms. Bei fehlenden Rechten oder fehlender Information kann ein optionaler Wert leer sein. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.process.process_id

Windows-Prozess-ID. PIDs können später wiederverwendet werden; die Prozessreferenz ergänzt daher gegebenenfalls Startzeit und weitere Merkmale. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.process.process_name

Vom System ermittelter Prozessname, nicht notwendigerweise der sichtbare Fenstertitel. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.process.start_time_utc

UTC-Startzeit der Prozessinstanz zur Unterscheidung wiederverwendeter PIDs. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.process.window_handle

Native Fensterkennung der Programminstanz. Sie gilt im laufenden System und ist kein stabiler Dateipfad oder dauerhafter Job-Bezeichner. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### schema.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### schema.inputs

Zuordnung von UI-Feld-ID zu typisierter Wertequelle. Bei vorhandenen inputs serialisiert der kanonische Job-Serializer settings nicht zusätzlich. Leere inputs mit settings bleiben als Legacy-/direkte Konfiguration lesbar.

### schema.inputs.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.inputs.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.inputs.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.inputs.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.inputs.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.inputs.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.inputs.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.inputs.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.inputs.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.is_enabled

true führt den Step aus; false deaktiviert ihn. Blockmarker können in der UI nicht beliebig deaktiviert werden, weil ihre Struktur erhalten bleiben muss.

### schema.settings

Direkte beziehungsweise ältere Step-Konfiguration. Für neue referenzbasierte Dateien inputs und localValues verwenden; die UI migriert gespeicherte Literalwerte in lokale Quellen.

### schema.settings.action

Wähle „Starten“, um ein Programm über seinen Pfad zu öffnen, oder „Beenden“, um die ausgewählte Prozessreferenz zu schließen. Es werden nur die Felder der gewählten Aktion eingeblendet.

### schema.settings.action.option.Start

Startet das konfigurierte Programm.

### schema.settings.action.option.Terminate

Beendet die gewählte Programminstanz beziehungsweise die unterstützte Zielsuche.

### schema.settings.arguments

Zusätzliche Startargumente des Programms, etwa der Pfad zu einer Textdatei für den Editor. Pfade mit Leerzeichen in Anführungszeichen setzen. Leer lassen, wenn keine Argumente nötig sind.

### schema.settings.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### schema.settings.monitor_index

Nullbasierter Monitor für Programmfenster oder Anzeige. Er bezieht sich auf die aktuelle Bildschirmkonfiguration.

### schema.settings.offset_x

Bei Position „Benutzerdefiniert“: Abstand in Pixeln vom linken Rand des Arbeitsbereichs des gewählten Monitors zur linken Fensterkante. Beispiel 120 setzt das Fenster 120 px nach rechts.

### schema.settings.offset_y

Bei Position „Benutzerdefiniert“: Abstand in Pixeln vom oberen Rand des Arbeitsbereichs des gewählten Monitors zur oberen Fensterkante. Beispiel 80 setzt das Fenster 80 px nach unten.

### schema.settings.placement_mode

„Zentriert“ positioniert das Programmfenster mittig auf dem gewählten Monitor; „Benutzerdefiniert“ verwendet die konfigurierten Versätze.

### schema.settings.placement_mode.option.Centered

Positioniert das Fenster mittig auf dem gewählten Monitor.

### schema.settings.placement_mode.option.Custom

Verwendet die angegebenen Positionsversätze.

### schema.settings.target

Prozess-Zielkonfiguration aus Name/Pfad/Titel oder einer konkreten Prozessreferenz. Die konkrete Referenz schützt vor Verwechslung gleichnamiger Instanzen.

### schema.settings.target.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### schema.settings.target.process_name

Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein.

### schema.settings.target.process_source

Referenz auf eine konkrete laufende Programminstanz, üblicherweise process aus get_process oder start_process.

### schema.settings.target.process_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.target.process_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.target.process_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.target.process_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.target.process_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.target.process_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.target.process_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.target.process_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.target.process_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.target.window_title_contains

Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht.

### schema.settings.wait_for_exit

Mit Häkchen wartet der Job auf das Ende des gestarteten Programms. Ohne Häkchen folgt der nächste Step nach dem Start; für einen geöffneten Editor ist das meist die passende Wahl.

### schema.settings.window_mode

Fensterdarstellung: Voreinstellung des Programms, Normal oder Maximiert, soweit der jeweilige Step diese Option unterstützt.

### schema.settings.window_mode.option.ApplicationDefault

Überlässt den anfänglichen Fenstermodus dem Programm.

### schema.settings.window_mode.option.Maximized

Maximiert das Fenster, soweit es dies unterstützt.

### schema.settings.window_mode.option.Normal

Verwendet ein normales, nicht maximiertes Fenster.

### schema.settings.working_directory

Arbeitsverzeichnis des gestarteten Programms. Relative Pfade des Programms können davon abhängen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "start_process",
  "name": "Prozess starten",
  "description": "Startet ein Programm und konfiguriert optional dessen Fenster.",
  "category": "ProgrammeFenster",
  "uiFieldIds": [
    "executable_path",
    "wait_for_exit",
    "arguments",
    "working_directory",
    "monitor_index",
    "window_mode",
    "placement_mode",
    "offset_x",
    "offset_y"
  ],
  "fields": [
    {
      "id": "action",
      "name": "Aktion",
      "descriptor": {
        "Id": "action",
        "LabelKey": "Ui.Step.Settings.Action",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "Start",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "Start",
            "Terminate"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": -2,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "Start",
            "LabelKey": "Enum.StartProcessAction.Start",
            "DisplayName": null
          },
          {
            "Value": "Terminate",
            "LabelKey": "Enum.StartProcessAction.Terminate",
            "DisplayName": null
          }
        ],
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "process_target",
      "name": "Prozessreferenz",
      "descriptor": {
        "Id": "process_target",
        "LabelKey": "Ui.Step.Settings.ProcessSource",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "process-target-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": -1,
        "VisibleWhen": {
          "FieldId": "action",
          "EqualsValue": "Terminate",
          "AnyOfValues": null
        },
        "Options": null,
        "InputContractId": "process",
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "executable_path",
      "name": "Programmpfad",
      "descriptor": {
        "Id": "executable_path",
        "LabelKey": "Ui.Step.Settings.PathProgram",
        "ValueKind": "FilePath",
        "Required": true,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "start-program-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
        "VisibleWhen": {
          "FieldId": "action",
          "EqualsValue": "Start",
          "AnyOfValues": null
        },
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "wait_for_exit",
      "name": "Auf Beendigung warten",
      "descriptor": {
        "Id": "wait_for_exit",
        "LabelKey": "Ui.Step.Settings.WaitForCompletion",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": false,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 1,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "arguments",
      "name": "Argumente",
      "descriptor": {
        "Id": "arguments",
        "LabelKey": "Ui.Step.Settings.Arguments",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 2,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "working_directory",
      "name": "Arbeitsverzeichnis",
      "descriptor": {
        "Id": "working_directory",
        "LabelKey": "Ui.Step.Settings.WorkingDirectory",
        "ValueKind": "DirectoryPath",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 3,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "monitor_index",
      "name": "Monitor",
      "descriptor": {
        "Id": "monitor_index",
        "LabelKey": "Ui.Step.Settings.Monitor",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": "monitor-picker",
        "Constraints": {
          "Minimum": 0,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": true,
        "Order": 4,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "window_mode",
      "name": "Fenstermodus",
      "descriptor": {
        "Id": "window_mode",
        "LabelKey": "Ui.Step.Settings.WindowMode",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "ApplicationDefault",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "ApplicationDefault",
            "Normal",
            "Maximized"
          ]
        },
        "Width": "Full",
        "Advanced": true,
        "Order": 5,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "ApplicationDefault",
            "LabelKey": "Enum.StartProcessWindowMode.ApplicationDefault",
            "DisplayName": null
          },
          {
            "Value": "Normal",
            "LabelKey": "Enum.StartProcessWindowMode.Normal",
            "DisplayName": null
          },
          {
            "Value": "Maximized",
            "LabelKey": "Enum.StartProcessWindowMode.Maximized",
            "DisplayName": null
          }
        ],
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "placement_mode",
      "name": "Position",
      "descriptor": {
        "Id": "placement_mode",
        "LabelKey": "Ui.Step.Settings.Position",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "Centered",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "Centered",
            "Custom"
          ]
        },
        "Width": "Full",
        "Advanced": true,
        "Order": 6,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "Centered",
            "LabelKey": "Enum.StartProcessPlacementMode.Centered",
            "DisplayName": null
          },
          {
            "Value": "Custom",
            "LabelKey": "Enum.StartProcessPlacementMode.Custom",
            "DisplayName": null
          }
        ],
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "offset_x",
      "name": "Versatz X (px)",
      "descriptor": {
        "Id": "offset_x",
        "LabelKey": "Ui.Step.Settings.XOffsetPixels",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 7,
        "VisibleWhen": {
          "FieldId": "placement_mode",
          "EqualsValue": "Custom",
          "AnyOfValues": null
        },
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "offset_y",
      "name": "Versatz Y (px)",
      "descriptor": {
        "Id": "offset_y",
        "LabelKey": "Ui.Step.Settings.YOffsetPixels",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 8,
        "VisibleWhen": {
          "FieldId": "placement_mode",
          "EqualsValue": "Custom",
          "AnyOfValues": null
        },
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null
      }
    }
  ],
  "schema": [
    {
      "id": "id",
      "name": "id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs",
      "name": "inputs",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "inputs.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "is_enabled",
      "name": "is_enabled",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    },
    {
      "id": "settings",
      "name": "settings",
      "type": "StartProcessSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.action",
      "name": "action",
      "type": "StartProcessAction",
      "options": [
        "Start",
        "Terminate"
      ],
      "defaultValue": "Start"
    },
    {
      "id": "settings.arguments",
      "name": "arguments",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.executable_path",
      "name": "executable_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.monitor_index",
      "name": "monitor_index",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.offset_x",
      "name": "offset_x",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.offset_y",
      "name": "offset_y",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.placement_mode",
      "name": "placement_mode",
      "type": "StartProcessPlacementMode",
      "options": [
        "Centered",
        "Custom"
      ],
      "defaultValue": "Centered"
    },
    {
      "id": "settings.target",
      "name": "target",
      "type": "ProcessTargetSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.executable_path",
      "name": "executable_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_name",
      "name": "process_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_source",
      "name": "process_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.window_title_contains",
      "name": "window_title_contains",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.wait_for_exit",
      "name": "wait_for_exit",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
    },
    {
      "id": "settings.window_mode",
      "name": "window_mode",
      "type": "StartProcessWindowMode",
      "options": [
        "ApplicationDefault",
        "Normal",
        "Maximized"
      ],
      "defaultValue": "ApplicationDefault"
    },
    {
      "id": "settings.working_directory",
      "name": "working_directory",
      "type": "String",
      "options": [],
      "defaultValue": ""
    }
  ],
  "inputs": [
    {
      "Key": "process",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "NotApplicable",
      "AcceptedShapes": [
        {
          "ValueKind": "ProcessReference",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        }
      ],
      "AllowedProviderIds": [
        "job_variable",
        "step_result"
      ],
      "LegacyAllowedProviderIds": [],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "StartProcessResult",
    "DisplayName": "StartProcessResult",
    "Properties": [
      {
        "Name": "Process",
        "DisplayName": "Process",
        "DataType": "ProcessReference",
        "Description": "ProcessReference, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process",
        "EnumDisplayNames": null,
        "StableId": "process"
      },
      {
        "Name": "Process.ProcessId",
        "DisplayName": "Process / Process Id",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.process_id",
        "EnumDisplayNames": null,
        "StableId": "process.process_id"
      },
      {
        "Name": "Process.StartTimeUtc",
        "DisplayName": "Process / Start Time Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.start_time_utc",
        "EnumDisplayNames": null,
        "StableId": "process.start_time_utc"
      },
      {
        "Name": "Process.ProcessName",
        "DisplayName": "Process / Process Name",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.process_name",
        "EnumDisplayNames": null,
        "StableId": "process.process_name"
      },
      {
        "Name": "Process.ExecutablePath",
        "DisplayName": "Process / Executable Path",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.executable_path",
        "EnumDisplayNames": null,
        "StableId": "process.executable_path"
      },
      {
        "Name": "Process.WindowHandle",
        "DisplayName": "Process / Window Handle",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.window_handle",
        "EnumDisplayNames": null,
        "StableId": "process.window_handle"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Process",
        "DisplayName": "Process",
        "Property": {
          "Name": "Process",
          "DisplayName": "Process",
          "DataType": "ProcessReference",
          "Description": "ProcessReference, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "process",
          "EnumDisplayNames": null,
          "StableId": "process"
        },
        "Children": [
          {
            "Segment": "ProcessId",
            "DisplayName": "Process Id",
            "Property": {
              "Name": "Process.ProcessId",
              "DisplayName": "Process / Process Id",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.process_id",
              "EnumDisplayNames": null,
              "StableId": "process.process_id"
            },
            "Children": []
          },
          {
            "Segment": "StartTimeUtc",
            "DisplayName": "Start Time Utc",
            "Property": {
              "Name": "Process.StartTimeUtc",
              "DisplayName": "Process / Start Time Utc",
              "DataType": "DateTime",
              "Description": "DateTime",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.start_time_utc",
              "EnumDisplayNames": null,
              "StableId": "process.start_time_utc"
            },
            "Children": []
          },
          {
            "Segment": "ProcessName",
            "DisplayName": "Process Name",
            "Property": {
              "Name": "Process.ProcessName",
              "DisplayName": "Process / Process Name",
              "DataType": "Text",
              "Description": "Text, kann leer sein",
              "IsNullable": true,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.process_name",
              "EnumDisplayNames": null,
              "StableId": "process.process_name"
            },
            "Children": []
          },
          {
            "Segment": "ExecutablePath",
            "DisplayName": "Executable Path",
            "Property": {
              "Name": "Process.ExecutablePath",
              "DisplayName": "Process / Executable Path",
              "DataType": "Text",
              "Description": "Text, kann leer sein",
              "IsNullable": true,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.executable_path",
              "EnumDisplayNames": null,
              "StableId": "process.executable_path"
            },
            "Children": []
          },
          {
            "Segment": "WindowHandle",
            "DisplayName": "Window Handle",
            "Property": {
              "Name": "Process.WindowHandle",
              "DisplayName": "Process / Window Handle",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.window_handle",
              "EnumDisplayNames": null,
              "StableId": "process.window_handle"
            },
            "Children": []
          }
        ]
      }
    ]
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "start_process",
  "settings": {
    "target": {
      "process_source": {
        "provider_id": "",
        "source_id": ""
      },
      "process_name": "",
      "executable_path": "",
      "window_title_contains": ""
    },
    "action": "Start",
    "executable_path": "",
    "arguments": "",
    "working_directory": "",
    "wait_for_exit": false,
    "monitor_index": 0,
    "placement_mode": "Centered",
    "offset_x": 0,
    "offset_y": 0,
    "window_mode": "ApplicationDefault"
  },
  "id": "6fbbf6ae-f17e-bbfb-d307-3c3f4e398d7a",
  "inputs": {},
  "is_enabled": true
}
```

