## Steps: Datei oder Ordner bearbeiten

ID: file_system_operation
Website: /doku/steps/file_system_operation/

Abbildung: [Kopieraktion mit Quell- und Zielpfad und automatischem Anlegen fehlender übergeordneter Ordner. Weitere Optionen stehen unter Erweitert. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/step-dateien.detail.png)

1. Aktion: Dateiaktion Copy, Move, Rename oder Delete. Quelle und benötigte Zusatzfelder ändern sich mit der Aktion.
2. Quelle: Datei- oder Ordnerpfad, auf den die Dateiaktion angewendet wird. Der Pfad ist auf dem Zielrechner zu prüfen, nicht nur auf dem Rechner eines Agenten.
3. Ziel: Ziel der Kopier- oder Verschiebeaktion. Bei anderen Aktionen nicht als Ersatz für new_name verwenden.
4. Fehlende übergeordnete Ordner automatisch erstellen: Legt fehlende übergeordnete Zielordner bei den unterstützten Kopier-/Verschiebeaktionen an. Dies gewährt keine zusätzlichen Zugriffsrechte.
5. Bei gesperrten Dateien wiederholen: Wiederholt eine wegen Dateisperre fehlgeschlagene Dateiaktion nach konfigurierter Wartezeit. Andere Konfigurationsfehler werden dadurch nicht behoben.
6. Anzahl der Wiederholungen: Anzahl zusätzlicher Wiederholungsversuche nach einer Dateisperre; 0 deaktiviert zusätzliche Versuche.
7. Wartezeit zwischen Versuchen (ms): Pause zwischen Wiederholungsversuchen in Millisekunden. Ein höherer Wert gibt dem anderen Programm mehr Zeit zur Freigabe.
Abbildung: [Vollständige Feld-Ausschnitte der echten Oberfläche mit Beispielwerten zur Aktion Umbenennen. Die Nummer markiert Beschriftung und Eingabe gemeinsam; keine Dateiaktion wird ausgeführt.](/ref-step-file-system-operation-rename.detail.png)

1. Aktion: Dateiaktion Copy, Move, Rename oder Delete. Quelle und benötigte Zusatzfelder ändern sich mit der Aktion.
2. Quelle: Datei- oder Ordnerpfad, auf den die Dateiaktion angewendet wird. Der Pfad ist auf dem Zielrechner zu prüfen, nicht nur auf dem Rechner eines Agenten.
3. Neuer Name: Neuer Dateiname für Rename. Hier wird ein Name angegeben; die Quelle bestimmt den umzubenennenden Eintrag.
4. Bei gesperrten Dateien wiederholen: Wiederholt eine wegen Dateisperre fehlgeschlagene Dateiaktion nach konfigurierter Wartezeit. Andere Konfigurationsfehler werden dadurch nicht behoben.
5. Anzahl der Wiederholungen: Anzahl zusätzlicher Wiederholungsversuche nach einer Dateisperre; 0 deaktiviert zusätzliche Versuche.
6. Wartezeit zwischen Versuchen (ms): Pause zwischen Wiederholungsversuchen in Millisekunden. Ein höherer Wert gibt dem anderen Programm mehr Zeit zur Freigabe.
Abbildung: [Vollständige Feld-Ausschnitte der echten Oberfläche mit Beispielwerten zur Aktion Löschen. Die Nummer markiert Beschriftung und Eingabe gemeinsam; keine Dateiaktion wird ausgeführt.](/ref-step-file-system-operation-delete.detail.png)

1. Aktion: Dateiaktion Copy, Move, Rename oder Delete. Quelle und benötigte Zusatzfelder ändern sich mit der Aktion.
2. Quelle: Datei- oder Ordnerpfad, auf den die Dateiaktion angewendet wird. Der Pfad ist auf dem Zielrechner zu prüfen, nicht nur auf dem Rechner eines Agenten.
3. Optionaler Filter: Nur bei Aktion „Löschen“ sichtbar: Dateimuster wie *.txt für passende Dateien innerhalb eines Ordners. Mehrere Muster mit Semikolon trennen; keine Ordnertrennzeichen eintragen. Leer beschränkt die Auswahl nicht durch ein Muster.
4. Bei gesperrten Dateien wiederholen: Wiederholt eine wegen Dateisperre fehlgeschlagene Dateiaktion nach konfigurierter Wartezeit. Andere Konfigurationsfehler werden dadurch nicht behoben.
5. Anzahl der Wiederholungen: Anzahl zusätzlicher Wiederholungsversuche nach einer Dateisperre; 0 deaktiviert zusätzliche Versuche.
6. Wartezeit zwischen Versuchen (ms): Pause zwischen Wiederholungsversuchen in Millisekunden. Ein höherer Wert gibt dem anderen Programm mehr Zeit zur Freigabe.
### errors

Prüfe bei Fehlern Quellpfad, Aktion, Ziel, Zugriffsrechte und Dateisperren. Wiederholungen gelten für konfigurierte gesperrte Dateiaktionen; sie machen eine falsche Quelle nicht gültig. Ein teilweise bearbeiteter Ordner wird bei einem späteren Fehler nicht automatisch zurückgerollt.

### example

Der Beispielablauf „Arbeitsunterlagen vorbereiten“ kopiert je nach Auswahl Rechnungen oder Notizen in getrennte Arbeitsordner.

### field.create_parent_directories

Legt fehlende übergeordnete Zielordner bei den unterstützten Kopier-/Verschiebeaktionen an. Dies gewährt keine zusätzlichen Zugriffsrechte.

### field.filter

Nur bei Aktion „Löschen“ sichtbar: Dateimuster wie *.txt für passende Dateien innerhalb eines Ordners. Mehrere Muster mit Semikolon trennen; keine Ordnertrennzeichen eintragen. Leer beschränkt die Auswahl nicht durch ein Muster.

### field.new_name

Neuer Dateiname für Rename. Hier wird ein Name angegeben; die Quelle bestimmt den umzubenennenden Eintrag.

### field.operation

Dateiaktion Copy, Move, Rename oder Delete. Quelle und benötigte Zusatzfelder ändern sich mit der Aktion.

### field.operation.option.Copy

Kopiert die Quelle zum Ziel; die Quelle bleibt erhalten.

### field.operation.option.Delete

Entfernt passende Quelleinträge; zuerst mit entbehrlichen Testdaten prüfen.

### field.operation.option.Move

Verschiebt die Quelle zum Ziel; bei Erfolg liegt sie dort statt am ursprünglichen Ort.

### field.operation.option.Rename

Benennt den Quelleintrag mit new_name um.

### field.retry_count

Anzahl zusätzlicher Wiederholungsversuche nach einer Dateisperre; 0 deaktiviert zusätzliche Versuche.

### field.retry_delay_ms

Pause zwischen Wiederholungsversuchen in Millisekunden. Ein höherer Wert gibt dem anderen Programm mehr Zeit zur Freigabe.

### field.retry_locked_files

Wiederholt eine wegen Dateisperre fehlgeschlagene Dateiaktion nach konfigurierter Wartezeit. Andere Konfigurationsfehler werden dadurch nicht behoben.

### field.source_path

Datei- oder Ordnerpfad, auf den die Dateiaktion angewendet wird. Der Pfad ist auf dem Zielrechner zu prüfen, nicht nur auf dem Rechner eines Agenten.

### field.target_path

Ziel der Kopier- oder Verschiebeaktion. Bei anderen Aktionen nicht als Ersatz für new_name verwenden.

### input.source

Quell-Dateipfad. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### input.target

Ziel-Dateipfad. Eine gültige, aber noch nicht verfügbare optionale Step-Quelle kann den Verbraucher mit NoInput überspringen; malformed Referenzen bleiben Fehler. Es wird kein beliebiges Listenelement ausgewählt. Akzeptierte Typen und Anbieter stehen in den Vertragsspalten; unbegrenzte Anbieterwahl bedeutet weiterhin Typprüfung.

### purpose

Wähle Kopieren, Verschieben, Umbenennen oder Löschen. Trage Quelle und die für diese Aktion benötigten Zielwerte ein. Nach dem Lauf zeigen affected_count und affected_paths, welche Einträge betroffen waren. Kopieren kann vorhandene Zieldateien ersetzen; teste den Ablauf zunächst mit den bereitgestellten Beispieldateien.

### result.affected_bytes

Umfang der betroffenen Dateidaten in Bytes. Einzelwert dieses Ergebnisses.

### result.affected_count

Gesamtzahl betroffener Einträge der Dateiaktion. Einzelwert dieses Ergebnisses.

### result.affected_directory_count

Anzahl betroffener Ordner, getrennt von Dateien. Einzelwert dieses Ergebnisses.

### result.affected_file_count

Anzahl betroffener Dateien, getrennt von Ordnern. Einzelwert dieses Ergebnisses.

### result.affected_paths

Liste der von der Dateiaktion betroffenen Pfade. Ein erfolgreicher Zähler ist nicht automatisch ein Backup-Versionsnachweis. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.affected_paths.count

Anzahl der Elemente der referenzierten Sammlung, automatisch als zusätzlicher Ergebniswert verfügbar. Einzelwert dieses Ergebnisses.

### result.completed_at_utc

UTC-Abschlusszeitpunkt der Aktion. Einzelwert dieses Ergebnisses.

### result.item_type

Ob Datei, Ordner oder mehrere Einträge von der Dateiaktion betroffen waren. Einzelwert dieses Ergebnisses.

### result.item_type.option.Directory

Betroffener Eintrag ist ein Ordner.

### result.item_type.option.File

Betroffener Eintrag ist eine Datei.

### result.item_type.option.Multiple

Mehrere Einträge wurden bearbeitet.

### result.operation

Tatsächlich angeforderte Dateiaktion. Zusammen mit den Zählern und Pfaden lesen, um deren Auswirkungen zu verstehen. Einzelwert dieses Ergebnisses.

### result.operation.option.Copy

Kopiert die Quelle zum Ziel; die Quelle bleibt erhalten.

### result.operation.option.Delete

Entfernt passende Quelleinträge; zuerst mit entbehrlichen Testdaten prüfen.

### result.operation.option.Move

Verschiebt die Quelle zum Ziel; bei Erfolg liegt sie dort statt am ursprünglichen Ort.

### result.operation.option.Rename

Benennt den Quelleintrag mit new_name um.

### result.source_path

Verwendeter Quellpfad der Dateiaktion. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.target_path

Verwendeter Zielpfad der Dateiaktion, soweit die gewählte Aktion ein Ziel besitzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

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

### schema.settings.create_parent_directories

Legt fehlende übergeordnete Zielordner bei den unterstützten Kopier-/Verschiebeaktionen an. Dies gewährt keine zusätzlichen Zugriffsrechte.

### schema.settings.filter

Nur bei Aktion „Löschen“ sichtbar: Dateimuster wie *.txt für passende Dateien innerhalb eines Ordners. Mehrere Muster mit Semikolon trennen; keine Ordnertrennzeichen eintragen. Leer beschränkt die Auswahl nicht durch ein Muster.

### schema.settings.new_name

Neuer Dateiname für Rename. Hier wird ein Name angegeben; die Quelle bestimmt den umzubenennenden Eintrag.

### schema.settings.operation

Dateiaktion Copy, Move, Rename oder Delete. Quelle und benötigte Zusatzfelder ändern sich mit der Aktion.

### schema.settings.operation.option.Copy

Kopiert die Quelle zum Ziel; die Quelle bleibt erhalten.

### schema.settings.operation.option.Delete

Entfernt passende Quelleinträge; zuerst mit entbehrlichen Testdaten prüfen.

### schema.settings.operation.option.Move

Verschiebt die Quelle zum Ziel; bei Erfolg liegt sie dort statt am ursprünglichen Ort.

### schema.settings.operation.option.Rename

Benennt den Quelleintrag mit new_name um.

### schema.settings.retry_count

Anzahl zusätzlicher Wiederholungsversuche nach einer Dateisperre; 0 deaktiviert zusätzliche Versuche.

### schema.settings.retry_delay_ms

Pause zwischen Wiederholungsversuchen in Millisekunden. Ein höherer Wert gibt dem anderen Programm mehr Zeit zur Freigabe.

### schema.settings.retry_locked_files

Wiederholt eine wegen Dateisperre fehlgeschlagene Dateiaktion nach konfigurierter Wartezeit. Andere Konfigurationsfehler werden dadurch nicht behoben.

### schema.settings.source_mode

Bei älterer Pfadkonfiguration: ExplicitPath liest source_path, TaskResult liest source_result.

### schema.settings.source_mode.option.ExplicitPath

Verwendet den direkt angegebenen Pfad.

### schema.settings.source_mode.option.TaskResult

Verwendet den verbundenen Step-Ergebniswert statt des direkten Literals.

### schema.settings.source_path

Datei- oder Ordnerpfad, auf den die Dateiaktion angewendet wird. Der Pfad ist auf dem Zielrechner zu prüfen, nicht nur auf dem Rechner eines Agenten.

### schema.settings.source_result

Typisierte Referenz, deren Text/Dateipfad als Quelle der Dateiaktion verwendet wird.

### schema.settings.source_result.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.source_result.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.source_result.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.source_result.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.source_result.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.source_result.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.source_result.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.source_result.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.source_result.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### schema.settings.target_mode

Bei älterer Pfadkonfiguration: ExplicitPath liest target_path, TaskResult liest target_result.

### schema.settings.target_mode.option.ExplicitPath

Verwendet den direkt angegebenen Pfad.

### schema.settings.target_mode.option.TaskResult

Verwendet den verbundenen Step-Ergebniswert statt des direkten Literals.

### schema.settings.target_path

Ziel der Kopier- oder Verschiebeaktion. Bei anderen Aktionen nicht als Ersatz für new_name verwenden.

### schema.settings.target_result

Typisierte Referenz, deren Text/Dateipfad als Ziel der Dateiaktion verwendet wird.

### schema.settings.target_result.items

Geordnete Unterbindungen einer Liste. Die Reihenfolge ist Teil der Eingabe; Elemente müssen das Listenelement-Schema erfüllen.

### schema.settings.target_result.members

Benannte Unterbindungen eines strukturierten Werts. Jeder Schlüssel ist eine stabile Mitglied-ID des Feldschemas; Unterbindungen können Werte einer Basisquelle gezielt überschreiben.

### schema.settings.target_result.property_id

Stabile Ergebnis-ID einer älteren Step-Referenz. Neue Dateien kodieren sie in source_id; nicht mit einem CLR-Propertynamen oder dem UI-Label verwechseln.

### schema.settings.target_result.property_path

Älterer Pfad zu einem Ergebnis-Unterwert. Bestehende Dateien bleiben lesbar; neue Referenzen verwenden stabile Ergebnis-IDs und gegebenenfalls value_path.

### schema.settings.target_result.provider_id

Anbieter der Wertequelle: local_value, job_variable, step_result oder secret. Ein Quellwert benötigt zusätzlich source_id und muss zum Eingabetyp passen.

### schema.settings.target_result.schema_id

Versionierte Form eines zusammengesetzten Eingabewerts. Sie gehört zum Bindungsbaum, nicht als frei erfundener Schlüssel in die Variable. Übernimm die für dieses Feld exportierte Schema-ID.

### schema.settings.target_result.source_id

Anbieterbezogene Identität. Bei step_result: v1/<URI-kodierte-Step-ID>/<URI-kodierte-stabile-Ergebnis-ID>; bei lokalen Werten und Jobvariablen deren GUID. Dies ist nicht nur die rohe Step-ID.

### schema.settings.target_result.source_step_id

Kompatibilitätsfeld älterer Referenzen. Neue Referenzen verwenden provider_id=step_result und die versionierte source_id.

### schema.settings.target_result.value_path

Optionaler Pfad unterhalb einer Anbieterquelle, zum Beispiel zu einem typisierten Objektmitglied. Er ändert den Quellanbieter nicht; der ausgewählte Unterwert muss kompatibel sein.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "file_system_operation",
  "name": "Datei oder Ordner bearbeiten",
  "description": "Kopiert, verschiebt, benennt Dateien und Ordner um oder löscht sie optional anhand eines Filters.",
  "category": "DateienOrdner",
  "uiFieldIds": [
    "operation",
    "source_path",
    "target_path",
    "new_name",
    "filter",
    "create_parent_directories",
    "retry_locked_files",
    "retry_count",
    "retry_delay_ms"
  ],
  "fields": [
    {
      "id": "operation",
      "name": "Aktion",
      "descriptor": {
        "Id": "operation",
        "LabelKey": "Ui.Step.FileSystem.Operation",
        "ValueKind": "Enum",
        "Required": true,
        "DefaultValue": "Copy",
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": null,
          "MaximumLength": null,
          "AllowedValues": [
            "Copy",
            "Move",
            "Rename",
            "Delete"
          ]
        },
        "Width": "Full",
        "Advanced": false,
        "Order": 0,
        "VisibleWhen": null,
        "Options": [
          {
            "Value": "Copy",
            "LabelKey": "Ui.Step.FileSystem.Copy",
            "DisplayName": null
          },
          {
            "Value": "Move",
            "LabelKey": "Ui.Step.FileSystem.Move",
            "DisplayName": null
          },
          {
            "Value": "Rename",
            "LabelKey": "Ui.Step.FileSystem.Rename",
            "DisplayName": null
          },
          {
            "Value": "Delete",
            "LabelKey": "Ui.Step.FileSystem.Delete",
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
      "id": "source_path",
      "name": "Quelle",
      "descriptor": {
        "Id": "source_path",
        "LabelKey": "Ui.Step.FileSystem.Source",
        "ValueKind": "DirectoryPath",
        "Required": true,
        "DefaultValue": null,
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
      "id": "target_path",
      "name": "Ziel",
      "descriptor": {
        "Id": "target_path",
        "LabelKey": "Ui.Step.FileSystem.Target",
        "ValueKind": "DirectoryPath",
        "Required": true,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "directory-picker",
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 2,
        "VisibleWhen": {
          "FieldId": "operation",
          "EqualsValue": null,
          "AnyOfValues": [
            "Copy",
            "Move"
          ]
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
      "id": "new_name",
      "name": "Neuer Name",
      "descriptor": {
        "Id": "new_name",
        "LabelKey": "Ui.Step.FileSystem.NewName",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 7,
        "VisibleWhen": {
          "FieldId": "operation",
          "EqualsValue": "Rename",
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
      "id": "filter",
      "name": "Optionaler Filter",
      "descriptor": {
        "Id": "filter",
        "LabelKey": "Ui.Step.FileSystem.Filter",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": null,
        "DescriptionKey": "Ui.Step.FileSystem.FilterHint",
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 8,
        "VisibleWhen": {
          "FieldId": "operation",
          "EqualsValue": "Delete",
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
      "id": "create_parent_directories",
      "name": "Fehlende übergeordnete Ordner automatisch erstellen",
      "descriptor": {
        "Id": "create_parent_directories",
        "LabelKey": "Ui.Step.FileSystem.CreateParents",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": true,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": false,
        "Order": 9,
        "VisibleWhen": {
          "FieldId": "operation",
          "EqualsValue": null,
          "AnyOfValues": [
            "Copy",
            "Move"
          ]
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
      "id": "retry_locked_files",
      "name": "Bei gesperrten Dateien wiederholen",
      "descriptor": {
        "Id": "retry_locked_files",
        "LabelKey": "Ui.Step.FileSystem.RetryLocked",
        "ValueKind": "Boolean",
        "Required": false,
        "DefaultValue": true,
        "DescriptionKey": null,
        "EditorHint": null,
        "Constraints": null,
        "Width": "Full",
        "Advanced": true,
        "Order": 10,
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
      "id": "retry_count",
      "name": "Anzahl der Wiederholungen",
      "descriptor": {
        "Id": "retry_count",
        "LabelKey": "Ui.Step.FileSystem.RetryCount",
        "ValueKind": "Integer",
        "Required": false,
        "DefaultValue": 3,
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
        "Order": 11,
        "VisibleWhen": {
          "FieldId": "retry_locked_files",
          "EqualsValue": true,
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
      "id": "retry_delay_ms",
      "name": "Wartezeit zwischen Versuchen (ms)",
      "descriptor": {
        "Id": "retry_delay_ms",
        "LabelKey": "Ui.Step.FileSystem.RetryDelay",
        "ValueKind": "Duration",
        "Required": false,
        "DefaultValue": 100,
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
        "Order": 12,
        "VisibleWhen": {
          "FieldId": "retry_locked_files",
          "EqualsValue": true,
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
      "type": "FileSystemOperationSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.create_parent_directories",
      "name": "create_parent_directories",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    },
    {
      "id": "settings.filter",
      "name": "filter",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.new_name",
      "name": "new_name",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.operation",
      "name": "operation",
      "type": "FileSystemOperation",
      "options": [
        "Copy",
        "Move",
        "Rename",
        "Delete"
      ],
      "defaultValue": "Copy"
    },
    {
      "id": "settings.retry_count",
      "name": "retry_count",
      "type": "Int32",
      "options": [],
      "defaultValue": 3
    },
    {
      "id": "settings.retry_delay_ms",
      "name": "retry_delay_ms",
      "type": "Int32",
      "options": [],
      "defaultValue": 100
    },
    {
      "id": "settings.retry_locked_files",
      "name": "retry_locked_files",
      "type": "Boolean",
      "options": [],
      "defaultValue": true
    },
    {
      "id": "settings.source_mode",
      "name": "source_mode",
      "type": "FileSystemPathSource",
      "options": [
        "ExplicitPath",
        "TaskResult"
      ],
      "defaultValue": "ExplicitPath"
    },
    {
      "id": "settings.source_path",
      "name": "source_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.source_result",
      "name": "source_result",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.source_result.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.source_result.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.source_result.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_mode",
      "name": "target_mode",
      "type": "FileSystemPathSource",
      "options": [
        "ExplicitPath",
        "TaskResult"
      ],
      "defaultValue": "ExplicitPath"
    },
    {
      "id": "settings.target_path",
      "name": "target_path",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target_result",
      "name": "target_result",
      "type": "ResultBinding",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.items",
      "name": "items",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.property_id",
      "name": "property_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.property_path",
      "name": "property_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.source_step_id",
      "name": "source_step_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.members",
      "name": "members",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.provider_id",
      "name": "provider_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target_result.schema_id",
      "name": "schema_id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.target_result.source_id",
      "name": "source_id",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.target_result.value_path",
      "name": "value_path",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ],
  "inputs": [
    {
      "Key": "source",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "NotApplicable",
      "AcceptedShapes": [
        {
          "ValueKind": "Text",
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
        }
      ],
      "AllowedProviderIds": [
        "step_result"
      ],
      "LegacyAllowedProviderIds": [
        "secret"
      ],
      "AllowsDirectValue": false,
      "LegacyAcceptedShapes": []
    },
    {
      "Key": "target",
      "Required": false,
      "MissingValuePolicy": "SkipStep",
      "CollectionConsumption": "NotApplicable",
      "AcceptedShapes": [
        {
          "ValueKind": "Text",
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
        }
      ],
      "AllowedProviderIds": [
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
    "TypeName": "FileSystemOperationResult",
    "DisplayName": "FileSystemOperationResult",
    "Properties": [
      {
        "Name": "Operation",
        "DisplayName": "Operation",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.Jobs.FileSystemOperation",
        "EnumValues": [
          "Copy",
          "Move",
          "Rename",
          "Delete"
        ],
        "Id": "operation",
        "EnumDisplayNames": null,
        "StableId": "operation"
      },
      {
        "Name": "SourcePath",
        "DisplayName": "Source Path",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_path",
        "EnumDisplayNames": null,
        "StableId": "source_path"
      },
      {
        "Name": "TargetPath",
        "DisplayName": "Target Path",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "target_path",
        "EnumDisplayNames": null,
        "StableId": "target_path"
      },
      {
        "Name": "ItemType",
        "DisplayName": "Item Type",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.Steps.FileSystemItemType",
        "EnumValues": [
          "File",
          "Directory",
          "Multiple"
        ],
        "Id": "item_type",
        "EnumDisplayNames": null,
        "StableId": "item_type"
      },
      {
        "Name": "AffectedCount",
        "DisplayName": "Affected Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_count",
        "EnumDisplayNames": null,
        "StableId": "affected_count"
      },
      {
        "Name": "AffectedFileCount",
        "DisplayName": "Affected File Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_file_count",
        "EnumDisplayNames": null,
        "StableId": "affected_file_count"
      },
      {
        "Name": "AffectedDirectoryCount",
        "DisplayName": "Affected Directory Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_directory_count",
        "EnumDisplayNames": null,
        "StableId": "affected_directory_count"
      },
      {
        "Name": "AffectedBytes",
        "DisplayName": "Affected Bytes",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_bytes",
        "EnumDisplayNames": null,
        "StableId": "affected_bytes"
      },
      {
        "Name": "AffectedPaths",
        "DisplayName": "Affected Paths",
        "DataType": "Text",
        "Description": "Text",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_paths",
        "EnumDisplayNames": null,
        "StableId": "affected_paths"
      },
      {
        "Name": "AffectedPaths.Count",
        "DisplayName": "Affected Paths / Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "affected_paths.count",
        "EnumDisplayNames": null,
        "StableId": "affected_paths.count"
      },
      {
        "Name": "CompletedAtUtc",
        "DisplayName": "Completed At Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "completed_at_utc",
        "EnumDisplayNames": null,
        "StableId": "completed_at_utc"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Operation",
        "DisplayName": "Operation",
        "Property": {
          "Name": "Operation",
          "DisplayName": "Operation",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.Jobs.FileSystemOperation",
          "EnumValues": [
            "Copy",
            "Move",
            "Rename",
            "Delete"
          ],
          "Id": "operation",
          "EnumDisplayNames": null,
          "StableId": "operation"
        },
        "Children": []
      },
      {
        "Segment": "SourcePath",
        "DisplayName": "Source Path",
        "Property": {
          "Name": "SourcePath",
          "DisplayName": "Source Path",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_path",
          "EnumDisplayNames": null,
          "StableId": "source_path"
        },
        "Children": []
      },
      {
        "Segment": "TargetPath",
        "DisplayName": "Target Path",
        "Property": {
          "Name": "TargetPath",
          "DisplayName": "Target Path",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "target_path",
          "EnumDisplayNames": null,
          "StableId": "target_path"
        },
        "Children": []
      },
      {
        "Segment": "ItemType",
        "DisplayName": "Item Type",
        "Property": {
          "Name": "ItemType",
          "DisplayName": "Item Type",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.Steps.FileSystemItemType",
          "EnumValues": [
            "File",
            "Directory",
            "Multiple"
          ],
          "Id": "item_type",
          "EnumDisplayNames": null,
          "StableId": "item_type"
        },
        "Children": []
      },
      {
        "Segment": "AffectedCount",
        "DisplayName": "Affected Count",
        "Property": {
          "Name": "AffectedCount",
          "DisplayName": "Affected Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_count",
          "EnumDisplayNames": null,
          "StableId": "affected_count"
        },
        "Children": []
      },
      {
        "Segment": "AffectedFileCount",
        "DisplayName": "Affected File Count",
        "Property": {
          "Name": "AffectedFileCount",
          "DisplayName": "Affected File Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_file_count",
          "EnumDisplayNames": null,
          "StableId": "affected_file_count"
        },
        "Children": []
      },
      {
        "Segment": "AffectedDirectoryCount",
        "DisplayName": "Affected Directory Count",
        "Property": {
          "Name": "AffectedDirectoryCount",
          "DisplayName": "Affected Directory Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_directory_count",
          "EnumDisplayNames": null,
          "StableId": "affected_directory_count"
        },
        "Children": []
      },
      {
        "Segment": "AffectedBytes",
        "DisplayName": "Affected Bytes",
        "Property": {
          "Name": "AffectedBytes",
          "DisplayName": "Affected Bytes",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_bytes",
          "EnumDisplayNames": null,
          "StableId": "affected_bytes"
        },
        "Children": []
      },
      {
        "Segment": "AffectedPaths",
        "DisplayName": "Affected Paths",
        "Property": {
          "Name": "AffectedPaths",
          "DisplayName": "Affected Paths",
          "DataType": "Text",
          "Description": "Text",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "affected_paths",
          "EnumDisplayNames": null,
          "StableId": "affected_paths"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Count",
            "Property": {
              "Name": "AffectedPaths.Count",
              "DisplayName": "Affected Paths / Count",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "affected_paths.count",
              "EnumDisplayNames": null,
              "StableId": "affected_paths.count"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "CompletedAtUtc",
        "DisplayName": "Completed At Utc",
        "Property": {
          "Name": "CompletedAtUtc",
          "DisplayName": "Completed At Utc",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "completed_at_utc",
          "EnumDisplayNames": null,
          "StableId": "completed_at_utc"
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
  "type": "file_system_operation",
  "settings": {
    "operation": "Copy",
    "source_mode": "ExplicitPath",
    "source_path": "",
    "source_result": {
      "provider_id": "",
      "source_id": ""
    },
    "target_mode": "ExplicitPath",
    "target_path": "",
    "target_result": {
      "provider_id": "",
      "source_id": ""
    },
    "new_name": "",
    "filter": "",
    "create_parent_directories": true,
    "retry_locked_files": true,
    "retry_count": 3,
    "retry_delay_ms": 100
  },
  "id": "fd4d948b-8366-d9ec-c236-0eb9d97b2607",
  "inputs": {},
  "is_enabled": true
}
```

