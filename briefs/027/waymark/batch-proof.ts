// Equality proof for the player's batch mode (brief 042): N variants played one process each and in one batch, stdout compared byte for byte.
// bun batch-proof.ts [--dll <player.dll>] [--cwd <engine root>] [--base <script>] [--n 600]
import { playMany } from './batch';
const arg = (k: string, d: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1]! : d; };
const ROOT = '/Users/sky/Projects/suborned';
const dll = arg('--dll', `${ROOT}/out/splice042/p19/bin/player.dll`), cwd = arg('--cwd', `${ROOT}/out/splice042/e19`);
const base = arg('--base', `${ROOT}/contracts/missions/handover/routes/loud.json`), n = Number(arg('--n', '600'));
const site = JSON.parse(await Bun.file(`${ROOT}/sim/data/handover.site.json`).text());
const pts = site.cover.points.map((p: any) => p.at as [number, number]);
// a deterministic mix: waits moved around, the lock rules, enforcer modes, LOG=1 (long output), a WRITE script, an unknown base-free oddity (QUIET)
let seed = 7; const rnd = (m: number) => (seed = (seed * 1103515245 + 12345) & 0x7fffffff) % m;
const rules = ['near', 'threat', 'focus', 'blocker', 'none'], enfs = ['go', 'wait'];
const envs: Record<string, string>[] = [];
for (let k = 0; k < n; k++) {
  const e: Record<string, string> = {};
  for (const w of ['S', 'F', 'T']) { const p = pts[rnd(pts.length)]!; e[w] = `${p[0] + rnd(101) - 50},${p[1] + rnd(101) - 50}`; }
  e.RELOCK = rules[rnd(rules.length)]!; e.ENF = enfs[rnd(enfs.length)]!;
  if (k % 7 === 0) e.LOG = '1'; if (k % 11 === 0) e.STANCE = 'return'; if (k % 13 === 0) e.DRAWAT = String(2000 + rnd(200)); if (k % 17 === 0) e.QUIET = '1';
  if (k % 19 === 0) e.LOCK = 'scout:security-2,fixer:security-2,tech:security-2';
  if (k % 23 === 0) e.WRITE = `/tmp/player-proof-${k}-${process.env.PROOF_MODE ?? 'x'}.json`;
  envs.push(e);
}
const wf = envs.filter(e => e.WRITE).map(e => e.WRITE!); const rd = async () => Promise.all(wf.map(async f => (await Bun.file(f).exists()) ? await Bun.file(f).text() : null));
const t0 = Date.now(); const a = await playMany(dll, cwd, base, envs, { mode: 'process', par: Number(arg('--par', '16')) }); const t1 = Date.now();
const wa = await rd(); const b = await playMany(dll, cwd, base, envs, { mode: 'batch' }); const t2 = Date.now();
const wb = await rd(); let diff = 0; for (let k = 0; k < wf.length; k++) if (wa[k] !== wb[k] || wa[k] === null) { diff++; console.log(`DIFF written script ${wf[k]}`); }
let diff2 = 0; for (let k = 0; k < n; k++) if (a[k] !== b[k]) { diff++; if (diff <= 3) console.log(`DIFF #${k} ${JSON.stringify(envs[k])}\n process: ${JSON.stringify(a[k])}\n batch:   ${JSON.stringify(b[k])}`); }
const kinds = new Map<string, number>(); for (const s of a) { const w = s.split(' ')[0]!; kinds.set(w, (kinds.get(w) ?? 0) + 1); }
console.log(`${n} variants: ${n - diff}/${n} stdout byte-identical, ${wf.length} written scripts compared; outcomes ${[...kinds].map(([k, v]) => `${k} ${v}`).join(', ')}; process ${(t1 - t0) / 1000}s, batch ${(t2 - t1) / 1000}s`);
process.exit(diff ? 1 : 0);
