// scratch (batch since brief 042): for each clean win in a sweep file, move each agent's wait 50 cm and 1 m in 8 directions; rank by how many stay clean
import { playMany } from './batch';
const E = process.env.E ?? '/Users/sky/Projects/suborned/out/waymark027/e18', BASE = process.env.BASE ?? '/Users/sky/Projects/suborned/contracts/missions/handover/routes/loud.json';
const site = JSON.parse(await Bun.file('/Users/sky/Projects/suborned/sim/data/handover.site.json').text());
const at = (id: string): [number, number] => { const p = site.cover.points.find((x: any) => x.id === id); return p.at; };
const lines = (await Bun.file(process.env.IN!).text()).trim().split('\n').filter(l => l.startsWith('Success') && l.includes('down[]'));
const combos = [...new Set(lines.map(l => l.split(' | ')[1]!))].slice(0, Number(process.env.TOP ?? 80));
const extra = Object.fromEntries((process.env.EXTRA ?? '').split(' ').filter(Boolean).map(x => x.split('=') as [string, string]));
const dirs = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]], steps = (process.env.STEPS ?? '50,100').split(',').map(Number);
const jobs: { combo: string; env: Record<string, string> }[] = [];
for (const c of combos) {
  const m = c.match(/S=(\S+) F=(\S+) T=(\S+) (\S+)/)!, base = { S: at(m[1]!), F: at(m[2]!), T: at(m[3]!) };
  for (const who of ['S', 'F', 'T'] as const) for (const st of steps) for (const [dx, dy] of dirs) {
    const env: Record<string, string> = { RELOCK: m[4]!, ...extra };
    for (const k of ['S', 'F', 'T'] as const) { const [x, y] = base[k]; env[k] = k === who ? `${x + dx! * st},${y + dy! * st}` : `${x},${y}`; }
    jobs.push({ combo: c, env });
  }
}
console.error(`${combos.length} combos, ${jobs.length} plays`);
// brief 042: all plays in one player process on every core (batch.ts; an older player build falls back to a process a play)
const outs = await playMany(process.env.PLAYER ?? '../player/bin/player.dll', E, BASE, jobs.map(j => j.env));
const res = new Map<string, { ok: number; n: number; hp: number[] }>();
jobs.forEach((j, k) => {
  const r = outs[k]!.trim(), e = res.get(j.combo) ?? { ok: 0, n: 0, hp: [] };
  e.n++; if (r.startsWith('Success') && r.includes('down[]')) { e.ok++; e.hp.push(Number(r.match(/hp-lost (\d+)/)![1])); }
  res.set(j.combo, e);
});
const rows = [...res.entries()].sort((a, b) => b[1].ok - a[1].ok);
for (const [c, e] of rows.slice(0, 25)) console.log(`${String(e.ok).padStart(3)}/${e.n} clean, hp ${e.hp.length ? Math.min(...e.hp) + '-' + Math.max(...e.hp) : '-'} | ${c} | base: ${lines.find(l => l.endsWith(c))!.split(' | ')[0]}`);
