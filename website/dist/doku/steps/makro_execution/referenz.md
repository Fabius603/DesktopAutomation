## Steps: Makro ausführen

ID: makro_execution
Website: /doku/steps/makro_execution/

Abbildung: [Ein Makro-Step verweist auf das Beispielmakro Notiz eingeben; im Beispieljob ist dieser Step deaktiviert. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/step-makro.detail.png)

1. Makro: Im Editor ein vorhandenes Makro wählen; seine Eingaben wirken auf den aktiven Desktop und das aktive Fenster.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Der Unterlagenablauf zeigt das optionale Makro „Notiz eingeben“. Fokussiere einen leeren Editor und teste das Makro zuerst separat; der illustrierte Job-Step ist anfangs deaktiviert.

### field.macro

Im Editor ein vorhandenes Makro wählen; seine Eingaben wirken auf den aktiven Desktop und das aktive Fenster.

### purpose

Wähle ein gespeichertes Makro per ID und führe dessen Befehle im aktuellen Desktop-Kontext aus. Es verwendet das gerade aktive Fenster. Stelle deshalb vor dem Makro über Prozess- und Fenster-Steps sicher, dass die Eingabe im vorgesehenen Programm landet.

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

### schema.settings.makro_id

GUID des auszuführenden Makros. Beim Duplizieren eines Makros die Referenzen auf dessen neue ID anpassen.

### schema.settings.makro_name

Anzeigename der Makroreferenz; die stabile ID bestimmt das Ziel.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "makro_execution",
  "name": "Makro ausführen",
  "description": "Führt ein zuvor aufgezeichnetes Makro aus.",
  "category": "MausTastatur",
  "uiFieldIds": [
    "macro"
  ],
  "fields": [
    {
      "id": "macro",
      "name": "Makro",
      "descriptor": {
        "Id": "macro",
        "LabelKey": "Ui.Step.Settings.Macro",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "macro-picker",
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
      "type": "MakroExecutionSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.makro_id",
      "name": "makro_id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.makro_name",
      "name": "makro_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "MakroExecutionResult",
    "DisplayName": "MakroExecutionResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "makro_execution",
  "settings": {
    "makro_name": "",
    "makro_id": null
  },
  "id": "9fd9f44c-6450-a88e-c624-8a1b7e809975",
  "inputs": {},
  "is_enabled": true
}
```

