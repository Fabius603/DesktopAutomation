## Werte und Ergebnisse: TemplateMatchingResult

ID: TemplateMatchingResult
Website: /doku/werte/TemplateMatchingResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag TemplateMatchingResult. Wird von Bildvorlage erkennen geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### result.all_detections

Liste einzelner Erkennungen mit eigenen Punkten, Rechtecken und Konfidenzen. Die Anzahl ist nicht gleich einer pauschalen Erfolgsbewertung. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box

Begrenzendes Rechteck eines Treffers; bei fehlender Erkennung kann ein optionales Rechteck fehlen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.height

Höhe des Ergebnisrechtecks in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.left

Linke Rechteckkante in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.top

Obere Rechteckkante in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.width

Breite des Ergebnisrechtecks in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.bounding_box.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.confidence

Normalisierte Trefferkonfidenz zwischen 0 und 1. 0.9 entspricht 90 %; mehrere Treffer können unterschiedliche Konfidenzen besitzen. Geordnete Liste; ein Verbraucher entscheidet gemäß seinem Eingabevertrag über erstes Element oder alle Elemente.

### result.all_detections.count

Anzahl der Elemente der referenzierten Sammlung, automatisch als zusätzlicher Ergebniswert verfügbar. Einzelwert dieses Ergebnisses.

### result.applied_roi

Tatsächlich angewendeter Suchbereich. Damit lässt sich prüfen, ob eine direkte oder dynamische Eingrenzung aktiv war. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.height

Höhe des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.left

Linke Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.top

Obere Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.width

Breite des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.applied_roi.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box

Begrenzendes Rechteck eines Treffers; bei fehlender Erkennung kann ein optionales Rechteck fehlen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.bottom

Untere Rechteckkante in Pixeln, aus Ursprung und Höhe abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.center

Mittelpunkt einer einzelnen Erkennung in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.center.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.center.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.height

Höhe des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.is_empty

Ob die Zwischenablage im abgefragten Kontext leer ist. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.left

Linke Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.location

Pixelposition/-bereich eines erkannten Wortes oder Trefferobjekts; verwende den im Ergebnis ausgewiesenen Koordinatenraum. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.location.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.location.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.right

Rechte Rechteckkante in Pixeln, aus Ursprung und Breite abgeleitet. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.top

Obere Rechteckkante in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.width

Breite des Ergebnisrechtecks in Pixeln. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.bounding_box.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.confidence

Normalisierte Trefferkonfidenz zwischen 0 und 1. 0.9 entspricht 90 %; mehrere Treffer können unterschiedliche Konfidenzen besitzen. Einzelwert dieses Ergebnisses.

### result.found

Ob die Erkennung/Suche in diesem Lauf einen passenden Treffer geliefert hat. false ist ein fachliches Ergebnis und nicht automatisch ein Step-Fehler. Einzelwert dieses Ergebnisses.

### result.point

Ermittelter Zielpunkt. Bei Bildschirmquellen ist der Quelle-zu-Desktop-Versatz bereits entsprechend dem Ergebnisvertrag berücksichtigt. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.point.x

Horizontale Punkt-/Versatzkoordinate in Pixeln; den im übergeordneten Ergebnis beschriebenen Koordinatenraum beachten. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.point.y

Vertikale Punkt-/Versatzkoordinate in Pixeln; nicht ungeprüft bildlokale und globale Werte mischen. Optionaler Einzelwert: bei fehlender Information keine Eigenschaft erfinden.

### result.source_capture_is_fresh

Frischezustand der verwendeten Aufnahme, unverändert in das Erkennungsergebnis übernommen. Einzelwert dieses Ergebnisses.

### result.source_capture_timestamp_utc

UTC-Aufnahmezeit der Bildquelle, aus der dieses Ergebnis berechnet wurde. Einzelwert dieses Ergebnisses.

### result.source_frame_timestamp

Monotoner Zeitstempel des zugrunde liegenden Frames. 0 bedeutet unbekannte zeitliche Herkunft. Einzelwert dieses Ergebnisses.

### result.source_frame_version

Frame-Identität der verwendeten Bildquelle; auch ein Erkennungsfehlschlag behält die Herkunftsinformation. Einzelwert dieses Ergebnisses.

### result.used_dynamic_roi

Ob für diese Auswertung ein dynamischer Suchbereich verwendet wurde. Einzelwert dieses Ergebnisses.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "TemplateMatchingResult",
  "name": "TemplateMatchingResult",
  "result": {
    "TypeName": "TemplateMatchingResult",
    "DisplayName": "TemplateMatchingResult",
    "Properties": [
      {
        "Name": "Found",
        "DisplayName": "Found",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "found",
        "EnumDisplayNames": null,
        "StableId": "found"
      },
      {
        "Name": "Point",
        "DisplayName": "Point",
        "DataType": "Point",
        "Description": "Point, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point",
        "EnumDisplayNames": null,
        "StableId": "point"
      },
      {
        "Name": "Point.X",
        "DisplayName": "Point / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point.x",
        "EnumDisplayNames": null,
        "StableId": "point.x"
      },
      {
        "Name": "Point.Y",
        "DisplayName": "Point / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "point.y",
        "EnumDisplayNames": null,
        "StableId": "point.y"
      },
      {
        "Name": "BoundingBox",
        "DisplayName": "Bounding Box",
        "DataType": "Rectangle",
        "Description": "Rectangle, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box",
        "EnumDisplayNames": null,
        "StableId": "bounding_box"
      },
      {
        "Name": "BoundingBox.X",
        "DisplayName": "Bounding Box / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.x",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.x"
      },
      {
        "Name": "BoundingBox.Y",
        "DisplayName": "Bounding Box / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.y",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.y"
      },
      {
        "Name": "BoundingBox.Width",
        "DisplayName": "Bounding Box / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.width",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.width"
      },
      {
        "Name": "BoundingBox.Height",
        "DisplayName": "Bounding Box / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.height",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.height"
      },
      {
        "Name": "BoundingBox.Left",
        "DisplayName": "Bounding Box / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.left",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.left"
      },
      {
        "Name": "BoundingBox.Top",
        "DisplayName": "Bounding Box / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.top",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.top"
      },
      {
        "Name": "BoundingBox.Right",
        "DisplayName": "Bounding Box / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.right",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.right"
      },
      {
        "Name": "BoundingBox.Bottom",
        "DisplayName": "Bounding Box / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.bottom",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.bottom"
      },
      {
        "Name": "BoundingBox.IsEmpty",
        "DisplayName": "Bounding Box / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.is_empty",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.is_empty"
      },
      {
        "Name": "BoundingBox.Location",
        "DisplayName": "Bounding Box / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.location"
      },
      {
        "Name": "BoundingBox.Location.X",
        "DisplayName": "Bounding Box / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location.x",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.location.x"
      },
      {
        "Name": "BoundingBox.Location.Y",
        "DisplayName": "Bounding Box / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.location.y",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.location.y"
      },
      {
        "Name": "BoundingBox.Center",
        "DisplayName": "Bounding Box / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.center"
      },
      {
        "Name": "BoundingBox.Center.X",
        "DisplayName": "Bounding Box / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center.x",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.center.x"
      },
      {
        "Name": "BoundingBox.Center.Y",
        "DisplayName": "Bounding Box / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "bounding_box.center.y",
        "EnumDisplayNames": null,
        "StableId": "bounding_box.center.y"
      },
      {
        "Name": "Confidence",
        "DisplayName": "Confidence",
        "DataType": "Number",
        "Description": "Number",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "confidence",
        "EnumDisplayNames": null,
        "StableId": "confidence"
      },
      {
        "Name": "SourceCaptureIsFresh",
        "DisplayName": "Source Capture Is Fresh",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_capture_is_fresh",
        "EnumDisplayNames": null,
        "StableId": "source_capture_is_fresh"
      },
      {
        "Name": "SourceCaptureTimestampUtc",
        "DisplayName": "Source Capture Timestamp Utc",
        "DataType": "DateTime",
        "Description": "DateTime",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_capture_timestamp_utc",
        "EnumDisplayNames": null,
        "StableId": "source_capture_timestamp_utc"
      },
      {
        "Name": "SourceFrameVersion",
        "DisplayName": "Source Frame Version",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_frame_version",
        "EnumDisplayNames": null,
        "StableId": "source_frame_version"
      },
      {
        "Name": "SourceFrameTimestamp",
        "DisplayName": "Source Frame Timestamp",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "source_frame_timestamp",
        "EnumDisplayNames": null,
        "StableId": "source_frame_timestamp"
      },
      {
        "Name": "AppliedRoi",
        "DisplayName": "Applied Roi",
        "DataType": "Rectangle",
        "Description": "Rectangle, kann leer sein",
        "IsNullable": true,
        "Example": "{X=10,Y=20,Width=300,Height=200}",
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi",
        "EnumDisplayNames": null,
        "StableId": "applied_roi"
      },
      {
        "Name": "AppliedRoi.X",
        "DisplayName": "Applied Roi / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.x",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.x"
      },
      {
        "Name": "AppliedRoi.Y",
        "DisplayName": "Applied Roi / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.y",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.y"
      },
      {
        "Name": "AppliedRoi.Width",
        "DisplayName": "Applied Roi / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.width",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.width"
      },
      {
        "Name": "AppliedRoi.Height",
        "DisplayName": "Applied Roi / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.height",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.height"
      },
      {
        "Name": "AppliedRoi.Left",
        "DisplayName": "Applied Roi / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.left",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.left"
      },
      {
        "Name": "AppliedRoi.Top",
        "DisplayName": "Applied Roi / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.top",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.top"
      },
      {
        "Name": "AppliedRoi.Right",
        "DisplayName": "Applied Roi / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.right",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.right"
      },
      {
        "Name": "AppliedRoi.Bottom",
        "DisplayName": "Applied Roi / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.bottom",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.bottom"
      },
      {
        "Name": "AppliedRoi.IsEmpty",
        "DisplayName": "Applied Roi / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.is_empty",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.is_empty"
      },
      {
        "Name": "AppliedRoi.Location",
        "DisplayName": "Applied Roi / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.location",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.location"
      },
      {
        "Name": "AppliedRoi.Location.X",
        "DisplayName": "Applied Roi / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.location.x",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.location.x"
      },
      {
        "Name": "AppliedRoi.Location.Y",
        "DisplayName": "Applied Roi / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.location.y",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.location.y"
      },
      {
        "Name": "AppliedRoi.Center",
        "DisplayName": "Applied Roi / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.center",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.center"
      },
      {
        "Name": "AppliedRoi.Center.X",
        "DisplayName": "Applied Roi / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.center.x",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.center.x"
      },
      {
        "Name": "AppliedRoi.Center.Y",
        "DisplayName": "Applied Roi / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "OptionalSingle",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "applied_roi.center.y",
        "EnumDisplayNames": null,
        "StableId": "applied_roi.center.y"
      },
      {
        "Name": "UsedDynamicRoi",
        "DisplayName": "Used Dynamic Roi",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "used_dynamic_roi",
        "EnumDisplayNames": null,
        "StableId": "used_dynamic_roi"
      },
      {
        "Name": "AllDetections",
        "DisplayName": "All Detections",
        "DataType": "Detection",
        "Description": "Detection",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections",
        "EnumDisplayNames": null,
        "StableId": "all_detections"
      },
      {
        "Name": "AllDetections.Count",
        "DisplayName": "All Detections / Count",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Single",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.count",
        "EnumDisplayNames": null,
        "StableId": "all_detections.count"
      },
      {
        "Name": "AllDetections[].Center",
        "DisplayName": "All Detections[] / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center",
        "EnumDisplayNames": null,
        "StableId": "all_detections.center"
      },
      {
        "Name": "AllDetections[].Center.X",
        "DisplayName": "All Detections[] / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.center.x"
      },
      {
        "Name": "AllDetections[].Center.Y",
        "DisplayName": "All Detections[] / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.center.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.center.y"
      },
      {
        "Name": "AllDetections[].BoundingBox",
        "DisplayName": "All Detections[] / Bounding Box",
        "DataType": "Rectangle",
        "Description": "Rectangle, kann leer sein",
        "IsNullable": true,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box"
      },
      {
        "Name": "AllDetections[].BoundingBox.X",
        "DisplayName": "All Detections[] / Bounding Box / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Y",
        "DisplayName": "All Detections[] / Bounding Box / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.y"
      },
      {
        "Name": "AllDetections[].BoundingBox.Width",
        "DisplayName": "All Detections[] / Bounding Box / Width",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.width",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.width"
      },
      {
        "Name": "AllDetections[].BoundingBox.Height",
        "DisplayName": "All Detections[] / Bounding Box / Height",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.height",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.height"
      },
      {
        "Name": "AllDetections[].BoundingBox.Left",
        "DisplayName": "All Detections[] / Bounding Box / Left",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.left",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.left"
      },
      {
        "Name": "AllDetections[].BoundingBox.Top",
        "DisplayName": "All Detections[] / Bounding Box / Top",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.top",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.top"
      },
      {
        "Name": "AllDetections[].BoundingBox.Right",
        "DisplayName": "All Detections[] / Bounding Box / Right",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.right",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.right"
      },
      {
        "Name": "AllDetections[].BoundingBox.Bottom",
        "DisplayName": "All Detections[] / Bounding Box / Bottom",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.bottom",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.bottom"
      },
      {
        "Name": "AllDetections[].BoundingBox.IsEmpty",
        "DisplayName": "All Detections[] / Bounding Box / Is Empty",
        "DataType": "Boolean",
        "Description": "Boolean",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.is_empty",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.is_empty"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location",
        "DisplayName": "All Detections[] / Bounding Box / Location",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.location"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location.X",
        "DisplayName": "All Detections[] / Bounding Box / Location / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.location.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Location.Y",
        "DisplayName": "All Detections[] / Bounding Box / Location / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.location.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.location.y"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center",
        "DisplayName": "All Detections[] / Bounding Box / Center",
        "DataType": "Point",
        "Description": "Point",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.center"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center.X",
        "DisplayName": "All Detections[] / Bounding Box / Center / X",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center.x",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.center.x"
      },
      {
        "Name": "AllDetections[].BoundingBox.Center.Y",
        "DisplayName": "All Detections[] / Bounding Box / Center / Y",
        "DataType": "Integer",
        "Description": "Integer",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.bounding_box.center.y",
        "EnumDisplayNames": null,
        "StableId": "all_detections.bounding_box.center.y"
      },
      {
        "Name": "AllDetections[].Confidence",
        "DisplayName": "All Detections[] / Confidence",
        "DataType": "Number",
        "Description": "Number",
        "IsNullable": false,
        "Example": null,
        "Cardinality": "Collection",
        "EnumTypeName": null,
        "EnumValues": null,
        "Id": "all_detections.confidence",
        "EnumDisplayNames": null,
        "StableId": "all_detections.confidence"
      }
    ],
    "PropertyTree": [
      {
        "Segment": "Found",
        "DisplayName": "Found",
        "Property": {
          "Name": "Found",
          "DisplayName": "Found",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "found",
          "EnumDisplayNames": null,
          "StableId": "found"
        },
        "Children": []
      },
      {
        "Segment": "Point",
        "DisplayName": "Point",
        "Property": {
          "Name": "Point",
          "DisplayName": "Point",
          "DataType": "Point",
          "Description": "Point, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "point",
          "EnumDisplayNames": null,
          "StableId": "point"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "Point.X",
              "DisplayName": "Point / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "point.x",
              "EnumDisplayNames": null,
              "StableId": "point.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "Point.Y",
              "DisplayName": "Point / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "point.y",
              "EnumDisplayNames": null,
              "StableId": "point.y"
            },
            "Children": []
          }
        ]
      },
      {
        "Segment": "BoundingBox",
        "DisplayName": "Bounding Box",
        "Property": {
          "Name": "BoundingBox",
          "DisplayName": "Bounding Box",
          "DataType": "Rectangle",
          "Description": "Rectangle, kann leer sein",
          "IsNullable": true,
          "Example": null,
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "bounding_box",
          "EnumDisplayNames": null,
          "StableId": "bounding_box"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "BoundingBox.X",
              "DisplayName": "Bounding Box / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.x",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "BoundingBox.Y",
              "DisplayName": "Bounding Box / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.y",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Width",
            "Property": {
              "Name": "BoundingBox.Width",
              "DisplayName": "Bounding Box / Width",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.width",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Height",
            "Property": {
              "Name": "BoundingBox.Height",
              "DisplayName": "Bounding Box / Height",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.height",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Left",
            "Property": {
              "Name": "BoundingBox.Left",
              "DisplayName": "Bounding Box / Left",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.left",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Top",
            "Property": {
              "Name": "BoundingBox.Top",
              "DisplayName": "Bounding Box / Top",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.top",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Right",
            "Property": {
              "Name": "BoundingBox.Right",
              "DisplayName": "Bounding Box / Right",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.right",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Bottom",
            "Property": {
              "Name": "BoundingBox.Bottom",
              "DisplayName": "Bounding Box / Bottom",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.bottom",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Is Empty",
            "Property": {
              "Name": "BoundingBox.IsEmpty",
              "DisplayName": "Bounding Box / Is Empty",
              "DataType": "Boolean",
              "Description": "Boolean",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.is_empty",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Location",
            "Property": {
              "Name": "BoundingBox.Location",
              "DisplayName": "Bounding Box / Location",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.location",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "BoundingBox.Location.X",
                  "DisplayName": "Bounding Box / Location / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.location.x",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "BoundingBox.Location.Y",
                  "DisplayName": "Bounding Box / Location / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.location.y",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "BoundingBox.Center",
              "DisplayName": "Bounding Box / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "bounding_box.center",
              "EnumDisplayNames": null,
              "StableId": "bounding_box.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "BoundingBox.Center.X",
                  "DisplayName": "Bounding Box / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "BoundingBox.Center.Y",
                  "DisplayName": "Bounding Box / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "bounding_box.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "bounding_box.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "Confidence",
        "DisplayName": "Confidence",
        "Property": {
          "Name": "Confidence",
          "DisplayName": "Confidence",
          "DataType": "Number",
          "Description": "Number",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "confidence",
          "EnumDisplayNames": null,
          "StableId": "confidence"
        },
        "Children": []
      },
      {
        "Segment": "SourceCaptureIsFresh",
        "DisplayName": "Source Capture Is Fresh",
        "Property": {
          "Name": "SourceCaptureIsFresh",
          "DisplayName": "Source Capture Is Fresh",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_capture_is_fresh",
          "EnumDisplayNames": null,
          "StableId": "source_capture_is_fresh"
        },
        "Children": []
      },
      {
        "Segment": "SourceCaptureTimestampUtc",
        "DisplayName": "Source Capture Timestamp Utc",
        "Property": {
          "Name": "SourceCaptureTimestampUtc",
          "DisplayName": "Source Capture Timestamp Utc",
          "DataType": "DateTime",
          "Description": "DateTime",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_capture_timestamp_utc",
          "EnumDisplayNames": null,
          "StableId": "source_capture_timestamp_utc"
        },
        "Children": []
      },
      {
        "Segment": "SourceFrameVersion",
        "DisplayName": "Source Frame Version",
        "Property": {
          "Name": "SourceFrameVersion",
          "DisplayName": "Source Frame Version",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_frame_version",
          "EnumDisplayNames": null,
          "StableId": "source_frame_version"
        },
        "Children": []
      },
      {
        "Segment": "SourceFrameTimestamp",
        "DisplayName": "Source Frame Timestamp",
        "Property": {
          "Name": "SourceFrameTimestamp",
          "DisplayName": "Source Frame Timestamp",
          "DataType": "Integer",
          "Description": "Integer",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "source_frame_timestamp",
          "EnumDisplayNames": null,
          "StableId": "source_frame_timestamp"
        },
        "Children": []
      },
      {
        "Segment": "AppliedRoi",
        "DisplayName": "Applied Roi",
        "Property": {
          "Name": "AppliedRoi",
          "DisplayName": "Applied Roi",
          "DataType": "Rectangle",
          "Description": "Rectangle, kann leer sein",
          "IsNullable": true,
          "Example": "{X=10,Y=20,Width=300,Height=200}",
          "Cardinality": "OptionalSingle",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "applied_roi",
          "EnumDisplayNames": null,
          "StableId": "applied_roi"
        },
        "Children": [
          {
            "Segment": "X",
            "DisplayName": "X",
            "Property": {
              "Name": "AppliedRoi.X",
              "DisplayName": "Applied Roi / X",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.x",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.x"
            },
            "Children": []
          },
          {
            "Segment": "Y",
            "DisplayName": "Y",
            "Property": {
              "Name": "AppliedRoi.Y",
              "DisplayName": "Applied Roi / Y",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.y",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.y"
            },
            "Children": []
          },
          {
            "Segment": "Width",
            "DisplayName": "Width",
            "Property": {
              "Name": "AppliedRoi.Width",
              "DisplayName": "Applied Roi / Width",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.width",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.width"
            },
            "Children": []
          },
          {
            "Segment": "Height",
            "DisplayName": "Height",
            "Property": {
              "Name": "AppliedRoi.Height",
              "DisplayName": "Applied Roi / Height",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.height",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.height"
            },
            "Children": []
          },
          {
            "Segment": "Left",
            "DisplayName": "Left",
            "Property": {
              "Name": "AppliedRoi.Left",
              "DisplayName": "Applied Roi / Left",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.left",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.left"
            },
            "Children": []
          },
          {
            "Segment": "Top",
            "DisplayName": "Top",
            "Property": {
              "Name": "AppliedRoi.Top",
              "DisplayName": "Applied Roi / Top",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.top",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.top"
            },
            "Children": []
          },
          {
            "Segment": "Right",
            "DisplayName": "Right",
            "Property": {
              "Name": "AppliedRoi.Right",
              "DisplayName": "Applied Roi / Right",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.right",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.right"
            },
            "Children": []
          },
          {
            "Segment": "Bottom",
            "DisplayName": "Bottom",
            "Property": {
              "Name": "AppliedRoi.Bottom",
              "DisplayName": "Applied Roi / Bottom",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.bottom",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.bottom"
            },
            "Children": []
          },
          {
            "Segment": "IsEmpty",
            "DisplayName": "Is Empty",
            "Property": {
              "Name": "AppliedRoi.IsEmpty",
              "DisplayName": "Applied Roi / Is Empty",
              "DataType": "Boolean",
              "Description": "Boolean",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.is_empty",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.is_empty"
            },
            "Children": []
          },
          {
            "Segment": "Location",
            "DisplayName": "Location",
            "Property": {
              "Name": "AppliedRoi.Location",
              "DisplayName": "Applied Roi / Location",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.location",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.location"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "AppliedRoi.Location.X",
                  "DisplayName": "Applied Roi / Location / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.location.x",
                  "EnumDisplayNames": null,
                  "StableId": "applied_roi.location.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "AppliedRoi.Location.Y",
                  "DisplayName": "Applied Roi / Location / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.location.y",
                  "EnumDisplayNames": null,
                  "StableId": "applied_roi.location.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "AppliedRoi.Center",
              "DisplayName": "Applied Roi / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "OptionalSingle",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "applied_roi.center",
              "EnumDisplayNames": null,
              "StableId": "applied_roi.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "AppliedRoi.Center.X",
                  "DisplayName": "Applied Roi / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "applied_roi.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "AppliedRoi.Center.Y",
                  "DisplayName": "Applied Roi / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "OptionalSingle",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "applied_roi.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "applied_roi.center.y"
                },
                "Children": []
              }
            ]
          }
        ]
      },
      {
        "Segment": "UsedDynamicRoi",
        "DisplayName": "Used Dynamic Roi",
        "Property": {
          "Name": "UsedDynamicRoi",
          "DisplayName": "Used Dynamic Roi",
          "DataType": "Boolean",
          "Description": "Boolean",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Single",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "used_dynamic_roi",
          "EnumDisplayNames": null,
          "StableId": "used_dynamic_roi"
        },
        "Children": []
      },
      {
        "Segment": "AllDetections",
        "DisplayName": "All Detections",
        "Property": {
          "Name": "AllDetections",
          "DisplayName": "All Detections",
          "DataType": "Detection",
          "Description": "Detection",
          "IsNullable": false,
          "Example": null,
          "Cardinality": "Collection",
          "EnumTypeName": null,
          "EnumValues": null,
          "Id": "all_detections",
          "EnumDisplayNames": null,
          "StableId": "all_detections"
        },
        "Children": [
          {
            "Segment": "Count",
            "DisplayName": "Count",
            "Property": {
              "Name": "AllDetections.Count",
              "DisplayName": "All Detections / Count",
              "DataType": "Integer",
              "Description": "Integer",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Single",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.count",
              "EnumDisplayNames": null,
              "StableId": "all_detections.count"
            },
            "Children": []
          },
          {
            "Segment": "Center",
            "DisplayName": "Center",
            "Property": {
              "Name": "AllDetections[].Center",
              "DisplayName": "All Detections[] / Center",
              "DataType": "Point",
              "Description": "Point",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.center",
              "EnumDisplayNames": null,
              "StableId": "all_detections.center"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "AllDetections[].Center.X",
                  "DisplayName": "All Detections[] / Center / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.center.x",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.center.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "AllDetections[].Center.Y",
                  "DisplayName": "All Detections[] / Center / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.center.y",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.center.y"
                },
                "Children": []
              }
            ]
          },
          {
            "Segment": "BoundingBox",
            "DisplayName": "Bounding Box",
            "Property": {
              "Name": "AllDetections[].BoundingBox",
              "DisplayName": "All Detections[] / Bounding Box",
              "DataType": "Rectangle",
              "Description": "Rectangle, kann leer sein",
              "IsNullable": true,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.bounding_box",
              "EnumDisplayNames": null,
              "StableId": "all_detections.bounding_box"
            },
            "Children": [
              {
                "Segment": "X",
                "DisplayName": "X",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.X",
                  "DisplayName": "All Detections[] / Bounding Box / X",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.x",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.x"
                },
                "Children": []
              },
              {
                "Segment": "Y",
                "DisplayName": "Y",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Y",
                  "DisplayName": "All Detections[] / Bounding Box / Y",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.y",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.y"
                },
                "Children": []
              },
              {
                "Segment": "Width",
                "DisplayName": "Width",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Width",
                  "DisplayName": "All Detections[] / Bounding Box / Width",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.width",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.width"
                },
                "Children": []
              },
              {
                "Segment": "Height",
                "DisplayName": "Height",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Height",
                  "DisplayName": "All Detections[] / Bounding Box / Height",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.height",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.height"
                },
                "Children": []
              },
              {
                "Segment": "Left",
                "DisplayName": "Left",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Left",
                  "DisplayName": "All Detections[] / Bounding Box / Left",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.left",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.left"
                },
                "Children": []
              },
              {
                "Segment": "Top",
                "DisplayName": "Top",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Top",
                  "DisplayName": "All Detections[] / Bounding Box / Top",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.top",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.top"
                },
                "Children": []
              },
              {
                "Segment": "Right",
                "DisplayName": "Right",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Right",
                  "DisplayName": "All Detections[] / Bounding Box / Right",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.right",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.right"
                },
                "Children": []
              },
              {
                "Segment": "Bottom",
                "DisplayName": "Bottom",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Bottom",
                  "DisplayName": "All Detections[] / Bounding Box / Bottom",
                  "DataType": "Integer",
                  "Description": "Integer",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.bottom",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.bottom"
                },
                "Children": []
              },
              {
                "Segment": "IsEmpty",
                "DisplayName": "Is Empty",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.IsEmpty",
                  "DisplayName": "All Detections[] / Bounding Box / Is Empty",
                  "DataType": "Boolean",
                  "Description": "Boolean",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.is_empty",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.is_empty"
                },
                "Children": []
              },
              {
                "Segment": "Location",
                "DisplayName": "Location",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Location",
                  "DisplayName": "All Detections[] / Bounding Box / Location",
                  "DataType": "Point",
                  "Description": "Point",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.location",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.location"
                },
                "Children": [
                  {
                    "Segment": "X",
                    "DisplayName": "X",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Location.X",
                      "DisplayName": "All Detections[] / Bounding Box / Location / X",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.location.x",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.location.x"
                    },
                    "Children": []
                  },
                  {
                    "Segment": "Y",
                    "DisplayName": "Y",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Location.Y",
                      "DisplayName": "All Detections[] / Bounding Box / Location / Y",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.location.y",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.location.y"
                    },
                    "Children": []
                  }
                ]
              },
              {
                "Segment": "Center",
                "DisplayName": "Center",
                "Property": {
                  "Name": "AllDetections[].BoundingBox.Center",
                  "DisplayName": "All Detections[] / Bounding Box / Center",
                  "DataType": "Point",
                  "Description": "Point",
                  "IsNullable": false,
                  "Example": null,
                  "Cardinality": "Collection",
                  "EnumTypeName": null,
                  "EnumValues": null,
                  "Id": "all_detections.bounding_box.center",
                  "EnumDisplayNames": null,
                  "StableId": "all_detections.bounding_box.center"
                },
                "Children": [
                  {
                    "Segment": "X",
                    "DisplayName": "X",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Center.X",
                      "DisplayName": "All Detections[] / Bounding Box / Center / X",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.center.x",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.center.x"
                    },
                    "Children": []
                  },
                  {
                    "Segment": "Y",
                    "DisplayName": "Y",
                    "Property": {
                      "Name": "AllDetections[].BoundingBox.Center.Y",
                      "DisplayName": "All Detections[] / Bounding Box / Center / Y",
                      "DataType": "Integer",
                      "Description": "Integer",
                      "IsNullable": false,
                      "Example": null,
                      "Cardinality": "Collection",
                      "EnumTypeName": null,
                      "EnumValues": null,
                      "Id": "all_detections.bounding_box.center.y",
                      "EnumDisplayNames": null,
                      "StableId": "all_detections.bounding_box.center.y"
                    },
                    "Children": []
                  }
                ]
              }
            ]
          },
          {
            "Segment": "Confidence",
            "DisplayName": "Confidence",
            "Property": {
              "Name": "AllDetections[].Confidence",
              "DisplayName": "All Detections[] / Confidence",
              "DataType": "Number",
              "Description": "Number",
              "IsNullable": false,
              "Example": null,
              "Cardinality": "Collection",
              "EnumTypeName": null,
              "EnumValues": null,
              "Id": "all_detections.confidence",
              "EnumDisplayNames": null,
              "StableId": "all_detections.confidence"
            },
            "Children": []
          }
        ]
      }
    ]
  }
}
```

