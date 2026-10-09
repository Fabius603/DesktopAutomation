## Steps: Punkt anklicken

ID: klick_on_point
Website: /doku/steps/klick_on_point/

Abbildung: [Detailansicht: Punkt anklicken. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-klick-on-point.detail.png)

1. Punktquelle: Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.
2. Maustaste: left, right oder middle sendet die entsprechende Maustaste; none führt nur die Mausbewegung aus.
3. Versatz X (px): Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.
4. Versatz Y (px): Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.
5. Zeitlimit (ms): Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.
6. Doppelklick: Sendet einen Doppelklick statt eines Einzelklicks, sofern eine Maustaste gewählt ist.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Ein Erkennungs-Step liefert point; der Klick-Step verwendet diese Punktquelle, linke Taste und optional offset_x=5.

### field.click_type

left, right oder middle sendet die entsprechende Maustaste; none führt nur die Mausbewegung aus.

### field.click_type.option.left

Linke Maustaste.

### field.click_type.option.middle

Mittlere Maustaste.

### field.click_type.option.none

Keine Maustaste senden; nur die Bewegung ausführen.

### field.click_type.option.right

Rechte Maustaste.

### field.double_click

Sendet einen Doppelklick statt eines Einzelklicks, sofern eine Maustaste gewählt ist.

### field.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### field.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### field.points_source

Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.

### field.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### input.points

Punkt-/Erkennungsergebnis. Fehlende erforderliche Werte führen zum Fehler. Bei kompatiblen Listen wird der erste Wert verwendet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Wähle eine kompatible Punktquelle und optional einen Pixelversatz. Bei einer Liste wird der erste passende Punkt verwendet, nicht jedes Listenelement angeklickt. Wähle eine Maustaste oder „none“ für reine Bewegung; ein Doppelklick ist eine zusätzliche Eingabeaktion.

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

### schema.settings.click_type

left, right oder middle sendet die entsprechende Maustaste; none führt nur die Mausbewegung aus.

### schema.settings.double_click

Sendet einen Doppelklick statt eines Einzelklicks, sofern eine Maustaste gewählt ist.

### schema.settings.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.points_source

Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.

### schema.settings.points_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.points_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.points_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.points_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.points_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.points_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.points_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.points_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.points_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "klick_on_point",
  "name": "Punkt anklicken",
  "description": "Klickt auf einen direkt angegebenen oder referenzierten Punkt.",
  "category": "MausTastatur",
  "uiFieldIds": [
    "points_source",
    "click_type",
    "offset_x",
    "offset_y",
    "timeout_ms",
    "double_click"
  ],
  "fields": [
    {
      "id": "points_source",
      "name": "Punktquelle",
      "descriptor": {
        "Id": "points_source",
        "LabelKey": "Ui.Step.Settings.PointSource",
        "ValueKind": "ResultBinding",
        "Required": true,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "value-reference-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": "points",
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
      "id": "click_type",
      "name": "Maustaste",
      "descriptor": {
        "Id": "click_type",
        "LabelKey": "Ui.Step.Settings.ClickType",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "left",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "left",
            "right",
            "middle",
            "none"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 1,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "left",
            "LabelKey": "Enum.MouseClickType.Left",
            "DisplayName": null
          },
          {
            "Value": "right",
            "LabelKey": "Enum.MouseClickType.Right",
            "DisplayName": null
          },
          {
            "Value": "middle",
            "LabelKey": "Enum.MouseClickType.Middle",
            "DisplayName": null
          },
          {
            "Value": "none",
            "LabelKey": "Enum.MouseClickType.None",
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
      "id": "timeout_ms",
      "name": "Zeitlimit (ms)",
      "descriptor": {
        "Id": "timeout_ms",
        "LabelKey": "Ui.Step.Settings.TimeoutMs",
        "ValueKind": "Duration",
        "Required": false,
        "DefaultValue": 0,
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
      "id": "double_click",
      "name": "Doppelklick",
      "descriptor": {
        "Id": "double_click",
        "LabelKey": "Ui.Step.Settings.DoubleClick",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": false,
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
      "type": "KlickOnPointSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.click_type",
      "name": "click_type",
      "type": "String",
      "options": [],
      "defaultValue": "left"
    },
    {
      "id": "settings.double_click",
      "name": "double_click",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
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
      "id": "settings.points_source",
      "name": "points_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.points_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.points_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.timeout_ms",
      "name": "timeout_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    }
  ],
  "inputs": [
    {
      "Key": "points",
      "Required": true,
      "MissingValuePolicy": "FailStep",
      "CollectionConsumption": "FirstValue",
      "AcceptedShapes": [
        {
          "ValueKind": "Point",
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
        "local_value"
      ],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "KlickOnPointResult",
    "DisplayName": "KlickOnPointResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "klick_on_point",
  "settings": {
    "double_click": false,
    "click_type": "left",
    "timeout_ms": 0,
    "offset_x": 0,
    "offset_y": 0,
    "points_source": {
      "provider_id": "",
      "source_id": ""
    }
  },
  "id": "12ba351e-b0cc-6cb8-3277-7dcc5cc6353f",
  "inputs": {},
  "is_enabled": true
}
```

