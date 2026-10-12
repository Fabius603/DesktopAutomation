## Steps: Bild speichern

ID: save_image
Website: /doku/steps/save_image/

Abbildung: [Detailansicht: Bild speichern. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-save-image.detail.png)

1. Bildquelle: Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.
2. Speicherpfad: Ordner für die Bild-/Videoausgabe. Er muss auf dem Zielrechner erreichbar und beschreibbar sein.
3. Dateiname: Dateiname der Bild-/Videoausgabe. Wähle ein zur Ausgabe passendes Format und prüfe vorhandene Dateien am Ziel. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.
4. Einblendungen: Zusammengesetzte Darstellungseinstellungen für Erkennungen und Text. Ab dem kommenden Update 1.6.2 können Textzeilen direkt eingegebene lokale Werte, gemeinsame Job-Werte oder vorherige Step-Ergebnisse anzeigen. Ergebnisquellen müssen vorher erzeugt worden sein. Ab dem kommenden Update 1.6.2 sind die direkt erzeugten Standardwerte für Schriftgröße und Deckkraft gültig; passende optionale Einzelwerte lassen sich für die Einstellungen auswählen.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Eine Bildschirmaufnahme als PNG in C:\DesktopAutomation-Beispiele\Ausgabe speichern; den zurückgegebenen file_path für eine Folgeaktion verwenden.

### field.file_name

Dateiname der Bild-/Videoausgabe. Wähle ein zur Ausgabe passendes Format und prüfe vorhandene Dateien am Ziel. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.

### field.image_source

Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.

### field.overlay

Zusammengesetzte Darstellungseinstellungen für Erkennungen und Text. Ab dem kommenden Update 1.6.2 können Textzeilen direkt eingegebene lokale Werte, gemeinsame Job-Werte oder vorherige Step-Ergebnisse anzeigen. Ergebnisquellen müssen vorher erzeugt worden sein. Ab dem kommenden Update 1.6.2 sind die direkt erzeugten Standardwerte für Schriftgröße und Deckkraft gültig; passende optionale Einzelwerte lassen sich für die Einstellungen auswählen.

### field.save_path

Ordner für die Bild-/Videoausgabe. Er muss auf dem Zielrechner erreichbar und beschreibbar sein.

### input.detections

Einblendungsquellen mit Geometrie. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Alle kompatiblen Listeneinträge werden verarbeitet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.image

Bildobjekt einer Aufnahme. Fehlende erforderliche Werte führen zum Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.text

Text beziehungsweise darstellbarer Wert. Ab dem kommenden Update 1.6.2 sind lokale Werte (local_value), gemeinsame Job-Werte (job_variable) und vorherige Step-Ergebnisse (step_result) erlaubt. Typ und Kardinalität müssen passen. Eine gültige, noch nicht verfügbare optionale Step-Quelle kann zum Überspringen führen; ungültige Referenzen bleiben Fehler. Ab dem kommenden Update 1.6.2 werden alle darstellbaren Werttypen einschließlich strukturierter Objekte und Listen akzeptiert. Listen erscheinen zeilenweise vollständig, Objekte als JSON und Zahlen im aktuellen Zahlenformat. Bilddaten sind ausgeschlossen.

### purpose

Wähle ein Bild, Ausgabeordner und Dateiname. Optional zeichnest du Einblendungen in die gespeicherte Kopie. file_path und saved_at_utc beschreiben die Ausgabe; die Bildquelle selbst bleibt eine Laufzeitressource und wird nicht als Bitmap in die Jobdatei geschrieben.

### result.file_name

Name der gespeicherten Bilddatei ohne Ordnerpfad. Beispiel: example.png.

### result.file_path

Vollständiger Pfad der gespeicherten Bilddatei, verwendbar für nachfolgende Dateiaktionen. Beispiel: C:\Data\example.png.

### result.file_size_bytes

Größe der gespeicherten Bilddatei in Bytes. Beispiel: 3.

### result.format

Format der gespeicherten Datei, beispielsweise PNG oder JPEG. Beispiel: PNG.

### result.height

Höhe des gespeicherten Bildes in Pixeln. Beispiel: 3.

### result.saved_at_utc

UTC-Zeitpunkt, zu dem das Bild gespeichert wurde. Beispiel: 2030-01-01T08:00:00Z.

### result.width

Breite des gespeicherten Bildes in Pixeln. Beispiel: 3.

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

### schema.settings.file_name

Dateiname der Bild-/Videoausgabe. Wähle ein zur Ausgabe passendes Format und prüfe vorhandene Dateien am Ziel.

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

### schema.settings.overlay

Zusammengesetzte Darstellungseinstellungen für Erkennungen und Text. Die zugrunde liegenden Ergebnisse müssen vorher erzeugt worden sein.

### schema.settings.overlay.detection_results

Liste referenzierter Erkennungsergebnisse für die Einblendung.

### schema.settings.overlay.detection_results.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.overlay.detection_results.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.overlay.detection_results.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.overlay.detection_results.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.overlay.detection_results.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.overlay.detection_results.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.overlay.detection_results.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.overlay.detection_results.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.overlay.detection_results.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.overlay.text_results

Liste referenzierter Text-/Anzeige-Werte für die Einblendung.

### schema.settings.overlay.text_results.clear_on_job_end

Entfernt die durch diesen Job erzeugte Anzeige beim regulären Aufräumen.

### schema.settings.overlay.text_results.desktop_index

Nullbasierter Zielmonitor der Desktop-Anzeige oder Benutzerauswahl. Prüfe ihn auf dem Zielrechner.

### schema.settings.overlay.text_results.duration_ms

Anzeigedauer in Millisekunden. Beachte die vom Editor angebotenen Grenzen und die zusätzliche Entfernung beim Job-Ende.

### schema.settings.overlay.text_results.font_color

Hex-Farbwert der Textanzeige. Wähle einen ausreichenden Kontrast zum Hintergrund. Ab dem kommenden Update 1.6.2 bleiben vorhandene Text-Farbreferenzen mit gültigem Farbtoken auch in verschachtelten Einblendungen zulässig. Neue Farbquellen verwenden Color.

### schema.settings.overlay.text_results.font_size

Schriftgröße der Textanzeige in Punkten. Prüfe Lesbarkeit auf dem tatsächlichen Monitor und dessen Skalierung.

### schema.settings.overlay.text_results.id

Stabile Identität dieses Eintrags. In einem Job müssen Step-IDs eindeutig sein; beim Kopieren eines eigenständigen Jobs/Makros eine neue Objekt-GUID erzeugen und interne Referenzen gezielt anpassen.

### schema.settings.overlay.text_results.offset_x

Zusätzlicher horizontaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.overlay.text_results.offset_y

Zusätzlicher vertikaler Versatz in Pixeln relativ zum ermittelten Punkt, Referenzpunkt oder Anzeigeursprung.

### schema.settings.overlay.text_results.opacity

Deckkraft als Anteil von 0 bis 1, auch im referenzierten localValues-Wert. 1 bedeutet vollständig sichtbar, 0 unsichtbar. Der Editor zeigt Prozent an.

### schema.settings.overlay.text_results.result

Typisierte Bindung der Textzeile. Ab dem kommenden Update 1.6.2 kann provider_id=local_value mit source_id=<GUID aus localValues> einen direkt eingegebenen Text referenzieren. Alternativ job_variable oder step_result verwenden. Bei Step-Ergebnissen die Ausführungsreihenfolge beachten.

### schema.settings.overlay.text_results.result.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.overlay.text_results.result.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.overlay.text_results.result.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.overlay.text_results.result.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.overlay.text_results.result.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.overlay.text_results.result.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.overlay.text_results.result.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.overlay.text_results.result.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.overlay.text_results.result.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.save_path

Ordner für die Bild-/Videoausgabe. Er muss auf dem Zielrechner erreichbar und beschreibbar sein.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "save_image",
  "name": "Bild speichern",
  "description": "Speichert die ausgewählte Bildquelle als Bilddatei.",
  "category": "AnzeigenSpeichern",
  "uiFieldIds": [
    "image_source",
    "save_path",
    "file_name",
    "overlay"
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
      "id": "save_path",
      "name": "Speicherpfad",
      "helpKey": "Ui.Step.Settings.SavePath.Help",
      "help": "Zielordner für die Ausgabedatei.",
      "descriptor": {
        "Id": "save_path",
        "LabelKey": "Ui.Step.Settings.SavePath",
        "ValueKind": "DirectoryPath",
        "Required": true,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "directory-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 1,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": null,
        "DirectoryPickerOptions": {
          "SuggestedDirectory": "Pictures",
          "SuggestedSubfolder": "DesktopAutomation"
        },
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
      "id": "file_name",
      "name": "Dateiname",
      "helpKey": "Ui.Step.Settings.FileName.Help",
      "help": "Name der Ausgabedatei. Der Speicherpfad bestimmt den Zielordner.",
      "descriptor": {
        "Id": "file_name",
        "LabelKey": "Ui.Step.Settings.FileName",
        "ValueKind": "Text",
        "Required": true,
        "DefaultValue": "image.png",
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
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "overlay",
      "name": "Einblendungen",
      "helpKey": "Ui.Step.Settings.Overlays.Help",
      "help": "Einblendungen für Erkennungsergebnisse oder Text. Jede Einblendung besitzt eigene Darstellungswerte.",
      "descriptor": {
        "Id": "overlay",
        "LabelKey": "Ui.Step.Settings.Overlays",
        "ValueKind": "Object",
        "Required": false,
        "DefaultValue": {
          "detection_results": [],
          "text_results": []
        },
        "DescriptionKey": null,
        "EditorHint": "visual-overlay",
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 3,
        "VisibleWhen": null,
        "Options": null,
        "InputContractId": null,
        "VisibleWhenAll": null,
        "VisualOverlayOptions": {
          "DetectionInputContractId": "detections",
          "TextInputContractId": "text",
          "SupportsDesktopPlacement": false
        },
        "DirectoryPickerOptions": null,
        "FilePickerOptions": null,
        "RoiPickerOptions": null,
        "YoloPickerOptions": null,
        "WindowsCapabilityPickerOptions": null,
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": "desktopautomation.visual-overlay/v1",
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
      "type": "SaveImageSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.file_name",
      "name": "file_name",
      "type": "String",
      "options": [],
      "defaultValue": "image.png"
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
      "id": "settings.overlay",
      "name": "overlay",
      "type": "VisualOverlaySettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results",
      "name": "detection_results",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.detection_results.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results",
      "name": "text_results",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.clear_on_job_end",
      "name": "clear_on_job_end",
      "type": "Boolean",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.desktop_index",
      "name": "desktop_index",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.duration_ms",
      "name": "duration_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.font_color",
      "name": "font_color",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.font_size",
      "name": "font_size",
      "type": "Single",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.id",
      "name": "id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.offset_x",
      "name": "offset_x",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.offset_y",
      "name": "offset_y",
      "type": "Int32",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.opacity",
      "name": "opacity",
      "type": "Single",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result",
      "name": "result",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.overlay.text_results.result.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.save_path",
      "name": "save_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
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
      "Key": "detections",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "AllValues",
      "AcceptedShapes": [
        {
          "ValueKind": "Detection",
          "Cardinalities": [
            "Collection"
          ]
        },
        {
          "ValueKind": "Rectangle",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
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
    },
    {
      "Key": "text",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "AllValues",
      "AcceptedShapes": [
        {
          "ValueKind": "Text",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Boolean",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Integer",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Number",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "DateTime",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Point",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Rectangle",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Detection",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "ProcessReference",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "ResultObject",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Enum",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "JobReference",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "MacroReference",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "Color",
          "Cardinalities": [
            "Single",
            "OptionalSingle",
            "Collection"
          ]
        },
        {
          "ValueKind": "FilePath",
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
        "secret"
      ],
      "AllowsDirectValue": true,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "SaveImageResult",
    "DisplayName": "SaveImageResult",
    "Properties": [
      {
        "Name": "FilePath",
        "DisplayName": "Vollständiger Dateipfad",
        "DataType": "Text",
        "Description": "Vollständiger Pfad der gespeicherten Bilddatei, verwendbar für nachfolgende Dateiaktionen.",
        "IsNullable": true,
        "Example": "C:\\Data\\example.png",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "file_path",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "file_path"
      },
      {
        "Name": "FileName",
        "DisplayName": "Dateiname",
        "DataType": "Text",
        "Description": "Name der gespeicherten Bilddatei ohne Ordnerpfad.",
        "IsNullable": true,
        "Example": "example.png",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "file_name",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "file_name"
      },
      {
        "Name": "Format",
        "DisplayName": "Bildformat",
        "DataType": "Text",
        "Description": "Format der gespeicherten Datei, beispielsweise PNG oder JPEG.",
        "IsNullable": true,
        "Example": "PNG",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "format",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "format"
      },
      {
        "Name": "Width",
        "DisplayName": "Bildbreite",
        "DataType": "Integer",
        "Description": "Breite des gespeicherten Bildes in Pixeln.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "width",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "width"
      },
      {
        "Name": "Height",
        "DisplayName": "Bildhöhe",
        "DataType": "Integer",
        "Description": "Höhe des gespeicherten Bildes in Pixeln.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "height",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "height"
      },
      {
        "Name": "FileSizeBytes",
        "DisplayName": "Dateigröße (Bytes)",
        "DataType": "Integer",
        "Description": "Größe der gespeicherten Bilddatei in Bytes.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "file_size_bytes",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "file_size_bytes"
      },
      {
        "Name": "SavedAtUtc",
        "DisplayName": "Gespeichert am (UTC)",
        "DataType": "DateTime",
        "Description": "UTC-Zeitpunkt, zu dem das Bild gespeichert wurde.",
        "IsNullable": false,
        "Example": "2030-01-01T08:00:00Z",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "saved_at_utc",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "saved_at_utc"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "FilePath",
        "DisplayName": "Vollständiger Dateipfad",
        "Property": {
          "Name": "FilePath",
          "DisplayName": "Vollständiger Dateipfad",
          "DataType": "Text",
          "Description": "Vollständiger Pfad der gespeicherten Bilddatei, verwendbar für nachfolgende Dateiaktionen.",
          "IsNullable": true,
          "Example": "C:\\Data\\example.png",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "file_path",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "file_path"
        },
        "Children": []
      },
      {
        "Segment": "FileName",
        "DisplayName": "Dateiname",
        "Property": {
          "Name": "FileName",
          "DisplayName": "Dateiname",
          "DataType": "Text",
          "Description": "Name der gespeicherten Bilddatei ohne Ordnerpfad.",
          "IsNullable": true,
          "Example": "example.png",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "file_name",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "file_name"
        },
        "Children": []
      },
      {
        "Segment": "Format",
        "DisplayName": "Bildformat",
        "Property": {
          "Name": "Format",
          "DisplayName": "Bildformat",
          "DataType": "Text",
          "Description": "Format der gespeicherten Datei, beispielsweise PNG oder JPEG.",
          "IsNullable": true,
          "Example": "PNG",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "format",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "format"
        },
        "Children": []
      },
      {
        "Segment": "Width",
        "DisplayName": "Bildbreite",
        "Property": {
          "Name": "Width",
          "DisplayName": "Bildbreite",
          "DataType": "Integer",
          "Description": "Breite des gespeicherten Bildes in Pixeln.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "width",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "width"
        },
        "Children": []
      },
      {
        "Segment": "Height",
        "DisplayName": "Bildhöhe",
        "Property": {
          "Name": "Height",
          "DisplayName": "Bildhöhe",
          "DataType": "Integer",
          "Description": "Höhe des gespeicherten Bildes in Pixeln.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "height",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "height"
        },
        "Children": []
      },
      {
        "Segment": "FileSizeBytes",
        "DisplayName": "Dateigröße (Bytes)",
        "Property": {
          "Name": "FileSizeBytes",
          "DisplayName": "Dateigröße (Bytes)",
          "DataType": "Integer",
          "Description": "Größe der gespeicherten Bilddatei in Bytes.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "file_size_bytes",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "file_size_bytes"
        },
        "Children": []
      },
      {
        "Segment": "SavedAtUtc",
        "DisplayName": "Gespeichert am (UTC)",
        "Property": {
          "Name": "SavedAtUtc",
          "DisplayName": "Gespeichert am (UTC)",
          "DataType": "DateTime",
          "Description": "UTC-Zeitpunkt, zu dem das Bild gespeichert wurde.",
          "IsNullable": false,
          "Example": "2030-01-01T08:00:00Z",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "saved_at_utc",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "saved_at_utc"
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
  "type": "save_image",
  "settings": {
    "save_path": "",
    "file_name": "image.png",
    "image_source": {
      "provider_id": "",
      "source_id": ""
    },
    "overlay": {
      "detection_results": [],
      "text_results": []
    }
  },
  "id": "469a3886-c8fa-1eba-0a8f-bbed4435a3cc",
  "inputs": {},
  "is_enabled": true
}
```

