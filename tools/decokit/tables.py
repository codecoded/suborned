"""The shared tables a DecoKit run writes beside the pieces (decokit_materials.json, decokit_manifest.json): merge, never shrink.

A partial run (`bun run decokit --only X`, `--cats`, `--lods-only`) knows only the rows its own imports and builds registered: materials a builder adds
lazily (car and robot enamels, regen orders, a family that failed to import) are missing from it. Brief 072-C: such a run updates the rows it has and keeps
every other row of the table already on disk, in that table's order; new rows follow in the run's order. Only a full run (no --only / --cats) may drop a row.
Pure Python (no bpy), so tools/decokit/tables.test.ts can test it.
"""

import json
from pathlib import Path


def merge_rows(old, new, key="name"):
    """Rows of `old` updated by `new` (matched on key), old order first, then new rows in their order. Never fewer rows than old."""
    fresh = {r[key]: r for r in new}
    out = [fresh.pop(r[key], r) for r in old]
    return out + [r for r in new if r[key] in fresh]


def read_rows(path, field):
    p = Path(path)
    if not p.exists():
        return []
    try:
        return json.loads(p.read_text()).get(field, [])
    except (json.JSONDecodeError, AttributeError):
        print(f"[DK-EXPORT] WARNING {p.name} unreadable: written from this run only")
        return []


def write_merged(path, field, new, partial, key="name"):
    """Write {field: rows} to path; a partial run merges into the rows on disk. Returns (rows written, rows kept from disk)."""
    old = read_rows(path, field) if partial else []
    rows = merge_rows(old, new, key) if partial else list(new)
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps({field: rows}, indent=1))
    names = {r[key] for r in new}
    return rows, sum(1 for r in old if r[key] not in names)
