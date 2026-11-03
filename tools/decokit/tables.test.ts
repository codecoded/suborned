// Brief 072-C: a partial DecoKit run (--only X) merges into the shared tables (decokit_materials.json, decokit_manifest.json), never shrinks them.
// Runs tools/decokit/tables.py (pure Python, no Blender) on a temp folder.
import { expect, test } from 'bun:test';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const here = import.meta.dir;
const py = (code: string) => {
  const r = Bun.spawnSync(['python3', '-I', '-c', `import sys; sys.path.insert(0, ${JSON.stringify(here)}); import tables, json\n${code}`], { stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode) throw new Error(r.stderr.toString());
  return r.stdout.toString();
};

test('a partial run keeps every row on disk, updates its own, appends new ones in order', () => {
  const dir = mkdtempSync(join(tmpdir(), 'dk-tables-'));
  try {
    const f = join(dir, 'decokit_materials.json');
    writeFileSync(f, JSON.stringify({ materials: [{ name: 'DK_A', v: 1 }, { name: 'DK_CarEnamel_Coral', v: 1 }, { name: 'DK_B', v: 1 }] }));
    const out = py(`rows, kept = tables.write_merged(${JSON.stringify(f)}, "materials", [{"name": "DK_B", "v": 2}, {"name": "DK_New", "v": 2}], True)\nprint(kept)`);
    expect(out.trim()).toBe('2');
    const rows = JSON.parse(readFileSync(f, 'utf8')).materials;
    expect(rows).toEqual([{ name: 'DK_A', v: 1 }, { name: 'DK_CarEnamel_Coral', v: 1 }, { name: 'DK_B', v: 2 }, { name: 'DK_New', v: 2 }]);
    // the same partial run again: byte-identical (deterministic, idempotent)
    const before = readFileSync(f, 'utf8');
    py(`tables.write_merged(${JSON.stringify(f)}, "materials", [{"name": "DK_B", "v": 2}, {"name": "DK_New", "v": 2}], True)`);
    expect(readFileSync(f, 'utf8')).toBe(before);
  } finally { rmSync(dir, { recursive: true, force: true }); }
});

test('the manifest merges the same way; only a full run may drop rows; a missing table starts from the run', () => {
  const dir = mkdtempSync(join(tmpdir(), 'dk-tables-'));
  try {
    const f = join(dir, 'decokit_manifest.json');
    py(`tables.write_merged(${JSON.stringify(f)}, "assets", [{"name": "SideBay_3"}], True)`);
    expect(JSON.parse(readFileSync(f, 'utf8')).assets).toEqual([{ name: 'SideBay_3' }]);
    py(`tables.write_merged(${JSON.stringify(f)}, "assets", [{"name": "AlleyWall_3", "tris": 9}], True)`);
    expect(JSON.parse(readFileSync(f, 'utf8')).assets.map((a: { name: string }) => a.name)).toEqual(['SideBay_3', 'AlleyWall_3']);
    py(`tables.write_merged(${JSON.stringify(f)}, "assets", [{"name": "AlleyWall_3"}], False)`);
    expect(JSON.parse(readFileSync(f, 'utf8')).assets).toEqual([{ name: 'AlleyWall_3' }]);
  } finally { rmSync(dir, { recursive: true, force: true }); }
});

test('export_kit routes both shared tables through the merge', () => {
  const src = readFileSync(join(here, 'export_kit.py'), 'utf8');
  expect(src).toContain('tables.write_merged(DST / "decokit_materials.json", "materials"');
  expect(src).toContain('tables.write_merged(DST / "decokit_manifest.json", "assets"');
  expect(src).not.toMatch(/mats\.export_json\(DST/);
});
