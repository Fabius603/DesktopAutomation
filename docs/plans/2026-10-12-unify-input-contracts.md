# Implementierungsplan: Eingabeverträge vereinheitlichen

Stand: 2026-10-12. Status: umgesetzt und vollständig geprüft.
Die Umsetzung und die vollständige Entfernung der Übergangsadapter sind in der
[akzeptierten Entscheidung](../decisions/2026-10-12-remove-legacy-input-contract-adapters.md)
festgehalten. Die folgenden Arbeitspakete dokumentieren den durchgeführten Umbau.

## Umsetzungsnachweis

- Gemeinsame transportierbare Vertragsknoten, Schemata und Wertmetadaten liegen in
  TaskAutomation.Contracts. InputContractRegistry besitzt die Root-/Memberdefinitionen
  und die Feld-/Pfadauflösung; fachliche Schema-/Spezialtypzuordnungen sind explizit.
- InputCompatibilityEvaluator, InputValueConverter und InputBindingContractTraversal
  werden von Auswahl, Validierung und Materialisierung verwendet. Der Bedingungseditor
  delegiert seine Quellenauswahl ebenfalls an ConditionRules im Backend.
- Die alten Descriptor- und Registrytypen wurden in der anschließenden Bereinigung
  vollständig gelöscht. Definitionen, Aufrufer und Tests verwenden direkt das neue
  Modell. Eine Architekturprüfung auf kompilierten Typen verhindert ihre Wiedereinführung.
  Die veröffentlichten JSON-Referenzschlüssel bleiben durch reine Exportabbildungen erhalten.
- Number verwendet den gemeinsamen Reader JsonNumberRules; seine bestehende double-
  Laufzeitdarstellung und die getrennten Integer-Grenzen bleiben erhalten. Gespeicherte
  Jobs, IDs und das Bindungsformat benötigen keine Migration.
- Die neue versionierte inputContracts-Referenz ergänzt bestehende bindingSchemas und
  fieldSchemas. Architektur, Step-Anleitung sowie UI-/JSON-Produktreferenzen wurden
  aktualisiert; 59 betroffene Dokumentationsaufnahmen wurden gerendert und geprüft.
- `eng/verify.ps1 -Mode Full` ist nach vollständiger Adapterentfernung erfolgreich:
  893 Unit-, 127 Vertrags-, 9 Architektur-, 159 Integrations-, 302 UI- und
  27 End-to-End-Tests; zusammen 1.517 Tests ohne Fehler
  oder übersprungene Fälle. Build, Formatierung, Lokalisierung, Release Notes und strenge
  Websiteprüfungen bestanden ebenfalls. Die bestehenden Paketkompatibilitätswarnungen
  bei älteren Abhängigkeiten bleiben bestehen.

## Ziel und verbindliche Invarianten

Jeder Eingabepfad besitzt genau einen fachlichen Vertrag. Editor, Jobvalidierung,
Import und Ausführung verwenden diesen Vertrag und dieselbe Kompatibilitätsprüfung.
Das gilt für normale Felder, Objektmitglieder und Listenelemente.

- Eine neu auswählbare Quelle muss anhand ihrer Metadaten auch durch die
  Jobvalidierung zugelassen werden. Unbekannte Laufzeitwerte werden dabei nicht als
  bereits gültig behauptet.
- Eine zugelassene Umwandlung wird bei bekannten Editorwerten und zur Laufzeit
  mit derselben Implementierung ausgeführt.
- OptionalSingle als Quellform ist keine Erlaubnis, einen benötigten Wert auszulassen.
- Neue Verbindungen und bestehende Altverbindungen haben ausdrücklich verschiedene
  Zulassungsprofile. Altverbindungen werden nicht automatisch neu angeboten.
- Die Möglichkeit, eine Variable zu erstellen oder direkt zu bearbeiten, bestimmt
  nicht, ob eine vorhandene Variable verwendbar ist.
- Stabile Step-, Feld-, Member-, Schema-, Provider- und Source-IDs sowie das bestehende
  ResultBinding-Speicherformat bleiben erhalten. Der Umbau benötigt keine Jobmigration.
- Sichtbarkeit, bedingte Felder, fehlende Quellen, Skip-Verhalten, ROI-Rückkopplung,
  Ergebniseigenschaften und Kulturverhalten behalten ihre bestehenden Regeln.
- Geheimnisse werden nicht für eine Auswahlprüfung geladen und erscheinen nicht in
  Diagnosen oder Exporten.

Grundlagen: [Architektur](../architecture/overview.md),
[aktuelle Verträge](../architecture/result-contracts.md),
[gemeinsame Quellkompatibilität](../decisions/2026-10-12-share-field-and-nested-source-compatibility.md)
und [Textumwandlung](../decisions/2026-10-12-convert-displayable-values-for-text-inputs.md).

## Zielstruktur und Zuständigkeiten

Die folgenden Typnamen sind Vorschläge. Bestehende abstrahierende Typen werden vor
dem Hinzufügen erweitert, wenn sie diese Aufgaben ohne doppelte Semantik übernehmen können.

| Baustein | Aufgabe | Besitzer |
| --- | --- | --- |
| InputContractNode | Unveränderliche Daten für Zieltyp, Schema, Quellen, Umwandlung, Kardinalität, Fehlwertverhalten und Constraints; Member- und Itemverträge | TaskAutomation.Contracts |
| InputContractRegistry | Definition und Auflösung des Vertrags für einen konfigurierten Step und Eingabepfad | TaskAutomation |
| InputCompatibilityEvaluator | Metadatenprüfung mit explizitem Profil NewSelection oder ExistingBinding | TaskAutomation |
| InputCompatibilityResult | Ergebnis, Diagnosecode und erforderlicher ConversionPlan | TaskAutomation.Contracts |
| InputValueConverter | Ausführung der im Plan ausdrücklich freigegebenen Umwandlungen | TaskAutomation |
| StepInputValueResolver / StepInputMaterializer | Quellauflösung, Zusammensetzung und Laufzeitprüfung mit dem kanonischen Vertrag | TaskAutomation |
| StepFieldDescriptor / WPF-Adapter | Präsentation, Beschriftung, Editorwahl und Projektion der Backenddiagnosen | Contracts / DesktopAutomationApp |

Transportierbare Verträge enthalten keine Delegates, WPF-Typen oder Backendabhängigkeiten.
Vor dem Verschieben von ResultValueKind und ResultCardinality wird ihre Abhängigkeitskette
geprüft; vorhandene Typen werden wiederverwendet, keine parallelen Typen eingeführt.
Der Registry-Baum verwendet stabile Schemareferenzen und einen kanonischen Pfadresolver.
Schemaauflösung und unzulässige Zyklen werden beim Registry-Aufbau geprüft.

Ein Ergebnis unterscheidet mindestens AcceptedExact, AcceptedConverted,
AcceptedLegacy und Rejected. AcceptedLegacy enthält gegebenenfalls ebenfalls einen
ConversionPlan. Fehler enthalten Pfad und stabilen Diagnosecode, keine Quellwerte.
NewSelection/ExistingBinding sind unabhängig von Authoring/Runtime: Eine bestehende
Altverbindung bleibt auch zur Laufzeit im Kompatibilitätsprofil ExistingBinding.
Fehlende Werte und Ausführungsentscheidungen gehören nicht in die Metadatenprüfung.

## Arbeitspakete und Reihenfolge

### 1. Bestehendes Verhalten inventarisieren und absichern

Betroffene Besitzer und Aufrufer erfassen:

- StepInputContractRegistry, StepFieldDescriptor und die Stepdefinitionen.
- ValueBindingSchemaRegistry und ValueBindingMemberDescriptor in ValueBindingTree.cs.
- JobVariableInputMigration, JobVariableValueRules und Provider-Metadaten.
- JobValidation, StepInputValueResolver und StepInputMaterializer.
- ConditionRules und JobExecutor für Vergleichswerte.
- AddJobStepDialogViewModel, ValueReferencePickerViewModel und generierte Unterfeldeditoren.
- Website-Exporter sowie UI- und JSON-Vertragsreferenzen.

Für jede Feldfamilie Quellpolitik, aktuelle/alte Shapes, Conversion, Constraints,
Fehlwert- und Listenverhalten tabellarisch aufnehmen. Auch Pfade, geschlossene/dynamische
Enums, reine Direktwerte, Prozessziele, ROI, YOLO, Punktlisten und Overlaylisten abdecken.
Bestehende Regressionstests wiederverwenden; fehlende Verhaltenstests ergänzen.
Keine neuen Tests auf private Methoden oder die genaue Aufrufreihenfolge ausrichten.

**Abnahme:** Vollständige Verbraucher- und Vertragsmatrix sowie repräsentative
gespeicherte Fixtures. Unbeabsichtigte Verhaltensänderungen sind von geplanten Änderungen
unterscheidbar. Für die Architekturentscheidung einen vorgeschlagenen ADR erstellen;
bestehende Entscheidungen nicht vorzeitig als abgelöst markieren.

### 2. Gemeinsames Modell und kanonische Registry einführen

InputContractNode für Root, Member und Item definieren. Fachliche Eigenschaften von
EditorHint und Darstellungsbreite entkoppeln; Schema- und Vertrag-IDs explizit in
Stepdefinitionen verknüpfen. Der Registry-Resolver berücksichtigt die konfigurierte
Stepvariante, insbesondere dynamische Enums und bedingte Eingaben.

Die bisherige Definitionsmenge pro migrierter Feldfamilie in die kanonische Registry
überführen. Übergangsadapter dürfen nur während der Umstellung existieren und werden
nach Migration der Aufrufer gelöscht. Noch nicht migrierte Familien
haben einen dokumentierten Besitzer und werden nicht gleichzeitig neu definiert.

**Abnahme:** Registry-Checks erkennen unbekannte Schemata, doppelte IDs, unzulässige
Zyklen, fehlende Member-/Itemverträge und nicht unterstützte Conversion-IDs.
Exportdaten bleiben deterministisch. Kein Rückverweis von Contracts auf TaskAutomation.

### 3. Kompatibilitätsprüfung und ConversionPlan zentralisieren

Provider, Quelltyp, Quellkardinalität, Enumidentität und Legacyprofil gemeinsam prüfen.
Bool-Parameter wie includeLegacy/legacyDirect durch benannte Profile und normalisierte
Referenzmetadaten ersetzen. Legacyreferenzen über vorhandene Migration/Resolver
interpretieren, ohne eine zweite Zulassungsregel im Adapter aufzubauen.

Umwandlungen ausdrücklich registrieren: Exact, DisplayText und vorhandene
LegacyColor-/LegacyFilePath-Regeln. TextValueFormatter bleibt der Besitzer der
Textdarstellung. Keine pauschale Konvertierung auf Grundlage eines Text-EditorHints.
DirectoryPath und andere spezielle Textziele behalten ihre eigenen Constraints.
FirstValue/AllValues und optionale Quellen sind ausdrücklich Teil des Vertragsplans.

**Abnahme:** Datengetriebene Matrix über Typ, Kardinalität, Provider und Profil.
Unzulässige Kombinationen haben verständliche Diagnosecodes. Zulässige ConversionPlans
können tatsächlich ausgeführt werden; fehlende Werte werden gesondert behandelt.

### 4. Validierung, Materialisierung und Zahlen vereinheitlichen

JobValidation und StepInputMaterializer auf den Pfadresolver und Evaluator umstellen.
StepInputValueResolver verwendet den ConversionPlan statt eigener Zieltypableitung.
Gemeinsame Baumtraversierung für Schema-/Pfadauflösung nutzen; Authoring darf Werte
unaufgelöst lassen, Runtime prüft den tatsächlich vorhandenen Inhalt.
Objektbasiswerte, Memberüberschreibungen, Itemgrenzen und Legacy-Dotted-Keys erhalten
ihre bestehende Zusammensetzungssemantik.

Zahlen beim bestehenden gemeinsamen Reader normalisieren: float, double, decimal und
Ganzzahlen vor/nach JSON-Roundtrip gleich behandeln, NaN/Infinity ablehnen. Eine interne
Repräsentation erst nach Prüfung von Präzision, Wertebereichen und bestehenden Integer-
Constraints festlegen. Keine pauschale Konvertierung aller Zahlen zu double und keine
Änderung gespeicherter Zahlenwerte. Ein vollständiges neues Runtime-Wertsystem ist für
diesen Umbau nicht Voraussetzung.

ConditionRules liefert Vergleichsverträge aus demselben Modell und verwendet dieselben
Conversions. Operatorwahl und die eigentliche Vergleichssemantik bleiben dort verankert.

**Abnahme:** Bekannte Werte liefern bei Authoring/Runtime dieselben Inhalte und
Constraint-Ergebnisse. Fehlende/skipped Quellen, null/leere Listen, große Ganzzahlen,
Präzision und Kultur werden gezielt geprüft; ROI-Rückkopplung bleibt unverändert.

### 5. Editor auf Vertragsauflösung umstellen

ResolveNestedInputReference fragt den Vertrag über Step und vollständigen Eingabepfad
ab. Entfernen der selbst erzeugten fachlichen StepFieldDescriptors und lokalen
Provider-/Enum-Sonderregeln; diese kommen aus dem Backendvertrag.
ValueReferencePickerViewModel nutzt NewSelection für neue Quellen und ExistingBinding
für das Anzeigen/Laden einer gespeicherten Verbindung. SupportedKinds bleibt auf
Variablenerstellung und direkte Bearbeitung beschränkt.

Backenddiagnosen in vorhandene bzw. neue deutsche und englische Ressourcen projizieren.
Nicht verfügbare Referenzen und sensible Quellen behalten ihre Darstellungsregeln.

**Abnahme:** UI-Tests decken Root-/Member-/Itemauswahl, bestehende Altverbindungen,
optionale Quellen, Variablenerstellung und dynamische Enums ab. Die UI entscheidet
nicht eigenständig über erlaubte Quelltypen oder Provider.

### 6. Alle Familien migrieren, Adapter entfernen und dokumentieren

Mit normalen skalaren Feldern und Overlaytext beginnen, dann Prozessziel/ROI/
Punktlisten/YOLO/Auswahllisten und spezielle Referenzfelder übernehmen. Jede Familie
durchläuft Pakete 2 bis 5 vollständig, bevor ihre alte Definition entfernt wird.
Ein Zwischenstand darf Adapter enthalten, aber keine zwei aktiven Regelbesitzer.

Website-Exporter auf das kanonische Modell umstellen. Öffentliche JSON-Referenzschlüssel
und URLs erhalten; nötige Exportänderungen ausdrücklich versionieren oder kompatibel
ergänzen. Architekturtexte, UI-Anleitung und JSON-/Agentreferenzen gemeinsam prüfen.
Betroffene Screenshots gemäß Katalog selektiv rendern und visuell prüfen. Release Notes
nur für tatsächlich sichtbare Verbesserungen oder geänderte Fehlermeldungen ergänzen.

Zum Schluss die bisherigen Typen/ForField-Inferenzen und konkurrierende Prüfpfade
vollständig entfernen. Erneute Suche nach
fachlichen Entscheidungen in Frontends. ADR nach Annahme aktualisieren und nur dann
überholte Entscheidungen entsprechend verknüpfen.

**Abnahme:** Eine neue Feldfamilie braucht keine zusätzliche Kompatibilitätsregel im
Editor, Validator oder Materializer. Registrierungs- und Verhaltenstests reichen aus,
um ihre Quellenpolitik nachzuweisen. Die alten C#-Vertragsmodelle und Registryfassaden
existieren nicht mehr; Kompatibilität gespeicherter Jobs bleibt im Einlesepfad erhalten.

## Validierung und Abschlusskriterien

Pro Arbeitspaket betroffene Projekte bauen und passende Tests ausführen. Die Kernmatrix
liegt in Unit-/Contracttests; Persistenzfixtures prüfen stabile IDs und semantische
Roundtrips. Architekturtests prüfen Abhängigkeitsrichtung und strukturell prüfbare
Regeln. UI-Tests prüfen Interaktion, Integrationstests die gemeinsame Auflösung bis
zur Materialisierung; wenige End-to-End-Fälle prüfen kritische Benutzerabläufe.

Mindestens folgende Regressionen bleiben dauerhaft abgedeckt:

1. Lokalen Overlaytext hinzufügen und gültig ausführen.
2. Zahlen, Bool-Werte, strukturierte Werte und Listen in Textfeldern darstellen.
3. OptionalSingle auswählen, aber fehlenden Pflichtwert zur Laufzeit korrekt behandeln.
4. Alte Text-Farb-/Pfadverbindungen laden, neue unzulässige Textquellen ablehnen.
5. Ganze strukturierte Variablen ohne direkten Variableneditor auswählen.
6. Numerische Defaults und Referenzwerte vor/nach Speichern gleich verarbeiten.
7. Verschachtelte Basiswerte überschreiben und ungültige Member/Indizes zurückweisen.
8. Dynamische Enumidentitäten, reine Direktwerte und sensible Provider korrekt behandeln.

Für Dokumentation: `eng/verify.ps1 -Mode Focused -Checks repository,documentation,skills`.
Nach Vertrags-/Websiteänderungen zusätzlich Website regenerieren und
`eng/verify.ps1 -Mode Focused -Checks website-docs,documentation,skills` ausführen.
Zum Abschluss des gesamten Umbaus wegen der breiten Vertrags- und Kompositionsänderung
`eng/verify.ps1 -Mode Full` mit dem über eng/initialize-dotnet.ps1 initialisierten SDK.
Erforderliche Prüfungen müssen mit Exitcode 0 enden.

Der strukturelle Umbau fügt keine neue Benutzerfunktion hinzu. Die vorhandenen
unveröffentlichten Release Notes beschreiben bereits die sichtbaren Kompatibilitäts-
und Zahlenkorrekturen; ein zusätzlicher Refactoring-Eintrag wurde nicht ergänzt.
