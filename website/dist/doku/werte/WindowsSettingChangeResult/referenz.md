## Werte und Ergebnisse: WindowsSettingChangeResult

ID: WindowsSettingChangeResult
Website: /doku/werte/WindowsSettingChangeResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag WindowsSettingChangeResult. Wird von Windows-Einstellung ändern geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.windows_setting.applied_value

Vom Windows-Provider gemeldeter angewendeter Wert nach der Änderung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.error_code

Maschinenlesbarer Fehlercode einer Windows-Operation; bei Erfolg entsprechend dem Vertrag leer beziehungsweise nicht gesetzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.previous_value

Vor der Windows-Änderung beobachteter Wert. Für eine Wiederherstellung nur mit derselben Funktion und kompatibler Darstellung verwenden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.restart_required

Ob die abgefragte Funktion beziehungsweise Windows Update einen Neustartbedarf meldet. Einzelwert dieses Ergebnisses.

### result.windows_setting.setting_id

Kennung der geänderten Windows-Funktion. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.windows_setting.status

Vom zuständigen Windows-Dienst gemeldeter Zustand. Nicht durch frei erfundene Statusnamen ersetzen. Einzelwert dieses Ergebnisses.

### result.windows_setting.status.option.AccessDenied

Windows verweigert den Zugriff; kein negativer fachlicher Zustand, sondern fehlende Berechtigung.

### result.windows_setting.status.option.Failed

Lauf wegen Fehler fehlgeschlagen.

### result.windows_setting.status.option.Success

Windows-Aufruf erfolgreich. Erst anschließend den fachlichen Ergebniswert auswerten.

### result.windows_setting.status.option.Timeout

Windows-Aufruf überschritt seine Zeitgrenze; daraus keinen Systemzustand ableiten.

### result.windows_setting.status.option.Unsupported

Funktion auf diesem Zielsystem nicht unterstützt.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "WindowsSettingChangeResult",
  "name": "WindowsSettingChangeResult",
  "result": {
    "TypeName": "WindowsSettingChangeResult",
    "DisplayName": "WindowsSettingChangeResult",
    "Properties": [
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
        "Id": "windows_setting.status",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.status"
      },
      {
        "Name": "SettingId",
        "DisplayName": "Einstellung",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.setting_id",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.setting_id"
      },
      {
        "Name": "PreviousValue",
        "DisplayName": "Vorheriger Wert",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.previous_value",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.previous_value"
      },
      {
        "Name": "AppliedValue",
        "DisplayName": "Angewendeter Wert",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.applied_value",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.applied_value"
      },
      {
        "Name": "RestartRequired",
        "DisplayName": "Neustart erforderlich",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.restart_required",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.restart_required"
      },
      {
        "Name": "ErrorCode",
        "DisplayName": "Fehlercode",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "windows_setting.error_code",
        "EnumDisplayNames": null,
        "StableId": "windows_setting.error_code"
      }
    ],
    "PropertyTree": [
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
          "Id": "windows_setting.status",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.status"
        },
        "Children": []
      },
      {
        "Segment": "SettingId",
        "DisplayName": "Einstellung",
        "Property": {
          "Name": "SettingId",
          "DisplayName": "Einstellung",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.setting_id",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.setting_id"
        },
        "Children": []
      },
      {
        "Segment": "PreviousValue",
        "DisplayName": "Vorheriger Wert",
        "Property": {
          "Name": "PreviousValue",
          "DisplayName": "Vorheriger Wert",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.previous_value",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.previous_value"
        },
        "Children": []
      },
      {
        "Segment": "AppliedValue",
        "DisplayName": "Angewendeter Wert",
        "Property": {
          "Name": "AppliedValue",
          "DisplayName": "Angewendeter Wert",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.applied_value",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.applied_value"
        },
        "Children": []
      },
      {
        "Segment": "RestartRequired",
        "DisplayName": "Neustart erforderlich",
        "Property": {
          "Name": "RestartRequired",
          "DisplayName": "Neustart erforderlich",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.restart_required",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.restart_required"
        },
        "Children": []
      },
      {
        "Segment": "ErrorCode",
        "DisplayName": "Fehlercode",
        "Property": {
          "Name": "ErrorCode",
          "DisplayName": "Fehlercode",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "windows_setting.error_code",
          "EnumDisplayNames": null,
          "StableId": "windows_setting.error_code"
        },
        "Children": []
      }
    ]
  }
}
```

