## Werte und Ergebnisse: MakroExecutionResult

ID: MakroExecutionResult
Website: /doku/werte/MakroExecutionResult/

### errors

Ein konfigurierte Quelle hat noch nicht zwingend einen Wert geliefert. Prüfe den fachlichen Erfolgswert, fehlende optionale Eigenschaften und den Ausführungszustand des Produzenten. Nur DynamicRoi besitzt die dokumentierte Ausnahme für späteres Feedback; andere Verbraucher dürfen keine beliebigen Vorwärtsreferenzen verwenden.

### example

Eine Ergebnisreferenz verwendet provider_id=step_result und source_id=v1/<kodierte-Step-ID>/<kodierte-Ergebnis-ID>. Die Anleitung „Werte verbinden“ zeigt eine vollständige Verbindung. Prüfe Typ und Kardinalität vor der Verwendung in einer Bedingung oder Folgeaktion.

### purpose

Typisierter Ergebnisvertrag MakroExecutionResult. Wird von Makro ausführen geliefert. Die stabilen Ergebnis-IDs unten sind unabhängig von CLR-Namen und UI-Übersetzungen. Wähle im Ergebnis-Auswahldialog eine kompatible Eigenschaft statt das ganze Objekt ungeprüft als Text zu verwenden.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "MakroExecutionResult",
  "name": "MakroExecutionResult",
  "result": {
    "TypeName": "MakroExecutionResult",
    "DisplayName": "MakroExecutionResult",
    "Properties": [],
    "PropertyTree": []
  }
}
```

