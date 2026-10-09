"""Observable screenshot selection, freshness, review and placement contracts."""
from pathlib import Path
from tempfile import TemporaryDirectory
import json
import struct
import unittest
import zlib
import screenshots as images


def png(width, height):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress((b'\0' + b'\xff\xff\xff' * width) * height)) + chunk(b'IEND', b'')


class ScreenshotChecks(unittest.TestCase):
    def setUp(self):
        temporary = TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        for folder in ['website/docs', 'website/dist/doku/jobs', 'app/jobs', 'app/logs']:
            (self.root / folder).mkdir(parents=True)
        (self.root / 'app/theme.xaml').write_text('Light', encoding='utf-8')
        self.config = dict(theme='Light.Blue', culture='de-DE', commonSources=['app/theme.xaml'], entries=[])
        for id in ['jobs', 'logs']:
            (self.root / 'app' / id / 'view.xaml').write_text('view', encoding='utf-8')
            entry = dict(id=id, file=id + '.png', width=4, height=2, caption='Beispieldaten', pages=['/doku/jobs/'], sources=['app/' + id + '/*.xaml'])
            self.config['entries'].append(entry)
            (self.root / 'website/dist' / entry['file']).write_bytes(png(4, 2))
        (self.root / 'website/dist/doku/jobs/index.html').write_text('<img src="/jobs.png"><img src="/logs.png">', encoding='utf-8')
        self.write_catalog()
        images.update(self.root, 'jobs,logs')
        images.update(self.root, 'jobs,logs', approve=True)

    def write_catalog(self):
        (self.root / 'website/docs/screenshots.json').write_text(json.dumps(self.config), encoding='utf-8')

    def test_current_reviewed_images_pass_without_mutation(self):
        before = (self.root / 'website/docs/screenshots-state.json').read_bytes()
        images.check(self.root)
        self.assertEqual(before, (self.root / 'website/docs/screenshots-state.json').read_bytes())

    def test_only_images_depending_on_changed_view_are_stale(self):
        (self.root / 'app/jobs/view.xaml').write_text('new field', encoding='utf-8')
        records = images.state(self.root)
        self.assertEqual(['jobs'], [entry['id'] for entry in self.config['entries'] if images.stale(self.root, self.config, entry, records)])
        with self.assertRaisesRegex(ValueError, 'Screenshot stale.*jobs'):
            images.check(self.root)

    def test_new_source_files_in_a_watched_directory_invalidate_capture(self):
        (self.root / 'app/jobs/new-editor.xaml').write_text('editor', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Screenshot stale'):
            images.check(self.root, 'jobs')

    def test_render_requires_new_visual_review_and_cannot_approve_stale_sources(self):
        images.update(self.root, 'jobs')
        with self.assertRaisesRegex(ValueError, 'visual review'):
            images.check(self.root)
        images.check(self.root, 'jobs', allow_unreviewed=True)
        (self.root / 'app/jobs/view.xaml').write_text('changed after rendering', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'Cannot approve'):
            images.update(self.root, 'jobs', approve=True)

    def test_manually_modified_png_is_not_accepted(self):
        (self.root / 'website/dist/jobs.png').write_bytes(png(4, 2) + b'modified')
        with self.assertRaisesRegex(ValueError, 'Screenshot stale'):
            images.check(self.root)

    def test_missing_documentation_placement_fails(self):
        (self.root / 'website/dist/doku/jobs/index.html').write_text('<img src="/logs.png">', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'assigned page'):
            images.check(self.root)

    def test_incorrect_dimensions_and_missing_dependency_are_rejected(self):
        (self.root / 'website/dist/jobs.png').write_bytes(png(5, 2))
        with self.assertRaisesRegex(ValueError, 'dimensions'):
            images.update(self.root, 'jobs')
        self.config['entries'][0]['sources'] = ['app/missing/*.xaml']
        self.write_catalog()
        with self.assertRaisesRegex(ValueError, 'matches no files'):
            images.check(self.root, 'jobs')

    def test_unknown_ids_and_duplicate_targets_are_rejected(self):
        with self.assertRaisesRegex(ValueError, 'Unknown screenshot'):
            images.selected(self.config, 'unknown')
        self.config['entries'][1]['file'] = 'jobs.png'
        self.write_catalog()
        with self.assertRaisesRegex(ValueError, 'unique'):
            images.catalog(self.root)

    def test_shared_theme_change_invalidates_all_images(self):
        (self.root / 'app/theme.xaml').write_text('changed style', encoding='utf-8')
        records = images.state(self.root)
        self.assertTrue(all(images.stale(self.root, self.config, entry, records) for entry in self.config['entries']))

    def test_crop_and_annotations_are_required_and_checked_for_drift(self):
        entry = self.config['entries'][0]
        entry.update(crop=[0, 0, 4, 2], markLabels=['Ziel'])
        self.write_catalog()
        with self.assertRaisesRegex(ValueError, 'detail missing'):
            images.update(self.root, 'jobs')
        detail = self.root / 'website/dist/jobs.detail.png'
        receipt = self.root / 'website/dist/jobs.detail.json'
        detail.write_bytes(png(48, 2))
        receipt.write_text(json.dumps(dict(width=48, height=2, markers=[dict(number=1, label='Ziel')])), encoding='utf-8')
        (self.root / 'website/dist/doku/jobs/index.html').write_text('<img src="/jobs.detail.png"><img src="/logs.png">', encoding='utf-8')
        images.update(self.root, 'jobs'); images.update(self.root, 'jobs', approve=True)
        images.check(self.root)
        receipt.write_text(json.dumps(dict(width=48, height=2, markers=[dict(number=1, label='Quelle')])), encoding='utf-8')
        self.assertTrue(images.stale(self.root, self.config, entry, images.state(self.root)))

    def test_crop_cannot_leave_real_capture_and_markers_cannot_be_empty(self):
        entry = self.config['entries'][0]
        entry['crop'] = [1, 0, 4, 2]
        self.write_catalog()
        with self.assertRaisesRegex(ValueError, 'crop must stay'):
            images.catalog(self.root)
        entry['crop'] = [0, 0, 4, 2]; self.write_catalog()
        (self.root / 'website/dist/jobs.detail.png').write_bytes(png(48, 2))
        (self.root / 'website/dist/jobs.detail.json').write_text(json.dumps(dict(width=48, height=2, markers=[])), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'no real UI targets'):
            images.update(self.root, 'jobs')

    def test_native_field_composite_accepts_full_fields_below_the_window_and_rejects_cut_frames(self):
        entry = self.config['entries'][0]
        entry.update(crop=[0, 0, 4, 2], fieldDetails=True)
        self.write_catalog()
        detail = self.root / 'website/dist/jobs.detail.png'
        receipt = self.root / 'website/dist/jobs.detail.json'
        detail.write_bytes(png(48, 120))
        info = dict(width=48, height=120, mode='fields', markers=[dict(number=1, label='Ziel', bounds=[5, 10, 40, 90])])
        receipt.write_text(json.dumps(info), encoding='utf-8')
        images.update(self.root, 'jobs')
        info['markers'][0]['bounds'] = [5, 10, 40, 111]
        receipt.write_text(json.dumps(info), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'complete native field'):
            images.update(self.root, 'jobs')

    def test_empty_visible_input_prevents_recording_a_field_example(self):
        entry = self.config['entries'][0]
        entry.update(crop=[0, 0, 4, 2], fieldDetails=True)
        self.write_catalog()
        (self.root / 'website/dist/jobs.detail.png').write_bytes(png(48, 120))
        info = dict(width=48, height=120, mode='fields', markers=[dict(number=1, label='Ziel', bounds=[5, 10, 40, 90], inputs=[dict(kind='text', value='')])])
        receipt = self.root / 'website/dist/jobs.detail.json'
        receipt.write_text(json.dumps(info), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'empty input'):
            images.update(self.root, 'jobs')
        info['markers'][0]['inputs'][0]['value'] = 'Variable nicht mehr vorhanden'
        receipt.write_text(json.dumps(info), encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'empty input'):
            images.update(self.root, 'jobs')
        info['markers'][0]['inputs'][0]['value'] = 'Beispiele/Archiv'
        receipt.write_text(json.dumps(info), encoding='utf-8')
        images.update(self.root, 'jobs')


if __name__ == '__main__':
    unittest.main()
