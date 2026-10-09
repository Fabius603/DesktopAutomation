## Steps: Eingaben blockieren

ID: block_input
Website: /doku/steps/block_input/

Abbildung: [Detailansicht: Eingaben blockieren. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-block-input.detail.png)

1. Automatisch freigeben nach (s): Maximale Sicherheitsdauer der Eingabesperre in Sekunden. Plane zusätzlich eine ausdrückliche Freigabe beziehungsweise reguläres Job-Aufräumen.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Blockiere Eingaben für fünf Sekunden, sende ein Makro und gib sie anschließend mit unblock_input frei.

### field.safety_timeout_seconds

Maximale Sicherheitsdauer der Eingabesperre in Sekunden. Plane zusätzlich eine ausdrückliche Freigabe beziehungsweise reguläres Job-Aufräumen.

### purpose

Sperre physische Maus- und Tastatureingaben für eine kurze automatisierte Eingabesequenz. Setze eine endliche Sicherheitsfrist und plane die Freigabe in der Endphase. Die Sperre wird durch den Job besessen und beim Aufräumen freigegeben; sie ist keine Sperre einzelner Programmfenster.

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

### schema.settings.safety_timeout_seconds

Maximale Sicherheitsdauer der Eingabesperre in Sekunden. Plane zusätzlich eine ausdrückliche Freigabe beziehungsweise reguläres Job-Aufräumen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "block_input",
  "name": "Eingaben blockieren",
  "description": "Blockiert physische Maus- und Tastatureingaben vorübergehend.",
  "category": "AblaufSteuern",
  "uiFieldIds": [
    "safety_timeout_seconds"
  ],
  "fields": [
    {
      "id": "safety_timeout_seconds",
      "name": "Automatisch freigeben nach (s)",
      "descriptor": {
        "Id": "safety_timeout_seconds",
        "LabelKey": "Ui.Step.Settings.SafetyTimeoutSeconds",
        "ValueKind": "Integer",
        "Required": true,
        "DefaultValue": 30,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": 1,
          "Maximum": 3600,
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
      "type": "BlockInputSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.safety_timeout_seconds",
      "name": "safety_timeout_seconds",
      "type": "Int32",
      "options": [],
      "defaultValue": 30
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "InputControlResult",
    "DisplayName": "InputControlResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "block_input",
  "settings": {
    "safety_timeout_seconds": 30
  },
  "id": "c79b421d-88ca-12f3-f47f-cb78a41eab44",
  "inputs": {},
  "is_enabled": true
}
```

