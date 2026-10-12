## Steps: Laufenden Prozess suchen

ID: get_process
Website: /doku/steps/get_process/

Abbildung: [Detailansicht: Laufenden Prozess suchen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-get-process.detail.png)

1. Prozessname: Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.
2. Programmpfad: Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.
3. Fenstertitel enthält: Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

process_name auf notepad setzen; process an den nächsten Fokus-Step weiterreichen.

### field.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### field.process_name

Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.

### field.window_title_contains

Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.

### purpose

Suche eine laufende Programminstanz über Namen, ausführbare Datei und optionalen Fenstertitel. Die Ausgabe process bezeichnet eine konkrete Instanz und kann an Statusprüfung, Fenstersteuerung oder Beenden weitergereicht werden. Verwende found beziehungsweise match_count, wenn kein oder mehr als ein Treffer möglich ist.

### result.found

Gibt an, ob die Prozesssuche einen passenden Prozess gefunden hat. Beispiel: true.

### result.process

Referenz auf die konkrete Prozessinstanz für nachfolgende Prozessaktionen. Enthält ID und Startzeit, damit eine später wiederverwendete Prozess-ID nicht verwechselt wird. Kann fehlen.

### result.process.executable_path

Pfad der ausführbaren Programmdatei, soweit Windows ihn bereitstellt. Kann leer sein, etwa bei fehlenden Zugriffsrechten. Beispiel: C:\Data\example.png.

### result.process.process_id

Windows-ID der ermittelten Prozessinstanz. Zusammen mit der Startzeit verwenden, um die Instanz eindeutig zu erkennen. Beispiel: 3.

### result.process.process_name

Name des ermittelten Prozesses, beispielsweise notepad. Geeignet zur Anzeige oder für einen Textvergleich.

### result.process.start_time_utc

UTC-Startzeit dieser Prozessinstanz. Unterscheidet zusammen mit der Prozess-ID verschiedene Starts desselben Programms. Beispiel: 2030-01-01T08:00:00Z.

### result.process.window_handle

Technische Windows-Kennung des zugeordneten Fensters. 0 bedeutet, dass kein Fenster zugeordnet ist. Beispiel: 3.

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

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen. Neue Verbindungen müssen die aktuellen Anbieter- und Quellformen erfüllen. Legacy-Regeln erhalten bestehende Referenzen und erweitern nicht automatisch die neue Auswahl.

### schema.inputs.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID. Die gemeinsame Eingabereferenz inputContracts/version=1 beschreibt erlaubte Member- und Itemverträge unabhängig vom Editor; bindingSchemas und fieldSchemas bleiben verfügbar.

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
      "helpKey": "Ui.Step.Settings.ProcessName.Help",
      "help": "Name des gesuchten Prozesses, beispielsweise notepad. Mehrere Instanzen können denselben Namen haben.",
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
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "executable_path",
      "name": "Programmpfad",
      "helpKey": "Ui.Step.Settings.PathProgram.Help",
      "help": "Pfad zur ausführbaren Programmdatei.",
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
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "window_title_contains",
      "name": "Fenstertitel enthält",
      "helpKey": "Ui.Step.Settings.WindowTitleContains.Help",
      "help": "Optionaler Teil des Fenstertitels zum Eingrenzen der Suche. Leerer Text setzt diesen Filter nicht.",
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
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
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
        "DisplayName": "Prozess gefunden",
        "DataType": "Boolean",
        "Description": "Gibt an, ob die Prozesssuche einen passenden Prozess gefunden hat.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "found",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "found"
      },
      {
        "Name": "Process",
        "DisplayName": "Ermittelter Prozess",
        "DataType": "ProcessReference",
        "Description": "Referenz auf die konkrete Prozessinstanz für nachfolgende Prozessaktionen. Enthält ID und Startzeit, damit eine später wiederverwendete Prozess-ID nicht verwechselt wird. Kann fehlen.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "process"
      },
      {
        "Name": "Process.ProcessId",
        "DisplayName": "Prozess / Prozess-ID",
        "DataType": "Integer",
        "Description": "Windows-ID der ermittelten Prozessinstanz. Zusammen mit der Startzeit verwenden, um die Instanz eindeutig zu erkennen.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.process_id",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "process.process_id"
      },
      {
        "Name": "Process.StartTimeUtc",
        "DisplayName": "Prozess / Startzeit (UTC)",
        "DataType": "DateTime",
        "Description": "UTC-Startzeit dieser Prozessinstanz. Unterscheidet zusammen mit der Prozess-ID verschiedene Starts desselben Programms.",
        "IsNullable": false,
        "Example": "2030-01-01T08:00:00Z",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.start_time_utc",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "process.start_time_utc"
      },
      {
        "Name": "Process.ProcessName",
        "DisplayName": "Prozess / Prozessname",
        "DataType": "Text",
        "Description": "Name des ermittelten Prozesses, beispielsweise notepad. Geeignet zur Anzeige oder für einen Textvergleich.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.process_name",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "process.process_name"
      },
      {
        "Name": "Process.ExecutablePath",
        "DisplayName": "Prozess / Programmpfad",
        "DataType": "Text",
        "Description": "Pfad der ausführbaren Programmdatei, soweit Windows ihn bereitstellt. Kann leer sein, etwa bei fehlenden Zugriffsrechten.",
        "IsNullable": true,
        "Example": "C:\\Data\\example.png",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.executable_path",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "process.executable_path"
      },
      {
        "Name": "Process.WindowHandle",
        "DisplayName": "Prozess / Fensterkennung",
        "DataType": "Integer",
        "Description": "Technische Windows-Kennung des zugeordneten Fensters. 0 bedeutet, dass kein Fenster zugeordnet ist.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "process.window_handle",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "process.window_handle"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Found",
        "DisplayName": "Prozess gefunden",
        "Property": {
          "Name": "Found",
          "DisplayName": "Prozess gefunden",
          "DataType": "Boolean",
          "Description": "Gibt an, ob die Prozesssuche einen passenden Prozess gefunden hat.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "found",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "found"
        },
        "Children": []
      },
      {
        "Segment": "Process",
        "DisplayName": "Ermittelter Prozess",
        "Property": {
          "Name": "Process",
          "DisplayName": "Ermittelter Prozess",
          "DataType": "ProcessReference",
          "Description": "Referenz auf die konkrete Prozessinstanz für nachfolgende Prozessaktionen. Enthält ID und Startzeit, damit eine später wiederverwendete Prozess-ID nicht verwechselt wird. Kann fehlen.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "process",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "process"
        },
        "Children": [
          {
            "Segment": "ProcessId",
            "DisplayName": "Prozess-ID",
            "Property": {
              "Name": "Process.ProcessId",
              "DisplayName": "Prozess / Prozess-ID",
              "DataType": "Integer",
              "Description": "Windows-ID der ermittelten Prozessinstanz. Zusammen mit der Startzeit verwenden, um die Instanz eindeutig zu erkennen.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.process_id",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "process.process_id"
            },
            "Children": []
          },
          {
            "Segment": "StartTimeUtc",
            "DisplayName": "Startzeit (UTC)",
            "Property": {
              "Name": "Process.StartTimeUtc",
              "DisplayName": "Prozess / Startzeit (UTC)",
              "DataType": "DateTime",
              "Description": "UTC-Startzeit dieser Prozessinstanz. Unterscheidet zusammen mit der Prozess-ID verschiedene Starts desselben Programms.",
              "IsNullable": false,
              "Example": "2030-01-01T08:00:00Z",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.start_time_utc",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "process.start_time_utc"
            },
            "Children": []
          },
          {
            "Segment": "ProcessName",
            "DisplayName": "Prozessname",
            "Property": {
              "Name": "Process.ProcessName",
              "DisplayName": "Prozess / Prozessname",
              "DataType": "Text",
              "Description": "Name des ermittelten Prozesses, beispielsweise notepad. Geeignet zur Anzeige oder für einen Textvergleich.",
              "IsNullable": true,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.process_name",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "process.process_name"
            },
            "Children": []
          },
          {
            "Segment": "ExecutablePath",
            "DisplayName": "Programmpfad",
            "Property": {
              "Name": "Process.ExecutablePath",
              "DisplayName": "Prozess / Programmpfad",
              "DataType": "Text",
              "Description": "Pfad der ausführbaren Programmdatei, soweit Windows ihn bereitstellt. Kann leer sein, etwa bei fehlenden Zugriffsrechten.",
              "IsNullable": true,
              "Example": "C:\\Data\\example.png",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.executable_path",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "process.executable_path"
            },
            "Children": []
          },
          {
            "Segment": "WindowHandle",
            "DisplayName": "Fensterkennung",
            "Property": {
              "Name": "Process.WindowHandle",
              "DisplayName": "Prozess / Fensterkennung",
              "DataType": "Integer",
              "Description": "Technische Windows-Kennung des zugeordneten Fensters. 0 bedeutet, dass kein Fenster zugeordnet ist.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "process.window_handle",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
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

