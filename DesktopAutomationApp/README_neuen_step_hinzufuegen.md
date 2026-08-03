# Neuen Job-Step im Frontend bereitstellen

Job-Steps werden nicht mehr mit einem eigenen XAML-Editor, `ShowXxx`-Properties,
einem Fabrik-Switch oder einem `Prefill`-Fall in der Desktop-Anwendung eingebaut.
Die fachliche Beschreibung eines Steps liegt vollständig in `TaskAutomation`.

Die verbindliche Anleitung steht in:

- `TaskAutomation/Steps/ADDING_A_JOB_STEP.md`
- `TaskAutomation/Steps/RESULT_CONTRACTS.md`

## Verantwortlichkeiten

`TaskAutomation` definiert pro Step:

- stabile Feld-IDs, Datentypen und Standardwerte;
- Auswahlwerte und Sichtbarkeitsregeln;
- Validierung, Zusammenfassung und Detaildarstellung;
- Eingabe- und Ergebnisverträge;
- optionale, frontend-neutrale `EditorHint`-Werte.

`DesktopAutomationApp` stellt den Vertrag mit dem gemeinsamen
`GeneratedStepEditor` dar. Frontend-spezifische Komfortfunktionen wie Datei-,
Monitor-, ROI-, Kamera- oder Ergebnis-Picker werden als wiederverwendbare Adapter
für einen `EditorHint` implementiert. Ein Adapter darf keine fachliche
Step-Konfiguration duplizieren.

## Wann ist eine Frontend-Änderung nötig?

Für einen neuen Step mit vorhandenen Feldtypen und `EditorHint`-Werten ist keine
Frontend-Änderung erforderlich. Wird eine neue Art von Auswahlhilfe benötigt:

1. neutralen `EditorHint` und portable Optionsdaten in
   `TaskAutomation.Contracts` ergänzen;
2. einen wiederverwendbaren Adapter im generierten Desktop-Editor implementieren;
3. keine Abhängigkeit von WPF oder `DesktopAutomationApp` in `TaskAutomation`
   einführen;
4. Darstellung, Bearbeitung, Validierung und Roundtrip mit Tests abdecken.

Die Step-Auswahl, das Erstellen und das Bearbeiten werden automatisch über
`BuiltInStepDefinitions` und `IStepDefinitionCatalog` angebunden.
