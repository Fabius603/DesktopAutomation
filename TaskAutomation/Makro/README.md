# 🧰 TaskAutomation Makro – Befehlsspezifikation

Dieses Modul erlaubt die Definition und Ausführung automatisierter Eingabesequenzen (Makros) über JSON-Dateien. Unterstützt werden Mausbewegungen, Mausklicks, Tastendrücke und Pausen.

---

## 📐 Allgemeine Struktur

Die Makro-Datei ist ein JSON-Objekt mit globalen Einstellungen und einem Array von Befehlen:

```json
{
  "static_desktop": true,
  "desktop_index": 0,
  "adapter_index": 0,
  "commands": [
    {
      "type": "mouseMove",
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
| static_desktop       | boolean    | Wenn true, werden die Werte von desktop_index und adapter_index verwendet. Das Makro wird dann auf dem spezifisch angegebenen Desktop und Adapter ausgeführt. Wenn false, werden desktop_index und adapter_index ignoriert, und die Zielinformationen müssen extern bereitgestellt werden.      |
| desktop_index        | int        | Der Index des Desktops (beginnend bei 0), auf dem das Makro ausgeführt werden soll. Wird nur verwendet, wenn static_desktop true ist.      |
| adapter_index        | int        | Der Index des Display-Adapters (beginnend bei 0), der für die Ausführung des Makros verwendet werden soll. Wird nur verwendet, wenn static_desktop true ist.|
| commands             | array      | Ein Array von Befehlsobjekten (siehe unten).|

---

## 🖱️ MouseMoveBefehl

Bewegt die Maus an eine bestimmte Position.

```json
{
  "type": "mouseMove",
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
  "type": "mouseDown",
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
  "type": "mouseUp",
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
  "type": "keyDown",
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
  "type": "keyUp",
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
  { "type": "mouseMove", "x": 300, "y": 400 },
  { "type": "mouseDown", "button": "left", "x": 300, "y": 400 },
  { "type": "mouseUp", "button": "left", "x": 300, "y": 400 },
  { "type": "timeout", "duration": 1000 },
  { "type": "keyDown", "key": "A" },
  { "type": "keyUp", "key": "A" }
]
```

---

## 📝 Hinweise

- Die `"type"`-Eigenschaft muss exakt mit dem Klassennamen des Befehls übereinstimmen.
- Die JSON-Datei muss ein gültiges Array von Befehlen enthalten.
- Die Koordinaten (x, y) müssen relativ zum verwendeten Bildschirm angegeben werden.
- Die Tastennamen müssen mit dem verwendeten Eingabe-Framework kompatibel sein (z. B. `WindowsInput.Native.VirtualKeyCode`).

---