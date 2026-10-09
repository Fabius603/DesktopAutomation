## Steps: Bild speichern

ID: save_image
Website: /doku/steps/save_image/

Abbildung: [Detailansicht: Bild speichern. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-save-image.detail.png)

1. Bildquelle: Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.
2. Speicherpfad: Ordner für die Bild-/Videoausgabe. Er muss auf dem Zielrechner erreichbar und beschreibbar sein.
3. Dateiname: Dateiname der Bild-/Videoausgabe. Wähle ein zur Ausgabe passendes Format und prüfe vorhandene Dateien am Ziel.
4. Einblendungen: Zusammengesetzte Darstellungseinstellungen für Erkennungen und Text. Die zugrunde liegenden Ergebnisse müssen vorher erzeugt worden sein.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Eine Bildschirmaufnahme als PNG in C:\DesktopAutomation-Beispiele\Ausgabe speichern; den zurückgegebenen file_path für eine Folgeaktion verwenden.

### field.file_name

Dateiname der Bild-/Videoausgabe. Wähle ein zur Ausgabe passendes Format und prüfe vorhandene Dateien am Ziel.

### field.image_source

Bildobjekt eines Aufnahme-Steps. Ein Dateipfad als Text ist nicht automatisch eine kompatible Bildquelle.

### field.overlay

Zusammengesetzte Darstellungseinstellungen für Erkennungen und Text. Die zugrunde liegenden Ergebnisse müssen vorher erzeugt worden sein.

### field.save_path

Ordner für die Bild-/Videoausgabe. Er muss auf dem Zielrechner erreichbar und beschreibbar sein.

### input.detections

Einblendungsquellen mit Geometrie. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Alle kompatiblen Listeneinträge werden verarbeitet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.image

Bildobjekt einer Aufnahme. Fehlende erforderliche Werte führen zum Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.text

Text beziehungsweise darstellbarer Wert. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Alle kompatiblen Listeneinträge werden verarbeitet. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Wähle ein Bild, Ausgabeordner und Dateiname. Optional zeichnest du Einblendungen in die gespeicherte Kopie. file_path und saved_at_utc beschreiben die Ausgabe; die Bildquelle selbst bleibt eine Laufzeitressource und wird nicht als Bitmap in die Jobdatei geschrieben.

### result.file_name

Name der erzeugten Datei ohne zwingend den ganzen Verzeichnispfad. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.file_path

Pfad der erzeugten Bild-/Videodatei auf dem ausführenden Rechner. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.file_size_bytes

Größe der gespeicherten Datei in Bytes. Einzelwert dieses Ergebnisses.

### result.format

Ausgabeformat der Bild-/Videodatei. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.height

Höhe des Ergebnisrechtecks in Pixeln. Einzelwert dieses Ergebnisses.

### result.saved_at_utc

UTC-Zeitpunkt der Speicherung/Finalisierung. Einzelwert dieses Ergebnisses.

### result.width

Breite des Ergebnisrechtecks in Pixeln. Einzelwert dieses Ergebnisses.

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

Hex-Farbwert der Textanzeige. Wähle einen ausreichenden Kontrast zum Hintergrund.

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

Typisierte Ergebnisreferenz für den betreffenden Wert; Quelle und Ergebnis-ID auswählen und die Ausführungsreihenfolge beachten.

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
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "save_path",
      "name": "Speicherpfad",
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
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "file_name",
      "name": "Dateiname",
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
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "overlay",
      "name": "Einblendungen",
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
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Boolean",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Integer",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Number",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "DateTime",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Color",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "FilePath",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
          ]
        },
        {
          "ValueKind": "Enum",
          "Cardinalities": [
            "Single",
            "OptionalSingle"
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
        }
      ],
      "AllowedProviderIds": [
        "job_variable",
        "step_result"
      ],
      "LegacyAllowedProviderIds": [
        "secret"
      ],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    }
  ],
  "result": {
    "TypeName": "SaveImageResult",
    "DisplayName": "SaveImageResult",
    "Properties": [
      {
        "Name": "FilePath",
        "DisplayName": "File Path",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "file_path",
        "EnumDisplayNames": null,
        "StableId": "file_path"
      },
      {
        "Name": "FileName",
        "DisplayName": "File Name",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "file_name",
        "EnumDisplayNames": null,
        "StableId": "file_name"
      },
      {
        "Name": "Format",
        "DisplayName": "Format",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "format",
        "EnumDisplayNames": null,
        "StableId": "format"
      },
      {
        "Name": "Width",
        "DisplayName": "Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "width",
        "EnumDisplayNames": null,
        "StableId": "width"
      },
      {
        "Name": "Height",
        "DisplayName": "Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "height",
        "EnumDisplayNames": null,
        "StableId": "height"
      },
      {
        "Name": "FileSizeBytes",
        "DisplayName": "File Size Bytes",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "file_size_bytes",
        "EnumDisplayNames": null,
        "StableId": "file_size_bytes"
      },
      {
        "Name": "SavedAtUtc",
        "DisplayName": "Saved At Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "saved_at_utc",
        "EnumDisplayNames": null,
        "StableId": "saved_at_utc"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "FilePath",
        "DisplayName": "File Path",
        "Property": {
          "Name": "FilePath",
          "DisplayName": "File Path",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "file_path",
          "EnumDisplayNames": null,
          "StableId": "file_path"
        },
        "Children": []
      },
      {
        "Segment": "FileName",
        "DisplayName": "File Name",
        "Property": {
          "Name": "FileName",
          "DisplayName": "File Name",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "file_name",
          "EnumDisplayNames": null,
          "StableId": "file_name"
        },
        "Children": []
      },
      {
        "Segment": "Format",
        "DisplayName": "Format",
        "Property": {
          "Name": "Format",
          "DisplayName": "Format",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "format",
          "EnumDisplayNames": null,
          "StableId": "format"
        },
        "Children": []
      },
      {
        "Segment": "Width",
        "DisplayName": "Width",
        "Property": {
          "Name": "Width",
          "DisplayName": "Width",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "width",
          "EnumDisplayNames": null,
          "StableId": "width"
        },
        "Children": []
      },
      {
        "Segment": "Height",
        "DisplayName": "Height",
        "Property": {
          "Name": "Height",
          "DisplayName": "Height",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "height",
          "EnumDisplayNames": null,
          "StableId": "height"
        },
        "Children": []
      },
      {
        "Segment": "FileSizeBytes",
        "DisplayName": "File Size Bytes",
        "Property": {
          "Name": "FileSizeBytes",
          "DisplayName": "File Size Bytes",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "file_size_bytes",
          "EnumDisplayNames": null,
          "StableId": "file_size_bytes"
        },
        "Children": []
      },
      {
        "Segment": "SavedAtUtc",
        "DisplayName": "Saved At Utc",
        "Property": {
          "Name": "SavedAtUtc",
          "DisplayName": "Saved At Utc",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "saved_at_utc",
          "EnumDisplayNames": null,
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

