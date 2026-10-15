// The dialogue checker (Fable, brief 012): rule ids D-0.. on the voice tables and on the lines they expand to. Each rule has an id and a
// source (GDD § or a rules file) and a failing example in tools/dialogue/test/check.test.ts. The checker holds the tables to the same rules
// as the people's lines: the tables are expanded for every person of the groups in scope, and every filled line is checked, so a template
// that is too long only for the person with the longest name fails with that person named.
//
// Carried over from the Handover's checker (contracts/missions/handover/text.test.ts): every situation covered, line lengths, no uncast names, tone, signal
// words, no repeats, determinism. Added: slots, scene shape, the lexicon, the knowledge rule for needs and secrets, the reference.
import { Lines as LinesSchema, type Lexicon, type Voices } from '../../contracts/dialogue/voices.schema';
import type { People } from '../../contracts/people/people.schema';
import type { Check } from '../plan/checks';
import { ALL, expandAll, linesFile, merge, plainCell, sceneId, type ExpandCtx, type Gap, type Merged, type Parts } from './expand';
import { referenceOf, REFERENCE } from './fixture';
import { slotsIn, wordCount } from './fill';
import { debriefFields, lexiconNeeds, TEMPERAMENTS, type DialogueRules, type PeopleRules, type Rulebook } from './rules';
import { keyOf, requiredCells, requiredScenes, requiredWatch, sceneKeyOf } from './situations';
import type { TextFile } from '../../contracts/missions/handover/text.schema';

export const GROUP = 'Dialogue';
export const RULES_SRC: Record<string, string> = {
  'D-0': 'GDD §09 (nothing hand-authored; unknown = reject): every situation the rulebook and the people rules can produce has a cell, and no cell is for one they cannot; the blocks a scope writes are exactly its own',
  'D-1': 'GDD §04 person = routine + need + temperament + memory; §09 dialogue from person data: every person of the groups in scope has a line for every situation they can be in, a Listen scene and a Watch scene',
  'D-2': 'dialogue.rules.json limits (the Handover\'s text.test.ts: a line ≤ 80 characters, a spoken line 1–12 words, ends like a sentence, 2–4 variants); GDD §10 text is a light layer',
  'D-3': 'dialogue.rules.json slots: only the slots the context allows, every slot filled for every person who reaches it, none left in a filled line',
  'D-4': 'GDD §09/§14 history, not backstory: the tables name no one; a scene names only the two people on its tie (the Handover\'s "no uncast names")',
  'D-5': 'GDD §09 tone + rating filters; dialogue.rules.json tone (avoid, profanity, rating, gendered, payload); GDD §14 civilians are never the payload',
  'D-6': 'dialogue.rules.json tone.signal and tone.pending: the game\'s own words are the cell\'s, a person says no digits, and no line names what awaits Sky',
  'D-7': 'the Handover\'s text.test.ts: no two lines repeat, and no cell repeats its own (GDD §09 risk: sameness)',
  'D-8': 'GDD §04 knowledge: a cold Ask or a Pay surfaces no need; a qualified cell is for a need or secret the rules list, on the replies and questioning that read them; dialogue.rules.json limits.qualifiedCellsPerGroup',
  'D-9': 'GDD §01/§09 same orders → same result: the expansion is byte-identical twice and valid as suborned.lines/1',
  'D-10': 'brief 012: a scene has a focus who speaks, a partner only where the tie gives one, the Scout only in a Watch, 3–6 beats, 2–3 variants that differ',
  'D-11': 'people.rules needs, positions and roles; the rulebook debrief.lines fields ("the debrief reports each with cause"); the engine\'s summons codes: the lexicon words every one, and a debrief line carries its cause',
  'D-12': 'GDD §14 the Handover is the reference: every situation its hand-written text gives a person of the voice, the tables give too, in as many variants',
};

export type CheckCtx = {
  dr: DialogueRules; rules: PeopleRules; rb: Rulebook; people: People;
  /** the tables of the run: the scope's own file and any pinned ones it fills its slots from */
  voices: Voices[];
  /** the scope being checked: a key of dr.scopes, or `all` (every block of every scope) */
  scope: string;
  seed: string;
  /** every name a line must not carry (the district's pool); defaults to the people's own given and family names */
  pool?: Iterable<string>;
  /** names a line may carry (the Handover's pinned names, for the hand-written fixture only) */
  named?: readonly string[];
  /** the Handover's text, for D-12; absent: D-12 is not run */
  reference?: Pick<TextFile, 'dialogue' | 'scenes'>;
};
export type Checked = { ok: boolean; checks: Check[]; counts: { persons: number; entries: number; scenes: number; faults: number }; lines: Lines | undefined; gaps: Gap[] };
type Lines = ReturnType<typeof linesFile>;
type R = { id: string; rule: string; bad: string[]; value?: string; limit?: string };
const row = (r: R): Check => ({ id: r.id, group: GROUP, rule: r.rule, value: r.value ?? (r.bad.length ? `${r.bad.length} fault(s)` : 'ok'), limit: r.limit ?? '0 faults', ok: !r.bad.length, ...(r.bad.length ? { detail: r.bad.slice(0, 12).join('; ') + (r.bad.length > 12 ? `; +${r.bad.length - 12} more` : '') } : {}), source: RULES_SRC[r.id] });
const RULES: Record<string, string> = {
  'D-0': 'Every situation has a cell, and only those; a scope writes only its own blocks',
  'D-1': 'Everyone speaks: every person of the groups in scope has all their lines and scenes',
  'D-2': 'Every filled line fits: at most 80 characters, spoken lines 1–12 words, sentence ends, 2–4 variants',
  'D-3': 'Slots: only the allowed ones, each filled, none left over',
  'D-4': 'Names: the tables name no one; a scene names only the two people on its tie',
  'D-5': 'Tone: no modern speech, strong profanity, explicit or gory words, no gendered words; civilians are never the payload',
  'D-6': 'Signal words: none of the cell\'s words in a person\'s mouth, no digits, nothing awaiting Sky',
  'D-7': 'No repeats: within a cell, a group, a scope or a scene',
  'D-8': 'Knowledge: a cold approach surfaces no need; qualified cells name a need or secret of the rules',
  'D-9': 'The expansion is deterministic and valid as suborned.lines/1',
  'D-10': 'Scenes: a focus who speaks, a partner only on a tie, the Scout only in a Watch, 3–6 beats, variants that differ',
  'D-11': 'The lexicon words every need, position, temperament, debrief line and summons key, each debrief line with its cause',
  'D-12': 'The Handover\'s reference lines are matched: the same situations, as many variants',
};

type Str = { path: string; text: string; kind: 'cell' | 'reads' | 'beat' | 'summon' | 'lexicon' | 'debrief' | 'address' | 'voice'; who?: string };
const CAP_OK = (dr: DialogueRules, named: readonly string[]) => new Set([...dr.tone.capitals, ...named]);

/** Every template string in the tables, with where it stands. */
export function templates(m: Merged): Str[] {
  const out: Str[] = [];
  for (const g of m.groups.values()) {
    out.push({ path: `${g.group}.voice`, text: g.voice, kind: 'voice', who: g.group });
    for (const a of g.address) out.push({ path: `${g.group}.address`, text: a, kind: 'address' });
    for (const c of g.cells) c.lines.forEach((t, i) => out.push({ path: `${g.group}.${keyOf(c)}${c.need ? `[need ${c.need}]` : ''}${c.secret ? `[secret ${c.secret}]` : ''}#${i}`, text: t, kind: 'cell', who: g.group }));
    g.reads.watch.forEach((t, i) => out.push({ path: `${g.group}.reads.watch#${i}`, text: t, kind: 'reads', who: g.group }));
  }
  for (const s of m.scenes) s.variants.forEach((v, j) => v.beats.forEach((b, k) => out.push({ path: `scene ${sceneId(s)} v${j} b${k}`, text: b.line, kind: 'beat', who: b.who })));
  const L = m.lexicon;
  if (L) {
    for (const n of L.needs) for (const f of ['noun', 'own', 'met'] as const) out.push({ path: `lexicon.needs.${n.need}.${f}`, text: n[f], kind: 'lexicon' });
    for (const p of L.positions) for (const f of ['job', 'noun'] as const) out.push({ path: `lexicon.positions.${p.position}.${f}`, text: p[f], kind: 'lexicon' });
    for (const r of L.reads) r.lines.forEach((t, i) => out.push({ path: `lexicon.reads.${r.temperament}#${i}`, text: t, kind: 'reads', who: 'scout' }));
    for (const d of L.debrief) d.lines.forEach((t, i) => out.push({ path: `lexicon.debrief.${d.id}#${i}`, text: t, kind: 'debrief' }));
    for (const s of L.summons) for (const side of ['caller', 'summoned'] as const) s[side].forEach((t, i) => out.push({ path: `lexicon.summons.${s.key}.${side}#${i}`, text: t, kind: 'summon', who: side }));
  }
  return out;
}

/** verb, tie, group of the scene a beat's path names (`scene listen/workmate/-/idealist v0 b1`). */
const sceneParts = (path: string): [string, string, string] => { const [v, t, g] = /^scene (\S+)/.exec(path)![1]!.split('/'); return [v!, t!, g!]; };
const norm = (s: string) => s.toLowerCase().replace(/\{[a-z_-]+\}/g, '{}').replace(/[^a-z{}]+/g, ' ').trim();
const esc = (w: string) => w.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

export function checkDialogue(c: CheckCtx): Checked {
  const { dr, rules, rb } = c, scopeDef = c.scope === 'all' ? undefined : dr.scopes[c.scope];
  const out: R[] = [];
  if (c.scope !== 'all' && !scopeDef) throw new Error(`unknown scope ${c.scope}`);
  const blocks = new Set(c.scope === 'all' ? Object.values(dr.scopes).flatMap(s => s.blocks) : scopeDef!.blocks);
  const scopeGroups = c.scope === 'all' ? Object.keys(dr.groups) : [...(scopeDef!.groups ?? [])];
  const { merged: M, problems } = merge(c.voices);
  const named = c.named ?? [], capOk = CAP_OK(dr, named);
  const pool = new Set(c.pool ?? c.people.persons.flatMap(p => p.name.split(/\s+/))), poolBad = (w: string) => pool.has(w) && !named.includes(w);

  // ---- D-0 coverage
  {
    const bad = [...problems], mine = c.voices.filter(v => c.scope === 'all' || v.scope === c.scope);
    if (!mine.length) bad.push(`no tables for scope ${c.scope}`);
    for (const v of mine) {
      if (v.lexicon && !blocks.has('lexicon')) bad.push(`${v.scope}: writes a lexicon, which is not a ${c.scope} block`);
      if (v.groups.length && !blocks.has('groups')) bad.push(`${v.scope}: writes groups, which is not a ${c.scope} block`);
      if (v.scenes.length && !blocks.has('scenes') && !scopeGroups.length) bad.push(`${v.scope}: writes scenes, which is not a ${c.scope} block`);
      for (const g of v.groups) if (!scopeGroups.includes(g.group)) bad.push(`${v.scope}: group ${g.group} is not one of this scope's (${scopeGroups.join(', ') || 'none'})`);
    }
    if (blocks.has('lexicon') && !M.lexicon) bad.push('the lexicon is missing');
    const want = [...(blocks.has('scenes') ? requiredScenes(rules) : []), ...requiredWatch(scopeGroups)], have = new Set(M.scenes.map(sceneId)), wanted = new Set(want.map(sceneKeyOf));
    for (const w of want) if (!have.has(sceneKeyOf(w))) bad.push(`scene ${sceneKeyOf(w)}: missing`);
    for (const v of mine) for (const s of v.scenes) if (!wanted.has(sceneId(s))) bad.push(`scene ${sceneId(s)}: not asked for (a tie, temperament or group the rules do not list, or another scope's)`);
    for (const gname of scopeGroups) {
      const g = M.groups.get(gname);
      if (!g) { bad.push(`group ${gname}: not written`); continue; }
      const req = requiredCells(dr, rb, rules, gname), reqKeys = new Set(req.map(keyOf)), seen = new Set<string>();
      for (const k of req) if (!plainCell(g.cells, k)) bad.push(`${gname}: no plain cell ${keyOf(k)}`);
      for (const cell of g.cells) {
        const key = keyOf(cell), full = `${key}|${cell.need ?? ''}|${cell.secret ?? ''}`;
        if (seen.has(full)) bad.push(`${gname}: cell ${key}${cell.need ? ` need ${cell.need}` : ''}${cell.secret ? ` secret ${cell.secret}` : ''} is written twice`);
        seen.add(full);
        if (!reqKeys.has(key)) bad.push(`${gname}: cell ${key} is not a situation ${gname} can be in (a dead cell)`);
      }
      if (g.address.length < dr.limits.addressTerms.min || g.address.length > dr.limits.addressTerms.max) bad.push(`${gname}: ${g.address.length} address terms (${dr.limits.addressTerms.min}–${dr.limits.addressTerms.max})`);
    }
    for (const g of M.groups.keys()) if (!dr.groups[g]) bad.push(`group ${g} is not in the rules`);
    out.push({ id: 'D-0', rule: RULES['D-0']!, bad, value: bad.length ? undefined : `${scopeGroups.length} group(s), ${scopeGroups.reduce((n, g) => n + (M.groups.get(g)?.cells.length ?? 0), 0)} cell(s), ${mine.reduce((n, v) => n + v.scenes.length, 0)} scene(s)${blocks.has('lexicon') ? ', lexicon' : ''}` });
  }

  // ---- expansion
  const parts: Parts = c.scope === 'all' ? ALL(dr) : { groups: scopeGroups, scenes: blocks.has('scenes'), lexicon: blocks.has('lexicon') };
  const ctx: ExpandCtx = { people: c.people, rules, dr, rb, tables: M, seed: c.seed };
  const run = expandAll(ctx, parts), again = expandAll(ctx, parts);
  const lines = linesFile(ctx, run.persons, { people: '', rules: '', tables: {} }, { id: c.people.plan.id, version: c.people.plan.version });
  const gapRows = (rule: string) => {
    const by = new Map<string, string[]>();
    for (const g of run.gaps.filter(x => x.rule === rule)) { const k = g.message + ' · ' + g.where.replace(/^p-[a-z0-9-]+-\d+ /, ''); by.set(k, [...(by.get(k) ?? []), g.where]); }
    return [...by].map(([k, w]) => `${k}: ${w.length} (${w[0]}${w.length > 1 ? ', …' : ''})`);
  };
  {
    const bad = gapRows('D-1');
    out.push({ id: 'D-1', rule: RULES['D-1']!, bad, value: bad.length ? undefined : `${run.persons.length} people` });
  }

  // ---- strings: templates (pre-fill) and what the people get (filled)
  const T = templates(M);
  const inScope = (s: Str) => {
    if (c.scope === 'all') return true;
    if (s.kind === 'lexicon' || s.kind === 'debrief' || s.kind === 'summon') return blocks.has('lexicon');
    if (s.kind === 'reads') return s.who === 'scout' ? blocks.has('lexicon') : scopeGroups.includes(s.who!);
    if (s.kind === 'beat') { const [verb, , group] = sceneParts(s.path); return verb === 'watch' && group !== '-' ? scopeGroups.includes(group) : blocks.has('scenes'); }
    return scopeGroups.includes(s.who!) || s.kind === 'address' && scopeGroups.includes(s.path.split('.')[0]!);
  };
  const mineT = T.filter(inScope);

  // D-2 fit
  {
    const bad: string[] = [], max = dr.limits.maxLine, push = (m: string) => bad.push(m);
    const check = (path: string, t: string, spoken = true) => {
      if (t.length > max) push(`${path}: ${t.length} characters (${t})`);
      if (spoken) {
        const w = wordCount(t);
        if (w < dr.limits.minWords || w > dr.limits.maxWords) push(`${path}: ${w} words (${t})`);
        if (!/[.!?]$/.test(t) || t !== t.trim() || /\s{2}/.test(t)) push(`${path}: not a clean sentence (${t})`);
      }
    };
    const seenAt = new Set<string>();
    for (const p of run.persons) {
      for (const e of p.entries) e.lines.forEach((t, i) => { const k = `${p.group}/${e.event}/${e.verb}/${e.outcome}/${e.temperament}/${e.side}#${i}/${t}`; if (!seenAt.has(k)) { seenAt.add(k); check(`${p.id} ${e.speaker} ${e.event}/${e.verb ?? ''}/${e.outcome ?? ''}`, t); } });
      for (const s of p.scenes) {
        for (const v of s.variants) v.beats.forEach((b, k) => check(`${p.id} scene ${s.verb}/${s.tie} b${k}`, b.line));
        if (s.result.length > 160) push(`${p.id} scene ${s.verb}/${s.tie} result: ${s.result.length} characters`);
      }
    }
    for (const g of M.groups.values()) for (const cell of g.cells) if (cell.lines.length < dr.limits.cellLines.min || cell.lines.length > dr.limits.cellLines.max) push(`${g.group}.${keyOf(cell)}: ${cell.lines.length} variants`);
    for (const s of M.scenes) if (s.variants.length < dr.limits.sceneVariants.min || s.variants.length > dr.limits.sceneVariants.max) push(`scene ${sceneId(s)}: ${s.variants.length} variants`);
    for (const t of mineT) if (t.kind !== 'voice' && t.kind !== 'address' && t.text.length > dr.limits.templateMax) push(`${t.path}: a template of ${t.text.length} characters`);
    if (blocks.has('lexicon') && M.lexicon) {
      for (const d of M.lexicon.debrief) {
        const f = d.lines.map(l => l.replace(/\{([a-z-]+)\}/g, (_, k: string) => dr.lexicon.debriefFill[k] ?? `{${k}}`));
        f.forEach((l, i) => { if (l.length > dr.limits.debriefMax) push(`lexicon.debrief.${d.id}#${i}: ${l.length} characters filled widest (${l}); at most ${dr.limits.debriefMax}`); });
      }
    }
    out.push({ id: 'D-2', rule: RULES['D-2']!, bad });
  }

  // D-3 slots
  {
    const bad = gapRows('D-3'), allowed = (s: Str): string[] => {
      if (s.kind === 'cell') { const g = dr.groups[s.who!]; return dr.slots.cell.filter(x => !(g?.without ?? []).includes(x)); }
      if (s.kind === 'reads') return s.who === 'scout' ? dr.slots.reads : dr.slots.reads.filter(x => !(dr.groups[s.who!]?.without ?? []).includes(x) && !dr.slots.needSlots.includes(x)); // a Watch teaches the routine, not the need
      if (s.kind === 'summon') return dr.slots.summons;
      if (s.kind === 'debrief') return [];
      if (s.kind === 'beat') {
        const [verb, tieK, groupK] = sceneParts(s.path), noPartner = tieK === 'none' || (verb === 'watch' && groupK !== '-');
        return dr.slots.scene.filter(x => !(noPartner && x === 'other') && !(groupK !== '-' && (dr.groups[groupK]?.without ?? []).includes(x)) && !(verb === 'watch' && dr.slots.needSlots.includes(x)));
      }
      return [];
    };
    for (const s of mineT) {
      if (s.kind === 'debrief') {
        const id = /^lexicon\.debrief\.([a-z-]+)#/.exec(s.path)![1]!, fs = debriefFields(rb)[id] ?? [];
        for (const k of slotsIn(s.text)) if (!fs.includes(k)) bad.push(`${s.path}: {${k}} is not a field of the rulebook's ${id} line (${fs.join(', ')})`);
        continue;
      }
      const ok = allowed(s);
      for (const k of new Set(slotsIn(s.text))) if (!ok.includes(k)) bad.push(`${s.path}: {${k}} is not allowed here (allowed: ${ok.join(', ') || 'none'})`);
      if (/[{}]/.test(s.text.replace(/\{[a-z][a-z0-9_-]*\}/g, ''))) bad.push(`${s.path}: a stray brace (${s.text})`);
    }
    for (const p of run.persons) {
      for (const e of p.entries) for (const t of e.lines) if (/[{}]/.test(t)) bad.push(`${p.id} ${e.event}: an unfilled slot (${t})`);
      for (const s of p.scenes) { for (const v of s.variants) for (const b of v.beats) if (/[{}]/.test(b.line)) bad.push(`${p.id} scene ${s.verb}: an unfilled slot (${b.line})`); if (/[{}]/.test(s.result.replace('{temperament}', ''))) bad.push(`${p.id} scene ${s.verb}: an unfilled slot in the result (${s.result})`); }
    }
    out.push({ id: 'D-3', rule: RULES['D-3']!, bad: [...new Set(bad)] });
  }

  // D-4 names
  {
    const bad: string[] = [];
    for (const s of mineT) {
      const text = s.text.replace(/\{[a-z][a-z0-9_-]*\}/g, 'x');
      for (const w of text.match(/[\p{L}'-]+/gu) ?? []) if (poolBad(w)) bad.push(`${w}: ${s.path}`);
      const toks = text.split(/\s+/);
      toks.forEach((tok, i) => {
        const w = tok.replace(/^[^\p{L}]+|[^\p{L}]+$/gu, '');
        if (!w || !/^\p{Lu}/u.test(w) || w === 'x') return;
        const prev = i === 0 ? '.' : toks[i - 1]!;
        if (/[.!?:·]$/.test(prev)) return;
        if (!capOk.has(w.replace(/'(s|m|ve|ll|d|re)$/, ''))) bad.push(`${w} (a capital mid-sentence): ${s.path}`);
      });
    }
    for (const p of run.persons) for (const s of p.scenes) {
      const me = c.people.persons.find(x => x.id === p.id)!, other = s.with ? c.people.persons.find(x => x.id === s.with) : undefined;
      const ok = new Set([me.name.split(/\s+/)[0]!, ...(other ? [other.name.split(/\s+/)[0]!] : []), ...named]);
      for (const v of s.variants) for (const b of v.beats) for (const w of b.line.match(/[\p{L}'-]+/gu) ?? []) if (pool.has(w) && !ok.has(w)) bad.push(`${p.id}: ${w} is no one on the tie (${s.verb}/${s.tie})`);
    }
    out.push({ id: 'D-4', rule: RULES['D-4']!, bad: [...new Set(bad)] });
  }

  // D-5 tone, D-6 signal words
  {
    const bad: string[] = [], whole = (w: string) => new RegExp(`(^|[^a-z])${esc(w)}($|[^a-z])`, 'i'), start = (w: string) => new RegExp(`\\b${esc(w)}`, 'i');
    for (const s of mineT) {
      const t = s.text.toLowerCase();
      for (const w of dr.tone.avoid) if (whole(w).test(t)) bad.push(`"${w}" (modern speech): ${s.path}`);
      for (const w of dr.tone.profanity) if (start(w).test(t)) bad.push(`"${w}" (strong profanity): ${s.path}`);
      for (const w of [...dr.tone.rating.explicit, ...dr.tone.rating.gore]) if (start(w).test(t)) bad.push(`"${w}" (rating): ${s.path}`);
      for (const w of dr.tone.gendered) if (whole(w).test(t)) bad.push(`"${w}" (gendered): ${s.path}`);
      for (const re of dr.tone.payload) if (new RegExp(re, 'i').test(t)) bad.push(`"${re}" (a civilian as payload): ${s.path}`);
    }
    out.push({ id: 'D-5', rule: RULES['D-5']!, bad: [...new Set(bad)] });
    const sig: string[] = [];
    for (const s of mineT) {
      const t = s.text.toLowerCase(), person = s.kind === 'cell' || s.kind === 'summon' || s.kind === 'address' || (s.kind === 'beat' && s.who !== 'scout');
      for (const w of dr.tone.pending) if (new RegExp(`\\b${esc(w.toLowerCase())}\\b`).test(t)) sig.push(`"${w}" (awaits Sky): ${s.path}`);
      if (!person) continue;
      for (const w of dr.tone.signal) if (whole(w).test(t)) sig.push(`"${w}" (the game's word): ${s.path}`);
      if (/[\d$]/.test(s.text)) sig.push(`a digit or a dollar sign in speech: ${s.path}`);
    }
    out.push({ id: 'D-6', rule: RULES['D-6']!, bad: [...new Set(sig)] });
  }

  // D-7 repeats
  {
    const bad: string[] = [], at = new Map<string, string>();
    for (const s of mineT) {
      if (s.kind === 'address' || s.kind === 'lexicon' || s.kind === 'voice') continue;
      const k = norm(s.text), prev = at.get(k);
      if (prev !== undefined) bad.push(`"${s.text}" in ${prev} and ${s.path}`); else at.set(k, s.path);
    }
    out.push({ id: 'D-7', rule: RULES['D-7']!, bad });
  }

  // D-8 knowledge and qualifiers
  {
    const bad: string[] = [], needs = new Set(lexiconNeeds(rules)), secrets = Object.keys(rules.secrets?.list ?? {});
    const okNeed = new Set(dr.slots.needIn);
    for (const gname of scopeGroups) {
      const g = M.groups.get(gname);
      if (!g) continue;
      const q = g.cells.filter(x => x.need || x.secret);
      if (q.length > dr.limits.qualifiedCellsPerGroup) bad.push(`${gname}: ${q.length} qualified cells (at most ${dr.limits.qualifiedCellsPerGroup})`);
      for (const cell of q) {
        const ev = cell.verb ? `${cell.event}/${cell.verb}` : cell.event;
        if (cell.need && !needs.has(cell.need)) bad.push(`${gname}.${keyOf(cell)}: need ${cell.need} is not one the lexicon words`);
        if (cell.secret && !secrets.includes(cell.secret)) bad.push(`${gname}.${keyOf(cell)}: secret ${cell.secret} is not in the rules`);
        if (!(cell.event === 'reply' || cell.event === 'questioned')) bad.push(`${gname}.${keyOf(cell)}: only replies and questioning can be qualified`);
        if (cell.need && cell.event === 'reply' && !okNeed.has(ev)) bad.push(`${gname}.${keyOf(cell)}: a need-qualified reply surfaces the need, so only ${dr.slots.needIn.join(', ')}`);
      }
      for (const cell of g.cells) {
        const ev = cell.verb ? `${cell.event}/${cell.verb}` : cell.event;
        const uses = cell.lines.some(t => slotsIn(t).some(s => dr.slots.needSlots.includes(s)));
        if (uses && !okNeed.has(ev)) bad.push(`${gname}.${keyOf(cell)}: surfaces the need before it is known (need slots only in ${dr.slots.needIn.join(', ')})`);
      }
    }
    out.push({ id: 'D-8', rule: RULES['D-8']!, bad });
  }

  // D-9 deterministic and valid
  {
    const bad: string[] = [];
    if (JSON.stringify(run.persons) !== JSON.stringify(again.persons)) bad.push('two expansions of the same tables differ');
    const parsed = LinesSchema.safeParse({ ...lines, inputs: { ...lines.inputs } });
    if (!parsed.success && run.persons.length) bad.push(`the lines do not parse as suborned.lines/1: ${parsed.error.issues.slice(0, 3).map(i => `${i.path.join('.')} ${i.message}`).join(' | ')}`);
    out.push({ id: 'D-9', rule: RULES['D-9']!, bad });
  }

  // D-10 scenes
  {
    const bad: string[] = [];
    for (const s of M.scenes) {
      const id = sceneId(s), partner = (s.verb === 'listen' || s.verb === 'call') && s.tie !== 'none' || (s.verb === 'watch' && s.tie === 'lover');
      if (s.variants.length >= 2 && new Set(s.variants.map(v => JSON.stringify(v))).size !== s.variants.length) bad.push(`scene ${id}: two variants are the same`);
      if ((s.verb === 'listen' || s.verb === 'call') && (s.group || !s.temperament || !s.tie)) bad.push(`scene ${id}: a ${s.verb} scene names a tie and a temperament, no group`);
      if (s.verb === 'call' && s.tie !== 'lover') bad.push(`scene ${id}: a call scene is the lovers' summons`);
      if (s.verb === 'watch' && ((s.group && s.tie) || (!s.group && s.tie !== 'lover') || s.temperament)) bad.push(`scene ${id}: a Watch scene is a group's, or the lovers', with no temperament`);
      s.variants.forEach((v, j) => {
        const who = new Set(v.beats.map(b => b.who));
        if (!who.has('focus')) bad.push(`scene ${id} v${j}: the focus never speaks`);
        if (partner && !who.has('other')) bad.push(`scene ${id} v${j}: the partner never speaks`);
        if (!partner && who.has('other')) bad.push(`scene ${id} v${j}: there is no partner on this scene`);
        if (who.has('scout') && s.verb !== 'watch') bad.push(`scene ${id} v${j}: the Scout speaks only in a Watch`);
        if (who.has('bystander') && partner) bad.push(`scene ${id} v${j}: a tie scene is between its two people, no bystander`);
        if (!partner && s.verb !== 'watch' && !who.has('bystander')) bad.push(`scene ${id} v${j}: with no tie, the focus talks with a bystander`);
      });
    }
    out.push({ id: 'D-10', rule: RULES['D-10']!, bad });
  }

  // D-11 the lexicon
  {
    const bad: string[] = [], L: Lexicon | undefined = M.lexicon;
    if (blocks.has('lexicon') && L) {
      const same = (name: string, have: string[], want: string[]) => {
        for (const w of want) if (!have.includes(w)) bad.push(`${name}: ${w} is missing`);
        for (const h of new Set(have)) if (!want.includes(h)) bad.push(`${name}: ${h} is not one the rules list`);
        if (new Set(have).size !== have.length) bad.push(`${name}: a word is written twice`);
      };
      same('needs', L.needs.map(n => n.need), lexiconNeeds(rules));
      same('positions', L.positions.map(p => p.position), [...new Set(Object.values(dr.groups).flatMap(g => g.covers))].sort());
      same('reads', L.reads.map(r => r.temperament), [...TEMPERAMENTS]);
      same('summons', L.summons.map(s => s.key), dr.lexicon.summonKeys);
      const fields = debriefFields(rb);
      same('debrief', L.debrief.map(d => d.id), Object.keys(fields));
      for (const d of L.debrief) for (const [i, l] of d.lines.entries()) {
        const have = new Set(slotsIn(l));
        for (const f of fields[d.id] ?? []) if (!have.has(f)) bad.push(`debrief ${d.id}#${i}: the cause {${f}} is not in the line`);
      }
      for (const f of new Set(Object.values(fields).flat())) if (!(f in dr.lexicon.debriefFill)) bad.push(`debriefFill has no width for the rulebook field ${f}`);
    }
    out.push({ id: 'D-11', rule: RULES['D-11']!, bad });
  }

  // D-12 the reference
  if (c.reference) {
    const bad: string[] = [];
    for (const gname of scopeGroups) {
      const ref = referenceOf(c.reference, gname), g = M.groups.get(gname);
      if (!ref || !g) continue;
      const temps = new Set(requiredCells(dr, rb, rules, gname).filter(k => k.temperament).map(k => k.temperament));
      for (const r of ref.cells) {
        if (r.temperament && !temps.has(r.temperament)) continue;
        const k = { event: r.event, verb: r.verb, outcome: r.outcome, temperament: r.temperament }, mine = plainCell(g.cells, k);
        if (!mine) bad.push(`${gname}: the Handover has ${keyOf(k)} (${REFERENCE[gname]}) and the tables do not`);
        else if (mine.lines.length < r.lines.length) bad.push(`${gname}.${keyOf(k)}: ${mine.lines.length} variants, the Handover has ${r.lines.length}`);
      }
      if (ref.watch.length && g.reads.watch.length < ref.watch.length) bad.push(`${gname}: the Watch read-out has fewer lines than the Handover's`);
    }
    out.push({ id: 'D-12', rule: RULES['D-12']!, bad });
  }

  const checks = out.map(row), entries = run.persons.reduce((n, p) => n + p.entries.length, 0), scenes = run.persons.reduce((n, p) => n + p.scenes.length, 0);
  return { ok: checks.every(x => x.ok), checks, counts: { persons: run.persons.length, entries, scenes, faults: out.reduce((n, r) => n + r.bad.length, 0) }, lines: run.persons.length ? lines : undefined, gaps: run.gaps };
}
