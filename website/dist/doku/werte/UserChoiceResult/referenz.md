## Werte und Ergebnisse: UserChoiceResult

ID: UserChoiceResult
Website: /doku/werte/UserChoiceResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag UserChoiceResult. Wird von Benutzerauswahl abfragen geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.selected_index

Position in der Antwortliste: 0 ist die erste Antwort. -1 bedeutet, dass keine Antwort gewählt wurde. Beispiel: 0.

### result.selected_label

Sichtbarer Name der gewählten Antwort, geeignet für Rückmeldungen und Textanzeigen. Bei Abbruch leer. Beispiel: ABC 123.

### result.selected_option_id

Stabile ID der gewählten Antwort, geeignet für Bedingungen unabhängig vom angezeigten Namen. Bei Abbruch leer.

### result.selected_value

Konfigurierter Textwert der gewählten Antwort für nachfolgende Schritte oder Vergleiche. Bei Abbruch leer.

### result.was_cancelled

Gibt an, ob das Auswahlfenster ohne Antwort geschlossen wurde. Bedeutet allein nicht, dass der gesamte Job beendet wurde. Beispiel: true.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "UserChoiceResult",
  "name": "UserChoiceResult",
  "result": {
    "TypeName": "UserChoiceResult",
    "DisplayName": "UserChoiceResult",
    "Properties": [
      {
        "Name": "SelectedOptionId",
        "DisplayName": "Ausgewählte Antwort",
        "DataType": "Text",
        "Description": "Stabile ID der gewählten Antwort, geeignet für Bedingungen unabhängig vom angezeigten Namen. Bei Abbruch leer.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_option_id",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "selected_option_id"
      },
      {
        "Name": "SelectedLabel",
        "DisplayName": "Angezeigter Antwortname",
        "DataType": "Text",
        "Description": "Sichtbarer Name der gewählten Antwort, geeignet für Rückmeldungen und Textanzeigen. Bei Abbruch leer.",
        "IsNullable": true,
        "Example": "ABC 123",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_label",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "selected_label"
      },
      {
        "Name": "SelectedValue",
        "DisplayName": "Rückgabewert der Antwort",
        "DataType": "Text",
        "Description": "Konfigurierter Textwert der gewählten Antwort für nachfolgende Schritte oder Vergleiche. Bei Abbruch leer.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_value",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "selected_value"
      },
      {
        "Name": "SelectedIndex",
        "DisplayName": "Position der Antwort",
        "DataType": "Integer",
        "Description": "Position in der Antwortliste: 0 ist die erste Antwort. -1 bedeutet, dass keine Antwort gewählt wurde.",
        "IsNullable": false,
        "Example": "0",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "selected_index",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "selected_index"
      },
      {
        "Name": "WasCancelled",
        "DisplayName": "Auswahl abgebrochen",
        "DataType": "Boolean",
        "Description": "Gibt an, ob das Auswahlfenster ohne Antwort geschlossen wurde. Bedeutet allein nicht, dass der gesamte Job beendet wurde.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "was_cancelled",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "was_cancelled"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "SelectedOptionId",
        "DisplayName": "Ausgewählte Antwort",
        "Property": {
          "Name": "SelectedOptionId",
          "DisplayName": "Ausgewählte Antwort",
          "DataType": "Text",
          "Description": "Stabile ID der gewählten Antwort, geeignet für Bedingungen unabhängig vom angezeigten Namen. Bei Abbruch leer.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_option_id",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "selected_option_id"
        },
        "Children": []
      },
      {
        "Segment": "SelectedLabel",
        "DisplayName": "Angezeigter Antwortname",
        "Property": {
          "Name": "SelectedLabel",
          "DisplayName": "Angezeigter Antwortname",
          "DataType": "Text",
          "Description": "Sichtbarer Name der gewählten Antwort, geeignet für Rückmeldungen und Textanzeigen. Bei Abbruch leer.",
          "IsNullable": true,
          "Example": "ABC 123",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_label",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "selected_label"
        },
        "Children": []
      },
      {
        "Segment": "SelectedValue",
        "DisplayName": "Rückgabewert der Antwort",
        "Property": {
          "Name": "SelectedValue",
          "DisplayName": "Rückgabewert der Antwort",
          "DataType": "Text",
          "Description": "Konfigurierter Textwert der gewählten Antwort für nachfolgende Schritte oder Vergleiche. Bei Abbruch leer.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_value",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "selected_value"
        },
        "Children": []
      },
      {
        "Segment": "SelectedIndex",
        "DisplayName": "Position der Antwort",
        "Property": {
          "Name": "SelectedIndex",
          "DisplayName": "Position der Antwort",
          "DataType": "Integer",
          "Description": "Position in der Antwortliste: 0 ist die erste Antwort. -1 bedeutet, dass keine Antwort gewählt wurde.",
          "IsNullable": false,
          "Example": "0",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "selected_index",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "selected_index"
        },
        "Children": []
      },
      {
        "Segment": "WasCancelled",
        "DisplayName": "Auswahl abgebrochen",
        "Property": {
          "Name": "WasCancelled",
          "DisplayName": "Auswahl abgebrochen",
          "DataType": "Boolean",
          "Description": "Gibt an, ob das Auswahlfenster ohne Antwort geschlossen wurde. Bedeutet allein nicht, dass der gesamte Job beendet wurde.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "was_cancelled",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "was_cancelled"
        },
        "Children": []
      }
    ]
  }
}
```

