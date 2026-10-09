## Steps: Laufenden Prozess suchen

ID: get_process
Website: /doku/steps/get_process/

Abbildung: [Detailansicht: Laufenden Prozess suchen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-get-process.detail.png)

1. Prozessname: Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein.
2. Programmpfad: Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.
3. Fenstertitel enthält: Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

process_name auf notepad setzen; process an den nächsten Fokus-Step weiterreichen.

### field.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### field.process_name

Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein.

### field.window_title_contains

Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht.

### purpose

Suche eine laufende Programminstanz über Namen, ausführbare Datei und optionalen Fenstertitel. Die Ausgabe process bezeichnet eine konkrete Instanz und kann an Statusprüfung, Fenstersteuerung oder Beenden weitergereicht werden. Verwende found beziehungsweise match_count, wenn kein oder mehr als ein Treffer möglich ist.

### result.found

Ob die Erkennung/Suche in diesem Lauf einen passenden Treffer geliefert hat. false ist ein fachliches Ergebnis und nicht automatisch ein Step-Fehler. Einzelwert dieses Ergebnisses.

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

### schema.settings.query

Prozesssuche mit process_name, executable_path, window_title_contains oder einer konkreten process_source. Leere Suchkriterien und Typkompatibilität gemäß Prozessvertrag prüfen.

### schema.settings.query.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### schema.settings.query.process_name

Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein.

### schema.settings.query.process_source

Referenz auf eine konkrete laufende Programminstanz, üblicherweise process aus get_process oder start_process.

### schema.settings.query.process_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.query.process_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.query.process_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.query.process_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.query.process_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.query.process_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.query.process_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.query.process_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.query.process_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.query.window_title_contains

Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "get_process",
  "name": "Laufenden Prozess suchen",
  "description": "Sucht einen laufenden Prozess anhand seiner Merkmale und stellt die konkrete Prozessreferenz für nachfolgende Steps bereit.",
  "category": "ProgrammeFenster",
  "uiFieldIds": [
    "process_name",
    "executable_path",
    "window_title_contains"
  ],
  "fields": [
    {
      "id": "process_name",
      "name": "Prozessname",
      "descriptor": {
        "Id": "process_name",
        "LabelKey": "Ui.Step.Settings.ProcessName",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "process-name-suggestions",
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
      "id": "executable_path",
      "name": "Programmpfad",
      "descriptor": {
        "Id": "executable_path",
        "LabelKey": "Ui.Step.Settings.PathProgram",
        "ValueKind": "FilePath",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "executable-path-suggestions",
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
      "id": "window_title_contains",
      "name": "Fenstertitel enthält",
      "descriptor": {
        "Id": "window_title_contains",
        "LabelKey": "Ui.Step.Settings.WindowTitleContains",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
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
      "type": "GetProcessSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query",
      "name": "query",
      "type": "ProcessTargetSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.executable_path",
      "name": "executable_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.query.process_name",
      "name": "process_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.query.process_source",
      "name": "process_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.query.process_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.process_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.query.process_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query.window_title_contains",
      "name": "window_title_contains",
      "type": "String",
      "options": [],
      "defaultValue": ""
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "GetProcessResult",
    "DisplayName": "GetProcessResult",
    "Properties": [
      {
        "Name": "Found",
        "DisplayName": "Found",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "found",
        "EnumDisplayNames": null,
        "StableId": "found"
      },
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
        "Segment": "Found",
        "DisplayName": "Found",
        "Property": {
          "Name": "Found",
          "DisplayName": "Found",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "found",
          "EnumDisplayNames": null,
          "StableId": "found"
        },
        "Children": []
      },
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
  "type": "get_process",
  "settings": {
    "query": {
      "process_source": {
        "provider_id": "",
        "source_id": ""
      },
      "process_name": "",
      "executable_path": "",
      "window_title_contains": ""
    }
  },
  "id": "4d2791b6-420f-dcae-97a3-f32ff904ab70",
  "inputs": {},
  "is_enabled": true
}
```

