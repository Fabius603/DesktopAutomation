## Werte und Ergebnisse: KlickOnPoint3DResult

ID: KlickOnPoint3DResult
Website: /doku/werte/KlickOnPoint3DResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag KlickOnPoint3DResult. Wird von 3D-Ziel anklicken geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.click_on_point_3d.applied_delta_x

Tatsächlich angewendeter ganzzahliger horizontaler Mausversatz nach Skalierung und Rundung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.applied_delta_y

Tatsächlich angewendeter ganzzahliger vertikaler Mausversatz nach Skalierung und Rundung. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.delta_x

Horizontale Abweichung beziehungsweise Bewegungsanteil in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.delta_y

Vertikale Abweichung beziehungsweise Bewegungsanteil in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.movement_blocked

Ob die 3D-Eingabe wegen Bewegungstoleranz oder nicht neuerem Quell-Frame unterdrückt wurde. Es wurde dabei keine neue Bewegung und kein Klick gesendet. Einzelwert dieses Ergebnisses.

### result.click_on_point_3d.movement_factor_x

Horizontaler Skalierungsfaktor, der für diese Eingabe verwendet wurde. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.click_on_point_3d.movement_factor_y

Vertikaler Skalierungsfaktor, der für diese Eingabe verwendet wurde. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "KlickOnPoint3DResult",
  "name": "KlickOnPoint3DResult",
  "result": {
    "TypeName": "KlickOnPoint3DResult",
    "DisplayName": "KlickOnPoint3DResult",
    "Properties": [
      {
        "Name": "DeltaX",
        "DisplayName": "Delta X",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.delta_x",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.delta_x"
      },
      {
        "Name": "DeltaY",
        "DisplayName": "Delta Y",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.delta_y",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.delta_y"
      },
      {
        "Name": "MovementFactorX",
        "DisplayName": "Movement Factor X",
        "DataType": "Number",
        "Description": "Number, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.movement_factor_x",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.movement_factor_x"
      },
      {
        "Name": "MovementFactorY",
        "DisplayName": "Movement Factor Y",
        "DataType": "Number",
        "Description": "Number, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.movement_factor_y",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.movement_factor_y"
      },
      {
        "Name": "AppliedDeltaX",
        "DisplayName": "Applied Delta X",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.applied_delta_x",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.applied_delta_x"
      },
      {
        "Name": "AppliedDeltaY",
        "DisplayName": "Applied Delta Y",
        "DataType": "Integer",
        "Description": "Integer, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.applied_delta_y",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.applied_delta_y"
      },
      {
        "Name": "MovementBlocked",
        "DisplayName": "Movement Blocked",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "click_on_point_3d.movement_blocked",
        "EnumDisplayNames": null,
        "StableId": "click_on_point_3d.movement_blocked"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "DeltaX",
        "DisplayName": "Delta X",
        "Property": {
          "Name": "DeltaX",
          "DisplayName": "Delta X",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.delta_x",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.delta_x"
        },
        "Children": []
      },
      {
        "Segment": "DeltaY",
        "DisplayName": "Delta Y",
        "Property": {
          "Name": "DeltaY",
          "DisplayName": "Delta Y",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.delta_y",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.delta_y"
        },
        "Children": []
      },
      {
        "Segment": "MovementFactorX",
        "DisplayName": "Movement Factor X",
        "Property": {
          "Name": "MovementFactorX",
          "DisplayName": "Movement Factor X",
          "DataType": "Number",
          "Description": "Number, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.movement_factor_x",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.movement_factor_x"
        },
        "Children": []
      },
      {
        "Segment": "MovementFactorY",
        "DisplayName": "Movement Factor Y",
        "Property": {
          "Name": "MovementFactorY",
          "DisplayName": "Movement Factor Y",
          "DataType": "Number",
          "Description": "Number, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.movement_factor_y",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.movement_factor_y"
        },
        "Children": []
      },
      {
        "Segment": "AppliedDeltaX",
        "DisplayName": "Applied Delta X",
        "Property": {
          "Name": "AppliedDeltaX",
          "DisplayName": "Applied Delta X",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.applied_delta_x",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.applied_delta_x"
        },
        "Children": []
      },
      {
        "Segment": "AppliedDeltaY",
        "DisplayName": "Applied Delta Y",
        "Property": {
          "Name": "AppliedDeltaY",
          "DisplayName": "Applied Delta Y",
          "DataType": "Integer",
          "Description": "Integer, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.applied_delta_y",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.applied_delta_y"
        },
        "Children": []
      },
      {
        "Segment": "MovementBlocked",
        "DisplayName": "Movement Blocked",
        "Property": {
          "Name": "MovementBlocked",
          "DisplayName": "Movement Blocked",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "click_on_point_3d.movement_blocked",
          "EnumDisplayNames": null,
          "StableId": "click_on_point_3d.movement_blocked"
        },
        "Children": []
      }
    ]
  }
}
```

