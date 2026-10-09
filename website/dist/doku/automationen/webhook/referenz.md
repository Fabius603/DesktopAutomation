## Automationen: Webhook

ID: webhook
Website: /doku/automationen/webhook/

Abbildung: [Detailansicht: Webhook. Markierte Beschriftungen zeigen die Einstellungen im Editor. Die Abbildung zeigt eine Beispielkonfiguration, keine ausgeführte Aktion.](/ref-auto-webhook.detail.png)

1. Trigger-Typ: Hier wählst du, welches Ereignis die Automation startet.
2. Erreichbarkeit: Bestimmt, ob der HTTP-Auslöser lokal oder im Netzwerk erreichbar ist.
3. Port: Port des lokalen HTTP-Listeners.
4. Webhook-URL: Zieladresse des authentifizierten POST-Aufrufs; hier mit Beispieldaten.
5. Webhook-Secret: Authentifizierung des Aufrufs; der abgebildete Wert ist ein erfundenes Beispiel.
6. PowerShell-Aufruf: Kopierbares Aufrufmuster mit POST, Authentifizierung und JSON-Body.
7. Job oder Makro: Hier steht das Ziel, das bei einer passenden Auslösung gestartet wird.
### errors

Eine Triggerbeobachtung ist noch kein erfolgreicher Zielstart. Prüfe active, Ziel-ID, Pflichtparameter, aktives Zeitfenster, Cooldown und bereits laufendes Ziel. Für Webhooks zusätzlich Port, Netzwerkmodus und Authentifizierung prüfen; für Datei-Trigger muss der Ordner existieren. Diagnose und Startentscheidung stehen im Automationsverlauf.

### example

Lege eine deaktivierte Automation an, wähle den beschriebenen Trigger beziehungsweise die Ziel-/Startregel und teste den Zieljob zuerst manuell. Die Dateianleitung zeigt die vollständige Hülle mit trigger, action und run_policy.

### field.hook_id

Stabile GUID des Webhook-Endpunkts /api/v1/webhooks/<hook_id>. Sie ist die Routing-ID und nicht der geheime Zugangsschlüssel.

### field.kind

Abgeleitete Triggerart zur Anzeige. Für die polymorphe Deserialisierung ist $type maßgeblich; die kanonische Vorlage zeigt die zusätzlich geschriebenen Kind/kind-Werte.

### field.kind.option.FileSystemEvent

Start bei einem passenden Datei-/Ordnerereignis.

### field.kind.option.Hotkey

Start über Tastenkombination.

### field.kind.option.Interval

Start in einem positiven Wiederholungsintervall.

### field.kind.option.OnceAt

Einmaliger Start zu einem zukünftigen Zeitpunkt.

### field.kind.option.ProcessExited

Start, wenn ein passender Prozess beendet wird.

### field.kind.option.ProcessStarted

Start, wenn ein passender Prozess neu erscheint.

### field.kind.option.Schedule

Start zu einer Uhrzeit an ausgewählten Wochentagen.

### field.kind.option.SystemEvent

Start bei einem unterstützten Sitzungs-/Energie-/Systemereignis.

### field.kind.option.Webhook

Start durch einen authentifizierten HTTP-POST-Aufruf.

### field.kind.option.WindowEvent

Start bei einem passenden Fensterereignis.

### field.kind.option.WindowsEvent

Start über eine unterstützte Funktion des Windows-Ereigniskatalogs.

### field.network_mode

Offline bindet lokal, Lan erlaubt lokale Netzwerkzugriffe, Online benötigt eine konfigurierte HTTPS-Basisadresse. Eine Online-Einstellung richtet keinen Tunnel automatisch ein.

### field.network_mode.option.Lan

Webhook-Listener für das lokale Netzwerk binden; Zugriff weiterhin mit Secret authentifizieren.

### field.network_mode.option.Offline

Webhook-Listener nur lokal verwenden.

### field.network_mode.option.Online

Externen Zugriff über eine separat eingerichtete HTTPS-Adresse konfigurieren.

### field.online_base_url

Öffentliche HTTPS-Basisadresse ohne Query und Fragment für Online-Webhooks. Bereitstellung/Weiterleitung muss separat eingerichtet sein.

### field.port

Lokaler Listener-Port von 1024 bis 65535; Standard 17843. Er darf nicht bereits von einem anderen Dienst belegt sein.

### field.secret

Webhook-Zugangsschlüssel mit mindestens 24 Zeichen. Beim Aufruf X-Webhook-Secret oder Authorization: Bearer verwenden; kein echtes Secret in öffentlich geteilte Beispieldateien schreiben.

### purpose

Startet die konfigurierte Aktion über POST /api/v1/webhooks/<hook_id>. Der Aufruf muss X-Webhook-Secret oder Authorization: Bearer mit dem gespeicherten Secret senden. HTTP 202 bestätigt eine angenommene Auslösung, nicht den erfolgreichen Abschluss des Jobs; diesen im Verlauf prüfen.

### Vertrag (Metadaten; keine Konfigurationsdatei)
```json
{
  "id": "webhook",
  "name": "Webhook",
  "fields": [
    {
      "id": "hook_id",
      "name": "hook_id",
      "type": "Guid",
      "options": [],
      "defaultValue": null
    },
    {
      "id": "kind",
      "name": "kind",
      "type": "AutomationTriggerKind",
      "options": [
        "Hotkey",
        "OnceAt",
        "Schedule",
        "Interval",
        "ProcessStarted",
        "ProcessExited",
        "WindowEvent",
        "FileSystemEvent",
        "SystemEvent",
        "WindowsEvent",
        "Webhook"
      ],
      "defaultValue": "Webhook"
    },
    {
      "id": "network_mode",
      "name": "network_mode",
      "type": "WebhookNetworkMode",
      "options": [
        "Offline",
        "Lan",
        "Online"
      ],
      "defaultValue": "Offline"
    },
    {
      "id": "online_base_url",
      "name": "online_base_url",
      "type": "String",
      "options": [],
      "defaultValue": ""
    },
    {
      "id": "port",
      "name": "port",
      "type": "Int32",
      "options": [],
      "defaultValue": 17843
    },
    {
      "id": "secret",
      "name": "secret",
      "type": "String",
      "options": [],
      "defaultValue": null
    }
  ]
}
```

### Strukturelle Serializer-Vorlage
```json
{
  "$type": "webhook",
  "Kind": "Webhook",
  "hook_id": "d4fa91c4-b683-2c23-b89c-7ab50f8b606c",
  "network_mode": "Offline",
  "port": 17843,
  "secret": "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD",
  "online_base_url": "",
  "kind": "Webhook"
}
```

