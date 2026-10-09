"""Build static reference pages from exported contracts; never invent product rules."""
from pathlib import Path
from html import escape as esc
from html.parser import HTMLParser
from urllib.parse import urlsplit, unquote
import argparse
import hashlib
import json
import sys
from functools import lru_cache
from guide_content import guides

ROOT = Path(__file__).resolve().parents[2]
SITE = ROOT / 'website'
DIST = SITE / 'dist'
DOCS = SITE / 'docs'
GROUPS = {'steps': 'Steps', 'automationen': 'Automationen', 'makros': 'Makros', 'logs': 'Logs', 'werte': 'Werte und Ergebnisse'}


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def text(value):
    if value is None:
        return 'Nicht angegeben'
    if isinstance(value, str):
        return value or 'Leerer Text'
    return json.dumps(value, ensure_ascii=False, separators=(', ', ': '))


def code(value):
    return '<code>' + esc(text(value)) + '</code>'


def anchor(prefix, value, label):
    target = esc(prefix + value, quote=True)
    return '<a id="' + target + '" href="#' + target + '">' + label + '</a>'


def table(head, rows):
    return '<div class="table-scroll"><table><thead><tr>' + ''.join('<th scope="col">' + esc(h) + '</th>' for h in head) + '</tr></thead><tbody>' + ''.join('<tr>' + ''.join('<td>' + c + '</td>' for c in row) + '</tr>' for row in rows) + '</tbody></table></div>'


# UI explanations share the editorial source with the machine reference.
# Binding bookkeeping remains in the technical section; actual nested settings do not.
BINDING_PARTS = {'provider_id', 'source_id', 'schema_id', 'value_path', 'property_id', 'property_path', 'source_step_id', 'members', 'items'}
UI_NAMES = {
    'camera_id': 'Kamera › Gerätezuordnung', 'camera_name': 'Kamera › Anzeigename',
    'quality_mode': 'Kamera › Qualität', 'frames_per_second': 'Kamera › Bildrate (FPS)',
    'pixel_format': 'Kamera › Pixelformat', 'model': 'YOLO-Modell', 'class_name': 'Objektklasse',
    'enable_roi': 'Festen Bildbereich verwenden', 'monitor_device_name': 'Monitor-Kennung',
    'target': 'Prozessreferenz', 'query': 'Prozesssuche', 'roi': 'Bildbereich', 'overlay': 'Einblendung',
    'text_results': 'Textergebnis', 'detection_results': 'Erkennungen', 'conditions': 'Bedingungen',
    'comparison': 'Vergleich', 'comparison_value': 'Vergleichswert', 'value': 'Wert', 'operator': 'Operator',
    'process_name': 'Prozessname', 'executable_path': 'Programmpfad', 'window_title_contains': 'Fenstertitel enthält',
    'x': 'X (px)', 'y': 'Y (px)', 'width': 'Breite (px)', 'height': 'Höhe (px)',
    'enabled': 'Aktiviert', 'use_roi': 'Bereich verwenden', 'points': 'Punkte', 'source': 'Quelle',
    'manual_x': 'X (px)', 'manual_y': 'Y (px)', 'offset_x': 'Versatz X (px)', 'offset_y': 'Versatz Y (px)',
    'reference_x': 'Referenz X (px)', 'reference_y': 'Referenz Y (px)', 'reference_source': 'Referenzquelle',
    'offset_settings': 'Abstandstoleranz', 'expression_settings': 'Achsenbedingungen', 'expressions': 'Bedingungen',
    'axis': 'Achse', 'combine_mode': 'Verknüpfung', 'options': 'Antwortmöglichkeiten', 'id': 'Antwort-ID',
    'label': 'Beschriftung', 'font_color': 'Schriftfarbe', 'desktop_index': 'Monitor', 'duration_ms': 'Dauer (ms)',
    'clear_on_job_end': 'Bei Job-Ende entfernen', 'result': 'Ergebnisquelle',
}
EXAMPLE_TEXT = {
    'process_name': 'notepad', 'window_title_contains': 'Editor', 'executable_path': r'C:\Windows\System32\notepad.exe',
    'script_path': r'C:\Beispiele\Beispiel.ps1', 'template_path': r'C:\Beispiele\Vorlage.png',
    'source_path': r'C:\Beispiele\Eingang', 'target_path': r'C:\Beispiele\Archiv',
    'save_path': r'C:\Beispiele\Ausgabe', 'output_path': r'C:\Beispiele\Ausgabe.png',
    'arguments': r'C:\Beispiele\Notiz.txt', 'working_directory': r'C:\Beispiele', 'new_name': 'Archiv.txt',
    'filter': '*.txt', 'key': 'Tab', 'text': 'Bearbeitung abgeschlossen', 'title': 'Hinweis',
    'description': 'Wähle den nächsten Arbeitsschritt.', 'question': 'Dateien sichern?',
    'color_hex': '#2563EB', 'font_color': '#2563EB', 'model': 'Beispielmodell.onnx', 'class_name': 'person',
    'camera': 'Beispielkamera · Automatisch', 'capability': 'Systemlautstärke · 50',
    'conditions': 'Rückgabewert der Antwort = yes', 'job': 'Unterlagen vorbereiten', 'macro': 'Notiz eintragen',
    'image_source': 'Bildaufnahme → Bild', 'bounds_source': 'Farberkennung → Begrenzungsrahmen',
    'points_source': 'Farberkennung → Punkt', 'reference_points_source': 'Farberkennung → Punkt',
    'process_target': 'Programm starten → Prozess', 'padding_source': '20 px',
    'text_result': 'Benutzerauswahl → Antworttext', 'origin': 'X = 100, Y = 80',
    'roi': 'X = 100, Y = 80, Breite = 320, Höhe = 180', 'points': 'X = 120, Y = 90',
    'expressions': 'X < 500', 'options': 'Ja, sichern → yes / Nein → no',
    'overlay': 'Farberkennung → Alle Erkennungen; Text bei X = 100, Y = 80',
    'yolo_selection': 'Beispielmodell.onnx · person',
    'script': 'return 42;', 'label': 'Ja, sichern', 'value': 'yes', 'comparison_value': 'yes', 'axis': 'X',
}


@lru_cache(maxsize=8)
def localized_labels(resource):
    import xml.etree.ElementTree as ET
    if not resource.exists():
        return {}
    return {d.attrib['name']: d.findtext('value', '') for d in ET.parse(resource).getroot().findall('data')}


def option_label(field, value):
    resource = ROOT / 'DesktopAutomationApp/Resources/Strings.resx'
    labels = localized_labels(resource)
    option = next((o for o in (field.get('descriptor', {}).get('Options') or []) if str(o['Value']) == str(value)), {})
    if isinstance(value, bool):
        return 'An' if value else 'Aus'
    return option.get('DisplayName') or labels.get(option.get('LabelKey'), labels.get('Enum.' + field.get('type', '') + '.' + str(value), str(value)))


def ui_example(field):
    key = field['id'].split('.')[-1]
    if key == 'value' and field.get('type') in {'Int32', 'Double'}:
        return '500'
    if key in EXAMPLE_TEXT:
        return EXAMPLE_TEXT[key]
    if 'path' in key or 'directory' in key:
        return r'C:\Beispiele\Dokumente'
    descriptor = field.get('descriptor', {})
    value = descriptor.get('DefaultValue', field.get('defaultValue'))
    kind = descriptor.get('ValueKind', field.get('type', ''))
    if isinstance(value, bool) or kind == 'Boolean':
        return 'An (Häkchen gesetzt)' if value else 'Aus (Häkchen entfernt)'
    if isinstance(value, (int, float)):
        if '%' in field.get('name', ''):
            return str(round(value * 100, 2)) + ' %'
        return str(value)
    if isinstance(value, str) and value:
        return option_label(field, value)
    if kind in {'Int32', 'Integer', 'Double', 'Number'}:
        return '100'
    if kind in {'String', 'Text'}:
        return 'Beispieltext'
    options = field.get('options') or [o['Value'] for o in descriptor.get('Options') or []]
    if options:
        return option_label(field, options[0])
    return 'Passenden Wert oder Ergebnis eines vorherigen Steps auswählen; Unterfelder siehe unten.'


def ui_field_rows(item, notes):
    fields = item.get('fields', [])
    if 'uiFieldIds' in item:
        fields = [f for f in fields if f['id'] in item['uiFieldIds']]
    names = {f['id']: f['name'] for f in fields}
    rows = []
    for f in fields:
        descriptor = f.get('descriptor', {})
        options = f.get('options') or (descriptor.get('Constraints') or {}).get('AllowedValues') or [o['Value'] for o in descriptor.get('Options') or []]
        explanation = esc(notes.get('field.' + f['id'], ''))
        if descriptor.get('Advanced'):
            explanation += '<p>Im Bereich „Erweitert“ verfügbar.</p>'
        conditions = descriptor.get('VisibleWhenAll') or ([descriptor['VisibleWhen']] if descriptor.get('VisibleWhen') else [])
        conditions = [c for c in conditions if c.get('FieldId') in names]
        if conditions:
            explanation += '<p>Sichtbar bei: ' + esc(' und '.join(names.get(c.get('FieldId'), c.get('FieldId', '')) + ' = ' + ' / '.join(option_label(next((v for v in fields if v['id'] == c.get('FieldId')), {}), v) for v in (c.get('AnyOfValues') or [c.get('EqualsValue')])) for c in conditions)) + '.</p>'
        limits = descriptor.get('Constraints') or {}
        if limits.get('Minimum') is not None or limits.get('Maximum') is not None:
            shown_limit = lambda value: str(round(value * 100, 2)) + ' %' if '%' in f['name'] else str(value)
            explanation += '<p>Zulässiger Bereich: ' + esc(('ab ' + shown_limit(limits['Minimum']) if limits.get('Minimum') is not None else '') + (' bis ' + shown_limit(limits['Maximum']) if limits.get('Maximum') is not None else '')) + '.</p>'
        if options:
            explanation += '<dl class="option-notes">' + ''.join('<dt>' + esc(option_label(f, o)) + '</dt><dd>' + esc(notes.get('field.' + f['id'] + '.option.' + str(o), '')) + '</dd>' for o in options) + '</dl>'
        rows.append([esc(f['name']), explanation, esc(ui_example(f))])
    return rows


def ui_nested_rows(item, notes):
    rows = []
    top_fields = {f['id'] for f in item.get('fields', [])}
    compound_members = {'camera_id', 'camera_name', 'quality_mode', 'width', 'height', 'frames_per_second', 'pixel_format', 'model', 'class_name', 'enable_roi', 'monitor_device_name'}
    for f in item.get('schema', []):
        parts = f['id'].split('.')
        if 'uiFieldIds' in item and parts[1:2] == ['target'] and 'process_target' not in item['uiFieldIds']:
            continue
        if parts[0] != 'settings' or parts[-1] in {'Empty', 'IsEmpty'} or len(parts) < 2 or (len(parts) == 2 and (parts[-1] in top_fields or parts[-1] not in compound_members)) or any(p in BINDING_PARTS for p in parts) or f['type'] in {'ResultBinding', 'List`1', 'Dictionary`2'}:
            continue
        if f['type'] not in {'String', 'Int32', 'Double', 'Boolean'} and not f.get('options'):
            continue
        label = ' › '.join(UI_NAMES.get(p, p.replace('_', ' ').capitalize()) for p in parts[1:])
        explanation = esc(notes.get('schema.' + f['id'], ''))
        if f.get('options'):
            explanation += '<dl class="option-notes">' + ''.join('<dt>' + esc(option_label(f, o)) + '</dt><dd>' + esc(notes.get('schema.' + f['id'] + '.option.' + str(o), '')) + '</dd>' for o in f['options']) + '</dl>'
        rows.append([esc(label), explanation, esc(ui_example(f))])
    return rows


def navigation_link(label, url, current, section=False):
    state = 'page' if current == url else 'location' if section and current.startswith(url) else None
    return '<a href="' + esc(url, quote=True) + '"' + (' aria-current="' + state + '"' if state else '') + '>' + esc(label) + '</a>'


def breadcrumbs(title, parents):
    return '<nav class="breadcrumbs wrap" aria-label="Seitenpfad"><ol>' + ''.join('<li><a href="' + esc(url, quote=True) + '">' + esc(label) + '</a></li>' for label, url in parents) + '<li><span aria-current="page">' + esc(title) + '</span></li></ol></nav>'


def header(current='/', title='Startseite', parents=()):
    links = [('Startseite', '/', False), ('Beispiele', '/beispiele/', True), ('Anwendung', '/anwendung/', True), ('Dokumentation', '/doku/', True), ('Download', '/download/', True)]
    return '<a class="skip-link" href="#inhalt">Zum Inhalt</a><div class="site-header-shell"><header class="site-header wrap"><a href="/" class="logo"><img class="app-icon" src="/app.ico" width="34" height="34" alt="">DesktopAutomation</a><button class="menu-toggle" type="button" aria-expanded="false" aria-controls="main-nav">Menü</button><nav id="main-nav" aria-label="Hauptnavigation">' + ''.join(navigation_link(label, url, current, section) for label, url, section in links) + '</nav></header>' + (breadcrumbs(title, parents) if parents else '') + '</div>'


def footer():
    return '''<footer class="site-footer wrap"><a class="logo" href="/"><img class="app-icon" src="/app.ico" width="30" height="30" alt="">DesktopAutomation</a><span>© 2026</span><a href="https://github.com/Fabius603/DesktopAutomation" target="_blank" rel="noopener">GitHub</a><a href="https://github.com/Fabius603/DesktopAutomation/issues" target="_blank" rel="noopener">Feedback</a><a href="mailto:fjschlieper@gmail.com">Kontakt</a><a href="/impressum.html">Impressum</a><a href="/datenschutz.html">Datenschutz</a></footer><script src="/website.js?v=20261009-6"></script>'''


def page(title, body, description='', noindex=False, current='/', parents=()):
    return '<!doctype html><html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>' + esc(title) + ' – DesktopAutomation</title><meta name="description" content="' + esc(description or title, quote=True) + '"><link rel="icon" href="/app.ico"><link rel="stylesheet" href="/website.css?v=20261009-6">' + ('<meta name="robots" content="noindex,follow">' if noindex else '<link rel="canonical" href="https://desktopautomation.pages.dev' + current + '">') + '</head><body>' + header(current, title, parents) + body + footer() + '</body></html>\n'


def build_static_pages():
    output = {}
    for filename, title, current in [('index.html', 'Startseite', '/'), ('anwendung/index.html', 'Anwendung', '/anwendung/'), ('download/index.html', 'Download', '/download/'), ('impressum.html', 'Impressum', '/impressum.html'), ('datenschutz.html', 'Datenschutz', '/datenschutz.html'), ('404.html', 'Seite nicht gefunden', '/404.html')]:
        template = (SITE / 'pages' / filename).read_text(encoding='utf-8')
        if template.count('{{HEADER}}') != 1 or template.count('{{FOOTER}}') != 1:
            raise ValueError('Page template must use the shared header and footer exactly once: ' + filename)
        output[filename] = template.replace('{{HEADER}}', header(current, title, () if current == '/' else [('Startseite', '/')])).replace('{{FOOTER}}', footer()).replace('{{ASSET_VERSION}}', '20261009-6')
    return output


def required(item):
    keys = {'purpose', 'example', 'errors'}
    for f in item.get('fields', []):
        keys.add('field.' + f['id'])
        options = f.get('options', []) or (f.get('descriptor', {}).get('Constraints') or {}).get('AllowedValues', [])
        options = list(options or []) + [x['Value'] for x in f.get('descriptor', {}).get('Options', []) or []]
        for option in options:
            keys.add('field.' + f['id'] + '.option.' + str(option))
    for f in item.get('schema', []):
        keys.add('schema.' + f['id'])
        for option in f.get('options', []):
            keys.add('schema.' + f['id'] + '.option.' + str(option))
    for prop in (item.get('result') or {}).get('Properties', []):
        keys.add('result.' + prop['StableId'])
        for option in prop.get('EnumValues', []) or []:
            keys.add('result.' + prop['StableId'] + '.option.' + option)
    for option in item.get('options', []):
        keys.add('option.' + option)
    for value in item.get('inputs', []):
        keys.add('input.' + value['Key'])
    return keys


def explain_options(notes, prefix, options):
    return '<dl class="option-notes">' + ''.join('<dt>' + anchor('option-' + prefix, str(value), code(value)) + '</dt><dd>' + esc(notes.get(prefix + str(value), '')) + '</dd>' for value in options) + '</dl>' if options else ''


def entries(meta):
    records = []
    for source, group in [('steps', 'steps'), ('automations', 'automationen'), ('macros', 'makros'), ('logs', 'logs'), ('logCodes', 'logs')]:
        for item in meta[source]:
            records.append((group, item))
    for result in meta['resultTypes']:
        records.append(('werte', {'id': result['TypeName'], 'name': result['DisplayName'], 'result': result}))
    for capability in meta.get('windowsCapabilities', []):
        c = capability['descriptor']
        fields = [{'id': p['Name'], 'name': p['DisplayName'], 'type': p['Type'], 'Required': p.get('Required', False), 'defaultValue': p.get('DefaultValue'), 'options': p.get('AllowedValues', []) or []} for p in c.get('Parameters', []) or []]
        records.append(('werte', {'id': c['Id'], 'name': c['DisplayName'], 'fields': fields, 'result': capability['result']}))
    return records


def pre(value):
    return '<pre class="json-example"><code>' + esc(json.dumps(value, ensure_ascii=False, indent=2)) + '</code></pre>'


def screenshot_entries():
    path = DOCS / 'screenshots.json'
    return read(path)['entries'] if path.exists() else []


def screenshot_reference(entry):
    result = {k: entry[k] for k in ('id', 'file', 'title', 'caption', 'width', 'height', 'pages')}
    if entry.get('crop'):
        info = read(DIST / (Path(entry['file']).stem + '.detail.json'))
        result.update(file=Path(entry['file']).stem+'.detail.png', width=info['width'], height=info['height'],
                      markers=[dict(m, explanation=marker_explanation(entry, m['label'])) for m in info['markers']])
    return result


def marker_explanation(entry, label):
    normalize = lambda v: v.split(' (')[0].strip().casefold()
    key = next((p.removeprefix('/doku/').rstrip('/') for p in entry['pages'] if p.startswith('/doku/steps/')), '')
    if key:
        metadata = read(DOCS / 'metadata.json')
        notes = read(DOCS / 'content.json').get(key, {})
        item = next((v for group, v in entries(metadata) if group + '/' + v['id'] == key), {})
        field = next((f for f in item.get('fields', []) if normalize(f['name']) == normalize(label)), None)
        if field:
            return notes.get('field.' + field['id'], '')
    return entry.get('markNotes', {}).get(label, '')


def illustrations(url):
    result = ''
    for entry in screenshot_entries():
        if url not in entry['pages']:
            continue
        file, width, height, markers = entry['file'], entry['width'], entry['height'], []
        if entry.get('crop'):
            file = Path(file).stem + '.detail.png'
            info = read(DIST / (Path(entry['file']).stem + '.detail.json'))
            width, height, markers = info['width'], info['height'], info['markers']
        result += '<figure class="doc-figure' + (' annotated-figure' if markers else '') + (' wide-figure' if width > 700 else '') + '"><figcaption><strong>' + esc(entry['title']) + '</strong><span>' + esc(entry['caption']) + '</span></figcaption><div class="figure-content"><a href="/' + esc(file, quote=True) + '" target="_blank" rel="noopener" aria-label="' + esc(entry['title'] + ' in voller Größe ansehen', quote=True) + '"><img src="/' + esc(file, quote=True) + '" width="' + str(width) + '" height="' + str(height) + '" loading="lazy" alt="' + esc(entry['caption'], quote=True) + '"></a>'
        if markers:
            result += '<ol class="figure-key">' + ''.join('<li><span class="marker-number">' + str(m['number']) + '</span><div><strong>' + esc(m['label']) + '</strong>' + ('<p>'+esc(marker_explanation(entry, m['label']))+'</p>' if marker_explanation(entry, m['label']) else '') + '</div></li>' for m in markers) + '</ol>'
        result += '</div><p class="figure-note">' + ('Editor-Ausschnitt mit Markierungen' if markers else 'App-Ansicht') + ' · Lightmode · Beispieldaten · Bild anklicken zum Vergrößern. Die Ansichten erklären die Bedienung; noch nicht gewählte Quellen, Geräte oder Modelle müssen für einen ausführbaren Job ergänzt werden.</p></figure>'
    return result


def visual_map(title, cards, caption='Schematische Übersicht: Beschriftungen und Werte aus der Referenz, keine nachgezeichnete Bedienoberfläche.'):
    """Semantic diagrams for data contracts, with exact names and adjacent explanations."""
    if not cards:
        return ''
    return '<figure class="visual-map"><figcaption>' + esc(title) + '</figcaption><ol>' + ''.join('<li><span class="marker-number">' + str(i+1) + '</span><div><strong>' + esc(label) + '</strong><span class="visual-value">' + esc(value) + '</span>' + ('<p>' + esc(note) + '</p>' if note else '') + '</div></li>' for i, (label, value, note) in enumerate(cards)) + '</ol><p class="figure-note">' + esc(caption) + '</p></figure>'


def contract_visual(item, notes):
    cards = []
    for f in item.get('fields', []):
        d = f.get('descriptor') or f
        name = f['name']; kind = d.get('ValueKind', f.get('type', 'Wert'))
        value = d.get('DefaultValue', f.get('defaultValue'))
        options = (d.get('Constraints') or {}).get('AllowedValues') or f.get('options') or [o['Value'] for o in d.get('Options', []) or []]
        shown = (', '.join(str(v) for v in options) if options else text(value))
        cards.append((name, f['id']+' · '+str(kind)+' · '+shown, notes.get('field.'+f['id'], '').split('. ')[0]))
    for p in (item.get('result') or {}).get('Properties', []):
        cards.append((p.get('DisplayName') or p['StableId'], 'Ergebnis: '+p['StableId']+' · '+str(p.get('ValueKind', 'Wert')), notes.get('result.'+p['StableId'], '').split('. ')[0]))
    for option in item.get('options', []):
        cards.append((str(option), 'Status / Auswahlwert', notes.get('option.'+str(option), '')))
    if not cards:
        cards = [(item['name'], item['id'], notes['purpose']), ('Beispiel', 'Anwendung', notes['example']), ('Prüfen', 'Fehler und Grenzen', notes['errors'])]
    return visual_map('Felder und Zusammenhänge auf einen Blick', cards)


DEVELOPER_GUIDES = {'dateiformate', 'agenten'}
DEVELOPER_SECTIONS = {
    'werte-verbinden': {'anbieter', 'lokal', 'strukturen'},
    'makros-bearbeiten': {'format', 'befehle'},
    'automationen-einrichten': {'huelle', 'webhook'},
    'logs-verstehen': {'korrelation', 'dateien', 'jsonl-beispiel'},
    'windows-funktionen': {'agent'},
}


def developer_details(body):
    return '<details class="developer-doc" id="entwickler"><summary>Entwickler- und Agenten-Doku <span>JSON, Feldnamen, Typen und Verträge</span></summary><div class="developer-content"><p>Dieser Bereich erklärt die Bearbeitung von Textdateien und die technischen Verträge. <a href="/doku/entwickler/">Zur Entwickler-Doku</a></p>' + body + '</div></details>'


def render_guide(guide):
    result = '<p class="eyebrow">' + ('Entwickler- und Agenten-Doku' if guide['id'] in DEVELOPER_GUIDES else 'Benutzer-Doku') + '</p><p class="lead">' + esc(guide['intro']) + '</p>'
    technical = ''
    for section in guide['sections']:
        start = len(result)
        result += '<section id="' + esc(section['id'], quote=True) + '"><h2>' + esc(section['title']) + '</h2>'
        if section['steps']:
            result += visual_map('Ablauf: '+section['title'], [('Schritt '+str(i+1), '→' if i<len(section['steps'])-1 else 'Abschluss', step) for i, step in enumerate(section['steps'])], 'Leserichtung: von oben nach unten. Die Zahlen zeigen die Reihenfolge.')
        elif section['rows']:
            result += visual_map(section['title']+' im Zusammenhang', [(row[0], row[1] if len(row)>1 else '', ' · '.join(row[2:])) for row in section['rows']])
        elif section['code'] is not None:
            values = section['code'].items() if isinstance(section['code'], dict) else [('Beispiel', section['code'])]
            result += visual_map('JSON-Aufbau: '+section['title'], [(key, text(value), '') for key,value in values])
        else:
            result += visual_map(section['title']+' – Orientierung', [('Zusammenhang '+str(i+1), '', paragraph) for i, paragraph in enumerate(section['paragraphs'])])
        if section['steps'] or section['rows'] or section['code'] is not None:
            result += ''.join('<p>' + esc(p) + '</p>' for p in section['paragraphs'])
        if section['code'] is not None:
            result += '<p class="version-note">JSON-Strukturbeispiel – bei Ausschnitten fehlt die vollständige Dateihülle.</p>' + pre(section['code'])
        if section['links']:
            result += '<ul>' + ''.join('<li><a href="' + esc(url, quote=True) + '">' + esc(label) + '</a></li>' for label, url in section['links']) + '</ul>'
        result += '</section>'
        if guide['id'] not in DEVELOPER_GUIDES and section['id'] in DEVELOPER_SECTIONS.get(guide['id'], set()):
            technical += result[start:]
            result = result[:start]
    if technical:
        result += developer_details(technical)
    return result


def machine_reference(meta, content, authoring, handbooks, records):
    output, reference_index = {}, []
    lines = ['# DesktopAutomation – vollständige Produktreferenz', '',
             'Entwicklungsstand ' + meta['appVersion'] + ' · 2026-10-08 · Arbeitsbaum, möglicherweise noch unveröffentlicht.',
             'Formatversionen: Job 4, Makro 3, Automation 1. Defaultvorlagen sind strukturell, nicht vollständig lauffähig.',
             'Vertragsdaten mit Descriptoren, Typen, Standards, Kardinalitäten, bindingSchemas und Vorlagen: /doku/vertragsdaten.json', '']
    for guide in handbooks:
        lines.extend(['## ' + guide['title'], '', 'Website: /doku/' + guide['id'] + '/', '', guide['intro'], ''])
        for entry in screenshot_entries():
            if '/doku/' + guide['id'] + '/' in entry['pages']:
                lines.extend(['Abbildung: [' + entry['caption'] + '](/' + screenshot_reference(entry)['file'] + ')', ''])
                lines.extend(str(m['number'])+'. '+m['label']+': '+m['explanation'] for m in screenshot_reference(entry).get('markers', []))
        if 'guides/' + guide['id'] in content:
            lines.extend([content['guides/' + guide['id']]['purpose'], ''])
        for section in guide['sections']:
            lines.extend(['### ' + section['title'], ''] + section['paragraphs'])
            lines.extend(str(i + 1) + '. ' + p for i, p in enumerate(section['steps']))
            lines.extend(' | '.join(row) for row in section['rows'])
            if section['code'] is not None:
                lines.extend(['```json', json.dumps(section['code'], ensure_ascii=False, indent=2), '```'])
            lines.extend('[' + label + '](' + url + ')' for label, url in section['links'])
            lines.append('')
    for group, item in records:
        key = group + '/' + item['id']
        start = len(lines)
        lines.extend(['## ' + GROUPS[group] + ': ' + item['name'], '', 'ID: ' + item['id'],
                      'Website: /doku/' + key + '/', ''])
        pictures = [entry for entry in screenshot_entries() if '/doku/' + key + '/' in entry['pages']]
        for entry in pictures:
            lines.extend(['Abbildung: [' + entry['caption'] + '](/' + screenshot_reference(entry)['file'] + ')', ''])
            lines.extend(str(m['number'])+'. '+m['label']+': '+m['explanation'] for m in screenshot_reference(entry).get('markers', []))
        lines.extend('### ' + name + '\n\n' + explanation + '\n' for name, explanation in sorted(content[key].items()))
        # Include exact contract facts alongside prose; metadata is reference data, not wire JSON.
        lines.extend(['### Vertrag (Metadaten; keine Konfigurationsdatei)', '```json', json.dumps(item, ensure_ascii=False, indent=2), '```', ''])
        template_group = {'steps':'steps', 'automationen':'automations', 'makros':'macros'}.get(group)
        template = authoring['templates'].get(template_group, {}).get(item['id']) if template_group else None
        if group == 'makros' and item['id'] == 'recording':
            template = authoring['templates']['macro']
        if group == 'automationen' and item['id'] in ('AutomationAction', 'AutomationRunPolicy'):
            template = authoring['templates']['automation']['action' if item['id'] == 'AutomationAction' else 'run_policy']
        if template is not None:
            lines.extend(['### Strukturelle Serializer-Vorlage', '```json', json.dumps(template, ensure_ascii=False, indent=2), '```', ''])
        output['doku/' + key + '/referenz.md'] = '\n'.join(lines[start:]) + '\n'
        output['doku/' + key + '/vertrag.json'] = json.dumps(dict(appVersion=meta['appVersion'],
            contract=item, explanations=content[key], template=template,
            screenshots=[screenshot_reference(entry) for entry in pictures],
            note='Referenzdaten. Defaultvorlage vor Verwendung vervollständigen und fachlich validieren.'), ensure_ascii=False, indent=2) + '\n'
        reference_index.append(dict(id=item['id'], group=group, title=item['name'],
            url='/doku/' + key + '/', markdown='/doku/' + key + '/referenz.md', json='/doku/' + key + '/vertrag.json'))
    reference = '\n'.join(lines) + '\n'
    bundle = dict(formatVersion=1, appVersion=meta['appVersion'], releaseChannel=meta.get('releaseChannel', 'working-tree'),
                  note='Referenzdaten, kein JSON-Schema. Defaultvorlagen können Pflichtwerte vermissen. App-Validatoren verwenden.',
                  contracts=meta, authoring=authoring, explanations=content, guides=handbooks,
                  screenshots=[screenshot_reference(entry) for entry in screenshot_entries()])
    entry = '# DesktopAutomation\n\nProduktdokumentation für UI-Benutzer und Agenten.\n\n' + 'Entwicklungsstand ' + meta['appVersion'] + ' (Arbeitsbaum, 2026-10-08). Formatversionen: Job 4, Makro 3, Automation 1.\n\n'
    entry += '\n'.join('- [' + g['title'] + '](/doku/' + g['id'] + '/): ' + g['intro'] for g in handbooks)
    entry += '\n\n- [Vollständige Referenz](/llms-full.txt)\n- [Verträge, Erklärungen und Serializer-Vorlagen](/doku/vertragsdaten.json)\n- [Illustrierte Beispielabläufe](/beispiele/)\n\nIDs und Enum-Schreibweisen aus den Verträgen übernehmen. Jobs/Makros verwenden type, Trigger $type. URI-kodierte step_result-Referenzen und inputs/localValues beachten. Defaultvorlagen sind keine getesteten ausführbaren Abläufe. Dateien fachlich validieren; aktive Automationen erst nach manuellem Test. Bei Loglücken keinen Erfolg erfinden.\n'
    entry += '\n\n[Entwickler- und Agenten-Doku](/doku/entwickler/) · [Benutzer-Doku](/doku/benutzer/)\n'
    entry += '\n## Referenzen gezielt lesen\n\n[Maschinenlesbarer Referenzindex](/doku/referenz-index.json). Jede Referenz besitzt eine einzelne Markdown-Datei und einen einzelnen JSON-Vertrag; für gezielte Änderungen genügt der betreffende Eintrag. Zusammengesetzte Binding-Schemas und vollständige Dateihüllen stehen im gesamten Vertragsdatensatz.\n\n'
    entry += '\n'.join('- [' + r['title'] + '](' + r['markdown'] + ') · ' + r['group'] + ' · ' + r['id'] for r in reference_index) + '\n'
    output.update({'doku/vertragsdaten.json': json.dumps(bundle, ensure_ascii=False, indent=2) + '\n',
                   'doku/referenz-index.json': json.dumps(reference_index, ensure_ascii=False, indent=2) + '\n',
                   'doku/referenz.md': reference, 'llms-full.txt': reference, 'llms.txt': entry})
    return output


def build(meta, content):
    output, index = {}, []
    records = entries(meta)
    authoring = read(DOCS / 'authoring.json')
    handbooks = guides(meta, authoring, [])
    def layout(title, body, toc='', pending=False, current='/doku/', group=None):
        body = illustrations(current) + body
        notice = '<div class="doc-notice">Die ausführlichen Erklärungen werden noch ergänzt. Diese Referenz zeigt den aktuellen Entwicklungsstand.</div>' if pending else ''
        parents = [('Startseite', '/')]
        if current != '/doku/':
            parents.append(('Dokumentation', '/doku/'))
        is_developer = current == '/doku/entwickler/' or current in {'/doku/' + slug + '/' for slug in DEVELOPER_GUIDES}
        if current not in ('/doku/', '/doku/benutzer/', '/doku/entwickler/'):
            parents.append(('Entwickler-Doku', '/doku/entwickler/') if is_developer else ('Benutzer-Doku', '/doku/benutzer/'))
        if group:
            parents.append((GROUPS[group], '/doku/' + group + '/'))
        active_nav = '<details class="docs-navigation" open><summary>Dokumentation</summary><nav aria-label="Dokumentationsbereiche">' + navigation_link('Übersicht', '/doku/', current) + '<strong>Benutzer-Doku</strong>' + navigation_link('Bedienung und Anleitungen', '/doku/benutzer/', current) + ''.join(navigation_link(g['title'], '/doku/' + g['id'] + '/', current) for g in handbooks if g['id'] not in DEVELOPER_GUIDES) + '<strong>Funktionen nachschlagen</strong>' + ''.join(navigation_link(name, '/doku/' + key + '/', current, section=True) for key, name in GROUPS.items()) + '<strong>Entwickler und Agenten</strong>' + navigation_link('Technische Dokumentation', '/doku/entwickler/', current) + ''.join(navigation_link(g['title'], '/doku/' + g['id'] + '/', current) for g in handbooks if g['id'] in DEVELOPER_GUIDES) + '</nav></details>'
        audience_nav = '<nav class="doc-audiences" aria-label="Dokumentation für deine Aufgabe">' + navigation_link('Benutzer-Doku', '/doku/benutzer/', '/doku/entwickler/' if is_developer else '/doku/benutzer/') + navigation_link('Entwickler / Agenten', '/doku/entwickler/', '/doku/entwickler/' if is_developer else '/doku/benutzer/') + '</nav>' if current != '/doku/' else ''
        return page(title, '<main id="inhalt" class="docs-layout wrap"><aside>' + active_nav + '</aside><article class="docs-article">' + audience_nav + '<p class="version-note">Vorschau · Entwicklungsstand ' + esc(meta['appVersion']) + '</p><h1>' + esc(title) + '</h1>' + notice + body + '</article><aside class="docs-toc" aria-label="Auf dieser Seite">' + toc + '</aside></main>', noindex=True, current=current, parents=parents)
    counts = {g: sum(1 for group, _ in records if group == g) for g in GROUPS}
    search_form = '<label class="search-label" for="doc-search">Dokumentation durchsuchen</label><input id="doc-search" class="doc-search" type="search" placeholder="Step, Feld, Trigger oder Logcode" autocomplete="off"><p id="search-status" role="status" aria-live="polite">Suche in Anleitungen und allen Referenzen.</p><ul id="search-results" class="search-results"></ul>'
    # Separate entry points share the canonical guides and function references.
    audience_cards = '<div class="doc-cards audience-cards"><a href="/doku/benutzer/"><span>BEDIENUNG</span><h2>Benutzer-Doku</h2><p>Jobs, Makros und Automationen in der Oberfläche einrichten. Einstellungen verstehen und Fehler beheben.</p></a><a href="/doku/entwickler/"><span>JSON UND AGENTEN</span><h2>Entwickler-Doku</h2><p>Dateiformate, technische Feldnamen, Ergebnisverträge und maschinenlesbare Referenzen.</p></a></div>'
    output['doku/index.html'] = layout('Dokumentation', audience_cards + search_form + '<script src="/doku/search.js?v=2"></script>')
    user_guides = [g for g in handbooks if g['id'] not in DEVELOPER_GUIDES]
    def guide_cards(items, technical=False):
        return '<div class="doc-cards">' + ''.join('<a href="/doku/' + g['id'] + '/' + ('#entwickler' if technical and g['id'] not in DEVELOPER_GUIDES and DEVELOPER_SECTIONS.get(g['id']) else '') + '"><h3>' + esc(g['title']) + '</h3><p>' + esc(g['intro']) + '</p></a>' for g in items) + '</div>'
    reference_cards = '<div class="doc-cards">' + ''.join('<a href="/doku/' + g + '/"><h3>' + name + '</h3><p>' + str(counts[g]) + ' Funktionen und Referenzen</p></a>' for g, name in GROUPS.items()) + '</div>'
    output['doku/benutzer/index.html'] = layout('Benutzer-Doku', '<p class="lead">Die Anwendung bedienen: Abläufe erstellen, Werte auswählen und Ausführungen nachvollziehen.</p><p>Jede Funktionsseite beginnt mit der Bedienung, markierten Bildern, Einstellungen und Beispielen. Technische Details findest du bei Bedarf gesammelt am Seitenende.</p><h2>Anleitungen für die Oberfläche</h2>' + guide_cards(user_guides) + '<h2>Funktionen nachschlagen</h2>' + reference_cards + '<p><a href="/doku/entwickler/">Du bearbeitest JSON-Dateien? Zur Entwickler-Doku</a></p>', current='/doku/benutzer/')
    output['doku/entwickler/index.html'] = layout('Entwickler- und Agenten-Doku', '<p class="lead">Jobs, Makros und Automationen als Textdateien bearbeiten und ihre technischen Verträge nachschlagen.</p><h2>Dateiformate und Arbeitsablauf</h2>' + guide_cards([g for g in handbooks if g['id'] in DEVELOPER_GUIDES]) + '<h2>Technische Details nach Thema</h2>' + guide_cards([g for g in handbooks if DEVELOPER_SECTIONS.get(g['id'])], technical=True) + '<h2>Verträge je Funktion</h2><p>Öffne auf einer Funktionsseite den Bereich „Entwickler- und Agenten-Doku“ am Seitenende. Dort stehen technische Feldnamen, Typen, Regeln, Ergebnisverträge und JSON-Vorlagen.</p>' + reference_cards + '<h2>Maschinenlesbare Dokumentation</h2><ul><li><a href="/llms.txt">Einstieg für Agenten</a></li><li><a href="/llms-full.txt">Vollständige Textreferenz</a></li><li><a href="/doku/referenz-index.json">Referenzindex als JSON</a></li><li><a href="/doku/vertragsdaten.json">Verträge und Serializer-Vorlagen</a></li></ul><p><a href="/doku/benutzer/">Zur Benutzer-Doku</a></p>', current='/doku/entwickler/')
    for group, item in records:
        key = group + '/' + item['id']
        notes = content[key]
        url = '/doku/' + key + '/'
        pending = any(not notes.get(k, '').strip() for k in required(item))
        body = '<section id="zweck"><h2>Wofür ist das gedacht?</h2><p>' + esc(notes.get('purpose') or item.get('description') or 'Die Erklärung dieser Funktion folgt.') + '</p></section>'
        technical = '<p class="reference-id">Identifier: ' + code(item['id']) + '</p>'
        has_photo = any(url in entry['pages'] for entry in screenshot_entries())
        if not has_photo:
            body += visual_map(item['name'] + ' verstehen', [(f['name'], '', notes.get('field.' + f['id'], '')) for f in item.get('fields', [])] or [('Zweck', '', notes['purpose']), ('Beispiel', '', notes['example']), ('Hinweise', '', notes['errors'])], 'Orientierung für die Bedienung. Technische Verträge stehen im Entwicklerbereich am Seitenende.')
        technical += '<div class="visual-reference">' + contract_visual(item, notes) + '</div>'
        fields = item.get('fields', [])
        if fields:
            rows = []
            for f in fields:
                descriptor = f.get('descriptor', {})
                facts = descriptor if descriptor else f
                values = facts.get('Constraints', {}) or {}
                options = facts.get('Options', []) or []
                default = facts.get('DefaultValue', f.get('defaultValue'))
                constraints = values.get('AllowedValues') or f.get('options') or [v['Value'] for v in options]
                rows.append([anchor('field-', f['id'], esc(f['name'])) + '<br>' + code(f['id']), code(facts.get('ValueKind', f.get('type'))), code(default),
                    esc('Pflichtfeld' if facts.get('Required') else 'Regeln siehe Erklärung') + '<br>' + (code(constraints) if constraints else '') + (('<br>Grenzen: ' + code({k: v for k, v in values.items() if v is not None and k != 'AllowedValues'})) if any(v is not None and k != 'AllowedValues' for k, v in values.items()) else '') +
                    (('<br>Sichtbar wenn: ' + code(facts.get('VisibleWhen') or facts.get('VisibleWhenAll'))) if facts.get('VisibleWhen') or facts.get('VisibleWhenAll') else ''), esc(notes.get('field.' + f['id'], '')) + explain_options(notes, 'field.' + f['id'] + '.option.', constraints or [])])
            body += '<section id="felder"><h2>Einstellungen in der Oberfläche</h2>' + '<p>Beispielwerte dienen zur Orientierung. Passe Pfade, Quellen und Geräte an deinen Arbeitsplatz an. Ein ausgeschaltetes Häkchen und die Zahl 0 sind ebenfalls gültige Beispielwerte.</p>' + table(['Einstellung', 'Bedeutung und Bedienung', 'Beispielwert'], ui_field_rows(item, notes)) + '</section>'
            technical += '<section id="feldvertrag"><h2>Feldtypen, Standards und Regeln</h2>' + table(['Wert', 'Typ', 'Standard laut Vertrag', 'Optionen und Regeln', 'Erklärung'], rows) + '</section>'
        nested_rows = ui_nested_rows(item, notes) if group == 'steps' else []
        if nested_rows:
            body += '<section id="unterfelder"><h2>Eingaben innerhalb zusammengesetzter Einstellungen</h2><p>Diese Felder erscheinen innerhalb der jeweiligen Einstellung und je nach gewählter Aktion oder Quelle. Bei einer Ergebnisquelle wählst du den passenden vorherigen Step statt eines direkten Werts.</p>' + table(['Einstellung / Unterfeld', 'Bedeutung und Bedienung', 'Beispielwert'], nested_rows) + '</section>'
        if item.get('schema'):
            technical += '<section id="unterwerte"><h2>JSON-Felder und verschachtelte Einstellungen</h2><p>Technische Feldnamen des Modells. Die Einstellungsfelder oben beschreiben die UI; die JSON-Vorlage zeigt die tatsächliche Schreibweise. Bei aktuellen inputs/localValues stammen aktive Werte aus den Referenzen. Die Modell-Standardwerte und Enum-Namen sind keine universelle JSON-Schreibweise.</p>' + table(['Feld', 'Modelltyp', 'Modell-Standard / Optionen', 'Erklärung'], [[anchor('schema-', f['id'], code(f['id'])), code(f['type']), code(f.get('defaultValue')) + (('<br>' + code(f['options'])) if f.get('options') else ''), esc(notes.get('schema.' + f['id'], '')) + explain_options(notes, 'schema.' + f['id'] + '.option.', f.get('options', []))] for f in item['schema']]) + '</section>'
        if item.get('options'):
            body += '<section id="optionen"><h2>Werte</h2>' + table(['Wert', 'Erklärung'], [[code(o), esc(notes.get('option.' + o, ''))] for o in item['options']]) + '</section>'
        if item.get('inputs'):
            technical += '<section id="eingaben"><h2>Eingabeverträge</h2>' + table(['Eingabe', 'Fehlende Werte', 'Listen', 'Akzeptierte Typen / Quellen', 'Erklärung'], [[anchor('input-', i['Key'], code(i['Key'])), code(i['MissingValuePolicy']), code(i['CollectionConsumption']), code(i['AcceptedShapes']) + '<br>' + code(i.get('AllowedProviderIds')), esc(notes.get('input.' + i['Key'], ''))] for i in item['inputs']]) + '</section>'
        if item.get('result'):
            variant = '<p>Antwort-IDs hängen von deiner Benutzerauswahl ab. Die hier aufgeführten Options-IDs sind Beispiele.</p>' if item['id'] == 'user_choice' else ''
            body += '<section id="ergebnisse"><h2>Ergebniswerte verwenden</h2>' + variant + table(['Ergebnis', 'Bedeutung'], [[esc(p.get('DisplayName') or p['StableId']), esc(notes.get('result.' + p['StableId'], ''))] for p in item['result']['Properties']]) + '</section>'
            technical += '<section id="ergebnisvertrag"><h2>Ergebnisvertrag</h2>' + variant + table(['Ergebnis', 'Typ / Optionen', 'Anzahl', 'Erklärung'], [[anchor('result-', p['StableId'], code(p['StableId'])), code(p['DataType']) + (('<br>' + code(p['EnumValues'])) if p.get('EnumValues') else ''), code(p['Cardinality']), esc(notes.get('result.' + p['StableId'], '')) + explain_options(notes, 'result.' + p['StableId'] + '.option.', p.get('EnumValues') or [])] for p in item['result']['Properties']]) + '</section>'
        body += '<section id="beispiel"><h2>Beispiel</h2><p>' + esc(notes.get('example') or 'Das ausführliche Beispiel wird ergänzt.') + '</p><a href="/beispiele/">Vorbereitete Beispielabläufe ansehen</a></section><section id="fehler"><h2>Fehler und Hinweise</h2><p>' + esc(notes.get('errors') or 'Die Erklärung des Fehlerverhaltens wird ergänzt.') + '</p></section>'
        technical += '<p class="version-note">Diese Referenz für Textdateien und Agenten: <a href="' + url + 'referenz.md">Markdown</a> · <a href="' + url + 'vertrag.json">JSON-Vertrag</a></p>'
        template_group = {'steps': 'steps', 'automationen': 'automations', 'makros': 'macros'}.get(group)
        template = authoring['templates'].get(template_group, {}).get(item['id']) if template_group else None
        if group == 'makros' and item['id'] == 'recording':
            template = authoring['templates']['macro']
        if group == 'automationen' and item['id'] in ('AutomationAction', 'AutomationRunPolicy'):
            template = authoring['templates']['automation']['action' if item['id'] == 'AutomationAction' else 'run_policy']
        if template is not None:
            template_path = 'doku/vorlagen/' + key + '.json'
            output[template_path] = json.dumps(template, ensure_ascii=False, indent=2) + '\n'
            technical += '<section id="json"><h2>JSON-Vorlage für Textdateien und Agenten</h2><p>Strukturelle Defaultvorlage direkt vom App-Serializer. Leere Pflichtwerte und Ziele vor Verwendung ergänzen; diese Vorlage ist kein vollständig validierter Ablauf. Bei neuen Jobs den aktuellen inputs/localValues-Vertrag verwenden.</p><a href="/doku/dateiformate/">Dateiformate und Regeln</a> · <a href="/' + template_path + '" download>JSON herunterladen</a>' + pre(template) + '</section>'
        body += developer_details(technical)
        toc = '<strong>Auf dieser Seite</strong><a href="#zweck">Zweck</a>' + ('<a href="#felder">Einstellungen</a>' if fields else '') + ('<a href="#ergebnisse">Ergebnisse</a>' if item.get('result') else '') + '<a href="#beispiel">Beispiel</a><a href="#fehler">Fehler</a><a href="#entwickler">Entwickler / Agenten</a>'
        output['doku/' + key + '/index.html'] = layout(item['name'], body, toc, pending, current=url, group=group)
        index.append({'title': item['name'], 'group': GROUPS[group], 'url': url, 'terms': ' '.join([item['id'], item.get('description', '')] + [f['name'] + ' ' + f['id'] for f in fields] + [f['id'] for f in item.get('schema', [])] + sorted(required(item)))})
    for group, name in GROUPS.items():
        rows = [p for p in index if p['group'] == name]
        output['doku/' + group + '/index.html'] = layout(name, '<p>Alle ' + str(len(rows)) + ' Referenzen dieses Bereichs mit Feldern, Optionen, Beispielen und Fehlerverhalten.</p><ul class="catalog-list">' + ''.join('<li><a href="' + p['url'] + '">' + esc(p['title']) + '</a></li>' for p in rows) + '</ul>', current='/doku/' + group + '/')
    for guide in handbooks:
        summary = content.get('guides/' + guide['id'], {}).get('purpose', '')
        guide_toc = '<strong>Auf dieser Seite</strong>' + ''.join('<a href="#' + s['id'] + '">' + esc(s['title']) + '</a>' for s in guide['sections'] if guide['id'] in DEVELOPER_GUIDES or s['id'] not in DEVELOPER_SECTIONS.get(guide['id'], set()))
        if guide['id'] not in DEVELOPER_GUIDES and any(s['id'] in DEVELOPER_SECTIONS.get(guide['id'], set()) for s in guide['sections']):
            guide_toc += '<a href="#entwickler">Entwickler / Agenten</a>'
        output['doku/' + guide['id'] + '/index.html'] = layout(guide['title'], render_guide(guide) + ('<p class="version-note">' + esc(summary) + '</p>' if summary else ''), guide_toc, current='/doku/' + guide['id'] + '/')
        index.append({'title': guide['title'], 'group': 'Anleitungen', 'url': '/doku/' + guide['id'] + '/', 'terms': guide['intro'] + ' ' + ' '.join(' '.join(s['paragraphs'] + s['steps']) for s in guide['sections'])})
    output.update(machine_reference(meta, content, authoring, handbooks, records))
    output['doku/search-index.json'] = json.dumps(index, ensure_ascii=False, indent=2) + '\n'
    return output


class Links(HTMLParser):
    def __init__(self):
        super().__init__()
        self.ids, self.refs = [], []
    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if 'id' in attrs:
            self.ids.append(attrs['id'])
        self.refs.extend(attrs[k] for k in ('href', 'src') if k in attrs)


def validate_links():
    pages = {}
    for path in DIST.rglob('*.html'):
        parsed = Links()
        parsed.feed(path.read_text(encoding='utf-8'))
        if len(parsed.ids) != len(set(parsed.ids)):
            raise ValueError('Duplicate IDs: ' + str(path))
        pages[path.resolve()] = parsed
    for path, parsed in pages.items():
        for ref in parsed.refs:
            url = urlsplit(ref)
            if url.scheme or url.netloc:
                continue
            target = (DIST / unquote(url.path).lstrip('/') if url.path.startswith('/') else path.parent / unquote(url.path)) if url.path else path
            if target.is_dir():
                target = target / 'index.html'
            target = target.resolve()
            if not target.is_relative_to(DIST.resolve()) or not target.exists():
                raise ValueError(f'Broken asset/link: {path} -> {ref}')
            if url.fragment and target in pages and url.fragment not in pages[target].ids:
                raise ValueError(f'Broken fragment: {path} -> {ref}')
    search = read(DIST / 'doku/search-index.json')
    for entry in search:
        if not (DIST / entry['url'].strip('/') / 'index.html').exists():
            raise ValueError('Invalid search target: ' + entry['url'])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--strict', action='store_true')
    parser.add_argument('--init-content', action='store_true')
    args = parser.parse_args()
    meta = read(DOCS / 'metadata.json')
    for relative, expected in meta['sources'].items():
        if not (ROOT / relative).exists() or hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() != expected:
            raise ValueError('Contracts or behavior changed; export and review documentation: ' + relative)
    allkeys = {group + '/' + item['id']: required(item) for group, item in entries(meta)}
    allkeys.update({'guides/' + slug: {'purpose'} for slug in ('jobs', 'erste-schritte', 'kompatibilitaet')})
    if args.init_content:
        if (DOCS / 'content.json').exists():
            raise ValueError('Content already exists; initialization cannot overwrite explanations.')
        content = {key: {field: '' for field in sorted(fields)} for key, fields in allkeys.items()}
        (DOCS / 'content.json').write_text(json.dumps(content, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        (DOCS / 'pending.json').write_text(json.dumps({'reason': 'Initial editorial backlog explicitly requested on 2026-10-08; no new blank entries may be added implicitly.', 'entries': sorted(key + ':' + field for key, fields in allkeys.items() for field in fields)}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    content = read(DOCS / 'content.json')
    allowed = set(read(DOCS / 'pending.json')['entries'])
    if set(content) != set(allkeys):
        raise ValueError('Missing or obsolete reference entries: ' + str(set(content) ^ set(allkeys)))
    missing = []
    for key, fields in allkeys.items():
        if set(content[key]) != fields:
            raise ValueError('Missing or obsolete value explanations: ' + key + ' ' + str(set(content[key]) ^ fields))
        for field in fields:
            if not isinstance(content[key][field], str):
                raise ValueError('Explanation must be plain text: ' + key + ':' + field)
            if not content[key][field].strip():
                missing.append(key + ':' + field)
    if args.strict and missing:
        raise ValueError(f'Complete documentation required: {len(missing)} explanations pending.')
    if set(missing) - allowed:
        raise ValueError('New undocumented values: ' + str(sorted(set(missing) - allowed)[:20]))
    files = build(meta, content)
    files.update(build_examples(meta))
    files.update(build_static_pages())
    for relative, value in files.items():
        path = DIST / relative
        value = value.encode('utf-8') if isinstance(value, str) else value
        if args.check:
            if not path.exists() or path.read_bytes() != value:
                raise ValueError('Generated website output stale: ' + relative)
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(value)
    manifest = DOCS / 'generated-files.json'
    previous = read(manifest) if manifest.exists() else []
    for relative in set(previous) - set(files):
        if (DIST / relative).exists():
            raise ValueError('Obsolete generated URL requires a redirect or compatibility page: ' + relative)
    if args.check and previous != sorted(files):
        raise ValueError('Generated file manifest stale')
    if not args.check:
        manifest.write_text(json.dumps(sorted(files), indent=2) + '\n', encoding='utf-8')
    validate_links()
    print(f'{len(files)} generated outputs valid; {len(missing)} explicitly deferred explanations remain.')


def build_examples(meta):
    output, cards = {}, []
    source = DOCS / 'examples'
    descriptions = read(DOCS / 'examples.json')
    for example in descriptions:
        key = example['id']
        folder = source / key
        job = read(folder / 'jobs' / (key + '.json'))
        cards.append('<a href="/beispiele/' + key + '/"><h2>' + esc(example['title']) + '</h2><p>' + esc(example['description']) + '</p><span>' + str(len(job['steps'])) + ' Steps · ' + esc(example['trigger']) + '</span></a>')
        flow = '<ol class="example-flow">' + ''.join('<li><strong>' + esc(step['title']) + '</strong><p>' + esc(step['description']) + '</p></li>' for step in example['flow']) + '</ol>'
        body = '<main id="inhalt" class="example-page wrap"><p class="eyebrow">' + esc(example['trigger']) + ' · ' + str(len(job['steps'])) + ' Steps</p><h1>' + esc(example['title']) + '</h1><p class="lead">' + esc(example['description']) + '</p><p class="version-note">Beispiel für den aktuellen Entwicklungsstand. Der Ablauf dient zur Orientierung beim Erstellen eigener Jobs.</p><figure><a href="/' + key + '.png" target="_blank" rel="noopener"><img src="/' + key + '.png" width="1600" height="980" alt="' + esc(example['title'], quote=True) + ' im echten Job-Editor" loading="lazy"></a><figcaption>Echter Job-Editor · Light-Theme · Beispieldaten</figcaption></figure><div class="example-columns"><section><h2>So läuft der Job ab</h2>' + flow + '</section><section><h2>Selbst einen Ablauf erstellen</h2><p>Lege einen eigenen Job in der Oberfläche an und orientiere dich an den beschriebenen Steps. Die Dokumentation erklärt ihre Einstellungen und Werteverbindungen.</p><p><a href="/doku/erste-schritte/">Erste Schritte in der UI</a></p><h2>Das erwartete Ergebnis</h2><p>' + esc(example['result']) + '</p></section></div><h2>Daten zwischen Steps</h2>' + table(['Quelle', 'Wert', 'Verwendung'], [[esc(x) for x in row] for row in example['bindings']]) + '<p><a href="/doku/steps/">Step-Referenz</a> · <a href="/doku/automationen/">Automationen</a> · <a href="/doku/logs/">Logs</a></p></main>'
        output['beispiele/' + key + '/index.html'] = page(example['title'], body, example['description'], noindex=True, current='/beispiele/' + key + '/', parents=[('Startseite', '/'), ('Beispiele', '/beispiele/')])
    output['beispiele/index.html'] = page('Beispiele', '<main id="inhalt" class="example-page wrap"><div class="eyebrow">VOLLSTÄNDIGE ABLÄUFE</div><h1>Mehr als ein einzelner Klick.</h1><p class="lead">Drei unterschiedliche Jobs mit Entscheidungen, Datenfluss und passenden Automationen. Die beschriebenen Abläufe zeigen, wie sich einzelne Steps miteinander verbinden lassen.</p><div class="doc-cards">' + ''.join(cards) + '</div><p>Nutze die Abläufe als Anregung für eigene Jobs und teste sie mit passenden Einstellungen und Testdaten.</p></main>', current='/beispiele/', parents=[('Startseite', '/')])
    return output


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, FileNotFoundError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
