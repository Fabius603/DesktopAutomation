# Datenschutzerklärung für DesktopAutomation

Stand: 7. Oktober 2026

## 1. Anbieter und Kontakt

Anbieter von DesktopAutomation und verantwortlich für die in dieser Erklärung beschriebene Verarbeitung personenbezogener Daten durch den Anbieter:

Fabius Jo Schlieper  
Breite Straße 3  
33602 Bielefeld  
Deutschland  
E-Mail: [fjschlieper@gmail.com](mailto:fjschlieper@gmail.com)

Diese Erklärung gilt für die Windows-Anwendung DesktopAutomation, einschließlich der Microsoft-Store-Version und der direkt heruntergeladenen Version, sowie für die Bearbeitung von Supportanfragen durch den Anbieter. Sie beschreibt auch externe Dienste, die bei Installation, Downloads oder Support genutzt werden.

## 2. Lokale Verarbeitung auf Ihrem Gerät

DesktopAutomation dient der Automatisierung von Arbeitsabläufen. Die Anwendung verarbeitet die für Ihre eingerichteten Funktionen erforderlichen Daten grundsätzlich lokal auf Ihrem Gerät. Ein Benutzerkonto beim Anbieter ist für die lokale Nutzung nicht erforderlich.

Abhängig von den verwendeten Funktionen können insbesondere folgende Daten verarbeitet werden:

- Einstellungen, Jobs, Makros, Automationen und Variablen;
- aufgezeichnete Maus- und Tastatureingaben;
- Informationen über Fenster, Prozesse, Dateien und Dateipfade;
- Bildschirmaufnahmen, Bilder, Kamerabilder und daraus erkannte Texte oder Objekte;
- von Ihnen hinterlegte Zugangsdaten und andere geheime Werte;
- Ausführungsverläufe, Diagnoseinformationen und Fehlermeldungen.

Bildschirmaufnahmen, Eingaben und andere Inhalte können personenbezogene Daten enthalten. Welche Inhalte verarbeitet werden, hängt von Ihren eingerichteten Abläufen ab. Kamera-, Aufnahme- und Erkennungsfunktionen dienen den von Ihnen ausgewählten Aufgaben.

Texterkennung und Objekterkennung erfolgen lokal. Die zu erkennenden Bilder werden dafür nicht an einen Cloud-Erkennungsdienst übermittelt. Der Anbieter erhält durch die gewöhnliche lokale Nutzung keinen Zugriff auf Ihre Konfigurationen, Aufnahmen, Zugangsdaten oder Ausführungsprotokolle.

Bei der Verarbeitung von Daten anderer Personen, insbesondere im beruflichen Einsatz, bestimmt der jeweilige Nutzer beziehungsweise die einsetzende Organisation Zwecke, Inhalte und gegebenenfalls die erforderliche Rechtsgrundlage dieser Verarbeitung.

## 3. Speicherung und Schutz lokaler Daten

Anwendungsdaten werden insbesondere in folgenden Verzeichnissen Ihres Windows-Benutzerkontos gespeichert:

- `%APPDATA%\DesktopAutomation`
- `%LOCALAPPDATA%\DesktopAutomation`

Gesonderte Profile verwenden entsprechende Unterverzeichnisse. Durch Automationen erzeugte Dateien, Bilder oder Videos können außerdem an von Ihnen festgelegten Speicherorten abgelegt werden.

Geheime Werte aus der Zugangsdatenverwaltung werden mithilfe der Windows-Funktion DPAPI verschlüsselt und an Ihr Windows-Benutzerkonto gebunden. Dieser Schutz gilt nicht automatisch für sämtliche Konfigurationen, Protokolle oder selbst erzeugten Dateien.

Lokale Protokolle dienen der Fehleranalyse und der Nachvollziehbarkeit von Ausführungen. Bekannte geheime Werte und bestimmte erkennbare Zugangsdatenmuster werden maskiert. Dennoch können Protokolle personenbezogene Angaben enthalten, etwa in Namen, Dateipfaden oder Fehlermeldungen. Die Anwendung übermittelt diese Protokolle nicht automatisch an den Anbieter.

## 4. Internetverbindungen und externe Dienste

### Microsoft Store und Windows

Bei Installation und Aktualisierung über den Microsoft Store verarbeitet Microsoft die dafür erforderlichen Daten. Auch Windows kann entsprechend Ihren Systemeinstellungen Diagnoseinformationen verarbeiten. Für diese Verarbeitung gilt die [Datenschutzerklärung von Microsoft](https://privacy.microsoft.com/de-de/privacystatement).

Microsoft kann dem Anbieter im Partner Center Berichte über die Verbreitung, Nutzung und technische Zuverlässigkeit der Anwendung bereitstellen, beispielsweise Statistiken zu Installationen und technische Fehler- oder Absturzinformationen. Welche Berichte tatsächlich verfügbar sind, hängt unter anderem von Microsofts Diensten, der Anwendung und den Diagnoseeinstellungen ab. DesktopAutomation enthält hierfür keine eigene zusätzliche Analyseintegration.

Soweit der Anbieter solche Berichte nutzt, dient dies der Beurteilung der Verbreitung und der Verbesserung der Stabilität der Anwendung. Soweit dabei personenbezogene Daten durch den Anbieter verarbeitet werden, beruht dies auf Art. 6 Abs. 1 lit. f DSGVO. Das berechtigte Interesse liegt in der Fehlerbehebung und der zuverlässigen Bereitstellung der Software. Die Bereitstellung und Aufbewahrung im Partner Center werden durch Microsoft bestimmt; eigene personenbezogene Kopien werden nur so lange aufbewahrt, wie sie für diese Zwecke erforderlich sind.

### Updates und Modelldownloads über GitHub

Die direkt heruntergeladene und mit Velopack installierte Version nutzt GitHub zur Prüfung und zum Herunterladen von Updates. Die Microsoft-Store-Version nutzt diesen Velopack-Updateweg nicht.

Beim Herunterladen angebotener Modelle zur lokalen Objekterkennung entstehen ebenfalls Verbindungen zu GitHub und den zugehörigen Downloadservern. Die heruntergeladenen Modelle werden anschließend lokal verwendet.

Die jeweiligen Dienste erhalten technisch erforderliche Verbindungsinformationen, insbesondere Ihre IP-Adresse und Angaben zur Anfrage. Es gelten die [Datenschutzhinweise von GitHub](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

### Von Ihnen eingerichtete Automationen

DesktopAutomation kann andere Programme starten, Skripte ausführen, Eingaben erzeugen und Dateien bearbeiten. Wenn Sie dadurch externe Anwendungen oder Online-Dienste bedienen, können Daten entsprechend Ihrer Konfiguration an diese weitergegeben werden. Empfänger und Inhalte hängen von Ihren Abläufen ab. Für die weitere Verarbeitung gelten die Datenschutzhinweise der jeweiligen Dienste.

## 5. Keine eigene Werbeverfolgung

DesktopAutomation enthält keine integrierte Werbeverfolgung und keine eigene automatische Übermittlung von Nutzungsanalysen, Bildschirmaufnahmen oder lokalen Ausführungsprotokollen an den Anbieter. Davon unabhängig können die in Abschnitt 4 beschriebenen Dienste Daten nach ihren eigenen Datenschutzhinweisen verarbeiten.

## 6. Support über GitHub und E-Mail

Support ist über [GitHub Issues](https://github.com/Fabius603/DesktopAutomation/issues) und [GitHub Discussions](https://github.com/Fabius603/DesktopAutomation/discussions) vorgesehen. Für vertrauliche Anliegen und Datenschutzanfragen können Sie die oben genannte E-Mail-Adresse verwenden.

Der Anbieter verarbeitet die von Ihnen übermittelten Angaben zur Bearbeitung Ihrer Anfrage. Dazu können Ihr GitHub-Nutzername, öffentliche Profilinformationen, Ihre E-Mail-Adresse, Nachrichten sowie freiwillig beigefügte Dateien gehören.

**Beiträge und Anhänge in öffentlichen GitHub Issues und Discussions sind öffentlich sichtbar.** Sie können von anderen Personen gelesen, kopiert und durch Suchmaschinen erfasst werden. Bitte veröffentlichen Sie dort keine Passwörter, vertraulichen Informationen oder personenbezogenen Daten anderer Personen. Prüfen und bereinigen Sie Protokolle und Aufnahmen vor einer Weitergabe.

Für GitHub gelten die oben verlinkten Datenschutzhinweise. E-Mails an den Anbieter werden über Gmail von Google empfangen und bearbeitet. Hierfür gilt die [Datenschutzerklärung von Google](https://policies.google.com/privacy?hl=de).

Die Bearbeitung von Anfragen erfolgt auf Grundlage von Art. 6 Abs. 1 lit. b DSGVO, soweit sie einen Vertrag oder vorvertragliche Maßnahmen betrifft. Bei sonstigen Supportanfragen erfolgt sie auf Grundlage von Art. 6 Abs. 1 lit. f DSGVO. Das berechtigte Interesse besteht in der Beantwortung von Anfragen, der Fehleranalyse und der Verbesserung der Software. Die Bereitstellung von Angaben ist freiwillig; ohne die zur Klärung erforderlichen Informationen kann eine Anfrage möglicherweise nicht bearbeitet werden.

Personenbezogene Supportdaten werden gelöscht, sobald sie für die Bearbeitung und erforderliche Nachverfolgung nicht mehr benötigt werden. Sie können außerdem jederzeit eine Löschung anfragen. Gesetzliche Aufbewahrungspflichten oder die erforderliche Geltendmachung, Ausübung oder Verteidigung von Rechtsansprüchen können einer sofortigen Löschung entgegenstehen.

Bei GitHub kann eine Löschung oder Bearbeitung zusätzlich Maßnahmen durch Sie oder GitHub erfordern. Der Anbieter kann nicht gewährleisten, dass bereits von Dritten angefertigte Kopien oder Suchmaschinen-Caches entfernt werden. Technische Problemlösungen können ohne die dafür nicht mehr benötigten personenbezogenen Angaben weiter dokumentiert werden.

## 7. Verarbeitung außerhalb der EU und des EWR

Bei der Nutzung von Microsoft, GitHub oder Google kann eine Verarbeitung auch außerhalb der Europäischen Union oder des Europäischen Wirtschaftsraums, insbesondere in den USA, stattfinden. Informationen zu den beteiligten Unternehmen, Verarbeitungsländern und den jeweils angewandten Übermittlungsgrundlagen und Schutzmaßnahmen finden Sie in den verlinkten Datenschutzhinweisen dieser Anbieter.

## 8. Speicherdauer und Löschung lokaler Daten

Lokale Konfigurationen, gespeicherte Zugangsdaten und erzeugte Dateien bleiben grundsätzlich gespeichert, bis Sie diese löschen. Für strukturierte Ereignisprotokolle bestehen automatische Alters- und Größenbegrenzungen. Dies bedeutet nicht, dass alle Protokolltypen, Ausführungsübersichten oder exportierten Dateien nach derselben Frist gelöscht werden.

**Die Deinstallation entfernt die gespeicherten Benutzerdaten nicht automatisch.** Zur vollständigen Entfernung können Sie nach dem Beenden der Anwendung die in Abschnitt 3 genannten Datenverzeichnisse sowie selbst gewählte Ausgabeordner und exportierte Dateien löschen. Dabei gehen die dort gespeicherten Konfigurationen und Daten verloren.

Sicherungen, synchronisierte Kopien und bereits an externe Dienste übermittelte Daten müssen gegebenenfalls gesondert gelöscht werden. Der Anbieter kann ausschließlich auf Ihrem Gerät gespeicherte Daten nicht für Sie einsehen oder löschen.

## 9. Ihre Datenschutzrechte

Soweit der Anbieter personenbezogene Daten über Sie verarbeitet, haben Sie unter den gesetzlichen Voraussetzungen Rechte auf Auskunft, Berichtigung, Löschung, Einschränkung der Verarbeitung und Datenübertragbarkeit.

Gegen eine Verarbeitung auf Grundlage von Art. 6 Abs. 1 lit. f DSGVO können Sie aus Gründen Ihrer besonderen Situation Widerspruch einlegen. Soweit eine Verarbeitung auf einer Einwilligung beruht, können Sie diese jederzeit mit Wirkung für die Zukunft widerrufen.

Sie können sich bei einer Datenschutzaufsichtsbehörde beschweren, insbesondere an Ihrem gewöhnlichen Aufenthaltsort, Ihrem Arbeitsplatz oder am Ort des vermuteten Verstoßes. Für den Anbieter in Nordrhein-Westfalen ist die [Landesbeauftragte für Datenschutz und Informationsfreiheit Nordrhein-Westfalen](https://www.ldi.nrw.de/) zuständig.

Zur Ausübung Ihrer Rechte gegenüber dem Anbieter wenden Sie sich an [fjschlieper@gmail.com](mailto:fjschlieper@gmail.com). Für Verarbeitungen durch Microsoft, GitHub oder Google können Sie sich auch unmittelbar an den jeweiligen Dienst wenden.

Der Anbieter verwendet die beschriebenen Daten nicht für Profiling oder automatisierte Entscheidungen mit rechtlicher oder ähnlich erheblicher Wirkung.

## 10. Änderungen dieser Erklärung

Diese Erklärung wird angepasst, wenn sich die beschriebenen Funktionen oder Datenverarbeitungen ändern. Die aktuelle Fassung ist unter [Datenschutzerklärung für DesktopAutomation](https://github.com/Fabius603/DesktopAutomation/blob/master/PRIVACY.md) verfügbar.
