## Steps: Skript ausführen

ID: script_execution
Website: /doku/steps/script_execution/

Abbildung: [Detailansicht: Skript ausführen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-script-execution.detail.png)

1. Skriptpfad: Pfad zur vorhandenen .ps1-, .py- oder Batch-Datei. Der entsprechende Interpreter muss auf dem Zielrechner verfügbar sein.
2. Auf Beendigung warten: true wartet an diesem Step auf das Ende des gestarteten Programms/Skripts. Ohne sofortiges Warten bleibt besessene Skriptarbeit bis zum Job-Ende zugeordnet.
3. Argumente: Argumentzeichenfolge für das Programm oder Skript. Passe Anführungszeichen an den Interpreter an; Argumente werden nicht als vollständiger Inhalt in den Verlauf übernommen.
### errors

Ein nicht vorhandener Interpreter oder Skriptpfad, ein fehlerhafter Exit beziehungsweise ein fehlgeschlagener Hintergrundprozess kann den Job fehlschlagen lassen. Der Verlauf enthält Kontext und bereinigte Diagnose, nicht automatisch den vollständigen Skriptinhalt oder dessen Argumente.

### example

Ein selbst erstelltes .ps1-Skript mit einem Testordner als Argument wählen und wait_for_exit aktivieren; den Verlauf auf Exit-Fehler prüfen.

### field.arguments

Argumentzeichenfolge für das Programm oder Skript. Passe Anführungszeichen an den Interpreter an; Argumente werden nicht als vollständiger Inhalt in den Verlauf übernommen.

### field.script_path

Pfad zur vorhandenen .ps1-, .py- oder Batch-Datei. Der entsprechende Interpreter muss auf dem Zielrechner verfügbar sein.

### field.wait_for_exit

true wartet an diesem Step auf das Ende des gestarteten Programms/Skripts. Ohne sofortiges Warten bleibt besessene Skriptarbeit bis zum Job-Ende zugeordnet.

### purpose

Wähle ein vorhandenes PowerShell-, Python- oder Batch-Skript und dessen Argumente. Mit Warten wird sein Ende vor dem nächsten Step abgewartet. Ohne Warten gehört der Hintergrundprozess weiterhin zum Job: Fehler werden dem Besitzer gemeldet und die Endphase beginnt erst nach dem Zusammenführen besessener Arbeit.

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

### schema.settings.arguments

Argumentzeichenfolge für das Programm oder Skript. Passe Anführungszeichen an den Interpreter an; Argumente werden nicht als vollständiger Inhalt in den Verlauf übernommen.

### schema.settings.script_path

Pfad zur vorhandenen .ps1-, .py- oder Batch-Datei. Der entsprechende Interpreter muss auf dem Zielrechner verfügbar sein.

### schema.settings.wait_for_exit

true wartet an diesem Step auf das Ende des gestarteten Programms/Skripts. Ohne sofortiges Warten bleibt besessene Skriptarbeit bis zum Job-Ende zugeordnet.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "script_execution",
  "name": "Skript ausführen",
  "description": "Führt ein externes PowerShell-, Python- oder Batch-Skript aus.",
  "category": "ProgrammeFenster",
  "uiFieldIds": [
    "script_path",
    "wait_for_exit",
    "arguments"
  ],
  "fields": [
    {
      "id": "script_path",
      "name": "Skriptpfad",
      "descriptor": {
        "Id": "script_path",
        "LabelKey": "Ui.Step.Settings.ScriptPath",
        "ValueKind": "FilePath",
        "Required": true,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "file-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
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
      "type": "ScriptExecutionSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.arguments",
      "name": "arguments",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.script_path",
      "name": "script_path",
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
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "ScriptExecutionResult",
    "DisplayName": "ScriptExecutionResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "script_execution",
  "settings": {
    "script_path": "",
    "arguments": "",
    "wait_for_exit": false
  },
  "id": "ed6670e1-efaa-085a-1445-700c4fc2d2bb",
  "inputs": {},
  "is_enabled": true
}
```

