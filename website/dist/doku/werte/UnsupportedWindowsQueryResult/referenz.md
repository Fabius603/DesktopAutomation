## Werte und Ergebnisse: UnsupportedWindowsQueryResult

ID: UnsupportedWindowsQueryResult
Website: /doku/werte/UnsupportedWindowsQueryResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag UnsupportedWindowsQueryResult. Wird von der zugehörigen Windows-Abfrage geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.captured_at

UTC-Zeitpunkt, zu dem der Windows-Zustand ermittelt wurde. Beispiel: 2030-01-01T08:00:00Z.

### result.error_code

Code zur Fehlerdiagnose. Bei erfolgreicher Windows-Aktion leer. In der App unter Diagnose aufgeführt.

### result.error_message

Beschreibung des Windows-Fehlers. Bei erfolgreicher Aktion leer. In der App unter Diagnose aufgeführt.

### result.status

Zeigt Erfolg, fehlende Unterstützung, verweigerten Zugriff oder einen Fehler der Windows-Aktion.

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

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "UnsupportedWindowsQueryResult",
  "name": "UnsupportedWindowsQueryResult",
  "result": {
    "TypeName": "UnsupportedWindowsQueryResult",
    "DisplayName": "UnsupportedWindowsQueryResult",
    "Properties": [
      {
        "Name": "Status",
        "DisplayName": "Ergebnis der Windows-Aktion",
        "DataType": "Enum",
        "Description": "Zeigt Erfolg, fehlende Unterstützung, verweigerten Zugriff oder einen Fehler der Windows-Aktion.",
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
        "IsDiagnostic": false,
        "StableId": "status"
      },
      {
        "Name": "CapturedAt",
        "DisplayName": "Abfragezeit (UTC)",
        "DataType": "DateTime",
        "Description": "UTC-Zeitpunkt, zu dem der Windows-Zustand ermittelt wurde.",
        "IsNullable": false,
        "Example": "2030-01-01T08:00:00Z",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "captured_at",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "captured_at"
      },
      {
        "Name": "ErrorCode",
        "DisplayName": "Technischer Fehlercode",
        "DataType": "Text",
        "Description": "Code zur Fehlerdiagnose. Bei erfolgreicher Windows-Aktion leer.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "error_code",
        "EnumDisplayNames": null,
        "IsDiagnostic": true,
        "StableId": "error_code"
      },
      {
        "Name": "ErrorMessage",
        "DisplayName": "Fehlerbeschreibung",
        "DataType": "Text",
        "Description": "Beschreibung des Windows-Fehlers. Bei erfolgreicher Aktion leer.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "error_message",
        "EnumDisplayNames": null,
        "IsDiagnostic": true,
        "StableId": "error_message"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Status",
        "DisplayName": "Ergebnis der Windows-Aktion",
        "Property": {
          "Name": "Status",
          "DisplayName": "Ergebnis der Windows-Aktion",
          "DataType": "Enum",
          "Description": "Zeigt Erfolg, fehlende Unterstützung, verweigerten Zugriff oder einen Fehler der Windows-Aktion.",
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
          "IsDiagnostic": false,
          "StableId": "status"
        },
        "Children": []
      },
      {
        "Segment": "CapturedAt",
        "DisplayName": "Abfragezeit (UTC)",
        "Property": {
          "Name": "CapturedAt",
          "DisplayName": "Abfragezeit (UTC)",
          "DataType": "DateTime",
          "Description": "UTC-Zeitpunkt, zu dem der Windows-Zustand ermittelt wurde.",
          "IsNullable": false,
          "Example": "2030-01-01T08:00:00Z",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "captured_at",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "captured_at"
        },
        "Children": []
      },
      {
        "Segment": "ErrorCode",
        "DisplayName": "Technischer Fehlercode",
        "Property": {
          "Name": "ErrorCode",
          "DisplayName": "Technischer Fehlercode",
          "DataType": "Text",
          "Description": "Code zur Fehlerdiagnose. Bei erfolgreicher Windows-Aktion leer.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "error_code",
          "EnumDisplayNames": null,
          "IsDiagnostic": true,
          "StableId": "error_code"
        },
        "Children": []
      },
      {
        "Segment": "ErrorMessage",
        "DisplayName": "Fehlerbeschreibung",
        "Property": {
          "Name": "ErrorMessage",
          "DisplayName": "Fehlerbeschreibung",
          "DataType": "Text",
          "Description": "Beschreibung des Windows-Fehlers. Bei erfolgreicher Aktion leer.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "error_message",
          "EnumDisplayNames": null,
          "IsDiagnostic": true,
          "StableId": "error_message"
        },
        "Children": []
      }
    ]
  }
}
```

