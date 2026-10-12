## Steps: Objekte erkennen

ID: yolo_detection
Website: /doku/steps/yolo_detection/

Abbildung: [Detailansicht: Objekte erkennen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-yolo-detection.detail.png)

1. Bildquelle: Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.
2. YOLO-Modell: Wähle zuerst ein auf dem Zielrechner verfügbares YOLO-Modell und danach die gewünschte Objektklasse aus dessen Klassenliste. Das Modell bestimmt, welche Objekte erkannt werden können; eine freie Klassenbezeichnung ersetzt keinen Modell-Eintrag.
3. Bildbereich (ROI): Suchrechteck in Pixeln relativ zur Bildquelle. x/y bestimmen die linke obere Ecke, width/height die Größe; es muss zur Quelle passen.
4. Konfidenz (%): Mindestkonfidenz als Anteil von 0 bis 1, auch im referenzierten localValues-Wert: 0.9 bedeutet 90 %. Der Editor zeigt Prozent an.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Ein kompatibles installiertes Modell auswählen und Erkennungen einer Bildschirmaufnahme im Vorschaufenster einblenden.

### field.confidence_threshold

Mindestkonfidenz als Anteil von 0 bis 1, auch im referenzierten localValues-Wert: 0.9 bedeutet 90 %. Der Editor zeigt Prozent an.

### field.image_source

Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.

### field.roi

Suchrechteck in Pixeln relativ zur Bildquelle. x/y bestimmen die linke obere Ecke, width/height die Größe; es muss zur Quelle passen.

### field.yolo_selection

Wähle zuerst ein auf dem Zielrechner verfügbares YOLO-Modell und danach die gewünschte Objektklasse aus dessen Klassenliste. Das Modell bestimmt, welche Objekte erkannt werden können; eine freie Klassenbezeichnung ersetzt keinen Modell-Eintrag.

### input.dynamicRoi

dynamischer Suchbereich. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Bei kompatiblen Listen wird der erste Wert verwendet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.image

Bildobjekt einer Aufnahme. Fehlende erforderliche Werte führen zum Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Wähle ein installiertes YOLO-Modell, Bildquelle und Mindestkonfidenz. Das Modell liefert erkannte Klassen, Rechtecke und Punkte. Seine Klassen und Genauigkeit hängen vom gewählten Modell ab; found und all_detections beschreiben die tatsächlichen Treffer dieses Laufs.

### result.all_detections

Liste aller gelieferten Treffer mit Position, Bereich und eigener Erkennungsqualität. Ohne Treffer ist die Liste leer.

### result.all_detections.bounding_box

Bereich dieses Treffers in Pixeln. Bildschirmquellen verwenden Desktopkoordinaten, Kameras Bildkoordinaten. Bei einer leeren Trefferliste werden keine Bereiche geliefert.

### result.all_detections.bounding_box.bottom

Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.center

Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.center.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.center.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.height

Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.is_empty

Gibt an, ob der Bereich keine positive Breite oder Höhe hat.

### result.all_detections.bounding_box.left

Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.location

Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.location.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.location.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.right

Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.top

Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.width

Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.bounding_box.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.center

Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.center.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.center.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.

### result.all_detections.confidence

Bewertung der Erkennung von 0 bis 1. Höhere Werte bedeuten höhere Erkennungsqualität; 0,92 entspricht 92 %. Keine Garantie für einen richtigen Treffer.

### result.all_detections.count

Anzahl der Elemente in „Alle erkannten Treffer“. Eine leere Liste hat die Anzahl 0. Beispiel: 3.

### result.applied_roi

Tatsächlich verwendeter Suchbereich in Pixeln. Bei Bildschirmquellen ist der Desktopversatz bereits eingerechnet, bei Kameras sind es Bildkoordinaten. Ohne Eingrenzung kann der Wert fehlen. Beispiel: {X=10,Y=20,Width=300,Height=200}.

### result.applied_roi.bottom

Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.center

Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: {X=100,Y=200}.

### result.applied_roi.center.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.center.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.height

Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.is_empty

Gibt an, ob der Bereich keine positive Breite oder Höhe hat. Beispiel: true.

### result.applied_roi.left

Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.location

Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: {X=100,Y=200}.

### result.applied_roi.location.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.location.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.right

Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.top

Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.width

Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.applied_roi.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box

Rechteck um den erkannten Inhalt in Pixeln. Bildschirmquellen verwenden Desktopkoordinaten, Kameras Bildkoordinaten. Kann ohne Treffer fehlen. Beispiel: {X=10,Y=20,Width=300,Height=200}.

### result.bounding_box.bottom

Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.center

Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: {X=100,Y=200}.

### result.bounding_box.center.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.center.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.height

Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.is_empty

Gibt an, ob der Bereich keine positive Breite oder Höhe hat. Beispiel: true.

### result.bounding_box.left

Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.location

Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: {X=100,Y=200}.

### result.bounding_box.location.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.location.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.right

Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.top

Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.width

Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounding_box.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.confidence

Bewertung der Erkennung von 0 bis 1. Höhere Werte bedeuten höhere Erkennungsqualität; 0,92 entspricht 92 %. Keine Garantie für einen richtigen Treffer. Beispiel: 0.92.

### result.found

Gibt an, ob die Suche einen passenden Treffer geliefert hat. Kein Treffer ist ein gültiges Ergebnis. Beispiel: true.

### result.point

Position des ausgewählten Treffers in Pixeln, verwendbar als Klickziel. Bei Bildschirmquellen ist der Desktopversatz bereits eingerechnet; bei Kameras bleiben es Bildkoordinaten. Fehlt ohne Treffer. Beispiel: {X=100,Y=200}.

### result.point.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.point.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.source_capture_is_fresh

Frischezustand der Aufnahme, die für diese Erkennung verwendet wurde. false bedeutet, dass ein vorhandenes Bild wiederverwendet wurde. Beispiel: true.

### result.source_capture_timestamp_utc

UTC-Aufnahmezeit des Bildes, aus dem dieses Ergebnis berechnet wurde. Bei Wiederverwendung bleibt die ursprüngliche Aufnahmezeit erhalten. Beispiel: 2030-01-01T08:00:00Z.

### result.source_frame_timestamp

Monotoner Zeitstempel zur technischen Reihenfolgeprüfung. 0 bedeutet unbekannt. Kein Kalenderdatum und keine Millisekunden seit 1970. Beispiel: 3. In der App unter Diagnose aufgeführt.

### result.source_frame_version

Laufende Bildkennung innerhalb derselben Aufnahmequelle. Hilft zu erkennen, ob zwei Ergebnisse auf demselben Bild beruhen; nicht zwischen unterschiedlichen Quellen vergleichen. Beispiel: 3. In der App unter Diagnose aufgeführt.

### result.used_dynamic_roi

Gibt an, ob für diese Suche ein dynamischer Suchbereich übernommen wurde. Beispiel: true.

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

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen. Neue Verbindungen müssen die aktuellen Anbieter- und Quellformen erfüllen. Legacy-Regeln erhalten bestehende Referenzen und erweitern nicht automatisch die neue Auswahl.

### schema.inputs.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID. Die gemeinsame Eingabereferenz inputContracts/version=1 beschreibt erlaubte Member- und Itemverträge unabhängig vom Editor; bindingSchemas und fieldSchemas bleiben verfügbar.

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

### schema.settings.class_name

Name einer vom gewählten Modell gelieferten Klasse. Verwende vorhandene Modellklassen statt frei erfundener Objektbezeichnungen.

### schema.settings.confidence_threshold

Mindestkonfidenz als Anteil von 0 bis 1, auch im referenzierten localValues-Wert: 0.9 bedeutet 90 %. Der Editor zeigt Prozent an.

### schema.settings.dynamic_roi_source

Kompatible dynamische Rechteckquelle. Eine spätere DynamicRoi-Quelle darf Feedback für den nächsten Durchlauf liefern; dies ist eine besondere Ausnahme von normalen Vorwärtsreferenzen.

### schema.settings.dynamic_roi_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.dynamic_roi_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.dynamic_roi_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.dynamic_roi_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.dynamic_roi_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.dynamic_roi_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.dynamic_roi_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.dynamic_roi_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.dynamic_roi_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.enable_roi

Aktiviert den direkt konfigurierten Suchbereich. Ohne Aktivierung wird der Standardbereich der Quelle verwendet, sofern keine dynamische ROI-Quelle greift.

### schema.settings.image_source

Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.

### schema.settings.image_source.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.image_source.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.image_source.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.image_source.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.image_source.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.image_source.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.image_source.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.image_source.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.image_source.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.model

Modellkennung beziehungsweise Modellwahl der betreffenden Erkennung/Vorhersage. Sie bestimmt die unterstützten Ausgaben und muss auf dem Zielrechner verfügbar sein.

### schema.settings.roi

Suchrechteck in Pixeln relativ zur Bildquelle. x/y bestimmen die linke obere Ecke, width/height die Größe; es muss zur Quelle passen.

### schema.settings.roi.Empty

Vom serialisierten Geometrieobjekt angebotener Leerzustand. Kein eigener ROI-Schalter: den Suchbereich mit enable_roi beziehungsweise roi.enabled und den Koordinaten konfigurieren; dieses abgeleitete Mitglied nicht als Eingabequelle verwenden.

### schema.settings.roi.height

Höhe des Rechtecks oder Bildformats in Pixeln. Negative Größen sind kein gültiger Suchbereich.

### schema.settings.roi.width

Breite des Rechtecks oder Bildformats in Pixeln. Zusammen mit Höhe und Ursprung ergibt sie die räumliche Größe.

### schema.settings.roi.x

Horizontale Position der linken oberen Ecke des Suchrechtecks in Pixeln relativ zum Quellbild. Beispiel 100 startet den Bereich bei Bildkoordinate X = 100; Bildschirm- und Bildkoordinaten können voneinander abweichen.

### schema.settings.roi.y

Vertikale Position der linken oberen Ecke des Suchrechtecks in Pixeln relativ zum Quellbild. Beispiel 100 startet den Bereich bei Bildkoordinate Y = 100; Bildschirm- und Bildkoordinaten können voneinander abweichen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "yolo_detection",
  "name": "Objekte erkennen",
  "description": "Erkennt Objekte mithilfe eines YOLO-Modells und speichert ihre Position.",
  "category": "BildAuswerten",
  "uiFieldIds": [
    "image_source",
    "yolo_selection",
    "roi",
    "confidence_threshold"
  ],
  "fields": [
    {
      "id": "image_source",
      "name": "Bildquelle",
      "helpKey": "Ui.Step.Settings.ImageSource.Help",
      "help": "Bild, das dieser Step verarbeitet. Wähle beispielsweise die Ausgabe einer Bildschirm- oder Kameraaufnahme.",
      "descriptor": {
        "Id": "image_source",
        "LabelKey": "Ui.Step.Settings.ImageSource",
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
        "InputContractId": "image",
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "yolo_selection",
      "name": "YOLO-Modell",
      "helpKey": "Ui.Step.Settings.Model.Help",
      "help": "YOLO-Modell für die Objekterkennung. Das Modell bestimmt die verfügbaren Objektklassen.",
      "descriptor": {
        "Id": "yolo_selection",
        "LabelKey": "Ui.Step.Settings.Model",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": {
          "model": "",
          "class_name": ""
        },
        "DescriptionKey": null,
        "EditorHint": "yolo-picker",
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
        "YoloPickerOptions": {
          "RecommendedConfidenceTargetFieldId": "confidence_threshold"
        },
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": "desktopautomation.yolo-selection/v1",
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "roi",
      "name": "Bildbereich (ROI)",
      "helpKey": "Ui.Step.Settings.ROI.Help",
      "help": "Begrenzt die Bildverarbeitung auf einen ausgewählten Bereich. Ohne Begrenzung wird das vollständige Bild verwendet.",
      "descriptor": {
        "Id": "roi",
        "LabelKey": "Ui.Step.Settings.ROI",
        "ValueKind": "Object",
        "Required": false,
        "DefaultValue": {
          "enabled": false,
          "x": 0,
          "y": 0,
          "width": 0,
          "height": 0,
          "dynamic_source": null
        },
        "DescriptionKey": null,
        "EditorHint": "roi-picker",
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
        "RoiPickerOptions": {
          "DynamicInputContractId": "dynamicRoi"
        },
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": "desktopautomation.roi/v1",
        "OwnsStructuredInputContract": true,
        "InputValueKind": null
      }
    },
    {
      "id": "confidence_threshold",
      "name": "Konfidenz (%)",
      "helpKey": "Ui.Step.Settings.ConfidencePercent.Help",
      "help": "Mindestqualität, die ein Treffer erreichen muss. Höhere Werte akzeptieren nur sicherere Treffer.",
      "descriptor": {
        "Id": "confidence_threshold",
        "LabelKey": "Ui.Step.Settings.ConfidencePercent",
        "ValueKind": "Number",
        "Required": false,
        "DefaultValue": 0.5,
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
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
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
      "type": "YOLODetectionStepSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.class_name",
      "name": "class_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.confidence_threshold",
      "name": "confidence_threshold",
      "type": "Single",
      "options": [],
      "defaultValue": 0.5
    },
    {
      "id": "settings.dynamic_roi_source",
      "name": "dynamic_roi_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.dynamic_roi_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.dynamic_roi_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.dynamic_roi_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.enable_roi",
      "name": "enable_roi",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
    },
    {
      "id": "settings.image_source",
      "name": "image_source",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.image_source.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.image_source.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.image_source.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.model",
      "name": "model",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.roi",
      "name": "roi",
      "type": "PixelRegion",
      "options": [],
      "defaultValue": {
        "x": 0,
        "y": 0,
        "width": 0,
        "height": 0
      }
    },
    {
      "id": "settings.roi.Empty",
      "name": "Empty",
      "type": "PixelRegion",
      "options": [],
      "defaultValue": {
        "x": 0,
        "y": 0,
        "width": 0,
        "height": 0
      }
    },
    {
      "id": "settings.roi.height",
      "name": "height",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.roi.width",
      "name": "width",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.roi.x",
      "name": "x",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.roi.y",
      "name": "y",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    }
  ],
  "inputs": [
    {
      "Key": "image",
      "Required": true,
      "MissingValuePolicy": "FailStep",
      "CollectionConsumption": "NotApplicable",
      "AcceptedShapes": [
        {
          "ValueKind": "Image",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        }
      ],
      "AllowedProviderIds": [
        "step_result"
      ],
      "LegacyAllowedProviderIds": [],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    },
    {
      "Key": "dynamicRoi",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "FirstValue",
      "AcceptedShapes": [
        {
          "ValueKind": "Rectangle",
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
    "TypeName": "YOLODetectionResult",
    "DisplayName": "YOLODetectionResult",
    "Properties": [
      {
        "Name": "Found",
        "DisplayName": "Treffer gefunden",
        "DataType": "Boolean",
        "Description": "Gibt an, ob die Suche einen passenden Treffer geliefert hat. Kein Treffer ist ein gültiges Ergebnis.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "found",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "found"
      },
      {
        "Name": "Point",
        "DisplayName": "Erkannte Zielposition",
        "DataType": "Point",
        "Description": "Position des ausgewählten Treffers in Pixeln, verwendbar als Klickziel. Bei Bildschirmquellen ist der Desktopversatz bereits eingerechnet; bei Kameras bleiben es Bildkoordinaten. Fehlt ohne Treffer.",
        "IsNullable": true,
        "Example": "{X=100,Y=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "point"
      },
      {
        "Name": "Point.X",
        "DisplayName": "Erkannte Zielposition / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "point.x"
      },
      {
        "Name": "Point.Y",
        "DisplayName": "Erkannte Zielposition / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "point.y"
      },
      {
        "Name": "BoundingBox",
        "DisplayName": "Erkannter Bereich",
        "DataType": "Rectangle",
        "Description": "Rechteck um den erkannten Inhalt in Pixeln. Bildschirmquellen verwenden Desktopkoordinaten, Kameras Bildkoordinaten. Kann ohne Treffer fehlen.",
        "IsNullable": true,
        "Example": "{X=10,Y=20,Width=300,Height=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box"
      },
      {
        "Name": "BoundingBox.X",
        "DisplayName": "Erkannter Bereich / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.x"
      },
      {
        "Name": "BoundingBox.Y",
        "DisplayName": "Erkannter Bereich / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.y"
      },
      {
        "Name": "BoundingBox.Width",
        "DisplayName": "Erkannter Bereich / Breite (px)",
        "DataType": "Integer",
        "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.width",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.width"
      },
      {
        "Name": "BoundingBox.Height",
        "DisplayName": "Erkannter Bereich / Höhe (px)",
        "DataType": "Integer",
        "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.height",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.height"
      },
      {
        "Name": "BoundingBox.Left",
        "DisplayName": "Erkannter Bereich / Linke Kante (px)",
        "DataType": "Integer",
        "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.left",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.left"
      },
      {
        "Name": "BoundingBox.Top",
        "DisplayName": "Erkannter Bereich / Obere Kante (px)",
        "DataType": "Integer",
        "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.top",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.top"
      },
      {
        "Name": "BoundingBox.Right",
        "DisplayName": "Erkannter Bereich / Rechte Kante (px)",
        "DataType": "Integer",
        "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.right",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.right"
      },
      {
        "Name": "BoundingBox.Bottom",
        "DisplayName": "Erkannter Bereich / Untere Kante (px)",
        "DataType": "Integer",
        "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.bottom",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.bottom"
      },
      {
        "Name": "BoundingBox.IsEmpty",
        "DisplayName": "Erkannter Bereich / Bereich ist leer",
        "DataType": "Boolean",
        "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.is_empty",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.is_empty"
      },
      {
        "Name": "BoundingBox.Location",
        "DisplayName": "Erkannter Bereich / Linke obere Ecke",
        "DataType": "Point",
        "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.location"
      },
      {
        "Name": "BoundingBox.Location.X",
        "DisplayName": "Erkannter Bereich / Linke obere Ecke / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.location.x"
      },
      {
        "Name": "BoundingBox.Location.Y",
        "DisplayName": "Erkannter Bereich / Linke obere Ecke / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.location.y"
      },
      {
        "Name": "BoundingBox.Center",
        "DisplayName": "Erkannter Bereich / Mittelpunkt",
        "DataType": "Point",
        "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.center"
      },
      {
        "Name": "BoundingBox.Center.X",
        "DisplayName": "Erkannter Bereich / Mittelpunkt / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.center.x"
      },
      {
        "Name": "BoundingBox.Center.Y",
        "DisplayName": "Erkannter Bereich / Mittelpunkt / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounding_box.center.y"
      },
      {
        "Name": "Confidence",
        "DisplayName": "Erkennungsqualität",
        "DataType": "Number",
        "Description": "Bewertung der Erkennung von 0 bis 1. Höhere Werte bedeuten höhere Erkennungsqualität; 0,92 entspricht 92 %. Keine Garantie für einen richtigen Treffer.",
        "IsNullable": false,
        "Example": "0.92",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "confidence",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "confidence"
      },
      {
        "Name": "SourceCaptureIsFresh",
        "DisplayName": "Quellbild ist frisch",
        "DataType": "Boolean",
        "Description": "Frischezustand der Aufnahme, die für diese Erkennung verwendet wurde. false bedeutet, dass ein vorhandenes Bild wiederverwendet wurde.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_capture_is_fresh",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "source_capture_is_fresh"
      },
      {
        "Name": "SourceCaptureTimestampUtc",
        "DisplayName": "Aufnahmezeit des Quellbilds (UTC)",
        "DataType": "DateTime",
        "Description": "UTC-Aufnahmezeit des Bildes, aus dem dieses Ergebnis berechnet wurde. Bei Wiederverwendung bleibt die ursprüngliche Aufnahmezeit erhalten.",
        "IsNullable": false,
        "Example": "2030-01-01T08:00:00Z",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_capture_timestamp_utc",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "source_capture_timestamp_utc"
      },
      {
        "Name": "AppliedRoi",
        "DisplayName": "Verwendeter Suchbereich",
        "DataType": "Rectangle",
        "Description": "Tatsächlich verwendeter Suchbereich in Pixeln. Bei Bildschirmquellen ist der Desktopversatz bereits eingerechnet, bei Kameras sind es Bildkoordinaten. Ohne Eingrenzung kann der Wert fehlen.",
        "IsNullable": true,
        "Example": "{X=10,Y=20,Width=300,Height=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi"
      },
      {
        "Name": "AppliedRoi.X",
        "DisplayName": "Verwendeter Suchbereich / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.x"
      },
      {
        "Name": "AppliedRoi.Y",
        "DisplayName": "Verwendeter Suchbereich / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.y"
      },
      {
        "Name": "AppliedRoi.Width",
        "DisplayName": "Verwendeter Suchbereich / Breite (px)",
        "DataType": "Integer",
        "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.width",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.width"
      },
      {
        "Name": "AppliedRoi.Height",
        "DisplayName": "Verwendeter Suchbereich / Höhe (px)",
        "DataType": "Integer",
        "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.height",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.height"
      },
      {
        "Name": "AppliedRoi.Left",
        "DisplayName": "Verwendeter Suchbereich / Linke Kante (px)",
        "DataType": "Integer",
        "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.left",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.left"
      },
      {
        "Name": "AppliedRoi.Top",
        "DisplayName": "Verwendeter Suchbereich / Obere Kante (px)",
        "DataType": "Integer",
        "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.top",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.top"
      },
      {
        "Name": "AppliedRoi.Right",
        "DisplayName": "Verwendeter Suchbereich / Rechte Kante (px)",
        "DataType": "Integer",
        "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.right",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.right"
      },
      {
        "Name": "AppliedRoi.Bottom",
        "DisplayName": "Verwendeter Suchbereich / Untere Kante (px)",
        "DataType": "Integer",
        "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.bottom",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.bottom"
      },
      {
        "Name": "AppliedRoi.IsEmpty",
        "DisplayName": "Verwendeter Suchbereich / Bereich ist leer",
        "DataType": "Boolean",
        "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.is_empty",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.is_empty"
      },
      {
        "Name": "AppliedRoi.Location",
        "DisplayName": "Verwendeter Suchbereich / Linke obere Ecke",
        "DataType": "Point",
        "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.location",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.location"
      },
      {
        "Name": "AppliedRoi.Location.X",
        "DisplayName": "Verwendeter Suchbereich / Linke obere Ecke / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.location.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.location.x"
      },
      {
        "Name": "AppliedRoi.Location.Y",
        "DisplayName": "Verwendeter Suchbereich / Linke obere Ecke / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.location.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.location.y"
      },
      {
        "Name": "AppliedRoi.Center",
        "DisplayName": "Verwendeter Suchbereich / Mittelpunkt",
        "DataType": "Point",
        "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.center",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.center"
      },
      {
        "Name": "AppliedRoi.Center.X",
        "DisplayName": "Verwendeter Suchbereich / Mittelpunkt / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.center.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.center.x"
      },
      {
        "Name": "AppliedRoi.Center.Y",
        "DisplayName": "Verwendeter Suchbereich / Mittelpunkt / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.center.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "applied_roi.center.y"
      },
      {
        "Name": "UsedDynamicRoi",
        "DisplayName": "Dynamischer Suchbereich verwendet",
        "DataType": "Boolean",
        "Description": "Gibt an, ob für diese Suche ein dynamischer Suchbereich übernommen wurde.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "used_dynamic_roi",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "used_dynamic_roi"
      },
      {
        "Name": "AllDetections",
        "DisplayName": "Alle erkannten Treffer",
        "DataType": "Detection",
        "Description": "Liste aller gelieferten Treffer mit Position, Bereich und eigener Erkennungsqualität. Ohne Treffer ist die Liste leer.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections"
      },
      {
        "Name": "AllDetections.Count",
        "DisplayName": "Alle erkannten Treffer / Anzahl",
        "DataType": "Integer",
        "Description": "Anzahl der Elemente in „Alle erkannten Treffer“. Eine leere Liste hat die Anzahl 0.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.count",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.count"
      },
      {
        "Name": "AllDetections[].Center",
        "DisplayName": "Alle erkannten Treffer / Mittelpunkt",
        "DataType": "Point",
        "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.center"
      },
      {
        "Name": "AllDetections[].Center.X",
        "DisplayName": "Alle erkannten Treffer / Mittelpunkt / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.center.x"
      },
      {
        "Name": "AllDetections[].Center.Y",
        "DisplayName": "Alle erkannten Treffer / Mittelpunkt / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.center.y"
      },
      {
        "Name": "AllDetections[].BoundingBox",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich",
        "DataType": "Rectangle",
        "Description": "Bereich dieses Treffers in Pixeln. Bildschirmquellen verwenden Desktopkoordinaten, Kameras Bildkoordinaten. Bei einer leeren Trefferliste werden keine Bereiche geliefert.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box"
      },
      {
        "Name": "AllDetections[].BoundingBox.X",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Y",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.y"
      },
      {
        "Name": "AllDetections[].BoundingBox.Width",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Breite (px)",
        "DataType": "Integer",
        "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.width",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.width"
      },
      {
        "Name": "AllDetections[].BoundingBox.Height",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Höhe (px)",
        "DataType": "Integer",
        "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.height",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.height"
      },
      {
        "Name": "AllDetections[].BoundingBox.Left",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke Kante (px)",
        "DataType": "Integer",
        "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.left",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.left"
      },
      {
        "Name": "AllDetections[].BoundingBox.Top",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Obere Kante (px)",
        "DataType": "Integer",
        "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.top",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.top"
      },
      {
        "Name": "AllDetections[].BoundingBox.Right",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Rechte Kante (px)",
        "DataType": "Integer",
        "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.right",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.right"
      },
      {
        "Name": "AllDetections[].BoundingBox.Bottom",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Untere Kante (px)",
        "DataType": "Integer",
        "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.bottom",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.bottom"
      },
      {
        "Name": "AllDetections[].BoundingBox.IsEmpty",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Bereich ist leer",
        "DataType": "Boolean",
        "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.is_empty",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.is_empty"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke obere Ecke",
        "DataType": "Point",
        "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.location"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location.X",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke obere Ecke / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.location.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location.Y",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke obere Ecke / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.location.y"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Mittelpunkt",
        "DataType": "Point",
        "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.center"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center.X",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Mittelpunkt / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.center.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center.Y",
        "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Mittelpunkt / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.bounding_box.center.y"
      },
      {
        "Name": "AllDetections[].Confidence",
        "DisplayName": "Alle erkannten Treffer / Erkennungsqualität",
        "DataType": "Number",
        "Description": "Bewertung der Erkennung von 0 bis 1. Höhere Werte bedeuten höhere Erkennungsqualität; 0,92 entspricht 92 %. Keine Garantie für einen richtigen Treffer.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.confidence",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "all_detections.confidence"
      },
      {
        "Name": "SourceFrameVersion",
        "DisplayName": "Quellbild-Version",
        "DataType": "Integer",
        "Description": "Laufende Bildkennung innerhalb derselben Aufnahmequelle. Hilft zu erkennen, ob zwei Ergebnisse auf demselben Bild beruhen; nicht zwischen unterschiedlichen Quellen vergleichen.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_frame_version",
        "EnumDisplayNames": null,
        "IsDiagnostic": true,
        "StableId": "source_frame_version"
      },
      {
        "Name": "SourceFrameTimestamp",
        "DisplayName": "Quellbild-Zeitstempel (Ticks)",
        "DataType": "Integer",
        "Description": "Monotoner Zeitstempel zur technischen Reihenfolgeprüfung. 0 bedeutet unbekannt. Kein Kalenderdatum und keine Millisekunden seit 1970.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_frame_timestamp",
        "EnumDisplayNames": null,
        "IsDiagnostic": true,
        "StableId": "source_frame_timestamp"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Found",
        "DisplayName": "Treffer gefunden",
        "Property": {
          "Name": "Found",
          "DisplayName": "Treffer gefunden",
          "DataType": "Boolean",
          "Description": "Gibt an, ob die Suche einen passenden Treffer geliefert hat. Kein Treffer ist ein gültiges Ergebnis.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "found",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "found"
        },
        "Children": []
      },
      {
        "Segment": "Point",
        "DisplayName": "Erkannte Zielposition",
        "Property": {
          "Name": "Point",
          "DisplayName": "Erkannte Zielposition",
          "DataType": "Point",
          "Description": "Position des ausgewählten Treffers in Pixeln, verwendbar als Klickziel. Bei Bildschirmquellen ist der Desktopversatz bereits eingerechnet; bei Kameras bleiben es Bildkoordinaten. Fehlt ohne Treffer.",
          "IsNullable": true,
          "Example": "{X=100,Y=200}",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "point",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "point"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X-Koordinate (px)",
            "Property": {
              "Name": "Point.X",
              "DisplayName": "Erkannte Zielposition / X-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "point.x",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "point.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y-Koordinate (px)",
            "Property": {
              "Name": "Point.Y",
              "DisplayName": "Erkannte Zielposition / Y-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "point.y",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "point.y"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "BoundingBox",
        "DisplayName": "Erkannter Bereich",
        "Property": {
          "Name": "BoundingBox",
          "DisplayName": "Erkannter Bereich",
          "DataType": "Rectangle",
          "Description": "Rechteck um den erkannten Inhalt in Pixeln. Bildschirmquellen verwenden Desktopkoordinaten, Kameras Bildkoordinaten. Kann ohne Treffer fehlen.",
          "IsNullable": true,
          "Example": "{X=10,Y=20,Width=300,Height=200}",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "bounding_box",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "bounding_box"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X-Koordinate (px)",
            "Property": {
              "Name": "BoundingBox.X",
              "DisplayName": "Erkannter Bereich / X-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.x",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y-Koordinate (px)",
            "Property": {
              "Name": "BoundingBox.Y",
              "DisplayName": "Erkannter Bereich / Y-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.y",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Breite (px)",
            "Property": {
              "Name": "BoundingBox.Width",
              "DisplayName": "Erkannter Bereich / Breite (px)",
              "DataType": "Integer",
              "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.width",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Höhe (px)",
            "Property": {
              "Name": "BoundingBox.Height",
              "DisplayName": "Erkannter Bereich / Höhe (px)",
              "DataType": "Integer",
              "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.height",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Linke Kante (px)",
            "Property": {
              "Name": "BoundingBox.Left",
              "DisplayName": "Erkannter Bereich / Linke Kante (px)",
              "DataType": "Integer",
              "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.left",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Obere Kante (px)",
            "Property": {
              "Name": "BoundingBox.Top",
              "DisplayName": "Erkannter Bereich / Obere Kante (px)",
              "DataType": "Integer",
              "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.top",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Rechte Kante (px)",
            "Property": {
              "Name": "BoundingBox.Right",
              "DisplayName": "Erkannter Bereich / Rechte Kante (px)",
              "DataType": "Integer",
              "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.right",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Untere Kante (px)",
            "Property": {
              "Name": "BoundingBox.Bottom",
              "DisplayName": "Erkannter Bereich / Untere Kante (px)",
              "DataType": "Integer",
              "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.bottom",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Bereich ist leer",
            "Property": {
              "Name": "BoundingBox.IsEmpty",
              "DisplayName": "Erkannter Bereich / Bereich ist leer",
              "DataType": "Boolean",
              "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
              "IsNullable": false,
              "Example": "true",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.is_empty",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Linke obere Ecke",
            "Property": {
              "Name": "BoundingBox.Location",
              "DisplayName": "Erkannter Bereich / Linke obere Ecke",
              "DataType": "Point",
              "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "{X=100,Y=200}",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.location",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "BoundingBox.Location.X",
                  "DisplayName": "Erkannter Bereich / Linke obere Ecke / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.location.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounding_box.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "BoundingBox.Location.Y",
                  "DisplayName": "Erkannter Bereich / Linke obere Ecke / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.location.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounding_box.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Mittelpunkt",
            "Property": {
              "Name": "BoundingBox.Center",
              "DisplayName": "Erkannter Bereich / Mittelpunkt",
              "DataType": "Point",
              "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "{X=100,Y=200}",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.center",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounding_box.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "BoundingBox.Center.X",
                  "DisplayName": "Erkannter Bereich / Mittelpunkt / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.center.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounding_box.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "BoundingBox.Center.Y",
                  "DisplayName": "Erkannter Bereich / Mittelpunkt / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.center.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
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
        "DisplayName": "Erkennungsqualität",
        "Property": {
          "Name": "Confidence",
          "DisplayName": "Erkennungsqualität",
          "DataType": "Number",
          "Description": "Bewertung der Erkennung von 0 bis 1. Höhere Werte bedeuten höhere Erkennungsqualität; 0,92 entspricht 92 %. Keine Garantie für einen richtigen Treffer.",
          "IsNullable": false,
          "Example": "0.92",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "confidence",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "confidence"
        },
        "Children": []
      },
      {
        "Segment": "SourceCaptureIsFresh",
        "DisplayName": "Quellbild ist frisch",
        "Property": {
          "Name": "SourceCaptureIsFresh",
          "DisplayName": "Quellbild ist frisch",
          "DataType": "Boolean",
          "Description": "Frischezustand der Aufnahme, die für diese Erkennung verwendet wurde. false bedeutet, dass ein vorhandenes Bild wiederverwendet wurde.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_capture_is_fresh",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "source_capture_is_fresh"
        },
        "Children": []
      },
      {
        "Segment": "SourceCaptureTimestampUtc",
        "DisplayName": "Aufnahmezeit des Quellbilds (UTC)",
        "Property": {
          "Name": "SourceCaptureTimestampUtc",
          "DisplayName": "Aufnahmezeit des Quellbilds (UTC)",
          "DataType": "DateTime",
          "Description": "UTC-Aufnahmezeit des Bildes, aus dem dieses Ergebnis berechnet wurde. Bei Wiederverwendung bleibt die ursprüngliche Aufnahmezeit erhalten.",
          "IsNullable": false,
          "Example": "2030-01-01T08:00:00Z",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_capture_timestamp_utc",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "source_capture_timestamp_utc"
        },
        "Children": []
      },
      {
        "Segment": "AppliedRoi",
        "DisplayName": "Verwendeter Suchbereich",
        "Property": {
          "Name": "AppliedRoi",
          "DisplayName": "Verwendeter Suchbereich",
          "DataType": "Rectangle",
          "Description": "Tatsächlich verwendeter Suchbereich in Pixeln. Bei Bildschirmquellen ist der Desktopversatz bereits eingerechnet, bei Kameras sind es Bildkoordinaten. Ohne Eingrenzung kann der Wert fehlen.",
          "IsNullable": true,
          "Example": "{X=10,Y=20,Width=300,Height=200}",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "applied_roi",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "applied_roi"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X-Koordinate (px)",
            "Property": {
              "Name": "AppliedRoi.X",
              "DisplayName": "Verwendeter Suchbereich / X-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.x",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y-Koordinate (px)",
            "Property": {
              "Name": "AppliedRoi.Y",
              "DisplayName": "Verwendeter Suchbereich / Y-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.y",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Breite (px)",
            "Property": {
              "Name": "AppliedRoi.Width",
              "DisplayName": "Verwendeter Suchbereich / Breite (px)",
              "DataType": "Integer",
              "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.width",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Höhe (px)",
            "Property": {
              "Name": "AppliedRoi.Height",
              "DisplayName": "Verwendeter Suchbereich / Höhe (px)",
              "DataType": "Integer",
              "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.height",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Linke Kante (px)",
            "Property": {
              "Name": "AppliedRoi.Left",
              "DisplayName": "Verwendeter Suchbereich / Linke Kante (px)",
              "DataType": "Integer",
              "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.left",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Obere Kante (px)",
            "Property": {
              "Name": "AppliedRoi.Top",
              "DisplayName": "Verwendeter Suchbereich / Obere Kante (px)",
              "DataType": "Integer",
              "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.top",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Rechte Kante (px)",
            "Property": {
              "Name": "AppliedRoi.Right",
              "DisplayName": "Verwendeter Suchbereich / Rechte Kante (px)",
              "DataType": "Integer",
              "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.right",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Untere Kante (px)",
            "Property": {
              "Name": "AppliedRoi.Bottom",
              "DisplayName": "Verwendeter Suchbereich / Untere Kante (px)",
              "DataType": "Integer",
              "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.bottom",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Bereich ist leer",
            "Property": {
              "Name": "AppliedRoi.IsEmpty",
              "DisplayName": "Verwendeter Suchbereich / Bereich ist leer",
              "DataType": "Boolean",
              "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
              "IsNullable": false,
              "Example": "true",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.is_empty",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Linke obere Ecke",
            "Property": {
              "Name": "AppliedRoi.Location",
              "DisplayName": "Verwendeter Suchbereich / Linke obere Ecke",
              "DataType": "Point",
              "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "{X=100,Y=200}",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.location",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "AppliedRoi.Location.X",
                  "DisplayName": "Verwendeter Suchbereich / Linke obere Ecke / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.location.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "applied_roi.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "AppliedRoi.Location.Y",
                  "DisplayName": "Verwendeter Suchbereich / Linke obere Ecke / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.location.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "applied_roi.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Mittelpunkt",
            "Property": {
              "Name": "AppliedRoi.Center",
              "DisplayName": "Verwendeter Suchbereich / Mittelpunkt",
              "DataType": "Point",
              "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "{X=100,Y=200}",
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.center",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "applied_roi.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "AppliedRoi.Center.X",
                  "DisplayName": "Verwendeter Suchbereich / Mittelpunkt / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.center.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "applied_roi.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "AppliedRoi.Center.Y",
                  "DisplayName": "Verwendeter Suchbereich / Mittelpunkt / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.center.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "applied_roi.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "UsedDynamicRoi",
        "DisplayName": "Dynamischer Suchbereich verwendet",
        "Property": {
          "Name": "UsedDynamicRoi",
          "DisplayName": "Dynamischer Suchbereich verwendet",
          "DataType": "Boolean",
          "Description": "Gibt an, ob für diese Suche ein dynamischer Suchbereich übernommen wurde.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "used_dynamic_roi",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "used_dynamic_roi"
        },
        "Children": []
      },
      {
        "Segment": "AllDetections",
        "DisplayName": "Alle erkannten Treffer",
        "Property": {
          "Name": "AllDetections",
          "DisplayName": "Alle erkannten Treffer",
          "DataType": "Detection",
          "Description": "Liste aller gelieferten Treffer mit Position, Bereich und eigener Erkennungsqualität. Ohne Treffer ist die Liste leer.",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "all_detections",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "all_detections"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Anzahl",
            "Property": {
              "Name": "AllDetections.Count",
              "DisplayName": "Alle erkannten Treffer / Anzahl",
              "DataType": "Integer",
              "Description": "Anzahl der Elemente in „Alle erkannten Treffer“. Eine leere Liste hat die Anzahl 0.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.count",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "all_detections.count"
            },
            "Children": []
          },
          {
            "Segment": "Center",
            "DisplayName": "Mittelpunkt",
            "Property": {
              "Name": "AllDetections[].Center",
              "DisplayName": "Alle erkannten Treffer / Mittelpunkt",
              "DataType": "Point",
              "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.center",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "all_detections.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "AllDetections[].Center.X",
                  "DisplayName": "Alle erkannten Treffer / Mittelpunkt / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.center.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "AllDetections[].Center.Y",
                  "DisplayName": "Alle erkannten Treffer / Mittelpunkt / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.center.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.center.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "BoundingBox",
            "DisplayName": "Erkannter Bereich",
            "Property": {
              "Name": "AllDetections[].BoundingBox",
              "DisplayName": "Alle erkannten Treffer / Erkannter Bereich",
              "DataType": "Rectangle",
              "Description": "Bereich dieses Treffers in Pixeln. Bildschirmquellen verwenden Desktopkoordinaten, Kameras Bildkoordinaten. Bei einer leeren Trefferliste werden keine Bereiche geliefert.",
              "IsNullable": true,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.bounding_box",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "all_detections.bounding_box"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.X",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Y",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.y"
                },
                "Children": []
              },
              {
                "Segment": "Width",
                "DisplayName": "Breite (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Width",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Breite (px)",
                  "DataType": "Integer",
                  "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.width",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.width"
                },
                "Children": []
              },
              {
                "Segment": "Height",
                "DisplayName": "Höhe (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Height",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Höhe (px)",
                  "DataType": "Integer",
                  "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.height",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.height"
                },
                "Children": []
              },
              {
                "Segment": "Left",
                "DisplayName": "Linke Kante (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Left",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke Kante (px)",
                  "DataType": "Integer",
                  "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.left",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.left"
                },
                "Children": []
              },
              {
                "Segment": "Top",
                "DisplayName": "Obere Kante (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Top",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Obere Kante (px)",
                  "DataType": "Integer",
                  "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.top",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.top"
                },
                "Children": []
              },
              {
                "Segment": "Right",
                "DisplayName": "Rechte Kante (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Right",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Rechte Kante (px)",
                  "DataType": "Integer",
                  "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.right",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.right"
                },
                "Children": []
              },
              {
                "Segment": "Bottom",
                "DisplayName": "Untere Kante (px)",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Bottom",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Untere Kante (px)",
                  "DataType": "Integer",
                  "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.bottom",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.bottom"
                },
                "Children": []
              },
              {
                "Segment": "IsEmpty",
                "DisplayName": "Bereich ist leer",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.IsEmpty",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Bereich ist leer",
                  "DataType": "Boolean",
                  "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.is_empty",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.is_empty"
                },
                "Children": []
              },
              {
                "Segment": "Location",
                "DisplayName": "Linke obere Ecke",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Location",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke obere Ecke",
                  "DataType": "Point",
                  "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.location",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.location"
                },
                "Children": [
                  {
                    "Segment": "X",
                    "DisplayName": "X-Koordinate (px)",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Location.X",
                      "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke obere Ecke / X-Koordinate (px)",
                      "DataType": "Integer",
                      "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.location.x",
                      "EnumDisplayNames": null,
                      "IsDiagnostic": false,
                      "StableId": "all_detections.bounding_box.location.x"
                    },
                    "Children": []
                  },
                  {
                    "Segment": "Y",
                    "DisplayName": "Y-Koordinate (px)",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Location.Y",
                      "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Linke obere Ecke / Y-Koordinate (px)",
                      "DataType": "Integer",
                      "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.location.y",
                      "EnumDisplayNames": null,
                      "IsDiagnostic": false,
                      "StableId": "all_detections.bounding_box.location.y"
                    },
                    "Children": []
                  }
                ]
              },
              {
                "Segment": "Center",
                "DisplayName": "Mittelpunkt",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Center",
                  "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Mittelpunkt",
                  "DataType": "Point",
                  "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.center",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "all_detections.bounding_box.center"
                },
                "Children": [
                  {
                    "Segment": "X",
                    "DisplayName": "X-Koordinate (px)",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Center.X",
                      "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Mittelpunkt / X-Koordinate (px)",
                      "DataType": "Integer",
                      "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.center.x",
                      "EnumDisplayNames": null,
                      "IsDiagnostic": false,
                      "StableId": "all_detections.bounding_box.center.x"
                    },
                    "Children": []
                  },
                  {
                    "Segment": "Y",
                    "DisplayName": "Y-Koordinate (px)",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Center.Y",
                      "DisplayName": "Alle erkannten Treffer / Erkannter Bereich / Mittelpunkt / Y-Koordinate (px)",
                      "DataType": "Integer",
                      "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.center.y",
                      "EnumDisplayNames": null,
                      "IsDiagnostic": false,
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
            "DisplayName": "Erkennungsqualität",
            "Property": {
              "Name": "AllDetections[].Confidence",
              "DisplayName": "Alle erkannten Treffer / Erkennungsqualität",
              "DataType": "Number",
              "Description": "Bewertung der Erkennung von 0 bis 1. Höhere Werte bedeuten höhere Erkennungsqualität; 0,92 entspricht 92 %. Keine Garantie für einen richtigen Treffer.",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.confidence",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "all_detections.confidence"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "SourceFrameVersion",
        "DisplayName": "Quellbild-Version",
        "Property": {
          "Name": "SourceFrameVersion",
          "DisplayName": "Quellbild-Version",
          "DataType": "Integer",
          "Description": "Laufende Bildkennung innerhalb derselben Aufnahmequelle. Hilft zu erkennen, ob zwei Ergebnisse auf demselben Bild beruhen; nicht zwischen unterschiedlichen Quellen vergleichen.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_frame_version",
          "EnumDisplayNames": null,
          "IsDiagnostic": true,
          "StableId": "source_frame_version"
        },
        "Children": []
      },
      {
        "Segment": "SourceFrameTimestamp",
        "DisplayName": "Quellbild-Zeitstempel (Ticks)",
        "Property": {
          "Name": "SourceFrameTimestamp",
          "DisplayName": "Quellbild-Zeitstempel (Ticks)",
          "DataType": "Integer",
          "Description": "Monotoner Zeitstempel zur technischen Reihenfolgeprüfung. 0 bedeutet unbekannt. Kein Kalenderdatum und keine Millisekunden seit 1970.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_frame_timestamp",
          "EnumDisplayNames": null,
          "IsDiagnostic": true,
          "StableId": "source_frame_timestamp"
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
  "type": "yolo_detection",
  "settings": {
    "model": "",
    "confidence_threshold": 0.5,
    "class_name": "",
    "roi": {
      "x": 0,
      "y": 0,
      "width": 0,
      "height": 0
    },
    "enable_roi": false,
    "image_source": {
      "provider_id": "",
      "source_id": ""
    },
    "dynamic_roi_source": {
      "provider_id": "",
      "source_id": ""
    }
  },
  "id": "84d7921e-2d7a-d7f3-ad05-8f65a32352c8",
  "inputs": {},
  "is_enabled": true
}
```

