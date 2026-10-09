## Werte und Ergebnisse: StartProcessResult

ID: StartProcessResult
Website: /doku/werte/StartProcessResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag StartProcessResult. Wird von Prozess starten geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

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

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "StartProcessResult",
  "name": "StartProcessResult",
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

