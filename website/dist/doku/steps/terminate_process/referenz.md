## Steps: Prozess beenden

ID: terminate_process
Website: /doku/steps/terminate_process/

Abbildung: [Detailansicht: Prozess beenden. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-terminate-process.detail.png)

1. Prozessreferenz: Im Editor eine kompatible Prozessreferenz wählen. Suchkriterien einer älteren settings.target-Konfiguration sind kein Ersatz für eine beliebige Textquelle im typisierten Prozesseingang.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Eine zuvor über get_process ermittelte Testprogramm-Instanz beenden; keine ungesicherten Dokumente im Testprogramm offen lassen.

### field.process_target

Im Editor eine kompatible Prozessreferenz wählen. Suchkriterien einer älteren settings.target-Konfiguration sind kein Ersatz für eine beliebige Textquelle im typisierten Prozesseingang.

### input.process

konkrete Prozessreferenz. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Beende die gewählte Programminstanz beziehungsweise passende Instanzen der konfigurierten Suche. Prüfe Prozessreferenz und Titel sorgfältig: das Beenden eines Programms kann ungespeicherte Arbeit verlieren. Eine nicht mehr vorhandene Instanz ist von einem Berechtigungsfehler zu unterscheiden.

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

### schema.settings.target

Prozess-Zielkonfiguration aus Name/Pfad/Titel oder einer konkreten Prozessreferenz. Die konkrete Referenz schützt vor Verwechslung gleichnamiger Instanzen.

### schema.settings.target.executable_path

Pfad zur ausführbaren Datei. Beim Start muss er zu einem startbaren Programm führen; bei einer Suche grenzt er die Programminstanz ein.

### schema.settings.target.process_name

Name des gesuchten Prozesses, zum Beispiel notepad. Ein Name kann mehrere Instanzen treffen; eine konkrete Prozessreferenz grenzt gezielt ein.

### schema.settings.target.process_source

Referenz auf eine konkrete laufende Programminstanz, üblicherweise process aus get_process oder start_process.

### schema.settings.target.process_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.target.process_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.target.process_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.target.process_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.target.process_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.target.process_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.target.process_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.target.process_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.target.process_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.target.window_title_contains

Optionaler Teil des Fenstertitels zum Eingrenzen mehrerer Fenster. Ein leerer Text setzt diesen Filter nicht.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "terminate_process",
  "name": "Prozess beenden",
  "description": "Beendet laufende Prozesse anhand einer Prozessreferenz oder ihres Namens und optional ihres Fenstertitels.",
  "category": "ProgrammeFenster",
  "uiFieldIds": [
    "process_target"
  ],
  "fields": [
    {
      "id": "process_target",
      "name": "Prozessreferenz",
      "descriptor": {
        "Id": "process_target",
        "LabelKey": "Ui.Step.Settings.ProcessSource",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "process-target-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": "process",
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
      "type": "TerminateProcessSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target",
      "name": "target",
      "type": "ProcessTargetSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.executable_path",
      "name": "executable_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_name",
      "name": "process_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_source",
      "name": "process_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.process_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target.process_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target.window_title_contains",
      "name": "window_title_contains",
      "type": "String",
      "options": [],
      "defaultValue": ""
    }
  ],
  "inputs": [
    {
      "Key": "process",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "NotApplicable",
      "AcceptedShapes": [
        {
          "ValueKind": "ProcessReference",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        }
      ],
      "AllowedProviderIds": [
        "job_variable",
        "step_result"
      ],
      "LegacyAllowedProviderIds": [],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "TerminateProcessResult",
    "DisplayName": "TerminateProcessResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "terminate_process",
  "settings": {
    "target": {
      "process_source": {
        "provider_id": "",
        "source_id": ""
      },
      "process_name": "",
      "executable_path": "",
      "window_title_contains": ""
    }
  },
  "id": "1a0a37d7-cbd4-4080-4162-27be3efcf1d4",
  "inputs": {},
  "is_enabled": true
}
```

