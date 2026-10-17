// The checker rules for open needs (brief 017 N-1..N-8; Motive owns the rules, Pulse runs them): the kinds, the catalogue of records and the records the people drew.
// Each has a failing example in tools/people/test/needs.test.ts. Run by checkPeople when the ctx carries needs; a ctx without them (a pinned older run) has no N-rules.
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Cast } from '../../contracts/people/cast.schema';
import type { People } from '../../contracts/people/people.schema';
import { root } from '../rag/config';
import { needFits, needsOf } from './expand';
import { metByOf, type Needs } from './needs';
import type { Rules } from './rules';
import type { MissionIn, Cast as RoleCast } from './handover';
import type { Scope } from './scope';

export type NR = { id: string; rule: string; bad: string[]; value: string; limit: string };
export const N_SRC: Record<string, string> = {
  'N-1': 'brief 017 (Sky 2026-10-18): every record\'s kind exists (unknown = reject); the kinds equal the engine\'s own table (people.rules.json needs.list less the retired `secret`), so no number moves; each person\'s record is of their kind',
  'N-2': 'brief 017: holder, subject and size use what the rules allow and bind for every person who draws the record, else the expansion fails (as P-0); a size priced by a rulebook value needs that value to exist',
  'N-3': 'brief 017 (Fable\'s labels): 1-2 words, at most 20 characters, no sentence, no digits, dates or era words, no gendered words, no names; unique in the district',
  'N-4': 'brief 017: tone, GDD §06 content line: no label or subject names a child, harm or grief as leverage; care records are never pressable',
  'N-5': 'brief 017: a record\'s metBy is a subset of its kind\'s; every temperament in a row can still reach a record (needs.fit, today\'s needFits)',
  'N-6': 'brief 017 (Sky answer 1, 2026-10-18): no two records of an area differ only in wording; each differs in kind, size, holder, subject or approaches',
  'N-7': 'brief 017: no orphan records (every record\'s area is an area of the cast); every need kind and secret kind has a record in every area',
  'N-8': 'brief 017 (Handover pins, M-2): a mission role that names a need is cast on a person whose record is of that kind, size standard, holder beyond the sheet ($40,000 Help each; the purse sum holds)',
};

const words = (label: string) => label.trim().split(/\s+/).filter(Boolean);
let rulebookValues: Set<string> | undefined;
const rulebookHas = (v: string) => (rulebookValues ??= new Set(Object.keys(JSON.parse(readFileSync(resolve(root, 'contracts/rules/rulebook.json'), 'utf8')).values))).has(v);

export function checkNeeds(a: { needs: Needs; rules: Rules; cast: Cast; scope: Scope; people: People; mission?: MissionIn; handover: RoleCast[]; peopleRulesList?: Rules['needs']['list'] }): NR[] {
  const { needs, rules, cast, scope, people } = a, N = needs.rules, C = needs.catalogue, P = people.persons, out: NR[] = [];
  const areas = new Set(cast.rows.map(r => r.area));
  const recs = [...C.needs.map(r => ({ r, secret: false })), ...C.secrets.map(r => ({ r, secret: true }))];
  const kindOf = (r: { kind: string }, secret: boolean) => (secret ? N.secretKinds[r.kind] : N.kinds[r.kind]);
  const byId = new Map(recs.map(x => [x.r.id, x]));
  const give = (id: string, rule: string, bad: string[], value: string, limit: string) => out.push({ id, rule, bad: [...new Set(bad)], value, limit });

  // N-1 the kinds exist and are the engine's
  {
    const bad: string[] = [], list = rules.needs.list;
    const want = Object.keys(list).filter(k => !N.sameAs.minus.includes(k)).sort(), have = Object.keys(N.kinds).sort();
    if (JSON.stringify(want) !== JSON.stringify(have)) bad.push(`kinds ${have.join(',')} are not people.rules.json needs.list less ${N.sameAs.minus.join(',')} (${want.join(',')})`);
    for (const k of have) {
      const d = list[k], v = N.kinds[k]!;
      if (!d) continue;
      if (JSON.stringify([...d.metBy].sort()) !== JSON.stringify([...v.metBy].sort())) bad.push(`kind ${k}: metBy ${v.metBy.join('/')}, the engine reads ${d.metBy.join('/')}`);
      if (d.help !== v.help || d.requires !== v.requires) bad.push(`kind ${k}: help ${v.help}, requires ${v.requires}; the engine reads ${d.help}, ${d.requires}`);
    }
    if (rules.secrets) {
      const sw = Object.keys(rules.secrets.list).sort(), sh = Object.keys(N.secretKinds).sort();
      if (JSON.stringify(sw) !== JSON.stringify(sh)) bad.push(`secret kinds ${sh.join(',')} are not people.rules.json secrets.list (${sw.join(',')})`);
      for (const k of sh) { const d = (rules.secrets.list as Record<string, { requires: string }>)[k]; if (d && d.requires !== N.secretKinds[k]!.requires) bad.push(`secret kind ${k}: requires ${N.secretKinds[k]!.requires}, the engine reads ${d.requires}`); }
    }
    for (const { r, secret } of recs) if (!kindOf(r, secret)) bad.push(`record ${r.id}: ${secret ? 'secret ' : ''}kind ${r.kind} is not in the rules`);
    for (const p of P) {
      const rec = p.needRecord ? byId.get(p.needRecord) : undefined;
      if (!p.needRecord) bad.push(`${p.id}: no need record`);
      else if (!rec || rec.secret) bad.push(`${p.id}: need record ${p.needRecord} is not in the catalogue`);
      else if (rec.r.kind !== p.need) bad.push(`${p.id}: record ${p.needRecord} is of kind ${rec.r.kind}, their need is ${p.need}`);
      if (p.secret) {
        const s = p.secretRecord ? byId.get(p.secretRecord) : undefined;
        if (!p.secretRecord) bad.push(`${p.id}: secret ${p.secret} has no secret record`);
        else if (!s || !s.secret) bad.push(`${p.id}: secret record ${p.secretRecord} is not in the catalogue`);
        else if (s.r.kind !== p.secret) bad.push(`${p.id}: secret record ${p.secretRecord} is of kind ${s.r.kind}, their secret is ${p.secret}`);
      } else if (p.secretRecord) bad.push(`${p.id}: carries a secret record and no secret`);
    }
    give('N-1', 'Every record\'s kind exists, the kinds are the engine\'s, and each person\'s record is of their kind', bad, `${C.needs.length + C.secrets.length} records, ${have.length} kinds`, 'unknown = reject');
  }

  // N-2 holder, subject, size and binding
  {
    const bad: string[] = [];
    const known = (b: string, x: string) => (b === 'trade' ? x in rules.temperamentsBy.trades : b === 'role' ? x in rules.temperamentsBy.roles : x in rules.ties.kinds);
    for (const { r, secret } of recs) {
      const k = kindOf(r, secret);
      if (!secret) {
        const nr = r as (typeof C.needs)[number], price = N.sizes[nr.size];
        if (price === undefined) bad.push(`record ${r.id}: size ${nr.size} is not in the rules (${Object.keys(N.sizes).join(', ')})`);
        else if (typeof price === 'string' && !rulebookHas(price.slice(2))) bad.push(`record ${r.id}: size ${nr.size} is priced by ${price}, which the rulebook does not have yet`);
      }
      if (r.holder && r.holder !== 'beyond') {
        const [b, x] = r.holder.split(':') as [string, string];
        if (!(N.bindings as readonly string[]).includes(b)) bad.push(`record ${r.id}: holder ${r.holder} uses a binding the rules do not have`);
        else if (!known(b, x)) bad.push(`record ${r.id}: holder ${r.holder} names no ${b} of the district`);
        else if (!(k && 'holders' in k && k.holders?.includes(r.holder))) bad.push(`record ${r.id}: its kind ${r.kind} does not allow holder ${r.holder}`);
      }
      if (r.subject) {
        const x = r.subject.split(':')[1]!;
        if (!known('tie', x)) bad.push(`record ${r.id}: subject ${r.subject} names no tie of the rules`);
        else if (!secret && N.kinds[r.kind] && N.kinds[r.kind]!.requires !== `${x}-tie`) bad.push(`record ${r.id}: subject ${r.subject}, but its kind ${r.kind} requires ${N.kinds[r.kind]!.requires}`);
      }
    }
    for (const p of P) {
      const rec = p.needRecord ? byId.get(p.needRecord)?.r : undefined;
      if (!rec) continue;
      const home = scope.homes.find(h => h.plot === p.home.plot)?.area ?? scope.areas[0]!, area = p.work?.area ?? home;
      if (rec.area !== area) bad.push(`${p.id}: record ${rec.id} is of area ${rec.area}, they stand in ${area}`);
      if (rec.subject && !p.ties.some(t => t.kind === rec.subject!.split(':')[1])) bad.push(`${p.id}: record ${rec.id} is about a ${rec.subject}, which they have no tie for`);
      if (p.secretRecord) { const s = byId.get(p.secretRecord)?.r; if (s && s.area !== area) bad.push(`${p.id}: secret record ${s.id} is of area ${s.area}, they stand in ${area}`); }
    }
    give('N-2', 'Holders, subjects and sizes use what the rules allow, and bind for every person who draws the record', bad, `${recs.length} records, ${P.length} people`, '0 unbound');
  }

  // N-3 labels
  {
    const bad: string[] = [], L = N.label, seenId = new Set<string>(), seenLabel = new Map<string, string>();
    const names = new Set<string>([...cast.names.given.map(n => n.name), ...cast.names.family.map(n => n.name), ...rules.names.pinned.map(n => n.name)].map(n => n.toLowerCase()));
    for (const { r } of recs) {
      if (seenId.has(r.id)) bad.push(`record id ${r.id} is used twice`); seenId.add(r.id);
      const w = words(r.label), lw = w.map(x => x.toLowerCase());
      if (w.length < 1 || w.length > L.maxWords) bad.push(`record ${r.id}: label "${r.label}" is ${w.length} word(s), rules 1-${L.maxWords}`);
      if (r.label.length > L.maxChars) bad.push(`record ${r.id}: label "${r.label}" is ${r.label.length} characters, rules ${L.maxChars}`);
      if (/[.!?,;:{}()"]|\d/.test(r.label)) bad.push(`record ${r.id}: label "${r.label}" holds punctuation, a placeholder or a digit (no sentence, no date)`);
      else if (!/^[A-Z][a-z' -]*$/.test(r.label)) bad.push(`record ${r.id}: label "${r.label}" is not one capital and lower case (a capital inside is a name or a title)`);
      for (const x of lw) {
        if (L.era.includes(x)) bad.push(`record ${r.id}: label "${r.label}" has the era word ${x}`);
        if (L.gendered.includes(x)) bad.push(`record ${r.id}: label "${r.label}" has the gendered word ${x}`);
        if (names.has(x)) bad.push(`record ${r.id}: label "${r.label}" has the name ${x} (names come only from bindings)`);
      }
      const key = r.label.toLowerCase();
      if (seenLabel.has(key)) bad.push(`records ${seenLabel.get(key)} and ${r.id} share the label "${r.label}"`); else seenLabel.set(key, r.id);
    }
    give('N-3', 'Labels are 1-2 words, short, no sentence, no digits, era or gendered words or names, and unique in the district', bad, `${recs.length} labels`, `≤ ${L.maxWords} words, ≤ ${L.maxChars} characters`);
  }

  // N-4 tone
  {
    const bad: string[] = [], T = N.tone;
    for (const { r, secret } of recs) {
      for (const x of words(r.label).map(w => w.toLowerCase())) {
        if (T.never.includes(x)) bad.push(`record ${r.id}: label "${r.label}" has "${x}", which tone rules out (civilians are never the payload)`);
        if (T.children.includes(x)) bad.push(`record ${r.id}: label "${r.label}" names a child ("${x}"); a child is never a subject`);
      }
      if (!secret && T.neverPressable.includes(r.kind) && metByOf(needs, r as (typeof C.needs)[number]).includes('pressure')) bad.push(`record ${r.id}: a ${r.kind} record is never pressable`);
    }
    for (const k of T.neverPressable) if (N.kinds[k]?.metBy.includes('pressure')) bad.push(`kind ${k} is never pressable but its metBy has pressure`);
    give('N-4', 'No label names a child or harm; care records are never pressable', bad, `${recs.length} records`, 'tone: GDD §06');
  }

  // N-5 metBy narrows, never widens; every temperament in every row can still reach a record
  {
    const bad: string[] = [], has = { home: true, work: true, family: true, rival: true };
    for (const { r, secret } of recs) {
      if (r.metBy && secret) bad.push(`record ${r.id}: a secret record names no metBy (secret kinds have none)`);
      else if (r.metBy && !secret) { const k = N.kinds[r.kind]; for (const x of r.metBy) if (k && !(k.metBy as readonly string[]).includes(x)) bad.push(`record ${r.id}: metBy ${x} widens its kind ${r.kind} (${k.metBy.join('/')})`); }
    }
    for (const row of cast.rows) {
      for (const t of row.temperaments) {
        const ok = row.positions.some(p => {
          return needsOf(row, p.position).some(n => needFits(rules, n.need, t.temperament, has) && C.needs.some(r => r.area === row.area && r.kind === n.need && metByOf(needs, r).some(x => (rules.temperaments[t.temperament as keyof Rules['temperaments']]?.respondsTo as readonly string[] | undefined)?.includes(x))));
        });
        if (!ok) bad.push(`${row.area}/${row.occupation}: a ${t.temperament} reaches no record the row offers`);
      }
    }
    give('N-5', 'A record\'s approaches are a subset of its kind\'s, and every temperament in each row can reach a record', bad, `${cast.rows.length} rows`, 'narrow, never widen');
  }

  // N-6 no two records differ only in wording
  {
    const bad: string[] = [], seen = new Map<string, string>();
    for (const { r, secret } of recs) {
      const key = JSON.stringify([secret, r.area, r.kind, (r as { size?: string }).size ?? '', r.holder ?? 'beyond', r.subject ?? '', [...(r.metBy ?? [])].sort()]);
      if (seen.has(key)) bad.push(`records ${seen.get(key)} and ${r.id} differ only in wording (same area, kind, size, holder, subject and approaches)`); else seen.set(key, r.id);
    }
    give('N-6', 'No two records of an area differ only in wording; each differs in kind, size, holder, subject or approaches', bad, `${recs.length} records`, 'each differs in play');
  }

  // N-7 no orphans; every kind has a record in every area
  {
    const bad: string[] = [];
    for (const { r } of recs) if (!areas.has(r.area)) bad.push(`record ${r.id}: area ${r.area} is no area of the cast (an orphan)`);
    for (const area of areas) {
      for (const k of Object.keys(N.kinds)) if (!C.needs.some(r => r.area === area && r.kind === k)) bad.push(`area ${area}: no record of need kind ${k}`);
      for (const k of Object.keys(N.secretKinds)) if (!C.secrets.some(r => r.area === area && r.kind === k)) bad.push(`area ${area}: no record of secret kind ${k}`);
    }
    give('N-7', 'No orphan records; every need kind and secret kind has a record in every area', bad, `${areas.size} area(s)`, 'every kind covered');
  }

  // N-8 the Handover's pins
  {
    const bad: string[] = [];
    if (!a.mission) bad.push('no mission to cast');
    else {
      let n = 0;
      a.mission.roles.forEach((role, i) => {
        if (!role.need) return;
        n++;
        const pin = a.handover[i]?.pinned, p = P.find(x => x.id === pin), rec = p?.needRecord ? C.needs.find(r => r.id === p.needRecord) : undefined;
        if (!p) return; // a role the cast cannot play is P-6's and M-2's to name, not this rule's
        if (!rec) bad.push(`${role.id} (${p.id}): no need record`);
        else {
          if (rec.kind !== role.need) bad.push(`${role.id} (${p.id}): record ${rec.id} is of kind ${rec.kind}, the role needs ${role.need}`);
          if (rec.size !== 'standard') bad.push(`${role.id} (${p.id}): record ${rec.id} is size ${rec.size}, the Handover's $40,000 Help is standard`);
          if (rec.holder !== undefined && rec.holder !== 'beyond') bad.push(`${role.id} (${p.id}): record ${rec.id} has holder ${rec.holder}; the Handover's holders are beyond the sheet (no new route opens)`);
        }
      });
      if (!n) bad.push('the mission names no role with a need');
    }
    give('N-8', 'The Handover\'s pinned roles keep their kind, size standard and holder beyond the sheet', bad, 'Inez debt, the driver cash', 'purse sum $80,000 holds');
  }
  return out;
}
