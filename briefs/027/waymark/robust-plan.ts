// scratch (Waymark, brief 027, engine 0.19.0): robustness of a loud plan by composing it with one wait moved 0.5 m and 1 m in 8 directions.
// Each variant is composed (the composer re-times every conditional step) and played on the tree engine. Prints n clean of 48 per group.
import { compose, loadPlan } from '../../../tools/missions/check/compose';
import { engineAt, play } from '../../../tools/missions/check/engine';
const id = process.env.PLAN ?? 'loud';
const plan = loadPlan(process.env.PLANFILE ?? `/Users/sky/Projects/suborned/contracts/missions/handover/routes/plans/${id}.plan.json`), e = engineAt('tree', 'tree');
const same = (a: unknown, b: number[]) => Array.isArray(a) && a[0] === b[0] && a[1] === b[1];
// a wait = every move step of that op to that point (the Scout's and the Fixer's car waits are also where they walk back to)
const waits: Record<string, [string, number[]]> = JSON.parse(process.env.WAITS ?? JSON.stringify({
  S: ['scout', [975, -4625]], F: ['fixer', [1175, -4825]], T: ['tech', [725, -4625]],
  Sflank: ['scout', [4300, -3100]], Fflank: ['fixer', [4400, -3350]], Tpier: ['tech', [675, -4625]],
}));
const dirs = [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]];
const jobs: { key: string; group: string; plan: typeof plan }[] = [];
for (const [w, [op, pt]] of Object.entries(waits)) for (const st of [50, 100]) for (const [dx, dy] of dirs) {
  const p = structuredClone(plan);
  for (const s of p.steps) if (s.op === op && s.do === 'move' && same(s.to, pt)) s.to = [pt[0]! + dx! * st, pt[1]! + dy! * st];
  jobs.push({ key: `${w} ${dx! * st},${dy! * st}`, group: w.replace(/flank|pier/, '2'), plan: p });
}
const done = new Map<string, string>(); if (process.env.RESUME) for (const l of (await Bun.file(process.env.RESUME).text()).trim().split('\n')) { const m = l.match(/^\d+\/\d+ (.*)$/); if (m) done.set(m[1]!.split(' | ').at(-1)!, m[1]!); }
if (process.env.BASEONLY) jobs.splice(0, jobs.length, { key: 'base', group: 'base', plan });
const res: string[] = new Array(jobs.length); let next = 0;
const ops = ['scout', 'fixer', 'tech', 'enforcer'];
await Promise.all(Array.from({ length: Number(process.env.PAR ?? 6) }, async () => {
  while (next < jobs.length) {
    const k = next++, j = jobs[k]!;
    if (done.has(j.key)) { res[k] = done.get(j.key)!; continue; }
    try {
      const { script } = await compose(j.plan, e, `rob-${id}-${k}`);
      const r = await play(script, `rob-${id}-${k}-final`, e);
      const hits = r.log.filter(l => l.code === 'shot.hit' && new RegExp(`fires on (${ops.join('|')}) `).test(l.text)).length;
      const down = ops.filter(o => r.log.some(l => l.code === `${o}.down`)), left = r.log.filter(l => l.code === 'captured').map(l => l.text.split(' ')[1]);
      res[k] = `${r.outcome} ${r.fail} ${Math.floor(r.tick / 600)}:${String(((r.tick % 600) / 10).toFixed(1)).padStart(4, '0')} T${r.trail} A${r.alert} hits ${hits}/18 (${Math.round(hits * 100 / 18)}%) down[${down}] left[${left}] | ${j.group} | ${j.key}`;
    } catch (err) { res[k] = `NOT COMPOSED ${(err as Error).message.slice(0, 120)} | ${j.group} | ${j.key}`; }
    console.error(`${k + 1}/${jobs.length} ${res[k]}`);
  }
}));
const clean = (l: string) => l.startsWith('success') && l.includes('down[]') && l.includes('left[]');
for (const g of [...new Set(jobs.map(j => j.group))]) { const rs = res.filter(l => l.includes(`| ${g} |`)); console.log(`${g}: ${rs.filter(clean).length}/${rs.length} clean`); }
const groups = { first: ['S', 'F', 'T'], second: ['S2', 'F2', 'T2'] };
for (const [n, gs] of Object.entries(groups)) { const rs = res.filter(l => gs.some(g => l.includes(`| ${g} |`))); if (rs.length) console.log(`${n} waits: ${rs.filter(clean).length}/${rs.length} clean`); }
await Bun.write(process.env.OUT ?? `/Users/sky/Projects/suborned/out/waymark027/w19/robplan-${id}.txt`, res.join('\n') + '\n');
