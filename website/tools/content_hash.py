"""Ignore Git's text line-ending conversion while retaining content drift checks."""
import hashlib


def normalize_line_endings(data):
    return data.replace(b'\r\n', b'\n')


def source_hash(path):
    return hashlib.sha256(normalize_line_endings(path.read_bytes())).hexdigest()
