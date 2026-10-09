"""Reviewed product guides shared by HTML and the agent-readable documentation."""


def section(id, title, paragraphs=(), steps=(), rows=(), headings=(), links=(), code=None):
    return dict(id=id, title=title, paragraphs=list(paragraphs), steps=list(steps),
                rows=list(rows), headings=list(headings), links=list(links), code=code)


def guides(meta, authoring, examples):
    templates = authoring['templates']
    return [
        dict(id='erste-schritte', title='Erste Schritte in der UI', audience='UI',
             intro='Einen Ablauf erstellen, kontrolliert ausprobieren und anschließend automatisieren – ohne JSON bearbeiten zu müssen.', sections=[
            section('erster-job', 'Einen ersten Job erstellen', steps=[
                'Öffne Jobs und lege einen Job mit einem beschreibenden Namen an. Lass Wiederholen für den ersten Versuch ausgeschaltet.',
                'Füge in der Hauptphase einen Step hinzu. Wähle zuerst eine einfache Aktion wie Timeout oder Benutzerauswahl. Die Step-Referenz erklärt jede Einstellung und die angebotenen Optionen.',
                'Wähle beim Bearbeiten eines Werts einen direkten Wert oder eine kompatible Wertequelle. Ein direkter Wert gehört zu diesem Step; eine Job-Variable lässt sich mehrfach verwenden.',
                'Speichere und behebe die angezeigten Validierungsfehler. Ein fehlendes Pflichtfeld oder ein unpassender Quellentyp wird nicht durch die Reihenfolge der Steps gültig.',
                'Starte den Job manuell. Prüfe anschließend Logs: Laufstatus, Step-Status und fachliches Ergebnis müssen zum erwarteten Ablauf passen.'],
                links=[('Alle Steps', '/doku/steps/'), ('Werte verbinden', '/doku/werte-verbinden/'), ('Logs lesen', '/doku/logs-verstehen/')]),
            section('echte-beispiele', 'Mit vollständigen Beispielen weiterarbeiten', paragraphs=[
                'Die drei illustrierten Beispiele zeigen eine Rückfrage mit Verzweigungen, eine Dateisicherung mit Prüfungen und einen Arbeitsplatz mit Prozessaktionen. Sie erklären den Aufbau und die Werteverbindungen als Anregung für eigene Jobs.',
                'Die Konfigurationen wurden mit den App-Serializern erneut gelesen und validiert. Programme, Dateiaktionen und Eingaben wurden dabei nicht auf einem echten Desktop ausgeführt. Passe Pfade, Zielprogramme und Bildschirme an und teste jeden Ablauf manuell.'],
                links=[('Arbeitsunterlagen vorbereiten', '/beispiele/unterlagen/'), ('Sicherung mit Rückfrage', '/beispiele/sicherung/'), ('Arbeitsplatz vorbereiten', '/beispiele/arbeitsplatz/')]),
            section('automation', 'Einen geprüften Job automatisch starten', steps=[
                'Lege im Bereich Automationen eine Automation an und lass sie zunächst deaktiviert.',
                'Wähle einen Trigger, zum Beispiel Hotkey, Zeitplan oder eine Dateiänderung, und ergänze dessen Pflichtparameter.',
                'Wähle den bereits manuell geprüften Job als Ziel. Lege fest, was passieren soll, falls das Ziel schon läuft, und ob ein Cooldown benötigt wird.',
                'Aktiviere die Automation und löse den Trigger gezielt aus. Prüfe in Logs sowohl die Triggerentscheidung als auch den zugehörigen Joblauf.'],
                links=[('Automationen einrichten', '/doku/automationen-einrichten/')])]),
        dict(id='jobs', title='Jobs und Abläufe', audience='UI und JSON',
             intro='Ein Job besteht aus drei Phasen. Reihenfolge, Blockstruktur und Werteverbindungen bestimmen gemeinsam, was ausgeführt wird.', sections=[
            section('phasen', 'Startphase, Hauptphase und Endphase', rows=[
                ['Startphase / startSteps', 'Einmalige Vorbereitung vor der Hauptphase. Hier erzeugte Ergebnisse können in der Hauptphase verwendet werden.'],
                ['Hauptphase / steps', 'Der eigentliche Ablauf. repeating=true wiederholt diese Phase; aktuelle Ergebnisse werden je Durchlauf erneuert.'],
                ['Endphase / endSteps', 'Aufräumen nach dem Ablauf, auch bei regulären Stopps beziehungsweise Fehlerbehandlung. endPhaseTimeoutSeconds begrenzt diese Phase auf 1–3600 Sekunden, Standard 10.']], headings=['Phase', 'Verhalten'], paragraphs=[
                'Ein Fehler in der Start- oder Hauptphase beendet deren normale Ausführung. Die vorgesehene Endphase wird anschließend zum Aufräumen behandelt. Ein End-Job-Step kann ausdrücklich skip_end_steps setzen; plane notwendiges Aufräumen deshalb bewusst.',
                'Hintergrundskripte und gestartete Hintergrundjobs gehören ihrem übergeordneten Job. Dieser sammelt beziehungsweise beendet seine Hintergrundarbeit vor der Endphase. Ein erfolgreiches Starten beweist noch keinen erfolgreichen Abschluss; ein Hintergrundfehler kann den Besitzer fehlschlagen lassen.']),
            section('verzweigen', 'Mit Bedingungen verzweigen', steps=[
                'Füge If hinzu und wähle eine typisierte Quelle sowie einen passenden Vergleich. All bedeutet: alle Bedingungen müssen zutreffen; Any: mindestens eine.',
                'Füge die Aktionen des ersten Zweigs ein. Ergänze bei Bedarf Else If und Else als alternative Zweige.',
                'Schließe den Block mit End If. Verschachtelte Blöcke müssen jeweils vollständig geschlossen sein.',
                'Prüfe auch den Fall ohne Treffer. Deaktivierte Steps, nicht gewählte Zweige und gültige, noch nicht verfügbare optionale Eingaben sind unterschiedliche Gründe für Nichtausführung.'],
                paragraphs=['Struktur-Steps wie Else und End If lassen sich nicht wie normale Aktionen beliebig deaktivieren. Ergebnisse eines nicht ausgeführten Zweigs sind keine sicheren Quellen für einen späteren Pflichtverbraucher.'],
                links=[('If', '/doku/steps/if/'), ('End If', '/doku/steps/end_if/'), ('Unterlagen-Beispiel', '/beispiele/unterlagen/')]),
            section('kontrolle', 'Fortsetzen, stoppen und Eingaben aufräumen', paragraphs=[
                'Continue Job überspringt den verbleibenden aktuellen Hauptdurchlauf. End Job beendet den Job gemäß seiner Endphasenoption. Timeout wartet; er wiederholt keinen Step und ersetzt keine fachliche Bereitschaftsprüfung.',
                'Bei Eingaben und Makros wirkt der aktuelle Desktopzustand: fokussiere das richtige Fenster und berücksichtige Bildschirmgeometrie. Block Input besitzt eine Sicherheitszeitgrenze; plane das Entsperren ein. Ein Abbruch kann gehaltene Tasten freigeben, macht bereits eingegebene Texte oder verschobene Dateien aber nicht rückgängig.'],
                links=[('Continue Job', '/doku/steps/continue_job/'), ('End Job', '/doku/steps/end_job/'), ('Block Input', '/doku/steps/block_input/')])]),
        dict(id='werte-verbinden', title='Werte verbinden', audience='UI und JSON',
             intro='Eine Eingabe enthält entweder einen direkten Wert, eine wiederverwendbare Variable, eine Geheimnisreferenz oder einen Ergebniswert eines anderen Steps. Anbieter, Typ und Anzahl müssen zum Verbraucher passen.', sections=[
            section('ui', 'Wertequellen in der Oberfläche auswählen', steps=[
                'Öffne die Einstellung des konsumierenden Steps und die Quellenwahl am betreffenden Feld.',
                'Für eine feste Einstellung wähle einen direkten Wert. Für mehrfach verwendete Einstellungen lege eine Job-Variable an. Für berechnete Werte wähle den produzierenden Step und dessen einzelne Ergebnis-Eigenschaft.',
                'Prüfe den Typ: Text, Integer, Number, Boolean, Path, ProcessReference oder ein strukturierter Ergebniswert sind nicht beliebig austauschbar. Ein Text mit der Zahl 10 ist nicht automatisch ein Integer.',
                'Prüfe die Anzahl: Single ist ein Einzelwert, OptionalSingle kann fehlen, Collection ist eine Liste. Der Eingabevertrag bestimmt, ob das erste Element, alle Elemente oder keine Listenauswahl zulässig ist.'],
                paragraphs=['Ein technischer Step-Abschluss ist kein fachlicher Treffer: prüfe beispielsweise found, success oder available. Normale Ergebnisquellen müssen vor dem Verbraucher verfügbar sein. Dynamic ROI ist die ausdrücklich unterstützte Rückkopplungsausnahme; andere Vorwärtsreferenzen werden dadurch nicht erlaubt.']),
            section('anbieter', 'Anbieter und source_id', rows=[
                ['local_value', 'GUID eines Eintrags in localValues. owner_step_id und input_path ordnen ihn genau einer Step-Eingabe zu.'],
                ['job_variable', 'GUID eines Eintrags in variables; der deklarierte Typ und die Kardinalität bleiben maßgeblich.'],
                ['step_result', 'v1/<URI-kodierte Step-ID>/<URI-kodierte stabile Ergebnis-ID>. Beide Teile separat wie Uri.EscapeDataString kodieren, keine Base64-Kodierung.'],
                ['secret', 'Kennung eines vorhandenen Geheimnisses. Nur Felder verwenden, deren Anbieter-/Typvertrag dies erlaubt; nicht jedes Textfeld akzeptiert secret.']], headings=['provider_id', 'Bedeutung'],
                code={'provider_id':'step_result', 'source_id':'v1/unterlagen-0/selected_value'},
                paragraphs=['provider_id/source_id sind der aktuelle Referenzvertrag. Die früheren source_step_id, property_id und property_path bleiben für bestehende Jobs lesbar; für neue Verbindungen stabile Ergebnis-IDs und den Anbieter verwenden. Leere Referenzen sind keine konfigurierten Quellen.']),
            section('lokal', 'Direkte Werte im aktuellen Jobformat', paragraphs=[
                'Der Editor speichert direkte Werte in localValues; inputs des Steps referenziert sie. Das ist keine Ersetzung durch einen eingebetteten value-Wert in inputs. Jede lokale GUID ist im Job eindeutig; owner_step_id muss auf den Besitzer zeigen und input_path den Feld-/Unterfeldpfad benennen.',
                'localValues und variables enthalten id, name, description, scope, value_kind, cardinality und value. scope ist StepValue oder Shared. Enum-Werte ergänzen enum_type_name, enum_values und gegebenenfalls enum_display_names; sie sind Typmetadaten, keine Übersetzung des gespeicherten Werts.',
                'Number-Anteile wie Konfidenz und Deckkraft speichern 0 bis 1, obwohl die UI Prozent zeigt: 90 % entspricht 0.9. Zeit-, Pixel- und Mengenfelder verwenden die Einheit der jeweiligen Feldreferenz.'],
                code=authoring['examples']['valueBindingJob'],
                links=[('Illustrierte Beispielabläufe', '/beispiele/')]),
            section('strukturen', 'Zusammengesetzte Werte und Listen', paragraphs=[
                'Ein ResultBinding kann schema_id sowie members oder items enthalten. members ist ein Objekt aus stabilen Mitglieds-IDs und weiteren Bindings; items ist eine geordnete Liste weiterer Bindings. Die Blattknoten verwenden provider_id/source_id. Ein beliebiger Objektaufbau oder frei erfundener schema_id ist nicht gültig.',
                'Der exportierte Vertragsdatensatz enthält bindingSchemas: zu jeder versionierten Schema-ID die erlaubten Mitglieder, deren ValueKind, Cardinality, NestedSchemaId, AllowedProviderIds und LegacyAllowedProviderIds. ItemSchemaId beschreibt Listen. fieldSchemas ordnet zusammengesetzte Editorfelder ihrem Schema zu.',
                'ROI verwendet desktopautomation.roi/v1 mit enabled, x, y, width, height. Ein Prozessziel verwendet desktopautomation.process-target/v1 mit process_name, executable_path und window_title_contains. Verschachtelte Einblendungs- und Antwortlisten besitzen eigene Schemas. Übernimm dafür die konkreten Vertragsdaten statt die sichtbare UI-Struktur als JSON zu erraten.'],
                links=[('Maschinenlesbare Vertragsdaten', '/doku/vertragsdaten.json'), ('Dynamic ROI', '/doku/steps/dynamic_roi/')]),
            section('fehlen', 'Fehlende Werte richtig behandeln', paragraphs=[
                'FailStep lässt fehlende erforderliche Werte fehlschlagen. SkipStep kann eine gültige, noch nicht verfügbare optionale Step-Quelle mit NoInput überspringen. UseDefault verwendet nur dort den definierten Standard, wo der Eingabevertrag es erlaubt. Eine kaputte Referenz oder ein falscher Typ ist kein harmlos fehlender Wert.',
                'Die Step-Seiten zeigen AcceptedShapes, AllowedProviderIds, MissingValuePolicy und CollectionConsumption. Unbegrenzte Anbieterwahl hebt die Typprüfung nicht auf. Ergebnisse aus einer früheren, nicht mehr passenden Iteration nicht als neue Beobachtung behandeln.'])]),
        dict(id='dateiformate', title='Jobs und Konfigurationen als JSON', audience='JSON und Agenten',
             intro='Die App speichert Jobs, Makros und Automationen als getrennte UTF-8-JSON-Dateien. Diese Anleitung beschreibt den aktuellen Entwicklungsstand, nicht ein beliebiges älteres Release.', sections=[
            section('ablage', 'Ablage und sicherer Bearbeitungsablauf', rows=[
                ['Jobs', r'%APPDATA%\DesktopAutomation\Configs\Job'],
                ['Makros', r'%APPDATA%\DesktopAutomation\Configs\Makro'],
                ['Automationen', r'%APPDATA%\DesktopAutomation\Configs\Automation'],
                ['Profile', r'%APPDATA%\DesktopAutomation\Profiles\<Profil>\Configs\…'],
                ['Logs', r'%LOCALAPPDATA%\DesktopAutomation\Logs\v2']], headings=['Daten', 'Standardpfad'],
                steps=['Erstelle eine Kopie beziehungsweise verwende ein eigenes Testprofil. Bearbeite keine Datei gleichzeitig im geöffneten Editor und extern.',
                       'Erhalte vorhandene Objekt-, Step-, Antwort- und Variablen-IDs beim Ändern. Vergib bei neuen Objekten eindeutige IDs und aktualisiere zugehörige Referenzen.',
                       'Speichere gültiges UTF-8-JSON: keine Kommentare, keine nachgestellten Kommas. Windows-Pfade benötigen doppelte Backslashes im JSON oder müssen vom Serializer geschrieben werden.',
                       'Lade die Datei über den vorgesehenen App-Konfigurationsweg, öffne sie im Editor und behebe die Validierungsfehler. Ein erfolgreicher JSON-Parser prüft noch keine Job-/Triggerregeln.',
                       'Teste den Ablauf manuell mit angepassten Testpfaden. Aktiviere zugehörige Automationen erst danach.']),
            section('job-huelle', 'Die Job-Hülle', code=templates['job'], rows=[
                ['formatVersion', '4 für aktuelle Jobs. Keine Versionsnummer erhöhen, um alte Dateien scheinbar zu migrieren.'],
                ['id / name', 'Eindeutige Job-GUID und Anzeigename. Automations-Ziele und Start-Job-Steps referenzieren die ID.'],
                ['repeating', 'Boolean: Wiederholung der Hauptphase.'],
                ['variables / localValues', 'Typisierte wiederverwendbare Variablen und direkte Step-Werte. IDs, Besitzer und Referenzen konsistent halten.'],
                ['startSteps / steps / endSteps', 'Geordnete Step-Listen je Phase; jeder Step besitzt type und eine eindeutige string-ID.'],
                ['endPhaseTimeoutSeconds', 'Ganzzahl 1–3600; Standard 10. Zeitlimit der Endphase.']], headings=['Feld', 'Bedeutung'],
                paragraphs=['Die leere Hülle illustriert die Struktur; sie ist kein nützlicher ausführbarer Ablauf. Das Wertebeispiel in der Anleitung „Werte verbinden“ zeigt eine vollständige inputs/localValues-Datei.']),
            section('steps', 'Step-Dateien: settings und inputs', paragraphs=[
                'type ist die stabile Step-ID wie timeout, user_choice oder file_system_operation. id ist die Identität dieser Instanz; is_enabled steuert normale Aktionen. Struktur-Steps können zusätzliche abgeleitete Mitglieder wie CanBeDisabled serialisieren; sie sind keine frei nutzbaren Funktionsschalter.',
                'Bei aktuellen Steps mit nicht leerem inputs wird settings vom kanonischen Job-Serializer nicht zusätzlich geschrieben. Aktive Einstellungen stammen aus den Bindings und deren Quellen. Alte beziehungsweise strukturelle Defaultvorlagen mit leerem inputs besitzen weiterhin settings. Mische diese Wege nicht als zwei konkurrierende aktive Konfigurationen.',
                'Jede Step-Seite enthält eine vom Serializer erzeugte strukturelle Defaultvorlage und erklärt auch die verschachtelten settings-Felder. Defaultvorlagen können leere Pflichtwerte enthalten. Für einen vollständigen gültigen Job speichere einen im Editor angelegten Job.'],
                links=[('Step-Referenz', '/doku/steps/'), ('Werte verbinden', '/doku/werte-verbinden/'), ('Validierte Beispiele', '/beispiele/')]),
            section('skalare', 'Schreibweisen, Typen und Einheiten', rows=[
                ['Discriminator', 'Jobs/Makrobefehle: type. Automations-Trigger: $type. Groß-/Kleinschreibung und stabile IDs übernehmen.'],
                ['Enums', 'Viele Fachwerte sind Namen wie Copy oder Ignore. MacroRecordingMode und Hotkey-Modifier sind dagegen Zahlen. Eine globale Umwandlung aller Enums in Strings ist falsch. Die JSON-Vorlage zeigt die tatsächliche Schreibweise.'],
                ['TimeSpan', 'Zeichenfolge wie 00:00:01; wird zum Beispiel für Cooldown, Debounce und Verzögerungen verwendet.'],
                ['Zeitpunkte / Uhrzeit', 'DateTimeOffset mit Offset, z. B. 2030-01-01T08:00:00+00:00; TimeOnly z. B. 08:30:00. Zeitpunkt und tägliche Uhrzeit nicht verwechseln.'],
                ['Zeiten', 'Step-Felder meist ausdrücklich *_ms / *_seconds; Makro duration ist Millisekunden, durationUs und delayBeforeUs sind Mikrosekunden.'],
                ['Zahlen / Booleans / null', 'JSON-Zahlen mit Punkt, true/false ohne Anführungszeichen. null, leere Zeichenfolge und 0 haben unterschiedliche Bedeutungen.'],
                ['Konfidenz / Deckkraft', 'Anteil 0–1 im gespeicherten Zahlenwert; die UI kann Prozent anzeigen.'],
                ['Koordinaten', 'Pixel gemäß jeweiligem Bildschirm-/Koordinatenmodus. Der virtuelle Desktop kann negative Koordinaten haben.']], headings=['Thema', 'Regel']),
            section('vollstaendig', 'Vollständige Dateien und Referenz herunterladen', paragraphs=[
                'Die Vertragsdaten bündeln App-Version, formatVersions, Descriptoren, Eingabeverträge, Ergebnis- und Windows-Kataloge, bindingSchemas, Feld-Schemazuordnungen, Erklärungen und tatsächliche Serializer-Vorlagen. Sie sind ein Referenzdatensatz, kein JSON-Schema und kein Ersatz für die App-Validatoren.',
                'Die vollständige Textreferenz enthält dieselben Erklärungen wie die Website. Für einen Agenten sind stabile IDs und Ergebnisverträge maßgeblich, nicht übersetzte Anzeigenamen.'],
                links=[('Vertragsdaten als JSON', '/doku/vertragsdaten.json'), ('Vollständige Referenz als Markdown', '/doku/referenz.md'), ('Agenten-Einstieg', '/llms.txt')])]),
        dict(id='makros-bearbeiten', title='Makros aufnehmen und bearbeiten', audience='UI und JSON',
             intro='Ein Makro führt eine geordnete Folge von Maus- und Tastaturbefehlen aus. Die Aufnahmeumgebung und die Zeiteinheiten sind Teil des Vertrags.', sections=[
            section('aufnehmen', 'Eine Aufnahme vorbereiten', steps=[
                'Lege ein Makro an und wähle den Aufnahmemodus sowie die zu erfassenden Eingaben. Einstellungen wirken auf zukünftige Aufnahmen.',
                'ClicksOnly erfasst die ausgewählten Eingaben ohne vollständige Bewegungsspur. ScreenAccurateAbsolute erhält absolute Positionen; MotionFaithfulRelative verwendet relative Bewegung.',
                'Prüfe Aufnahmetaste, Mindestintervall und Mindestdistanz. Die Stoppgeste kann aus der Aufnahme entfernt werden; automatische Bewegungsgruppen ordnen die Darstellung.',
                'Beende die Aufnahme und kontrolliere Befehle, Gruppen und Timeline. Wähle für den ersten Test einen leeren Editor und die passende Bildschirmumgebung.',
                'Führe das Makro aus und kontrolliere Logs. Down/Up-Paare vollständig halten; Gruppen ordnen die Darstellung. Maßgeblich für die Wiedergabe bleibt die geordnete Befehlsliste.'],
                links=[('Alle Makrobefehle', '/doku/makros/'), ('Aufnahme und Gruppen', '/doku/makros/recording/')]),
            section('format', 'Makroformat 3', code=templates['macro'], rows=[
                ['formatVersion / id / name', 'Version 3, eindeutige Makro-GUID, Anzeigename.'],
                ['commands', 'Geordnete polymorphe Liste mit type und eindeutigen Befehls-IDs.'],
                ['groups', 'Gruppen-ID, title und automatic. Ein Befehl verweist über groupId auf eine vorhandene Gruppe. Jede Gruppe muss Befehle enthalten; deren Befehle müssen einen zusammenhängenden Block in commands bilden, also nicht A–B–A.'],
                ['recordingSettings', 'mode: 0=ClicksOnly, 1=ScreenAccurateAbsolute, 2=MotionFaithfulRelative. minimumIntervalUs: Mikrosekunden; minimumDistancePixels: Pixel. recordKeyboard, combineKeyboardInputs, recordMouseButtons, removeStopGesture und automaticMovementGroups sind Booleans. recordingHotkeyModifiers ist Bitmaske, recordingHotkeyVirtualKey Windows-Virtual-Key-Zahl.'],
                ['recordedEnvironment', 'Optionale Aufnahmeumgebung: virtualDesktopX/Y/Width/Height beschreiben den virtuellen Desktop, recordedAtUtc den Aufnahmezeitpunkt, startCursorX/Y die optionale Startposition. Geometrie nicht frei erfinden.']], headings=['Feld', 'Bedeutung']),
            section('befehle', 'Zeit, Tasten und Maus', rows=[
                ['timeout.duration', 'Millisekunden: 200 bedeutet 0,2 Sekunden.'],
                ['text_input.durationUs / key_combination.durationUs', 'Mikrosekunden: 200000 bedeutet 0,2 Sekunden.'],
                ['delayBeforeUs', 'Optionale geplante Pause vor einem Befehl in Mikrosekunden.'],
                ['key_down / key_up', 'Tastenkennung, z. B. CONTROL und VK_C. Aliase wie CTRL, STRG, ENTER und ESC werden kanonisch geparst.'],
                ['key_combination.keys', 'Mindestens zwei unterschiedliche Tasten: zuerst Modifier, zuletzt eine Nicht-Modifier-Taste, z. B. ["CONTROL", "VK_C"].'],
                ['mouse_move_absolute', 'x/y im virtuellen Desktop; Mehrmonitor-Layouts können negative Positionen besitzen.'],
                ['mouse_move_relative / mouse_wheel', 'deltaX/deltaY: Bewegungsdifferenz beziehungsweise Rad-Deltas, keine absoluten Zielkoordinaten.'],
                ['mouse_down / mouse_up', 'Passende Maustastenkennung und Gegenbefehl; konkrete erlaubte Namen in der Befehlsreferenz.']], headings=['Befehl/Feld', 'Regel'],
                paragraphs=['Der Executor koordiniert Eingaben exklusiv und gibt von ihm gehaltene Tasten beziehungsweise Maustasten bei Fehler oder Abbruch frei. Er stellt keine bereits veränderten Dateien und Texte wieder her. Bildschirm-/Fokusänderungen können absolute Wiedergaben beeinflussen.'],
                code=templates['macros']['key_combination'], links=[('Tastenkombination', '/doku/makros/key_combination/'), ('Mausbewegung absolut', '/doku/makros/mouse_move_absolute/')])]),
        dict(id='automationen-einrichten', title='Automationen einrichten', audience='UI und JSON',
             intro='Ein Trigger beobachtet etwas. Die Startregeln entscheiden anschließend, ob der ausgewählte Job oder das Makro tatsächlich gestartet wird.', sections=[
            section('ui', 'Ziel und Startregeln festlegen', steps=[
                'Erstelle eine deaktivierte Automation mit einem verständlichen Namen und wähle genau eine Triggerart.',
                'Ergänze die spezifischen Felder: bei Hotkey Taste/Modifier, bei Zeitplan Tage/Uhrzeit, bei Ordnern existierender Pfad/Filter/Ereignis, bei Windows-Ereignissen Funktion und angebotene Parameter.',
                'Wähle einen vorhandenen Job oder ein Makro. Eine gültige Ziel-ID ist erforderlich; ein Anzeigename allein referenziert das Ziel nicht.',
                'Prüfe Cooldown und Zeitfenster. Wähle bewusst das Verhalten bei laufendem Ziel: ignorieren, stoppen, neu starten oder parallel starten.',
                'Teste das Ziel manuell, aktiviere danach die Automation und prüfe Trigger, Entscheidung und Zielrun gemeinsam in Logs.'],
                links=[('Trigger, Aktionen und Startregeln', '/doku/automationen/')]),
            section('huelle', 'Automationformat 1', code=templates['automation'], rows=[
                ['formatVersion / id / name / description / active', 'Version 1, eindeutige GUID, Anzeigename, Beschreibung und Aktivierung. Neue Testdateien active=false lassen.'],
                ['trigger', 'Polymorphes Objekt mit $type. Der kanonische Serializer schreibt zusätzlich Kind/kind als abgeleitete Angaben; die Vorlage unverändert als Struktur nutzen.'],
                ['action', 'name als Anzeige, action_type Job oder Makro, passende job_id beziehungsweise makro_id als GUID. Nicht beide Ziele als aktive Auswahl behandeln.'],
                ['run_policy', 'already_running_behavior, cooldown als TimeSpan sowie optionale enabled_from / enabled_until als tägliche Uhrzeiten.'],
                ['created_at / updated_at / last_run_at', 'Zeitpunkte mit Offset; last_run_at kann null sein. Kein eigener Erfolgsbeleg für den Zieljob.']], headings=['Feld', 'Bedeutung']),
            section('trigger', 'Die elf Triggerarten', rows=[
                ['hotkey', 'Windows-Virtual-Key-Zahl und Modifier-Bitmaske: Alt=1, Ctrl=2, Shift=4, Windows=8; Strg+Alt ist 3.'],
                ['schedule', 'Wochentage und Uhrzeit für wiederkehrende geplante Starts.'],
                ['once_at', 'Einmaliger Zeitpunkt mit Offset; vergangene Zeitpunkte bewusst prüfen.'],
                ['interval', 'Periodischer Abstand als TimeSpan.'],
                ['system_event', 'Eines der angebotenen Systemereignisse, beispielsweise Anwendung oder Sitzung.'],
                ['file_system_event', 'Änderung eines existierenden beobachteten Ordners; Filter, Änderungsart und Unterordner beachten.'],
                ['process_started', 'Beobachteter Prozessstart mit den angebotenen Filtern.'],
                ['process_exited', 'Beobachtetes Prozessende mit den angebotenen Filtern.'],
                ['windows_event', 'Eine vom Windows-Katalog als Ereignis unterstützte Funktion.'],
                ['window_event', 'Beobachtete Fensteränderung mit Ereignisart, Prozess- und Titelfilter.'],
                ['webhook', 'Authentifizierte HTTP-Anforderung an den konfigurierten lokalen Listener.']], headings=['$type', 'Auslöser'],
                paragraphs=['Die tatsächlichen stabilen IDs und konkreten Parameter stehen in den Trigger-Referenzen und Serializer-Vorlagen. Debounce und delay_after_event sind TimeSpan-Werte; Cooldown gehört dagegen zur Startregel.'],
                links=[('Trigger-Katalog', '/doku/automationen/')]),
            section('webhook', 'Webhook verwenden', paragraphs=[
                'Der Listener verwendet standardmäßig Port 17843. Offline bindet lokal; Lan beziehungsweise Online erweitern den Netzwerkzugriff gemäß Konfiguration. Ein Online-Modus richtet selbst keinen Tunnel ein. Die externe HTTPS-Basisadresse darf keine Query oder Fragmentteile enthalten.',
                'Sende POST an /api/v1/webhooks/{hook_id}. Authentifiziere mit X-Webhook-Secret oder Authorization: Bearer <secret>. Der generierte Secretwert besitzt 48 Hexzeichen; die Validierung verlangt mindestens 24 Zeichen. Verwende für deine Installation einen eigenen Secretwert statt des Platzhalters in der Vorlage.',
                'HTTP 202 bestätigt die Annahme, nicht den erfolgreichen Jobabschluss. Prüfe danach AutomationDecision und den verknüpften Zielrun. Portbelegung, Bind-Adresse, Authentifizierung und Startregeln können einen Start verhindern.'],
                code=templates['automations']['webhook'], links=[('Webhook-Feldreferenz', '/doku/automationen/webhook/')])]),
        dict(id='logs-verstehen', title='Logs lesen und auswerten', audience='UI und JSON',
             intro='Logs unterscheiden den Start eines Ablaufs, seinen belegten Abschluss, fachliche Ergebnisse und die Vollständigkeit der gespeicherten Daten.', sections=[
            section('ui', 'Einen Lauf in der UI untersuchen', steps=[
                'Öffne Logs und grenze Quelle, Zeitraum und gegebenenfalls Schweregrad ein. Ein enger Schweregradfilter kann hilfreiche Details verbergen.',
                'Öffne den betreffenden Lauf. Prüfe Outcome, CompletionReason und ob die Daten vollständig gelesen wurden.',
                'Vergleiche Step-Übersicht und Einzelereignisse. Wiederholte erfolgreiche oder übersprungene Hauptphasen-Steps können in StepSummaries aggregiert sein; Warnungen und Fehler bleiben einzeln.',
                'Bei Automationen zuerst Triggerbeobachtung und Startentscheidung lesen, dann den zugehörigen Job-/Makrolauf. Ein abgewiesener Start ist keine ausgeführte Aktion.',
                'Exportiere den Bericht, wenn du den Ablauf weiter untersuchen möchtest. Prüfe vor Weitergabe die enthaltenen strukturierten Pfade und Identitäten.'],
                links=[('Alle Logfelder und Codes', '/doku/logs/')]),
            section('ansichten', 'Die vier Log-Ansichten', rows=[
                ['Übersicht', 'Zeigt aktuelle Läufe und neue Probleme. Die Problemzahl umfasst den gewählten Bestand, nicht nur die sichtbaren Vorschauzeilen.'],
                ['Job-Ausführungen', 'Zeigt konkrete Jobläufe mit Status, Dauer und Step-Verlauf. Normales Öffnen zeigt auch erfolgreiche Läufe; die Problemaktion kann gezielt nur neue Probleme filtern.'],
                ['Automationen', 'Zeigt Beobachtung und Entscheidung je Trigger-ID sowie zugehörige neue oder bereits laufende Ziele. Ein bloßer Startauftrag ohne belegten Lauf bleibt angefordert beziehungsweise unklar.'],
                ['Anwendungsdiagnose', 'Erlaubt Suche nach Quelle, Fachbereich und Schweregrad. Die gezielte Quelle Application schließt Job-/Makro-Ereignisse aus; quellenübergreifende Diagnose berücksichtigt auch Domain-Ereignisse.']], headings=['Ansicht', 'Wann verwenden?'],
                paragraphs=['Beim Lesen beziehungsweise Scrollen kann die sichtbare Live-Aktualisierung pausieren; die Erfassung läuft weiter. Neu passende Ereignisse werden gezählt und können anschließend geladen werden. Weitere Seiten behalten ihren Abfrage-Snapshot, damit neue Ankünfte die Liste nicht verschieben.',
                    'Run-Export verwendet den aktuellen Filter und Checkpoint für alle passenden Läufe, nicht nur die sichtbare Seite. Bei Ereignisexport gelten entsprechend die Ereignisfilter. Nach einer Definitionänderung bleiben frühere Trigger-, Ziel- und Step-Snapshots erhalten; sie werden nicht aus der aktuellen Definition neu erfunden.']),
            section('probleme', 'Neu, Gesehen und Erledigt', paragraphs=[
                'Diese Markierungen beschreiben deinen Bearbeitungsstand eines Problems, nicht die Qualität des ausgeführten Jobs. Neu weist auf noch nicht bestätigte Probleme hin; Gesehen hält fest, dass eine konkrete Beobachtung gelesen wurde; Erledigt ist eine ausdrückliche Bearbeitungsmarkierung.',
                'Nach erfolgreichem Laden einer Problembeobachtung wird nur diese Auswahl bestätigt. Andere ungesehene Probleme desselben Steps oder Laufs bleiben neu. Eine erneut beobachtete gleiche Problem-ID bleibt bestätigt; neue Problem-IDs oder eine höhere Schwere können wieder Aufmerksamkeit benötigen.',
                'Die Markierungen werden getrennt von den unveränderten Logs gespeichert und über Neustarts erhalten. Sie ändern weder ursprüngliche Fehler-/Warnungszahlen noch die Aufbewahrung oder den Export der Ausführungsbelege. Du kannst eine Markierung ausdrücklich wieder öffnen.',
                'Bei Speicherfehlern bleibt die Bestätigung als Fehler sichtbar; die UI soll keinen erfolgreich gespeicherten Status vortäuschen. Bei unvollständiger Evidenz wird eine bestätigte Laufzusammenfassung separat behandelt, statt nicht vorhandene Ereignisse zu erfinden.']),
            section('navigation', 'Vom Log zum betroffenen Step', paragraphs=[
                'Wähle den konkreten Step beziehungsweise die konkrete Ausführung im Laufdetail. Die Navigation verwendet gespeicherte Step-ID, Phase, Position und Ausführungs-ID und prüft dann, ob der aktuelle Job und Step noch existieren.',
                'Gelöschte Definitionen oder nicht mehr verfügbare Pfade werden mit einem Grund angezeigt. Diagnoseaktionen können erlaubte Ordner beziehungsweise Dateien im Explorer anzeigen; sie führen keine im Log genannten Dateien aus. Auch ein nicht ausgeführter Step kann über seinen vorhandenen Snapshot nachvollzogen werden.']),
            section('korrelation', 'Identitäten und Status', rows=[
                ['RunId', 'Identität eines konkreten Laufs; nicht mit Job-/Makro-/Automation-ID verwechseln.'],
                ['Source / SourceId', 'Art und Identität der Ursprungsdefinition.'],
                ['StepId / StepExecutionId', 'Konfigurierter Step und konkrete Ausführung; Wiederholungen können neue Ausführungs-IDs besitzen.'],
                ['ParentRunId / RelatedInstanceIds', 'Verbindung zu übergeordneten beziehungsweise zugehörigen Läufen/Instanzen.'],
                ['Outcome / CompletionReason', 'Belegtes Resultat und Abschlussgrund. Start, Dispatch und eine Ausgabe ersetzen keinen terminalen Abschluss.'],
                ['IsComplete / LostEntries / Issues / ReadState', 'Qualität der Belege. Bei Lücken keinen Erfolg aus fehlenden Fehlerereignissen ableiten.']], headings=['Wert', 'Auswertung'],
                paragraphs=['Nach einem Neustart kann ein vorher offener Lauf als interrupted/incomplete erkannt werden. Ein unbekannter Abschlusszeitpunkt wird nicht erfunden. found=false beziehungsweise available=false im Step-Ergebnis bleibt eine fachliche Aussage, die vom technischen Step-Status getrennt zu prüfen ist.']),
            section('dateien', 'JSONL und Support-Export', paragraphs=[
                'Schema-Version 2 schreibt strukturierte JSONL-Ereignisse, Laufmetadaten und Speicherzustand unter dem lokalen Logs/v2-Verzeichnis. Eine JSONL-Zeile ist ein JSON-Objekt. Context trägt die Zuordnung; Code ist stabiler als der übersetzte Message-Text.',
                'Der ZIP-Export enthält einen Manifest-/Übersichtsbestand, runs.json, JSONL-Ereignisse und report.txt. Der Store wird davor gespült; ein bestehendes Exportziel wird nicht überschrieben. Lies Laufzusammenfassung und Ereignisse gemeinsam, statt nur nach Fehlertexten zu suchen.',
                'Retention hält Ereignisse standardmäßig ungefähr 30 Tage und bis ungefähr 500 MiB. Aktive geschützte Läufe können Grenzen überschreiten. Das Alter richtet sich nach dem letzten Anhängen eines Segments, nicht jeder einzelnen Zeile; Aufräumen erfolgt beim Start, bei Rotation und stündlich. Das Bestätigen eines Log-Hinweises verlängert die Aufbewahrung nicht.',
                'Unlesbare Dateien und bekannte Verluste ergeben Partial/Unavailable beziehungsweise einen unvollständigen Bestand. Die UI kann lesbare Teile weiterhin zeigen. Ältere Logs werden nicht zu einem künstlich vollständigen v2-Lauf zusammengemischt.']),
            section('jsonl-beispiel', 'Ein Ereignis als JSON lesen', code=authoring['examples']['logEvent'], paragraphs=[
                'Dieses synthetische Ereignis wurde mit den echten Log-Serializeroptionen erzeugt. In einer JSONL-Datei steht das Objekt auf einer einzelnen Zeile. PascalCase-Feldnamen wie SchemaVersion, Code und Context sowie Enum-Namen wie Job und Information bleiben erhalten.',
                'Context.RunId verbindet das Ereignis mit dem Lauf, StepId mit der Konfiguration und StepExecutionId mit dieser konkreten Ausführung. step.completed und DurationMs=250 belegen diesen Step-Abschluss; der Abschluss des ganzen Jobs benötigt weiterhin den zugehörigen Laufstatus beziehungsweise run.completed.',
                'Für Abfragen verwenden LogQuery und LogPage die dokumentierten Filter, Snapshot-/Sequenzgrenzen und den Lesezustand. LogStepExecution verbindet Ereignisse, Status, Ursache und gegebenenfalls eine aggregierte Summary.']),
            section('privat', 'Welche Daten aufgezeichnet werden', paragraphs=[
                'Strukturierte Details folgen einer Allowlist. Bekannte geladene Secrets werden maskiert; rohe Eingabetexte, Skriptargumente und Bilder werden nicht pauschal als Datenabbild gespeichert. Erlaubte Dateipfade und Identitäten können dennoch im Bericht stehen.',
                'Für Agenten: Code, typisierte Statuswerte, Run-/Step-IDs und Vollständigkeit auswerten. Message dient Menschen. Verlorene Ereignisse nicht rekonstruieren und unbekannte Endzeitpunkte nicht ersetzen.'],
                links=[('LogEvent', '/doku/logs/LogEvent/'), ('LogRun', '/doku/logs/LogRun/'), ('storage.gap', '/doku/logs/storage.gap/')])]),
        dict(id='windows-funktionen', title='Windows-Funktionen verwenden', audience='UI und JSON',
             intro='Der Windows-Katalog unterscheidet Ereignisse, typisierte Zustandsabfragen und Einstellungsänderungen. Eine Funktion unterstützt nur die im Katalog gesetzten Fähigkeiten.', sections=[
            section('auswahl', 'Die passende Funktion auswählen', steps=[
                'Wähle für eine aktuelle Beobachtung den Step Windows-Zustand abfragen und eine Funktion mit SupportsStateQuery.',
                'Wähle für einen Start bei Änderungen eine Windows-Ereignis-Automation und eine Funktion mit SupportsEvents.',
                'Wähle für eine Änderung den Step Windows-Einstellung ändern und eine Funktion mit SupportsSettingChange.',
                'Ergänze die angebotenen Parameter und nutze vorhandene Geräte, Profile, Drucker oder Energiesparpläne. Prüfe Ergebnisstatus und Fehlercode, bevor du einen Folge-Step darauf aufbaust.'],
                paragraphs=['Ein Ereignis ist keine aktuelle Zustandsabfrage. Eine reine Einstellung bietet nicht automatisch einen Abfragevertrag. Verfügbarkeit hängt von Windows-Version, installierter Hardware/Diensten und Berechtigungen ab. Anforderungen wie RequiresElevation stehen in den Vertragsdaten.'],
                links=[('Windows- und Ergebnisreferenzen', '/doku/werte/'), ('Windows-Zustand', '/doku/steps/windows_state_query/'), ('Windows-Einstellung', '/doku/steps/windows_setting_change/')]),
            section('agent', 'Parameter und Ergebnisse für Textdateien', paragraphs=[
                'Verwende die genaue Capability-ID wie network.connectivity oder audio.master_volume. Parameters enthält Name, Type, DefaultValue, AllowedValues und Grenzen; die Werte werden nicht aus dem Anzeigenamen einer Funktion abgeleitet.',
                'Der jeweilige Ergebnisvertrag beschreibt stabile Property-IDs, Datentypen, Kardinalität und Enum-Werte. Erst Success beziehungsweise den fachlichen Erfolgswert prüfen, dann die abgefragte Eigenschaft auswerten. Unsupported, AccessDenied und Timeout beschreiben fehlgeschlagene Beobachtungen, nicht einen ausgeschalteten oder negativen Systemzustand.'])]),
        dict(id='agenten', title='Dokumentation für Agenten', audience='JSON und Agenten',
             intro='Ein Agent kann diese Referenz ohne Browser lesen und daraus Dateien erstellen oder bestehende Konfigurationen bearbeiten. Die UI- und Dateianleitungen verwenden dieselben kanonischen Verträge.', sections=[
            section('einstieg', 'Die Dokumentation maschinell lesen', links=[
                ('Kompakter Einstieg: llms.txt', '/llms.txt'), ('Vollständiger Text: llms-full.txt', '/llms-full.txt'),
                ('Vollständige Referenz als Markdown', '/doku/referenz.md'), ('Verträge, Erklärungen und Serializer-Vorlagen als JSON', '/doku/vertragsdaten.json')],
                paragraphs=['Lies zuerst Version und Dateiformate, dann den konkreten Step-/Trigger-/Makrovertrag. Die vollständige Referenz enthält alle Feld-, Options-, Eingabe- und Ergebnis-Erklärungen. JSON-Vertragsdaten erlauben gezieltes Nachschlagen; es handelt sich um Referenzdaten, nicht ein ausführbares Programm oder JSON-Schema.',
                    'Für kleine Kontexte enthält llms.txt Links auf einzelne Markdown-Referenzen. Jede Funktionsseite bietet außerdem vertrag.json mit genau ihrem Vertrag, ihren Erklärungen und ihrer Vorlage. Der Referenzindex ordnet stabile IDs diesen URLs zu; so muss ein Agent nicht die gesamte mehrmegabytegroße Referenz lesen.']),
            section('workflow', 'Verlässlicher Bearbeitungsablauf', steps=[
                'Bestimme App-Version und Formatversion der vorhandenen Datei. Behalte bestehende IDs und Eigenschaften, wenn der Auftrag sie nicht ändert.',
                'Wähle stabile type- beziehungsweise $type-IDs aus der Referenz. Für Windows-Funktionen nur unterstützte Capability-IDs und Parameter verwenden.',
                'Modelliere den Datenfluss: Anbieter, Source-ID, Typ, Kardinalität und Produzentenreihenfolge prüfen. Für direkte Werte aktuelle inputs/localValues verwenden; compound schemas nicht frei erfinden.',
                'Prüfe jede Einheit und Enum-Schreibweise an der tatsächlichen JSON-Vorlage. Defaultvorlagen mit leeren Pflichtwerten ergänzen; sie nicht als erfolgreich geprüften Ablauf ausgeben.',
                'Schreibe eine separate UTF-8-Datei, prüfe JSON-Syntax und lade sie zur fachlichen Validierung in die App. Bei Zugriff auf das Repository bestehende Serializer, Migratoren und Validatoren verwenden.',
                'Teste mit passenden Testdaten und deaktivierten Automationen. Prüfe in Logs technischen Status, fachliches Ergebnis und vollständige Belege.',
                'Berichte konkret, ob nur die Datei erzeugt, die Konfiguration fachlich validiert oder der Ablauf tatsächlich ausgeführt wurde.']),
            section('pflege', 'Bei Produktänderungen die Doku mitpflegen', paragraphs=[
                'Agenten im Repository müssen die betroffenen Anleitungen, Referenztexte, Werte-/Optionsbeschreibungen, Dateibeispiele und gegebenenfalls Screenshots in derselben Änderung pflegen. Das gilt auch für reine Verhaltensänderungen ohne neue Felder.',
                'website/Build.ps1 exportiert die kanonischen Verträge und validiert die internen Beispielkonfigurationen. Der Website-Check vergleicht Quellen, Export und Ausgabe und prüft vollständige Erklärungsschlüssel, Verweise und Regressionen. Der vollständige Repository-Check eng/verify.ps1 -Mode Full bleibt das Abschlusskriterium.',
                'Der Generator aktualisiert technische Kataloge und Seiten, schreibt aber keine ungeprüften Verhaltenserklärungen. Neue oder geänderte Konzepte benötigen eine semantische Prüfung durch den bearbeitenden Agenten.'])]),
        dict(id='fehlerbehebung', title='Fehler systematisch eingrenzen', audience='UI und JSON',
             intro='Beginne beim ersten belegten Fehler und prüfe Quelle, fachliche Voraussetzungen und Startentscheidung getrennt.', sections=[
            section('quelle', 'Eine Eingabe fehlt oder hat den falschen Typ', steps=[
                'Öffne den konsumierenden Step und identifiziere das fehlerhafte Feld.',
                'Prüfe provider_id/source_id beziehungsweise die Auswahl im Editor. Existieren Variable, Secret oder Produzent und dessen stabile Ergebnis-ID?',
                'Vergleiche Datentyp und Kardinalität mit dem Eingabevertrag. Prüfe bei localValues Besitzer-ID, input_path und tatsächlichen value-Typ.',
                'Prüfe, ob der Produzent in diesem Zweig und Durchlauf tatsächlich ausgeführt wurde. Eine optionale fehlende Quelle kann überspringen; ein falscher Typ bleibt ein Fehler.']),
            section('datei', 'Eine Dateiaktion schlägt fehl', paragraphs=[
                'Prüfe zuerst die aufgelösten Quell- und Zielpfade, den gewählten Operationsmodus, Existenz, Filter und Schreibrechte. Bei Copy/Move/Rename gelten unterschiedliche Zielfelder; eine Liste von Treffern ist kein einzelner Pfad.',
                'Bei gesperrten Dateien retry_locked_files, retry_count und retry_delay_ms prüfen. Wiederholungen schaffen keine fehlenden Berechtigungen. Arbeite mit einem Testordner und überprüfe die fachlichen Ergebniswerte sowie step.paths, soweit vorhanden.'], links=[('Dateioperation', '/doku/steps/file_system_operation/')]),
            section('start', 'Die Automation startet das Ziel nicht', paragraphs=[
                'Prüfe active und ob der Trigger tatsächlich beobachtet wurde. Prüfe danach AutomationDecision: ungültiges Ziel, Cooldown, Zeitfenster oder ein laufendes Ziel können einen Start verhindern.',
                'Bei Hotkeys Modifier-Bitmaske und Virtual-Key-Code prüfen; bei Ordnern existierendes Verzeichnis und Filter; bei Zeitplänen Zeitpunkt/Uhrzeit und Tage; bei Webhooks Bind-Adresse, Port und Secret. Ein Triggerereignis oder HTTP 202 beweist noch keinen fertigen Zieljob.']),
            section('belege', 'Logs wirken leer oder unvollständig', paragraphs=[
                'Erweitere zunächst Quelle, Zeitraum und Schweregradfilter. Lies auch StepSummaries, weil nicht jeder erfolgreiche wiederholte Step als separate Ereigniszeile gespeichert wird.',
                'Prüfe ReadState, IsComplete, LostEntries und Issues. Ein Aufbewahrungsablauf, nicht lesbare Datei oder Speicherverlust begrenzt die Diagnose. Berichte diese Grenze statt fehlende Ereignisse als Erfolg auszulegen.'])]),
        dict(id='kompatibilitaet', title='Version und Kompatibilität', audience='UI und JSON',
             intro=f"Diese Referenz beschreibt den Repository-Entwicklungsstand {meta['appVersion']} vom 8. Oktober 2026.", sections=[
            section('stand', 'Entwicklungsstand und Download', paragraphs=[
                'Die Beschreibung enthält Änderungen des aktuellen Arbeitsbaums, die noch nicht separat veröffentlicht sein können. Ein unverändertes älteres Download-Release besitzt deshalb möglicherweise nicht alle beschriebenen Felder oder Funktionen.',
                'Aktuelle gespeicherte Formate sind Job 4, Makro 3 und Automation 1. Versionsnummern gehören zum jeweiligen Dateivertrag, nicht zur Website. Bestehende Dateien werden über die vorgesehenen Kompatibilitätswege gelesen; eine manuelle Versionsänderung ist keine Migration.']),
            section('alt', 'Vorhandene Konfigurationen erhalten', paragraphs=[
                'Legacy-settings und ältere Ergebnisreferenzen bleiben über die vorgesehenen Lesepfade unterstützt. Beim Bearbeiten bestehender Dateien Identitäten und Referenzen erhalten und nach dem Laden die fachliche Validierung prüfen.',
                'Die Website-Vorlagen stammen direkt vom Serializer; Defaultwerte sind keine Zusage, dass leere Ziel-/Quellfelder einen ausführbaren Ablauf ergeben. Vollständige Beispiele sind gesondert validiert und müssen für die lokale Umgebung angepasst werden.'], links=[('Dateiformate', '/doku/dateiformate/'), ('Beispielabläufe', '/beispiele/')])])
    ]
