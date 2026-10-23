// Suture, brief 027, engine 0.20.0: the draw-timing sweep. Each of the Scout, Fixer and Tech joins the response-unit fight at one of six
// moments: with the doors (base), +0.5, +1, +2 s, at the first officer down (clean through the opening volley), or never (clean through the
// unit; boards with the rest). 6^3 = 216 plans into out/suture020/sweep-plans (scratch; the table is the evidence).
import { readFileSync, writeFileSync, mkdirSync, rmSync } from 'node:fs';
import { join } from 'node:path';
const DIR = import.meta.dir, OUT = join(DIR, '../../../../out/suture020/sweep-plans');
rmSync(OUT, { recursive: true, force: true }); mkdirSync(OUT, { recursive: true });
const base = JSON.parse(readFileSync(join(DIR, 'plans/base.plan.json'), 'utf8'));
const moments = ['0', '05', '1', '2', 'od', 'no'] as const;
const at = { '0': null, '05': '3:46.0', '1': '3:46.5', '2': '3:47.5' } as Record<string, string | null>;
const ix = { scout: [11, 12, 13, 20], fixer: [14, 15, 16, 21], tech: [17, 18, 19, 22] } as const;
let n = 0;
for (const s of moments) for (const f of moments) for (const t of moments) {
  const m = { scout: s, fixer: f, tech: t } as Record<string, string>;
  const steps: any[] = [];
  base.steps.forEach((st: any, i: number) => {
    const op = (Object.keys(ix) as (keyof typeof ix)[]).find(o => (ix[o] as readonly number[]).includes(i));
    if (!op) { steps.push(structuredClone(st)); return; }
    const k = m[op]!, c = structuredClone(st);
    if (k === 'no') return; // never joins the unit fight
    if (k === 'od') { // clean through the opening volley: draws when the first officer goes down, then locks the blocker
      if (i === ix[op][2]) return; // no lock on security-2
      if (i === ix[op][0]) { c.when = 'security.officer-down'; c.label = `${op}-lock1`; }
      if (i === ix[op][3]) { c.after = [`${op}-lock1`]; }
      steps.push(c); return;
    }
    if (i === ix[op][0] && at[k]) c.at = at[k];
    steps.push(c);
  });
  // the crew: the Scout's and the Fixer's flank steps stay theirs; an agent who never fought the unit still flanks (draws there)
  const id = `sw-${s}-${f}-${t}`;
  writeFileSync(join(OUT, `${id}.plan.json`), JSON.stringify({ ...base, note: `Suture 0.20 sweep ${id} (S F T join the unit fight at: ${s} ${f} ${t})`, steps }, null, 1) + '\n'); n++;
}
console.log(n, 'sweep plans in', OUT);
