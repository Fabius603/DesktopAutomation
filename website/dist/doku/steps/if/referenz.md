## Steps: Wenn

ID: if
Website: /doku/steps/if/

Abbildung: [Der Wenn-Block vergleicht das Ergebnis der Benutzerauswahl mit dem Wert rechnung. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/step-bedingung.detail.png)

1. Bedingungen: Geordnete Bedingungsliste des Wenn-Zweigs. Jede Bedingung benötigt eine passende Ergebnisquelle und den gewählten Operator.
### errors

Ungültige Ergebnisreferenzen, inkompatible Vergleichstypen und unvollständige Blockstruktur werden zurückgewiesen. Rohwerte und Secrets gehören nicht in Diagnosemeldungen; der Verlauf beschreibt Operator und Entscheidungsstatus.

### example

Der Beispieljob „Arbeitsunterlagen vorbereiten“ vergleicht selected_value mit „rechnung“ und führt nur den passenden Kopierzweig aus.

### field.conditions

Geordnete Bedingungsliste des Wenn-Zweigs. Jede Bedingung benötigt eine passende Ergebnisquelle und den gewählten Operator.

### purpose

Beginne einen bedingten Block und wähle Ergebniswert, Operator und gegebenenfalls Vergleichswert. „Alle“ verknüpft Bedingungen mit UND, „Mindestens eine“ mit ODER. Bedingungen verwenden typisierte Werte; ein noch nicht ausgeführter oder inkompatibler Quell-Step darf nicht als vorhandenes Ergebnis angenommen werden.

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

### schema.settings.conditions

Geordnete Bedingungsliste des Wenn-Zweigs. Jede Bedingung benötigt eine passende Ergebnisquelle und den gewählten Operator.

### schema.settings.conditions.comparison

Rechte Seite des Vergleichs: Literal oder ein weiteres Job-Ergebnis, abhängig von kind.

### schema.settings.conditions.comparison.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.conditions.comparison.items.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.conditions.comparison.items.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.conditions.comparison.items.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.conditions.comparison.items.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.conditions.comparison.items.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.conditions.comparison.items.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.conditions.comparison.items.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.conditions.comparison.items.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.conditions.comparison.items.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.conditions.comparison.kind

Literal verwendet den direkt konfigurierten Vergleichswert; JobResult verwendet die zweite typisierte Ergebnisreferenz. Dieses Enum ist kein type/$type-Discriminator.

### schema.settings.conditions.comparison.kind.option.JobResult

Verwendet einen kompatiblen Ergebniswert eines Job-Steps.

### schema.settings.conditions.comparison.kind.option.Literal

Verwendet einen direkt gespeicherten Vergleichswert.

### schema.settings.conditions.comparison.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.conditions.comparison.members.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.conditions.comparison.members.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.conditions.comparison.members.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.conditions.comparison.members.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.conditions.comparison.members.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.conditions.comparison.members.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.conditions.comparison.members.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.conditions.comparison.members.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.conditions.comparison.members.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.conditions.comparison.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.conditions.comparison.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.conditions.comparison.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.conditions.comparison.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.conditions.comparison.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.conditions.comparison.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.conditions.comparison.value

Gespeicherter Literal- oder Vergleichswert. Sein JSON-Typ muss zum angegebenen value_kind und zur Kardinalität passen; Zahlen und Booleans nicht versehentlich als Text speichern.

### schema.settings.conditions.comparison.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.conditions.comparison_value

Direkt eingetragener Vergleichswert. Sein Typ muss zum linken Ergebnis und Operator passen.

### schema.settings.conditions.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.conditions.items.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.conditions.items.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.conditions.items.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.conditions.items.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.conditions.items.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.conditions.items.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.conditions.items.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.conditions.items.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.conditions.items.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.conditions.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.conditions.members.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.conditions.members.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.conditions.members.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.conditions.members.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.conditions.members.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.conditions.members.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.conditions.members.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.conditions.members.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.conditions.members.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.conditions.operator

Vergleichsoperator. Wahrheits-/Leerheitsprüfungen benötigen keinen zusätzlichen Vergleichstext; andere Operatoren verlangen kompatible Werte.

### schema.settings.conditions.operator.option.Contains

Prüft, ob Text den angegebenen Vergleichstext enthält.

### schema.settings.conditions.operator.option.Equals

Prüft typkompatible Werte auf Gleichheit.

### schema.settings.conditions.operator.option.GreaterThan

Linker Wert muss größer sein als der rechte Vergleichswert.

### schema.settings.conditions.operator.option.GreaterThanOrEqual

Linker Wert muss größer oder gleich dem Vergleichswert sein.

### schema.settings.conditions.operator.option.IsEmpty

Prüft einen vorhandenen Wert auf Leerheit; ungültige Referenzen werden dadurch nicht gültig.

### schema.settings.conditions.operator.option.IsFalse

Prüft einen booleschen Wert auf false, ohne rechten Vergleichswert.

### schema.settings.conditions.operator.option.IsNotEmpty

Prüft einen vorhandenen Wert auf Nicht-Leerheit.

### schema.settings.conditions.operator.option.IsTrue

Prüft einen booleschen Wert auf true, ohne rechten Vergleichswert.

### schema.settings.conditions.operator.option.LessThan

Linker Wert muss kleiner sein als der rechte Vergleichswert.

### schema.settings.conditions.operator.option.LessThanOrEqual

Linker Wert muss kleiner oder gleich dem Vergleichswert sein.

### schema.settings.conditions.operator.option.NotEquals

Prüft typkompatible Werte auf Ungleichheit.

### schema.settings.conditions.operator.option.StartsWith

Prüft, ob Text mit dem angegebenen Vergleichstext beginnt.

### schema.settings.conditions.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.conditions.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.conditions.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.conditions.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.conditions.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.conditions.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.conditions.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.match_mode

All verlangt alle Bedingungen; Any lässt eine passende Bedingung genügen.

### schema.settings.match_mode.option.All

Alle betrachteten Bedingungen beziehungsweise Punkte müssen passen.

### schema.settings.match_mode.option.Any

Mindestens eine betrachtete Bedingung beziehungsweise ein Punkt muss passen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "if",
  "name": "Wenn",
  "description": "Beginnt einen bedingten Block.",
  "category": "AblaufSteuern",
  "uiFieldIds": [
    "conditions"
  ],
  "fields": [
    {
      "id": "conditions",
      "name": "Bedingungen",
      "descriptor": {
        "Id": "conditions",
        "LabelKey": "Ui.Step.Settings.Evaluation",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": {
          "match_mode": "All",
          "conditions": []
        },
        "DescriptionKey": null,
        "EditorHint": "condition-editor",
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
      "type": "IfConditionSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions",
      "name": "conditions",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison",
      "name": "comparison",
      "type": "ComparisonOperand",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.items.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.kind",
      "name": "kind",
      "type": "ComparisonOperandKind",
      "options": [
        "Literal",
        "JobResult"
      ],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.members.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.value",
      "name": "value",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.comparison_value",
      "name": "comparison_value",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.items.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.members.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.operator",
      "name": "operator",
      "type": "ConditionOperator",
      "options": [
        "IsTrue",
        "IsFalse",
        "Equals",
        "NotEquals",
        "GreaterThan",
        "LessThan",
        "GreaterThanOrEqual",
        "LessThanOrEqual",
        "Contains",
        "StartsWith",
        "IsEmpty",
        "IsNotEmpty"
      ],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.conditions.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.match_mode",
      "name": "match_mode",
      "type": "ConditionMatchMode",
      "options": [
        "All",
        "Any"
      ],
      "defaultValue": "All"
    }
  ],
  "inputs": [],
  "result": null
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "if",
  "settings": {
    "match_mode": "All",
    "conditions": []
  },
  "CanBeDisabled": false,
  "id": "d63699e5-2497-796d-bcbd-666b1c4a79ab",
  "inputs": {},
  "is_enabled": true
}
```

