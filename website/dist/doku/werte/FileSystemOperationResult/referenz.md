## Werte und Ergebnisse: FileSystemOperationResult

ID: FileSystemOperationResult
Website: /doku/werte/FileSystemOperationResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag FileSystemOperationResult. Wird von Datei oder Ordner bearbeiten geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.affected_bytes

Umfang der betroffenen Dateidaten in Bytes. Einzelwert dieses Ergebnisses.

### result.affected_count

Gesamtzahl betroffener Einträge der Dateiaktion. Einzelwert dieses Ergebnisses.

### result.affected_directory_count

Anzahl betroffener Ordner, getrennt von Dateien. Einzelwert dieses Ergebnisses.

### result.affected_file_count

Anzahl betroffener Dateien, getrennt von Ordnern. Einzelwert dieses Ergebnisses.

### result.affected_paths

Liste der von der Dateiaktion betroffenen Pfade. Ein erfolgreicher Zähler ist nicht automatisch ein Backup-Versionsnachweis. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.affected_paths.count

Anzahl der Elemente der referenzierten Sammlung, automatisch als zusätzlicher Ergebniswert verfügbar. Einzelwert dieses Ergebnisses.

### result.completed_at_utc

UTC-Abschlusszeitpunkt der Aktion. Einzelwert dieses Ergebnisses.

### result.item_type

Ob Datei, Ordner oder mehrere Einträge von der Dateiaktion betroffen waren. Einzelwert dieses Ergebnisses.

### result.item_type.option.Directory

Betroffener Eintrag ist ein Ordner.

### result.item_type.option.File

Betroffener Eintrag ist eine Datei.

### result.item_type.option.Multiple

Mehrere Einträge wurden bearbeitet.

### result.operation

Tatsächlich angeforderte Dateiaktion. Zusammen mit den Zählern und Pfaden lesen, um deren Auswirkungen zu verstehen. Einzelwert dieses Ergebnisses.

### result.operation.option.Copy

Kopiert die Quelle zum Ziel; die Quelle bleibt erhalten.

### result.operation.option.Delete

Entfernt passende Quelleinträge; zuerst mit entbehrlichen Testdaten prüfen.

### result.operation.option.Move

Verschiebt die Quelle zum Ziel; bei Erfolg liegt sie dort statt am ursprünglichen Ort.

### result.operation.option.Rename

Benennt den Quelleintrag mit new_name um.

### result.source_path

Verwendeter Quellpfad der Dateiaktion. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.target_path

Verwendeter Zielpfad der Dateiaktion, soweit die gewählte Aktion ein Ziel besitzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "FileSystemOperationResult",
  "name": "FileSystemOperationResult",
  "result": {
    "TypeName": "FileSystemOperationResult",
    "DisplayName": "FileSystemOperationResult",
    "Properties": [
      {
        "Name": "Operation",
        "DisplayName": "Operation",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.Jobs.FileSystemOperation",
        "EnumValues": [
          "Copy",
          "Move",
          "Rename",
          "Delete"
        ],
        "Id": "operation",
        "EnumDisplayNames": null,
        "StableId": "operation"
      },
      {
        "Name": "SourcePath",
        "DisplayName": "Source Path",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_path",
        "EnumDisplayNames": null,
        "StableId": "source_path"
      },
      {
        "Name": "TargetPath",
        "DisplayName": "Target Path",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "target_path",
        "EnumDisplayNames": null,
        "StableId": "target_path"
      },
      {
        "Name": "ItemType",
        "DisplayName": "Item Type",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.Steps.FileSystemItemType",
        "EnumValues": [
          "File",
          "Directory",
          "Multiple"
        ],
        "Id": "item_type",
        "EnumDisplayNames": null,
        "StableId": "item_type"
      },
      {
        "Name": "AffectedCount",
        "DisplayName": "Affected Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_count",
        "EnumDisplayNames": null,
        "StableId": "affected_count"
      },
      {
        "Name": "AffectedFileCount",
        "DisplayName": "Affected File Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_file_count",
        "EnumDisplayNames": null,
        "StableId": "affected_file_count"
      },
      {
        "Name": "AffectedDirectoryCount",
        "DisplayName": "Affected Directory Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_directory_count",
        "EnumDisplayNames": null,
        "StableId": "affected_directory_count"
      },
      {
        "Name": "AffectedBytes",
        "DisplayName": "Affected Bytes",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_bytes",
        "EnumDisplayNames": null,
        "StableId": "affected_bytes"
      },
      {
        "Name": "AffectedPaths",
        "DisplayName": "Affected Paths",
        "DataType": "Text",
        "Description": "Text",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_paths",
        "EnumDisplayNames": null,
        "StableId": "affected_paths"
      },
      {
        "Name": "AffectedPaths.Count",
        "DisplayName": "Affected Paths / Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_paths.count",
        "EnumDisplayNames": null,
        "StableId": "affected_paths.count"
      },
      {
        "Name": "CompletedAtUtc",
        "DisplayName": "Completed At Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "completed_at_utc",
        "EnumDisplayNames": null,
        "StableId": "completed_at_utc"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Operation",
        "DisplayName": "Operation",
        "Property": {
          "Name": "Operation",
          "DisplayName": "Operation",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.Jobs.FileSystemOperation",
          "EnumValues": [
            "Copy",
            "Move",
            "Rename",
            "Delete"
          ],
          "Id": "operation",
          "EnumDisplayNames": null,
          "StableId": "operation"
        },
        "Children": []
      },
      {
        "Segment": "SourcePath",
        "DisplayName": "Source Path",
        "Property": {
          "Name": "SourcePath",
          "DisplayName": "Source Path",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_path",
          "EnumDisplayNames": null,
          "StableId": "source_path"
        },
        "Children": []
      },
      {
        "Segment": "TargetPath",
        "DisplayName": "Target Path",
        "Property": {
          "Name": "TargetPath",
          "DisplayName": "Target Path",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "target_path",
          "EnumDisplayNames": null,
          "StableId": "target_path"
        },
        "Children": []
      },
      {
        "Segment": "ItemType",
        "DisplayName": "Item Type",
        "Property": {
          "Name": "ItemType",
          "DisplayName": "Item Type",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.Steps.FileSystemItemType",
          "EnumValues": [
            "File",
            "Directory",
            "Multiple"
          ],
          "Id": "item_type",
          "EnumDisplayNames": null,
          "StableId": "item_type"
        },
        "Children": []
      },
      {
        "Segment": "AffectedCount",
        "DisplayName": "Affected Count",
        "Property": {
          "Name": "AffectedCount",
          "DisplayName": "Affected Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_count",
          "EnumDisplayNames": null,
          "StableId": "affected_count"
        },
        "Children": []
      },
      {
        "Segment": "AffectedFileCount",
        "DisplayName": "Affected File Count",
        "Property": {
          "Name": "AffectedFileCount",
          "DisplayName": "Affected File Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_file_count",
          "EnumDisplayNames": null,
          "StableId": "affected_file_count"
        },
        "Children": []
      },
      {
        "Segment": "AffectedDirectoryCount",
        "DisplayName": "Affected Directory Count",
        "Property": {
          "Name": "AffectedDirectoryCount",
          "DisplayName": "Affected Directory Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_directory_count",
          "EnumDisplayNames": null,
          "StableId": "affected_directory_count"
        },
        "Children": []
      },
      {
        "Segment": "AffectedBytes",
        "DisplayName": "Affected Bytes",
        "Property": {
          "Name": "AffectedBytes",
          "DisplayName": "Affected Bytes",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_bytes",
          "EnumDisplayNames": null,
          "StableId": "affected_bytes"
        },
        "Children": []
      },
      {
        "Segment": "AffectedPaths",
        "DisplayName": "Affected Paths",
        "Property": {
          "Name": "AffectedPaths",
          "DisplayName": "Affected Paths",
          "DataType": "Text",
          "Description": "Text",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_paths",
          "EnumDisplayNames": null,
          "StableId": "affected_paths"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Count",
            "Property": {
              "Name": "AffectedPaths.Count",
              "DisplayName": "Affected Paths / Count",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "affected_paths.count",
              "EnumDisplayNames": null,
              "StableId": "affected_paths.count"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "CompletedAtUtc",
        "DisplayName": "Completed At Utc",
        "Property": {
          "Name": "CompletedAtUtc",
          "DisplayName": "Completed At Utc",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "completed_at_utc",
          "EnumDisplayNames": null,
          "StableId": "completed_at_utc"
        },
        "Children": []
      }
    ]
  }
}
```

