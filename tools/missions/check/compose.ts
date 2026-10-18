// Route plans → route scripts (brief 011 B; Aster: "build so a re-run is cheap"). A route plan names the orders a player gives, in
// order, and when each may be given at the earliest; the composer finds, by playing the engine, the first tick each order is taken
// (and, for `clear`, the first tick it is taken without the run failing soon after), and writes the engine's own order script. So a
// world that moves (a planter becomes a solid, a walk lengthens, a rule re-times) is one `bun run missions:compose` away from new
// ticks, and the plan, not the ticks, is what is reviewed.
//
// A plan: { "schema": "suborned.mission.plan/1", "mission", "note", "start", "until", "steps": [Step] }. A step is an order in the
// engine's format without its tick, plus:
//   label   a name later steps can wait for;
//   after   the labels (or ops) this step waits for: it is given no earlier than each of them (default: the previous step of its own op);
//   gap     seconds to add after what it waits for (default 0);
//   at      m:ss: no earlier than this mission time;
//   clear   seconds the run must go on without failing after the order is given (an entry the patrol would see is "taken", then fails);
//   by      m:ss: the latest the step may be given; past it the plan fails to compose, naming the step;
//   when    an engine log code that must have been logged first (fight.over: the last hostile is down; blocker.out, brief 018: every
//           hostile the engine sent for the road is down, broken or gone);
//   unless  an engine log code that must not be logged by the order or `clear` s after it (captured: nobody left behind at the extract).
// The search: from the earliest tick, every 5 s until taken, then back tick by tick to the first tick taken. Deterministic: the same
// plan on the same engine gives the same script.
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { z } from 'zod';
import { playAll, type EngineAt, type Order, type Script } from './engine';

const at = z.string().regex(/^\d{1,2}:[0-5]\d(\.\d)?$/);
export const Step = z.strictObject({
  op: z.enum(['scout', 'fixer', 'tech', 'enforcer', 'cell']).optional(), do: z.string().min(1),
  to: z.union([z.string().min(1), z.tuple([z.number().int(), z.number().int()])]).optional(), target: z.string().min(1).optional(), approach: z.string().min(1).optional(),
  run: z.boolean().optional(), force: z.boolean().optional(), stance: z.enum(['hold', 'return', 'free']).optional(), how: z.enum(['knockout', 'kill']).optional(),
  label: z.string().min(1).optional(), after: z.array(z.string().min(1)).optional(), gap: z.number().nonnegative().optional(),
  at: at.optional(), clear: z.number().positive().optional(), by: at.optional(), when: z.string().min(1).optional(), unless: z.string().min(1).optional(), why: z.string().min(1).optional(),
});
export const Plan = z.strictObject({
  schema: z.literal('suborned.mission.plan/1'), mission: z.string().min(1), note: z.string().min(1),
  start: z.record(z.string(), z.unknown()).default({}), until: z.number().int().positive(), steps: z.array(Step).min(1),
});
export type Step = z.infer<typeof Step>;
export type Plan = z.infer<typeof Plan>;

const logged = (log: { tick: number; code: string }[], code: string, t: number) => log.some(l => l.code === code && l.tick <= t);
const ticks = (s: string) => { const [m, r] = s.split(':'), [sec, d] = r!.split('.'); return (Number(m) * 60 + Number(sec)) * 10 + Number(d ?? 0); };
const orderOf = (s: Step, tick: number): Order => {
  const o: Record<string, unknown> = { tick };
  for (const k of ['op', 'do', 'to', 'target', 'approach', 'run', 'stance', 'force', 'how'] as const) if (s[k] !== undefined) o[k] = s[k];
  return o as Order;
};

/** Compose a plan into a script on engine `e`. Throws naming the step that can't be given by its `by` (or by `until`). */
export async function compose(plan: Plan, e: EngineAt, name: string, log: (m: string) => void = () => {}): Promise<{ script: Script; placed: { step: number; tick: number; why?: string }[] }> {
  const placed: Order[] = [], marks = new Map<string, number>(), lastOf = new Map<string, number>(), rows: { step: number; tick: number; why?: string }[] = [];
  const scriptWith = (o: Order | undefined, until: number): Script => ({ schema: 'suborned.sim.script/1', mission: plan.mission as 'handover', start: plan.start, orders: [...placed, ...(o ? [o] : [])].sort((a, b) => a.tick - b.tick), until });
  for (const [i, s] of plan.steps.entries()) {
    const deps = s.after ?? (s.op ? [s.op] : []);
    let lo = s.at ? ticks(s.at) : 0;
    for (const d of deps) { const t = marks.get(d) ?? lastOf.get(d); if (t === undefined && !s.after) continue; if (t === undefined) throw new Error(`${name} step ${i + 1} (${s.op} ${s.do}): waits for ${d}, which no earlier step is`); lo = Math.max(lo, t); }
    lo += Math.round((s.gap ?? 0) * 10);
    const hi = s.by ? ticks(s.by) : plan.until, clearT = Math.round((s.clear ?? 0) * 10);
    /** Is the order taken at tick t (and, with `clear`, the run not failed `clear` s later)? The last match is this step's: a repeated order (Help twice) placed at a tick an earlier one holds sorts after it. */
    const tryAt = async (ts: number[]) => (await playAll(ts.map(t => ({ name: `${name}-s${i + 1}-${t}`, script: scriptWith(orderOf(s, t), t + Math.max(1, clearT)) })), e, 12)).map((p, k) => {
      const t = ts[k]!, mine = p.orders.findLast(x => x.order.tick === t && x.order.op === s.op && x.order.do === s.do && (x.order.target ?? '') === (s.target ?? ''));
      return !!mine?.ok && !(clearT && p.outcome === 'failure') && (!s.when || logged(p.log, s.when, t)) && (!s.unless || !p.log.some(l => l.code === s.unless));
    });
    let found = -1;
    for (let t0 = lo; t0 <= hi && found < 0; t0 += 50 * 12) { // coarse: every 5 s, twelve at a time
      const ts = Array.from({ length: 12 }, (_, k) => t0 + k * 50).filter(t => t <= hi), ok = await tryAt(ts);
      const k = ok.indexOf(true); if (k >= 0) found = ts[k]!;
    }
    if (found < 0) throw new Error(`${name} step ${i + 1} (${s.op ?? ''} ${s.do}${s.target ? ` ${s.target}` : ''}): not taken${clearT ? ` clear for ${s.clear} s` : ''} between ${lo} and ${hi}`);
    for (let a = Math.max(lo, found - 49); a < found; a += 12) { // fine: back to the first tick taken
      const ts = Array.from({ length: 12 }, (_, k) => a + k).filter(t => t < found), ok = await tryAt(ts);
      const k = ok.indexOf(true); if (k >= 0) { found = ts[k]!; break; }
    }
    const o = orderOf(s, found);
    placed.push(o); rows.push({ step: i + 1, tick: found, ...(s.why ? { why: s.why } : {}) });
    if (s.op) lastOf.set(s.op, found);
    if (s.label) marks.set(s.label, found);
    log(`${name} ${i + 1}/${plan.steps.length} ${s.op ?? ''} ${s.do}${s.target ? ` ${s.target}` : ''}${typeof s.to === 'string' ? ` ${s.to}` : ''} at tick ${found}`);
  }
  return { script: scriptWith(undefined, plan.until), placed: rows };
}

/** Write a composed script in the routes' layout: one order per line, the plan's note. */
export function writeScript(file: string, note: string, s: Script) {
  const body = ['{', ' "schema": "suborned.sim.script/1",', ` "mission": "${s.mission}",`, ` "note": ${JSON.stringify(note)},`, ` "start": ${JSON.stringify(s.start)},`, ' "orders": [',
    ...s.orders.map((o, i) => `  ${JSON.stringify(o)}${i < s.orders.length - 1 ? ',' : ''}`), ' ],', ` "until": ${s.until}`, '}'];
  writeFileSync(file, body.join('\n') + '\n');
}
export const loadPlan = (file: string) => Plan.parse(JSON.parse(readFileSync(file, 'utf8')));
export { join };
