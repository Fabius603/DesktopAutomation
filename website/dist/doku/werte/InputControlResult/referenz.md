## Werte und Ergebnisse: InputControlResult

ID: InputControlResult
Website: /doku/werte/InputControlResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag InputControlResult. Wird von Eingaben blockieren, Eingaben freigeben geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "InputControlResult",
  "name": "InputControlResult",
  "result": {
    "TypeName": "InputControlResult",
    "DisplayName": "InputControlResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

