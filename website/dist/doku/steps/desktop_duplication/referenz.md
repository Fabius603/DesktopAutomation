## Steps: Bildschirm aufnehmen

ID: desktop_duplication
Website: /doku/steps/desktop_duplication/

Abbildung: [Detailansicht: Bildschirm aufnehmen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-desktop-duplication.detail.png)

1. Monitor: Nullbasierter Monitorindex der Bildschirmaufnahme. Auf einem anderen Rechner kann die Monitorreihenfolge abweichen.
2. Mauszeiger aufnehmen: Nimmt den Mauszeiger in die Bildschirmaufnahme auf. Für Bildvergleiche kann der Zeiger störende zusätzliche Pixel verursachen.
3. Auf neues Bild warten: Wartet auf einen neuen Desktop-Frame statt sofort den zuletzt verfügbaren Frame zu übernehmen.
4. Zeitlimit (ms): Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.
5. Bei Zeitlimit letztes Bild verwenden: Erlaubt nach Ablauf des Aufnahmezeitlimits den zuletzt verfügbaren Frame. Prüfe dessen is_fresh, bevor eine zeitkritische Folgeaktion ausgeführt wird.
6. Monitor-Kennung: Optional gespeicherte Monitor-Gerätekennung zur Wiedererkennung des gewünschten Bildschirms. Bei Umbau der Monitoranordnung prüfen.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Monitor 0 aufnehmen → Bild speichern. Bei zeitkritischer Erkennung auf ein neues Bild warten und den Rückfall auf Cache deaktivieren.

### field.allow_cached_fallback

Erlaubt nach Ablauf des Aufnahmezeitlimits den zuletzt verfügbaren Frame. Prüfe dessen is_fresh, bevor eine zeitkritische Folgeaktion ausgeführt wird.

### field.capture_cursor

Nimmt den Mauszeiger in die Bildschirmaufnahme auf. Für Bildvergleiche kann der Zeiger störende zusätzliche Pixel verursachen.

### field.desktop_idx

Nullbasierter Monitorindex der Bildschirmaufnahme. Auf einem anderen Rechner kann die Monitorreihenfolge abweichen.

### field.monitor_device_name

Optional gespeicherte Monitor-Gerätekennung zur Wiedererkennung des gewünschten Bildschirms. Bei Umbau der Monitoranordnung prüfen.

### field.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### field.wait_for_new_frame

Wartet auf einen neuen Desktop-Frame statt sofort den zuletzt verfügbaren Frame zu übernehmen.

### purpose

Wähle einen Monitor als Bildquelle. Optional wartet die Aufnahme auf ein neues Desktop-Bild; beim Zeitlimit kannst du den letzten verfügbaren Frame zulassen. Prüfe is_fresh und frame_timestamp, wenn eine spätere Mausaktion nur auf neue Bilder reagieren soll.

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

### schema.settings.allow_cached_fallback

Erlaubt nach Ablauf des Aufnahmezeitlimits den zuletzt verfügbaren Frame. Prüfe dessen is_fresh, bevor eine zeitkritische Folgeaktion ausgeführt wird.

### schema.settings.capture_cursor

Nimmt den Mauszeiger in die Bildschirmaufnahme auf. Für Bildvergleiche kann der Zeiger störende zusätzliche Pixel verursachen.

### schema.settings.desktop_idx

Nullbasierter Monitorindex der Bildschirmaufnahme. Auf einem anderen Rechner kann die Monitorreihenfolge abweichen.

### schema.settings.monitor_device_name

Optional gespeicherte Monitor-Gerätekennung zur Wiedererkennung des gewünschten Bildschirms. Bei Umbau der Monitoranordnung prüfen.

### schema.settings.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### schema.settings.wait_for_new_frame

Wartet auf einen neuen Desktop-Frame statt sofort den zuletzt verfügbaren Frame zu übernehmen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "desktop_duplication",
  "name": "Bildschirm aufnehmen",
  "description": "Nimmt einen Screenshot des gewählten Monitors auf und stellt ihn als Bildquelle bereit.",
  "category": "BildAufnehmen",
  "uiFieldIds": [
    "desktop_idx",
    "capture_cursor",
    "wait_for_new_frame",
    "timeout_ms",
    "allow_cached_fallback",
    "monitor_device_name"
  ],
  "fields": [
    {
      "id": "desktop_idx",
      "name": "Monitor",
      "descriptor": {
        "Id": "desktop_idx",
        "LabelKey": "Ui.Step.Settings.DesktopIndex",
        "ValueKind": "Integer",
        "Required": true,
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
        "MonitorDeviceNameFieldId": "monitor_device_name"
      }
    },
    {
      "id": "capture_cursor",
      "name": "Mauszeiger aufnehmen",
      "descriptor": {
        "Id": "capture_cursor",
        "LabelKey": "Ui.Step.Settings.CaptureMousePointer",
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
      "id": "wait_for_new_frame",
      "name": "Auf neues Bild warten",
      "descriptor": {
        "Id": "wait_for_new_frame",
        "LabelKey": "Ui.Step.Capture.WaitForNewFrame",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": true,
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
    },
    {
      "id": "timeout_ms",
      "name": "Zeitlimit (ms)",
      "descriptor": {
        "Id": "timeout_ms",
        "LabelKey": "Ui.Step.Capture.Timeout",
        "ValueKind": "Integer",
        "Required": true,
        "DefaultValue": 250,
        "DescriptionKey": "Ui.Step.Capture.TimeoutHelp",
        "EditorHint": null,
        "Constraints": {
          "Minimum": 1,
          "Maximum": 60000,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": false,
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
      "id": "allow_cached_fallback",
      "name": "Bei Zeitlimit letztes Bild verwenden",
      "descriptor": {
        "Id": "allow_cached_fallback",
        "LabelKey": "Ui.Step.Capture.AllowCachedFallback",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": true,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
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
      "id": "monitor_device_name",
      "name": "Monitor-Kennung",
      "descriptor": {
        "Id": "monitor_device_name",
        "LabelKey": "Ui.Step.Capture.MonitorIdentity",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": "Ui.Step.Capture.MonitorIdentityHelp",
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
      "type": "DesktopDuplicationSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.allow_cached_fallback",
      "name": "allow_cached_fallback",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    },
    {
      "id": "settings.capture_cursor",
      "name": "capture_cursor",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
    },
    {
      "id": "settings.desktop_idx",
      "name": "desktop_idx",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.monitor_device_name",
      "name": "monitor_device_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.timeout_ms",
      "name": "timeout_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 250
    },
    {
      "id": "settings.wait_for_new_frame",
      "name": "wait_for_new_frame",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "DesktopDuplicationResult",
    "DisplayName": "DesktopDuplicationResult",
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
  "type": "desktop_duplication",
  "settings": {
    "desktop_idx": 0,
    "capture_cursor": false,
    "monitor_device_name": "",
    "wait_for_new_frame": true,
    "timeout_ms": 250,
    "allow_cached_fallback": true
  },
  "id": "ffa47583-1458-1a09-c1e7-2592222fbe7b",
  "inputs": {},
  "is_enabled": true
}
```

