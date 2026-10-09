## Steps: Benutzerauswahl abfragen

ID: user_choice
Website: /doku/steps/user_choice/

Abbildung: [Eine Rückfrage mit zwei beschrifteten Optionen und getrennten Rückgabewerten. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/step-auswahl.detail.png)

1. Überschrift (optional): Sichtbare Überschrift der Auswahl beziehungsweise Gruppentitel. Sie ist keine technische Identität.
2. Titel (optional): Fragetext der Benutzerauswahl. Formuliere ihn so, dass die angebotenen Antworten eine eindeutige Entscheidung ermöglichen.
3. Beschreibung (optional): Zusätzliche Beschreibung für Menschen; sie ist kein auszuführender Befehl.
4. Monitor: Nullbasierter Zielmonitor der Desktop-Anzeige oder Benutzerauswahl. Prüfe ihn auf dem Zielrechner.
5. Antwortmöglichkeiten (2 bis 18, verpflichtend): Lege zwei bis achtzehn Antworten an. Jede Antwort benötigt einen sichtbaren Antworttext. Ein optionaler Rückgabewert ermöglicht stabile Bedingungen; bleibt er leer, wird der Antworttext zurückgegeben. Die interne ID bleibt beim Umbenennen oder Umordnen erhalten.
### errors

Abbrechen der Auswahl ist von Stoppen des ganzen Jobs zu unterscheiden. Prüfe was_cancelled, bevor du selected_value verwendest. Ungültige, doppelte oder zu wenige/zu viele Antworten werden bei der Konfiguration zurückgewiesen.

### example

Zwei Antworten: ID rechnung, Label Rechnungen, Wert rechnung sowie ID notiz, Label Notizen, Wert notiz. Der Wenn-Step verwendet selected_value.

### field.description

Zusätzliche Beschreibung für Menschen; sie ist kein auszuführender Befehl.

### field.desktop_index

Nullbasierter Zielmonitor der Desktop-Anzeige oder Benutzerauswahl. Prüfe ihn auf dem Zielrechner.

### field.options

Lege zwei bis achtzehn Antworten an. Jede Antwort benötigt einen sichtbaren Antworttext. Ein optionaler Rückgabewert ermöglicht stabile Bedingungen; bleibt er leer, wird der Antworttext zurückgegeben. Die interne ID bleibt beim Umbenennen oder Umordnen erhalten.

### field.question

Fragetext der Benutzerauswahl. Formuliere ihn so, dass die angebotenen Antworten eine eindeutige Entscheidung ermöglichen.

### field.title

Sichtbare Überschrift der Auswahl beziehungsweise Gruppentitel. Sie ist keine technische Identität.

### purpose

Lege zwei bis achtzehn Antworten an und frage den Benutzer während des Jobs nach einer Auswahl. Jede Antwort hat eine stabile ID, ein sichtbares Label und einen weitergegebenen Textwert. Die Ergebnisse selected_option_id, selected_label und selected_value erfüllen unterschiedliche Zwecke; ein Abbruch wird separat als was_cancelled gemeldet.

### result.selected_index

Index der gewählten Antwort, nullbasiert. Vor Verwendung was_cancelled prüfen. Einzelwert dieses Ergebnisses.

### result.selected_label

Sichtbarer Text der gewählten Antwort, geeignet für eine verständliche Rückmeldung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.selected_option_id

Stabile ID der gewählten Antwort; sie ist vom sichtbaren Label und vom fachlichen Textwert getrennt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.selected_option_id.option.example-option-0

Beispielhafte Antwort-ID einer Benutzerauswahl; bei eigener Antwortliste entstehen deren eigene stabile IDs. Der zugehörige Wert beschreibt nur diese Option.

### result.selected_option_id.option.example-option-1

Beispielhafte Antwort-ID einer Benutzerauswahl; bei eigener Antwortliste entstehen deren eigene stabile IDs. Der zugehörige Wert beschreibt nur diese Option.

### result.selected_value

Weitergereichter fachlicher Textwert der Antwort, geeignet für nachfolgende Vergleiche. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.was_cancelled

Ob der Benutzer das Auswahlfenster ohne Antwort abgebrochen hat. Dies allein bedeutet nicht, dass der komplette Job gestoppt wurde. Einzelwert dieses Ergebnisses.

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

### schema.settings.description

Zusätzliche Beschreibung für Menschen; sie ist kein auszuführender Befehl.

### schema.settings.desktopIndex

Nullbasierter Monitorindex dieser Konfiguration; die Monitorreihenfolge ist rechnerabhängig.

### schema.settings.options

Antwortliste mit zwei bis achtzehn Einträgen. Jeder Eintrag benötigt stabile ID, sichtbares Label und einen weitergereichten Textwert.

### schema.settings.options.id

Eindeutige, stabile ID dieser Antwort innerhalb der Antwortliste. Über diese ID können nachfolgende Steps die gewählte Option erkennen. Beim Umbenennen der Beschriftung die ID beibehalten.

### schema.settings.options.label

Für Menschen sichtbarer Antworttext. Verwende value für stabile fachliche Vergleiche, wenn das Label später umformuliert werden soll.

### schema.settings.options.value

Optionaler Textwert, der bei Auswahl dieser Antwort weitergereicht wird, zum Beispiel „yes“. Leer verwendet den Antworttext. Mit einem stabilen Rückgabewert funktionieren Bedingungen auch nach einer Änderung der sichtbaren Beschriftung.

### schema.settings.question

Fragetext der Benutzerauswahl. Formuliere ihn so, dass die angebotenen Antworten eine eindeutige Entscheidung ermöglichen.

### schema.settings.title

Sichtbare Überschrift der Auswahl beziehungsweise Gruppentitel. Sie ist keine technische Identität.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "user_choice",
  "name": "Benutzerauswahl abfragen",
  "description": "Öffnet während der Job-Ausführung ein Auswahlfenster und stellt die gewählte Antwort nachfolgenden Steps bereit.",
  "category": "AblaufSteuern",
  "uiFieldIds": [
    "title",
    "question",
    "description",
    "desktop_index",
    "options"
  ],
  "fields": [
    {
      "id": "title",
      "name": "Überschrift (optional)",
      "descriptor": {
        "Id": "title",
        "LabelKey": "Ui.UserChoice.Title",
        "ValueKind": "Text",
        "Required": false,
        "DefaultValue": "",
        "DescriptionKey": null,
        "EditorHint": "single-line-text",
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
    },
    {
      "id": "question",
      "name": "Titel (optional)",
      "descriptor": {
        "Id": "question",
        "LabelKey": "Ui.UserChoice.Question",
        "ValueKind": "MultilineText",
        "Required": false,
        "DefaultValue": "",
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
        "MonitorDeviceNameFieldId": null
      }
    },
    {
      "id": "description",
      "name": "Beschreibung (optional)",
      "descriptor": {
        "Id": "description",
        "LabelKey": "Ui.UserChoice.Description",
        "ValueKind": "MultilineText",
        "Required": false,
        "DefaultValue": "",
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
      "id": "desktop_index",
      "name": "Monitor",
      "descriptor": {
        "Id": "desktop_index",
        "LabelKey": "Ui.Step.Settings.DesktopIndex",
        "ValueKind": "Integer",
        "Required": false,
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
      "id": "options",
      "name": "Antwortmöglichkeiten (2 bis 18, verpflichtend)",
      "descriptor": {
        "Id": "options",
        "LabelKey": "Ui.UserChoice.Answers",
        "ValueKind": "Collection",
        "Required": true,
        "DefaultValue": null,
        "DescriptionKey": null,
        "EditorHint": "user-choice-options",
        "Constraints": {
          "Minimum": null,
          "Maximum": null,
          "MinimumLength": 2,
          "MaximumLength": 18,
          "AllowedValues": null
        },
        "Width": "Full",
        "Advanced": false,
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
      "type": "UserChoiceSettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.description",
      "name": "description",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.desktopIndex",
      "name": "desktopIndex",
      "type": "Int32",
      "options": [],
      "defaultValue": 0
    },
    {
      "id": "settings.options",
      "name": "options",
      "type": "List`1",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.options.id",
      "name": "id",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.options.label",
      "name": "label",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.options.value",
      "name": "value",
      "type": "String",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.question",
      "name": "question",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "settings.title",
      "name": "title",
      "type": "String",
      "options": [],
      "defaultValue": ""
    }
  ],
  "inputs": [],
  "result": {
    "TypeName": "UserChoiceResult",
    "DisplayName": "UserChoiceResult",
    "Properties": [
      {
        "Name": "SelectedOptionId",
        "DisplayName": "Selected Option Id",
        "DataType": "Enum",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": "UserChoiceResult:documentation-user_choice",
        "EnumValues": [
          "example-option-0",
          "example-option-1"
        ],
        "Id": "selected_option_id",
        "EnumDisplayNames": {
          "example-option-0": "",
          "example-option-1": ""
        },
        "StableId": "selected_option_id"
      },
      {
        "Name": "SelectedLabel",
        "DisplayName": "Selected Label",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_label",
        "EnumDisplayNames": null,
        "StableId": "selected_label"
      },
      {
        "Name": "SelectedValue",
        "DisplayName": "Selected Value",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_value",
        "EnumDisplayNames": null,
        "StableId": "selected_value"
      },
      {
        "Name": "SelectedIndex",
        "DisplayName": "Selected Index",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_index",
        "EnumDisplayNames": null,
        "StableId": "selected_index"
      },
      {
        "Name": "WasCancelled",
        "DisplayName": "Was Cancelled",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "was_cancelled",
        "EnumDisplayNames": null,
        "StableId": "was_cancelled"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "SelectedOptionId",
        "DisplayName": "Selected Option Id",
        "Property": {
          "Name": "SelectedOptionId",
          "DisplayName": "Selected Option Id",
          "DataType": "Enum",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": "UserChoiceResult:documentation-user_choice",
          "EnumValues": [
            "example-option-0",
            "example-option-1"
          ],
          "Id": "selected_option_id",
          "EnumDisplayNames": {
            "example-option-0": "",
            "example-option-1": ""
          },
          "StableId": "selected_option_id"
        },
        "Children": []
      },
      {
        "Segment": "SelectedLabel",
        "DisplayName": "Selected Label",
        "Property": {
          "Name": "SelectedLabel",
          "DisplayName": "Selected Label",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_label",
          "EnumDisplayNames": null,
          "StableId": "selected_label"
        },
        "Children": []
      },
      {
        "Segment": "SelectedValue",
        "DisplayName": "Selected Value",
        "Property": {
          "Name": "SelectedValue",
          "DisplayName": "Selected Value",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_value",
          "EnumDisplayNames": null,
          "StableId": "selected_value"
        },
        "Children": []
      },
      {
        "Segment": "SelectedIndex",
        "DisplayName": "Selected Index",
        "Property": {
          "Name": "SelectedIndex",
          "DisplayName": "Selected Index",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_index",
          "EnumDisplayNames": null,
          "StableId": "selected_index"
        },
        "Children": []
      },
      {
        "Segment": "WasCancelled",
        "DisplayName": "Was Cancelled",
        "Property": {
          "Name": "WasCancelled",
          "DisplayName": "Was Cancelled",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "was_cancelled",
          "EnumDisplayNames": null,
          "StableId": "was_cancelled"
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
  "type": "user_choice",
  "settings": {
    "title": "",
    "question": "",
    "description": "",
    "desktopIndex": 0,
    "options": [
      {
        "id": "c4de30c6-f016-2700-00a7-c656a7abcf11",
        "label": "",
        "value": ""
      },
      {
        "id": "5c562fe4-9360-401a-e540-39a24df5f2aa",
        "label": "",
        "value": ""
      }
    ]
  },
  "id": "50a7ffc1-3f4c-06a1-4d31-b5461fe898cb",
  "inputs": {},
  "is_enabled": true
}
```

