## Steps: Windows-Zustand abfragen

ID: windows_state_query
Website: /doku/steps/windows_state_query/

Abbildung: [Detailansicht: Windows-Zustand abfragen. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion. Zusammengestellte Ausschnitte echter Einstellungsfelder mit Beispielwerten; die Nummer markiert Beschriftung und Eingabe gemeinsam.](/ref-step-windows-state-query.detail.png)

1. Windows-Funktion: Windows-Funktion und dazugehörige Parameter im Editor. Wähle eine Funktion, die die gewünschte Abfrage/Änderung tatsächlich unterstützt.
### errors

Prüfe die in dieser Referenz beschriebenen aktiven Pflichtfelder, kompatible Wertequellen und die Voraussetzungen auf dem Zielrechner. Ungültige Quellen oder Typen sind Fehler; eine gültige, aber nicht verfügbare optionale Quelle kann gemäß Eingabevertrag zum Überspringen führen. Ein Fehler beendet die Start-/Hauptphase; die vorgesehene Endphase wird zum Aufräumen behandelt. Der Verlauf zeigt den tatsächlich belegten Status.

### example

process.running mit name=notepad abfragen und is_running als Bedingung verwenden.

### field.capability

Windows-Funktion und dazugehörige Parameter im Editor. Wähle eine Funktion, die die gewünschte Abfrage/Änderung tatsächlich unterstützt.

### purpose

Wähle eine Windows-Funktion mit Zustandsabfrage. Ihr konkreter Ergebnisvertrag bestimmt die verfügbaren Werte; ein Wechsel der Funktion kann bestehende nachfolgende Referenzen ungültig machen. Nutze beispielsweise Netzwerkstatus, Leerlauf oder laufende Prozesse für eine Bedingung.

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

### schema.settings.parameters

Benannte Parameter der Windows-Funktion beziehungsweise bereinigte Diagnoseparameter im Log. Parameternamen sind stabil und keine UI-Labels.

### schema.settings.query_type

Stabile Kennung der Windows-Zustandsabfrage. Nach einem Wechsel nachfolgende Ergebnisbindungen erneut prüfen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "windows_state_query",
  "name": "Windows-Zustand abfragen",
  "description": "Fragt einen aktuellen Windows-Zustand ab und stellt das Ergebnis nachfolgenden Steps bereit.",
  "category": "WindowsSystem",
  "uiFieldIds": [
    "capability"
  ],
  "fields": [
    {
      "id": "capability",
      "name": "Windows-Funktion",
      "descriptor": {
        "Id": "capability",
        "LabelKey": "Ui.Windows.Capability",
        "ValueKind": "Object",
        "Required": true,
        "DefaultValue": {
          "capability_id": "network.connectivity",
          "parameters": {}
        },
        "DescriptionKey": null,
        "EditorHint": "windows-capability-picker",
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
        "WindowsCapabilityPickerOptions": {
          "Mode": "StateQuery"
        },
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
      "type": "WindowsStateQuerySettings",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.parameters",
      "name": "parameters",
      "type": "Dictionary`2",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "settings.query_type",
      "name": "query_type",
      "type": "String",
      "options": [],
      "defaultValue": "network.connectivity"
    }
  ],
  "inputs": [],
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

### Strukturelle Serializer-Vorlage
```json
{
  "type": "windows_state_query",
  "settings": {
    "query_type": "network.connectivity",
    "parameters": {}
  },
  "id": "6e1150cf-6454-7283-17b2-4a12a6c84fb9",
  "inputs": {},
  "is_enabled": true
}
```

