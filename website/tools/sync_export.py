from pathlib import Path
import argparse
import json
import sys

parser = argparse.ArgumentParser()
parser.add_argument('export', type=Path)
parser.add_argument('--check', action='store_true')
parser.add_argument('--write', action='store_true')
args = parser.parse_args()
destination = Path(__file__).resolve().parents[1] / 'docs'
files = [args.export / 'metadata.json', args.export / 'authoring.json'] + sorted((args.export / 'examples').rglob('*.json'))
expected = set()
for source in files:
    relative = source.relative_to(args.export)
    expected.add(relative.as_posix())
    target = destination / relative
    data = source.read_bytes()
    if args.check:
        if not target.exists() or json.loads(target.read_text(encoding='utf-8')) != json.loads(data):
            print('Export differs; run website/Build.ps1 and review docs: ' + str(relative), file=sys.stderr)
            sys.exit(1)
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
for target in (destination / 'examples').rglob('*.json'):
    if target.relative_to(destination).as_posix() not in expected:
        raise ValueError('Obsolete example configuration: ' + str(target))
print('Canonical export and example configurations match.' if args.check else 'Canonical export and example configurations updated.')
