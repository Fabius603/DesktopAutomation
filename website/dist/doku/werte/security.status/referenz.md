## Werte und Ergebnisse: Windows-Sicherheitsstatus

ID: security.status
Website: /doku/werte/security.status/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle security.status im Step „Windows-Zustand abfragen“ und verbinde einen unten beschriebenen Ergebniswert mit einer Bedingung.

### purpose

Zustandsabfrage: Windows-Sicherheitsstatus. Liest den aktuellen vom System angebotenen Zustand und stellt die hier aufgeführten typisierten Werte bereit. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Administratorrechte sind erforderlich.

### result.captured_at

UTC-Zeitpunkt, zu dem der Windows-Zustand ermittelt wurde. Beispiel: 2030-01-01T08:00:00Z.

### result.error_code

Code zur Fehlerdiagnose. Bei erfolgreicher Windows-Aktion leer. In der App unter Diagnose aufgeführt.

### result.error_message

Beschreibung des Windows-Fehlers. Bei erfolgreicher Aktion leer. In der App unter Diagnose aufgeführt.

### result.exists

Gibt an, ob die Windows-Konfiguration für den Firewall-Dienst mpssvc vorhanden ist. Beispiel: true.

### result.is_enabled

Gibt an, ob die Startkonfiguration des Firewall-Dienstes nicht deaktiviert ist. Belegt nicht, dass der Dienst läuft oder alle Firewall-Profile aktiv sind. Beispiel: true.

### result.on_off_state

Ein-/Aus-Zustand aus der Startkonfiguration des Firewall-Dienstes. Kein vollständiger Sicherheitsstatus des Systems.

### result.on_off_state.option.Off

Der abgefragte Schalter ist ausgeschaltet.

### result.on_off_state.option.On

Der abgefragte Schalter ist eingeschaltet.

### result.on_off_state.option.Unknown

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
  "id": "security.status",
  "name": "Windows-Sicherheitsstatus",
  "fields": [],
  "result": {
    "TypeName": "SecurityStatusQueryResult",
    "DisplayName": "SecurityStatusQueryResult",
    "Properties": [
      {
        "Name": "Exists",
        "DisplayName": "Firewall-Dienst vorhanden",
        "DataType": "Boolean",
        "Description": "Gibt an, ob die Windows-Konfiguration für den Firewall-Dienst mpssvc vorhanden ist.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "exists",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "exists"
      },
      {
        "Name": "IsEnabled",
        "DisplayName": "Firewall-Dienst nicht deaktiviert",
        "DataType": "Boolean",
        "Description": "Gibt an, ob die Startkonfiguration des Firewall-Dienstes nicht deaktiviert ist. Belegt nicht, dass der Dienst läuft oder alle Firewall-Profile aktiv sind.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_enabled",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "is_enabled"
      },
      {
        "Name": "OnOffState",
        "DisplayName": "Startkonfiguration der Firewall",
        "DataType": "Enum",
        "Description": "Ein-/Aus-Zustand aus der Startkonfiguration des Firewall-Dienstes. Kein vollständiger Sicherheitsstatus des Systems.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsOnOffState",
        "EnumValues": [
          "Unknown",
          "Off",
          "On"
        ],
        "Id": "on_off_state",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "on_off_state"
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
        "Segment": "Exists",
        "DisplayName": "Firewall-Dienst vorhanden",
        "Property": {
          "Name": "Exists",
          "DisplayName": "Firewall-Dienst vorhanden",
          "DataType": "Boolean",
          "Description": "Gibt an, ob die Windows-Konfiguration für den Firewall-Dienst mpssvc vorhanden ist.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "exists",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "exists"
        },
        "Children": []
      },
      {
        "Segment": "IsEnabled",
        "DisplayName": "Firewall-Dienst nicht deaktiviert",
        "Property": {
          "Name": "IsEnabled",
          "DisplayName": "Firewall-Dienst nicht deaktiviert",
          "DataType": "Boolean",
          "Description": "Gibt an, ob die Startkonfiguration des Firewall-Dienstes nicht deaktiviert ist. Belegt nicht, dass der Dienst läuft oder alle Firewall-Profile aktiv sind.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_enabled",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "is_enabled"
        },
        "Children": []
      },
      {
        "Segment": "OnOffState",
        "DisplayName": "Startkonfiguration der Firewall",
        "Property": {
          "Name": "OnOffState",
          "DisplayName": "Startkonfiguration der Firewall",
          "DataType": "Enum",
          "Description": "Ein-/Aus-Zustand aus der Startkonfiguration des Firewall-Dienstes. Kein vollständiger Sicherheitsstatus des Systems.",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsOnOffState",
          "EnumValues": [
            "Unknown",
            "Off",
            "On"
          ],
          "Id": "on_off_state",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "on_off_state"
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

