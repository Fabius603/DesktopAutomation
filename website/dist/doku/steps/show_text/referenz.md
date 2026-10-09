## Steps: Text auf Desktop anzeigen

ID: show_text
Website: /doku/steps/show_text/

Abbildung: [Die Desktop-Textausgabe übernimmt die Beschriftung der Benutzerauswahl als Ergebnisquelle. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/step-werte.detail.png)

1. Text: Text oder kompatibler Anzeige-Wert aus einer Quelle. Für Benutzerauswahlen eignet sich selected_label.
2. Monitor: Nullbasierter Zielmonitor der Desktop-Anzeige oder Benutzerauswahl. Prüfe ihn auf dem Zielrechner.
3. Schriftgröße (pt): Schriftgröße der Textanzeige in Punkten. Prüfe Lesbarkeit auf dem tatsächlichen Monitor und dessen Skalierung.
4. Schriftfarbe: Hex-Farbwert der Textanzeige. Wähle einen ausreichenden Kontrast zum Hintergrund.
5. Deckkraft (%): Deckkraft als Anteil von 0 bis 1, auch im referenzierten localValues-Wert. 1 bedeutet vollständig sichtbar, 0 unsichtbar. Der Editor zeigt Prozent an.
6. Anzeigedauer (ms): Anzeigedauer in Millisekunden. Beachte die vom Editor angebotenen Grenzen und die zusätzliche Entfernung beim Job-Ende.
7. Bei Job-Ende entfernen: Entfernt die durch diesen Job erzeugte Anzeige beim regulären Aufräumen.
8. Position: 
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Die gewählte Antwort aus user_choice über selected_label zwei Sekunden auf Monitor 0 anzeigen.

### field.clear_on_job_end

Entfernt die durch diesen Job erzeugte Anzeige beim regulären Aufräumen.

### field.desktop_index

Nullbasierter Zielmonitor der Desktop-Anzeige oder Benutzerauswahl. Prüfe ihn auf dem Zielrechner.

### field.duration_ms

Anzeigedauer in Millisekunden. Beachte die vom Editor angebotenen Grenzen und die zusätzliche Entfernung beim Job-Ende.

### field.font_color

Hex-Farbwert der Textanzeige. Wähle einen ausreichenden Kontrast zum Hintergrund.

### field.font_size

Schriftgröße der Textanzeige in Punkten. Prüfe Lesbarkeit auf dem tatsächlichen Monitor und dessen Skalierung.

### field.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### field.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### field.opacity

Deckkraft als Anteil von 0 bis 1, auch im referenzierten localValues-Wert. 1 bedeutet vollständig sichtbar, 0 unsichtbar. Der Editor zeigt Prozent an.

### field.text_result

Text oder kompatibler Anzeige-Wert aus einer Quelle. Für Benutzerauswahlen eignet sich selected_label.

### input.text

Text beziehungsweise darstellbarer Wert. Fehlende erforderliche Werte führen zum Fehler. Bei kompatiblen Listen wird der erste Wert verwendet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Trage Text ein oder verbinde einen kompatiblen Wert. Wähle Monitor, Schrift, Farbe, Deckkraft und Anzeigedauer. Die Anzeige kann nach Zeitablauf oder beim Job-Ende entfernt werden. Verwende selected_label einer Benutzerauswahl für eine verständliche Rückmeldung.

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

### schema.settings.clear_on_job_end

Entfernt die durch diesen Job erzeugte Anzeige beim regulären Aufräumen.

### schema.settings.desktop_index

Nullbasierter Zielmonitor der Desktop-Anzeige oder Benutzerauswahl. Prüfe ihn auf dem Zielrechner.

### schema.settings.duration_ms

Anzeigedauer in Millisekunden. Beachte die vom Editor angebotenen Grenzen und die zusätzliche Entfernung beim Job-Ende.

### schema.settings.font_color

Hex-Farbwert der Textanzeige. Wähle einen ausreichenden Kontrast zum Hintergrund.

### schema.settings.font_size

Schriftgröße der Textanzeige in Punkten. Prüfe Lesbarkeit auf dem tatsächlichen Monitor und dessen Skalierung.

### schema.settings.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.opacity

Deckkraft als Anteil von 0 bis 1, auch im referenzierten localValues-Wert. 1 bedeutet vollständig sichtbar, 0 unsichtbar. Der Editor zeigt Prozent an.

### schema.settings.text

Direkt angezeigter beziehungsweise eingegebener Text. Ein Makro schreibt ihn in das aktive Fenster; die Desktop-Anzeige stellt ihn nur dar.

### schema.settings.text_result

Text oder kompatibler Anzeige-Wert aus einer Quelle. Für Benutzerauswahlen eignet sich selected_label.

### schema.settings.text_result.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.text_result.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.text_result.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.text_result.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.text_result.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.text_result.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.text_result.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.text_result.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.text_result.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.text_source

ExplicitText verwendet den direkten Text, TaskResult den verbundenen Ergebniswert in der älteren settings-Konfiguration.

### schema.settings.text_source.option.ExplicitText

Verwendet den direkt eingegebenen Text.

### schema.settings.text_source.option.TaskResult

Verwendet den verbundenen Step-Ergebniswert statt des direkten Literals.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "show_text",
  "name": "Text auf Desktop anzeigen",
  "description": "Zeigt konfigurierbaren Text auf dem Desktop an.",
  "category": "AnzeigenSpeichern",
  "uiFieldIds": [
    "text_result",
    "desktop_index",
    "font_size",
    "font_color",
    "opacity",
    "duration_ms",
    "clear_on_job_end",
    "offset_x",
    "offset_y"
  ],
  "fields": [
    {
      "id": "text_result",
      "name": "Text",
      "descriptor": {
        "Id": "text_result",
        "LabelKey": "Ui.Step.Settings.DisplayText",
        "ValueKind": "ResultBinding",
        "Required": true,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "value-reference-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": "text",
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": true,
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "desktop_index",
      "name": "Monitor",
      "descriptor": {
        "Id": "desktop_index",
        "LabelKey": "Ui.Step.Settings.DesktopIndex",
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
      "id": "font_size",
      "name": "Schriftgröße (pt)",
      "descriptor": {
        "Id": "font_size",
        "LabelKey": "Ui.Step.Settings.FontSizePt",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 24,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": 0.01,
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
      "id": "font_color",
      "name": "Schriftfarbe",
      "descriptor": {
        "Id": "font_color",
        "LabelKey": "Ui.Step.Settings.FontColor",
        "ValueKind": "Color",
        "Required": false,
        "DefaultValue": "#FFFFFF",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 5,
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
      "id": "opacity",
      "name": "Deckkraft (%)",
      "descriptor": {
        "Id": "opacity",
        "LabelKey": "Ui.Step.Settings.OpacityPercent",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 1,
        "DescriptionKey": null,
        "EditorHint": "percentage",
        "Constraints": {
          "Minimum": 0,
          "Maximum": 1,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": true,
        "Order": 6,
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
      "id": "duration_ms",
      "name": "Anzeigedauer (ms)",
      "descriptor": {
        "Id": "duration_ms",
        "LabelKey": "Ui.Step.Settings.DisplayDurationMs",
        "ValueKind": "Duration",
        "Required": false,
        "DefaultValue": 5000,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": 0,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": true,
        "Order": 7,
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
      "id": "clear_on_job_end",
      "name": "Bei Job-Ende entfernen",
      "descriptor": {
        "Id": "clear_on_job_end",
        "LabelKey": "Ui.Step.Settings.RemoveWhenJobEnds",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": false,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 8,
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
      "id": "offset_x",
      "name": "Versatz X (px)",
      "descriptor": {
        "Id": "offset_x",
        "LabelKey": "Ui.Step.Settings.XOffsetPixels",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 100,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 9,
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
      "id": "offset_y",
      "name": "Versatz Y (px)",
      "descriptor": {
        "Id": "offset_y",
        "LabelKey": "Ui.Step.Settings.YOffsetPixels",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 100,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 10,
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
      "type": "ShowTextSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.clear_on_job_end",
      "name": "clear_on_job_end",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
    },
    {
      "id": "settings.desktop_index",
      "name": "desktop_index",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.duration_ms",
      "name": "duration_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 5000
    },
    {
      "id": "settings.font_color",
      "name": "font_color",
      "type": "String",
      "options": [],
      "defaultValue": "#FFFFFF"
    },
    {
      "id": "settings.font_size",
      "name": "font_size",
      "type": "Single",
      "options": [],
      "defaultValue": 24
    },
    {
      "id": "settings.offset_x",
      "name": "offset_x",
      "type": "Int32",
      "options": [],
      "defaultValue": 100
    },
    {
      "id": "settings.offset_y",
      "name": "offset_y",
      "type": "Int32",
      "options": [],
      "defaultValue": 100
    },
    {
      "id": "settings.opacity",
      "name": "opacity",
      "type": "Single",
      "options": [],
      "defaultValue": 1
    },
    {
      "id": "settings.text",
      "name": "text",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.text_result",
      "name": "text_result",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.text_result.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_result.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.text_result.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.text_source",
      "name": "text_source",
      "type": "ShowTextSource",
      "options": [
        "ExplicitText",
        "TaskResult"
      ],
      "defaultValue": "TaskResult"
    }
  ],
  "inputs": [
    {
      "Key": "text",
      "Required": true,
      "MissingValuePolicy": "FailStep",
      "CollectionConsumption": "FirstValue",
      "AcceptedShapes": [
        {
          "ValueKind": "Text",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Boolean",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Integer",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Number",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "DateTime",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Color",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "FilePath",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Enum",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Point",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Rectangle",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Detection",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "ProcessReference",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        }
      ],
      "AllowedProviderIds": [
        "job_variable",
        "step_result"
      ],
      "LegacyAllowedProviderIds": [
        "secret"
      ],
      "AllowsDirectValue": true,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "ShowTextResult",
    "DisplayName": "ShowTextResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "show_text",
  "settings": {
    "text_source": "TaskResult",
    "text": "",
    "text_result": {
      "provider_id": "",
      "source_id": ""
    },
    "font_size": 24,
    "font_color": "#FFFFFF",
    "opacity": 1,
    "desktop_index": 0,
    "offset_x": 100,
    "offset_y": 100,
    "duration_ms": 5000,
    "clear_on_job_end": false
  },
  "id": "53488c77-d339-7ac1-2fc2-7774b8ec431e",
  "inputs": {},
  "is_enabled": true
}
```

