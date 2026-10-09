## Steps: Bewegung vorhersagen

ID: predict_movement
Website: /doku/steps/predict_movement/

Abbildung: [Detailansicht: Bewegung vorhersagen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-predict-movement.detail.png)

1. Punktquelle: Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.
2. Vorhersagemodell: Modell der Zielvorhersage: automatische Auswahl oder das ausdrücklich gewählte lineare, Beschleunigungs- beziehungsweise Kalman-Modell.
3. Mindestkonfidenz (%): Mindestkonfidenz für die Übernahme eines Treffers in ROI/Vorhersage. Erkennungsergebnisse verwenden 0 bis 1; der Editor kann denselben Wert als Prozent darstellen.
4. Mindestanzahl Messwerte: Mindestzahl verwertbarer Messpunkte, bevor eine Vorhersage möglich ist.
5. Spur zurücksetzen ab (px): Pixelabstand, ab dem ein Sprung die bisherige Bewegungsspur zurücksetzt.
6. Maximales Alter der Messwerte (ms): Maximales Alter einer Messung in Millisekunden gegen die aktuelle Uhr. Wiederholtes Lesen desselben eingefrorenen Frames verjüngt sie nicht.
7. Maximale Vorhersagedistanz (px): Maximal zulässige Entfernung der Vorhersage vom gemessenen Punkt in Pixeln.
8. Maximaler Modellfehler (px): Maximal akzeptierter Modellfehler in Pixeln. Eine unzuverlässige Anpassung wird nicht als sichere Vorhersage weitergereicht.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

In einem wiederholten Job mehrere zeitlich frische Erkennungspunkte sammeln und 50 ms in die Zukunft vorhersagen; is_predicted prüfen.

### field.max_fit_error

Maximal akzeptierter Modellfehler in Pixeln. Eine unzuverlässige Anpassung wird nicht als sichere Vorhersage weitergereicht.

### field.max_prediction_distance

Maximal zulässige Entfernung der Vorhersage vom gemessenen Punkt in Pixeln.

### field.max_sample_age_ms

Maximales Alter einer Messung in Millisekunden gegen die aktuelle Uhr. Wiederholtes Lesen desselben eingefrorenen Frames verjüngt sie nicht.

### field.min_samples

Mindestzahl verwertbarer Messpunkte, bevor eine Vorhersage möglich ist.

### field.minimum_confidence

Mindestkonfidenz für die Übernahme eines Treffers in ROI/Vorhersage. Erkennungsergebnisse verwenden 0 bis 1; der Editor kann denselben Wert als Prozent darstellen.

### field.points_source

Kompatible Punkt-/Erkennungsquelle. Der betreffende Eingabevertrag bestimmt, ob der erste oder alle Listeneinträge verarbeitet werden.

### field.prediction_model

Modell der Zielvorhersage: automatische Auswahl oder das ausdrücklich gewählte lineare, Beschleunigungs- beziehungsweise Kalman-Modell.

### field.prediction_model.option.Acceleration

Bewegungsmodell mit Beschleunigung.

### field.prediction_model.option.Automatic

Überlässt die Auswahl des geeigneten Vorhersagemodells dem Backend.

### field.prediction_model.option.Kalman

Kalman-Modell für die Bewegungsschätzung.

### field.prediction_model.option.Linear

Lineares Bewegungsmodell ohne ausdrücklich modellierte Beschleunigung.

### field.prediction_ms

Zeitlicher Vorhersagehorizont in Millisekunden. Größere Horizonte erhöhen typischerweise die Unsicherheit.

### field.reset_distance_threshold

Pixelabstand, ab dem ein Sprung die bisherige Bewegungsspur zurücksetzt.

### field.time_basis

Zeitgrundlage der Bewegungsschätzung. Prüfe die angebotene Quelle und deren Empfangs-/Frame-Zeitstempel statt ungeprüft die Step-Reihenfolge als Messzeit anzunehmen.

### input.points

Punkt-/Erkennungsergebnis. Fehlende erforderliche Werte führen zum Fehler. Alle kompatiblen Listeneinträge werden verarbeitet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Sammle Erkennungspunkte und schätze eine zukünftige Position. Mindestkonfidenz, Probenzahl, Alter, Modellfehler und Sprunggrenzen begrenzen die verwendete Spur. Prüfe is_predicted vor der Weiterverwendung; eingefrorene Frames altern gegen die aktuelle Uhr und sind keine neuen Messungen.

### result.all_detections

Liste einzelner Erkennungen mit eigenen Punkten, Rechtecken und Konfidenzen. Die Anzahl ist nicht gleich einer pauschalen Erfolgsbewertung. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box

Begrenzendes Rechteck eines Treffers; bei fehlender Erkennung kann ein optionales Rechteck fehlen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.height

Höhe des Ergebnisrechtecks in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.left

Linke Rechteckkante in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.top

Obere Rechteckkante in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.width

Breite des Ergebnisrechtecks in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.confidence

Normalisierte Trefferkonfidenz zwischen 0 und 1. 0.9 entspricht 90 %; mehrere Treffer können unterschiedliche Konfidenzen besitzen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.count

Anzahl der Elemente der referenzierten Sammlung, automatisch als zusätzlicher Ergebniswert verfügbar. Einzelwert dieses Ergebnisses.

### result.bounding_box

Begrenzendes Rechteck eines Treffers; bei fehlender Erkennung kann ein optionales Rechteck fehlen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.height

Höhe des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.left

Linke Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.top

Obere Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.width

Breite des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.confidence

Normalisierte Trefferkonfidenz zwischen 0 und 1. 0.9 entspricht 90 %; mehrere Treffer können unterschiedliche Konfidenzen besitzen. Einzelwert dieses Ergebnisses.

### result.found

Ob die Erkennung/Suche in diesem Lauf einen passenden Treffer geliefert hat. false ist ein fachliches Ergebnis und nicht automatisch ein Step-Fehler. Einzelwert dieses Ergebnisses.

### result.is_predicted

Ob ein Vorhersagemodell genügend geeignete Messwerte für eine gültige Prognose hatte. Einzelwert dieses Ergebnisses.

### result.point

Ermittelter Zielpunkt. Bei Bildschirmquellen ist der Quelle-zu-Desktop-Versatz bereits entsprechend dem Ergebnisvertrag berücksichtigt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.point.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.point.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.predicted_for_utc

UTC-Zielzeitpunkt, für den die vorhergesagte Position gilt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.source_capture_is_fresh

Frischezustand der verwendeten Aufnahme, unverändert in das Erkennungsergebnis übernommen. Einzelwert dieses Ergebnisses.

### result.source_capture_timestamp_utc

UTC-Aufnahmezeit der Bildquelle, aus der dieses Ergebnis berechnet wurde. Einzelwert dieses Ergebnisses.

### result.source_frame_timestamp

Monotoner Zeitstempel des zugrunde liegenden Frames. 0 bedeutet unbekannte zeitliche Herkunft. Einzelwert dieses Ergebnisses.

### result.source_frame_version

Frame-Identität der verwendeten Bildquelle; auch ein Erkennungsfehlschlag behält die Herkunftsinformation. Einzelwert dieses Ergebnisses.

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

### schema.settings.max_fit_error

Maximal akzeptierter Modellfehler in Pixeln. Eine unzuverlässige Anpassung wird nicht als sichere Vorhersage weitergereicht.

### schema.settings.max_prediction_distance

Maximal zulässige Entfernung der Vorhersage vom gemessenen Punkt in Pixeln.

### schema.settings.max_sample_age_ms

Maximales Alter einer Messung in Millisekunden gegen die aktuelle Uhr. Wiederholtes Lesen desselben eingefrorenen Frames verjüngt sie nicht.

### schema.settings.min_samples

Mindestzahl verwertbarer Messpunkte, bevor eine Vorhersage möglich ist.

### schema.settings.minimum_confidence

Mindestkonfidenz für die Übernahme eines Treffers in ROI/Vorhersage. Erkennungsergebnisse verwenden 0 bis 1; der Editor kann denselben Wert als Prozent darstellen.

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

### schema.settings.prediction_model

Modell der Zielvorhersage: automatische Auswahl oder das ausdrücklich gewählte lineare, Beschleunigungs- beziehungsweise Kalman-Modell.

### schema.settings.prediction_ms

Zeitlicher Vorhersagehorizont in Millisekunden. Größere Horizonte erhöhen typischerweise die Unsicherheit.

### schema.settings.reset_distance_threshold

Pixelabstand, ab dem ein Sprung die bisherige Bewegungsspur zurücksetzt.

### schema.settings.time_basis

Zeitgrundlage der Bewegungsschätzung. Prüfe die angebotene Quelle und deren Empfangs-/Frame-Zeitstempel statt ungeprüft die Step-Reihenfolge als Messzeit anzunehmen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "predict_movement",
  "name": "Bewegung vorhersagen",
  "description": "Berechnet aus vorherigen Erkennungspunkten eine vorhergesagte Zielposition.",
  "category": "BildAuswerten",
  "uiFieldIds": [
    "points_source",
    "prediction_model",
    "minimum_confidence",
    "min_samples",
    "reset_distance_threshold",
    "max_sample_age_ms",
    "max_prediction_distance",
    "max_fit_error"
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
      "id": "prediction_model",
      "name": "Vorhersagemodell",
      "descriptor": {
        "Id": "prediction_model",
        "LabelKey": "Ui.Step.Settings.PredictionModel",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "Automatic",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "Automatic",
            "Linear",
            "Acceleration",
            "Kalman"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 1,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "Automatic",
            "LabelKey": "Enum.PredictionModel.Automatic",
            "DisplayName": null
          },
          {
            "Value": "Linear",
            "LabelKey": "Enum.PredictionModel.Linear",
            "DisplayName": null
          },
          {
            "Value": "Acceleration",
            "LabelKey": "Enum.PredictionModel.Acceleration",
            "DisplayName": null
          },
          {
            "Value": "Kalman",
            "LabelKey": "Enum.PredictionModel.Kalman",
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
      "id": "minimum_confidence",
      "name": "Mindestkonfidenz (%)",
      "descriptor": {
        "Id": "minimum_confidence",
        "LabelKey": "Ui.Step.Settings.MinimumConfidencePercent",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 0.15,
        "DescriptionKey": null,
        "EditorHint": "percentage",
        "Constraints": {
          "Minimum": 0,
          "Maximum": 1,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
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
      "id": "min_samples",
      "name": "Mindestanzahl Messwerte",
      "descriptor": {
        "Id": "min_samples",
        "LabelKey": "Ui.Step.Settings.MinValues",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 3,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": 2,
          "Maximum": null,
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
      "id": "reset_distance_threshold",
      "name": "Spur zurücksetzen ab (px)",
      "descriptor": {
        "Id": "reset_distance_threshold",
        "LabelKey": "Ui.Step.Settings.ResetAtDistance",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 250,
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
      "id": "max_sample_age_ms",
      "name": "Maximales Alter der Messwerte (ms)",
      "descriptor": {
        "Id": "max_sample_age_ms",
        "LabelKey": "Ui.Step.Settings.MaxAgeMs",
        "ValueKind": "Duration",
        "Required": false,
        "DefaultValue": 500,
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
      "id": "max_prediction_distance",
      "name": "Maximale Vorhersagedistanz (px)",
      "descriptor": {
        "Id": "max_prediction_distance",
        "LabelKey": "Ui.Step.Settings.MaxPredictionDistance",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 500,
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
      "id": "max_fit_error",
      "name": "Maximaler Modellfehler (px)",
      "descriptor": {
        "Id": "max_fit_error",
        "LabelKey": "Ui.Step.Settings.MaxFitError",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 75,
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
      "id": "prediction_ms",
      "name": "Vorhersage (ms)",
      "descriptor": {
        "Id": "prediction_ms",
        "LabelKey": "Ui.Step.Settings.PredictionMs",
        "ValueKind": "Duration",
        "Required": false,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
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
      "id": "time_basis",
      "name": "Zeitbasis",
      "descriptor": {
        "Id": "time_basis",
        "LabelKey": "Ui.Step.Settings.TimeBasis",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "Execution",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
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
      "type": "PredictMovementSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.max_fit_error",
      "name": "max_fit_error",
      "type": "Double",
      "options": [],
      "defaultValue": 75
    },
    {
      "id": "settings.max_prediction_distance",
      "name": "max_prediction_distance",
      "type": "Double",
      "options": [],
      "defaultValue": 500
    },
    {
      "id": "settings.max_sample_age_ms",
      "name": "max_sample_age_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 500
    },
    {
      "id": "settings.minimum_confidence",
      "name": "minimum_confidence",
      "type": "Double",
      "options": [],
      "defaultValue": 0.15
    },
    {
      "id": "settings.min_samples",
      "name": "min_samples",
      "type": "Int32",
      "options": [],
      "defaultValue": 3
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
      "id": "settings.prediction_model",
      "name": "prediction_model",
      "type": "String",
      "options": [],
      "defaultValue": "Automatic"
    },
    {
      "id": "settings.prediction_ms",
      "name": "prediction_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.reset_distance_threshold",
      "name": "reset_distance_threshold",
      "type": "Double",
      "options": [],
      "defaultValue": 250
    },
    {
      "id": "settings.time_basis",
      "name": "time_basis",
      "type": "String",
      "options": [],
      "defaultValue": "Execution"
    }
  ],
  "inputs": [
    {
      "Key": "points",
      "Required": true,
      "MissingValuePolicy": "FailStep",
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
        "step_result"
      ],
      "LegacyAllowedProviderIds": [],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "PredictMovementResult",
    "DisplayName": "PredictMovementResult",
    "Properties": [
      {
        "Name": "Found",
        "DisplayName": "Found",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "found",
        "EnumDisplayNames": null,
        "StableId": "found"
      },
      {
        "Name": "Point",
        "DisplayName": "Point",
        "DataType": "Point",
        "Description": "Point, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point",
        "EnumDisplayNames": null,
        "StableId": "point"
      },
      {
        "Name": "Point.X",
        "DisplayName": "Point / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point.x",
        "EnumDisplayNames": null,
        "StableId": "point.x"
      },
      {
        "Name": "Point.Y",
        "DisplayName": "Point / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point.y",
        "EnumDisplayNames": null,
        "StableId": "point.y"
      },
      {
        "Name": "BoundingBox",
        "DisplayName": "Bounding Box",
        "DataType": "Rectangle",
        "Description": "Rectangle, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box",
        "EnumDisplayNames": null,
        "StableId": "bounding_box"
      },
      {
        "Name": "BoundingBox.X",
        "DisplayName": "Bounding Box / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.x",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.x"
      },
      {
        "Name": "BoundingBox.Y",
        "DisplayName": "Bounding Box / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.y",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.y"
      },
      {
        "Name": "BoundingBox.Width",
        "DisplayName": "Bounding Box / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.width",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.width"
      },
      {
        "Name": "BoundingBox.Height",
        "DisplayName": "Bounding Box / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.height",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.height"
      },
      {
        "Name": "BoundingBox.Left",
        "DisplayName": "Bounding Box / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.left",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.left"
      },
      {
        "Name": "BoundingBox.Top",
        "DisplayName": "Bounding Box / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.top",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.top"
      },
      {
        "Name": "BoundingBox.Right",
        "DisplayName": "Bounding Box / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.right",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.right"
      },
      {
        "Name": "BoundingBox.Bottom",
        "DisplayName": "Bounding Box / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.bottom",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.bottom"
      },
      {
        "Name": "BoundingBox.IsEmpty",
        "DisplayName": "Bounding Box / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.is_empty",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.is_empty"
      },
      {
        "Name": "BoundingBox.Location",
        "DisplayName": "Bounding Box / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.location"
      },
      {
        "Name": "BoundingBox.Location.X",
        "DisplayName": "Bounding Box / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location.x",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.location.x"
      },
      {
        "Name": "BoundingBox.Location.Y",
        "DisplayName": "Bounding Box / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location.y",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.location.y"
      },
      {
        "Name": "BoundingBox.Center",
        "DisplayName": "Bounding Box / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.center"
      },
      {
        "Name": "BoundingBox.Center.X",
        "DisplayName": "Bounding Box / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center.x",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.center.x"
      },
      {
        "Name": "BoundingBox.Center.Y",
        "DisplayName": "Bounding Box / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center.y",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.center.y"
      },
      {
        "Name": "Confidence",
        "DisplayName": "Confidence",
        "DataType": "Number",
        "Description": "Number",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "confidence",
        "EnumDisplayNames": null,
        "StableId": "confidence"
      },
      {
        "Name": "SourceCaptureIsFresh",
        "DisplayName": "Source Capture Is Fresh",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_capture_is_fresh",
        "EnumDisplayNames": null,
        "StableId": "source_capture_is_fresh"
      },
      {
        "Name": "SourceCaptureTimestampUtc",
        "DisplayName": "Source Capture Timestamp Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_capture_timestamp_utc",
        "EnumDisplayNames": null,
        "StableId": "source_capture_timestamp_utc"
      },
      {
        "Name": "SourceFrameVersion",
        "DisplayName": "Source Frame Version",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_frame_version",
        "EnumDisplayNames": null,
        "StableId": "source_frame_version"
      },
      {
        "Name": "SourceFrameTimestamp",
        "DisplayName": "Source Frame Timestamp",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_frame_timestamp",
        "EnumDisplayNames": null,
        "StableId": "source_frame_timestamp"
      },
      {
        "Name": "IsPredicted",
        "DisplayName": "Is Predicted",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_predicted",
        "EnumDisplayNames": null,
        "StableId": "is_predicted"
      },
      {
        "Name": "PredictedForUtc",
        "DisplayName": "Predicted For Utc",
        "DataType": "DateTime",
        "Description": "DateTime, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "predicted_for_utc",
        "EnumDisplayNames": null,
        "StableId": "predicted_for_utc"
      },
      {
        "Name": "AllDetections",
        "DisplayName": "All Detections",
        "DataType": "Detection",
        "Description": "Detection",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections",
        "EnumDisplayNames": null,
        "StableId": "all_detections"
      },
      {
        "Name": "AllDetections.Count",
        "DisplayName": "All Detections / Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.count",
        "EnumDisplayNames": null,
        "StableId": "all_detections.count"
      },
      {
        "Name": "AllDetections[].Center",
        "DisplayName": "All Detections[] / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center",
        "EnumDisplayNames": null,
        "StableId": "all_detections.center"
      },
      {
        "Name": "AllDetections[].Center.X",
        "DisplayName": "All Detections[] / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.center.x"
      },
      {
        "Name": "AllDetections[].Center.Y",
        "DisplayName": "All Detections[] / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.center.y"
      },
      {
        "Name": "AllDetections[].BoundingBox",
        "DisplayName": "All Detections[] / Bounding Box",
        "DataType": "Rectangle",
        "Description": "Rectangle, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box"
      },
      {
        "Name": "AllDetections[].BoundingBox.X",
        "DisplayName": "All Detections[] / Bounding Box / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Y",
        "DisplayName": "All Detections[] / Bounding Box / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.y"
      },
      {
        "Name": "AllDetections[].BoundingBox.Width",
        "DisplayName": "All Detections[] / Bounding Box / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.width",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.width"
      },
      {
        "Name": "AllDetections[].BoundingBox.Height",
        "DisplayName": "All Detections[] / Bounding Box / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.height",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.height"
      },
      {
        "Name": "AllDetections[].BoundingBox.Left",
        "DisplayName": "All Detections[] / Bounding Box / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.left",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.left"
      },
      {
        "Name": "AllDetections[].BoundingBox.Top",
        "DisplayName": "All Detections[] / Bounding Box / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.top",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.top"
      },
      {
        "Name": "AllDetections[].BoundingBox.Right",
        "DisplayName": "All Detections[] / Bounding Box / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.right",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.right"
      },
      {
        "Name": "AllDetections[].BoundingBox.Bottom",
        "DisplayName": "All Detections[] / Bounding Box / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.bottom",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.bottom"
      },
      {
        "Name": "AllDetections[].BoundingBox.IsEmpty",
        "DisplayName": "All Detections[] / Bounding Box / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.is_empty",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.is_empty"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location",
        "DisplayName": "All Detections[] / Bounding Box / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.location"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location.X",
        "DisplayName": "All Detections[] / Bounding Box / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.location.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location.Y",
        "DisplayName": "All Detections[] / Bounding Box / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.location.y"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center",
        "DisplayName": "All Detections[] / Bounding Box / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.center"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center.X",
        "DisplayName": "All Detections[] / Bounding Box / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.center.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center.Y",
        "DisplayName": "All Detections[] / Bounding Box / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.center.y"
      },
      {
        "Name": "AllDetections[].Confidence",
        "DisplayName": "All Detections[] / Confidence",
        "DataType": "Number",
        "Description": "Number",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.confidence",
        "EnumDisplayNames": null,
        "StableId": "all_detections.confidence"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Found",
        "DisplayName": "Found",
        "Property": {
          "Name": "Found",
          "DisplayName": "Found",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "found",
          "EnumDisplayNames": null,
          "StableId": "found"
        },
        "Children": []
      },
      {
        "Segment": "Point",
        "DisplayName": "Point",
        "Property": {
          "Name": "Point",
          "DisplayName": "Point",
          "DataType": "Point",
          "Description": "Point, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "point",
          "EnumDisplayNames": null,
          "StableId": "point"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "Point.X",
              "DisplayName": "Point / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "point.x",
              "EnumDisplayNames": null,
              "StableId": "point.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "Point.Y",
              "DisplayName": "Point / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "point.y",
              "EnumDisplayNames": null,
              "StableId": "point.y"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "BoundingBox",
        "DisplayName": "Bounding Box",
        "Property": {
          "Name": "BoundingBox",
          "DisplayName": "Bounding Box",
          "DataType": "Rectangle",
          "Description": "Rectangle, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "bounding_box",
          "EnumDisplayNames": null,
          "StableId": "bounding_box"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "BoundingBox.X",
              "DisplayName": "Bounding Box / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.x",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "BoundingBox.Y",
              "DisplayName": "Bounding Box / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.y",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Width",
            "Property": {
              "Name": "BoundingBox.Width",
              "DisplayName": "Bounding Box / Width",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.width",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Height",
            "Property": {
              "Name": "BoundingBox.Height",
              "DisplayName": "Bounding Box / Height",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.height",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Left",
            "Property": {
              "Name": "BoundingBox.Left",
              "DisplayName": "Bounding Box / Left",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.left",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Top",
            "Property": {
              "Name": "BoundingBox.Top",
              "DisplayName": "Bounding Box / Top",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.top",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Right",
            "Property": {
              "Name": "BoundingBox.Right",
              "DisplayName": "Bounding Box / Right",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.right",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Bottom",
            "Property": {
              "Name": "BoundingBox.Bottom",
              "DisplayName": "Bounding Box / Bottom",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.bottom",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Is Empty",
            "Property": {
              "Name": "BoundingBox.IsEmpty",
              "DisplayName": "Bounding Box / Is Empty",
              "DataType": "Boolean",
              "Description": "Boolean",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.is_empty",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Location",
            "Property": {
              "Name": "BoundingBox.Location",
              "DisplayName": "Bounding Box / Location",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.location",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "BoundingBox.Location.X",
                  "DisplayName": "Bounding Box / Location / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.location.x",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "BoundingBox.Location.Y",
                  "DisplayName": "Bounding Box / Location / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.location.y",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "BoundingBox.Center",
              "DisplayName": "Bounding Box / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.center",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "BoundingBox.Center.X",
                  "DisplayName": "Bounding Box / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "BoundingBox.Center.Y",
                  "DisplayName": "Bounding Box / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "Confidence",
        "DisplayName": "Confidence",
        "Property": {
          "Name": "Confidence",
          "DisplayName": "Confidence",
          "DataType": "Number",
          "Description": "Number",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "confidence",
          "EnumDisplayNames": null,
          "StableId": "confidence"
        },
        "Children": []
      },
      {
        "Segment": "SourceCaptureIsFresh",
        "DisplayName": "Source Capture Is Fresh",
        "Property": {
          "Name": "SourceCaptureIsFresh",
          "DisplayName": "Source Capture Is Fresh",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_capture_is_fresh",
          "EnumDisplayNames": null,
          "StableId": "source_capture_is_fresh"
        },
        "Children": []
      },
      {
        "Segment": "SourceCaptureTimestampUtc",
        "DisplayName": "Source Capture Timestamp Utc",
        "Property": {
          "Name": "SourceCaptureTimestampUtc",
          "DisplayName": "Source Capture Timestamp Utc",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_capture_timestamp_utc",
          "EnumDisplayNames": null,
          "StableId": "source_capture_timestamp_utc"
        },
        "Children": []
      },
      {
        "Segment": "SourceFrameVersion",
        "DisplayName": "Source Frame Version",
        "Property": {
          "Name": "SourceFrameVersion",
          "DisplayName": "Source Frame Version",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_frame_version",
          "EnumDisplayNames": null,
          "StableId": "source_frame_version"
        },
        "Children": []
      },
      {
        "Segment": "SourceFrameTimestamp",
        "DisplayName": "Source Frame Timestamp",
        "Property": {
          "Name": "SourceFrameTimestamp",
          "DisplayName": "Source Frame Timestamp",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_frame_timestamp",
          "EnumDisplayNames": null,
          "StableId": "source_frame_timestamp"
        },
        "Children": []
      },
      {
        "Segment": "IsPredicted",
        "DisplayName": "Is Predicted",
        "Property": {
          "Name": "IsPredicted",
          "DisplayName": "Is Predicted",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_predicted",
          "EnumDisplayNames": null,
          "StableId": "is_predicted"
        },
        "Children": []
      },
      {
        "Segment": "PredictedForUtc",
        "DisplayName": "Predicted For Utc",
        "Property": {
          "Name": "PredictedForUtc",
          "DisplayName": "Predicted For Utc",
          "DataType": "DateTime",
          "Description": "DateTime, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "predicted_for_utc",
          "EnumDisplayNames": null,
          "StableId": "predicted_for_utc"
        },
        "Children": []
      },
      {
        "Segment": "AllDetections",
        "DisplayName": "All Detections",
        "Property": {
          "Name": "AllDetections",
          "DisplayName": "All Detections",
          "DataType": "Detection",
          "Description": "Detection",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "all_detections",
          "EnumDisplayNames": null,
          "StableId": "all_detections"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Count",
            "Property": {
              "Name": "AllDetections.Count",
              "DisplayName": "All Detections / Count",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.count",
              "EnumDisplayNames": null,
              "StableId": "all_detections.count"
            },
            "Children": []
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "AllDetections[].Center",
              "DisplayName": "All Detections[] / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.center",
              "EnumDisplayNames": null,
              "StableId": "all_detections.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "AllDetections[].Center.X",
                  "DisplayName": "All Detections[] / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "AllDetections[].Center.Y",
                  "DisplayName": "All Detections[] / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.center.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "BoundingBox",
            "DisplayName": "Bounding Box",
            "Property": {
              "Name": "AllDetections[].BoundingBox",
              "DisplayName": "All Detections[] / Bounding Box",
              "DataType": "Rectangle",
              "Description": "Rectangle, kann leer sein",
              "IsNullable": true,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.bounding_box",
              "EnumDisplayNames": null,
              "StableId": "all_detections.bounding_box"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.X",
                  "DisplayName": "All Detections[] / Bounding Box / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.x",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Y",
                  "DisplayName": "All Detections[] / Bounding Box / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.y",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.y"
                },
                "Children": []
              },
              {
                "Segment": "Width",
                "DisplayName": "Width",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Width",
                  "DisplayName": "All Detections[] / Bounding Box / Width",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.width",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.width"
                },
                "Children": []
              },
              {
                "Segment": "Height",
                "DisplayName": "Height",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Height",
                  "DisplayName": "All Detections[] / Bounding Box / Height",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.height",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.height"
                },
                "Children": []
              },
              {
                "Segment": "Left",
                "DisplayName": "Left",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Left",
                  "DisplayName": "All Detections[] / Bounding Box / Left",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.left",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.left"
                },
                "Children": []
              },
              {
                "Segment": "Top",
                "DisplayName": "Top",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Top",
                  "DisplayName": "All Detections[] / Bounding Box / Top",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.top",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.top"
                },
                "Children": []
              },
              {
                "Segment": "Right",
                "DisplayName": "Right",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Right",
                  "DisplayName": "All Detections[] / Bounding Box / Right",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.right",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.right"
                },
                "Children": []
              },
              {
                "Segment": "Bottom",
                "DisplayName": "Bottom",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Bottom",
                  "DisplayName": "All Detections[] / Bounding Box / Bottom",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.bottom",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.bottom"
                },
                "Children": []
              },
              {
                "Segment": "IsEmpty",
                "DisplayName": "Is Empty",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.IsEmpty",
                  "DisplayName": "All Detections[] / Bounding Box / Is Empty",
                  "DataType": "Boolean",
                  "Description": "Boolean",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.is_empty",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.is_empty"
                },
                "Children": []
              },
              {
                "Segment": "Location",
                "DisplayName": "Location",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Location",
                  "DisplayName": "All Detections[] / Bounding Box / Location",
                  "DataType": "Point",
                  "Description": "Point",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.location",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.location"
                },
                "Children": [
                  {
                    "Segment": "X",
                    "DisplayName": "X",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Location.X",
                      "DisplayName": "All Detections[] / Bounding Box / Location / X",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.location.x",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.location.x"
                    },
                    "Children": []
                  },
                  {
                    "Segment": "Y",
                    "DisplayName": "Y",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Location.Y",
                      "DisplayName": "All Detections[] / Bounding Box / Location / Y",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.location.y",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.location.y"
                    },
                    "Children": []
                  }
                ]
              },
              {
                "Segment": "Center",
                "DisplayName": "Center",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Center",
                  "DisplayName": "All Detections[] / Bounding Box / Center",
                  "DataType": "Point",
                  "Description": "Point",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.center",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.center"
                },
                "Children": [
                  {
                    "Segment": "X",
                    "DisplayName": "X",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Center.X",
                      "DisplayName": "All Detections[] / Bounding Box / Center / X",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.center.x",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.center.x"
                    },
                    "Children": []
                  },
                  {
                    "Segment": "Y",
                    "DisplayName": "Y",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Center.Y",
                      "DisplayName": "All Detections[] / Bounding Box / Center / Y",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.center.y",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.center.y"
                    },
                    "Children": []
                  }
                ]
              }
            ]
          },
          {
            "Segment": "Confidence",
            "DisplayName": "Confidence",
            "Property": {
              "Name": "AllDetections[].Confidence",
              "DisplayName": "All Detections[] / Confidence",
              "DataType": "Number",
              "Description": "Number",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.confidence",
              "EnumDisplayNames": null,
              "StableId": "all_detections.confidence"
            },
            "Children": []
          }
        ]
      }
    ]
  }
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "type": "predict_movement",
  "settings": {
    "points_source": {
      "provider_id": "",
      "source_id": ""
    },
    "min_samples": 3,
    "prediction_ms": 0,
    "reset_distance_threshold": 250,
    "max_sample_age_ms": 500,
    "prediction_model": "Automatic",
    "time_basis": "Execution",
    "max_prediction_distance": 500,
    "max_fit_error": 75,
    "minimum_confidence": 0.15
  },
  "id": "85d18ddf-cfef-1f36-8b83-b38783c5f34b",
  "inputs": {},
  "is_enabled": true
}
```

