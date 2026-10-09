## Steps: Kamerabild aufnehmen

ID: camera_capture
Website: /doku/steps/camera_capture/

Abbildung: [Detailansicht: Kamerabild aufnehmen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-camera-capture.detail.png)

1. Kamera: Kameraauswahl im Editor. Übernimm ein auf dem Zielrechner verfügbares Gerät und den gewünschten Aufnahme-/Qualitätsmodus.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Kamera auswählen → Kamerabild aufnehmen → Bild anzeigen mit image als Bildquelle.

### field.camera

Kameraauswahl im Editor. Übernimm ein auf dem Zielrechner verfügbares Gerät und den gewünschten Aufnahme-/Qualitätsmodus.

### purpose

Wähle eine Kamera und lasse ein Einzelbild aufnehmen. Der Step liefert ein Bildobjekt für nachfolgende Erkennung, Vorschau oder Speicherung. Prüfe Kameraberechtigung und Geräteverfügbarkeit; die Aufnahmezeit bezeichnet den Empfang des Bildes und nicht einen garantierten Belichtungszeitpunkt.

### result.bounds

Ermitteltes Rechteck im Koordinatenraum des entsprechenden Ergebnisobjekts. Einzelwert dieses Ergebnisses.

### result.bounds.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Einzelwert dieses Ergebnisses.

### result.bounds.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Einzelwert dieses Ergebnisses.

### result.bounds.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Einzelwert dieses Ergebnisses.

### result.bounds.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Einzelwert dieses Ergebnisses.

### result.bounds.height

Höhe des Ergebnisrechtecks in Pixeln. Einzelwert dieses Ergebnisses.

### result.bounds.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Einzelwert dieses Ergebnisses.

### result.bounds.left

Linke Rechteckkante in Pixeln. Einzelwert dieses Ergebnisses.

### result.bounds.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Einzelwert dieses Ergebnisses.

### result.bounds.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Einzelwert dieses Ergebnisses.

### result.bounds.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Einzelwert dieses Ergebnisses.

### result.bounds.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Einzelwert dieses Ergebnisses.

### result.bounds.top

Obere Rechteckkante in Pixeln. Einzelwert dieses Ergebnisses.

### result.bounds.width

Breite des Ergebnisrechtecks in Pixeln. Einzelwert dieses Ergebnisses.

### result.bounds.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Einzelwert dieses Ergebnisses.

### result.bounds.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Einzelwert dieses Ergebnisses.

### result.capture_timestamp_utc

UTC-Zeitpunkt des erfassten Bildes; bei wiederverwendeten Frames bleibt der ursprüngliche Zeitpunkt erhalten. Einzelwert dieses Ergebnisses.

### result.frame_timestamp

Monotoner Stopwatch/QPC-Zeitstempel des Frames. 0 bedeutet unbekannt; dies ist kein Datum in Millisekunden seit 1970. Einzelwert dieses Ergebnisses.

### result.frame_version

Laufzeit-Identität des Frames innerhalb seines Aufnahmeproduzenten. Cache-/Zeigeraktualisierungen sind keine neuen Desktop-Bildversionen; nicht zwischen unabhängigen Kameras/Monitorquellen vergleichen. Einzelwert dieses Ergebnisses.

### result.has_image

Ob die Zwischenablage ein Bild enthält. Daraus folgt nicht automatisch, dass ein Bild als persistierbares JSON-Literal vorliegt. Einzelwert dieses Ergebnisses.

### result.image

Laufzeit-Bildobjekt für Bildverbraucher. Es wird nicht als Bitmap-Payload in der Jobdatei gespeichert. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.is_fresh

Ob die Aufnahme einen frischen Frame statt einer zwischengespeicherten Aufnahme geliefert hat. Einzelwert dieses Ergebnisses.

### result.offset

Pixelversatz der Bildquelle gegenüber dem globalen Desktop. Bei bildlokalen Punkten addieren, bevor sie global verwendet werden. Einzelwert dieses Ergebnisses.

### result.offset.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Einzelwert dieses Ergebnisses.

### result.offset.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Einzelwert dieses Ergebnisses.

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

### schema.settings.camera_id

Gespeicherte Geräte-ID der Kamera. Ein Anzeigename allein garantiert bei mehreren Geräten keine eindeutige Zuordnung.

### schema.settings.camera_name

Gespeicherter lesbarer Kameraname für Auswahl und Anzeige.

### schema.settings.frames_per_second

Gewünschte Anzahl Videobilder je Sekunde. Die tatsächliche Aufnahme hängt auch von der gelieferten Bildrate ab.

### schema.settings.height

Höhe des Kamerabilds in Pixeln für einen konkreten Aufnahmemodus, zum Beispiel 720. Im Editor den vollständigen angebotenen Modus wählen. Bei automatischer Qualität muss keine eigene Höhe eingetragen werden.

### schema.settings.pixel_format

Gewähltes Kamera-/Bildformat. Verwende nur ein vom Gerät beziehungsweise Editor angebotenes Format.

### schema.settings.quality_mode

Automatische, höchstmögliche oder konkret gewählte Kameraqualität. Unterstützte Auflösungen und Formate hängen vom Gerät ab.

### schema.settings.quality_mode.option.Automatic

Überlässt Kamera-/OCR-Moduswahl dem zuständigen Backend gemäß dieser Einstellung.

### schema.settings.quality_mode.option.HighestAvailable

Höchste angebotene Kameraqualität auswählen.

### schema.settings.quality_mode.option.Specific

Die konkret angegebenen Kameraformatwerte verwenden.

### schema.settings.width

Breite des Kamerabilds in Pixeln für einen konkreten Aufnahmemodus, zum Beispiel 1280. Im Editor wird die Breite zusammen mit Höhe, Bildrate und Pixelformat als angebotener Modus ausgewählt; bei automatischer Qualität bestimmt das Gerät den Modus.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "camera_capture",
  "name": "Kamerabild aufnehmen",
  "description": "Nimmt ein Einzelbild mit der ausgewählten Kamera auf und stellt es als Bildquelle für nachfolgende Steps bereit.",
  "category": "BildAufnehmen",
  "uiFieldIds": [
    "camera"
  ],
  "fields": [
    {
      "id": "camera",
      "name": "Kamera",
      "descriptor": {
        "Id": "camera",
        "LabelKey": "Ui.Step.Camera.Camera",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": {
          "camera_id": "",
          "camera_name": "",
          "quality_mode": "Automatic",
          "width": 0,
          "height": 0,
          "frames_per_second": 0,
          "pixel_format": ""
        },
        "DescriptionKey": null,
        "EditorHint": "camera-picker",
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
      "type": "CameraCaptureSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.camera_id",
      "name": "camera_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.camera_name",
      "name": "camera_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.frames_per_second",
      "name": "frames_per_second",
      "type": "Double",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.height",
      "name": "height",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.pixel_format",
      "name": "pixel_format",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.quality_mode",
      "name": "quality_mode",
      "type": "CameraQualityMode",
      "options": [
        "Automatic",
        "HighestAvailable",
        "Specific"
      ],
      "defaultValue": "Automatic"
    },
    {
      "id": "settings.width",
      "name": "width",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "CameraCaptureResult",
    "DisplayName": "CameraCaptureResult",
    "Properties": [
      {
        "Name": "Image",
        "DisplayName": "Image",
        "DataType": "Image",
        "Description": "Image, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "image",
        "EnumDisplayNames": null,
        "StableId": "image"
      },
      {
        "Name": "Bounds",
        "DisplayName": "Bounds",
        "DataType": "Rectangle",
        "Description": "Rectangle",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds",
        "EnumDisplayNames": null,
        "StableId": "bounds"
      },
      {
        "Name": "Bounds.X",
        "DisplayName": "Bounds / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.x",
        "EnumDisplayNames": null,
        "StableId": "bounds.x"
      },
      {
        "Name": "Bounds.Y",
        "DisplayName": "Bounds / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.y",
        "EnumDisplayNames": null,
        "StableId": "bounds.y"
      },
      {
        "Name": "Bounds.Width",
        "DisplayName": "Bounds / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.width",
        "EnumDisplayNames": null,
        "StableId": "bounds.width"
      },
      {
        "Name": "Bounds.Height",
        "DisplayName": "Bounds / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.height",
        "EnumDisplayNames": null,
        "StableId": "bounds.height"
      },
      {
        "Name": "Bounds.Left",
        "DisplayName": "Bounds / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.left",
        "EnumDisplayNames": null,
        "StableId": "bounds.left"
      },
      {
        "Name": "Bounds.Top",
        "DisplayName": "Bounds / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.top",
        "EnumDisplayNames": null,
        "StableId": "bounds.top"
      },
      {
        "Name": "Bounds.Right",
        "DisplayName": "Bounds / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.right",
        "EnumDisplayNames": null,
        "StableId": "bounds.right"
      },
      {
        "Name": "Bounds.Bottom",
        "DisplayName": "Bounds / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.bottom",
        "EnumDisplayNames": null,
        "StableId": "bounds.bottom"
      },
      {
        "Name": "Bounds.IsEmpty",
        "DisplayName": "Bounds / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.is_empty",
        "EnumDisplayNames": null,
        "StableId": "bounds.is_empty"
      },
      {
        "Name": "Bounds.Location",
        "DisplayName": "Bounds / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.location",
        "EnumDisplayNames": null,
        "StableId": "bounds.location"
      },
      {
        "Name": "Bounds.Location.X",
        "DisplayName": "Bounds / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.location.x",
        "EnumDisplayNames": null,
        "StableId": "bounds.location.x"
      },
      {
        "Name": "Bounds.Location.Y",
        "DisplayName": "Bounds / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.location.y",
        "EnumDisplayNames": null,
        "StableId": "bounds.location.y"
      },
      {
        "Name": "Bounds.Center",
        "DisplayName": "Bounds / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.center",
        "EnumDisplayNames": null,
        "StableId": "bounds.center"
      },
      {
        "Name": "Bounds.Center.X",
        "DisplayName": "Bounds / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.center.x",
        "EnumDisplayNames": null,
        "StableId": "bounds.center.x"
      },
      {
        "Name": "Bounds.Center.Y",
        "DisplayName": "Bounds / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.center.y",
        "EnumDisplayNames": null,
        "StableId": "bounds.center.y"
      },
      {
        "Name": "Offset",
        "DisplayName": "Offset",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "offset",
        "EnumDisplayNames": null,
        "StableId": "offset"
      },
      {
        "Name": "Offset.X",
        "DisplayName": "Offset / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "offset.x",
        "EnumDisplayNames": null,
        "StableId": "offset.x"
      },
      {
        "Name": "Offset.Y",
        "DisplayName": "Offset / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "offset.y",
        "EnumDisplayNames": null,
        "StableId": "offset.y"
      },
      {
        "Name": "IsFresh",
        "DisplayName": "Is Fresh",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_fresh",
        "EnumDisplayNames": null,
        "StableId": "is_fresh"
      },
      {
        "Name": "CaptureTimestampUtc",
        "DisplayName": "Capture Timestamp Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "capture_timestamp_utc",
        "EnumDisplayNames": null,
        "StableId": "capture_timestamp_utc"
      },
      {
        "Name": "FrameVersion",
        "DisplayName": "Frame Version",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "frame_version",
        "EnumDisplayNames": null,
        "StableId": "frame_version"
      },
      {
        "Name": "FrameTimestamp",
        "DisplayName": "Frame Timestamp",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "frame_timestamp",
        "EnumDisplayNames": null,
        "StableId": "frame_timestamp"
      },
      {
        "Name": "HasImage",
        "DisplayName": "Has Image",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "has_image",
        "EnumDisplayNames": null,
        "StableId": "has_image"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Image",
        "DisplayName": "Image",
        "Property": {
          "Name": "Image",
          "DisplayName": "Image",
          "DataType": "Image",
          "Description": "Image, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "image",
          "EnumDisplayNames": null,
          "StableId": "image"
        },
        "Children": []
      },
      {
        "Segment": "Bounds",
        "DisplayName": "Bounds",
        "Property": {
          "Name": "Bounds",
          "DisplayName": "Bounds",
          "DataType": "Rectangle",
          "Description": "Rectangle",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "bounds",
          "EnumDisplayNames": null,
          "StableId": "bounds"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "Bounds.X",
              "DisplayName": "Bounds / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.x",
              "EnumDisplayNames": null,
              "StableId": "bounds.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "Bounds.Y",
              "DisplayName": "Bounds / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.y",
              "EnumDisplayNames": null,
              "StableId": "bounds.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Width",
            "Property": {
              "Name": "Bounds.Width",
              "DisplayName": "Bounds / Width",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.width",
              "EnumDisplayNames": null,
              "StableId": "bounds.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Height",
            "Property": {
              "Name": "Bounds.Height",
              "DisplayName": "Bounds / Height",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.height",
              "EnumDisplayNames": null,
              "StableId": "bounds.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Left",
            "Property": {
              "Name": "Bounds.Left",
              "DisplayName": "Bounds / Left",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.left",
              "EnumDisplayNames": null,
              "StableId": "bounds.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Top",
            "Property": {
              "Name": "Bounds.Top",
              "DisplayName": "Bounds / Top",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.top",
              "EnumDisplayNames": null,
              "StableId": "bounds.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Right",
            "Property": {
              "Name": "Bounds.Right",
              "DisplayName": "Bounds / Right",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.right",
              "EnumDisplayNames": null,
              "StableId": "bounds.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Bottom",
            "Property": {
              "Name": "Bounds.Bottom",
              "DisplayName": "Bounds / Bottom",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.bottom",
              "EnumDisplayNames": null,
              "StableId": "bounds.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Is Empty",
            "Property": {
              "Name": "Bounds.IsEmpty",
              "DisplayName": "Bounds / Is Empty",
              "DataType": "Boolean",
              "Description": "Boolean",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.is_empty",
              "EnumDisplayNames": null,
              "StableId": "bounds.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Location",
            "Property": {
              "Name": "Bounds.Location",
              "DisplayName": "Bounds / Location",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.location",
              "EnumDisplayNames": null,
              "StableId": "bounds.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "Bounds.Location.X",
                  "DisplayName": "Bounds / Location / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.location.x",
                  "EnumDisplayNames": null,
                  "StableId": "bounds.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "Bounds.Location.Y",
                  "DisplayName": "Bounds / Location / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.location.y",
                  "EnumDisplayNames": null,
                  "StableId": "bounds.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "Bounds.Center",
              "DisplayName": "Bounds / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.center",
              "EnumDisplayNames": null,
              "StableId": "bounds.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "Bounds.Center.X",
                  "DisplayName": "Bounds / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "bounds.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "Bounds.Center.Y",
                  "DisplayName": "Bounds / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "bounds.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "Offset",
        "DisplayName": "Offset",
        "Property": {
          "Name": "Offset",
          "DisplayName": "Offset",
          "DataType": "Point",
          "Description": "Point",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "offset",
          "EnumDisplayNames": null,
          "StableId": "offset"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "Offset.X",
              "DisplayName": "Offset / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "offset.x",
              "EnumDisplayNames": null,
              "StableId": "offset.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "Offset.Y",
              "DisplayName": "Offset / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "offset.y",
              "EnumDisplayNames": null,
              "StableId": "offset.y"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "IsFresh",
        "DisplayName": "Is Fresh",
        "Property": {
          "Name": "IsFresh",
          "DisplayName": "Is Fresh",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_fresh",
          "EnumDisplayNames": null,
          "StableId": "is_fresh"
        },
        "Children": []
      },
      {
        "Segment": "CaptureTimestampUtc",
        "DisplayName": "Capture Timestamp Utc",
        "Property": {
          "Name": "CaptureTimestampUtc",
          "DisplayName": "Capture Timestamp Utc",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "capture_timestamp_utc",
          "EnumDisplayNames": null,
          "StableId": "capture_timestamp_utc"
        },
        "Children": []
      },
      {
        "Segment": "FrameVersion",
        "DisplayName": "Frame Version",
        "Property": {
          "Name": "FrameVersion",
          "DisplayName": "Frame Version",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "frame_version",
          "EnumDisplayNames": null,
          "StableId": "frame_version"
        },
        "Children": []
      },
      {
        "Segment": "FrameTimestamp",
        "DisplayName": "Frame Timestamp",
        "Property": {
          "Name": "FrameTimestamp",
          "DisplayName": "Frame Timestamp",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "frame_timestamp",
          "EnumDisplayNames": null,
          "StableId": "frame_timestamp"
        },
        "Children": []
      },
      {
        "Segment": "HasImage",
        "DisplayName": "Has Image",
        "Property": {
          "Name": "HasImage",
          "DisplayName": "Has Image",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "has_image",
          "EnumDisplayNames": null,
          "StableId": "has_image"
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
  "type": "camera_capture",
  "settings": {
    "camera_id": "",
    "camera_name": "",
    "quality_mode": "Automatic",
    "width": 0,
    "height": 0,
    "frames_per_second": 0,
    "pixel_format": ""
  },
  "id": "f86a035d-110f-72d1-11bd-b0e1f4d55409",
  "inputs": {},
  "is_enabled": true
}
```

