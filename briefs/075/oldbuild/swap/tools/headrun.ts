// Brief 075 remap step 2, the swap (Latch): run one Unity batch step with HEAD's versions of the superseded, uncommitted palette files in place
// ("Uncommitted superseded edits" in briefs/075/notes.md), then put the working tree's bytes back. Restore-in-memory: the working-tree bytes are held
// in memory (and copied to out/scratch/latch/075swap/wt-backup/ with their sha256, in case this process dies), HEAD's bytes are written for the length
// of the step, and the working-tree bytes are written back in `finally` and checked by sha256. Nothing is staged or committed; no git checkout.
//   bun briefs/075/oldbuild/swap/tools/headrun.ts <log> -- <Unity args after -projectPath unity ...>
// The Unity step runs through the playing guard (tools/game/playing.ts), batchmode, offscreen.
import { createHash } from 'node:crypto';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';

const root = resolve(import.meta.dir, '../../../../..');
export const FILES = [
  'contracts/style/suborned.style.json',
  'contracts/world/kit.json',
  'contracts/world/kit.collide.json',
  'unity/Assets/Suborned/Editor/World/facade.json',
  'unity/Assets/Suborned/Editor/World/look.json',
  'unity/Assets/Suborned/Editor/World/looks/harbour.json',
];
const UNITY = '/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity';
const sha = (b: Buffer | string) => createHash('sha256').update(b).digest('hex');
const args = process.argv.slice(2), sep = args.indexOf('--');
if (sep < 1) { console.error('usage: headrun.ts <log> -- <unity args>'); process.exit(2); }
const log = resolve(root, args[0]!), unityArgs = args.slice(sep + 1);
const backup = resolve(root, 'out/scratch/latch/075swap/wt-backup');
mkdirSync(backup, { recursive: true });
// checked before anything is read: after a crash the tree holds HEAD's bytes and only the backup holds the working tree's
if (existsSync(resolve(backup, 'IN-PROGRESS'))) { console.error('headrun: a previous run did not restore (wt-backup/IN-PROGRESS); restore from wt-backup first'); process.exit(2); }

const held = new Map<string, Buffer>();
const manifest: Record<string, { working: string; head: string }> = {};
for (const f of FILES) {
  const wt = readFileSync(resolve(root, f));
  const head = execFileSync('git', ['show', `HEAD:${f}`], { cwd: root, maxBuffer: 64 << 20 });
  held.set(f, wt);
  manifest[f] = { working: sha(wt), head: sha(head) };
  const b = resolve(backup, f.replaceAll('/', '__'));
  writeFileSync(b, wt);
}
writeFileSync(resolve(backup, 'manifest.json'), JSON.stringify({ when: new Date().toISOString(), files: manifest }, null, 1));
writeFileSync(resolve(backup, 'IN-PROGRESS'), String(process.pid));
let code = 1;
try {
  for (const f of FILES) writeFileSync(resolve(root, f), execFileSync('git', ['show', `HEAD:${f}`], { cwd: root, maxBuffer: 64 << 20 }));
  console.log(`headrun: HEAD versions in place for ${FILES.length} files; running Unity (log ${log})`);
  mkdirSync(dirname(log), { recursive: true });
  const r = spawnSync('bun', ['tools/game/playing.ts', UNITY, '-batchmode', '-quit', '-projectPath', resolve(root, 'unity'), '-logFile', log, ...unityArgs], { cwd: root, stdio: 'inherit' });
  code = r.status ?? 1;
} finally {
  const bad: string[] = [];
  for (const [f, b] of held) { writeFileSync(resolve(root, f), b); if (sha(readFileSync(resolve(root, f))) !== manifest[f]!.working) bad.push(f); }
  if (bad.length) { console.error(`headrun: RESTORE MISMATCH ${bad.join(', ')}: copies in ${backup}`); process.exit(3); }
  writeFileSync(resolve(backup, 'IN-PROGRESS'), ''); // emptied, then removed below
  execFileSync('rm', [resolve(backup, 'IN-PROGRESS')]);
  console.log(`headrun: working-tree bytes restored and checked (sha256) for ${held.size} files; Unity exit ${code}`);
}
process.exit(code);
