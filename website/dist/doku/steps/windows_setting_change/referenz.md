## Steps: Windows-Einstellung ändern

ID: windows_setting_change
Website: /doku/steps/windows_setting_change/

Abbildung: [Detailansicht: Windows-Einstellung ändern. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-windows-setting-change.detail.png)

1. Windows-Funktion: Windows-Funktion und dazugehörige Parameter im Editor. Wähle eine Funktion, die die gewünschte Abfrage/Änderung tatsächlich unterstützt.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

audio.mute auswählen und state auf on setzen; previous_value für die spätere Wiederherstellung lesen.

### field.capability

Windows-Funktion und dazugehörige Parameter im Editor. Wähle eine Funktion, die die gewünschte Abfrage/Änderung tatsächlich unterstützt.

### purpose

Wähle eine Windows-Funktion, die Einstellungsänderungen unterstützt, und ergänze deren Parameter. Der Step meldet vorherigen und angewendeten Wert sowie Erfolg oder Fehler. Administratorrechte und gerätespezifische Voraussetzungen stehen auf der jeweiligen Funktionsseite; sie können nicht durch eine JSON-Einstellung ersetzt werden.

### result.windows_setting.applied_value

Vom Windows-Provider gemeldeter angewendeter Wert nach der Änderung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.error_code

Maschinenlesbarer Fehlercode einer Windows-Operation; bei Erfolg entsprechend dem Vertrag leer beziehungsweise nicht gesetzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.previous_value

Vor der Windows-Änderung beobachteter Wert. Für eine Wiederherstellung nur mit derselben Funktion und kompatibler Darstellung verwenden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.restart_required

Ob die abgefragte Funktion beziehungsweise Windows Update einen Neustartbedarf meldet. Einzelwert dieses Ergebnisses.

### result.windows_setting.setting_id

Kennung der geänderten Windows-Funktion. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.status

Vom zuständigen Windows-Dienst gemeldeter Zustand. Nicht durch frei erfundene Statusnamen ersetzen. Einzelwert dieses Ergebnisses.

### result.windows_setting.status.option.AccessDenied

Windows verweigert den Zugriff; kein negativer fachlicher Zustand, sondern fehlende Berechtigung.

### result.windows_setting.status.option.Failed

Lauf wegen Fehler fehlgeschlagen.

### result.windows_setting.status.option.Success

Windows-Aufruf erfolgreich. Erst anschließend den fachlichen Ergebniswert auswerten.

### result.windows_setting.status.option.Timeout

Windows-Aufruf überschritt seine Zeitgrenze; daraus keinen Systemzustand ableiten.

### result.windows_setting.status.option.Unsupported

Funktion auf diesem Zielsystem nicht unterstützt.

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

### schema.settings.parameters

Benannte Parameter der Windows-Funktion beziehungsweise bereinigte Diagnoseparameter im Log. Parameternamen sind stabil und keine UI-Labels.

### schema.settings.setting_id

Stabile Kennung der zu ändernden Windows-Einstellung. Ihre Parameter sind im Windows-Funktionskatalog dokumentiert.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "windows_setting_change",
  "name": "Windows-Einstellung ändern",
  "description": "Ändert eine ausgewählte Windows-Einstellung und liefert den vorherigen sowie den angewendeten Wert.",
  "category": "WindowsSystem",
  "uiFieldIds": [
    "capability"
  ],
  "fields": [
    {
      "id": "capability",
      "name": "Windows-Funktion",
      "descriptor": {
        "Id": "capability",
        "LabelKey": "Ui.Windows.Capability",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": {
          "capability_id": "audio.master_volume",
          "parameters": {
            "value": "50"
          }
        },
        "DescriptionKey": null,
        "EditorHint": "windows-capability-picker",
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
        "WindowsCapabilityPickerOptions": {
          "Mode": "SettingChange"
        },
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
      "type": "WindowsSettingChangeSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.parameters",
      "name": "parameters",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.setting_id",
      "name": "setting_id",
      "type": "String",
      "options": [],
      "defaultValue": "audio.master_volume"
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "WindowsSettingChangeResult",
    "DisplayName": "WindowsSettingChangeResult",
    "Properties": [
      {
        "Name": "Status",
        "DisplayName": "Status",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsCapabilityStatus",
        "EnumValues": [
          "Success",
          "Unsupported",
          "AccessDenied",
          "Timeout",
          "Failed"
        ],
        "Id": "windows_setting.status",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.status"
      },
      {
        "Name": "SettingId",
        "DisplayName": "Einstellung",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.setting_id",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.setting_id"
      },
      {
        "Name": "PreviousValue",
        "DisplayName": "Vorheriger Wert",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.previous_value",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.previous_value"
      },
      {
        "Name": "AppliedValue",
        "DisplayName": "Angewendeter Wert",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.applied_value",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.applied_value"
      },
      {
        "Name": "RestartRequired",
        "DisplayName": "Neustart erforderlich",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.restart_required",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.restart_required"
      },
      {
        "Name": "ErrorCode",
        "DisplayName": "Fehlercode",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.error_code",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.error_code"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Status",
        "DisplayName": "Status",
        "Property": {
          "Name": "Status",
          "DisplayName": "Status",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsCapabilityStatus",
          "EnumValues": [
            "Success",
            "Unsupported",
            "AccessDenied",
            "Timeout",
            "Failed"
          ],
          "Id": "windows_setting.status",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.status"
        },
        "Children": []
      },
      {
        "Segment": "SettingId",
        "DisplayName": "Einstellung",
        "Property": {
          "Name": "SettingId",
          "DisplayName": "Einstellung",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.setting_id",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.setting_id"
        },
        "Children": []
      },
      {
        "Segment": "PreviousValue",
        "DisplayName": "Vorheriger Wert",
        "Property": {
          "Name": "PreviousValue",
          "DisplayName": "Vorheriger Wert",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.previous_value",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.previous_value"
        },
        "Children": []
      },
      {
        "Segment": "AppliedValue",
        "DisplayName": "Angewendeter Wert",
        "Property": {
          "Name": "AppliedValue",
          "DisplayName": "Angewendeter Wert",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.applied_value",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.applied_value"
        },
        "Children": []
      },
      {
        "Segment": "RestartRequired",
        "DisplayName": "Neustart erforderlich",
        "Property": {
          "Name": "RestartRequired",
          "DisplayName": "Neustart erforderlich",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.restart_required",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.restart_required"
        },
        "Children": []
      },
      {
        "Segment": "ErrorCode",
        "DisplayName": "Fehlercode",
        "Property": {
          "Name": "ErrorCode",
          "DisplayName": "Fehlercode",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.error_code",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.error_code"
        },
        "Children": []
      }
    ]
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "windows_setting_change",
  "settings": {
    "setting_id": "audio.master_volume",
    "parameters": {}
  },
  "id": "cdf47e67-4f34-aa28-2720-e262a0db31d5",
  "inputs": {},
  "is_enabled": true
}
```

