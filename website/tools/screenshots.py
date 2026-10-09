"""Select, record and verify real application screenshots and their source dependencies."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import struct

ROOT = Path(__file__).resolve().parents[2]


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def catalog(root):
    value = read(root / 'website/docs/screenshots.json')
    entries = value['entries']
    ids = [entry['id'] for entry in entries]
    files = [entry['file'] for entry in entries]
    if len(set(ids)) != len(ids) or len(set(files)) != len(files):
        raise ValueError('Screenshot IDs and files must be unique.')
    for entry in entries:
        if not re.fullmatch(r'[a-z0-9]+(?:-[a-z0-9]+)*', entry['id']) or '/' in entry['file'] or '\\' in entry['file'] or Path(entry['file']).name != entry['file'] or not entry['file'].endswith('.png'):
            raise ValueError('Invalid screenshot ID or file: ' + entry['id'])
        if entry['width'] < 1 or entry['height'] < 1 or not entry['caption'] or not entry['pages']:
            raise ValueError('Screenshot needs dimensions, caption and documentation pages: ' + entry['id'])
        if entry.get('crop'):
            crop = entry['crop']
            if len(crop) != 4 or any(type(v) is not int for v in crop) or min(crop[:2]) < 0 or min(crop[2:]) < 1 or crop[0]+crop[2]>entry['width'] or crop[1]+crop[3]>entry['height']:
                raise ValueError('Screenshot crop must stay within the rendered image: ' + entry['id'])
        for page in entry['pages']:
            if not page.startswith('/doku/') or '..' in page or not page.endswith('/'):
                raise ValueError('Invalid documentation target: ' + page)
    return value


def fingerprint(root, configuration, entry):
    sources = {}
    for pattern in configuration['commonSources'] + entry['sources']:
        if Path(pattern).is_absolute() or '..' in Path(pattern).parts:
            raise ValueError('Source patterns must stay inside the repository: ' + pattern)
        matches = sorted(path for path in root.glob(pattern) if path.is_file())
        if not matches:
            raise ValueError('Screenshot dependency matches no files: ' + pattern)
        for path in matches:
            sources[path.relative_to(root).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    data = dict(id=entry['id'], file=entry['file'], width=entry['width'], height=entry['height'],
                fieldDetails=entry.get('fieldDetails', False), crop=entry.get('crop'), labels=entry.get('markLabels'), notes=entry.get('markNotes'), caption=entry['caption'], pages=entry['pages'],
                theme=configuration['theme'], culture=configuration['culture'], sources=sources)
    return hashlib.sha256(json.dumps(data, sort_keys=True).encode()).hexdigest()


def image_hash(root, entry):
    path = root / 'website/dist' / entry['file']
    if not path.is_file():
        raise ValueError('Screenshot missing: ' + entry['file'])
    data = path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n' or len(data) < 24:
        raise ValueError('Invalid PNG: ' + entry['file'])
    if struct.unpack('>II', data[16:24]) != (entry['width'], entry['height']):
        raise ValueError('Screenshot dimensions differ from catalog: ' + entry['file'])
    if entry.get('crop'):
        base = Path(entry['file']).stem
        detail = root / 'website/dist' / (base+'.detail.png')
        receipt = root / 'website/dist' / (base+'.detail.json')
        if not detail.exists() or not receipt.exists():
            raise ValueError('Annotated detail missing: ' + base)
        info = read(receipt)
        bitmap = detail.read_bytes()
        if info['width'] != entry['crop'][2]+44 or not 1 <= info['height'] <= (30000 if entry.get('fieldDetails') else entry['crop'][3]) or bitmap[:8] != b'\x89PNG\r\n\x1a\n' or len(bitmap) < 24 or struct.unpack('>II', bitmap[16:24]) != (info['width'], info['height']):
            raise ValueError('Annotated detail dimensions differ from crop: ' + base)
        if not info['markers'] or any(not marker['label'] for marker in info['markers']):
            raise ValueError('Annotated detail has no real UI targets: ' + base)
        if info.get('mode') == 'fields':
            for marker in info['markers']:
                x, y, w, h = marker.get('bounds', [0, 0, 0, 0])
                if any(not str(v.get('value', '')).strip() or str(v.get('value', '')).strip().startswith(('Variable wählen', 'Variable nicht', 'Ungültige Referenz')) for v in marker.get('inputs', [])):
                    raise ValueError('Field example contains an empty input: ' + base + ' / ' + marker['label'])
                if w < 20 or h < 20 or min(x, y) < 0 or x+w > info['width'] or y+h > info['height']:
                    raise ValueError('Field annotation must contain the complete native field: ' + base)
        data += bitmap + receipt.read_bytes()
    return hashlib.sha256(data).hexdigest()


def state(root):
    path = root / 'website/docs/screenshots-state.json'
    return read(path) if path.exists() else {}


def selected(configuration, ids=None):
    requested = set(ids.split(',')) if ids else {entry['id'] for entry in configuration['entries']}
    unknown = requested - {entry['id'] for entry in configuration['entries']}
    if unknown:
        raise ValueError('Unknown screenshot IDs: ' + ', '.join(sorted(unknown)))
    return [entry for entry in configuration['entries'] if entry['id'] in requested]


def stale(root, configuration, entry, records):
    record = records.get(entry['id'], {})
    try:
        return record.get('source') != fingerprint(root, configuration, entry) or record.get('image') != image_hash(root, entry)
    except ValueError:
        return True


def check(root, ids=None, allow_unreviewed=False):
    configuration, records = catalog(root), state(root)
    for entry in selected(configuration, ids):
        # Dependency errors are not allowed to hide behind a stale status.
        fingerprint(root, configuration, entry)
        if stale(root, configuration, entry, records):
            raise ValueError('Screenshot stale; render and inspect: ' + entry['id'])
        if not allow_unreviewed and not records[entry['id']].get('reviewed'):
            raise ValueError('Screenshot needs visual review: ' + entry['id'])
        for page in entry['pages']:
            path = root / 'website/dist' / page.lstrip('/') / 'index.html'
            displayed = Path(entry['file']).stem+'.detail.png' if entry.get('crop') else entry['file']
            if not path.exists() or ('src="/' + displayed + '"') not in path.read_text(encoding='utf-8'):
                raise ValueError('Documentation image missing from assigned page: ' + page)


def update(root, ids, approve=False):
    configuration, records = catalog(root), state(root)
    for entry in selected(configuration, ids):
        if approve:
            if stale(root, configuration, entry, records):
                raise ValueError('Cannot approve a missing, changed or stale render: ' + entry['id'])
            records[entry['id']]['reviewed'] = True
        else:
            records[entry['id']] = dict(source=fingerprint(root, configuration, entry), image=image_hash(root, entry), reviewed=False)
    (root / 'website/docs/screenshots-state.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['list', 'select', 'files', 'record', 'approve', 'check'])
    parser.add_argument('--ids')
    parser.add_argument('--changed', action='store_true')
    parser.add_argument('--allow-unreviewed', action='store_true')
    args = parser.parse_args()
    configuration, records = catalog(ROOT), state(ROOT)
    if args.mode == 'files':
        for entry in selected(configuration, args.ids):
            print(entry['file'])
            if entry.get('crop'):
                base=Path(entry['file']).stem
                print(base+'.detail.png'); print(base+'.detail.json')
    elif args.mode in ('list', 'select'):
        entries = selected(configuration, args.ids)
        for entry in entries:
            fingerprint(ROOT, configuration, entry)
        if args.changed:
            entries = [entry for entry in entries if stale(ROOT, configuration, entry, records)]
        if args.mode == 'select':
            print(','.join(entry['id'] for entry in entries))
        else:
            for entry in entries:
                status = 'stale' if stale(ROOT, configuration, entry, records) else 'reviewed' if records[entry['id']].get('reviewed') else 'needs review'
                print(entry['id'] + ' | ' + status + ' | ' + ', '.join(entry['pages']))
    elif args.mode == 'check':
        check(ROOT, args.ids, args.allow_unreviewed)
        print('Screenshot dependencies, annotated details, review status and documentation placement are valid.')
    else:
        if not args.ids:
            raise ValueError('Recording or approving requires explicit --ids.')
        update(ROOT, args.ids, approve=args.mode == 'approve')
        print('Visually reviewed screenshots recorded.' if args.mode == 'approve' else 'Rendered screenshots recorded; visual inspection and approve are required.')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, FileNotFoundError) as error:
        raise SystemExit(str(error))
