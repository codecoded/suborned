// What the rules say of each approach on one person (the roster viewer's character sheet; brief 010, Aster 2026-10-15).
// Read, never invented: the approach's price and requirement from people.rules.json `approaches`, the person's fit from the temperament
// (respondsTo suits, refuses is refused, anything else works one step worse: contracts/rules/rulebook.json `fit`, Sky Q4), whether the need
// is one the approach acts on (needs.list[need].metBy), and the steps of a worse or standing-shifted approach from the rulebook's `values`.
// The rulebook is read as plain JSON (Keel's file, uncommitted while this was written): a value it lacks is left out and said so, not defaulted.
import type { Rules } from '../people/rules';

export const APPROACH_IDS = ['ask', 'pay', 'help', 'pressure'] as const;
export type ApproachId = (typeof APPROACH_IDS)[number];
export type FitWord = 'suits' | 'worse' | 'refused';
export type Rulebook = { values?: Record<string, { v: number }>; fit?: { approaches?: Record<string, { fit: FitWord }>; standing?: { band: string; temperament: string; approaches: string[]; fit: FitWord; src?: string }[] }; reports?: unknown } | undefined;

export type ApproachRow = {
  a: ApproachId;
  /** what the cell must know first: nothing, routine (Watch), need (Listen) */
  requires: string;
  usd: number; timeS: number; trail: number; leaves: string;
  /** the fit in a neutral district */
  fit: FitWord;
  fitWhy: string;
  /** the fit where the rulebook shifts it for the district's standing (Hero, Menace) */
  standing: { band: string; fit: FitWord; src: string }[];
  /** the need is one this approach acts on (help, pressure); for pay: whether paying meets it; ask: null */
  needActs: boolean | null;
  /** can the approach turn this person at all (neutral district) */
  works: boolean;
  /** Help: orders it takes; others null */
  orders: number | null;
  /** the price at the fit, in the same units the preview shows */
  total: { usd: number; trail: number; timeS: number };
  /** the lines that make it, base first */
  lines: string[];
  reports: boolean;
  /** one sentence */
  say: string;
};

const val = (rb: Rulebook, k: string): number | undefined => rb?.values?.[k]?.v;
const usd = (n: number) => `$${n.toLocaleString('en-US')}`;

/** The four approaches on a person of `temperament` with `need`, in a neutral district. */
export function approachRows(rules: Rules, rb: Rulebook, temperament: string, need: string): ApproachRow[] {
  const T = rules.temperaments[temperament as keyof Rules['temperaments']], N = rules.needs.list[need as keyof Rules['needs']['list']];
  return APPROACH_IDS.map((a): ApproachRow => {
    const A = rules.approaches[a];
    const asked = rb?.fit?.approaches?.[a]?.fit;
    const fit: FitWord = asked ?? ((T.respondsTo as readonly string[]).includes(a) ? 'suits' : (T.refuses as readonly string[]).includes(a) ? 'refused' : 'worse');
    const fitWhy = asked ? `rulebook fit.approaches.${a}: ${asked} for everyone cold` : fit === 'suits' ? `${T.name} responds to ${a}` : fit === 'refused' ? `${T.name} refuses ${a}` : `${T.name} responds to ${T.respondsTo.join('/')}, not ${a}: it still works, one step worse`;
    const standing = (rb?.fit?.standing ?? []).filter(r => r.temperament === temperament && r.approaches.includes(a)).map(r => ({ band: r.band, fit: r.fit, src: r.src ?? '' }));
    const metBy = (N?.metBy ?? []) as readonly string[];
    const needActs = a === 'ask' ? null : metBy.includes(a);
    const works = fit !== 'refused' && (a === 'pay' ? true : a === 'ask' ? fit === 'suits' : !!needActs);
    const lines = [`base ${usd(A.usd)} · ${A.timeS} s · +${A.trail} T`];
    let orders: number | null = null, usdTotal = A.usd, trail = A.trail, timeS = A.timeS;
    if (a === 'pay' && fit === 'worse') { const w = val(rb, 'worse-pay-usd'); if (w !== undefined) { usdTotal += w; lines.push(`wrong approach +${usd(w)}`); } }
    if (a === 'pressure' && fit === 'worse') { const w = val(rb, 'worse-pressure-trail'); if (w !== undefined) { trail += w; lines.push(`wrong approach +${w} T`); } }
    if (a === 'help') {
      orders = val(rb, 'help-orders') ?? 1;
      if (fit === 'worse') { const w = val(rb, 'worse-help-orders'); if (w !== undefined) { orders += w; lines.push(`wrong approach: +${w} Help order`); } }
      if (fit === 'suits' && temperament === 'loyal') { const w = val(rb, 'loyal-help-orders'); if (w !== undefined) { orders += w; lines.push(`trust over time: +${w} Help order`); } }
      usdTotal = A.usd * orders; trail = A.trail * orders; timeS = A.timeS * orders;
    }
    const mer = temperament === 'mercenary' && a === 'pay';
    if (mer) { const h = val(rb, 'mercenary-hero-usd'), m = val(rb, 'mercenary-menace-usd'); if (h !== undefined) lines.push(`Hero district ${h >= 0 ? '+' : '−'}${usd(Math.abs(h))}`); if (m !== undefined) lines.push(`Menace district ${m >= 0 ? '+' : '−'}${usd(Math.abs(m))}`); }
    const reports = T.reportsApproach && fit !== 'suits';
    const say = a === 'ask'
      ? (fit === 'refused' ? `Refused cold; leaves a remembered refusal (later offers +${usd(val(rb, 'refusal-usd') ?? 10000)}, +${val(rb, 'refusal-trail') ?? 1} T).` : 'Suits them: turns on a plain ask.')
      : fit === 'refused' ? `Fails: ${T.name} refuses ${a}; leaves a remembered refusal.`
      : a === 'help' && !needActs ? `Does not act on ${need} (met by ${metBy.join(', ')}).`
      : a === 'pressure' && !needActs ? `Does not act on ${need} (met by ${metBy.join(', ')}).`
      : a === 'help' ? `${orders === 1 ? 'One Help' : `${orders} Helps`}, ${usd(usdTotal)} in all${fit === 'worse' ? ' (one step worse)' : ''}; meets ${need}.`
      : a === 'pressure' ? `${usd(0)}, +${trail} T${fit === 'worse' ? ' (one step worse)' : ''}; leans on ${need}.`
      : `${usd(usdTotal)}${fit === 'worse' ? ' (one step worse)' : ''}; a payment the city can trace${needActs ? `; also meets ${need}` : ''}.`;
    return { a, requires: A.requires, usd: A.usd, timeS: A.timeS, trail: A.trail, leaves: A.leaves, fit, fitWhy, standing, needActs, works, orders, total: { usd: usdTotal, trail, timeS }, lines, reports, say };
  });
}
export const usdText = usd;
