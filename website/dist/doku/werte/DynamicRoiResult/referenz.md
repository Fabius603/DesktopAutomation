## Werte und Ergebnisse: DynamicRoiResult

ID: DynamicRoiResult
Website: /doku/werte/DynamicRoiResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag DynamicRoiResult. Wird von Dynamischen Bildbereich erstellen geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.consecutive_misses

Anzahl aufeinanderfolgender nicht verwertbarer Treffer seit der letzten gültigen Aktualisierung. Einzelwert dieses Ergebnisses.

### result.full_search_interval

Konfigurierter Abstand regelmäßiger Vollsuchen in Durchläufen; 0 bedeutet keine regelmäßige Vollsuche. Einzelwert dieses Ergebnisses.

### result.global_bounds

Suchrechteck in globalen Desktopkoordinaten. Für bildlokale Verbraucher muss dessen Quelle/Versatz berücksichtigt werden. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.height

Höhe des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.left

Linke Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.top

Obere Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.width

Breite des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.global_bounds.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.roi_reset

Ob der Suchbereich wegen der Rücksetzregel auf den Ausgangsbereich zurückgesetzt wurde. Einzelwert dieses Ergebnisses.

### result.roi_updated

Ob der dynamische Suchbereich in diesem Step neu aus einem verwertbaren Treffer aktualisiert wurde. Einzelwert dieses Ergebnisses.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "DynamicRoiResult",
  "name": "DynamicRoiResult",
  "result": {
    "TypeName": "DynamicRoiResult",
    "DisplayName": "DynamicRoiResult",
    "Properties": [
      {
        "Name": "RoiUpdated",
        "DisplayName": "Roi Updated",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "roi_updated",
        "EnumDisplayNames": null,
        "StableId": "roi_updated"
      },
      {
        "Name": "RoiReset",
        "DisplayName": "Roi Reset",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "roi_reset",
        "EnumDisplayNames": null,
        "StableId": "roi_reset"
      },
      {
        "Name": "GlobalBounds",
        "DisplayName": "Global Bounds",
        "DataType": "Rectangle",
        "Description": "Rectangle, kann leer sein",
        "IsNullable": true,
        "Example": "{X=10,Y=20,Width=300,Height=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds",
        "EnumDisplayNames": null,
        "StableId": "global_bounds"
      },
      {
        "Name": "GlobalBounds.X",
        "DisplayName": "Global Bounds / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.x",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.x"
      },
      {
        "Name": "GlobalBounds.Y",
        "DisplayName": "Global Bounds / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.y",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.y"
      },
      {
        "Name": "GlobalBounds.Width",
        "DisplayName": "Global Bounds / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.width",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.width"
      },
      {
        "Name": "GlobalBounds.Height",
        "DisplayName": "Global Bounds / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.height",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.height"
      },
      {
        "Name": "GlobalBounds.Left",
        "DisplayName": "Global Bounds / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.left",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.left"
      },
      {
        "Name": "GlobalBounds.Top",
        "DisplayName": "Global Bounds / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.top",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.top"
      },
      {
        "Name": "GlobalBounds.Right",
        "DisplayName": "Global Bounds / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.right",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.right"
      },
      {
        "Name": "GlobalBounds.Bottom",
        "DisplayName": "Global Bounds / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.bottom",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.bottom"
      },
      {
        "Name": "GlobalBounds.IsEmpty",
        "DisplayName": "Global Bounds / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.is_empty",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.is_empty"
      },
      {
        "Name": "GlobalBounds.Location",
        "DisplayName": "Global Bounds / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.location",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.location"
      },
      {
        "Name": "GlobalBounds.Location.X",
        "DisplayName": "Global Bounds / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.location.x",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.location.x"
      },
      {
        "Name": "GlobalBounds.Location.Y",
        "DisplayName": "Global Bounds / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.location.y",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.location.y"
      },
      {
        "Name": "GlobalBounds.Center",
        "DisplayName": "Global Bounds / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.center",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.center"
      },
      {
        "Name": "GlobalBounds.Center.X",
        "DisplayName": "Global Bounds / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.center.x",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.center.x"
      },
      {
        "Name": "GlobalBounds.Center.Y",
        "DisplayName": "Global Bounds / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "global_bounds.center.y",
        "EnumDisplayNames": null,
        "StableId": "global_bounds.center.y"
      },
      {
        "Name": "ConsecutiveMisses",
        "DisplayName": "Consecutive Misses",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "consecutive_misses",
        "EnumDisplayNames": null,
        "StableId": "consecutive_misses"
      },
      {
        "Name": "FullSearchInterval",
        "DisplayName": "Full Search Interval",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "full_search_interval",
        "EnumDisplayNames": null,
        "StableId": "full_search_interval"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "RoiUpdated",
        "DisplayName": "Roi Updated",
        "Property": {
          "Name": "RoiUpdated",
          "DisplayName": "Roi Updated",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "roi_updated",
          "EnumDisplayNames": null,
          "StableId": "roi_updated"
        },
        "Children": []
      },
      {
        "Segment": "RoiReset",
        "DisplayName": "Roi Reset",
        "Property": {
          "Name": "RoiReset",
          "DisplayName": "Roi Reset",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "roi_reset",
          "EnumDisplayNames": null,
          "StableId": "roi_reset"
        },
        "Children": []
      },
      {
        "Segment": "GlobalBounds",
        "DisplayName": "Global Bounds",
        "Property": {
          "Name": "GlobalBounds",
          "DisplayName": "Global Bounds",
          "DataType": "Rectangle",
          "Description": "Rectangle, kann leer sein",
          "IsNullable": true,
          "Example": "{X=10,Y=20,Width=300,Height=200}",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "global_bounds",
          "EnumDisplayNames": null,
          "StableId": "global_bounds"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "GlobalBounds.X",
              "DisplayName": "Global Bounds / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.x",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "GlobalBounds.Y",
              "DisplayName": "Global Bounds / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.y",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Width",
            "Property": {
              "Name": "GlobalBounds.Width",
              "DisplayName": "Global Bounds / Width",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.width",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Height",
            "Property": {
              "Name": "GlobalBounds.Height",
              "DisplayName": "Global Bounds / Height",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.height",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Left",
            "Property": {
              "Name": "GlobalBounds.Left",
              "DisplayName": "Global Bounds / Left",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.left",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Top",
            "Property": {
              "Name": "GlobalBounds.Top",
              "DisplayName": "Global Bounds / Top",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.top",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Right",
            "Property": {
              "Name": "GlobalBounds.Right",
              "DisplayName": "Global Bounds / Right",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.right",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Bottom",
            "Property": {
              "Name": "GlobalBounds.Bottom",
              "DisplayName": "Global Bounds / Bottom",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.bottom",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Is Empty",
            "Property": {
              "Name": "GlobalBounds.IsEmpty",
              "DisplayName": "Global Bounds / Is Empty",
              "DataType": "Boolean",
              "Description": "Boolean",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.is_empty",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Location",
            "Property": {
              "Name": "GlobalBounds.Location",
              "DisplayName": "Global Bounds / Location",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.location",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "GlobalBounds.Location.X",
                  "DisplayName": "Global Bounds / Location / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "global_bounds.location.x",
                  "EnumDisplayNames": null,
                  "StableId": "global_bounds.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "GlobalBounds.Location.Y",
                  "DisplayName": "Global Bounds / Location / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "global_bounds.location.y",
                  "EnumDisplayNames": null,
                  "StableId": "global_bounds.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "GlobalBounds.Center",
              "DisplayName": "Global Bounds / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "global_bounds.center",
              "EnumDisplayNames": null,
              "StableId": "global_bounds.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "GlobalBounds.Center.X",
                  "DisplayName": "Global Bounds / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "global_bounds.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "global_bounds.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "GlobalBounds.Center.Y",
                  "DisplayName": "Global Bounds / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "global_bounds.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "global_bounds.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "ConsecutiveMisses",
        "DisplayName": "Consecutive Misses",
        "Property": {
          "Name": "ConsecutiveMisses",
          "DisplayName": "Consecutive Misses",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "consecutive_misses",
          "EnumDisplayNames": null,
          "StableId": "consecutive_misses"
        },
        "Children": []
      },
      {
        "Segment": "FullSearchInterval",
        "DisplayName": "Full Search Interval",
        "Property": {
          "Name": "FullSearchInterval",
          "DisplayName": "Full Search Interval",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "full_search_interval",
          "EnumDisplayNames": null,
          "StableId": "full_search_interval"
        },
        "Children": []
      }
    ]
  }
}
```

