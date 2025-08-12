# 🧰 TaskAutomation Makro – Befehlsspezifikation

Dieses Modul erlaubt die Definition und Ausführung automatisierter Eingabesequenzen (Makros) über JSON-Dateien. Unterstützt werden Mausbewegungen, Mausklicks, Tastendrücke und Pausen.

---

## 📐 Allgemeine Struktur

Die Makro-Datei ist ein JSON-Objekt mit globalen Einstellungen und einem Array von Befehlen:

```json
{
  "desktop_index": 0,
  "adapter_index": 0,
  "commands": [
    {
      "type": "mouse_m_ove",
      "x": 100,
      "y": 200
    }
    // ... weitere Befehle
  ]
}
```
- Jeder Befehl im "commands"-Array ist ein JSON-Objekt mit einem verpflichtenden Feld "type".
- Weitere Felder hängen vom jeweiligen Befehlstyp ab.
---

## ⚙️ Globale Einstellungen

Diese Einstellungen definieren das Verhalten des Makros in Bezug auf die Bildschirmoberfläche.

| Feld | Typ  | Beschreibung      |
|------|------|-------------------|
| desktop_index        | int        | Der Index des Desktops (beginnend bei 0), auf dem das Makro ausgeführt werden soll.      |
| adapter_index        | int        | Der Index des Display-Adapters (beginnend bei 0), der für die Ausführung des Makros verwendet werden soll. |
| commands             | array      | Ein Array von Befehlsobjekten (siehe unten).|

---

## 🖱️ MouseMoveBefehl

Bewegt die Maus an eine bestimmte Position.

```json
{
  "type": "mouse_m_ove",
  "x": 100,
  "y": 200
}
```

| Feld | Typ  | Beschreibung      |
|------|------|-------------------|
| x    | int  | X-Koordinate      |
| y    | int  | Y-Koordinate      |

---

## 🖱️ MouseDownBefehl

Drückt eine Maustaste an einer Position.

```json
{
  "type": "mouse_down",
  "button": "left",
  "x": 150,
  "y": 250
}
```

| Feld   | Typ    | Beschreibung                    |
|--------|--------|---------------------------------|
| button | string | `"left"`, `"right"`, `"middle"` |
| x      | int    | X-Koordinate                    |
| y      | int    | Y-Koordinate                    |

---

## 🖱️ MouseUpBefehl

Lässt eine Maustaste los.

```json
{
  "type": "mouse_up",
  "button": "left",
  "x": 150,
  "y": 250
}
```

| Feld   | Typ    | Beschreibung                    |
|--------|--------|---------------------------------|
| button | string | `"left"`, `"right"`, `"middle"` |
| x      | int    | X-Koordinate                    |
| y      | int    | Y-Koordinate                    |

---

## ⌨️ KeyDownBefehl

Drückt eine Taste auf der Tastatur.

```json
{
  "type": "key_down",
  "key": "A"
}
```

| Feld | Typ    | Beschreibung                          |
|------|--------|---------------------------------------|
| key  | string | z. B. `"A"`, `"Enter"`, `"Ctrl"` usw. |

---

## ⌨️ KeyUpBefehl

Lässt eine Taste auf der Tastatur los.

```json
{
  "type": "key_up",
  "key": "A"
}
```

| Feld | Typ    | Beschreibung                          |
|------|--------|---------------------------------------|
| key  | string | z. B. `"A"`, `"Enter"`, `"Ctrl"` usw. |

---

## ⏱️ TimeoutBefehl

Wartet eine definierte Zeit in Millisekunden.

```json
{
  "type": "timeout",
  "duration": 1000
}
```

| Feld     | Typ  | Beschreibung                 |
|----------|------|------------------------------|
| duration | int  | Zeit in Millisekunden (ms)   |

---

## 🧪 Beispiel: Vollständige Makrosequenz

```json
[
  { "type": "mouse_move", "x": 300, "y": 400 },
  { "type": "mouse_down", "button": "left", "x": 300, "y": 400 },
  { "type": "mouse_up", "button": "left", "x": 300, "y": 400 },
  { "type": "timeout", "duration": 1000 },
  { "type": "key_down", "key": "A" },
  { "type": "key_up", "key": "A" }
]
```

---

## 📝 Hinweise

- Die `"type"`-Eigenschaft muss exakt mit dem Klassennamen des Befehls übereinstimmen.
- Die JSON-Datei muss ein gültiges Array von Befehlen enthalten.
- Die Koordinaten (x, y) müssen relativ zum verwendeten Bildschirm angegeben werden.
- Die Tastennamen müssen mit dem verwendeten Eingabe-Framework kompatibel sein (z. B. `WindowsInput.Native.VirtualKeyCode`).

---