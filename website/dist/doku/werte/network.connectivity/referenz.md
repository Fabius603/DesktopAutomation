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

UTC-Zeitpunkt, zu dem der Windows-Zustand ermittelt wurde. Beispiel: 2030-01-01T08:00:00Z.

### result.connection_type

Art der ermittelten Verbindung, beispielsweise WLAN oder Ethernet.

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

Von Windows gemeldeter Grad der Netzwerkverbindung, etwa lokal oder mit Internetzugriff.

### result.connectivity.option.Disconnected

Verbindung getrennt beziehungsweise Sitzung nicht verbunden; Bedeutung ergibt sich aus dem jeweiligen Ergebnisfeld.

### result.connectivity.option.Internet

Windows meldet Internetkonnektivität.

### result.connectivity.option.LocalNetwork

Windows meldet lokale Netzwerkkonnektivität ohne belegten Internetzugang.

### result.connectivity.option.Unknown

Die vorhandenen Daten erlauben keinen eindeutigeren Zustand.

### result.count

Anzahl aktiver Netzwerkschnittstellen ohne Loopback. Beispiel: 3.

### result.error_code

Code zur Fehlerdiagnose. Bei erfolgreicher Windows-Aktion leer. In der App unter Diagnose aufgeführt.

### result.error_message

Beschreibung des Windows-Fehlers. Bei erfolgreicher Aktion leer. In der App unter Diagnose aufgeführt.

### result.is_connected

Gibt an, ob die konfigurierte Verbindung oder das abgefragte Gerät verbunden ist. Beispiel: true.

### result.items

Namen der aktiven Netzwerkschnittstellen ohne Loopback. Ohne Schnittstellen leer.

### result.items.count

Anzahl der Elemente in „Gefundene Elemente“. Eine leere Liste hat die Anzahl 0. Beispiel: 3.

### result.name

Name der ersten aktiven Netzwerkschnittstelle ohne Loopback; ohne Schnittstelle leer.

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
  "id": "network.connectivity",
  "name": "Aktuelle Netzwerkverbindung",
  "fields": [],
  "result": {
    "TypeName": "NetworkConnectivityQueryResult",
    "DisplayName": "NetworkConnectivityQueryResult",
    "Properties": [
      {
        "Name": "IsConnected",
        "DisplayName": "Verbindung vorhanden",
        "DataType": "Boolean",
        "Description": "Gibt an, ob die konfigurierte Verbindung oder das abgefragte Gerät verbunden ist.",
        "IsNullable": false,
        "Example": "true",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "is_connected",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "is_connected"
      },
      {
        "Name": "Connectivity",
        "DisplayName": "Netzwerkzugriff",
        "DataType": "Enum",
        "Description": "Von Windows gemeldeter Grad der Netzwerkverbindung, etwa lokal oder mit Internetzugriff.",
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
        "IsDiagnostic": false,
        "StableId": "connectivity"
      },
      {
        "Name": "ConnectionType",
        "DisplayName": "Verbindungsart",
        "DataType": "Enum",
        "Description": "Art der ermittelten Verbindung, beispielsweise WLAN oder Ethernet.",
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
        "IsDiagnostic": false,
        "StableId": "connection_type"
      },
      {
        "Name": "Name",
        "DisplayName": "Name der Netzwerkschnittstelle",
        "DataType": "Text",
        "Description": "Name der ersten aktiven Netzwerkschnittstelle ohne Loopback; ohne Schnittstelle leer.",
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
        "Name": "Count",
        "DisplayName": "Anzahl aktiver Netzwerkschnittstellen",
        "DataType": "Integer",
        "Description": "Anzahl aktiver Netzwerkschnittstellen ohne Loopback.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "count",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "count"
      },
      {
        "Name": "Items",
        "DisplayName": "Aktive Netzwerkschnittstellen",
        "DataType": "Text",
        "Description": "Namen der aktiven Netzwerkschnittstellen ohne Loopback. Ohne Schnittstellen leer.",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "items",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "items"
      },
      {
        "Name": "Items.Count",
        "DisplayName": "Gefundene Elemente / Anzahl",
        "DataType": "Integer",
        "Description": "Anzahl der Elemente in „Gefundene Elemente“. Eine leere Liste hat die Anzahl 0.",
        "IsNullable": false,
        "Example": "3",
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "items.count",
        "EnumDisplayNames": null,
        "IsDiagnostic": false,
        "StableId": "items.count"
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
        "Segment": "IsConnected",
        "DisplayName": "Verbindung vorhanden",
        "Property": {
          "Name": "IsConnected",
          "DisplayName": "Verbindung vorhanden",
          "DataType": "Boolean",
          "Description": "Gibt an, ob die konfigurierte Verbindung oder das abgefragte Gerät verbunden ist.",
          "IsNullable": false,
          "Example": "true",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "is_connected",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "is_connected"
        },
        "Children": []
      },
      {
        "Segment": "Connectivity",
        "DisplayName": "Netzwerkzugriff",
        "Property": {
          "Name": "Connectivity",
          "DisplayName": "Netzwerkzugriff",
          "DataType": "Enum",
          "Description": "Von Windows gemeldeter Grad der Netzwerkverbindung, etwa lokal oder mit Internetzugriff.",
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
          "IsDiagnostic": false,
          "StableId": "connectivity"
        },
        "Children": []
      },
      {
        "Segment": "ConnectionType",
        "DisplayName": "Verbindungsart",
        "Property": {
          "Name": "ConnectionType",
          "DisplayName": "Verbindungsart",
          "DataType": "Enum",
          "Description": "Art der ermittelten Verbindung, beispielsweise WLAN oder Ethernet.",
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
          "IsDiagnostic": false,
          "StableId": "connection_type"
        },
        "Children": []
      },
      {
        "Segment": "Name",
        "DisplayName": "Name der Netzwerkschnittstelle",
        "Property": {
          "Name": "Name",
          "DisplayName": "Name der Netzwerkschnittstelle",
          "DataType": "Text",
          "Description": "Name der ersten aktiven Netzwerkschnittstelle ohne Loopback; ohne Schnittstelle leer.",
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
        "Segment": "Count",
        "DisplayName": "Anzahl aktiver Netzwerkschnittstellen",
        "Property": {
          "Name": "Count",
          "DisplayName": "Anzahl aktiver Netzwerkschnittstellen",
          "DataType": "Integer",
          "Description": "Anzahl aktiver Netzwerkschnittstellen ohne Loopback.",
          "IsNullable": false,
          "Example": "3",
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "count",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "count"
        },
        "Children": []
      },
      {
        "Segment": "Items",
        "DisplayName": "Aktive Netzwerkschnittstellen",
        "Property": {
          "Name": "Items",
          "DisplayName": "Aktive Netzwerkschnittstellen",
          "DataType": "Text",
          "Description": "Namen der aktiven Netzwerkschnittstellen ohne Loopback. Ohne Schnittstellen leer.",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "items",
          "EnumDisplayNames": null,
          "IsDiagnostic": false,
          "StableId": "items"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Anzahl",
            "Property": {
              "Name": "Items.Count",
              "DisplayName": "Gefundene Elemente / Anzahl",
              "DataType": "Integer",
              "Description": "Anzahl der Elemente in „Gefundene Elemente“. Eine leere Liste hat die Anzahl 0.",
              "IsNullable": false,
              "Example": "3",
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "items.count",
              "EnumDisplayNames": null,
              "IsDiagnostic": false,
              "StableId": "items.count"
            },
            "Children": []
          }
        ]
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

