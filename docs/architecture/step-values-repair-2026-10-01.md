# Reparaturen zum Step-Werte-Audit

Stand: 2026-10-01. Dieser Nachtrag dokumentiert die Reparaturen zum
[ursprünglichen Audit](step-values-audit-2026-10-01.md). Der Bericht, der Feldkatalog und die
ursprünglichen Reproduktionsdaten bleiben als Ausgangsbefund erhalten.

## Behobene Befunde und Nachweise

| Befund | Reparatur | Regressionen |
| --- | --- | --- |
| STEP-AUDIT-001 | `ValueReferenceUsageInspector` folgt referenzierten gespeicherten JSON-Werten, erkennt Conditions und schreibt geänderte Referenzen in das ursprüngliche JSON zurück. Zyklische Wertegraphen bleiben begrenzt. | `StepValueGraphTests`: Embedded-Condition-Referenzen, Rückschreiben und Zyklen. |
| STEP-AUDIT-002 | `JobStepsSnapshotService` kopiert einen Graph aus Steps und lokalen Werten. Interne Step-IDs, lokale IDs, Besitzer und dynamische Enum-Identitäten werden gemeinsam angepasst. Externe Jobvariablen bleiben geteilt. | `StepValueGraphTests`; bestehende UI-Duplizierungstests. |
| STEP-AUDIT-003 | `JobValueSources` besitzt die Provider-/ID-Zuordnung. Backend, Migration, Picker, Dialog und Condition-Projektionen unterscheiden JobVariable und LocalValue auch bei identischen GUIDs. Nutzungszahlen verwenden dieselbe Identität. | `StepValuesAuditTests.StoredSourceIdentityIncludesItsProvider`; `StoredValueRepairTests.SourcePickerKeepsEqualIdsFromDifferentProvidersIndependent`. |
| STEP-AUDIT-004 | Fehlende oder doppelte IDs innerhalb eines Providers, doppelte Step-IDs sowie widersprüchliche vorhandene Besitzer-/Pfadangaben werden erkannt. Runtime-Stores akzeptieren keine doppeldeutigen IDs. Fehlende historische Besitzangaben bleiben kompatibel. | Identitäts- und Besitztests in `StepValuesAuditTests`. |
| STEP-AUDIT-005 | `StepEditorActivity` besitzt die Aktivitäts-/Sichtbarkeitsregel. Descriptor-Validierung, Binding-Leser, Authoring und Runtime berücksichtigen sie. Inaktive fehlerhafte Werte werden nicht eingesetzt, gültige Werte bleiben für einen späteren Moduswechsel erhalten. | `InactiveInvalidEnumDoesNotPreventMinimizeButRemainsPersisted`; bestehende Moduswechseltests. |
| STEP-AUDIT-006 | Antwortlisten prüfen Parse-Erfolg, Elementform, IDs, Labels, Eindeutigkeit und Grenzen. Spezialisierte Objekt- und Collection-Felder prüfen ihre deserialisierbare Form. Beschädigte dynamische Verträge verhindern nicht das Öffnen der Jobdatei. | UserChoice-/Objektform-Matrix in `StepValuesAuditTests`; bestehende 2-/18-Antwort- und Eindeutigkeitstests. |
| STEP-AUDIT-007 | `StepInputValueResolver` setzt Werte für Authoring und Runtime nach denselben vorhandenen Pfad- und Zielschemaregeln ein. Provider-only Items werden geprüft; Schema-IDs werden nicht still überschrieben. Root- und Member-Verträge prüfen Typ und Provider auch zur Laufzeit. Die Text-Overlay-Liste trägt Collection-Kardinalität. | Item-Override-, Schema-, Provider- und Metadatenregressionen. |
| STEP-AUDIT-008 | Point-, Axis- und Camera-Editoren erhalten unbekannte Tokens, zeigen einen Fehler und wählen keinen Ersatz. Erst eine explizite Auswahl repariert den Token. | Token-Matrix in `StoredValueRepairTests`. |
| STEP-AUDIT-009 | Der Variableneditor verändert ungültige Werte beim Öffnen nicht. `JobVariableValueRules` besitzt Formprüfung und explizite Defaults. Ein eigener Befehl ersetzt beschädigte Werte; Collection-Defaults sind Arrays. Beschädigte Editor-Collections bleiben bis zur Bearbeitung erhalten. | Variablen-, Default- und Collection-Tests in `StoredValueRepairTests`. |
| STEP-AUDIT-010 | EndJob-Entscheidungen verwenden materialisierte Eingaben in Start- und Hauptphase. Video startet beim ersten brauchbaren Frame mit aufgelösten Dateiwerten und Bildmaßen. YOLO-Vorladen verwendet aufgelöste Modelle und Entladen die tatsächlich erfolgreich geladenen Namen. | EndJob-Roundtrip-Matrix; `StepResourceInputTests` mit gefälschtem Recorder und YOLO-Manager, einschließlich Abbruch und Startfehler. |
| STEP-AUDIT-011 | `ResultValueShape` vereinheitlicht die Typ-/Kardinalitätsableitung. Primitive Collections sind lesbar; JSON-Arrays und deren Elemente werden auch vom typisierten Resolver korrekt gelesen. | Collection-Matrix und JSON-Array-Property-Test in `StepValuesAuditTests`. |
| STEP-AUDIT-012 | `ValueReferenceResolver` ist der gemeinsame Resolver für generische, typisierte, Provider- und Condition-Zugriffe. Der Result-Store kennt konfigurierte dynamische Verträge. Enum-Identität, Optionen und Beschriftungen bleiben erhalten. | Dynamische Enum-Metadaten, erlaubte Quellen und bestehende `ResultBindingResolverTests`. |
| STEP-AUDIT-013 | Toter Legacy-Validator, UI-Clone-Traversierung und eigener Reflection-Bereinigungspfad wurden entfernt. Kontextabhängige Kurz-APIs delegieren mit den vorhandenen Werten an die aktuelle Validierung. Bereinigung verwendet den vollständigen Referenzgraphen. | Kontext-API-Test; Bereinigung eingebetteter Conditions und Erhalt deaktivierter Quellen. |

Zusätzlich wurde eine weitere konkurrierende Wertregel beseitigt: `ColorValueRules` besitzt die
Farbnotationen für UI, Validierung und Rendering. Ungültige Fontfarben bleiben beim Öffnen
erhalten; Kurznotationen und ARGB verwenden denselben Parser. Fehlerhafte Overlay-Listen und
null-Elemente werden als Datenfehler gemeldet.

Erlaubte Jobvariablen bleiben auch mit historischem `StepValue`-Scope als Quellen zulässig;
Authoring und Runtime verwenden dafür denselben Quellenvertrag. Gemischte Integer-/Number-
Arrays verlieren beim typisierten Number-Zugriff keine ganzzahligen Einträge. Collections mit
unpassenden Elementtypen liefern einen Typfehler statt eines scheinbar erfolgreichen Teilergebnisses.

Die vollständige End-to-End-Prüfung deckte zudem eine Kompatibilitätskante auf: Neue Quellen-
auswahlen schließen Secrets weiterhin aus, vorhandene Text-Secret-Referenzen dürfen aber nicht
durch diese Einschränkung unlesbar werden. Der Quellenvertrag beschreibt diese Legacy-Ausnahme
explizit; Editor und Runtime erhalten sie gemeinsam. Der Editor zeigt eine maskierte Vorschau.
`PersistedSecretTextRemainsReadableWithoutEnablingNewSecretSelections`,
`ExistingSecretSelectionIsPreservedAndMaskedWithoutEnablingNewSecretSources` und der bestehende
Secret-End-to-End-Test sichern das Verhalten ab.

Die verbleibenden Adapter haben unterschiedliche Aufgaben: Authoring kennt gespeicherte Werte
und Metadaten, Runtime besitzt ausgeführte Ergebnisse und Ressourcen, WPF projiziert Controls.
Sie delegieren die gemeinsamen Regeln; die verschiedenen Phasen benötigen weiterhin unterschiedliche
Fehlerdarstellung und Verfügbarkeit von Werten.

## Offene Produktentscheidungen

Die [vorgeschlagenen Richtlinien](../decisions/2026-10-01-choose-step-value-and-resource-policies.md)
beschreiben sechs echte Wahlmöglichkeiten mit der aktuell eingesetzten Annahme: inaktive Werte,
fehlende strukturierte Pfade, Collection-Editoren, mehrere Video-Steps, YOLO-Ladezeitpunkt und
Secrets bei neuen Texteingaben.
Diese Fragen sind keine verbleibenden Audit-Fehler und erfordern keine Entscheidung über
Provider-Identität, ungültige Tokens oder stilles Überschreiben gespeicherter Werte.

## Verifikation

Die vollständige Prüfung erfolgt über `eng/verify.ps1 -Mode Full`. Der abschließende Handoff
nennt Exit-Code und Testzahlen. Hardware, echte Kamera-/Monitoraufnahmen, FFmpeg-Download und
echte YOLO-Modell-Downloads werden für diese Ressourcenregressionen durch kontrollierte Fakes
ersetzt; sie sind damit keine manuell durchgeführten Hardwaretests.
