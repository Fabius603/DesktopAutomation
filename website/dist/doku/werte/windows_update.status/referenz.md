## Werte und Ergebnisse: Windows-Update-Status

ID: windows_update.status
Website: /doku/werte/windows_update.status/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle windows_update.status im Step „Windows-Zustand abfragen“ und verbinde einen unten beschriebenen Ergebniswert mit einer Bedingung.

### purpose

Zustandsabfrage: Windows-Update-Status. Liest den aktuellen vom System angebotenen Zustand und stellt die hier aufgeführten typisierten Werte bereit. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### result.captured_at

Zeitpunkt der Windows-Abfrage beziehungsweise Erfassung im Ergebnis. Er zeigt den Beobachtungsstand, nicht einen garantierten späteren Systemzustand. Einzelwert dieses Ergebnisses.

### result.error_code

Maschinenlesbarer Fehlercode einer Windows-Operation; bei Erfolg entsprechend dem Vertrag leer beziehungsweise nicht gesetzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.error_message

Lesbare Diagnose der Windows-Operation. Für Entscheidungen vorzugsweise Status und stabilen Code verwenden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.pending_restart

Ob Windows Update noch einen Neustart verlangt. Einzelwert dieses Ergebnisses.

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

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "windows_update.status",
  "name": "Windows-Update-Status",
  "fields": [],
  "result": {
    "TypeName": "WindowsUpdateStatusQueryResult",
    "DisplayName": "WindowsUpdateStatusQueryResult",
    "Properties": [
      {
        "Name": "PendingRestart",
        "DisplayName": "Pending Restart",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "pending_restart",
        "EnumDisplayNames": null,
        "StableId": "pending_restart"
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
        "Segment": "PendingRestart",
        "DisplayName": "Pending Restart",
        "Property": {
          "Name": "PendingRestart",
          "DisplayName": "Pending Restart",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "pending_restart",
          "EnumDisplayNames": null,
          "StableId": "pending_restart"
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

