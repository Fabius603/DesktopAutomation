# 🧩 TaskAutomation Jobs – Konfigurationsformat

Dieses Modul erlaubt die Definition mehrstufiger Automatisierungs-Jobs in Form von JSON-Dateien. Jeder Job besteht aus einer Folge von `steps`, die verschiedene Aufgaben ausführen – z. B. Bildschirmaufnahme, Template-Matching, Bildanzeige oder Makro-Ausführung.

---

## 📐 Grundstruktur

```json
{
  "name": "TestJob",
  "repeating": true,
  "steps": [
    {
      "type": "step_typ",
      "settings": { ... }
    }
  ]
}
```

| Feld       | Typ      | Beschreibung                                  |
|------------|----------|-----------------------------------------------|
| name       | string   | Name des Jobs                                 |
| repeating  | boolean  | Wiederholt den Job nach Abschluss             |
| steps      | array    | Liste von einzelnen Steps in Ausführungsreihenfolge |

---

## 🧱 Verfügbare Step-Typen

### 🔹 `process_duplication`

Nutzt DXGI zur Fensteraufnahme eines laufenden Prozesses.

```json
{
  "type": "process_duplication",
  "settings": {
    "process_name": "discord"
  }
}
```

| Feld          | Typ    | Beschreibung                         |
|---------------|--------|--------------------------------------|
| process_name  | string | Name des Prozesses (z. B. `"notepad"`) |

---

### 🔹 `desktop_duplication`

Erfasst einen Monitorbereich per DirectX-Bildschirmduplikation.

```json
{
  "type": "desktop_duplication",
  "settings": {
    "desktop_idx": 0
  }
}
```

| Feld                   | Typ   | Beschreibung                     |
|------------------------|-------|----------------------------------|
| desktop_idx          | int   | Index des Monitors               |

---

### 🔹 `template_matching`

Führt ein Bildvergleichsverfahren mittels OpenCV durch.

```json
{
  "type": "template_matching",
  "settings": {
    "template_path": "Pfad/zur/Vorlage.png",
    "template_match_mode": "SqDiffNormed",
    "multiple_points": false,
    "confidence_threshold": 0.95,
    "roi": { "x": 0, "y": 300, "width": 400, "height": 400 },
    "enable_roi": true,
    "draw_results": true
  }
}
```

| Feld                   | Typ     | Beschreibung                                                  |
|------------------------|---------|---------------------------------------------------------------|
| template_path          | string  | Pfad zur Bildvorlage                                          |
| template_match_mode    | string  | Vergleichsverfahren (`SqDiff`, `CCorrNormed`, etc.)           |
| multiple_points        | bool    | Suche nach mehreren Treffern                                  |
| confidence_threshold   | double  | Mindestwert für Übereinstimmung (0.0 – 1.0)                   |
| roi                    | Objekt  | Region of Interest: `{x, y, width, height}`                   |
| enable_roi             | bool    | Aktiviert die ROI-Beschränkung                                |
| draw_results           | bool    | Markiert gefundene Stellen im Bild                            |

---

### 🔹 `show_image`

Zeigt das Ergebnis eines vorherigen Schritts in einem Fenster.

```json
{
  "type": "show_image",
  "settings": {
    "window_name": "MyWindow",
    "show_raw_image": true,
    "show_processed_image": true
  }
}
```

| Feld                | Typ    | Beschreibung                             |
|---------------------|--------|------------------------------------------|
| window_name         | string | Titel des Fensters                       |
| show_raw_image      | bool   | Zeigt das Originalbild                   |
| show_processed_image| bool   | Zeigt das weiterverarbeitete Bild       |

---

### 🔹 `video_creation`

Speichert den Job-Ablauf als Videodatei (z. B. für spätere Analyse oder Dokumentation).

```json
{
  "type": "video_creation",
  "settings": {
    "save_path": "Pfad/zum/Ordner",
    "file_name": "output.mp4",
    "use_raw_image": false,
    "use_processed_image": true
  }
}
```

| Feld                | Typ    | Beschreibung                             |
|---------------------|--------|------------------------------------------|
| save_path           | string | Zielverzeichnis für das Video            |
| file_name           | string | Dateiname (inkl. `.mp4`)                 |
| use_raw_image       | bool   | Verwendet unbearbeitetes Bild            |
| use_processed_image | bool   | Verwendet verarbeitetes Bild             |

---

### 🔹 `makro_execution`

Startet ein vordefiniertes Eingabe-Makro (z. B. Mausklicks, Tastenanschläge).

```json
{
  "type": "makro_execution",
  "settings": {
    "makro_name": "mein_makro.json"
  }
}
```

| Feld        | Typ    | Beschreibung                          |
|-------------|--------|---------------------------------------|
| makro_name  | string | Name der Datei ohne Dateiendung   |

---

## 📄 Beispiel-Job

```json
{
  "name": "Test",
  "repeating": true,
  "steps": [
    {
      "type": "process_duplication",
      "settings": {
        "process_name": "discord"
      }
    },
    {
      "type": "template_matching",
      "settings": {
        "template_path": "C:\\Users\\fjsch\\Pictures\\Screenshots\\Screenshot 2025-05-22 202830.png",
        "template_match_mode": "SqDiffNormed",
        "multiple_points": false,
        "confidence_threshold": 0.95,
        "roi": {
          "x": 0,
          "y": 300,
          "width": 400,
          "height": 400
        },
        "enable_roi": true,
        "draw_results": true
      }
    },
    {
      "type": "show_image",
      "settings": {
        "window_name": "Test",
        "show_raw_image": false,
        "show_processed_image": true
      }
    },
    {
      "type": "video_creation",
      "settings": {
        "use_raw_image": false,
        "use_processed_image": true
      }
    }
  ]
}
```

---


