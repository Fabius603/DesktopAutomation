param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))

$baseResx = Join-Path $ProjectRoot 'Resources\Strings.resx'
$enResx = Join-Path $ProjectRoot 'Resources\Strings.en.resx'
[xml]$deXml = [IO.File]::ReadAllText($baseResx)
[xml]$enXml = [IO.File]::ReadAllText($enResx)

$translations = @{
    'Abbrechen'='Cancel'; 'Ablauf'='Sequence'; 'Achse'='Axis'; 'Aktion'='Action'; 'Aktiv'='Active';
    'aktiv / gesamt'='active / total'; 'Aktiv bis'='Active until'; 'Aktiv von'='Active from';
    'Aktive Automationen'='Active automations'; 'Aktualisieren'='Refresh'; 'Alle (AND)'='All (AND)';
    'Alle gefundenen Punkte dieses Steps verwenden'='Use all points found by this step';
    'Alle Instanzen dieses Jobs beenden'='Stop all instances of this job';
    'Alle laufenden Jobs & Makros beenden (F10)'='Stop all running jobs & macros (F10)';
    'AND (alle)'='AND (all)'; 'Anzeigedauer (ms)'='Display duration (ms)';
    'Anzuzeigender Text (leer = entfernen)'='Text to display (empty = remove)'; 'Argumente'='Arguments';
    'Auf Beendigung warten'='Wait for completion'; 'Aufnehmen'='Capture';
    'Aus Detection-Ergebnis'='From detection result'; 'Ausdruck'='Expression';
    'Ausdruck entfernen'='Remove expression'; 'Ausführung'='Execution'; 'Ausführungen'='Executions';
    'Ausführungsregeln'='Execution rules'; 'Auswertung'='Evaluation'; 'Automation löschen'='Delete automation';
    'Automationen'='Automations'; 'Bei Job-Ende entfernen'='Remove when job ends'; 'Beschreibung'='Description';
    'Bildquelle'='Image source'; 'Breite'='Width'; 'Dashboard aktualisieren'='Refresh dashboard';
    'Datei öffnen'='Open file'; 'Dateiname'='File name'; 'Deckkraft'='Opacity'; 'Deinstallieren'='Uninstall';
    'Diese Punkte werden gegen die Bedingung geprüft'='These points are evaluated against the condition';
    'Doppelklick'='Double-click'; 'Eigenschaft'='Property'; 'Eine (OR)'='One (OR)';
    'Ergebnis (Erfolgreich = Prozess läuft) kann in If-Bedingungen ausgewertet werden.'='The result (success = process is running) can be evaluated in if conditions.';
    'Ergebnis (Fenster aktiv) kann in If-Bedingungen ausgewertet werden.'='The result (window active) can be evaluated in if conditions.';
    'Erkennungsquelle'='Detection source'; 'Erstellen'='Create'; 'Farbe'='Color'; 'Fenstermodus'='Window mode';
    'Fenstername'='Window name'; 'Fenstertitel enthält (optional)'='Window title contains (optional)';
    'gesamt'='total'; 'Größe'='Size'; 'GRUNDEINSTELLUNGEN'='BASIC SETTINGS'; 'Höhe'='Height';
    'Hotkey erfassen'='Capture hotkey'; 'Intervall'='Interval'; 'Jetzt ausführen'='Run now';
    'Jetzt updaten'='Update now'; 'Job löschen'='Delete job'; 'Job oder Makro'='Job or macro';
    'Job starten'='Start job'; 'Job stoppen'='Stop job'; 'Job-Logs'='Job logs';
    'Kein aktiver Job/Makro'='No active job/macro'; 'Keine aktive Automation'='No active automation';
    'Keine Konfiguration erforderlich. Beendet den Job sofort – nachfolgende Steps werden nicht mehr ausgeführt.'='No configuration required. Stops the job immediately; subsequent steps are not executed.';
    'Keine Konfiguration erforderlich. Beendet den vorherigen If/ElseIf/Else-Block.'='No configuration required. Ends the previous If/ElseIf/Else block.';
    'Keine Konfiguration erforderlich. Dieser Block wird ausgeführt, wenn keine vorherige Bedingung zutraf.'='No configuration required. This block runs if no previous condition matched.';
    'Klassenname'='Class name'; 'Klick-Typ'='Click type'; 'Koordinaten'='Coordinates';
    'Koordinaten (X / Y)'='Coordinates (X / Y)'; 'Laufende Jobs & Makros'='Running jobs & macros';
    'Letzter Lauf'='Last run'; 'Manuell'='Manual'; 'Manuell eingeben'='Enter manually';
    'Mauszeiger aufnehmen'='Capture mouse pointer'; 'Max. Alter (ms)'='Max. age (ms)';
    'Max. Größe'='Max. size'; 'Maximale Abweichung'='Maximum deviation'; 'Mehrere Treffer'='Multiple matches';
    'Min. Breite'='Min. width'; 'Min. Größe'='Min. size'; 'Min. Höhe'='Min. height';
    'Min. Matches'='Min. matches'; 'Min. Werte'='Min. values'; 'Mindestens einer (OR)'='At least one (OR)';
    'Modus'='Mode'; 'Monitor wählen'='Select monitor'; 'Nah an Referenzpunkt'='Near reference point';
    'Nächste Ausführung'='Next execution'; 'Name'='Name'; 'Nein'='No';
    'Neu starten um Update zu installieren'='Restart to install update'; 'Neue Automation'='New automation';
    'Neuer Job'='New job'; 'Neuer Step'='New step'; 'Neues Makro'='New macro'; 'Öffnen'='Open';
    'Offset / Toleranz'='Offset / tolerance'; 'Ordner im Explorer öffnen'='Open folder in Explorer';
    'Ordner öffnen'='Open folder'; 'Pfad / Programm'='Path / program';
    'Pfad zur .exe oder Programmname'='Path to .exe or program name'; 'Position'='Position';
    'Prozessauslöser'='Process trigger'; 'Prozessname'='Process name'; 'Punkte'='Points';
    'Punktquelle'='Point source'; 'Quell-Step'='Source step';
    'REFERENZPUNKT & TOLERANZ'='REFERENCE POINT & TOLERANCE'; 'Referenzquelle'='Reference source';
    'Reset ab Distanz'='Reset at distance'; 'ROI aktivieren'='Enable ROI'; 'Schriftfarbe'='Font color';
    'Schriftgröße (pt)'='Font size (pt)'; 'Schritt duplizieren'='Duplicate step';
    'Schritt nach oben'='Move step up'; 'Schritt nach unten'='Move step down'; 'Script-Pfad'='Script path';
    'Sekunden'='Seconds'; 'Sofort nach App-Start'='Immediately after app start'; 'Speichern'='Save';
    'Speicherpfad'='Save path'; 'Start'='Home'; 'Step aktivieren / deaktivieren'='Enable / disable step';
    'Step bearbeiten'='Edit step'; 'Step loeschen'='Delete step'; 'Step löschen'='Delete step';
    'STEP-TYP'='STEP TYPE'; 'Toleranz (Pixel)'='Tolerance (pixels)'; 'Trigger-Typ'='Trigger type';
    'Übereinstimmung'='Match'; 'Übersicht'='Overview'; 'Uhrzeit und Tage'='Time and days';
    'Umbenennen'='Rename'; 'Umschalten: Mauspfad aufzeichnen oder nur bei Klicks'='Toggle: record mouse path or clicks only';
    'Update verfügbar'='Update available'; 'Ursprung'='Origin'; 'Vergleichswert'='Comparison value';
    'Verknüpfen mit'='Link to'; 'Verknüpfung'='Link'; 'Verwerfen'='Discard';
    'Verzögerung (s)'='Delay (s)'; 'Vorhersage (ms)'='Prediction (ms)'; 'Wartezeit (ms)'='Wait time (ms)';
    'Wenn bereits aktiv'='If already active'; 'Wert'='Value'; 'Wiederholend'='Recurring';
    'X/Y-Vergleich (X > 100)'='X/Y comparison (X > 100)'; 'X-Offset (Pixel)'='X offset (pixels)';
    'Y-Offset (Pixel)'='Y offset (pixels)'; 'Zeitfenster über Mitternacht werden unterstützt'='Time windows spanning midnight are supported';
    'ZU PRÜFENDE PUNKTE'='POINTS TO CHECK'; 'Zurück'='Back'; '+ Ausdruck hinzufügen'='+ Add expression';
    '+ Bedingung hinzufügen'='+ Add condition'; '+ Punkt hinzufügen'='+ Add point'; '× Entfernen'='× Remove'
}

function Get-Key([string]$value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = (($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)) | ForEach-Object { $_.ToString('X2') }) -join '').Substring(0, 12) }
    finally { $sha.Dispose() }
    "Ui.$hash"
}

function Get-English([string]$value) {
    if ($translations.ContainsKey($value)) { return $translations[$value] }
    $trimmed = $value.TrimEnd(':', ' ')
    if ($translations.ContainsKey($trimmed)) { return $translations[$trimmed] + $value.Substring($trimmed.Length) }
    $value
}

function Add-Entry([xml]$xml, [string]$key, [string]$value) {
    if ($xml.root.data | Where-Object { $_.name -eq $key }) { return }
    $data = $xml.CreateElement('data'); [void]$data.SetAttribute('name', $key)
    [void]$data.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
    $valueNode = $xml.CreateElement('value'); $valueNode.InnerText = $value
    [void]$data.AppendChild($valueNode); [void]$xml.root.AppendChild($data)
}

$pattern = '(?<![\w])(?<attr>(?:[\w:]+\.)?(?:Text|Content|Header|Title|ToolTip|Watermark))="(?<value>[^"{][^"]*)"'
$files = Get-ChildItem $ProjectRoot -Recurse -File -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($file in $files) {
    $content = [IO.File]::ReadAllText($file.FullName); $changed = $false
    $updated = [regex]::Replace($content, $pattern, {
        param($match)
        $decoded = [Net.WebUtility]::HtmlDecode($match.Groups['value'].Value)
        $key = Get-Key $decoded; Add-Entry $deXml $key $decoded; Add-Entry $enXml $key (Get-English $decoded)
        $script:changed = $true
        $match.Groups['attr'].Value + '="{loc:Translate Key=' + $key + '}"'
    })
    if ($changed -and $updated -notmatch 'xmlns:loc=') {
        $updated = [regex]::Replace($updated, '(<(?:Application|UserControl|Window|mah:MetroWindow|ResourceDictionary)\b)', '$1 xmlns:loc="clr-namespace:DesktopAutomationApp.Localization"', 1)
    }
    if ($updated -ne $content) { [IO.File]::WriteAllText($file.FullName, $updated, [Text.UTF8Encoding]::new($false)) }
}

foreach ($deEntry in $deXml.root.data) {
    $enEntry = $enXml.root.data | Where-Object { $_.name -eq $deEntry.name } | Select-Object -First 1
    if ($enEntry -and $translations.ContainsKey([string]$deEntry.value)) {
        $enEntry.value = $translations[[string]$deEntry.value]
    }
}

$settings = [System.Xml.XmlWriterSettings]::new(); $settings.Indent = $true; $settings.Encoding = [Text.UTF8Encoding]::new($false)
$writer = [System.Xml.XmlWriter]::Create($baseResx, $settings); $deXml.Save($writer); $writer.Dispose()
$writer = [System.Xml.XmlWriter]::Create($enResx, $settings); $enXml.Save($writer); $writer.Dispose()
