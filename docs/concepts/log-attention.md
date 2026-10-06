# Aufmerksamkeit in der Logübersicht

Stand: 6. Oktober 2026. Umgesetzt nach ausdrücklichem Auftrag des Nutzers.

## Ziel

Die Übersicht meldet neue, noch nicht angesehene Probleme. Ein bekanntes Problem darf die Person nicht bei jedem Besuch erneut konfrontieren. Der Ausführungsstatus und die unveränderten Logbelege bleiben weiterhin sichtbar.

## Nutzerverhalten

| Zustand | Bedeutung | Anzeige in der Übersicht |
|---|---|---|
| Neu | Das Problem wurde noch nicht angezeigt. | Hinweis mit Anzahl betroffener Ausführungen und Aktion „Neue Probleme ansehen“. |
| Gesehen | Die konkrete Problembeschreibung wurde erfolgreich in der Detailansicht angezeigt. | Kein Aufmerksamkeitshinweis für dieses Problem. Im Verlauf bleibt es auffindbar. |
| Erledigt | Die Person hat bewusst „Als erledigt markieren“ gewählt. | Kein Aufmerksamkeitshinweis; Abschlussstatus im Verlauf. Bedeutet keine automatische Reparatur. |

Das Öffnen von Overview oder Executions allein bestätigt nichts. Erst die erfolgreich geladene, ausgewählte Problemdetailansicht setzt das konkrete Problem auf „Gesehen“. Das gilt auch für den Einstieg über Anwendungsdiagnose oder eine Step-Verknüpfung. Ein fehlgeschlagener Ladevorgang oder das Anzeigen eines erfolgreichen Steps bestätigt kein Problem. Bei mehreren Problemen einer Ausführung wird nur das angezeigte Problem bestätigt.

Der Hinweis lautet zum Beispiel „2 Ausführungen mit neuen Problemen“. Nach dem Anzeigen verschwinden die bestätigten Probleme aus dieser Zählung. Die normale Liste zeigt weiterhin „Mit Warnungen“ oder „Fehlgeschlagen“. Der Filter „Nur Probleme“ umfasst weiterhin alle problematischen Ausführungen, unabhängig vom Lesestatus. Zusätzlich wird „Nur neue Probleme“ angeboten. Eine Aktion „Alle als gesehen markieren“ braucht eine ausdrückliche Benutzeraktion und betrifft nur den geladenen, angezeigten Abfragezeitpunkt.

Eine neue Warnung oder ein neuer Fehler wird auch bei einer bereits angesehenen Ausführung wieder als neu angezeigt. Ein neuer Run erhält immer einen eigenen Lesestatus. Eine wiederholte Beobachtung desselben Problem-IDs erzeugt keinen neuen Hinweis; eine höhere Schwere desselben Problems schon. „Wieder als neu markieren“ erlaubt eine bewusste Wiedervorlage.

## Umsetzung

- Die Anwendungsschicht besitzt einen einzigen `LogAttentionService`, der offene Probleme, Bestätigungen und die Anzahl betroffener Runs liefert. Overview, Detailansichten und zusätzliche Filter konsumieren denselben Dienst.
- Lesestatus gehört in eine separate lokale, atomar geschriebene Benutzerdaten-Datei in `AppPaths.LogAttentionDirectory` (`LocalRoot/LogAttention/attention.json`). Er überlebt Neustarts; unveränderliche Logereignisse und ihre Ergebnisse werden nicht umgeschrieben. Die Umsetzung nutzt die atomare Ersetzung aus `JsonRepository`.
- Schlüssel: stabiler Run-ID und Problem-ID. Die vorhandene Problemkorrelation verbindet fachliche und diagnostische Beobachtungen desselben Fehlers. Bei Belegen ohne Problem-ID dient die stabile Ereignis-ID als Rückfall. Keine Identität aus Nachricht, Datum oder Tabellenposition ableiten.
- Eine Bestätigung speichert den Zustand, den bestätigten Schweregrad und die höchste tatsächlich betrachtete Belegsequenz. Gleichartige Doppelbeobachtungen derselben Problem-ID bleiben bestätigt; eine höhere Schwere oder neue Problem-ID wird erneut sichtbar. Später eingetroffene Probleme außerhalb des betrachteten Checkpoints werden nie nebenbei bestätigt.
- Bei einer problematischen Run-Zusammenfassung ohne erhaltene Einzelbelege ist ein ausdrücklicher Run-Hinweis mit eigenem Revisionsstand nötig. Das bloße Öffnen unvollständiger Details bestätigt diesen Hinweis nicht automatisch. Die Person kann ihn bewusst als gesehen markieren; wachsende Problemzähler oder neue Abschlussprobleme öffnen ihn erneut.
- Die fachlichen Klassifizierungen bleiben bei den bestehenden Logregeln. `LogQueryService.OverviewAsync` liefert eine ergänzende Aufmerksamkeitsprojektion; die bestehende Problemzahl bleibt als Qualitätskennzahl erhalten. Das Frontend darf nicht lokal Warnungen aus der Liste entfernen und daraus die offene Gesamtzahl berechnen.
- Beim nächsten erfolgreichen Statusschreiben werden Bestätigungen nicht mehr erhaltener Runs bereinigt. Bestätigungen erhaltener Runs bleiben unabhängig von Event-Retention bestehen. Alte v1-Logs bleiben ausgeschlossen. Bei fehlender oder beschädigter Bestätigungsdatei gilt „neu“; eine gescheiterte Speicherung darf keinen dauerhaften Erfolg behaupten.
- Supportexporte enthalten weiterhin alle Belege entsprechend ihrem fachlichen Filter. Der lokale Lesestatus ändert weder Ausführungsqualität noch Diagnoseinhalt und wird nicht ungefragt exportiert.

## Abnahme

Angesehene Warnung verschwindet aus dem Banner und bleibt im Verlauf; Zustand bleibt nach Neustart erhalten. Mehrere Probleme werden einzeln bestätigt. Neue Probleme und erhöhte Schwere erscheinen erneut, Doppelbeobachtungen nicht. Andere Einstiege setzen denselben Status. Paging und gleichzeitige Live-Ereignisse bestätigen nur betrachtete Belege. Fehler beim Laden/Speichern und unvollständige Historie bleiben nachvollziehbar. Gelöschte Definitionen verhindern das Anzeigen und Bestätigen erhaltener Probleme nicht.

## Umfang dieses Schritts

Die Überschriftenposition, der Standardfilter und die wiederkehrende Ladeanimation sind separat korrigiert. Der Aufmerksamkeitshinweis zählt jetzt Ausführungen mit neuen Problemen. Die getrennte Bestätigungsdatei speichert Gesehen/Erledigt; der Verlauf und die Ausführungsqualität bleiben erhalten. Die Detailansicht bietet Erledigt und Wiedervorlage; bei fehlenden Belegen muss der Ausführungshinweis ausdrücklich bestätigt werden. In Executions steht der zusätzliche Filter „Nur neue Probleme“ bereit. Die Übersicht bietet eine ausdrückliche Sammelbestätigung für den aktuellen Filter und Abfragezeitpunkt.


## Verifikation und Eigentümer

`LogAttentionTests` prüft Neustart, doppelte Beobachtungen, erhöhte Schwere, mehrere Probleme,
explizite Erledigung/Wiedervorlage, unvollständige Belege, Abfragezeitpunkt, konkurrierende
Bestätigungen, beschädigte oder nicht beschreibbare Statusdateien und konsistente Exportfilter.
`LogScreenRenderingTests` prüft die echten WPF-Aktionen. Hintergrundaktualisierungen bestätigen
keine ausdrücklich wieder auf Neu gesetzte Meldung erneut; ein neuer tatsächlicher Seitenbesuch
oder Auswahlwechsel darf sie wieder als gesehen markieren.

Der gemeinsame fachliche Eigentümer ist `DesktopAutomation.Application/Logging/LogAttentionService.cs`.
Siehe die [dauerhafte Entscheidung](../decisions/2026-10-06-store-log-attention-separately.md).


Beim Öffnen einer Ausführung werden bevorzugt noch nicht angesehene Probleme gewählt.
„Weiteres neues Problem ansehen“ führt gezielt zur nächsten Beobachtung, auch bei mehreren
Problemen desselben Steps. Gesehene Hauptfehler verdrängen dabei keine neue Warnung.
Fehlen Step-Belege für einen Run-bezogenen Fehler, wird dessen Anwendungsdiagnose geöffnet.
