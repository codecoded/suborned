// bun run decokit [--only a,b] [--cats X] [--out dir]: run the Blender DecoKit generator (export_kit.py) from this repo.
// Output: $DECOKIT_OUT, else out/decokit/ (never committed). Regen orders are read from the plan (orders.ts) and passed as --orders.
// Env: BLENDER (default /Applications/Blender.app/Contents/MacOS/Blender), SUBORNED_STORE (source-art textures).
import { mkdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { type Built, fitRow, printTable } from './fit';
import { readOrders, writeOrders } from './orders';
import { WAVE1 } from '../kit/tags';

const repo = resolve(import.meta.dir, '../..');
const argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1] : undefined; };
const out = resolve(opt('--out') ?? process.env.DECOKIT_OUT ?? resolve(repo, 'out/decokit'));
mkdirSync(out, { recursive: true });
const orders = resolve(out, 'orders.json'); writeOrders(orders);
const pass = argv.filter((_, i) => argv[i] !== '--out' && argv[i - 1] !== '--out');
const blender = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender';
const run = (args: string[]) => Bun.spawnSync([blender, '-b', '-t', '1', '-P', resolve(import.meta.dir, 'export_kit.py'), '--', ...args, '--orders', orders], { env: { ...process.env, DECOKIT_OUT: out, PYTHONHASHSEED: '0' }, stdout: 'inherit', stderr: 'inherit' }).exitCode;
// The budgeted ground textures (brief 026): numpy and Pillow, no Blender; export_kit copies them with the pieces.
const gt = Bun.spawnSync(['python3', resolve(import.meta.dir, 'ground_tex.py')], { stdout: 'inherit', stderr: 'inherit' }); if (gt.exitCode) process.exit(gt.exitCode);
const rd = Bun.spawnSync(['python3', resolve(import.meta.dir, 'road_tex.py')], { stdout: 'inherit', stderr: 'inherit' }); if (rd.exitCode) process.exit(rd.exitCode); // brief 028: the road enamel and pearl maps (the testbed's, full size), beside the ground set
const ct = Bun.spawnSync(['python3', resolve(import.meta.dir, 'cabinet_tex.py')], { stdout: 'inherit', stderr: 'inherit' }); if (ct.exitCode) process.exit(ct.exitCode);
const pt = Bun.spawnSync(['python3', resolve(import.meta.dir, 'pavilion_tex.py')], { stdout: 'inherit', stderr: 'inherit' }); if (pt.exitCode) process.exit(pt.exitCode);
const ft = Bun.spawnSync(['python3', resolve(import.meta.dir, 'fountain_tex.py')], { stdout: 'inherit', stderr: 'inherit' }); if (ft.exitCode) process.exit(ft.exitCode);
const rt = Bun.spawnSync(['python3', resolve(import.meta.dir, 'robot_tex.py')], { stdout: 'inherit', stderr: 'inherit' }); if (rt.exitCode) process.exit(rt.exitCode);
const rf = Bun.spawnSync(['python3', resolve(import.meta.dir, 'robot_face_stage.py')], { stdout: 'inherit', stderr: 'inherit' }); if (rf.exitCode) process.exit(rf.exitCode);
// Default: the plan's generated pieces (full export + LODs), then LODs only for the adopted DecoKit pieces (their binaries stay the store's).
const passes = pass.length ? [pass] : [['--only', readOrders().pieces.map(o => o.id).join(',')], ['--lods-only', '--only', [...WAVE1].join(',')]];
for (const a of passes) { const c = run(a); if (c) process.exit(c); }
// The fit table for every generated piece in this run's manifest.
const manifest = JSON.parse(readFileSync(resolve(out, 'decokit_manifest.json'), 'utf8')).assets as { name: string; order?: { hash: string }; built?: Built }[];
const orderSet = readOrders(), rows = manifest.filter(a => a.order && a.built).map(a => fitRow(orderSet, a.name, a.order!, a.built!));
if (rows.length) { console.log('\n' + printTable(rows)); if (rows.some(r => !r.fits)) process.exit(1); }
