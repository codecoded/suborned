import { Why, whyProblems } from '../../contracts/missions/handover/why.schema.ts';
import { shortGuideVersion, type Field, type Form, type ShortBrief, type ShortGuide, type ShortIssue, type ShortSet } from './short-types';
import { barkKey } from './short-barks';

/** The checks code can make for certain on a short-line family (Keel's side of the review); meaning, truth and voice are Motive's. */
const esc = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
/** Capitalised words a line may use mid-sentence: text.test.ts's list (the cast, the places, the verbs, the agents). */
const CAPS = new Set(['Inez', 'Popovic', 'Bureau', 'Scout', 'Fixer', 'Tech', 'Enforcer', 'Councillor', 'Council', 'Office', 'Records', 'Civic', 'Police', 'Handover', 'I', 'Trail', 'Alert',
  'Ask', 'Pay', 'Help', 'Pressure', 'Call', 'Watch', 'Listen', 'Loop', 'Reroute', 'Enter', 'Take', 'Fire', 'Carry', 'Board', 'Go', 'Kill', 'Security', 'Disable', 'Drive', 'Orbit', 'Walk', 'Mislead',
  'Withdraw', 'End', 'Hero', 'Neutral', 'Menace', 'Standing']);
const SHAPE: Record<Form, [RegExp, string]> = {
  speech: [/^[^\s].*[.!?]$/, 'a spoken line ends in . ? or !'], title: [/^[A-Z{][^.]*$/, 'a title starts with a capital and has no full stop'],
  short: [/^([a-z{]|Inez|Popovic)[^.]*$/, 'a short starts lower case (or with a name) and has no full stop'], line: [/^[A-Z{].*\.$/, 'a line is a sentence ending in a full stop'],
  plate: [/^[A-Z][^.]*$/, 'a plate starts with a capital and has no full stop'], label: [/^[^.!]*$/, 'a label has no full stop'], note: [/^[^.!]*$/, 'a note is a fragment, no full stop'],
};
const WORDY = /[\p{L}\p{N}]/u;
/** Words as the limits count them: a fill is one word; the part after " · " (a plate's seconds) is not counted. */
export const shortWords = (s: string) => s.replace(/\{[a-z-]+\}/g, 'x').split(' · ')[0]!.split(/\s+/).filter(t => WORDY.test(t)).length;
const widest = (s: string, f: Field) => s.replace(/\{([a-z-]+)\}/g, (m, k: string) => f.fills.find(x => x.id === k)?.widest ?? m);
const norm = (s: string) => s.toLowerCase().replace(/[^\p{L}\p{N} ]/gu, '').replace(/\s+/g, ' ').trim();

export function checkShort(brief: ShortBrief, set: ShortSet, guide: ShortGuide, base: { why?: unknown; dialogue?: unknown } = {}): ShortIssue[] {
  const bad: ShortIssue[] = [], flag = (rule: ShortIssue['rule'], reason: string) => bad.push({ rule, reason });
  const items = new Map(brief.items.map(i => [i.key, i]));
  const seen = new Map<string, string>();
  // guide /5: the other rows of the table, as the text the run read has them (a line is never in two rows: text.test.ts)
  if (brief.kind === 'bark' && shortGuideVersion(guide) >= 5 && Array.isArray(base.dialogue))
    for (const r of base.dialogue as { lines: string[] }[]) { const k = barkKey(r as never); if (!items.has(k)) for (const l of r.lines) seen.set(norm(l), k); }
  for (const k of set.items.map(i => i.key).filter((k, i, a) => a.indexOf(k) !== i)) flag('SHORT', `${k}: written twice; write each item once.`);
  for (const i of brief.items) if (!set.items.some(x => x.key === i.key)) flag('USEFUL', `${i.key}: missing; write it (or cut it, where it may be cut).`);
  for (const x of set.items) {
    const it = items.get(x.key);
    if (!it) { flag('TRUE', `${x.key}: not an item of this family.`); continue; }
    if (x.cut) {
      if (it.stays) flag('USEFUL', `${x.key}: may not be cut: ${it.stays}`);
      if (!x.reason.trim()) flag('USEFUL', `${x.key}: a cut needs its reason in one sentence.`);
      if (x.fields.length) flag('SHORT', `${x.key}: a cut item has no lines.`);
      continue;
    }
    for (const id of x.fields.map(f => f.id).filter((d, i, a) => a.indexOf(d) !== i)) flag('SHORT', `${x.key}: field ${id} written twice.`);
    for (const f of it.fields) {
      const got = x.fields.find(y => y.id === f.id);
      if (!got) { flag('SHORT', `${x.key}: its ${f.id} is missing.`); continue; }
      if (got.lines.length !== f.count) flag('SHORT', `${x.key}.${f.id}: ${got.lines.length} variants; write ${f.count}.`);
      for (const k of f.keep) if (!got.lines.includes(k)) flag('TRUE', `${x.key}.${f.id}: keep "${k}" word for word (a test pins it).`);
      got.lines.forEach((l, n) => {
        const at = `${x.key}.${f.id}${f.count > 1 ? ` #${n + 1}` : ''}`;
        const fills = [...l.matchAll(/\{([^}]*)\}/g)].map(m => m[1]!);
        for (const k of fills) if (!f.fills.some(y => y.id === k)) flag('TRUE', `${at}: {${k}} is not a fill this item has${f.fills.length ? ` (${f.fills.map(y => `{${y.id}}`).join(' ')})` : ' (none)'}.`);
        const wide = widest(l, f), words = shortWords(l);
        if (words > f.words) flag('SHORT', `${at}: ${words} words; this slot holds ${f.words}.`);
        if (wide.length > f.chars) flag('SHORT', `${at}: ${wide.length} characters at its widest ("${wide}"); this slot holds ${f.chars}.`);
        if (f.form === 'short' && l.length > 40) flag('SHORT', `${at}: ${l.length} characters as written; a short holds 40.`);
        const [shape, what] = SHAPE[f.form];
        if (!shape.test(l) || l !== l.trim() || /\s{2}/.test(l)) flag('SHORT', `${at}: ${what}, with no stray spaces.`);
        if (/[\r\n<>*_]|^\(|\)$/.test(l)) flag('SHORT', `${at}: one plain line: no markup, no stage directions.`);
        if (f.form !== 'speech' && l.includes('!')) flag('VOICE', `${at}: no exclamation marks in the book's voice.`);
        if (f.form === 'line' && (l.match(/[.!?](\s|$)/g)?.length ?? 0) > 2) flag('SHORT', `${at}: one sentence, two at most.`);
        for (const term of [...guide.avoid, ...guide.profanity])
          if (new RegExp(`(?<![\\p{L}\\p{N}_])${esc(term)}(?![\\p{L}\\p{N}_])`, 'iu').test(l)) flag(guide.profanity.includes(term) ? 'VOICE' : 'PLAIN', `${at}: "${term}" is on the mission's word filter.`);
        for (const name of guide.cell) if (new RegExp(`\\b${name}\\b`).test(l)) flag('NAMES', `${at}: "${name}" is a cell name; ${brief.kind === 'debrief' ? 'the page names an agent with {op}' : brief.kind === 'label' ? 'a label names an agent with {agent}' : 'nobody says it'}.`);
        if (/halcyon/i.test(l)) flag('NAMES', `${at}: Halcyon waits on Sky.`);
        const toks = l.replace(/\{[^}]*\}/g, 'x').split(/\s+/);
        toks.forEach((tok, i) => {
          const w = tok.replace(/^[^\p{L}]+|[^\p{L}]+$/gu, '').replace(/'(s|m|ve|ll|d|re)$/, '');
          if (i > 0 && w && /^\p{Lu}/u.test(w) && !/[.!?:·]$/.test(toks[i - 1]!) && !CAPS.has(w)) flag('NAMES', `${at}: "${w}" mid-sentence; only the game's names are capitalised.`);
        });
        if (brief.kind === 'bark' && it.speaker && !/^(scout|fixer|tech|enforcer):/.test(it.speaker) && /\d/.test(l)) flag('TRUE', `${at}: no digits in a person's mouth (people never quote the clock or the cell's numbers).`);
        if ((brief.kind === 'debrief' || brief.kind === 'label') && /[a-z]+-[a-z]+-[a-z]+|\bhash\b|replay|\btick\b|operative|engine|ledger/i.test(l)) flag('PLAIN', `${at}: an engine word; the ${brief.kind === 'label' ? 'HUD' : 'page'} uses the player's words (text.test.ts).`);
        // guide /10: Sky's rule on numbers in play: a label carries no number of its own (the game draws a key event's time beside it; never an act's time or a chance)
        if (brief.kind === 'label' && /\d|%/.test(l.replace(/\{[^}]*\}/g, ''))) flag('SHORT', `${at}: a label carries no number of its own: the game draws a key event's time beside it, and never an act's time or a chance (Sky 2026-11-08).`);
        for (const p of f.patterns) if (new RegExp(p.re, p.flags).test(l) !== p.must) flag('TRUE', `${at}: ${p.must ? 'needs' : 'must not have'} /${p.re}/: ${p.why}.`);
        if (f.form === 'speech') {
          const k = norm(l), other = seen.get(k);
          if (other && other !== x.key) flag('SHORT', `${at}: the same line as ${other}; every bark says its own words.`);
          else if (other === x.key) flag('SHORT', `${at}: repeats another variant of this item.`);
          seen.set(k, x.key);
        }
      });
    }
  }
  if (brief.kind === 'why' && base.why) for (const p of whyCheck(brief, set, base.why)) flag('SHORT', p);
  return bad;
}

// ---------------------------------------------------------------- where a why item lives in the block (the checks here, the apply step)

/** The block's path for an item's field: a string at the item's own path takes the field's line itself; otherwise the field is a key under it. */
export function whyPath(key: string, field: string, why: Record<string, unknown>): string[] {
  const [head, ...rest] = key.split('.');
  const segs = head === 'onWay' && rest[0] === 'to' ? ['onWay', 'to', rest.slice(1).join('.')] : head === 'places' || head === 'by' ? [head!, rest.join('.')] : [head!, ...rest];
  let at: unknown = why;
  for (const s of segs) at = (at as Record<string, unknown> | undefined)?.[s];
  return typeof at === 'string' ? segs : [...segs, field];
}
export function mergeWhy(brief: ShortBrief, set: ShortSet, why: unknown): Record<string, unknown> {
  const out = JSON.parse(JSON.stringify(why)) as Record<string, unknown>;
  for (const x of set.items) for (const f of x.fields) {
    if (!brief.items.some(i => i.key === x.key) || !f.lines[0]) continue;
    const path = whyPath(x.key, f.id, out);
    let at = out as Record<string, unknown>;
    for (const s of path.slice(0, -1)) at = (at[s] ??= {}) as Record<string, unknown>;
    at[path.at(-1)!] = f.lines[0];
  }
  return out;
}
/** why.schema.ts's own check on the block with this family's lines in it, kept to the family's items. */
function whyCheck(brief: ShortBrief, set: ShortSet, why: unknown): string[] {
  const merged = mergeWhy(brief, set, why), mine = brief.items.map(i => i.key);
  const parsed = Why.safeParse(merged);
  // the schema's superRefine is whyProblems itself: its issues carry the path in the message
  const problems = parsed.success ? whyProblems(parsed.data) : parsed.error.issues.map(i => i.path.length ? `${i.path.join('.')}: ${i.message}` : i.message);
  // whyProblems names a way `onWay.<way>` and a glossary word `word <key>`
  const ours = (p: string, k: string) => { const at = k.replace(/^onWay\.ways\./, 'onWay.'), word = k.match(/^(?:by|places|onWay\.to)\.(.+)$/)?.[1];
    return p.startsWith(`${at}.`) || p.startsWith(`${at}:`) || (word !== undefined && p.startsWith(`word ${word}:`)); };
  return problems.filter(p => mine.some(k => ours(p, k))).map(p => `why.schema.ts: ${p}`);
}
