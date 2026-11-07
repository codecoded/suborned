// Brief 100 (Fable, 2026-11-07; Sky, playing: "It would be good to explain why an alert raised, or why police came"): the `why` block of
// text.json. The cause of every Alert rise and every police call, and the reporter while a report is still on its way, in the player's
// words. The host reads the engine's alert history (Game.Ledger's alert rows today; Keel's structured read to come: cause, who, what,
// where, when) and words it from here, never from the engine's sentence. Host content, read only: nothing here changes a run.
// The words, their fills and who builds what: briefs/100/fable/words.md.
import { z } from 'zod';

/** The Alert causes, by the engine's ledger cause (whyKeyOf). Every one has words; a new cause in the engine needs a key here. */
export const ALERT_CAUSES = ['seen', 'carry', 'dead', 'down', 'woken', 'noise', 'mark', 'missing', 'cut', 'approach', 'staff', 'let-go', 'confirmed',
  'gunfire', 'violence', 'drawn', 'drawn-people', 'warned', 'fought', 'loiter', 'recorded', 'car', 'entry', 'sighting', 'start'] as const;
/** What brought the police (or the robots), by the engine's call (words.md lists the log line each comes from). */
export const POLICE_CAUSES = ['heard', 'witness', 'shot-at', 'patrol', 'rivals', 'gang', 'drawn', 'attack', 'post', 'report', 'body', 'robots', 'more',
  'cordon', 'loud', 'split', 'seal'] as const;
/** How a report travels while it is still to land (the engine's reports in transit). */
export const WAYS = ['radio', 'walk', 'run', 'look', 'camera', 'screen', 'robot', 'checkin', 'tell'] as const;
/** The fills a why text may carry; `rule` in text.json says what fills each. */
export const WHY_FILLS = ['who', 'about', 'agent', 'where', 'to', 'alert', 'clock', 's', 'cause', 'short', 'title', 'job'] as const;
type Fill = (typeof WHY_FILLS)[number];
/** The widest each fill is in play (the reporting cameras are the entrance's and the pickup's; a person by job, "a kiosk attendant"), so every
 *  text is held to its limit as the player reads it. A cause whose {about} is narrower or wider says so in WIDEST_BY. {cause} is the widest short. */
const WIDEST: Record<Exclude<Fill, 'cause' | 'short' | 'title'>, string> = { who: 'the entrance camera', about: 'a kiosk attendant', agent: 'the Enforcer',
  where: 'by the control cabinet', to: 'the official door', alert: '3', clock: '10:30', s: '99', job: 'shopkeeper' };
const WIDEST_BY: Record<string, Partial<Record<Fill, string>>> = { recorded: { about: 'the driver' }, cut: { about: 'the entrance camera' } }; // a Pay or Pressure is on the cast or a rival; a cut is a camera's

const str = z.string().min(1).regex(/^[^\n<>!]+$/, 'one line, no markup, no exclamation mark');
const key = z.string().regex(/^[a-z][a-z0-9-]*$/);
const Words = z.strictObject({ title: str.optional(), short: str.optional(), line: str.optional() });
const Cause = z.strictObject({ title: str, short: str, line: str, variants: z.record(key, Words).optional(), pick: z.string().min(1).optional() })
  .refine((c) => (c.variants === undefined) === (c.pick === undefined), 'variants go with a `pick` that says how the host chooses');
const Way = z.strictObject({ plate: str, line: str });

export const Why = z.strictObject({
  rule: z.string().min(1),
  by: z.strictObject({ camera: str, robot: str, police: str, desk: str, round: str, witness: str, another: str.includes('{job}') })
    .describe('a reporter or witness who is no person (names.targets words people): a city camera without its own name, a police robot, an officer, the desk, the bin round, an unnamed witness; `another` when they share a job with {about}'),
  places: z.record(z.string().regex(/^(d:[A-Z][^:]*|[a-z][a-z-]*)$/), str).describe('{where}: the site\'s named places (sim site places: the mission\'s d: destinations, cabinet, service-bay, service-drive, security-entry) with their preposition; `nearby` when none is within 13 m'),
  alert: z.record(z.enum(ALERT_CAUSES), Cause),
  again: z.strictObject({ short: str, line: str }).describe('added when the Alert came from the count of incidents (alert.byIncident), not the cause itself'),
  effect: z.record(z.enum(['1', '2', '3']), str).describe('what each Alert sets off (GDD §05\'s table, the Handover\'s dial)'),
  police: z.record(z.enum(POLICE_CAUSES), Cause),
  onWay: z.strictObject({ rule: z.string().min(1), to: z.record(z.string().regex(/^(door:[a-z-]+|call-box|phone)$/), str), ways: z.record(z.enum(WAYS), Way), fix: str, nofix: str, stopped: Way }),
  meter: z.strictObject({ rule: z.string().min(1), head: str, when: str, police: str, what: str, empty: str, start: str }),
}).superRefine((w, ctx) => { for (const p of whyProblems(w)) ctx.addIssue({ code: 'custom', message: p }); });
export type Why = z.infer<typeof Why>;

// ---------------------------------------------------------------- the limits (GDD §14 Fable: titles ≤ 4 words, play lines ≤ 6, a line ≤ 80 characters)

const wordsOf = (s: string) => s.replace(/\{[a-z]+\}/g, 'x').split(' · ')[0]!.split(/\s+/).filter(Boolean).length;
const filled = (s: string, by: Partial<Record<Fill, string>> = {}) => s.replace(/\{([a-z]+)\}/g, (m, k: string) => (by as Record<string, string>)[k] ?? (WIDEST as Record<string, string>)[k] ?? m);
type Kind = 'title' | 'short' | 'line' | 'plate' | 'word' | 'frag';
const LIMITS: Record<Kind, { chars: number; words: number; shape?: RegExp }> = {
  title: { chars: 32, words: 4, shape: /^[A-Z{][^.]*$/ }, // a capital, no full stop
  short: { chars: 56, words: 6, shape: /^([a-z{]|Inez|Popovic)[^.]*$/ }, // lower case (or a name), no full stop: the crucial card's dim line
  line: { chars: 80, words: 16, shape: /^[A-Z{].*\.$/ }, // one sentence
  plate: { chars: 36, words: 6, shape: /^[A-Z][^.]*$/ }, // the marked reporter's plate, before its " · {s} s"
  word: { chars: 28, words: 4 }, frag: { chars: 80, words: 16 },
};
/** Every text of the block held to its kind's limits at its widest fills; the fills known; a short as written fits the crucial card (≤ 40). */
export function whyProblems(w: Why): string[] {
  const bad: string[] = [];
  const shorts: string[] = [];
  const check = (path: string, s: string, kind: Kind, by: Partial<Record<Fill, string>> = {}) => {
    for (const [, f] of s.matchAll(/\{([a-z]+)\}/g)) if (!(WHY_FILLS as readonly string[]).includes(f!)) bad.push(`${path}: unknown fill {${f}}`);
    const L = LIMITS[kind], wide = filled(s, by);
    if (wide.length > L.chars) bad.push(`${path}: ${wide.length} characters at its widest (> ${L.chars}): ${wide}`);
    if (wordsOf(s) > L.words) bad.push(`${path}: ${wordsOf(s)} words (> ${L.words})`);
    if (L.shape && !L.shape.test(s)) bad.push(`${path}: not a ${kind}'s shape: ${s}`);
    if (kind === 'short' && s.length > 40) bad.push(`${path}: ${s.length} characters as written (> 40, the crucial card's line)`);
  };
  const causes = (group: 'alert' | 'police') => Object.entries(w[group]) as [string, z.infer<typeof Cause>][];
  for (const group of ['alert', 'police'] as const) for (const [k, c] of causes(group)) {
    const by = WIDEST_BY[k] ?? {};
    for (const [v, x] of [['', c], ...Object.entries(c.variants ?? {})] as [string, z.infer<typeof Words>][]) {
      const at = `${group}.${k}${v ? '.variants.' + v : ''}`;
      if (x.title) check(`${at}.title`, x.title, 'title', by); if (x.line) check(`${at}.line`, x.line, 'line', by);
      if (x.short) { check(`${at}.short`, x.short, 'short', by); if (group === 'alert') shorts.push(filled(x.short, by)); }
    }
  }
  const cause = shorts.reduce((a, b) => (b.length > a.length ? b : a), ''); // {cause}: the widest alert short, capitalised where a sentence starts with it
  const withCause = { cause: cause.charAt(0).toUpperCase() + cause.slice(1), short: cause, title: 'Police closing in' };
  for (const [k, c] of causes('police')) for (const f of ['short', 'line'] as const) if (c[f].includes('{cause}')) check(`police.${k}.${f}`, c[f], f, withCause);
  check('again.short', w.again.short, 'short'); check('again.line', w.again.line, 'line');
  for (const [k, s] of Object.entries(w.effect)) check(`effect.${k}`, s, 'line');
  for (const [k, s] of [...Object.entries(w.by), ...Object.entries(w.places), ...Object.entries(w.onWay.to)]) check(`word ${k}`, s, 'word');
  for (const [k, x] of [...Object.entries(w.onWay.ways), ['stopped', w.onWay.stopped] as const]) { check(`onWay.${k}.plate`, x.plate, 'plate'); check(`onWay.${k}.line`, x.line, 'line'); }
  check('onWay.fix', w.onWay.fix, 'plate'); check('onWay.nofix', w.onWay.nofix, 'plate');
  for (const k of ['head', 'empty', 'start'] as const) check(`meter.${k}`, w.meter[k], 'plate');
  for (const k of ['when', 'police', 'what'] as const) check(`meter.${k}`, w.meter[k], 'frag', withCause);
  return bad;
}

// ---------------------------------------------------------------- the reference reads (Latch ports these)

/** The words for an engine ledger cause (Game.Ledger, what "alert"): its `alert` key, or null for a cause with no words (a new engine cause:
 *  add its key). report:<kind>:<who> by its kind; report:cut:camera:<id> is cut; report:<role> (two parts) an approach reported, except the
 *  Office staff's and Popovic's reports of a cancelled Enter and Take. */
export function whyKeyOf(cause: string): (typeof ALERT_CAUSES)[number] | null {
  const p = cause.split(':'), head = p[0]!;
  if (head === 'report') {
    if (p.length === 2) return p[1] === 'office-staff' ? 'staff' : p[1] === 'councillor' ? 'let-go' : 'approach';
    return (ALERT_CAUSES as readonly string[]).includes(p[1]!) ? (p[1] as (typeof ALERT_CAUSES)[number]) : null;
  }
  if (head === 'drawn-seen') return p[1] === 'people' ? 'drawn-people' : 'drawn';
  const map: Record<string, (typeof ALERT_CAUSES)[number]> = { confirmed: 'confirmed', gunfire: 'gunfire', 'violence-seen': 'violence', 'drawn-warned': 'warned',
    'takedown-fails': 'fought', loiter: 'loiter', recorded: 'recorded', 'car-reported': 'car', entry: 'entry', sighting: 'sighting' };
  return map[head] ?? null;
}
/** A cause's words, a variant's over its base. */
export function whyWords(w: Why, group: 'alert' | 'police', k: string, variant?: string): { title: string; short: string; line: string } | undefined {
  const c = (w[group] as Record<string, z.infer<typeof Cause>>)[k]; if (!c) return undefined;
  const v = variant ? c.variants?.[variant] : undefined;
  return { title: v?.title ?? c.title, short: v?.short ?? c.short, line: v?.line ?? c.line };
}
/** Fill a why text; `cap` capitalises the first letter (a title or a line; a short stays lower case). */
export function fillWhy(s: string, v: Partial<Record<Fill, string | number>>, cap = true): string {
  const out = s.replace(/\{([a-z]+)\}/g, (m, k: string) => ((v as Record<string, string | number | undefined>)[k] === undefined ? m : String((v as Record<string, string | number>)[k])));
  return cap ? out.charAt(0).toUpperCase() + out.slice(1) : out;
}
