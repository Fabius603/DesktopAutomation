## Steps: Bildschirm aufnehmen

ID: desktop_duplication
Website: /doku/steps/desktop_duplication/

Abbildung: [Detailansicht: Bildschirm aufnehmen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-desktop-duplication.detail.png)

1. Monitor: Nullbasierter Monitorindex der Bildschirmaufnahme. Auf einem anderen Rechner kann die Monitorreihenfolge abweichen.
2. Mauszeiger aufnehmen: Nimmt den Mauszeiger in die Bildschirmaufnahme auf. Für Bildvergleiche kann der Zeiger störende zusätzliche Pixel verursachen.
3. Auf neues Bild warten: Wartet auf einen neuen Desktop-Frame statt sofort den zuletzt verfügbaren Frame zu übernehmen.
4. Zeitlimit (ms): Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.
5. Bei Zeitlimit letztes Bild verwenden: Erlaubt nach Ablauf des Aufnahmezeitlimits den zuletzt verfügbaren Frame. Prüfe dessen is_fresh, bevor eine zeitkritische Folgeaktion ausgeführt wird.
6. Monitor-Kennung: Optional gespeicherte Monitor-Gerätekennung zur Wiedererkennung des gewünschten Bildschirms. Bei Umbau der Monitoranordnung prüfen. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

Monitor 0 aufnehmen → Bild speichern. Bei zeitkritischer Erkennung auf ein neues Bild warten und den Rückfall auf Cache deaktivieren.

### field.allow_cached_fallback

Erlaubt nach Ablauf des Aufnahmezeitlimits den zuletzt verfügbaren Frame. Prüfe dessen is_fresh, bevor eine zeitkritische Folgeaktion ausgeführt wird.

### field.capture_cursor

Nimmt den Mauszeiger in die Bildschirmaufnahme auf. Für Bildvergleiche kann der Zeiger störende zusätzliche Pixel verursachen.

### field.desktop_idx

Nullbasierter Monitorindex der Bildschirmaufnahme. Auf einem anderen Rechner kann die Monitorreihenfolge abweichen.

### field.monitor_device_name

Optional gespeicherte Monitor-Gerätekennung zur Wiedererkennung des gewünschten Bildschirms. Bei Umbau der Monitoranordnung prüfen. Ab dem kommenden Update 1.6.2 akzeptiert dieses Textfeld auch Zahlen, Wahrheitswerte, Listen und strukturierte Werte; die Quelle bleibt typisiert, ihr Inhalt wird für die Eingabe in Text umgewandelt. Bilddaten sind keine Textquelle.

### field.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### field.wait_for_new_frame

Wartet auf einen neuen Desktop-Frame statt sofort den zuletzt verfügbaren Frame zu übernehmen.

### purpose

Wähle einen Monitor als Bildquelle. Optional wartet die Aufnahme auf ein neues Desktop-Bild; beim Zeitlimit kannst du den letzten verfügbaren Frame zulassen. Prüfe is_fresh und frame_timestamp, wenn eine spätere Mausaktion nur auf neue Bilder reagieren soll.

### result.bounds

Bereich der Aufnahme in Pixeln. Bei Bildschirmaufnahmen Desktopkoordinaten, bei Kameraaufnahmen Bildkoordinaten ab (0, 0). Beispiel: {X=10,Y=20,Width=300,Height=200}.

### result.bounds.bottom

Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.center

Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: {X=100,Y=200}.

### result.bounds.center.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.center.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.height

Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.is_empty

Gibt an, ob der Bereich keine positive Breite oder Höhe hat. Beispiel: true.

### result.bounds.left

Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.location

Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: {X=100,Y=200}.

### result.bounds.location.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.location.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.right

Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.top

Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.width

Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.bounds.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.capture_timestamp_utc

UTC-Zeitpunkt der Aufnahme. Bei einem wiederverwendeten Bild bleibt die ursprüngliche Aufnahmezeit erhalten. Beispiel: 2030-01-01T08:00:00Z.

### result.frame_timestamp

Monotoner Bildzeitstempel zur Diagnose der zeitlichen Reihenfolge. 0 bedeutet unbekannt; kein Kalenderdatum. Beispiel: 3. In der App unter Diagnose aufgeführt.

### result.frame_version

Kennung des Bildes innerhalb dieser Aufnahmequelle. Gleiche Kennungen stehen für dasselbe Bild; Kennungen unterschiedlicher Quellen sind nicht vergleichbar. Beispiel: 3. In der App unter Diagnose aufgeführt.

### result.has_image

Gibt an, ob diese Aufnahme ein Bild bereitstellt. Vor der Weiterverwendung des Bildes prüfbar. Beispiel: true.

### result.image

Das aufgenommene Bild zur Weitergabe an Erkennung, Bildanzeige oder Speicherung. Kann fehlen, wenn kein Bild verfügbar ist.

### result.is_fresh

Gibt an, ob ein frisches Bild geliefert wurde. false kennzeichnet eine wiederverwendete Aufnahme. Beispiel: true.

### result.offset

Pixelversatz des Bildursprungs zum Desktop. Für bildlokale Punkte zu X und Y addieren; bei Kameras ist der Versatz (0, 0). Bereits globale Erkennungspunkte nicht erneut verschieben. Beispiel: {X=100,Y=200}.

### result.offset.x

Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

### result.offset.y

Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert. Beispiel: 3.

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

### schema.settings.allow_cached_fallback

Erlaubt nach Ablauf des Aufnahmezeitlimits den zuletzt verfügbaren Frame. Prüfe dessen is_fresh, bevor eine zeitkritische Folgeaktion ausgeführt wird.

### schema.settings.capture_cursor

Nimmt den Mauszeiger in die Bildschirmaufnahme auf. Für Bildvergleiche kann der Zeiger störende zusätzliche Pixel verursachen.

### schema.settings.desktop_idx

Nullbasierter Monitorindex der Bildschirmaufnahme. Auf einem anderen Rechner kann die Monitorreihenfolge abweichen.

### schema.settings.monitor_device_name

Optional gespeicherte Monitor-Gerätekennung zur Wiedererkennung des gewünschten Bildschirms. Bei Umbau der Monitoranordnung prüfen.

### schema.settings.timeout_ms

Zeitlimit in Millisekunden für den betreffenden Vorgang. Das jeweilige Fehler-/Skip-Verhalten steht im Step-Text; es ist keine allgemeine Job-Gesamtdauer.

### schema.settings.wait_for_new_frame

Wartet auf einen neuen Desktop-Frame statt sofort den zuletzt verfügbaren Frame zu übernehmen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "desktop_duplication",
  "name": "Bildschirm aufnehmen",
  "description": "Nimmt einen Screenshot des gewählten Monitors auf und stellt ihn als Bildquelle bereit.",
  "category": "BildAufnehmen",
  "uiFieldIds": [
    "desktop_idx",
    "capture_cursor",
    "wait_for_new_frame",
    "timeout_ms",
    "allow_cached_fallback",
    "monitor_device_name"
  ],
  "fields": [
    {
      "id": "desktop_idx",
      "name": "Monitor",
      "helpKey": "Ui.Step.Settings.DesktopIndex.Help",
      "help": "Monitor, auf den sich die Aufnahme oder Anzeige bezieht.",
      "descriptor": {
        "Id": "desktop_idx",
        "LabelKey": "Ui.Step.Settings.DesktopIndex",
        "ValueKind": "Integer",
        "Required": true,
        "DefaultValue": 0,
        "DescriptionKey": null,
        "EditorHint": "monitor-picker",
        "Constraints": {
          "Minimum": 0,
          "Maximum": null,
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
        "MonitorDeviceNameFieldId": "monitor_device_name",
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "capture_cursor",
      "name": "Mauszeiger aufnehmen",
      "helpKey": "Ui.Step.Settings.CaptureMousePointer.Help",
      "help": "Nimmt den Mauszeiger in das erfasste Bildschirmbild auf.",
      "descriptor": {
        "Id": "capture_cursor",
        "LabelKey": "Ui.Step.Settings.CaptureMousePointer",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": false,
        "DescriptionKey": null,
        "EditorHint": null,
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
        "ScreenPointPickerOptions": null,
        "AllowsDirectValue": null,
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "wait_for_new_frame",
      "name": "Auf neues Bild warten",
      "helpKey": "Ui.Step.Capture.WaitForNewFrame.Help",
      "help": "Wartet auf ein neues Bild statt sofort ein bereits vorhandenes Bild zu verwenden.",
      "descriptor": {
        "Id": "wait_for_new_frame",
        "LabelKey": "Ui.Step.Capture.WaitForNewFrame",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": true,
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
      "id": "timeout_ms",
      "name": "Zeitlimit (ms)",
      "helpKey": "Ui.Step.Capture.TimeoutHelp",
      "help": "Begrenzt Wartezeiten und Wiederholungen. Die Initialisierung des Monitors und die Bildverarbeitung können zusätzliche Zeit benötigen.",
      "descriptor": {
        "Id": "timeout_ms",
        "LabelKey": "Ui.Step.Capture.Timeout",
        "ValueKind": "Integer",
        "Required": true,
        "DefaultValue": 250,
        "DescriptionKey": "Ui.Step.Capture.TimeoutHelp",
        "EditorHint": null,
        "Constraints": {
          "Minimum": 1,
          "Maximum": 60000,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": false,
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
    },
    {
      "id": "allow_cached_fallback",
      "name": "Bei Zeitlimit letztes Bild verwenden",
      "helpKey": "Ui.Step.Capture.AllowCachedFallback.Help",
      "help": "Verwendet bei Ablauf des Zeitlimits das zuletzt verfügbare Bild. Dieses kann älter als der aktuelle Durchlauf sein.",
      "descriptor": {
        "Id": "allow_cached_fallback",
        "LabelKey": "Ui.Step.Capture.AllowCachedFallback",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": true,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
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
        "MonitorDeviceNameFieldId": null,
        "InputSchemaId": null,
        "OwnsStructuredInputContract": false,
        "InputValueKind": null
      }
    },
    {
      "id": "monitor_device_name",
      "name": "Monitor-Kennung",
      "helpKey": "Ui.Step.Capture.MonitorIdentityHelp",
      "help": "Die Monitorauswahl speichert diese Kennung. Leer verwendet den Monitorindex; ein getrennter Monitor führt zu einem Fehler.",
      "descriptor": {
        "Id": "monitor_device_name",
        "LabelKey": "Ui.Step.Capture.MonitorIdentity",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": "Ui.Step.Capture.MonitorIdentityHelp",
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
      "type": "DesktopDuplicationSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.allow_cached_fallback",
      "name": "allow_cached_fallback",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    },
    {
      "id": "settings.capture_cursor",
      "name": "capture_cursor",
      "type": "Boolean",
      "options": [],
      "defaultValue": false
    },
    {
      "id": "settings.desktop_idx",
      "name": "desktop_idx",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.monitor_device_name",
      "name": "monitor_device_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.timeout_ms",
      "name": "timeout_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 250
    },
    {
      "id": "settings.wait_for_new_frame",
      "name": "wait_for_new_frame",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "DesktopDuplicationResult",
    "DisplayName": "DesktopDuplicationResult",
    "Properties": [
      {
        "Name": "Image",
        "DisplayName": "Aufgenommenes Bild",
        "DataType": "Image",
        "Description": "Das aufgenommene Bild zur Weitergabe an Erkennung, Bildanzeige oder Speicherung. Kann fehlen, wenn kein Bild verfügbar ist.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "image",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "image"
      },
      {
        "Name": "Bounds",
        "DisplayName": "Aufgenommener Bereich",
        "DataType": "Rectangle",
        "Description": "Bereich der Aufnahme in Pixeln. Bei Bildschirmaufnahmen Desktopkoordinaten, bei Kameraaufnahmen Bildkoordinaten ab (0, 0).",
        "IsNullable": false,
        "Example": "{X=10,Y=20,Width=300,Height=200}",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds"
      },
      {
        "Name": "Bounds.X",
        "DisplayName": "Aufgenommener Bereich / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.x"
      },
      {
        "Name": "Bounds.Y",
        "DisplayName": "Aufgenommener Bereich / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.y"
      },
      {
        "Name": "Bounds.Width",
        "DisplayName": "Aufgenommener Bereich / Breite (px)",
        "DataType": "Integer",
        "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.width",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.width"
      },
      {
        "Name": "Bounds.Height",
        "DisplayName": "Aufgenommener Bereich / Höhe (px)",
        "DataType": "Integer",
        "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.height",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.height"
      },
      {
        "Name": "Bounds.Left",
        "DisplayName": "Aufgenommener Bereich / Linke Kante (px)",
        "DataType": "Integer",
        "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.left",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.left"
      },
      {
        "Name": "Bounds.Top",
        "DisplayName": "Aufgenommener Bereich / Obere Kante (px)",
        "DataType": "Integer",
        "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.top",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.top"
      },
      {
        "Name": "Bounds.Right",
        "DisplayName": "Aufgenommener Bereich / Rechte Kante (px)",
        "DataType": "Integer",
        "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.right",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.right"
      },
      {
        "Name": "Bounds.Bottom",
        "DisplayName": "Aufgenommener Bereich / Untere Kante (px)",
        "DataType": "Integer",
        "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.bottom",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.bottom"
      },
      {
        "Name": "Bounds.IsEmpty",
        "DisplayName": "Aufgenommener Bereich / Bereich ist leer",
        "DataType": "Boolean",
        "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.is_empty",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.is_empty"
      },
      {
        "Name": "Bounds.Location",
        "DisplayName": "Aufgenommener Bereich / Linke obere Ecke",
        "DataType": "Point",
        "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.location",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.location"
      },
      {
        "Name": "Bounds.Location.X",
        "DisplayName": "Aufgenommener Bereich / Linke obere Ecke / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.location.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.location.x"
      },
      {
        "Name": "Bounds.Location.Y",
        "DisplayName": "Aufgenommener Bereich / Linke obere Ecke / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.location.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.location.y"
      },
      {
        "Name": "Bounds.Center",
        "DisplayName": "Aufgenommener Bereich / Mittelpunkt",
        "DataType": "Point",
        "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.center",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.center"
      },
      {
        "Name": "Bounds.Center.X",
        "DisplayName": "Aufgenommener Bereich / Mittelpunkt / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.center.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.center.x"
      },
      {
        "Name": "Bounds.Center.Y",
        "DisplayName": "Aufgenommener Bereich / Mittelpunkt / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounds.center.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "bounds.center.y"
      },
      {
        "Name": "Offset",
        "DisplayName": "Versatz zum Desktop",
        "DataType": "Point",
        "Description": "Pixelversatz des Bildursprungs zum Desktop. Für bildlokale Punkte zu X und Y addieren; bei Kameras ist der Versatz (0, 0). Bereits globale Erkennungspunkte nicht erneut verschieben.",
        "IsNullable": false,
        "Example": "{X=100,Y=200}",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "offset",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "offset"
      },
      {
        "Name": "Offset.X",
        "DisplayName": "Versatz zum Desktop / X-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "offset.x",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "offset.x"
      },
      {
        "Name": "Offset.Y",
        "DisplayName": "Versatz zum Desktop / Y-Koordinate (px)",
        "DataType": "Integer",
        "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "offset.y",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "offset.y"
      },
      {
        "Name": "IsFresh",
        "DisplayName": "Aufnahme ist frisch",
        "DataType": "Boolean",
        "Description": "Gibt an, ob ein frisches Bild geliefert wurde. false kennzeichnet eine wiederverwendete Aufnahme.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_fresh",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "is_fresh"
      },
      {
        "Name": "CaptureTimestampUtc",
        "DisplayName": "Aufnahmezeit (UTC)",
        "DataType": "DateTime",
        "Description": "UTC-Zeitpunkt der Aufnahme. Bei einem wiederverwendeten Bild bleibt die ursprüngliche Aufnahmezeit erhalten.",
        "IsNullable": false,
        "Example": "2030-01-01T08:00:00Z",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "capture_timestamp_utc",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "capture_timestamp_utc"
      },
      {
        "Name": "HasImage",
        "DisplayName": "Bild vorhanden",
        "DataType": "Boolean",
        "Description": "Gibt an, ob diese Aufnahme ein Bild bereitstellt. Vor der Weiterverwendung des Bildes prüfbar.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "has_image",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "has_image"
      },
      {
        "Name": "FrameVersion",
        "DisplayName": "Bildversion",
        "DataType": "Integer",
        "Description": "Kennung des Bildes innerhalb dieser Aufnahmequelle. Gleiche Kennungen stehen für dasselbe Bild; Kennungen unterschiedlicher Quellen sind nicht vergleichbar.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "frame_version",
        "EnumDisplayNames": null,
        "IsDiagnostic": true,
        "StableId": "frame_version"
      },
      {
        "Name": "FrameTimestamp",
        "DisplayName": "Bildzeitstempel (Ticks)",
        "DataType": "Integer",
        "Description": "Monotoner Bildzeitstempel zur Diagnose der zeitlichen Reihenfolge. 0 bedeutet unbekannt; kein Kalenderdatum.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "frame_timestamp",
        "EnumDisplayNames": null,
        "IsDiagnostic": true,
        "StableId": "frame_timestamp"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Image",
        "DisplayName": "Aufgenommenes Bild",
        "Property": {
          "Name": "Image",
          "DisplayName": "Aufgenommenes Bild",
          "DataType": "Image",
          "Description": "Das aufgenommene Bild zur Weitergabe an Erkennung, Bildanzeige oder Speicherung. Kann fehlen, wenn kein Bild verfügbar ist.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "image",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "image"
        },
        "Children": []
      },
      {
        "Segment": "Bounds",
        "DisplayName": "Aufgenommener Bereich",
        "Property": {
          "Name": "Bounds",
          "DisplayName": "Aufgenommener Bereich",
          "DataType": "Rectangle",
          "Description": "Bereich der Aufnahme in Pixeln. Bei Bildschirmaufnahmen Desktopkoordinaten, bei Kameraaufnahmen Bildkoordinaten ab (0, 0).",
          "IsNullable": false,
          "Example": "{X=10,Y=20,Width=300,Height=200}",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "bounds",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "bounds"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X-Koordinate (px)",
            "Property": {
              "Name": "Bounds.X",
              "DisplayName": "Aufgenommener Bereich / X-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.x",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y-Koordinate (px)",
            "Property": {
              "Name": "Bounds.Y",
              "DisplayName": "Aufgenommener Bereich / Y-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.y",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Breite (px)",
            "Property": {
              "Name": "Bounds.Width",
              "DisplayName": "Aufgenommener Bereich / Breite (px)",
              "DataType": "Integer",
              "Description": "Breite des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.width",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Höhe (px)",
            "Property": {
              "Name": "Bounds.Height",
              "DisplayName": "Aufgenommener Bereich / Höhe (px)",
              "DataType": "Integer",
              "Description": "Höhe des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.height",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Linke Kante (px)",
            "Property": {
              "Name": "Bounds.Left",
              "DisplayName": "Aufgenommener Bereich / Linke Kante (px)",
              "DataType": "Integer",
              "Description": "Linke Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.left",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Obere Kante (px)",
            "Property": {
              "Name": "Bounds.Top",
              "DisplayName": "Aufgenommener Bereich / Obere Kante (px)",
              "DataType": "Integer",
              "Description": "Obere Kante des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.top",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Rechte Kante (px)",
            "Property": {
              "Name": "Bounds.Right",
              "DisplayName": "Aufgenommener Bereich / Rechte Kante (px)",
              "DataType": "Integer",
              "Description": "Rechte Kante, berechnet als X + Breite in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.right",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Untere Kante (px)",
            "Property": {
              "Name": "Bounds.Bottom",
              "DisplayName": "Aufgenommener Bereich / Untere Kante (px)",
              "DataType": "Integer",
              "Description": "Untere Kante, berechnet als Y + Höhe in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.bottom",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Bereich ist leer",
            "Property": {
              "Name": "Bounds.IsEmpty",
              "DisplayName": "Aufgenommener Bereich / Bereich ist leer",
              "DataType": "Boolean",
              "Description": "Gibt an, ob der Bereich keine positive Breite oder Höhe hat.",
              "IsNullable": false,
              "Example": "true",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.is_empty",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Linke obere Ecke",
            "Property": {
              "Name": "Bounds.Location",
              "DisplayName": "Aufgenommener Bereich / Linke obere Ecke",
              "DataType": "Point",
              "Description": "Position der linken oberen Ecke in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "{X=100,Y=200}",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.location",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "Bounds.Location.X",
                  "DisplayName": "Aufgenommener Bereich / Linke obere Ecke / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.location.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounds.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "Bounds.Location.Y",
                  "DisplayName": "Aufgenommener Bereich / Linke obere Ecke / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.location.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounds.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Mittelpunkt",
            "Property": {
              "Name": "Bounds.Center",
              "DisplayName": "Aufgenommener Bereich / Mittelpunkt",
              "DataType": "Point",
              "Description": "Mittelpunkt des Bereichs in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "{X=100,Y=200}",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounds.center",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "bounds.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X-Koordinate (px)",
                "Property": {
                  "Name": "Bounds.Center.X",
                  "DisplayName": "Aufgenommener Bereich / Mittelpunkt / X-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.center.x",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounds.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y-Koordinate (px)",
                "Property": {
                  "Name": "Bounds.Center.Y",
                  "DisplayName": "Aufgenommener Bereich / Mittelpunkt / Y-Koordinate (px)",
                  "DataType": "Integer",
                  "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
                  "IsNullable": false,
                  "Example": "3",
                  "Cardinality": "Single",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounds.center.y",
                  "EnumDisplayNames": null,
                  "IsDiagnostic": false,
                  "StableId": "bounds.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "Offset",
        "DisplayName": "Versatz zum Desktop",
        "Property": {
          "Name": "Offset",
          "DisplayName": "Versatz zum Desktop",
          "DataType": "Point",
          "Description": "Pixelversatz des Bildursprungs zum Desktop. Für bildlokale Punkte zu X und Y addieren; bei Kameras ist der Versatz (0, 0). Bereits globale Erkennungspunkte nicht erneut verschieben.",
          "IsNullable": false,
          "Example": "{X=100,Y=200}",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "offset",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "offset"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X-Koordinate (px)",
            "Property": {
              "Name": "Offset.X",
              "DisplayName": "Versatz zum Desktop / X-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Horizontale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "offset.x",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "offset.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y-Koordinate (px)",
            "Property": {
              "Name": "Offset.Y",
              "DisplayName": "Versatz zum Desktop / Y-Koordinate (px)",
              "DataType": "Integer",
              "Description": "Vertikale Koordinate in Pixeln; verwendet denselben Koordinatenraum wie der übergeordnete Wert.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "offset.y",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "offset.y"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "IsFresh",
        "DisplayName": "Aufnahme ist frisch",
        "Property": {
          "Name": "IsFresh",
          "DisplayName": "Aufnahme ist frisch",
          "DataType": "Boolean",
          "Description": "Gibt an, ob ein frisches Bild geliefert wurde. false kennzeichnet eine wiederverwendete Aufnahme.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_fresh",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "is_fresh"
        },
        "Children": []
      },
      {
        "Segment": "CaptureTimestampUtc",
        "DisplayName": "Aufnahmezeit (UTC)",
        "Property": {
          "Name": "CaptureTimestampUtc",
          "DisplayName": "Aufnahmezeit (UTC)",
          "DataType": "DateTime",
          "Description": "UTC-Zeitpunkt der Aufnahme. Bei einem wiederverwendeten Bild bleibt die ursprüngliche Aufnahmezeit erhalten.",
          "IsNullable": false,
          "Example": "2030-01-01T08:00:00Z",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "capture_timestamp_utc",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "capture_timestamp_utc"
        },
        "Children": []
      },
      {
        "Segment": "HasImage",
        "DisplayName": "Bild vorhanden",
        "Property": {
          "Name": "HasImage",
          "DisplayName": "Bild vorhanden",
          "DataType": "Boolean",
          "Description": "Gibt an, ob diese Aufnahme ein Bild bereitstellt. Vor der Weiterverwendung des Bildes prüfbar.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "has_image",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "has_image"
        },
        "Children": []
      },
      {
        "Segment": "FrameVersion",
        "DisplayName": "Bildversion",
        "Property": {
          "Name": "FrameVersion",
          "DisplayName": "Bildversion",
          "DataType": "Integer",
          "Description": "Kennung des Bildes innerhalb dieser Aufnahmequelle. Gleiche Kennungen stehen für dasselbe Bild; Kennungen unterschiedlicher Quellen sind nicht vergleichbar.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "frame_version",
          "EnumDisplayNames": null,
          "IsDiagnostic": true,
          "StableId": "frame_version"
        },
        "Children": []
      },
      {
        "Segment": "FrameTimestamp",
        "DisplayName": "Bildzeitstempel (Ticks)",
        "Property": {
          "Name": "FrameTimestamp",
          "DisplayName": "Bildzeitstempel (Ticks)",
          "DataType": "Integer",
          "Description": "Monotoner Bildzeitstempel zur Diagnose der zeitlichen Reihenfolge. 0 bedeutet unbekannt; kein Kalenderdatum.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "frame_timestamp",
          "EnumDisplayNames": null,
          "IsDiagnostic": true,
          "StableId": "frame_timestamp"
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
  "type": "desktop_duplication",
  "settings": {
    "desktop_idx": 0,
    "capture_cursor": false,
    "monitor_device_name": "",
    "wait_for_new_frame": true,
    "timeout_ms": 250,
    "allow_cached_fallback": true
  },
  "id": "ffa47583-1458-1a09-c1e7-2592222fbe7b",
  "inputs": {},
  "is_enabled": true
}
```

