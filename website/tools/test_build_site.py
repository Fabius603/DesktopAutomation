"""Behavior checks for documentation drift, readable guides and agent downloads."""
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch
import contextlib
import hashlib
import io
import json
import unittest
from html.parser import HTMLParser
import build_site as site


class Navigation(HTMLParser):
    def __init__(self, html):
        super().__init__()
        self.scope, self.links, self.path = None, {}, []
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == 'nav':
            self.scope = attrs.get('aria-label')
        if tag == 'a' and self.scope:
            self.links.setdefault(self.scope, []).append(attrs)
        if self.scope == 'Seitenpfad' and tag == 'span':
            self.path.append(attrs)

    def handle_endtag(self, tag):
        if tag == 'nav':
            self.scope = None


class DocumentationChecks(unittest.TestCase):
    def setUp(self):
        self.temporary = TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.original = site.ROOT, site.SITE, site.DIST, site.DOCS
        self.addCleanup(self.restore)
        site.ROOT = Path(self.temporary.name)
        site.SITE = site.ROOT / 'website'
        site.DIST = site.SITE / 'dist'
        site.DOCS = site.SITE / 'docs'
        site.DOCS.mkdir(parents=True)
        site.DIST.mkdir()
        (site.ROOT / 'contract.cs').write_text('contract', encoding='utf-8')
        self.metadata = {'appVersion': '1.6.1', 'steps': [{'id': 'copy', 'name': 'Dateien kopieren', 'fields': [], 'schema': []}],
                         'automations': [], 'macros': [], 'logs': [], 'logCodes': [], 'resultTypes': [],
                         'sources': {'contract.cs': hashlib.sha256(b'contract').hexdigest()}}
        self.write_meta()
        (site.DOCS / 'examples.json').write_text('[]', encoding='utf-8')
        self.authoring = {'templates': {'job': {}, 'macro': {}, 'automation': {}, 'steps': {}, 'automations': {}, 'macros': {}}}
        (site.DOCS / 'authoring.json').write_text(json.dumps(self.authoring), encoding='utf-8')
        self.handbooks = [dict(id=slug, title=slug, intro='Anleitung', audience='UI', sections=[])
                          for slug in ('erste-schritte', 'dateiformate', 'jobs', 'kompatibilitaet')]
        handbook_patch = patch.object(site, 'guides', lambda *args: self.handbooks)
        handbook_patch.start()
        self.addCleanup(handbook_patch.stop)
        (site.SITE / 'pages').mkdir()
        for filename in ('index.html', 'impressum.html', 'datenschutz.html', '404.html', 'anwendung/index.html', 'download/index.html'):
            template = site.SITE / 'pages' / filename
            template.parent.mkdir(parents=True, exist_ok=True)
            template.write_text('{{HEADER}}<main id="inhalt"><section id="ansichten"></section><section id="start"></section></main>{{FOOTER}}', encoding='utf-8')
        for filename in ('app.ico', 'website.css', 'website.js'):
            (site.DIST / filename).write_text('', encoding='utf-8')
        (site.DIST / 'doku').mkdir()
        (site.DIST / 'doku/search.js').write_text('', encoding='utf-8')
        self.run_site('--init-content')

    def restore(self):
        site.ROOT, site.SITE, site.DIST, site.DOCS = self.original

    def write_meta(self):
        (site.DOCS / 'metadata.json').write_text(json.dumps(self.metadata), encoding='utf-8')

    def run_site(self, *args):
        with patch('sys.argv', ['build_site.py', *args]), contextlib.redirect_stdout(io.StringIO()):
            site.main()

    def test_authorized_backlog_does_not_block_structural_validation(self):
        self.run_site('--check')

    def test_strict_mode_rejects_incomplete_explanations(self):
        with self.assertRaisesRegex(ValueError, 'Complete documentation required'):
            self.run_site('--strict')

    def test_behavior_drift_requires_review_even_without_schema_change(self):
        (site.ROOT / 'contract.cs').write_text('changed behavior', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Contracts or behavior changed'):
            self.run_site('--check')

    def test_new_values_are_not_silently_added_to_backlog(self):
        self.metadata['steps'][0]['fields'] = [{'id': 'new_value', 'name': 'Neuer Wert'}]
        self.write_meta()
        with self.assertRaisesRegex(ValueError, 'Missing or obsolete value explanations'):
            self.run_site('--check')

    def test_generated_output_drift_is_detected(self):
        (site.DIST / 'doku/steps/copy/index.html').write_text('stale', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Generated website output stale'):
            self.run_site('--check')

    def test_static_pages_cannot_bypass_shared_navigation(self):
        template = site.SITE / 'pages/impressum.html'
        template.write_text(template.read_text(encoding='utf-8').replace('{{HEADER}}', ''), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'shared header and footer'):
            self.run_site()

    def test_manual_homepage_edits_are_detected_as_generated_output_drift(self):
        (site.DIST / 'index.html').write_text('stale', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Generated website output stale: index.html'):
            self.run_site('--check')

    def test_broken_cross_page_fragments_are_detected(self):
        with (site.SITE / 'pages/index.html').open('a', encoding='utf-8') as stream:
            stream.write('<a href="/doku/#missing">Broken link</a>')
        with self.assertRaisesRegex(ValueError, 'Broken fragment'):
            self.run_site()

    def test_page_location_and_ancestor_links_remain_unambiguous(self):
        reference = Navigation((site.DIST / 'doku/steps/copy/index.html').read_text(encoding='utf-8'))
        self.assertEqual([('/doku/', 'location')], [(a['href'], a['aria-current']) for a in reference.links['Hauptnavigation'] if 'aria-current' in a])
        self.assertEqual(['/', '/doku/', '/doku/benutzer/', '/doku/steps/'], [a['href'] for a in reference.links['Seitenpfad']])
        self.assertEqual([{'aria-current': 'page'}], reference.path)
        self.assertEqual([('/doku/steps/', 'location')], [(a['href'], a['aria-current']) for a in reference.links['Dokumentationsbereiche'] if 'aria-current' in a])
        guide = Navigation((site.DIST / 'doku/jobs/index.html').read_text(encoding='utf-8'))
        self.assertEqual([('/doku/jobs/', 'page')], [(a['href'], a['aria-current']) for a in guide.links['Dokumentationsbereiche'] if 'aria-current' in a])

    def test_every_page_has_same_navigation_destinations_including_legal_and_404(self):
        expected = ['/', '/beispiele/', '/anwendung/', '/doku/', '/download/']
        for path in site.DIST.rglob('*.html'):
            with self.subTest(path=path):
                nav = Navigation(path.read_text(encoding='utf-8'))
                self.assertEqual(expected, [a['href'] for a in nav.links['Hauptnavigation']])
        home = Navigation((site.DIST / 'index.html').read_text(encoding='utf-8'))
        self.assertEqual([('/', 'page')], [(a['href'], a['aria-current']) for a in home.links['Hauptnavigation'] if 'aria-current' in a])
        legal = Navigation((site.DIST / 'impressum.html').read_text(encoding='utf-8'))
        self.assertFalse(any('aria-current' in a for a in legal.links['Hauptnavigation']))

    def test_application_and_download_are_pages_with_their_own_location(self):
        for slug in ('anwendung', 'download'):
            with self.subTest(page=slug):
                nav = Navigation((site.DIST / slug / 'index.html').read_text(encoding='utf-8'))
                self.assertEqual([('/' + slug + '/', 'page')], [(a['href'], a['aria-current']) for a in nav.links['Hauptnavigation'] if 'aria-current' in a])
                self.assertEqual(['/'], [a['href'] for a in nav.links['Seitenpfad']])
                self.assertEqual([{'aria-current': 'page'}], nav.path)

    def test_illustrated_examples_remain_without_downloadable_job_packages(self):
        example = dict(id='demo', title='Beispielablauf', trigger='Zeitplan', description='Ein eigener Ablauf',
                       flow=[dict(title='Vorbereiten', description='Eine Aktion hinzufügen')],
                       result='Abschluss prüfen', bindings=[])
        folder = site.DOCS / 'examples/demo/jobs'
        folder.mkdir(parents=True)
        (folder / 'demo.json').write_text('{"steps":[]}', encoding='utf-8')
        (site.DOCS / 'examples.json').write_text(json.dumps([example]), encoding='utf-8')
        (site.DIST / 'demo.png').write_bytes(b'')
        self.run_site()
        rendered = (site.DIST / 'beispiele/demo/index.html').read_text(encoding='utf-8')
        self.assertIn('Eine Aktion hinzufügen', rendered)
        self.assertIn('href="/doku/erste-schritte/"', rendered)
        self.assertNotIn('download>', rendered)
        self.assertNotIn('/downloads/', rendered)
        self.assertFalse(any(path.endswith('.zip') or path == 'doku/beispiele/werteverbindung.job.json' for path in site.read(site.DOCS / 'generated-files.json')))

    def test_explanations_are_rendered_as_text_not_executable_markup(self):
        content = site.read(site.DOCS / 'content.json')
        content['steps/copy']['purpose'] = '<script>alert(1)</script>'
        (site.DOCS / 'content.json').write_text(json.dumps(content), encoding='utf-8')
        self.run_site()
        rendered = (site.DIST / 'doku/steps/copy/index.html').read_text(encoding='utf-8')
        self.assertIn('&lt;script&gt;alert(1)&lt;/script&gt;', rendered)
        self.assertNotIn('<script>alert(1)</script>', rendered)

    def test_agent_download_preserves_every_explanation_and_contract(self):
        content = site.read(site.DOCS / 'content.json')
        for notes in content.values():
            for key in notes:
                notes[key] = 'Geprüfte Erklärung: ' + key
        (site.DOCS / 'content.json').write_text(json.dumps(content), encoding='utf-8')
        self.run_site('--strict')
        bundle = site.read(site.DIST / 'doku/vertragsdaten.json')
        self.assertEqual(content, bundle['explanations'])
        self.assertEqual(self.metadata, bundle['contracts'])
        reference = (site.DIST / 'llms-full.txt').read_text(encoding='utf-8')
        for value in content['steps/copy'].values():
            self.assertIn(value, reference)
            self.assertIn(value, (site.DIST / 'doku/steps/copy/referenz.md').read_text(encoding='utf-8'))
        self.assertEqual(content['steps/copy'], site.read(site.DIST / 'doku/steps/copy/vertrag.json')['explanations'])
        catalog = site.read(site.DIST / 'doku/referenz-index.json')
        self.assertEqual('copy', catalog[0]['id'])
        for format in ('markdown', 'json'):
            self.assertTrue((site.DIST / catalog[0][format].lstrip('/')).exists())
        self.assertEqual(reference, (site.DIST / 'doku/referenz.md').read_text(encoding='utf-8'))

    def test_guides_escape_code_and_include_navigation_and_search(self):
        from guide_content import section
        self.handbooks[0]['sections'] = [section('beispiel', 'JSON-Beispiel', paragraphs=['<script>unsafe</script>'], code={'text':'</code><script>unsafe</script>'})]
        self.run_site()
        rendered = (site.DIST / 'doku/erste-schritte/index.html').read_text(encoding='utf-8')
        self.assertNotIn('<script>unsafe</script>', rendered)
        self.assertIn('&lt;script&gt;unsafe&lt;/script&gt;', rendered)
        self.assertIn('href="#beispiel"', rendered)
        self.assertTrue(any(item['url'] == '/doku/erste-schritte/' for item in site.read(site.DIST / 'doku/search-index.json')))

    def test_catalog_illustrations_are_shared_with_html_and_agent_references(self):
        entry = dict(id='jobs', file='jobs.png', title='Job-Editor', caption='Werte im Job-Editor', width=1600, height=980, pages=['/doku/jobs/', '/doku/steps/copy/'])
        (site.DOCS / 'screenshots.json').write_text(json.dumps(dict(entries=[entry])), encoding='utf-8')
        (site.DIST / 'jobs.png').write_bytes(b'')
        self.run_site()
        html = (site.DIST / 'doku/jobs/index.html').read_text(encoding='utf-8')
        self.assertIn('src="/jobs.png"', html)
        self.assertIn('alt="Werte im Job-Editor"', html)
        self.assertIn('[Werte im Job-Editor](/jobs.png)', (site.DIST / 'llms-full.txt').read_text(encoding='utf-8'))
        self.assertEqual([entry], site.read(site.DIST / 'doku/vertragsdaten.json')['screenshots'])
        self.assertIn('src="/jobs.png"', (site.DIST / 'doku/steps/copy/index.html').read_text(encoding='utf-8'))
        self.assertIn('[Werte im Job-Editor](/jobs.png)', (site.DIST / 'doku/steps/copy/referenz.md').read_text(encoding='utf-8'))
        self.assertEqual([entry], site.read(site.DIST / 'doku/steps/copy/vertrag.json')['screenshots'])

    def test_serializer_template_is_available_without_changing_wire_values(self):
        template = {'type':'copy', 'mode':1, 'id':'example', 'settings': {'text':'<data>'}}
        self.authoring['templates']['steps']['copy'] = template
        (site.DOCS / 'authoring.json').write_text(json.dumps(self.authoring), encoding='utf-8')
        self.run_site()
        self.assertEqual(template, site.read(site.DIST / 'doku/vorlagen/steps/copy.json'))
        rendered = (site.DIST / 'doku/steps/copy/index.html').read_text(encoding='utf-8')
        self.assertIn('Strukturelle Defaultvorlage', rendered)
        self.assertIn('&lt;data&gt;', rendered)

    def test_reader_entry_points_and_closed_technical_reference_preserve_downloads(self):
        self.authoring['templates']['steps']['copy'] = {'type': 'copy', 'settings': {'source_path': 'example'}}
        (site.DOCS / 'authoring.json').write_text(json.dumps(self.authoring), encoding='utf-8')
        self.run_site()
        overview = (site.DIST / 'doku/index.html').read_text(encoding='utf-8')
        self.assertIn('href="/doku/benutzer/"', overview)
        self.assertIn('href="/doku/entwickler/"', overview)
        developer = (site.DIST / 'doku/entwickler/index.html').read_text(encoding='utf-8')
        self.assertIn('href="/doku/vertragsdaten.json"', developer)
        self.assertIn('href="/llms-full.txt"', developer)
        reference = (site.DIST / 'doku/steps/copy/index.html').read_text(encoding='utf-8')
        before, after = reference.split('<details class="developer-doc" id="entwickler">')
        self.assertIn('id="fehler"', before)
        self.assertNotIn('source_path', before)
        self.assertIn('source_path', after)
        self.assertIn('id="json"', after)
        self.assertEqual('example', site.read(site.DIST / 'doku/vorlagen/steps/copy.json')['settings']['source_path'])

    def test_mixed_guide_retains_direct_anchors_but_separates_json_from_ui(self):
        from guide_content import section
        guide = dict(id='werte-verbinden', title='Werte verbinden', intro='Werte wählen', audience='UI und JSON',
                     sections=[section('ui', 'In der Oberfläche', steps=['Quelle wählen']),
                               section('anbieter', 'Referenzformat', code={'provider_id': 'step_result'})])
        self.handbooks.append(guide)
        self.run_site()
        reference = (site.DIST / 'doku/werte-verbinden/index.html').read_text(encoding='utf-8')
        before, after = reference.split('<details class="developer-doc" id="entwickler">')
        self.assertIn('Quelle wählen', before)
        self.assertNotIn('provider_id', before)
        self.assertIn('id="anbieter"', after)
        self.assertIn('provider_id', after)
        self.assertIn('provider_id', (site.DIST / 'llms-full.txt').read_text(encoding='utf-8'))
        self.assertIn('/doku/werte-verbinden/#entwickler', (site.DIST / 'doku/entwickler/index.html').read_text(encoding='utf-8'))

    def test_annotated_crop_and_marker_meaning_reach_ui_and_agent_reference(self):
        entry = dict(id='copy',file='copy.png',width=1600,height=980,title='Kopieren',caption='Ziel wählen',
                     pages=['/doku/steps/copy/'],crop=[0,0,420,600],markNotes={'Ziel':'Hier den Zielordner eintragen.'})
        (site.DOCS / 'screenshots.json').write_text(json.dumps(dict(entries=[entry])),encoding='utf-8')
        (site.DIST / 'copy.detail.png').write_bytes(b'')
        (site.DIST / 'copy.detail.json').write_text(json.dumps(dict(width=464,height=400,markers=[dict(number=1,label='Ziel')])),encoding='utf-8')
        self.run_site()
        html=(site.DIST / 'doku/steps/copy/index.html').read_text(encoding='utf-8')
        self.assertIn('src="/copy.detail.png"',html)
        self.assertNotIn('src="/copy.png"',html)
        self.assertIn('Hier den Zielordner eintragen.',html)
        reference=site.read(site.DIST / 'doku/steps/copy/vertrag.json')['screenshots'][0]
        self.assertEqual('copy.detail.png',reference['file'])
        self.assertEqual('Hier den Zielordner eintragen.',reference['markers'][0]['explanation'])

    def test_contract_visual_uses_actual_names_and_explanations(self):
        item=dict(id='state',name='Status',fields=[dict(id='timeout_ms',name='Zeitlimit',type='Integer',defaultValue=250)],options=['Completed'])
        html=site.contract_visual(item,dict(purpose='Zweck',example='Beispiel',errors='Fehler',**{'field.timeout_ms':'Millisekunden einstellen.','option.Completed':'Abgeschlossen.'}))
        self.assertIn('timeout_ms',html);self.assertIn('250',html);self.assertIn('Millisekunden einstellen.',html);self.assertIn('Abgeschlossen.',html)

    def test_user_field_options_and_visibility_are_explained_without_opening_developer_docs(self):
        item = dict(fields=[dict(id='action', name='Aktion', descriptor=dict(Options=[dict(Value='Copy', DisplayName='Kopieren')], DefaultValue='Copy')),
                            dict(id='target_path', name='Ziel', descriptor=dict(Advanced=True, VisibleWhen=dict(FieldId='action', EqualsValue='Copy')))])
        notes = {'field.action': 'Aktion auswählen.', 'field.action.option.Copy': 'Quelle bleibt erhalten.', 'field.target_path': 'Zielordner auswählen.'}
        rows = site.ui_field_rows(item, notes)
        self.assertIn('Quelle bleibt erhalten.', rows[0][1])
        self.assertIn('Kopieren', rows[0][1])
        self.assertIn('Aktion = Kopieren', rows[1][1])
        self.assertIn('Erweitert', rows[1][1])
        self.assertIn('C:', rows[1][2])

    def test_user_nested_fields_have_explanations_and_examples_but_hide_binding_bookkeeping(self):
        item = dict(schema=[dict(id='settings.roi.width', type='Int32', defaultValue=300),
                            dict(id='settings.target.process_name', type='String', defaultValue=''),
                            dict(id='settings.target.process_source.source_id', type='String', defaultValue='')])
        notes = {'schema.settings.roi.width': 'Breite des Suchbereichs.', 'schema.settings.target.process_name': 'Programm ohne Endung.'}
        rows = site.ui_nested_rows(item, notes)
        self.assertEqual(2, len(rows))
        self.assertIn('Breite (px)', rows[0][0])
        self.assertEqual('300', rows[0][2])
        self.assertEqual('notepad', rows[1][2])
        self.assertIn('Programm ohne Endung.', rows[1][1])

    def test_flat_model_members_of_a_composite_editor_are_visible_to_ui_readers(self):
        item = dict(fields=[dict(id='yolo_selection', name='Modell und Klasse')], schema=[
            dict(id='settings.model', type='String', defaultValue=''),
            dict(id='settings.class_name', type='String', defaultValue=''),
            dict(id='settings.image_source.provider_id', type='String', defaultValue='')])
        rows = site.ui_nested_rows(item, {'schema.settings.model': 'Zuerst das Modell wählen.', 'schema.settings.class_name': 'Eine Modellklasse wählen.'})
        self.assertEqual(2, len(rows))
        self.assertEqual('YOLO-Modell', rows[0][0])
        self.assertEqual('Beispielmodell.onnx', rows[0][2])
        self.assertEqual('person', rows[1][2])

    def test_compatibility_only_fields_do_not_promise_a_nonexistent_ui_control(self):
        item = dict(uiFieldIds=['executable_path'], fields=[
            dict(id='action', name='Aktion'), dict(id='executable_path', name='Programmpfad', descriptor={'VisibleWhen': {'FieldId': 'action', 'EqualsValue': 'Start'}})])
        rows = site.ui_field_rows(item, {'field.action': 'Dateivertrag.', 'field.executable_path': 'Programm auswählen.'})
        self.assertEqual(1, len(rows))
        self.assertEqual('Programmpfad', rows[0][0])
        self.assertNotIn('Sichtbar bei', rows[0][1])


if __name__ == '__main__':
    unittest.main()
