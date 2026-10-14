// The review page (brief 010 C): out/review/010-cast.html, made from a people file (`bun run people --review`), so a live run
// produces it as the stand-in does. Self-contained html. It shows the cast by occupation and temperament, five sample people with
// their day (scales as their step words, "can introduce", "knows the needs of"), who the Handover casts, and the cast
// requirements of Motive §3, read from the mission's roles and the rules (no person is named here).
import type { Cast } from '../../contracts/people/cast.schema';
import type { People, Person } from '../../contracts/people/people.schema';
import type { Check } from '../plan/checks';
import type { Ctx } from './expand';
import { hasRegularTie, REGULAR_SHARE_MIN } from '../missions/handover/cast';
import { castForNames } from './roles';
import type { Rules } from './rules';

const esc = (x: unknown) => String(x).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);
const hm = (m: number) => `${String(Math.floor(m / 60) % 24).padStart(2, '0')}:${String(m % 60).padStart(2, '0')}`;
export type Req = { id: string; need: string; ok: boolean; detail: string };

/** The step word of a scale value (rules scales[x].steps). */
export const stepWord = (rules: Rules, scale: string, v: number) => (rules.scales as Record<string, { steps?: readonly string[] }>)[scale]?.steps?.[v] ?? String(v);
/** The tie kinds that can introduce (rules ties.kinds[k].introduces). */
export const introducing = (rules: Rules) => Object.entries(rules.ties.kinds).filter(([, t]) => t.introduces).map(([k]) => k);

/** Motive §3 as checks on a cast and its people. Each reads the mission's roles and the rules; none names a person. */
export function requirements(c: Ctx, people: People): Req[] {
  const { rules, cast, scope } = c, roles = c.casting?.mission.roles ?? [], P = people.persons, out: Req[] = [];
  const rowOfRole = (employer: string) => { const u = scope.units.find(x => x.employer === employer); return u ? cast.rows.find(r => r.area === u.area && r.occupation === u.trade) : undefined; };
  for (const r of roles) {
    const pos = r.position ?? r.role!, row = rowOfRole(r.employer), mine = P.filter(p => p.work?.employer === r.employer && (p.work.position ?? p.work.role) === pos);
    if (r.need) {
      const w = (row?.needs ?? []).filter(n => n.need === r.need && (!n.positions || n.positions.includes(pos))).reduce((n, x) => n + x.weight, 0);
      const all = (row?.needs ?? []).filter(n => !n.positions || n.positions.includes(pos)).reduce((n, x) => n + x.weight, 0);
      const have = mine.filter(p => p.need === r.need).length;
      out.push({ id: `${r.id}-weights-${r.need}`, need: `the ${pos} row of ${r.employer} weights ${r.need} (the ${r.id} needs it)`, ok: w > 0 && have > 0, detail: `weight ${w}/${all} (${all ? Math.round((100 * w) / all) : 0}%); ${have} of ${mine.length} carry it` });
    }
    if (r.regularOf) {
      const staff = new Set(P.filter(p => p.work?.employer === r.regularOf).map(p => p.id));
      const variants = (row?.routines ?? []).filter(v => v.skeleton === 'worker' && (!v.positions || v.positions.includes(pos)));
      const pre = variants.length > 0 && variants.every(v => v.preShift === 'cafe'), ties = mine.filter(p => hasRegularTie(p, staff)).length;
      const unserved = mine.filter(p => p.routine.stops.some(s => s.doing === 'pre-shift') && !p.routine.stops.some(s => s.doing === 'pre-shift' && P.some(q => staff.has(q.id) && q.work && q.routine.stops.some(x => x.doing === 'work' && x.place === q.work!.place && x.from < s.to && x.to > s.from)))).length;
      const share = mine.length ? ties / mine.length : 0;
      out.push({ id: `${r.id}-regular-of-${r.regularOf}`, need: `the ${pos} row carries preShift cafe, and at least ${Math.round(REGULAR_SHARE_MIN * 100)}% of ${pos}s hold a regular tie into ${r.regularOf}`, ok: pre && share >= REGULAR_SHARE_MIN, detail: `${pre ? 'every variant has' : 'not every variant has'} preShift cafe; ${ties} of ${mine.length} hold a regular tie into ${r.regularOf} (${Math.round(share * 100)}%)${unserved ? `; ${unserved} pre-shift stops fall when no ${r.regularOf} staff is on shift, so no tie can aim there` : ''}` });
    }
  }
  const keepers = new Set(roles.map(r => r.employer));
  for (const k of people.knowledge?.needsKnown ?? []) {
    const p = P.find(x => x.id === k.keeper)!;
    if (keepers.has(p.work!.employer)) out.push({ id: `keeper-${p.work!.unit}`, need: `the keeper of ${p.work!.unit} knows the needs of workmates and regulars`, ok: k.knows.length > 0, detail: `${k.knows.length} needs known` });
  }
  const mix = rules.temperamentsBy.mix;
  for (const r of cast.rows) {
    const tot = r.temperaments.reduce((n, t) => n + t.weight, 0), top = Math.max(...r.temperaments.map(t => t.weight)) / tot;
    // the cap is the row's weights (people.rules.json temperamentsBy.mix: "the culture baseline is the cast row"); the people it makes are reported beside it, not held to it:
    // on a unit of a few people a share cap would ask for more kinds than the rules' own unit rule (two kinds from three staff), so the people's rule is P-10's unit minimum
    const ps = P.filter(p => (p.work ? `${p.work.area}/${p.work.trade}` : `${scope.homes.find(h => h.plot === p.home.plot)?.area ?? '?'}/resident`) === `${r.area}/${r.occupation}`);
    const by = new Map<string, number>(); for (const p of ps) by.set(p.temperament, (by.get(p.temperament) ?? 0) + 1);
    const [big, n] = [...by].sort((a, b) => b[1] - a[1] || (a[0] < b[0] ? -1 : 1))[0] ?? ['none', 0];
    out.push({ id: `mix-${r.area}-${r.occupation}`, need: `${r.area}/${r.occupation}: no temperament over ${Math.round(mix.rowMaxShare * 100)}% of the row's weights`, ok: top <= mix.rowMaxShare + 1e-9, detail: `largest ${Math.round(top * 100)}% of the weights; of the ${ps.length} people ${ps.length ? Math.round((100 * n) / ps.length) : 0}% ${big} (counted, not held: the cap is on the row's weights)` });
  }
  return out;
}

const who = (p: Person | undefined, by: Map<string, Person>) => p ? `${esc(p.name)} <small>(${esc(p.work ? (p.work.position ?? p.work.role) : 'resident')}, ${esc(p.id)})</small>` : '?';

export function reviewHtml(c: Ctx, people: People, checks: Check[], label: string): string {
  const { rules, scope } = c, P = people.persons, by = new Map(P.map(p => [p.id, p])), intro = new Set(introducing(rules));
  const rowKey = (p: Person) => (p.work ? `${p.work.area}/${p.work.trade}` : `${scope.homes.find(h => h.plot === p.home.plot)?.area ?? '?'}/resident`);
  const rows = [...new Set(P.map(rowKey))].sort(), tems = Object.keys(rules.temperaments), mixRules = rules.temperamentsBy.mix;
  const mix = rows.map(r => { const ps = P.filter(p => rowKey(p) === r); return `<tr><th>${esc(r)}</th><td>${ps.length}</td>${tems.map(t => { const n = ps.filter(p => p.temperament === t).length; return `<td>${n}${n ? ` <small>${Math.round((100 * n) / ps.length)}%</small>` : ''}</td>`; }).join('')}</tr>`; }).join('');
  const handover = c.casting ? castForNames(P, c.casting, rules) : [];
  const roleOf = new Map(handover.filter(h => h.pinned).map(h => [h.pinned!, h.role]));
  const keeper = P.find(p => p.work?.position === 'keeper' && c.casting?.mission.roles.some(r => r.employer === p.work!.employer));
  const sample: Person[] = [...new Set([...handover.flatMap(h => (h.pinned ? [h.pinned] : [])), ...(keeper ? [keeper.id] : []), ...P.map(p => p.id)])].slice(0, 5).map(id => by.get(id)!);
  const know = new Map((people.knowledge?.needsKnown ?? []).map(k => [k.keeper, k.knows]));
  const card = (p: Person) => {
    const sc = (['bravery', 'resentment', 'fear', 'loyalty'] as const).map(s => `${s} <b>${esc(stepWord(rules, s, p.start[s]))}</b>`).join(' · ');
    const can = p.ties.filter(t => intro.has(t.kind)).map(t => `${who(by.get(t.with), by)} <small>${esc(t.kind)}</small>`);
    const knows = (know.get(p.id) ?? []).map(id => `${who(by.get(id), by)}: ${esc(by.get(id)!.need)}`);
    return `<section class="p"><h3>${esc(p.name)} <small>${esc(p.id)}${roleOf.has(p.id) ? ` · the Handover's ${esc(roleOf.get(p.id))}` : ''}</small></h3>
<p>${esc(p.work ? `${p.work.position ?? p.work.role} at ${p.work.unit}, shift ${hm(p.work.shift.from)}-${hm(p.work.shift.to)}` : `resident of ${p.home.plot}`)} · ${esc(rules.temperaments[p.temperament as keyof Rules['temperaments']]!.name)} · need <b>${esc(p.need)}</b></p>
<p>${sc}</p>
<p><b>Can introduce:</b> ${can.length ? can.join('; ') : 'nobody'}</p>
<p><b>Knows the needs of:</b> ${knows.length ? knows.join('; ') : '<i>nobody (not a keeper)</i>'}</p>
<p><b>Day:</b> ${p.routine.stops.map(s => `${hm(s.from)}-${hm(s.to)} ${esc(s.doing)} at ${esc(s.place)}`).join(' · ')}</p></section>`;
  };
  const reqs = requirements(c, people);
  return `<!doctype html><html lang="en"><meta charset="utf-8"><title>010 · the cast</title><style>
body{font:15px/1.45 system-ui,sans-serif;max-width:1000px;margin:2em auto;padding:0 1em;color:#222}table{border-collapse:collapse;margin:.5em 0}td,th{border:1px solid #ccc;padding:.25em .6em;text-align:left}small{color:#666}.p{border:1px solid #ddd;border-radius:6px;padding:.2em 1em;margin:1em 0}.ok{color:#176b3a}.no{color:#b3261e;font-weight:600}</style>
<h1>010 · the cast: ${esc(scope.id)}</h1><p>${esc(label)} · ${P.length} simulated people · plan ${esc(people.plan.id)} ${esc(people.plan.version)} · cast ${esc(people.inputs.cast.slice(0, 12))}</p>
<h2>Checker</h2><table>${checks.map(k => `<tr><td class="${k.ok ? 'ok' : 'no'}">${k.ok ? 'ok' : 'FAIL'}</td><td>${esc(k.id)}</td><td>${esc(k.rule)}</td><td>${esc(k.value)}</td></tr>`).join('')}</table>
<h2>Cast requirements (Motive §3)</h2><table>${reqs.map(r => `<tr><td class="${r.ok ? 'ok' : 'no'}">${r.ok ? 'met' : 'NOT MET'}</td><td>${esc(r.need)}</td><td>${esc(r.detail)}</td></tr>`).join('')}</table>
<h2>The cast by occupation and temperament</h2><p><small>The cap (no temperament over ${Math.round(mixRules.rowMaxShare * 100)}%) is the row's weights, the culture baseline (people.rules.json temperamentsBy.mix); the people are counted here, and a unit of ${mixRules.whenStaffAtLeast} or more must carry ${mixRules.unitMinDistinct} kinds (P-10).</small></p><table><tr><th>row</th><th>people</th>${tems.map(t => `<th>${esc(t)}</th>`).join('')}</tr>${mix}</table>
<h2>Who the Handover casts</h2><table>${handover.map(h => `<tr><td>${esc(h.role)}</td><td class="${h.ok ? 'ok' : 'no'}">${h.ok ? who(by.get(h.pinned!), by) : 'NOT CAST'}</td><td>${esc(h.why)}</td></tr>`).join('')}</table>
<h2>Five people and their day</h2>${sample.map(card).join('')}
<h2>Knowledge at the start</h2><p>The cell is known by ${esc((people.knowledge?.identity ?? []).join('/'))} only; nobody knows anything else about it. Keepers know the needs of:</p><ul>${(people.knowledge?.needsKnown ?? []).map(k => `<li>${who(by.get(k.keeper), by)}: ${k.knows.length} people</li>`).join('')}</ul></html>
`;
}
