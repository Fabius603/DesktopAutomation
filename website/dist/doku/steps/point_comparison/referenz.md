## Steps: Punkte vergleichen

ID: point_comparison
Website: /doku/steps/point_comparison/

Abbildung: [Detailansicht: Punkte vergleichen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-point-comparison.detail.png)

1. Modus: Offset vergleicht Punkte mit einer Referenz und erlaubten X-/Y-Abweichungen. Expression wertet die konfigurierten Achsenbedingungen aus. Nur die Einstellungen des gewählten Modus sind aktiv.
2. Erforderliche Treffer: Legt fest, ob alle geprüften Punkte/Bedingungen oder wenigstens einer/eine die Prüfung bestehen müssen.
3. ZU PRÜFENDE PUNKTE: Füge die zu prüfenden Punkte hinzu. Wähle pro Eintrag eine manuelle X-/Y-Koordinate oder die kompatible Punktquelle eines vorherigen Steps. Die Auswahl bestimmt, welche Unterfelder erscheinen.
4. Referenzpunkt: 
5. Versatz X (px): Erlaubte horizontale Abweichung vom Referenzpunkt in Pixeln. Mit 10 besteht ein Punkt die X-Prüfung, wenn seine X-Koordinate höchstens 10 px von der Referenz abweicht.
6. Versatz Y (px): Erlaubte vertikale Abweichung vom Referenzpunkt in Pixeln. Mit 10 besteht ein Punkt die Y-Prüfung, wenn seine Y-Koordinate höchstens 10 px von der Referenz abweicht.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Prüfe, ob eine Erkennung in X höchstens zehn Pixel vom Referenzpunkt abweicht, bevor ein nachfolgender Step ausgeführt wird.

### field.combine_mode

Verknüpft Achsenbedingungen mit And (UND) oder Or (ODER).

### field.combine_mode.option.And

Bedingungen mit logischem UND verknüpfen.

### field.combine_mode.option.Or

Bedingungen mit logischem ODER verknüpfen.

### field.expressions

Liste von Achsenbedingungen. Jede Bedingung beschreibt Achse, Operator und Vergleichsausdruck.

### field.match_requirement

Legt fest, ob alle geprüften Punkte/Bedingungen oder wenigstens einer/eine die Prüfung bestehen müssen.

### field.match_requirement.option.All

Alle betrachteten Bedingungen beziehungsweise Punkte müssen passen.

### field.match_requirement.option.Any

Mindestens eine betrachtete Bedingung beziehungsweise ein Punkt muss passen.

### field.mode

Offset vergleicht Punkte mit einer Referenz und erlaubten X-/Y-Abweichungen. Expression wertet die konfigurierten Achsenbedingungen aus. Nur die Einstellungen des gewählten Modus sind aktiv.

### field.mode.option.Expression

Punkte anhand expliziter Achsenbedingungen vergleichen.

### field.mode.option.Offset

Punkte anhand einer Referenzposition und erlaubter Versätze vergleichen.

### field.offset_x

Erlaubte horizontale Abweichung vom Referenzpunkt in Pixeln. Mit 10 besteht ein Punkt die X-Prüfung, wenn seine X-Koordinate höchstens 10 px von der Referenz abweicht.

### field.offset_y

Erlaubte vertikale Abweichung vom Referenzpunkt in Pixeln. Mit 10 besteht ein Punkt die Y-Prüfung, wenn seine Y-Koordinate höchstens 10 px von der Referenz abweicht.

### field.points

Füge die zu prüfenden Punkte hinzu. Wähle pro Eintrag eine manuelle X-/Y-Koordinate oder die kompatible Punktquelle eines vorherigen Steps. Die Auswahl bestimmt, welche Unterfelder erscheinen.

### field.reference_points_source

Typisierte Punktquelle als Referenz für den Punktvergleich.

### field.reference_source

Manual verwendet die direkt angegebenen Referenzkoordinaten; JobResult verwendet reference_points_source.

### field.reference_source.option.JobResult

Verwendet einen kompatiblen Ergebniswert eines Job-Steps.

### field.reference_source.option.Manual

Verwendet die manuell angegebenen Koordinaten.

### field.reference_x

Horizontale Referenzkoordinate für den Offsetvergleich in Pixeln.

### field.reference_y

Vertikale Referenzkoordinate für den Offsetvergleich in Pixeln.

### input.points

Punkt-/Erkennungsergebnis. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Alle kompatiblen Listeneinträge werden verarbeitet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Vergleiche eine oder mehrere Punktquellen entweder mit einem Referenzpunkt und erlaubten Versätzen oder mit Achsenbedingungen. „Alle“ und „Mindestens eine“ bestimmen, wie mehrere Prüfungen zusammengefasst werden. Der Vergleich erzeugt ein Ergebnis; er bewegt die Maus nicht.

### result.match_count

Anzahl passender Prozesse oder Merkmalszuordnungen im konkreten Ergebnis; der Ergebnis-Typ bestimmt, was gezählt wird. Einzelwert dieses Ergebnisses.

### result.matches

Einzelne OCR-/Punktvergleichstreffer mit ihren Detailwerten. Der Ergebnis-Typ bestimmt die Elementstruktur. Einzelwert dieses Ergebnisses.

### result.total_count

Gesamtzahl der im konkreten Abfrageergebnis gelieferten Einträge. Einzelwert dieses Ergebnisses.

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

### schema.settings.expression_settings

Einstellungen für den Punktvergleich mittels Achsenbedingungen und deren Verknüpfung.

### schema.settings.expression_settings.combine_mode

Verknüpft Achsenbedingungen mit And (UND) oder Or (ODER).

### schema.settings.expression_settings.combine_mode.option.And

Bedingungen mit logischem UND verknüpfen.

### schema.settings.expression_settings.combine_mode.option.Or

Bedingungen mit logischem ODER verknüpfen.

### schema.settings.expression_settings.expressions

Liste von Achsenbedingungen. Jede Bedingung beschreibt Achse, Operator und Vergleichsausdruck.

### schema.settings.expression_settings.expressions.axis

Zu prüfende Koordinatenachse, etwa X oder Y. Die Bedingung bezieht sich auf diese Komponente des Punktes.

### schema.settings.expression_settings.expressions.operator

Vergleichsoperator. Wahrheits-/Leerheitsprüfungen benötigen keinen zusätzlichen Vergleichstext; andere Operatoren verlangen kompatible Werte.

### schema.settings.expression_settings.expressions.operator.option.Equal

Prüft die Achsenbedingung auf Gleichheit.

### schema.settings.expression_settings.expressions.operator.option.GreaterThan

Linker Wert muss größer sein als der rechte Vergleichswert.

### schema.settings.expression_settings.expressions.operator.option.GreaterThanOrEqual

Linker Wert muss größer oder gleich dem Vergleichswert sein.

### schema.settings.expression_settings.expressions.operator.option.LessThan

Linker Wert muss kleiner sein als der rechte Vergleichswert.

### schema.settings.expression_settings.expressions.operator.option.LessThanOrEqual

Linker Wert muss kleiner oder gleich dem Vergleichswert sein.

### schema.settings.expression_settings.expressions.operator.option.NotEqual

Prüft die Achsenbedingung auf Ungleichheit.

### schema.settings.expression_settings.expressions.value

Gespeicherter Literal- oder Vergleichswert. Sein JSON-Typ muss zum angegebenen value_kind und zur Kardinalität passen; Zahlen und Booleans nicht versehentlich als Text speichern.

### schema.settings.match_requirement

Legt fest, ob alle geprüften Punkte/Bedingungen oder wenigstens einer/eine die Prüfung bestehen müssen.

### schema.settings.match_requirement.option.All

Alle betrachteten Bedingungen beziehungsweise Punkte müssen passen.

### schema.settings.match_requirement.option.Any

Mindestens eine betrachtete Bedingung beziehungsweise ein Punkt muss passen.

### schema.settings.mode

Offset vergleicht Punkte mit einer Referenz und erlaubten X-/Y-Abweichungen. Expression wertet die konfigurierten Achsenbedingungen aus. Nur die Einstellungen des gewählten Modus sind aktiv.

### schema.settings.mode.option.Expression

Punkte anhand expliziter Achsenbedingungen vergleichen.

### schema.settings.mode.option.Offset

Punkte anhand einer Referenzposition und erlaubter Versätze vergleichen.

### schema.settings.offset_settings

Einstellungen für den Punktvergleich mit Referenzpunkt und erlaubten Abweichungen.

### schema.settings.offset_settings.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.offset_settings.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.offset_settings.reference_points_source

Typisierte Punktquelle als Referenz für den Punktvergleich.

### schema.settings.offset_settings.reference_points_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.offset_settings.reference_points_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.offset_settings.reference_points_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.offset_settings.reference_points_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.offset_settings.reference_points_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.offset_settings.reference_points_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.offset_settings.reference_points_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.offset_settings.reference_points_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.offset_settings.reference_points_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.offset_settings.reference_source

Manual verwendet die direkt angegebenen Referenzkoordinaten; JobResult verwendet reference_points_source.

### schema.settings.offset_settings.reference_source.option.JobResult

Verwendet einen kompatiblen Ergebniswert eines Job-Steps.

### schema.settings.offset_settings.reference_source.option.Manual

Verwendet die manuell angegebenen Koordinaten.

### schema.settings.offset_settings.reference_x

Horizontale Referenzkoordinate für den Offsetvergleich in Pixeln.

### schema.settings.offset_settings.reference_y

Vertikale Referenzkoordinate für den Offsetvergleich in Pixeln.

### schema.settings.points

Füge die zu prüfenden Punkte hinzu. Wähle pro Eintrag eine manuelle X-/Y-Koordinate oder die kompatible Punktquelle eines vorherigen Steps. Die Auswahl bestimmt, welche Unterfelder erscheinen.

### schema.settings.points.manual_x

Direkt eingetragene horizontale Punktkoordinate in Pixeln.

### schema.settings.points.manual_y

Direkt eingetragene vertikale Punktkoordinate in Pixeln.

### schema.settings.points.points_source

Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.

### schema.settings.points.points_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.points.points_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.points.points_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.points.points_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.points.points_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.points.points_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.points.points_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.points.points_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.points.points_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.points.source

Auswahl zwischen einem direkt angegebenen Wert und einem referenzierten Job-Ergebnis, soweit dieser Konfigurationstyp beide Varianten anbietet.

### schema.settings.points.source.option.JobResult

Verwendet einen kompatiblen Ergebniswert eines Job-Steps.

### schema.settings.points.source.option.Manual

Verwendet die manuell angegebenen Koordinaten.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "point_comparison",
  "name": "Punkte vergleichen",
  "description": "Vergleicht Punkte mit einem Referenzpunkt oder Achsenbedingungen.",
  "category": "BildAuswerten",
  "uiFieldIds": [
    "mode",
    "match_requirement",
    "points",
    "reference_source",
    "reference_x",
    "reference_y",
    "reference_points_source",
    "offset_x",
    "offset_y",
    "combine_mode",
    "expressions"
  ],
  "fields": [
    {
      "id": "mode",
      "name": "Modus",
      "descriptor": {
        "Id": "mode",
        "LabelKey": "Ui.Step.Settings.Mode",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "Offset",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "Offset",
            "Expression"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "Offset",
            "LabelKey": "Ui.Step.Settings.OffsetTolerance",
            "DisplayName": null
          },
          {
            "Value": "Expression",
            "LabelKey": "Ui.Step.Settings.Expression",
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
      "id": "match_requirement",
      "name": "Erforderliche Treffer",
      "descriptor": {
        "Id": "match_requirement",
        "LabelKey": "Ui.Step.Settings.RequiredMatches",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "All",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "All",
            "Any"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 1,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "All",
            "LabelKey": "Ui.Step.Settings.AllAND",
            "DisplayName": null
          },
          {
            "Value": "Any",
            "LabelKey": "Ui.Step.Settings.AtLeastOneOR",
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
      "id": "points",
      "name": "ZU PRÜFENDE PUNKTE",
      "descriptor": {
        "Id": "points",
        "LabelKey": "Ui.Step.Settings.PointsToCheck",
        "ValueKind": "Collection",
        "Required": true,
        "DefaultValue": [
          {
            "source": "Manual",
            "manual_x": 0,
            "manual_y": 0,
            "points_source": null
          }
        ],
        "DescriptionKey": null,
        "EditorHint": "point-entry-list",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 2,
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
      "id": "reference_source",
      "name": "Referenzquelle",
      "descriptor": {
        "Id": "reference_source",
        "LabelKey": "Ui.Step.Settings.ReferenceSource",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "Manual",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "Manual",
            "JobResult"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 3,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Offset",
          "AnyOfValues": null
        },
        "Options": [
          {
            "Value": "Manual",
            "LabelKey": "Ui.Step.Settings.EnterManually",
            "DisplayName": null
          },
          {
            "Value": "JobResult",
            "LabelKey": "Ui.Step.Settings.FromDetectionResult",
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
      "id": "reference_x",
      "name": "X",
      "descriptor": {
        "Id": "reference_x",
        "LabelKey": "Ui.Step.Settings.X",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 4,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Offset",
          "AnyOfValues": null
        },
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
      "id": "reference_y",
      "name": "Y",
      "descriptor": {
        "Id": "reference_y",
        "LabelKey": "Ui.Step.Settings.Y",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 5,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Offset",
          "AnyOfValues": null
        },
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
      "id": "reference_points_source",
      "name": "Punktquelle",
      "descriptor": {
        "Id": "reference_points_source",
        "LabelKey": "Ui.Step.Settings.PointSource",
        "ValueKind": "ResultBinding",
        "Required": false,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "value-reference-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 6,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Offset",
          "AnyOfValues": null
        },
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
      "id": "offset_x",
      "name": "Versatz X (px)",
      "descriptor": {
        "Id": "offset_x",
        "LabelKey": "Ui.Step.Settings.XOffsetPixels",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 10,
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
        "Advanced": false,
        "Order": 7,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Offset",
          "AnyOfValues": null
        },
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
        "DefaultValue": 10,
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
        "Advanced": false,
        "Order": 8,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Offset",
          "AnyOfValues": null
        },
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
      "id": "combine_mode",
      "name": "Verknüpfung",
      "descriptor": {
        "Id": "combine_mode",
        "LabelKey": "Ui.Step.Settings.CombineWith",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "And",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "And",
            "Or"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 9,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Expression",
          "AnyOfValues": null
        },
        "Options": [
          {
            "Value": "And",
            "LabelKey": "Ui.Step.Settings.ANDAll",
            "DisplayName": null
          },
          {
            "Value": "Or",
            "LabelKey": "Ui.Step.Settings.ORAny",
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
      "id": "expressions",
      "name": "Achsenbedingungen",
      "descriptor": {
        "Id": "expressions",
        "LabelKey": "Ui.Step.Settings.AxisExpressions",
        "ValueKind": "Collection",
        "Required": true,
        "DefaultValue": [
          {
            "axis": "X",
            "operator": "LessThan",
            "value": 0
          }
        ],
        "DescriptionKey": null,
        "EditorHint": "axis-expression-list",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 10,
        "VisibleWhen": {
          "FieldId": "mode",
          "EqualsValue": "Expression",
          "AnyOfValues": null
        },
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
      "type": "PointComparisonSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.expression_settings",
      "name": "expression_settings",
      "type": "ExpressionComparisonSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.expression_settings.combine_mode",
      "name": "combine_mode",
      "type": "ExpressionCombineMode",
      "options": [
        "And",
        "Or"
      ],
      "defaultValue": "And"
    },
    {
      "id": "settings.expression_settings.expressions",
      "name": "expressions",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.expression_settings.expressions.axis",
      "name": "axis",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.expression_settings.expressions.operator",
      "name": "operator",
      "type": "PointAxisOperator",
      "options": [
        "LessThan",
        "LessThanOrEqual",
        "GreaterThan",
        "GreaterThanOrEqual",
        "Equal",
        "NotEqual"
      ],
      "defaultValue": null
    },
    {
      "id": "settings.expression_settings.expressions.value",
      "name": "value",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.match_requirement",
      "name": "match_requirement",
      "type": "PointMatchRequirement",
      "options": [
        "All",
        "Any"
      ],
      "defaultValue": "All"
    },
    {
      "id": "settings.mode",
      "name": "mode",
      "type": "PointComparisonMode",
      "options": [
        "Offset",
        "Expression"
      ],
      "defaultValue": "Offset"
    },
    {
      "id": "settings.offset_settings",
      "name": "offset_settings",
      "type": "OffsetComparisonSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.offset_x",
      "name": "offset_x",
      "type": "Int32",
      "options": [],
      "defaultValue": 10
    },
    {
      "id": "settings.offset_settings.offset_y",
      "name": "offset_y",
      "type": "Int32",
      "options": [],
      "defaultValue": 10
    },
    {
      "id": "settings.offset_settings.reference_points_source",
      "name": "reference_points_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.offset_settings.reference_points_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_points_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.offset_settings.reference_points_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.offset_settings.reference_source",
      "name": "reference_source",
      "type": "PointEntrySource",
      "options": [
        "Manual",
        "JobResult"
      ],
      "defaultValue": "Manual"
    },
    {
      "id": "settings.offset_settings.reference_x",
      "name": "reference_x",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.offset_settings.reference_y",
      "name": "reference_y",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.points",
      "name": "points",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.manual_x",
      "name": "manual_x",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.manual_y",
      "name": "manual_y",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source",
      "name": "points_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.points_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.points.source",
      "name": "source",
      "type": "PointEntrySource",
      "options": [
        "Manual",
        "JobResult"
      ],
      "defaultValue": null
    }
  ],
  "inputs": [
    {
      "Key": "points",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "AllValues",
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
      "LegacyAllowedProviderIds": [],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "PointComparisonResult",
    "DisplayName": "PointComparisonResult",
    "Properties": [
      {
        "Name": "Matches",
        "DisplayName": "Matches",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "matches",
        "EnumDisplayNames": null,
        "StableId": "matches"
      },
      {
        "Name": "MatchCount",
        "DisplayName": "Match Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "match_count",
        "EnumDisplayNames": null,
        "StableId": "match_count"
      },
      {
        "Name": "TotalCount",
        "DisplayName": "Total Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "total_count",
        "EnumDisplayNames": null,
        "StableId": "total_count"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Matches",
        "DisplayName": "Matches",
        "Property": {
          "Name": "Matches",
          "DisplayName": "Matches",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "matches",
          "EnumDisplayNames": null,
          "StableId": "matches"
        },
        "Children": []
      },
      {
        "Segment": "MatchCount",
        "DisplayName": "Match Count",
        "Property": {
          "Name": "MatchCount",
          "DisplayName": "Match Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "match_count",
          "EnumDisplayNames": null,
          "StableId": "match_count"
        },
        "Children": []
      },
      {
        "Segment": "TotalCount",
        "DisplayName": "Total Count",
        "Property": {
          "Name": "TotalCount",
          "DisplayName": "Total Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "total_count",
          "EnumDisplayNames": null,
          "StableId": "total_count"
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
  "type": "point_comparison",
  "settings": {
    "mode": "Offset",
    "match_requirement": "All",
    "points": [
      {
        "source": "Manual",
        "manual_x": 0,
        "manual_y": 0,
        "points_source": {
          "provider_id": "",
          "source_id": ""
        }
      }
    ],
    "offset_settings": {
      "reference_source": "Manual",
      "reference_x": 0,
      "reference_y": 0,
      "reference_points_source": {
        "provider_id": "",
        "source_id": ""
      },
      "offset_x": 10,
      "offset_y": 10
    },
    "expression_settings": {
      "expressions": [
        {
          "axis": "X",
          "operator": "LessThan",
          "value": 0
        }
      ],
      "combine_mode": "And"
    }
  },
  "id": "e82b1d75-e0de-5ff6-a44a-a5c26f839b7f",
  "inputs": {},
  "is_enabled": true
}
```

