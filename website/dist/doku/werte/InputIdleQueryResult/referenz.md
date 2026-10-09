## Werte und Ergebnisse: InputIdleQueryResult

ID: InputIdleQueryResult
Website: /doku/werte/InputIdleQueryResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag InputIdleQueryResult. Wird von der zugehörigen Windows-Abfrage geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.captured_at

Zeitpunkt der Windows-Abfrage beziehungsweise Erfassung im Ergebnis. Er zeigt den Beobachtungsstand, nicht einen garantierten späteren Systemzustand. Einzelwert dieses Ergebnisses.

### result.error_code

Maschinenlesbarer Fehlercode einer Windows-Operation; bei Erfolg entsprechend dem Vertrag leer beziehungsweise nicht gesetzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.error_message

Lesbare Diagnose der Windows-Operation. Für Entscheidungen vorzugsweise Status und stabilen Code verwenden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.percentage

Aktueller Akkuladestand als Prozentwert, soweit vom System verfügbar. Einzelwert dieses Ergebnisses.

### result.status

Vom zuständigen Windows-Dienst gemeldeter Zustand. Nicht durch frei erfundene Statusnamen ersetzen. Einzelwert dieses Ergebnisses.

### result.status.option.AccessDenied

Windows verweigert den Zugriff; kein negativer fachlicher Zustand, sondern fehlende Berechtigung.

### result.status.option.Failed

Lauf wegen Fehler fehlgeschlagen.

### result.status.option.Success

Windows-Aufruf erfolgreich. Erst anschließend den fachlichen Ergebniswert auswerten.

### result.status.option.Timeout

Windows-Aufruf überschritt seine Zeitgrenze; daraus keinen Systemzustand ableiten.

### result.status.option.Unsupported

Funktion auf diesem Zielsystem nicht unterstützt.

### result.value

Der von dieser Windows-Abfrage gelieferte fachliche Wert; die Typ- und Kardinalitätsspalten beschreiben seine Darstellung. Einzelwert dieses Ergebnisses.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "InputIdleQueryResult",
  "name": "InputIdleQueryResult",
  "result": {
    "TypeName": "InputIdleQueryResult",
    "DisplayName": "InputIdleQueryResult",
    "Properties": [
      {
        "Name": "IdleMilliseconds",
        "DisplayName": "Idle Milliseconds",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "value",
        "EnumDisplayNames": null,
        "StableId": "value"
      },
      {
        "Name": "IdleSeconds",
        "DisplayName": "Idle Seconds",
        "DataType": "Number",
        "Description": "Number",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "percentage",
        "EnumDisplayNames": null,
        "StableId": "percentage"
      },
      {
        "Name": "Status",
        "DisplayName": "Status",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsCapabilityStatus",
        "EnumValues": [
          "Success",
          "Unsupported",
          "AccessDenied",
          "Timeout",
          "Failed"
        ],
        "Id": "status",
        "EnumDisplayNames": null,
        "StableId": "status"
      },
      {
        "Name": "CapturedAt",
        "DisplayName": "Captured At",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "captured_at",
        "EnumDisplayNames": null,
        "StableId": "captured_at"
      },
      {
        "Name": "ErrorCode",
        "DisplayName": "Error Code",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "error_code",
        "EnumDisplayNames": null,
        "StableId": "error_code"
      },
      {
        "Name": "ErrorMessage",
        "DisplayName": "Error Message",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": "No detection point available",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "error_message",
        "EnumDisplayNames": null,
        "StableId": "error_message"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "IdleMilliseconds",
        "DisplayName": "Idle Milliseconds",
        "Property": {
          "Name": "IdleMilliseconds",
          "DisplayName": "Idle Milliseconds",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "value",
          "EnumDisplayNames": null,
          "StableId": "value"
        },
        "Children": []
      },
      {
        "Segment": "IdleSeconds",
        "DisplayName": "Idle Seconds",
        "Property": {
          "Name": "IdleSeconds",
          "DisplayName": "Idle Seconds",
          "DataType": "Number",
          "Description": "Number",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "percentage",
          "EnumDisplayNames": null,
          "StableId": "percentage"
        },
        "Children": []
      },
      {
        "Segment": "Status",
        "DisplayName": "Status",
        "Property": {
          "Name": "Status",
          "DisplayName": "Status",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsCapabilityStatus",
          "EnumValues": [
            "Success",
            "Unsupported",
            "AccessDenied",
            "Timeout",
            "Failed"
          ],
          "Id": "status",
          "EnumDisplayNames": null,
          "StableId": "status"
        },
        "Children": []
      },
      {
        "Segment": "CapturedAt",
        "DisplayName": "Captured At",
        "Property": {
          "Name": "CapturedAt",
          "DisplayName": "Captured At",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "captured_at",
          "EnumDisplayNames": null,
          "StableId": "captured_at"
        },
        "Children": []
      },
      {
        "Segment": "ErrorCode",
        "DisplayName": "Error Code",
        "Property": {
          "Name": "ErrorCode",
          "DisplayName": "Error Code",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "error_code",
          "EnumDisplayNames": null,
          "StableId": "error_code"
        },
        "Children": []
      },
      {
        "Segment": "ErrorMessage",
        "DisplayName": "Error Message",
        "Property": {
          "Name": "ErrorMessage",
          "DisplayName": "Error Message",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": "No detection point available",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "error_message",
          "EnumDisplayNames": null,
          "StableId": "error_message"
        },
        "Children": []
      }
    ]
  }
}
```

