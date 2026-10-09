# Konzept: Website und Produktdokumentation

Stand: 8. Oktober 2026. Der Ausbau ist umgesetzt: vollständige Referenzen, elf Anleitungen für UI und JSON, Serializer-Vorlagen, Agenten-Zugänge, Suche, komplexere illustrierte Beispiele und Light-Screenshots. Alle erklärungspflichtigen Felder und Optionen sind ausgefüllt; der strenge Prüfablauf akzeptiert keinen Aufschub mehr. Die Pflicht zur Pflege beider Zugänge ist in den Agentenregeln verankert.

## Ziel und Aufbau

Die Startseite erklärt den Nutzen anhand vollständiger Arbeitsabläufe. Die Dokumentation erklärt jede Funktion so genau, dass Nutzer einen eigenen Ablauf konfigurieren, Ergebnisse weiterverwenden und Fehler nachvollziehen können. Jobs, Automationen, Makros und Logs erhalten jeweils einen eigenen Bereich.

Hauptnavigation: **Startseite · Beispiele · Anwendung · Dokumentation · Download**. Jeder Menüpunkt öffnet eine eigene Seite mit aktiver Navigation; Unterseiten erhalten einen angepinnten Seitenpfad. Die bestehende ruhige Gestaltung, das originale App-Symbol und direkte Downloads bleiben erhalten. Die Dokumentation erhält eine linke Bereichsnavigation, eine Suche und rechts auf großen Bildschirmen ein Inhaltsverzeichnis. Mobil werden Navigation und Inhaltsverzeichnis aufklappbar. Jede Referenz und jedes Feld ist direkt verlinkbar.

| Adresse | Inhalt |
| --- | --- |
| `/` | Nutzen, drei anspruchsvollere Beispiele und Einstiege zu Anwendung und Download |
| `/anwendung/` | Anwendung kennenlernen, echte App-Ansichten für Jobs, Makros, Automationen und Verlauf |
| `/download/` | Windows-Installer, Versionshinweise, Einrichtung und Einstieg in die Dokumentation |
| `/beispiele/` | Vollständige Anleitungen mit Job, Automation und gegebenenfalls Makro |
| `/beispiele/<id>/` | Ablauf, Voraussetzungen, Datenfluss, Hinweise zum eigenen Aufbau, erwarteter Verlauf |
| `/doku/` | Einstieg und Auswahl: Jobs, Automationen, Makros, Logs |
| `/doku/benutzer/` | Bedienung und Anleitungen für die Oberfläche |
| `/doku/entwickler/` | Dateiformate, Verträge und Referenzen für Entwickler und Agenten |
| `/doku/erste-schritte/` | Installation, Beispieldaten, erstes Ausführen und Prüfen |
| `/doku/jobs/` | Phasen, Reihenfolge, Variablen, Wertequellen, Ergebnisse, Bedingungen und Fehlerverhalten |
| `/doku/steps/` | Vollständiger, durchsuchbarer Step-Katalog mit Kategorien |
| `/doku/steps/<type-id>/` | Ein Step mit allen konfigurierbaren Feldern und Ergebniswerten |
| `/doku/werte/` | Datentypen, Einheiten, Listen, Enum-Optionen, Wertequellen und Ergebnisbindungen |
| `/doku/automationen/` | Trigger, Aktionen, Ausführungsregeln, Aktivierung und Diagnose |
| `/doku/automationen/<id>/` | Jeder Trigger, jede Aktion und Startregel mit sämtlichen Einstellungen |
| `/doku/makros/` | Aufnehmen, bearbeiten, gruppieren, Timing und Ausführen |
| `/doku/makros/befehle/<id>/` | Jeder Makrobefehl mit allen Werten |
| `/doku/logs/` | Übersicht, Status, Aufmerksamkeit, Details, Filter und Ausführungsbeziehungen |
| `/doku/logs/ereignisse/<code>/` | Ereignis-/Diagnosecode: Bedeutung, Ursachen, nächste Schritte |
| `/doku/kompatibilitaet/` | Unterstützte Version, Altdaten und relevante Änderungen |

Der Step-Katalog enthält wirklich alle registrierten Typen. Interne Strukturmarker wie Blockenden werden als solche erklärt und nicht als frei hinzufügbare Aktionen angeboten. Die Vollständigkeit bezieht sich auf Felder, Optionen und Ergebnisse; frei eingegebene Texte oder beliebige Dateipfade sind keine endliche Liste von Werten.

## Anspruchsvollere Beispiele auf der Startseite

Drei unterschiedliche Beispiele mit jeweils etwa 6–12 sichtbaren Schritten ersetzen die derzeit sehr kurzen Jobs. Die Komplexität entsteht durch nachvollziehbare Bedingungen und Datenfluss, nicht durch wiederholte Warte- oder Anzeigeaktionen. Bild- und Texterkennung bleiben im vollständigen Katalog, stehen jedoch nicht im Mittelpunkt der Startseite.

1. **Arbeitsunterlagen vorbereiten:** Auswahl zwischen zwei Arbeitsarten → Bedingung anhand der Auswahl → passende Testdateien kopieren → benötigte Programme starten → optional ein vorbereitetes Makro ausführen → Abschlussmeldung. Zeigt Auswahl, Verzweigung, Dateiverarbeitung und Zusammenspiel von Job und Makro.
2. **Eingangsordner verarbeiten:** Eine Ordner-Automation startet den Job → Ereigniswerte übernehmen, soweit der bestehende Vertrag sie verfügbar macht → passende Eingaben prüfen → Dateien mit konfigurierten Wiederholungsversuchen kopieren → nächste Verarbeitung starten → Ergebnis und Fehler im Verlauf prüfen. Zeigt Trigger, Wertequellen, Sperr-/Fehlerfälle und Logs. Welche Ereigniswerte verfügbar sind, muss vor Umsetzung an den tatsächlichen Verträgen geprüft werden.
3. **Arbeitsplatz nach Zeitplan starten:** Zeitplan startet einen Job → Prozess-/Fensterzustand abfragen → Bedingungen verhindern unnötige Starts → Programm öffnen oder vorhandenes Fenster fokussieren → freigegebene Eingabesequenz ausführen → Abschluss im Verlauf ansehen. Zeigt Automationen, Zustandsabfragen, Bedingungen und Makros.

Jedes Beispiel erhält eine Detailseite, ein illustriertes Ablaufdiagramm und einen echten Screenshot mit sichtbarer Verzweigung bzw. Wertebindung. Eine kompakte Tabelle erläutert, welche Daten von welchem Schritt an welchen anderen Schritt fließen. Die Beispiele dienen ausschließlich zur Erläuterung; vollständige Jobs werden nicht zum Download angeboten. Interne Beispielkonfigurationen bleiben als Quelle für Validierung und Screenshots erhalten. IDs und Referenzen müssen zusammenpassen. Die Anleitung benennt Änderungen an Pfaden, Programmen, Monitoren und Triggern sowie die erwarteten Resultate. Beispiele werden mit den kanonischen Serializern und Validatoren geprüft; tatsächliche Ausführung wird gesondert ausgewiesen und nur in einer isolierten Testumgebung geprüft. Kein Beispiel wird als erfolgreich ausgeführt dargestellt, wenn nur seine Konfiguration validiert wurde.

## Echte App-Ansichten

Die Galerie zeigt Übersicht, einen verzweigten Job, Werte-/Ergebnisbindungen, Automationseinstellungen, Makro-Editor und Logs. Alle Standard-Screenshots werden mit dem echten **Light-Theme** der Anwendung gerendert, einschließlich der Loggingseite. Der dunkle Verlaufsscreenshot wird ersetzt; eine bloße Farbkorrektur des Bildes reicht nicht aus. Anonymisierte Beispieldaten, gleiche Akzentfarbe und vergleichbare Fenstergrößen sorgen für Konsistenz. Vollansichten bleiben anklickbar. Die Anwendungsseite und die Log-Dokumentation verwenden dieselbe gepflegte Light-Aufnahme; Detailbilder ergänzen sie nur, wenn sie eine Bedienhandlung erklären.

## Inhalt einer Step-Referenz

Die Benutzer-Doku steht auf jeder Funktionsseite zuerst: markierte Bilder, Bedienung,
Einstellungsbedeutung, Ergebnisse, Beispiele und Fehlerhinweise. Technische Feldnamen,
Typen, Standards, Eingabe-/Ergebnisverträge und JSON-Vorlagen sind am Ende gemeinsam
einklappbar. Gemischte Anleitungen folgen dieser Trennung; Dateiformate und Agentenabläufe
haben eigene technische Anleitungen. Zwei Einstiegsseiten und ein sichtbarer Bereichswechsel
erschließen beide Zugänge. URLs, Feldanker und vollständige Maschinenreferenzen bleiben erhalten.

Jede Seite enthält Zweck, Voraussetzungen, ein minimales Beispiel, ein Beispiel mit Weiterverwendung des Ergebnisses sowie Nebenwirkungen und Fehlerverhalten. Für jedes Feld werden folgende Angaben vollständig erklärt:

- Anzeigename, stabiler Feld-Identifier und verständliche Bedeutung.
- Datentyp, Einheit, Pflicht/optional und tatsächlicher Standardwert; kein Standard wird ausdrücklich so bezeichnet.
- Zulässige Werte und Grenzen; bei Enums die Bedeutung jeder einzelnen Option.
- Sichtbarkeitsbedingungen und Abhängigkeiten von anderen Einstellungen.
- Verfügbare Wertequellen, direkt eingegebene Werte, Variablen und akzeptierte Ergebnisse früherer Steps; Listenverhalten und Verhalten bei fehlenden Werten.
- Konkretes Beispiel sowie mögliche Validierungsfehler und deren Behebung.

Zusammengesetzte Einstellungen werden rekursiv erklärt, einschließlich ihrer Unterfelder. Gemeinsame Einstellungen wie Aktivierung, Phase, Bedingungen und Fehlerpolitik werden zentral dokumentiert und aus jeder betroffenen Referenz verlinkt. Für Ergebnisse werden alle stabilen Property-IDs, Typen, Kardinalitäten und konfigurationsabhängigen Varianten beschrieben. Dynamische Optionen werden durch Herkunft, Beispiel und Auswahlregel erklärt; gerätespezifische Listen werden nicht als feste globale Liste ausgegeben. Veraltete, weiterhin lesbare Werte erhalten einen Kompatibilitätshinweis.

Beispiel „Dateien/Ordner“: Kopieren, Verschieben, Umbenennen und Löschen jeweils erklären; Quelle/Ziel, neuen Namen, Filter, Elternordner, Wiederholungsanzahl und Wartezeit dokumentieren. Sichtbarkeit der Felder je Operation erläutern. Überschreiben und Fehlersituationen werden am tatsächlichen Handler geprüft, nicht aus dem Feldnamen abgeleitet.

## Automationen, Makros und Logs

**Automationen:** sämtliche Trigger aus dem vorhandenen Modell, alle jeweiligen Felder und Optionen, Zielaktionen sowie Ausführungsregeln. Erklärt werden auch Entprellung, Verzögerungen, parallele Starts, Entscheidungen über unterdrückte Starts und das Verhältnis von Trigger, Automation und gestarteter Ausführung. Zeit-/Zeitzonen- und Neustartverhalten werden anhand der Implementierung dokumentiert. Webhook- oder Windows-Ereignis-Konfiguration erhält eigene Beispiele ohne echte Zugangsdaten.

**Makros:** jeden Befehlstyp, Mauskoordinaten und deren Bezugssystem, Tasten und Kombinationen, Text, Wartezeiten, Aufnahmeparameter, Gruppen und Ausführungszeiten dokumentieren. Unterschied zwischen Wartezeit vor einem Befehl und einem Wartebefehl erklären. Editoraktionen und Wiederverwendung im Job werden durch Anleitungen ergänzt. Monitor-/Umgebungsabhängigkeiten und Abbruchverhalten werden konkret beschrieben.

**Logs:** Job-, Makro-, Automations- und Anwendungsmeldungen erklären; Laufstatus, Step-Status und Schweregrad getrennt darstellen. Auch „übersprungen“, „nicht ausgeführt“, „abgebrochen“, „mit Warnungen“ und unterbrochene Läufe sind zu erläutern. Die Doku beschreibt Filter, Zeitleiste, aggregierte Wiederholungen, Ursachenbezüge und Eltern-/Kind-Ausführungen sowie unvollständige oder nicht verfügbare Logdaten. Ereignis- und Diagnosecodes erhalten Bedeutung, mögliche Ursachen und belegte Abhilfen. Aufbewahrung, Export und Datenschutz werden nur entsprechend vorhandener Funktionen beschrieben.

## Quellen und technische Pflege

Die bestehende statische Website bleibt das Ausgabeziel; ein Backend ist für die Dokumentation nicht nötig. Vorgeschlagene neue Quellen: `website/docs/content/` für Erklärtexte, `website/docs/examples/` für Beispielpakete und `website/docs/assets/` für Screenshots. Export und Seitengenerator liegen unter `website/tools/`; HTML, Suchindex und Downloads werden reproduzierbar nach `website/dist/` erzeugt. Die Umsetzung verwendet `website/docs/content.json`, `website/docs/examples/` und die Tool-Projekte unter `website/tools/`; Screenshots liegen in `website/dist/`. Der Pflegeablauf steht in `website/docs/README.md`.

| Bereich | Kanonische Faktenquelle im Repository |
| --- | --- |
| Steps und Felder | `TaskAutomation/Steps/Definitions/StepDefinitionCatalog.cs`, Definitionen und `TaskAutomation.Contracts/Steps/` |
| Eingaben und fehlende Werte | `StepInputContractRegistry`, Wertauflösung und Validatoren |
| Ergebnisse | `StepResultContractRegistry`, `StepResultMetadata`, Windows-Abfrageverträge und dynamische Ergebnisverträge |
| Automationen | `TaskAutomation/Automations/AutomationDefinition.cs`, `AutomationValidation`, Engine und Trigger-Provider |
| Makros | `TaskAutomation/Makros/MakroCommands.cs`, Regeln, Validatoren, Aufnahme- und Ausführungslogik |
| Logs | `TaskAutomation.Contracts/Logging/LogContracts.cs`, `TaskAutomation/Logging/` und aktuelle Logging-Architektur |
| Namen und Hilfetexte | Deutsche und englische App-Ressourcen, soweit vorhanden |

Der Export liest Fakten über die vorhandenen Kataloge und Verträge. Erklärungen ergänzen deren stabile IDs. Typ-, Feld- oder Enum-Listen werden nicht als unabhängige zweite Fachregel gepflegt. Für Automationen/Makros ergänzt ein dokumentationsseitiger Adapter die bestehenden Modelle; ein UI-Metadatenkatalog wird nur eingeführt, falls er tatsächlich gebraucht wird. Gemeinsame Feld-/Optionsbeschreibungen können wiederverwendet werden. Laufzeitabhängige Ergebnisschemata brauchen repräsentative Konfigurationen; Reflection allein deckt weder Verhalten noch alle Varianten ab.

Erklärtexte werden von Agenten bei Änderungen aktiv gepflegt und überprüft. Aus dem Code automatisch erzeugte Fakten ersetzen keine Erklärung. Ein Schema-Fingerprint macht Vertragsänderungen sichtbar; er beweist nicht, dass ein Text fachlich korrekt ist. Auch Änderungen an Handlern, Timing, Fehlerpolitik oder UI-Bedienung müssen inhaltlich bewertet werden. Quellbezüge und ein dokumentierter Review-Stand unterstützen diese Prüfung.

Die Doku zeigt die unterstützte App-Version. Unveröffentlichte Änderungen werden als kommende Version getrennt von der Dokumentation des aktuellen Downloads aufgebaut. Eine Veröffentlichung wählt ausdrücklich den passenden Release-Stand. Bestehende URLs bleiben stabil; umbenannte oder entfernte Funktionen erhalten Weiterleitung bzw. Kompatibilitätshinweis. Deutsche Inhalte sind zum Start vollständig; Export und stabile IDs unterstützen später Englisch, ohne unvollständige Übersetzungen als fertige Referenz auszugeben.

## Agentenablauf und vorgeschlagene Prüfungen

1. Vor einer relevanten Änderung die betroffenen Doku-Seiten und kanonischen Besitzer identifizieren.
2. Code, Erklärtexte, Feld-/Optionsbeschreibungen und Beispiele im selben Arbeitsauftrag ändern.
3. Metadaten exportieren und Dokumentationsausgabe neu erzeugen; veraltete generierte Dateien erkennen.
4. Vollständigkeit prüfen: alle Typen, verschachtelten Felder, Enum-Optionen, Ergebnisvarianten, Trigger, Makrobefehle und Logcodes sind beschrieben; keine verwaisten Referenzen oder Platzhalter.
5. Beispielpakete deserialisieren und mit bestehenden Validatoren prüfen; Beziehungen zwischen Dateien prüfen. Geeignete Ausführungstests decken Datenfluss, Verzweigungen und Abbruch ab.
6. Geänderte Bedienung durch echte Light-Screenshots belegen, Links/Suche/mobile Darstellung prüfen und das zum Änderungsrisiko passende Repository-Checkprofil ausführen.
7. In der Abschlussmeldung nennen, welche Dokumentation gepflegt wurde und welche Prüfungen erfolgten. Wenn Inhalte unverändert bleiben, dies anhand der tatsächlichen Auswirkungen begründen.

Der implementierte Check `website-docs` wird in den vorhandenen `eng/verify.ps1`-Einstieg integriert und in CI verbindlich. Er scheitert bei fehlender Abdeckung, nicht aktualisierten Metadaten, kaputten Links, ungültigen Beispielen oder veralteter Ausgabe. Bei Verhaltensänderungen verlangt der Ablauf eine überprüfte inhaltliche Dokumentationsentscheidung. Prüfungen dürfen nicht behaupten, semantische Richtigkeit vollständig automatisch erkennen zu können. Die Agentenpflege geschieht im jeweiligen Änderungsauftrag; dafür ist kein periodischer Hintergrundjob nötig. Automatisches Deployment ist davon getrennt.

## Umsetzung und Abnahme

Zusätzlich umgesetzt: `/doku/werte-verbinden/`, `/doku/dateiformate/`, `/doku/makros-bearbeiten/`,
`/doku/automationen-einrichten/`, `/doku/logs-verstehen/`, `/doku/windows-funktionen/`,
`/doku/agenten/` und `/doku/fehlerbehebung/`. `/llms.txt`, `/llms-full.txt`,
`/doku/referenz.md` und `/doku/vertragsdaten.json` verwenden dieselben gepflegten Inhalte.
Ein vollständiges Werteverbindungsbeispiel wird zusätzlich mit den App-Validatoren geprüft;
strukturelle Defaultvorlagen sind klar von validierten Beispielabläufen getrennt.

1. Inventar aller Typen, Felder, Optionen und Varianten erstellen; Exportformat und Inhaltsvorlage mit einer vollständigen Datei-Step-Referenz erproben.
2. Alle vier Doku-Bereiche mit vollständiger Referenz und zentralen Werte-/Fehlererklärungen ausbauen; Suche, stabile URLs und Versionszuordnung ergänzen.
3. Drei komplexere illustrierte Beispiele umsetzen und validieren, Galerie einschließlich Logs in Light neu rendern.
4. Generator und `website-docs`-Check anbinden; Pflegepflicht in den vorhandenen Step-/Logging-/Validierungsworkflows konkretisieren.
5. Website lokal prüfen und erst anschließend veröffentlichen.

Abnahme: kein registrierter Typ oder konfigurierbarer Wert bleibt unerklärt; keine nicht implementierte Funktion wird als vorhanden dargestellt; Beispieldownloads entsprechen den gezeigten Abläufen; alle Standardaufnahmen zeigen das echte Light-Theme; eine neue oder geänderte Funktion ohne passende Dokumentation lässt sich durch den eingebundenen Prüfablauf nicht stillschweigend abschließen.
