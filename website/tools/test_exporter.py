"""Exercise the real exporter against equivalent LF and CRLF checkouts."""
from pathlib import Path
from tempfile import TemporaryDirectory
import json
import subprocess
import sys
import unittest

EXPORTER = Path(sys.argv.pop(1)).resolve()
ROOT = Path(__file__).resolve().parents[2]


class ExporterChecks(unittest.TestCase):
    def test_checkout_line_endings_preserve_export_but_behavior_edits_change_fingerprint(self):
        sources = json.loads((ROOT / 'website/docs/metadata.json').read_text(encoding='utf-8'))['sources']
        with TemporaryDirectory() as directory:
            fixture = Path(directory) / 'repository'
            for relative in sources:
                target = fixture / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes((ROOT / relative).read_bytes().replace(b'\r\n', b'\n'))

            def export(name):
                output = Path(directory) / name
                result = subprocess.run(['dotnet', str(EXPORTER), str(fixture), str(output)],
                                        capture_output=True, text=True, timeout=60)
                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                return json.loads((output / 'metadata.json').read_text(encoding='utf-8'))

            baseline = export('lf')
            for relative in sources:
                target = fixture / relative
                target.write_bytes(target.read_bytes().replace(b'\n', b'\r\n'))
            self.assertEqual(baseline, export('crlf'))
            changed = next(relative for relative in sources if relative.endswith('.cs'))
            with (fixture / changed).open('ab') as stream:
                stream.write(b'\r\n// Changed behavior dependency\r\n')
            self.assertNotEqual(baseline['sources'][changed], export('changed')['sources'][changed])


if __name__ == '__main__':
    unittest.main()
