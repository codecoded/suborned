// The cell from Happy Mouse Labs' game bundles (brief 112): unzip the four bundles, convert each in Blender (cell_hml.py), write the cell's manifest, install.
//   bun tools/figures/cell_hml.ts [--only id,id] [--install] [--no-build]
// Bundles: $SUBORNED_STORE/characters/cell_hml/<id>-game-bundle.zip (default store ~/Projects/the-suborned-store), each checked against the sha256 in cell_hml.json.
// Out: out/people-models/CellHml (generated, git-ignored). The manifest FigureBuild reads is committed: briefs/112/people/manifest-cell.json.
// --install mirrors each figure's folder to unity/Assets/Suborned/People/Figures/<id> (those four are tracked in git: .gitignore), over the stand-in of the same id. Run it after
// `bun tools/figures/run.ts --install`, which puts the stand-ins back. --no-build installs the last build as it is.
import { existsSync, mkdirSync, readFileSync, rmSync, utimesSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { homedir } from 'node:os';

const root = resolve(import.meta.dir, '../..');
const has = (n: string) => process.argv.includes(n);
const arg = (n: string) => { const i = process.argv.indexOf(n); return i >= 0 ? process.argv[i + 1] : undefined; };
const specPath = join(import.meta.dir, 'cell_hml.json');
const spec = JSON.parse(readFileSync(specPath, 'utf8'));
const all: string[] = Object.keys(spec.figures);
const ids = (arg('--only') ?? '').split(',').filter(Boolean);
for (const id of ids) if (!all.includes(id)) throw new Error(`no figure ${id} in cell_hml.json (${all.join(', ')})`);
const build = ids.length ? ids : all;
const out = join(root, 'out/people-models/CellHml'), bundles = join(out, '.bundles');
const manifestPath = join(root, 'briefs/112/people/manifest-cell.json');
const blender = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender';
const store = join(process.env.SUBORNED_STORE ?? join(homedir(), 'Projects/the-suborned-store'), spec.store);

let version = '';
if (!has('--no-build')) {
  if (!existsSync(blender)) throw new Error(`Blender not found at ${blender} (set BLENDER)`);
  mkdirSync(bundles, { recursive: true });
  for (const id of build) {
    const zip = join(store, spec.figures[id].bundle);
    if (!existsSync(zip)) throw new Error(`bundle not found: ${zip} (set SUBORNED_STORE)`);
    const sha = new Bun.CryptoHasher('sha256').update(readFileSync(zip)).digest('hex');
    if (sha !== spec.figures[id].sha256) throw new Error(`${zip} is not the bundle cell_hml.json names (sha256 ${sha})`);
    rmSync(join(bundles, id), { recursive: true, force: true }); mkdirSync(join(bundles, id), { recursive: true });
    const r = Bun.spawnSync(['unzip', '-q', '-o', zip, '-d', join(bundles, id)]);
    if (r.exitCode !== 0) throw new Error(`unzip failed: ${zip}`);
  }
  // one Blender per figure, side by side (about half a minute in all). The same bytes every build: the FBX exporter walks sets of bones, whose order follows Python's
  // string hash, so the hash seed is fixed (Blender reads it only with --python-use-system-env; PYTHONPATH and PYTHONHOME are kept out of it).
  const env: Record<string, string> = { ...(process.env as Record<string, string>), PYTHONHASHSEED: '0' }; delete env.PYTHONPATH; delete env.PYTHONHOME;
  const runs = build.map(id => ({ id, p: Bun.spawn([blender, '-b', '--python-use-system-env', '-P', join(import.meta.dir, 'cell_hml.py'), '--', specPath, bundles, out, id], { stdout: 'pipe', stderr: 'pipe', env }) }));
  for (const { id, p } of runs) {
    const text = await new Response(p.stdout).text() + await new Response(p.stderr).text(); await p.exited;
    writeFileSync(join(out, `build-${id}.log`), text);
    const done = text.split('\n').find(l => l.startsWith(`DONE ${id} `));
    if (p.exitCode !== 0 || !done || /Traceback/.test(text)) { console.error(text.split('\n').filter(l => /rror|Trace|File "|line \d+/.test(l)).join('\n')); throw new Error(`${id}: the build failed (out/people-models/CellHml/build-${id}.log)`); }
    console.log(done); version ||= /Blender (\S+)/.exec(text)?.[1] ?? '';
  }
  // the manifest: every figure's row, in the spec's order (a figure not rebuilt keeps the row of its last build)
  const rows = all.map(id => { const p = join(out, `.cell.${id}.json`); if (!existsSync(p)) throw new Error(`${id} has no build yet: run without --only`); return JSON.parse(readFileSync(p, 'utf8')); });
  const manifest = {
    schema: 'suborned.cell-hml/1', brief: spec.brief, owner: 'Forma', for: 'Latch',
    built: { by: 'tools/figures/cell_hml.ts (cell_hml.py, cell_hml_clips.py)', blender: version, spec: 'tools/figures/cell_hml.json', store: spec.store },
    conventions: {
      unit: 'metres, FBX unit scale 1', axes: 'Y up, the figure faces +Z', origin: 'feet on y = 0',
      rig: 'Generic: each figure on its own 65-bone skeleton, its clips its own (no Humanoid retarget). The host finds Head, Hips, RightShoulder and RightHand by name.',
      detail: 'as the bundle has it: one mesh level, no decimation, no texture resize (compression goes through Sky)',
      clips: 'named after the shared controller\'s states (FigureBuild.CellOverrides swaps them in by name); `props` lists the props an action shows',
      props: 'prop_<name>: one mesh skinned rigidly to its socket bone; shown in the states listed (always: every state)',
    },
    figures: rows,
  };
  mkdirSync(join(root, 'briefs/112/people'), { recursive: true });
  writeFileSync(manifestPath, JSON.stringify(manifest, null, 1) + '\n');
  console.log('manifest', manifestPath.slice(root.length + 1), rows.map((r: any) => `${r.id} ${r.clips.length} clips`).join(', '));
}

if (has('--install')) {
  const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
  const dst = join(root, 'unity/Assets/Suborned/People/Figures');
  if (!existsSync(dst)) throw new Error(`no ${dst}: install the stand-ins first (bun tools/figures/run.ts --install)`);
  for (const id of build) {
    const src = join(out, 'Figures', id), row = manifest.figures.find((f: any) => f.id === id);
    const sha = existsSync(join(src, `${id}.fbx`)) ? new Bun.CryptoHasher('sha256').update(readFileSync(join(src, `${id}.fbx`))).digest('hex') : '';
    if (sha !== row?.sha256) throw new Error(`${id}: out/people-models/CellHml holds a different build than the manifest (rebuild without --no-build)`);
    const r = Bun.spawnSync(['rsync', '-a', '--delete', src + '/', join(dst, id) + '/']);
    if (r.exitCode !== 0) throw new Error(`rsync failed: ${id}`);
  }
  utimesSync(manifestPath, new Date(), new Date()); // FigureBuild.Library rebuilds the figure library when this manifest is newer than its controller
  console.log('installed', build.join(', '), 'to', dst.slice(root.length + 1));
}
