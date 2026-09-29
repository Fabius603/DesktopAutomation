---
date: 2026-09-28
title: Architecture conformance migration plan
status: historical-snapshot
superseded_by:
---

# Implementierungsplan zur vollständigen Architekturkonformität

## Context at the time

Das Repository besitzt verbindliche Regeln für Schichtengrenzen, einen kanonischen Besitzer je
fachlicher Regel, verhaltensorientierte Tests und ein deterministisches Abschluss-Gate. Bestehender
Code erfüllt diese Regeln noch nicht nachweislich überall. Insbesondere kann fachliche Logik in
`DesktopAutomationApp` liegen oder in mehreren Editoren, ViewModels, Convertern, Services und
Ausführungspfaden unterschiedlich implementiert sein.

Die Migration darf weder bestehende gespeicherte Jobs, Makros, Automationen und Einstellungen
beschädigen noch das sichtbare Verhalten unbeabsichtigt verändern. Sie erfolgt deshalb nicht als
projektweiser Großumbau, sondern in abgesicherten vertikalen Verhaltensbereichen.

## Requested behavior

Nach Abschluss der Migration gilt nachweisbar:

- Jede fachliche Regel, Validierung, Standardbelegung, Normalisierung, Zuordnung und
  Zustandsänderung hat genau einen benannten kanonischen Besitzer.
- `DesktopAutomationApp` enthält nur Darstellung, Interaktion, Bindings, Lokalisierung,
  UI-Zustand und die Projektion bereits getroffener fachlicher Entscheidungen.
- Alle Editoren, Ausführungspfade, Importer und Detailansichten verwenden dieselben gemeinsamen
  Regeln.
- Persistenz- und Vertragsänderungen bleiben rückwärtskompatibel oder besitzen eine ausdrücklich
  akzeptierte Migration.
- Mechanisch prüfbare Architekturgrenzen werden durch Architekturtests geschützt.
- Fachliche Invarianten werden auf der Ebene ihres kanonischen Besitzers getestet.
- `eng/verify.ps1 -Mode Full` ist nach jedem abgeschlossenen Migrationsbereich erfolgreich.

## Leitprinzipien

1. Zuerst Bestand erfassen, dann verändern.
2. Verhalten vor dem Refactoring mit geeigneten Tests absichern.
3. Eine fachliche Regel wird vertikal über alle Schichten migriert.
4. Neue Abstraktionen benötigen einen konkreten fachlichen Zweck; es entsteht kein allgemeiner
   Sammelservice.
5. Alte und neue Implementierung bleiben nicht dauerhaft parallel aktiv.
6. Kompatibilitätsadapter übersetzen nur alte Daten und delegieren anschließend an den
   kanonischen Besitzer.
7. Ein grüner Build ersetzt weder fachliche Tests noch die Suche nach konkurrierenden
   Implementierungen.

## Zielarchitektur

| Bereich | Erlaubte Verantwortung | Nicht erlaubte Verantwortung |
|---|---|---|
| `TaskAutomation.Contracts` | stabile IDs, Deskriptoren, Ein- und Ausgabeverträge | WPF-, WinForms-, Dialog- oder Lokalisierungslogik |
| `TaskAutomation` | Modelle, Invarianten, Validierung, Defaults, Normalisierung, Ausführung, Kompatibilität | UI-Zustand und lokalisierte Darstellung |
| `DesktopAutomation.Application` | Koordination anwendungsweiter Anwendungsfälle und Ports | XAML, Controls und duplizierte Fachregeln |
| `DesktopAutomationApp` | Views, ViewModels als Adapter, Interaktion, Binding, Fokus, Lokalisierung | eigenständige Geschäftsentscheidungen und Persistenzsemantik |
| Technische Projekte | klar abgegrenzte Betriebssystem-, Bild-, Capture-, Overlay- und Logging-Fähigkeiten | fachliche Job- oder Automationsregeln |
| `Common.JsonRepository` | Pfade und generische JSON-Persistenzmechanik | Bedeutung einzelner Job- oder Automationsfelder |

## Zu erstellende Steuerungsartefakte

### Architektur-Konformitätsregister

In Phase 1 wird `docs/architecture/conformance-register.md` als lebendes Register angelegt. Jeder
Fund erhält genau einen Datensatz mit folgenden Feldern:

| Feld | Inhalt |
|---|---|
| ID | stabile Kennung, beispielsweise `ARCH-001` |
| Invariante | beobachtbare fachliche Regel in einem Satz |
| Fundstellen | alle bekannten Implementierungen und Verbraucher |
| Verletzung | falsche Schicht, Duplikat, abweichendes Verhalten oder unklare Verantwortung |
| Kanonisches Ziel | Projekt, Namespace und vorgesehener Typ |
| Kompatibilitätsrisiko | keines, gering, mittel oder hoch mit Begründung |
| Benötigte Tests | Unit, Contract, Architecture, Integration, UI und/oder End-to-End |
| Abhängigkeiten | andere Register-IDs, die vorher bearbeitet werden müssen |
| Status | entdeckt, analysiert, abgesichert, migriert oder verifiziert |
| Nachweis | Testnamen, Suchergebnis und Verifikationslauf |

Das Register ist keine Ablage für unbestimmte technische Schulden. Jeder Eintrag braucht einen
vorgesehenen Besitzer und ein prüfbares Endkriterium.

### Architekturkarte

`docs/architecture/overview.md` wird nach dem Audit um die tatsächlich bestätigten
Abhängigkeitsrichtungen und wichtigsten fachlichen Besitzer ergänzt. Nur der implementierte
Ist-Zustand wird dort dokumentiert. Geplante, aber noch nicht umgesetzte Grenzen bleiben im
Konformitätsregister.

## Phase 0: Reproduzierbare Ausgangsbasis

### Arbeitsschritte

1. `git status --short` erfassen und bestehende Änderungen von der Migration abgrenzen.
2. `powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full` ausführen.
3. Anzahl und Ergebnis jeder Testebene als Ausgangsnachweis im Konformitätsregister festhalten.
4. Aktuelle Projektabhängigkeiten aus allen `*.csproj`-Dateien erfassen.
5. Bereits bekannte Warnungen separat dokumentieren; Warnungen dürfen nicht als neue
   Migrationsfehler fehlinterpretiert werden.

### Ergebnis

- reproduzierbarer grüner oder ausdrücklich blockierter Ausgangszustand;
- dokumentierter Abhängigkeitsgraph;
- keine Produktionscodeänderung.

### Abnahmekriterium

Die Ausgangsbasis lässt sich auf demselben Commit erneut ausführen und liefert dasselbe fachliche
Ergebnis. Ein bereits rotes Gate wird vor Beginn der Architekturarbeit repariert oder als externer
Blocker geklärt.

## Phase 1: Repository-weites Read-only-Audit

### 1.1 Frontend-Logik inventarisieren

Untersucht werden mindestens:

- `DesktopAutomationApp/**/*ViewModel*.cs`;
- `DesktopAutomationApp/**/*Converter*.cs`;
- Code-behind-Dateien von Views und Controls;
- frontendnahe Services, Dialoge und Editor-Resolver;
- Switch-Ausdrücke und Verzweigungen über Step-Typen, Operatoren und Statuswerte;
- Validierungsnachrichten, Defaultwerte, Optionslisten und Mappingtabellen;
- direkte Datei-, JSON- oder Pfadbehandlung im Frontend.

Für jeden Kandidaten wird entschieden, ob er reine Präsentation, Interaktionslogik oder eine
fachliche Entscheidung enthält. Nur die letzte Kategorie wird als Grenzverletzung registriert.

### 1.2 Duplikate und divergierende Regeln inventarisieren

Die Suche erfolgt fachlich, nicht nur textuell:

- gleiche IDs, Konstanten, Standardwerte oder Optionskataloge;
- gleichartige Validatoren in Editor und Executor;
- wiederholte Umwandlungen gespeicherter Werte;
- mehrfaches Ableiten von Step-Details oder Ergebnis-Metadaten;
- getrennte Regeln für Hinzufügen, Laden, Kopieren, Einfügen und Ausführen;
- mehrfach implementierte Control-Flow-, Verschiebe- oder Gruppierungsregeln;
- Servicevarianten, die dieselben Dateien, Logs oder Betriebssystemdaten unterschiedlich lesen.

Textuelle Clone-Erkennung darf Hinweise liefern, ersetzt aber nicht den Vergleich der fachlichen
Bedeutung.

### 1.3 Persistenz und Verträge prüfen

Für Jobs, Makros, Automationen, Einstellungen und Pfade werden geprüft:

- stabile IDs und Property-Pfade;
- Defaultwerte neuer und alter Datenstände;
- Deserialisierung fehlender oder unbekannter Felder;
- Migrationspfade und Legacy-Adapter;
- mehrfach vorhandene Serialisierungs- oder Normalisierungsregeln;
- Unterschiede zwischen Editor-Laden und Runtime-Materialisierung.

### 1.4 Tests klassifizieren

Jeder bestehende Test wird nicht einzeln umgeschrieben. Das Audit markiert Testgruppen, wenn sie:

- Quelltext, XAML-Fragmente oder interne Aufrufreihenfolgen als primären Vertrag verwenden;
- eine fachliche Regel ausschließlich über das Frontend testen;
- dieselbe Invariante redundant auf mehreren Ebenen prüfen;
- Zeit, Zufall, Kultur, Dateisystem, Netzwerk oder Maschinenzustand unkontrolliert verwenden;
- im falschen Testprojekt liegen.

### Ergebnis

- vollständiges Konformitätsregister;
- gruppierte Migrationsbereiche;
- noch keine Verschiebung von Produktionslogik.

### Abnahmekriterium

Jeder bekannte Verstoß besitzt Fundstellen, Zielbesitzer, Risiko, Testbedarf und eine Priorität.
Die Suche wurde über alle Produktionsprojekte durchgeführt, nicht nur über aktuell auffällige
Views.

## Phase 2: Priorisierung und Migrationsschnitt

### Prioritätsklassen

1. **P0 – Daten- oder Ausführungsrisiko:** Persistenz, Migration, Sicherheit, Control Flow,
   Abbruch und Ausführungssemantik.
2. **P1 – Divergierendes Benutzerverhalten:** Editor und Runtime entscheiden unterschiedlich;
   mehrere Oberflächen zeigen oder speichern andere Ergebnisse.
3. **P2 – Wartbarkeitsrisiko:** aktuell gleiches Verhalten mit mehreren Implementierungen.
4. **P3 – Strukturelle Bereinigung:** falsche Lage ohne aktuelle fachliche Divergenz.

Innerhalb derselben Klasse werden zuerst Regeln migriert, von denen viele andere Einträge
abhängen. Ein Migrationsbereich soll klein genug für eine eindeutige Verifikation sein, aber alle
Verbraucher derselben Invariante enthalten.

### Vorgesehene Reihenfolge der Arbeitsströme

1. Persistenzverträge, stabile IDs und Rückwärtskompatibilität.
2. Validierung, Defaults und Normalisierung.
3. Control Flow, legale Einfüge- und Verschieberegeln.
4. Step-Deskriptoren, Input- und Result-Contracts sowie Bindings.
5. Ausführungs- und Zustandsübergänge.
6. Anwendungsweite Koordination und Servicegrenzen.
7. Frontend-Projektionen, Converter und Detaildarstellung.
8. Technische Querschnittsdienste wie Logging, Pfade und Windows-Integration.
9. Verbleibende P2- und P3-Duplikate.

### Abnahmekriterium

Jeder Registereintrag gehört genau einem Arbeitsstrom und besitzt eine Reihenfolge. Zirkuläre
Abhängigkeiten sind aufgelöst oder durch eine vorgeschaltete gemeinsame Abstraktion ersetzt.

## Phase 3: Automatische Architekturgrenzen ausbauen

Diese Phase wird vor den großen Migrationen begonnen und während jeder Migration erweitert.

### Verbindliche Prüfungen

- Shared- und Contract-Projekte referenzieren `DesktopAutomationApp` nicht.
- `TaskAutomation.Contracts` aktiviert weder WPF noch WinForms und referenziert keine UI-Typen.
- Neue Projektabhängigkeiten folgen der dokumentierten Richtung.
- Stabilitätsrelevante IDs und Descriptor-Registrierungen sind eindeutig.
- Für deterministisch erkennbare verbotene Abhängigkeiten existieren Architekturtests.
- CI und lokale Ausführung verwenden weiterhin ausschließlich `eng/verify.ps1` als Pflicht-Gate.

### Umsetzung

1. Bestehende Architekturtests um den bestätigten Abhängigkeitsgraph erweitern.
2. Regeln bevorzugt gegen Projekt- und Assembly-Metadaten testen.
3. Namespace- oder Typabhängigkeiten nur dann prüfen, wenn daraus ein stabiler Vertrag entsteht.
4. Keine pauschalen Quelltextverbote einführen, die Kommentare oder legitime UI-Projektionen
   fälschlich beanstanden.
5. Jede neu gefundene wiederkehrende Verletzung auf deterministische Prüfbarkeit untersuchen.

### Abnahmekriterium

Ein absichtlich eingebauter verbotener Projektverweis lässt den Architekturtest scheitern. Die
Tests sind stabil gegenüber Umbenennungen interner Methoden und Formatierungsänderungen.

## Phase 4: Standardablauf je vertikalem Migrationsbereich

Jeder Bereich durchläuft dieselben Schritte und wird erst danach als `verifiziert` markiert.

### Schritt A: Invariante und Umfang festlegen

- Beobachtbares Verhalten in einem Satz formulieren.
- Sämtliche Ein- und Ausgänge nennen.
- Alle UI-, Runtime-, Persistenz- und Importpfade erfassen.
- Einen konkreten kanonischen Zieltyp benennen.
- Nicht zum Bereich gehörende Bereinigungen ausdrücklich ausschließen.

### Schritt B: Verhalten absichern

- Bestehende geeignete Tests suchen und erweitern.
- Fehlende fachliche Fälle am zukünftigen Besitzer oder zunächst über einen stabilen öffentlichen
  Einstiegspunkt abdecken.
- Relevante Erfolgs-, Fehler-, Missing-Input-, Abbruch-, Skip- und Legacy-Fälle bestimmen.
- Testdaten, Uhr, Zufall, Kultur, Verzeichnisse und externe Grenzen kontrollieren.

### Schritt C: Kanonischen Besitzer herstellen

- Regel in die niedrigste geeignete frontendneutrale Schicht verschieben.
- Benötigte Variation als explizite Daten, Policy, Strategy oder Interface modellieren.
- Diagnosecodes statt lokalisierter Meldungen aus Shared-Code zurückgeben.
- Persistierte Modelle nur mit sicheren Defaults erweitern.

### Schritt D: Alle Verbraucher umstellen

- Editor, Laden, Speichern, Kopieren, Einfügen, Details und Runtime auf den Besitzer umstellen.
- ViewModels und Converter auf Projektion und Formatierung reduzieren.
- Legacy-Eingaben einmalig adaptieren und anschließend an den Besitzer delegieren.
- Keine Übergangsimplementierung durch Copy-and-paste erstellen.

### Schritt E: Konkurrenz entfernen

- Alte Regeln, Konstanten, Hilfsmethoden und Sonderfälle löschen oder auf den Besitzer umleiten.
- Repository-Suche aus Schritt A wiederholen.
- Verbleibende Treffer als legitime Verbraucher oder offene Registereinträge klassifizieren.

### Schritt F: Ebenengerechte Tests fertigstellen

| Änderung | Primärer Nachweis | Zusätzlicher Nachweis bei Bedarf |
|---|---|---|
| reine Fachregel | Unit-Test am Besitzer | End-to-End für kritischen Ablauf |
| ID, Descriptor oder Persistenzformat | Contract-Test | Integrationstest mit Legacy-Fixture |
| Projekt- oder Schichtgrenze | Architekturtest | keiner |
| technische Adapterintegration | Integrationstest | End-to-End für kritischen Ablauf |
| Binding, Fokus oder Darstellung | UI-Test | gerenderte Prüfung bei visueller Relevanz |
| kompletter Benutzerablauf | End-to-End-Test | schmalere Tests für Fehlerdiagnose |

### Schritt G: Dokumentation und Gate

- Architekturübersicht aktualisieren, wenn sich der implementierte Ist-Zustand geändert hat.
- Dauerhafte neue Entscheidung als Decision Record erfassen.
- Registereintrag mit Besitzer, entfernten Duplikaten und Testnachweisen aktualisieren.
- Benutzerrelevante Änderung in den Release Notes zusammenfassen; reine interne Refactorings
  erhalten keinen Release-Note-Eintrag.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full` erfolgreich
  ausführen.

## Phase 5: Spezifische Arbeitsströme

### 5.1 Validierung, Defaults und Normalisierung

- Alle Editorvalidierungen ihren Shared-Validatoren gegenüberstellen.
- Defaultwerte aus ViewModels und Dialogkonstruktoren inventarisieren.
- Normalisierung beim Laden, Speichern und Ausführen vergleichen.
- Pro Step beziehungsweise Modell einen fachlichen Besitzer bestimmen.
- UI zeigt gemeinsame Diagnosecodes lokalisiert an und entscheidet nicht erneut über Gültigkeit.

Fertig, wenn Editor, Import und Runtime bei denselben Eingaben dieselbe fachliche Entscheidung
treffen.

### 5.2 Control Flow und Strukturregeln

- Blockerkennung, Verschieben, Einfügen, Gruppieren und Laufzeitübergänge vergleichen.
- `ControlFlowStructureAnalyzer` oder einen ausdrücklich benannten Nachfolger als gemeinsamen
  Besitzer bewerten.
- ViewModels dürfen erlaubte Aktionen nur aus gemeinsamen Ergebnissen ableiten.
- Strukturregeln mit tabellarischen Unit-Tests und kritischen End-to-End-Fällen absichern.

Fertig, wenn es keine separate UI- und Runtime-Interpretation derselben Struktur gibt.

### 5.3 Step-Metadaten und Result-Bindings

- Descriptor-Kataloge, Editor-Hints, Result-Metadaten, Property-IDs und Legacy-Pfade erfassen.
- Ableitungen in Pickern, Convertern und Detailansichten mit Contract-Metadaten vergleichen.
- Alle auswählbaren Resultate und verschachtelten Pfade über stabile IDs erreichbar halten.
- Metadaten einmal erzeugen und in allen Frontends projizieren.

Fertig, wenn Editor, Details, Persistenz und Runtime denselben Vertrag konsumieren.

### 5.4 Persistenz und Kompatibilität

- Fixtures repräsentativer alter Datenstände anlegen oder vervollständigen.
- Deserialisierung, Defaults und Best-effort-Migrationen über Contract-Tests absichern.
- Pfadlogik ausschließlich über `Common.JsonRepository/AppPaths.cs` beziehen.
- Migrationen dürfen existierende Benutzerdaten nicht überschreiben und Teilzustände tolerieren.

Fertig, wenn alle unterstützten Altstände laden und semantisch zum kanonischen Modell führen.

### 5.5 Ausführung und Anwendungskoordination

- Fachliche Regeln aus Dispatchern, Handlern und Frontend-Services trennen.
- Anwendungsfälle in `DesktopAutomation.Application` koordinieren, ohne UI-Typen einzuführen.
- Betriebssystemzugriffe hinter engen Ports oder technischen Services kapseln.
- Abbruch, Fehler, Skip und Statusübergänge explizit testen.

Fertig, wenn derselbe Anwendungsfall ohne WPF-ViewModel aufgerufen und getestet werden kann.

### 5.6 Frontend ausdünnen

- ViewModels behalten Commands, bindbaren Zustand und die Übersetzung gemeinsamer Ergebnisse.
- Converter formatieren Werte, entscheiden aber nicht über deren fachliche Bedeutung.
- Code-behind enthält nur Lebenszyklus, Fokus, Eventweiterleitung und echte View-Belange.
- UI-spezifische Services dürfen keine zweite Persistenz- oder Validierungssemantik besitzen.

Fertig, wenn entfernte UI-Komponenten keine fachlichen Regeln mitnehmen würden.

### 5.7 Technische Querschnittsdienste

- Dateizugriff, Logging, Capture, Bilderkennung und Windows-Zustand nach tatsächlicher Fähigkeit
  gruppieren.
- Mehrere Leser oder Mapper derselben technischen Quelle auf einen Service konsolidieren.
- Fachliche Interpretation bleibt außerhalb des technischen Adapters.

Fertig, wenn jede technische Quelle einen klaren Zugriffspfad und kontrollierbare Testgrenzen hat.

## Phase 6: Testbestand bereinigen

Die Testbereinigung erfolgt zusammen mit den jeweiligen Migrationsbereichen, nicht als isolierte
Massenverschiebung.

1. Fachliche Tests zum kanonischen Besitzer verschieben oder dort neu formulieren.
2. Tests entfernen, die nur eine nun gelöschte Duplikatimplementierung schützen.
3. UI-Tests auf Binding, Interaktion, Lokalisierung, Barrierefreiheit und Rendering begrenzen.
4. Contract-Tests für IDs, Persistenz, Metadaten und Kompatibilität verwenden.
5. Integrationstests nur für reale Komponentenübergänge einsetzen.
6. End-to-End-Tests auf wenige kritische Benutzerabläufe konzentrieren.
7. Quelltext- und Aufrufreihenfolgenassertions durch Verhaltensnachweise ersetzen.
8. Jede Testebene einzeln sowie abschließend über das Full-Gate ausführen.

## Phase 7: Abschlussaudit

### Wiederholung des Audits

- Alle Suchkategorien aus Phase 1 erneut ausführen.
- Jeden Registereintrag gegen Code, Tests und Dokumentation prüfen.
- Projektabhängigkeiten erneut erfassen und mit der Zielrichtung vergleichen.
- Verwaiste Adapter, alte Feature-Flags und nicht mehr verwendete Hilfstypen entfernen.
- Lokalisierung, Release Notes und Persistenz-Fixtures auf Konsistenz prüfen.

### Umgang mit Ausnahmen

Eine verbleibende Abweichung ist nur zulässig, wenn eine akzeptierte Designentscheidung die
allgemeine Regel für diesen Fall ausdrücklich ersetzt. Ein offener Backlogeintrag allein macht
das Repository nicht konform.

### Abschlussnachweis

Der Abschlussbericht enthält:

- Liste der kanonischen Besitzer nach fachlichem Bereich;
- Liste der entfernten oder zusammengeführten Implementierungen;
- Ergebnis aller Architektur- und Verhaltenstests;
- bestätigte Rückwärtskompatibilität;
- akzeptierte Ausnahmen mit Decision-Record-Link;
- Ausgabe des erfolgreichen `verify.ps1 -Mode Full`-Laufs.

## Acceptance criteria

Die Gesamtmigration ist ausschließlich dann abgeschlossen, wenn alle folgenden Bedingungen
erfüllt sind:

1. Das vollständige Repository wurde nach den Kategorien aus Phase 1 untersucht.
2. Das Konformitätsregister enthält keinen offenen Verstoß.
3. Jede fachliche Regel im Register besitzt genau einen implementierten kanonischen Besitzer.
4. Kein ViewModel, Converter oder Code-behind implementiert eigenständig Validierung, Defaults,
   Persistenzsemantik, Control Flow oder Ausführungsregeln.
5. Alle bekannten konkurrierenden Implementierungen wurden entfernt oder durch eine akzeptierte
   Entscheidung legitimiert.
6. Projekt- und Schichtgrenzen sind soweit deterministisch möglich durch Architekturtests
   geschützt.
7. Fachliche Invarianten sind am Besitzer durch verhaltensorientierte Tests abgesichert.
8. Alte unterstützte Persistenzstände laden weiterhin mit derselben fachlichen Bedeutung.
9. Deutsche und englische Ressourcen sind synchron.
10. Benutzerrelevante Änderungen sind in den Release Notes kuratiert; interne Refactorings nicht.
11. Der abschließende Full-Lauf von `eng/verify.ps1` endet mit Exit-Code `0`.

## Assumptions and constraints

- Bestehende, nicht zu einem Migrationsbereich gehörende Benutzeränderungen bleiben unangetastet.
- Es gibt keine Big-Bang-Neuschreibung und keine gleichzeitige Umstellung aller Projekte.
- Öffentliche und persistierte Verträge bleiben kompatibel, sofern keine Migration ausdrücklich
  vereinbart wurde.
- Neue externe Architektur- oder Analyseabhängigkeiten werden nur eingeführt, wenn vorhandene
  .NET- und Repositorymittel die Regel nicht stabil prüfen können.
- Die aktuelle Entscheidung
  [Give every business rule one canonical owner](../decisions/2026-09-28-single-owner-for-business-rules.md)
  bleibt für die Migration maßgeblich.
- Dieser Plan beschreibt den Stand vom 28. September 2026 und muss vor späterer Ausführung erneut
  bestätigt werden.

## Open questions

Vor Beginn von Phase 2 sind mit dem Benutzer zu bestätigen:

1. Welche Produktbereiche haben aktuell die höchste Fehler- oder Änderungshäufigkeit und sollen
   innerhalb derselben Prioritätsklasse zuerst migriert werden?
2. Welche historischen Persistenzstände gelten weiterhin als offiziell unterstützt?
3. Sind während der Migration bewusst sichtbare Verhaltenskorrekturen erlaubt, oder sollen
   zunächst ausschließlich verhaltensneutrale Refactorings erfolgen?
4. Sollen akzeptierte Ausnahmen vollständig beseitigt werden oder dürfen klar begründete
   plattformspezifische Grenzen dauerhaft bestehen bleiben?

> This specification is a historical snapshot. Confirm its current applicability with the user
> before using it as an implementation requirement.
