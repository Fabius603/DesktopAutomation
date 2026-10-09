# DesktopAutomation-Website

Öffentliche Adresse: https://desktopautomation.pages.dev

Dieser Ordner enthält die aktuelle Cloudflare-Website im DesktopAutomation-Repository. Die App und die Website werden unabhängig voneinander veröffentlicht.

## Umgesetzter Ausbau

Das [Website- und Dokumentationskonzept](CONCEPT.md) ist umgesetzt: komplexere Beispiele, Light-Screenshots und vollständige Referenzen für Steps, Werte, Automationen, Makros und Logs. Elf Anleitungen unterstützen UI-Bedienung und JSON-Bearbeitung. Agenten können `dist/llms.txt`, `dist/llms-full.txt` und `dist/doku/vertragsdaten.json` lesen. Der [Pflegeablauf](docs/README.md) erklärt die verbindlichen Aktualisierungs- und Vollständigkeitsprüfungen.

## Dateien bearbeiten

- `pages/index.html`: Vorlage der Startseite und Produkttexte.
- `pages/anwendung/index.html`: eigene Anwendungsseite mit Screenshot-Galerie.
- `pages/download/index.html`: eigene Downloadseite mit Installer und Einrichtung.
- `pages/impressum.html`, `pages/datenschutz.html` und `pages/404.html`: Vorlagen der Informationsseiten.
- `tools/build_site.py`: gemeinsamer angepinnter Header, Seitenpfad, aktive Navigation und Footer für alle Seiten.
- `dist/website.css`: Gestaltung und mobile Darstellung.
- `dist/website.js`: Screenshot-Galerie und mobiles Menü.
- `dist/doku/`: generierte Dokumentationsstruktur, Kataloge, Referenzen und Suche.
- `dist/beispiele/`: generierte illustrierte Beispielabläufe ohne Job-Downloads.
- `Build.ps1`, `tools/` und `docs/`: Export, Generator, Renderer, Inhalte und Regressionstests.
- `dist/impressum.html` und `dist/datenschutz.html`: generierte Anbieter- und Datenschutzhinweise.
- `dist/app.ico`: unverändertes App-Symbol aus `DesktopAutomationApp/Assets/App.ico`, auch als Favicon.
- `dist/startseite.png`, `dist/sicherung.png`, `dist/arbeitsplatz.png`, `dist/unterlagen.png`, `dist/automationen.png`, `dist/makro-editor.png` und `dist/verlauf.png`: isoliert gerenderte echte App-Ansichten mit Beispieldaten.
- `dist/404.html`, `dist/_headers`, `dist/robots.txt`, `dist/sitemap.xml`: Fehlerseite, HTTP-Header und Suchmaschinenangaben.
- `wrangler.jsonc`: Cloudflare-Pages-Konfiguration.
- `package.json` und `package-lock.json`: Upload-Werkzeug und festgelegte Abhängigkeiten.

Die redaktionellen Seitenvorlagen liegen in `pages`; CSS und JavaScript liegen in `dist`. Alle HTML-Seiten verwenden den gemeinsamen Header und Footer aus `tools/build_site.py`. Alle fünf Hauptmenüpunkte öffnen eigene Seiten; Anwendung und Download sind keine Sprunglinks mehr. Der Header und der hierarchische Seitenpfad bleiben beim Scrollen sichtbar. Die bisherigen Startseiten-Anker `/#ansichten` und `/#start` bleiben als Einstiege zu den neuen Seiten erreichbar. Die aktive Seite beziehungsweise ihr übergeordneter Bereich ist mit `aria-current` ausgezeichnet. Referenzseiten, Beispiel-Detailseiten, Suchindex und Dokumentationsdateien werden aus `docs` mit `Build.ps1` erzeugt. Generierte HTML-Dateien nicht von Hand bearbeiten. Vor einem Upload `Build.ps1 -Check` ausführen; neue App-Screenshots mit `Build.ps1 -Render` erzeugen und visuell prüfen.

## Auf diesem PC hochladen

Installiere zuerst die Upload-Abhängigkeiten mit `npm ci`. Öffne anschließend PowerShell in diesem Ordner und führe aus:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Deploy.ps1
```

Das Skript prüft zuerst die Website gegen den aktuellen Repository-Stand und verwendet anschließend die vorhandene Cloudflare-Anmeldung. Bei fehlender Anmeldung startet es die Browserfreigabe. Es veröffentlicht `dist` als neue Produktionsversion des bestehenden Projekts `desktopautomation`. Zugangsdaten werden nicht in diesen Ordner geschrieben oder ausgegeben.

## Auf einem anderen PC

Installiere Node.js mit npm. Verwende das vollständige Repository mit .NET 8 und Python 3, damit die verpflichtende Prüfung den aktuellen App-Vertrag exportieren kann. Installiere die Website-Abhängigkeiten im Ordner `website`. Dann:

```powershell
npm ci
npx wrangler login
npm run deploy
```

Melde dich beim Cloudflare-Konto an, in dem das Projekt `desktopautomation` existiert. Die Veröffentlichung erfolgt mit Wrangler, weil `cf pages deploy` den Pages-Datei-Upload in der verwendeten Beta noch nicht unterstützt. Für Projekte mit Wrangler-Konfiguration ist das in den Benutzeranweisungen ausdrücklich vorgesehen.

## Über das Cloudflare-Dashboard

Öffne Workers & Pages → desktopautomation → Create deployment. Wähle Production und lade die Inhalte von `dist` oder deren ZIP-Paket hoch. Bestätige den Upload mit Save and Deploy.

Nur die öffentlichen Dateien in `dist` hochladen. `node_modules`, Skripte und Zugangsdaten gehören nicht in das Upload-Paket.




## Screenshots gezielt pflegen

Der [Screenshot-Ablauf](docs/screenshots.md) beschreibt den Katalog, gezielte Generierung und Sichtprüfung. `Build.ps1 -ListScreenshots` zeigt den Status; `-Screenshots "verlauf,automationen"` rendert einzelne Ansichten, `-ChangedScreenshots` die veralteten und `-Render` alle. Der Makro-Editor wird ebenfalls vom gemeinsamen Renderer erzeugt. Bilder werden automatisch den im Katalog angegebenen Doku-Seiten zugeordnet; der Website-Check verhindert veraltete oder ungeprüfte Bilder.
