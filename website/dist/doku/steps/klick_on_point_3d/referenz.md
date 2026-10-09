## Steps: 3D-Ziel anklicken

ID: klick_on_point_3d
Website: /doku/steps/klick_on_point_3d/

Abbildung: [Detailansicht: 3D-Ziel anklicken. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-klick-on-point-3d.detail.png)

1. Punktquelle: Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.
2. Ursprung: Referenzursprung für die relative Zielbewegung. Die gewählte Koordinatenquelle und ihr Koordinatenraum müssen zum Ziel passen.
3. Maustaste: left, right oder middle sendet die entsprechende Maustaste; none führt nur die Mausbewegung aus.
4. Bewegungsfaktor X: Skalierung der horizontalen Zielabweichung für die relative Mausbewegung. Zunächst mit kleinen Faktoren testen.
5. Bewegungsfaktor Y: Skalierung der vertikalen Zielabweichung für die relative Mausbewegung. X- und Y-Achse können unterschiedlich reagieren.
6. Versatz X (px): Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.
7. Versatz Y (px): Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.
8. Zeitlimit (ms): Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.
9. Doppelklick: Sendet einen Doppelklick statt eines Einzelklicks, sofern eine Maustaste gewählt ist.
10. Bewegungstoleranz (px): Inklusive Toleranz in Pixeln auf beiden Achsen für wiederholte ganzzahlige Mausbewegungen. Standard 10; 0 unterdrückt nur exakt gleiche Bewegungen. Nur eine erfolgreich gesendete Bewegung aktualisiert die Vergleichsbasis.
### errors

Ein unterdrückter alter Frame oder eine Bewegung innerhalb der Toleranz sendet keine neue Bewegung und keinen Klick. Lies movement_blocked; fehlende Quellen, abgelaufene Eingaben und falsche Typen dürfen nicht als erfolgreiche Aktion behandelt werden.

### example

Erkennungspunkt relativ zum Bildschirmmittelpunkt verwenden; Faktoren zunächst klein wählen und movement_blocked im Debugging prüfen.

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

### field.movement_factor_x

Skalierung der horizontalen Zielabweichung für die relative Mausbewegung. Zunächst mit kleinen Faktoren testen.

### field.movement_factor_y

Skalierung der vertikalen Zielabweichung für die relative Mausbewegung. X- und Y-Achse können unterschiedlich reagieren.

### field.movement_threshold_px

Inklusive Toleranz in Pixeln auf beiden Achsen für wiederholte ganzzahlige Mausbewegungen. Standard 10; 0 unterdrückt nur exakt gleiche Bewegungen. Nur eine erfolgreich gesendete Bewegung aktualisiert die Vergleichsbasis.

### field.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### field.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### field.origin

Referenzursprung für die relative Zielbewegung. Die gewählte Koordinatenquelle und ihr Koordinatenraum müssen zum Ziel passen.

### field.points_source

Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.

### field.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### input.origin

Referenzursprung der Bewegung. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Bei kompatiblen Listen wird der erste Wert verwendet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.points

Punkt-/Erkennungsergebnis. Fehlende erforderliche Werte führen zum Fehler. Bei kompatiblen Listen wird der erste Wert verwendet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Wähle Zielpunkt und Ursprung für eine relative Mausbewegung mit getrennten Faktoren für X und Y. Die Bewegungstoleranz unterdrückt wiederholte nahezu identische ganzzahlige Bewegungen. Ein bekannter Quell-Frame, der nicht neuer als die letzte Eingabe ist, wird ebenfalls unterdrückt. movement_blocked unterscheidet diese Sperre von einer erfolgreich gesendeten Bewegung.

### result.click_on_point_3d.applied_delta_x

Tatsächlich angewendeter ganzzahliger horizontaler Mausversatz nach Skalierung und Rundung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.applied_delta_y

Tatsächlich angewendeter ganzzahliger vertikaler Mausversatz nach Skalierung und Rundung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.delta_x

Horizontale Abweichung beziehungsweise Bewegungsanteil in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.delta_y

Vertikale Abweichung beziehungsweise Bewegungsanteil in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.movement_blocked

Ob die 3D-Eingabe wegen Bewegungstoleranz oder nicht neuerem Quell-Frame unterdrückt wurde. Es wurde dabei keine neue Bewegung und kein Klick gesendet. Einzelwert dieses Ergebnisses.

### result.click_on_point_3d.movement_factor_x

Horizontaler Skalierungsfaktor, der für diese Eingabe verwendet wurde. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.movement_factor_y

Vertikaler Skalierungsfaktor, der für diese Eingabe verwendet wurde. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

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

### schema.settings.movement_factor

Zusammengesetzte Bewegungsfaktoren für die relative Maussteuerung. X- und Y-Faktor gehören zu derselben Einstellung.

### schema.settings.movement_factor_x

Skalierung der horizontalen Zielabweichung für die relative Mausbewegung. Zunächst mit kleinen Faktoren testen.

### schema.settings.movement_factor_y

Skalierung der vertikalen Zielabweichung für die relative Mausbewegung. X- und Y-Achse können unterschiedlich reagieren.

### schema.settings.movement_threshold_px

Inklusive Toleranz in Pixeln auf beiden Achsen für wiederholte ganzzahlige Mausbewegungen. Standard 10; 0 unterdrückt nur exakt gleiche Bewegungen. Nur eine erfolgreich gesendete Bewegung aktualisiert die Vergleichsbasis.

### schema.settings.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.origin_coordinate_space

Koordinatenraum des Ursprungs. Bildlokale Werte vor der Verwendung als Desktopkoordinate um den passenden Monitorversatz ergänzen.

### schema.settings.origin_monitor_index

Nullbasierter Monitor, auf den sich der konfigurierte Ursprung bezieht.

### schema.settings.origin_source

Wählt die direkte beziehungsweise referenzierte Herkunft des Bewegungsursprungs.

### schema.settings.origin_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.origin_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.origin_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.origin_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.origin_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.origin_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.origin_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.origin_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.origin_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.origin_x

Horizontale Koordinate des direkt konfigurierten Ursprungs in Pixeln.

### schema.settings.origin_y

Vertikale Koordinate des direkt konfigurierten Ursprungs in Pixeln.

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
  "id": "klick_on_point_3d",
  "name": "3D-Ziel anklicken",
  "description": "Bewegt die Maus per FOV-Berechnung auf ein Zielobjekt und klickt es an.",
  "category": "MausTastatur",
  "uiFieldIds": [
    "points_source",
    "origin",
    "click_type",
    "movement_factor_x",
    "movement_factor_y",
    "offset_x",
    "offset_y",
    "timeout_ms",
    "double_click",
    "movement_threshold_px"
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
      "id": "origin",
      "name": "Ursprung",
      "descriptor": {
        "Id": "origin",
        "LabelKey": "Ui.Step.Settings.Origin",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": {
          "monitor_index": 0,
          "x": 0,
          "y": 0,
          "coordinate_space": "monitor_local",
          "point_source": null
        },
        "DescriptionKey": "Ui.Step.Settings.OriginLocalHelp",
        "EditorHint": "screen-point-picker",
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
        "ScreenPointPickerOptions": {
          "DefaultToPrimaryMonitorCenter": true,
          "WholeValueInputContractId": "origin"
        },
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
        "Order": 2,
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
      "id": "movement_factor_x",
      "name": "Bewegungsfaktor X",
      "descriptor": {
        "Id": "movement_factor_x",
        "LabelKey": "Ui.Step.Settings.MovementFactorX",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 1,
        "DescriptionKey": "Ui.Step.Settings.MovementFactorHelp",
        "EditorHint": null,
        "Constraints": {
          "Minimum": 0.01,
          "Maximum": 100,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
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
      "id": "movement_factor_y",
      "name": "Bewegungsfaktor Y",
      "descriptor": {
        "Id": "movement_factor_y",
        "LabelKey": "Ui.Step.Settings.MovementFactorY",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 1,
        "DescriptionKey": "Ui.Step.Settings.MovementFactorHelp",
        "EditorHint": null,
        "Constraints": {
          "Minimum": 0.01,
          "Maximum": 100,
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
        "Order": 6,
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
        "Order": 7,
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
        "Order": 8,
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
      "id": "movement_threshold_px",
      "name": "Bewegungstoleranz (px)",
      "descriptor": {
        "Id": "movement_threshold_px",
        "LabelKey": "Ui.Step.Settings.MovementThresholdPixels",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 10,
        "DescriptionKey": "Ui.Step.Settings.MovementThresholdHelp",
        "EditorHint": null,
        "Constraints": {
          "Minimum": 0,
          "Maximum": 2147483647,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": true,
        "Order": 9,
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
      "type": "KlickOnPoint3DSettings",
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
      "id": "settings.movement_factor",
      "name": "movement_factor",
      "type": "Double",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.movement_factor_x",
      "name": "movement_factor_x",
      "type": "Double",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.movement_factor_y",
      "name": "movement_factor_y",
      "type": "Double",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.movement_threshold_px",
      "name": "movement_threshold_px",
      "type": "Int32",
      "options": [],
      "defaultValue": 10
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
      "id": "settings.origin_coordinate_space",
      "name": "origin_coordinate_space",
      "type": "String",
      "options": [],
      "defaultValue": "monitor_local"
    },
    {
      "id": "settings.origin_monitor_index",
      "name": "origin_monitor_index",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.origin_source",
      "name": "origin_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.origin_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.origin_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.origin_x",
      "name": "origin_x",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.origin_y",
      "name": "origin_y",
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
    },
    {
      "Key": "origin",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
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
      "LegacyAllowedProviderIds": [],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "KlickOnPoint3DResult",
    "DisplayName": "KlickOnPoint3DResult",
    "Properties": [
      {
        "Name": "DeltaX",
        "DisplayName": "Delta X",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.delta_x",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.delta_x"
      },
      {
        "Name": "DeltaY",
        "DisplayName": "Delta Y",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.delta_y",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.delta_y"
      },
      {
        "Name": "MovementFactorX",
        "DisplayName": "Movement Factor X",
        "DataType": "Number",
        "Description": "Number, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.movement_factor_x",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.movement_factor_x"
      },
      {
        "Name": "MovementFactorY",
        "DisplayName": "Movement Factor Y",
        "DataType": "Number",
        "Description": "Number, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.movement_factor_y",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.movement_factor_y"
      },
      {
        "Name": "AppliedDeltaX",
        "DisplayName": "Applied Delta X",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.applied_delta_x",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.applied_delta_x"
      },
      {
        "Name": "AppliedDeltaY",
        "DisplayName": "Applied Delta Y",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.applied_delta_y",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.applied_delta_y"
      },
      {
        "Name": "MovementBlocked",
        "DisplayName": "Movement Blocked",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.movement_blocked",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.movement_blocked"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "DeltaX",
        "DisplayName": "Delta X",
        "Property": {
          "Name": "DeltaX",
          "DisplayName": "Delta X",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.delta_x",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.delta_x"
        },
        "Children": []
      },
      {
        "Segment": "DeltaY",
        "DisplayName": "Delta Y",
        "Property": {
          "Name": "DeltaY",
          "DisplayName": "Delta Y",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.delta_y",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.delta_y"
        },
        "Children": []
      },
      {
        "Segment": "MovementFactorX",
        "DisplayName": "Movement Factor X",
        "Property": {
          "Name": "MovementFactorX",
          "DisplayName": "Movement Factor X",
          "DataType": "Number",
          "Description": "Number, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.movement_factor_x",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.movement_factor_x"
        },
        "Children": []
      },
      {
        "Segment": "MovementFactorY",
        "DisplayName": "Movement Factor Y",
        "Property": {
          "Name": "MovementFactorY",
          "DisplayName": "Movement Factor Y",
          "DataType": "Number",
          "Description": "Number, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.movement_factor_y",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.movement_factor_y"
        },
        "Children": []
      },
      {
        "Segment": "AppliedDeltaX",
        "DisplayName": "Applied Delta X",
        "Property": {
          "Name": "AppliedDeltaX",
          "DisplayName": "Applied Delta X",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.applied_delta_x",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.applied_delta_x"
        },
        "Children": []
      },
      {
        "Segment": "AppliedDeltaY",
        "DisplayName": "Applied Delta Y",
        "Property": {
          "Name": "AppliedDeltaY",
          "DisplayName": "Applied Delta Y",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.applied_delta_y",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.applied_delta_y"
        },
        "Children": []
      },
      {
        "Segment": "MovementBlocked",
        "DisplayName": "Movement Blocked",
        "Property": {
          "Name": "MovementBlocked",
          "DisplayName": "Movement Blocked",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.movement_blocked",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.movement_blocked"
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
  "type": "klick_on_point_3d",
  "settings": {
    "movement_threshold_px": 10,
    "double_click": false,
    "click_type": "left",
    "timeout_ms": 0,
    "origin_x": 0,
    "origin_y": 0,
    "origin_source": {
      "provider_id": "",
      "source_id": ""
    },
    "origin_monitor_index": 0,
    "origin_coordinate_space": "monitor_local",
    "movement_factor_x": null,
    "movement_factor_y": null,
    "offset_x": 0,
    "offset_y": 0,
    "points_source": {
      "provider_id": "",
      "source_id": ""
    }
  },
  "id": "23b322d2-328c-1eab-64b5-9a027aaa036d",
  "inputs": {},
  "is_enabled": true
}
```

