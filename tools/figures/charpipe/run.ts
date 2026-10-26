// Character pipeline proof (brief 069): role rules -> 50 people -> LOD'd FBX on one skeleton, one shared material, plus manifest rows shaped like People/manifest.json's.
//   bun tools/figures/charpipe/run.ts [--out <dir>] [--jobs 6] [--master] [--twice]
// Default out: out/scratch/forma/char-pipeline/CharPipe. Needs Blender 5.2 and MPFB 2.0.18 with the MakeHuman system assets (CC0) installed in a scratch user dir
// (out/scratch/forma/char-pipeline/blender-user: never Sky's Blender config). Build time only; nothing here runs in the game.
// --twice builds everything a second time into <out>.again and compares every file's sha256 (the determinism proof); --master writes the base bodies and the slider masters.
// Heavy: start it through `bun run guard bun tools/figures/charpipe/run.ts`.
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { join, resolve, relative } from 'node:path';
import { createHash } from 'node:crypto';

const root = resolve(import.meta.dir, '../../..');
const here = import.meta.dir;
const scratch = join(root, 'out/scratch/forma/char-pipeline');
const arg = (n: string) => { const i = process.argv.indexOf(n); return i >= 0 ? process.argv[i + 1] : undefined; };
const out = resolve(arg('--out') ?? join(scratch, 'CharPipe'));
const jobs = Number(arg('--jobs') ?? 6);
const blender = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender';
const user = join(scratch, 'blender-user');
const env = { ...process.env, BLENDER_USER_RESOURCES: user, BLENDER_USER_EXTENSIONS: join(user, 'extensions'), MPFBDATA: join(user, 'extensions/.user/user_default/mpfb/data') };
if (!existsSync(join(user, 'extensions/user_default/mpfb'))) throw new Error('MPFB is not in the scratch user dir: see the brief 069 handoff (dl/ and blender-user/)');

async function blend(args: string[]): Promise<string> {
  const p = Bun.spawn([blender, '-b', '--factory-startup', '-P', ...args], { env, stdout: 'pipe', stderr: 'pipe' });
  const text = (await new Response(p.stdout).text()) + (await new Response(p.stderr).text());
  await p.exited;
  if (p.exitCode !== 0 || /Traceback|^Error: Python/m.test(text)) { console.error(text.split('\n').filter(l => /rror|Trace|File|line/.test(l)).join('\n')); process.exit(1); }
  return text;
}

async function buildAll(dir: string) {
  rmSync(dir, { recursive: true, force: true }); mkdirSync(dir, { recursive: true });
  // stage 3, clips: the figures' speeds read the clip rows, so clips go first
  { const t = await blend([join(here, 'pipeline/clips.py'), '--', 'export', dir]); for (const l of t.split('\n')) if (l.startsWith('CLIP')) console.log(l); }
  const specs = join(dir, 'specs.json');
  const pk = Bun.spawnSync(['python3', '-I', join(here, 'pick.py'), specs], { stdout: 'pipe', stderr: 'pipe' });
  if (pk.exitCode !== 0) throw new Error(pk.stderr.toString());
  const ids: string[] = JSON.parse(readFileSync(specs, 'utf8')).people.map((p: { id: string }) => p.id);
  const chunks: string[][] = Array.from({ length: jobs }, () => []);
  ids.forEach((id, i) => chunks[i % jobs]!.push(id));
  const t0 = performance.now();
  const logs = await Promise.all([...chunks.filter(c => c.length).map(c => blend([join(here, 'build.py'), '--', specs, dir, ...c])), blend([join(here, 'build.py'), '--', specs, dir, 'shared'])]);
  const secs = (performance.now() - t0) / 1000;
  for (const l of logs.join('\n').split('\n')) if (l.startsWith('DONE')) console.log(l);
  // the manifest: rows in id order, totals, and what Latch needs to know
  const rows = ids.map(id => JSON.parse(readFileSync(join(dir, 'rows', id.replaceAll('-', '_') + '.json'), 'utf8')));
  const manifest = {
    schema: 'suborned.people/charpipe-proof-1', brief: '069', owner: 'Forma', standIn: true,
    conventions: {
      unit: 'metres, FBX unit scale 1 (file units cm, Unity useFileScale on)', axes: 'Y up, figure faces +Z', origin: 'feet on y = 0, centred on x and z',
      rig: 'Humanoid, one skeleton for every figure: MPFB mixamo_unity cut to 52 bones, Mixamo names without prefix (Hips, Spine, Spine1, Spine2, Neck, Head, Left/Right Shoulder, Arm, ForeArm, Hand, UpLeg, Leg, Foot, ToeBase, and Thumb/Index/Middle/Ring/Pinky 1-3 on each hand). Bone lengths follow each person\'s sliders (MPFB refits the rig), so each FBX makes its own avatar; the names, hierarchy and axes are the same in all.',
      lods: 'one FBX per figure holds the skeleton and both LODs as skinned meshes <id>_LOD0 (MakeHuman male1591/female1605 proxy + wardrobe) and <id>_LOD1 (proxy741 + the same wardrobe). Skin weights: 4 per vertex. lodScreenHeights [0.08, 0.012] as today\'s crowd: play zoom draws LOD1.',
      materials: 'ONE material for every figure and LOD: Shared/Materials/cp_palette.mat (URP Lit, _BaseMap cp_palette_albedo.png, _MetallicGlossMap cp_palette_metal.png). The colourway is baked into each figure\'s UVs (each face points at its colour\'s 16 px cell of the 256 px palette); UV2 (Channel) holds the colour role index for a later mask shader. Textures: no mips, point filter, uncompressed.',
      clips: 'People/Clips unchanged: any Humanoid clip plays on any figure.',
    },
    totals: { figures: rows.length, buildSeconds: Math.round(secs), jobs },
    figures: rows,
  };
  writeFileSync(join(dir, 'manifest.json'), JSON.stringify(manifest, null, 1) + '\n');
  console.log(`built ${rows.length} figures in ${secs.toFixed(0)} s on ${jobs} jobs -> ${relative(root, dir)}`);
  return secs;
}

function hashes(dir: string): Map<string, string> {
  const m = new Map<string, string>();
  const walk = (d: string) => { for (const f of readdirSync(d).sort()) { const p = join(d, f); if (statSync(p).isDirectory()) walk(p); else if (!f.endsWith('manifest.json')) m.set(relative(dir, p), createHash('sha256').update(readFileSync(p)).digest('hex')); } };
  walk(dir); return m;
}

if (process.argv.includes('--master')) {
  console.log((await blend([join(here, 'build.py'), '--', '-', out, '--master'])).split('\n').filter(l => l.startsWith('MASTER')).join('\n'));
  process.exit(0);
}
await buildAll(out);
if (process.argv.includes('--twice')) {
  const again = out + '.again';
  await buildAll(again);
  const a = hashes(out), b = hashes(again);
  const diff = [...new Set([...a.keys(), ...b.keys()])].filter(k => a.get(k) !== b.get(k));
  const report = { brief: '069', what: 'every file of two full builds from the same rules, sha256 compared (manifest.json left out: it holds the build seconds)', files: a.size, differing: diff,
    fbx: [...a.entries()].filter(([k]) => k.endsWith('.fbx')).map(([k, v]) => ({ file: k, sha256: v, again: b.get(k) })) };
  writeFileSync(join(scratch, 'determinism.json'), JSON.stringify(report, null, 1) + '\n');
  console.log(diff.length ? `DIFFER: ${diff.length} of ${a.size} files (${diff.slice(0, 5).join(', ')})` : `IDENTICAL: all ${a.size} files byte-identical across two builds`);
  if (diff.length) process.exit(1);
}
