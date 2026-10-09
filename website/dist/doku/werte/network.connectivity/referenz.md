## Werte und Ergebnisse: Aktuelle Netzwerkverbindung

ID: network.connectivity
Website: /doku/werte/network.connectivity/

### errors

Verfügbarkeit und Ereignisevidenz hängen von Windows-Version, installiertem Gerät/Dienst und Rechten ab. Die Abfrage ist ein zeitlicher Snapshot. Nur die im Katalog gesetzten Unterstützungsmerkmale verwenden: eine reine Änderungsfunktion liefert keinen eigenen Abfragevertrag, eine reine Abfrage ist kein Ereignistrigger. Bei Windows-Einstellungen den Erfolg und Fehlercode im Step-Ergebnis prüfen.

### example

Wähle network.connectivity im Step „Windows-Zustand abfragen“ und verbinde einen unten beschriebenen Ergebniswert mit einer Bedingung.

### purpose

Zustandsabfrage: Aktuelle Netzwerkverbindung. Liest den aktuellen vom System angebotenen Zustand und stellt die hier aufgeführten typisierten Werte bereit. Wähle die Funktion im Windows-Auswahldialog; die verfügbaren Parameter stehen unten. Der Katalog verlangt keine erhöhte Ausführung; einzelne Systemzugriffe können dennoch Rechte voraussetzen.

### result.captured_at

Zeitpunkt der Windows-Abfrage beziehungsweise Erfassung im Ergebnis. Er zeigt den Beobachtungsstand, nicht einen garantierten späteren Systemzustand. Einzelwert dieses Ergebnisses.

### result.connection_type

Art der ermittelten Netzwerkverbindung, soweit vom System verfügbar. Einzelwert dieses Ergebnisses.

### result.connection_type.option.Ethernet

Kabelgebundene Ethernet-Verbindung.

### result.connection_type.option.Mobile

Mobilfunkverbindung.

### result.connection_type.option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

### result.connection_type.option.Virtual

Virtueller Netzwerkadapter beziehungsweise virtuelle Verbindung.

### result.connection_type.option.WiFi

Drahtlose WLAN-Verbindung.

### result.connectivity

Vom Netzwerkprovider gemeldete Verbindungsqualität beziehungsweise Konnektivität. Einzelwert dieses Ergebnisses.

### result.connectivity.option.Disconnected

Verbindung getrennt beziehungsweise Sitzung nicht verbunden; Bedeutung ergibt sich aus dem jeweiligen Ergebnisfeld.

### result.connectivity.option.Internet

Windows meldet Internetkonnektivität.

### result.connectivity.option.LocalNetwork

Windows meldet lokale Netzwerkkonnektivität ohne belegten Internetzugang.

### result.connectivity.option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

### result.count

Anzahl der Elemente der referenzierten Sammlung, automatisch als zusätzlicher Ergebniswert verfügbar. Einzelwert dieses Ergebnisses.

### result.error_code

Maschinenlesbarer Fehlercode einer Windows-Operation; bei Erfolg entsprechend dem Vertrag leer beziehungsweise nicht gesetzt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.error_message

Lesbare Diagnose der Windows-Operation. Für Entscheidungen vorzugsweise Status und stabilen Code verwenden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.is_connected

Ob die Verbindung zum Zeitpunkt der Abfrage hergestellt war. Einzelwert dieses Ergebnisses.

### result.items

Liste abgefragter Objekte, zum Beispiel Geräte, Monitore, Drucker oder Laufwerke. Mitglieder besitzen die in dieser Referenz aufgeführten Unterwerte. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.items.count

Anzahl der Elemente der referenzierten Sammlung, automatisch als zusätzlicher Ergebniswert verfügbar. Einzelwert dieses Ergebnisses.

### result.name

Lesbarer Name des abgefragten Geräts, Monitors, Laufwerks oder Objekts. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

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
  "id": "network.connectivity",
  "name": "Aktuelle Netzwerkverbindung",
  "fields": [],
  "result": {
    "TypeName": "NetworkConnectivityQueryResult",
    "DisplayName": "NetworkConnectivityQueryResult",
    "Properties": [
      {
        "Name": "IsConnected",
        "DisplayName": "Is Connected",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_connected",
        "EnumDisplayNames": null,
        "StableId": "is_connected"
      },
      {
        "Name": "Connectivity",
        "DisplayName": "Connectivity",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsConnectivity",
        "EnumValues": [
          "Unknown",
          "Disconnected",
          "LocalNetwork",
          "Internet"
        ],
        "Id": "connectivity",
        "EnumDisplayNames": null,
        "StableId": "connectivity"
      },
      {
        "Name": "ConnectionType",
        "DisplayName": "Connection Type",
        "DataType": "Enum",
        "Description": "Enum",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsConnectionType",
        "EnumValues": [
          "Unknown",
          "Ethernet",
          "WiFi",
          "Mobile",
          "Virtual"
        ],
        "Id": "connection_type",
        "EnumDisplayNames": null,
        "StableId": "connection_type"
      },
      {
        "Name": "Name",
        "DisplayName": "Name",
        "DataType": "Text",
        "Description": "Text, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "name",
        "EnumDisplayNames": null,
        "StableId": "name"
      },
      {
        "Name": "Count",
        "DisplayName": "Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "count",
        "EnumDisplayNames": null,
        "StableId": "count"
      },
      {
        "Name": "Items",
        "DisplayName": "Items",
        "DataType": "Text",
        "Description": "Text",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "items",
        "EnumDisplayNames": null,
        "StableId": "items"
      },
      {
        "Name": "Items.Count",
        "DisplayName": "Items / Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "items.count",
        "EnumDisplayNames": null,
        "StableId": "items.count"
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
        "Segment": "IsConnected",
        "DisplayName": "Is Connected",
        "Property": {
          "Name": "IsConnected",
          "DisplayName": "Is Connected",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_connected",
          "EnumDisplayNames": null,
          "StableId": "is_connected"
        },
        "Children": []
      },
      {
        "Segment": "Connectivity",
        "DisplayName": "Connectivity",
        "Property": {
          "Name": "Connectivity",
          "DisplayName": "Connectivity",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsConnectivity",
          "EnumValues": [
            "Unknown",
            "Disconnected",
            "LocalNetwork",
            "Internet"
          ],
          "Id": "connectivity",
          "EnumDisplayNames": null,
          "StableId": "connectivity"
        },
        "Children": []
      },
      {
        "Segment": "ConnectionType",
        "DisplayName": "Connection Type",
        "Property": {
          "Name": "ConnectionType",
          "DisplayName": "Connection Type",
          "DataType": "Enum",
          "Description": "Enum",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": "TaskAutomation.WindowsIntegration.WindowsConnectionType",
          "EnumValues": [
            "Unknown",
            "Ethernet",
            "WiFi",
            "Mobile",
            "Virtual"
          ],
          "Id": "connection_type",
          "EnumDisplayNames": null,
          "StableId": "connection_type"
        },
        "Children": []
      },
      {
        "Segment": "Name",
        "DisplayName": "Name",
        "Property": {
          "Name": "Name",
          "DisplayName": "Name",
          "DataType": "Text",
          "Description": "Text, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "name",
          "EnumDisplayNames": null,
          "StableId": "name"
        },
        "Children": []
      },
      {
        "Segment": "Count",
        "DisplayName": "Count",
        "Property": {
          "Name": "Count",
          "DisplayName": "Count",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "count",
          "EnumDisplayNames": null,
          "StableId": "count"
        },
        "Children": []
      },
      {
        "Segment": "Items",
        "DisplayName": "Items",
        "Property": {
          "Name": "Items",
          "DisplayName": "Items",
          "DataType": "Text",
          "Description": "Text",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "items",
          "EnumDisplayNames": null,
          "StableId": "items"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Count",
            "Property": {
              "Name": "Items.Count",
              "DisplayName": "Items / Count",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "items.count",
              "EnumDisplayNames": null,
              "StableId": "items.count"
            },
            "Children": []
          }
        ]
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

