## Werte und Ergebnisse: SessionStateQueryResult

ID: SessionStateQueryResult
Website: /doku/werte/SessionStateQueryResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag SessionStateQueryResult. Wird von der zugehörigen Windows-Abfrage geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.captured_at

UTC-Zeitpunkt, zu dem der Windows-Zustand ermittelt wurde. Beispiel: 2030-01-01T08:00:00Z.

### result.error_code

Code zur Fehlerdiagnose. Bei erfolgreicher Windows-Aktion leer. In der App unter Diagnose aufgeführt.

### result.error_message

Beschreibung des Windows-Fehlers. Bei erfolgreicher Aktion leer. In der App unter Diagnose aufgeführt.

### result.id

ID der Sitzung, in der die Anwendung läuft, als Text.

### result.is_active

Gibt an, ob die abgefragte Windows-Sitzung aktiv ist. Beispiel: true.

### result.name

Benutzername der Sitzung, in der die Anwendung läuft.

### result.session_state

Zustand der abgefragten Windows-Sitzung, beispielsweise aktiv oder getrennt.

### result.session_state.option.Active

Die Windows-Sitzung ist aktiv.

### result.session_state.option.Disconnected

Verbindung getrennt beziehungsweise Sitzung nicht verbunden; Bedeutung ergibt sich aus dem jeweiligen Ergebnisfeld.

### result.session_state.option.Locked

Die Windows-Sitzung ist gesperrt.

### result.session_state.option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

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
  "id": "SessionStateQueryResult",
  "name": "SessionStateQueryResult",
  "result": {
    "TypeName": "SessionStateQueryResult",
    "DisplayName": "SessionStateQueryResult",
    "Properties": [
      {
        "Name": "IsActive",
        "DisplayName": "Sitzung ist aktiv",
        "DataType": "Boolean",
        "Description": "Gibt an, ob die abgefragte Windows-Sitzung aktiv ist.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_active",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "is_active"
      },
      {
        "Name": "Name",
        "DisplayName": "Name des angemeldeten Benutzers",
        "DataType": "Text",
        "Description": "Benutzername der Sitzung, in der die Anwendung läuft.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "name",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "name"
      },
      {
        "Name": "Id",
        "DisplayName": "Windows-Sitzungs-ID",
        "DataType": "Text",
        "Description": "ID der Sitzung, in der die Anwendung läuft, als Text.",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "id",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "id"
      },
      {
        "Name": "SessionState",
        "DisplayName": "Sitzungszustand",
        "DataType": "Enum",
        "Description": "Zustand der abgefragten Windows-Sitzung, beispielsweise aktiv oder getrennt.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsSessionState",
        "EnumValues": [
          "Unknown",
          "Active",
          "Locked",
          "Disconnected"
        ],
        "Id": "session_state",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "session_state"
      },
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
        "Segment": "IsActive",
        "DisplayName": "Sitzung ist aktiv",
        "Property": {
          "Name": "IsActive",
          "DisplayName": "Sitzung ist aktiv",
          "DataType": "Boolean",
          "Description": "Gibt an, ob die abgefragte Windows-Sitzung aktiv ist.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_active",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "is_active"
        },
        "Children": []
      },
      {
        "Segment": "Name",
        "DisplayName": "Name des angemeldeten Benutzers",
        "Property": {
          "Name": "Name",
          "DisplayName": "Name des angemeldeten Benutzers",
          "DataType": "Text",
          "Description": "Benutzername der Sitzung, in der die Anwendung läuft.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "name",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "name"
        },
        "Children": []
      },
      {
        "Segment": "Id",
        "DisplayName": "Windows-Sitzungs-ID",
        "Property": {
          "Name": "Id",
          "DisplayName": "Windows-Sitzungs-ID",
          "DataType": "Text",
          "Description": "ID der Sitzung, in der die Anwendung läuft, als Text.",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "id",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "id"
        },
        "Children": []
      },
      {
        "Segment": "SessionState",
        "DisplayName": "Sitzungszustand",
        "Property": {
          "Name": "SessionState",
          "DisplayName": "Sitzungszustand",
          "DataType": "Enum",
          "Description": "Zustand der abgefragten Windows-Sitzung, beispielsweise aktiv oder getrennt.",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsSessionState",
          "EnumValues": [
            "Unknown",
            "Active",
            "Locked",
            "Disconnected"
          ],
          "Id": "session_state",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "session_state"
        },
        "Children": []
      },
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

