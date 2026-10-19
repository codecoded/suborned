// Stand-in people for Unity (brief 020): Blender builds the figures and clips, python writes the manifest.
//   bun tools/figures/run.ts [--out <dir>] [--only id,id] [--install]   (the held props in props.json are built with every run)
// --install keeps Assets/Suborned/People/Generated (Latch's FigureBuild output: the library and the controller). Default out: out/people-models/People (generated, git-ignored). --install mirrors it to unity/Assets/Suborned/People (git-ignore that folder like Kit/).
// Needs Blender (BLENDER or /Applications/Blender.app) and the store (SUBORNED_STORE, default ~/Projects/the-suborned-store) with source-art/ and characters/.
import { existsSync, mkdirSync, readdirSync, rmSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { homedir } from 'node:os';

const root = resolve(import.meta.dir, '../..');
const arg = (n: string) => { const i = process.argv.indexOf(n); return i >= 0 ? process.argv[i + 1] : undefined; };
const out = resolve(arg('--out') ?? join(root, 'out/people-models/People'));
const only = (arg('--only') ?? '').split(',').filter(Boolean);
const blender = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender';
const store = (process.env.SUBORNED_STORE ?? join(homedir(), 'Projects/the-suborned-store')) + '/source-art';
if (!existsSync(blender)) throw new Error(`Blender not found at ${blender} (set BLENDER)`);
if (!existsSync(store)) throw new Error(`store not found at ${store} (set SUBORNED_STORE)`);

function run(cmd: string[]) {
  const r = Bun.spawnSync(cmd, { stdout: 'pipe', stderr: 'pipe', env: process.env });
  const text = r.stdout.toString() + r.stderr.toString();
  if (r.exitCode !== 0 || /Traceback|^Error: Python/m.test(text)) { console.error(text.split('\n').filter(l => /rror|Trace|File|line/.test(l)).join('\n')); process.exit(1); }
  return text;
}

if (!only.length) rmSync(join(out, 'Figures'), { recursive: true, force: true });
rmSync(join(out, 'Props'), { recursive: true, force: true });
mkdirSync(out, { recursive: true });
const t = run([blender, '-b', '-P', join(root, 'tools/figures/clips.py'), '--', join(root, 'tools/figures/clips.json'), out]);
console.log(t.split('\n').filter(l => l.startsWith('CLIP')).length, 'clips');
const b = run([blender, '-b', '-P', join(root, 'tools/figures/build.py'), '--', join(root, 'tools/figures/figures.json'), out, ...only]);
for (const l of b.split('\n')) if (l.startsWith('DONE')) console.log(l);
{ const pr = run([blender, '-b', '-P', join(root, 'tools/figures/props.py'), '--', join(root, 'tools/figures/props.json'), out]); for (const l of pr.split('\n')) if (l.startsWith('PROP')) console.log(l); }
const ver = /Blender (\S+)/.exec(b)?.[1] ?? '';
console.log(run(['python3', '-I', join(root, 'tools/figures/assemble.py'), out, ver]).trim());
if (process.argv.includes('--install')) {
  const dst = join(root, 'unity/Assets/Suborned/People');
  mkdirSync(dst, { recursive: true });
  const r = Bun.spawnSync(['rsync', '-a', '--delete', '--exclude', '.rows.*', '--exclude', 'Generated', '--exclude', 'Generated.meta', out + '/', dst + '/']);
  if (r.exitCode !== 0) throw new Error('rsync failed');
  console.log('installed to', dst, readdirSync(dst).length, 'entries');
}
