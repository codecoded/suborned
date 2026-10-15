// What Fable and Keel are shown about the scope besides the retrieved passages: what to write (the cells, scenes or lexicon words the checker will ask for), the slots and
// limits, who the people are in this district (so qualified cells can fit the needs and secrets that occur), the lexicon the slots fill from, and the Handover's
// hand-written lines as the bar to match or beat. Derived from the rules and the people, never typed here.
import type { Voices } from '../../../contracts/dialogue/voices.schema';
import type { TextFile } from '../../../contracts/missions/handover/text.schema';
import type { Parsed } from '../../dialogue/inputs';
import { referenceOf } from '../../dialogue/fixture';
import { debriefFields, groupOf, groupTemperaments, lexiconNeeds } from '../../dialogue/rules';
import { keyOf, requiredCells, requiredScenes, sceneKeyOf } from '../../dialogue/situations';

export type View = { scope: string; p: Parsed; text: Pick<TextFile, 'dialogue' | 'scenes'>; context: Voices[] };
const j = (x: unknown) => JSON.stringify(x);

export function limitsView(v: View): string {
  const { dr } = v.p;
  return `Limits (JSON): ${j(dr.limits)}\nSlots, by where a template stands (JSON; a slot is written {name} and filled per person; the cell slot \`address\` is one of the group's own terms, picked per person): ${j({ cell: dr.slots.cell, reads: dr.slots.reads, scene: dr.slots.scene, summons: dr.slots.summons })}\n`
    + `Need slots (${dr.slots.needSlots.join(', ')}) may stand only in a person's reply to ${dr.slots.needIn.join(', ')} (and in scenes and the Scout's read-outs): a cold Ask or a Pay never surfaces the need (§04: Help and Pressure are what a known need unlocks).\n`
    + `Tone (JSON): ${j({ world: dr.tone.world, avoid: dr.tone.avoid, strongProfanity: dr.tone.profanity, rating: dr.tone.rating, genderedWordsNoLineUses: dr.tone.gendered, signalWordsNoPersonSays: dr.tone.signal, namesAwaitingSky: dr.tone.pending, civiliansNeverThePayload: dr.tone.payload })}`;
}

const tally = (xs: string[]) => { const m = new Map<string, number>(); for (const x of xs) m.set(x, (m.get(x) ?? 0) + 1); return [...m].sort((a, b) => b[1] - a[1] || (a[0] < b[0] ? -1 : 1)).map(([k, n]) => `${k} ${n}`).join(', '); };

/** Who is in a group in this district: their positions, temperaments, needs and secrets, as counts. */
export function whoView(v: View, group: string): string {
  const { dr, people } = v.p, mine = people.persons.filter(x => groupOf(dr, x.position) === group);
  return `${mine.length} people: positions ${tally(mine.map(x => x.position))}; temperaments ${tally(mine.map(x => x.temperament))}; needs by temperament ${tally(mine.map(x => `${x.temperament}×${x.need}`)).split(', ').slice(0, 24).join(', ')}; secrets ${tally(mine.map(x => x.secret ?? 'none'))}`;
}

function cellsView(v: View, group: string): string {
  const { dr, rb, prules } = v.p, req = requiredCells(dr, rb, prules, group), by = new Map<string, string[]>();
  for (const k of req) { const t = k.temperament ?? 'any'; by.set(t, [...(by.get(t) ?? []), keyOf({ ...k, temperament: undefined })]); }
  return [...by].map(([t, ks]) => `  ${t === 'any' ? 'one voice (no temperament)' : t}: ${ks.join(', ')}`).join('\n');
}

/** The Handover's lines for the voice that stands for this group, as examples of the bar (match or beat; never copy a line). */
function exemplarsView(v: View, group: string): string {
  const ref = referenceOf(v.text as TextFile, group);
  if (!ref?.cells.length) return '  (the Handover has no hand-written lines for this voice; the bar is the other groups\' care)';
  const pick = ref.cells.filter(c => ['reply/ask/refused', 'reply/help/turned', 'reply/pressure/turned', 'reply/pay/turned', 'reply/help/not-yet', 'confirm/call', 'questioned/gives', 'questioned/lies', 'saw-gunfire'].some(p => keyOf({ event: c.event, verb: c.verb, outcome: c.outcome }).startsWith(p))).slice(0, 16);
  return pick.map(c => `  ${keyOf({ event: c.event, verb: c.verb, outcome: c.outcome, temperament: c.temperament })}: ${c.lines.map(l => `"${l}"`).join(' | ')}`).join('\n');
}

function lexiconContext(v: View, groups: string[]): string {
  const L = v.context.find(x => x.lexicon)?.lexicon;
  if (!L) return 'No lexicon is pinned (the stand-in is not words).';
  const covers = new Set(groups.flatMap(g => v.p.dr.groups[g]?.covers ?? []));
  return `The lexicon your slots fill from (pinned, promoted; JSON):\n  needs: ${j(L.needs)}\n  positions of these groups: ${j(L.positions.filter(x => covers.has(x.position)))}`;
}

function sceneExemplars(v: View): string {
  const sc = (v.text as TextFile).scenes.items.filter(s => s.kind === 'overheard' && s.verb === 'listen' && s.about === 'waitress' && ['fearful', 'idealist'].includes(s.temperament ?? '')).slice(0, 2);
  const call = (v.text as TextFile).scenes.items.filter(s => s.kind === 'call' && s.about === 'waitress' && ['fearful', 'mercenary'].includes(s.temperament ?? '')).slice(0, 2);
  const show = (s: (typeof sc)[number]) => `  ${s.kind}/${s.verb}/${s.temperament}: ${s.variants[0]!.beats.map(b => ('line' in b ? `${b.speaker === 'waitress' ? 'focus' : b.speaker === 'guard' ? 'other' : b.speaker}: "${b.line}"` : '')).join(' / ')}`;
  return [...sc, ...call].map(show).join('\n');
}

export function scopeView(v: View): string {
  const { dr, rb, prules, people } = v.p, s = dr.scopes[v.scope]!;
  const head = `Scope \`${v.scope}\`: ${s.why}\nBlocks to write: ${s.blocks.join(', ')}${s.groups ? `; groups ${s.groups.join(', ')}` : ''}. Return \`voices\` (schema suborned.voices/1, scope "${v.scope}"); leave the other blocks empty (\`groups\`/\`scenes\` as [], no \`lexicon\`).\n${limitsView(v)}\n`;
  const parts: string[] = [head];
  if (s.blocks.includes('groups')) {
    for (const g of s.groups!) {
      const temps = groupTemperaments(dr, prules, g), cov = dr.groups[g]!;
      parts.push(`\n== Group ${g}: ${cov.why}\n  covers (positions and roles; \`jobs\` and nouns for each are in the lexicon): ${cov.covers.join(', ')}\n  temperaments: ${temps.join(', ')}${cov.without?.length ? `\n  slots this group may NOT use (some of its people lack what fills them): ${cov.without.join(', ')}` : ''}\n  who they are here: ${whoView(v, g)}\n  one plain cell for each of these (event/verb/outcome, per temperament; every cell 2–4 variants, three is the norm):\n${cellsView(v, g)}\n  also: \`address\` (${dr.limits.addressTerms.min}–${dr.limits.addressTerms.max} single words: how this group addresses a stranger), \`voice\` (one line), \`reads.watch\` (2–4 lines of the Scout after Watching one of them), and ONE Watch scene in \`scenes\` ({"verb":"watch","group":"${g}", 2–3 variants of 3–6 beats; who: focus, bystander or scout}).\n  optional qualified cells (at most ${dr.limits.qualifiedCellsPerGroup} for the group): a cell with \`need\` (a need of the rules) or \`secret\` (affair or record), only on a reply or on questioning, where knowing it changes what this person says (a debtor's Help-turned is not a tenant's). Each needs its plain cell too.\n  the Handover's own lines for this kind of person (the bar to match or beat; write your own):\n${exemplarsView(v, g)}\n  ${lexiconContext(v, [g])}`);
    }
  }
  if (s.blocks.includes('scenes')) {
    const need = requiredScenes(prules).map(sceneKeyOf);
    const ties = Object.entries(prules.ties.kinds).map(([k, t]) => `${k} (${t.direction}${dr.listen.sides[k] ? `: ${dr.listen.sides[k]}` : ''})`).join('; ');
    const lovers = people.persons.filter(x => x.ties.some(t => t.kind === 'lover')).length, tiesN = tally(people.persons.flatMap(x => x.ties.map(t => t.kind)));
    parts.push(`\n== Scenes: ${need.length} scenes, one for each key (verb/tie/group/temperament; "-" = absent): ${need.join(', ')}.\n  Each has 2–3 variants of 3–6 beats; a beat is {who, line}. A listen or call scene is between the focus (the person overheard, in the temperament named) and \`other\` (their partner on the tie kind, named by {other}); tie "none": the focus talks with a \`bystander\` (an unnamed colleague, customer or passer-by) and may not use {other} or {other_noun}. The Scout speaks only in a Watch (the lovers' watch: scout, focus, other). Slots: ${dr.slots.scene.join(', ')}.\n  Tie kinds and who is who: ${ties}.\n  In this district: ties held ${tiesN}; ${lovers} people hold a lover tie (the affair is their secret: nobody in the city is told). A scene never shows the cell; the people do not know they are overheard.\n  The Learned line (what the player now knows) is written by the rules, not you: it says the need and temperament, and for a lover the secret. Say it through the scene, with tact: a secret worth keeping, nothing explicit.\n  the Handover's own scenes (the bar; write your own):\n${sceneExemplars(v)}\n  ${lexiconContext(v, Object.keys(dr.groups))}`);
  }
  if (s.blocks.includes('lexicon')) {
    const fields = debriefFields(rb), byGroup = Object.entries(dr.groups).map(([g, x]) => `${g}: ${x.covers.join(', ')} (${x.why})`).join('\n    ');
    const reads = (v.text as TextFile).dialogue.filter(e => e.event === 'read' && e.verb === 'listen' && e.speaker === 'scout').slice(0, 5).map(e => `${e.temperament ?? ''}: ${e.lines.map(l => `"${l}"`).join(' | ')}`).join('\n    ');
    const summon = (v.text as TextFile).dialogue.filter(e => e.event === 'summon-refused' && (e.outcome === 'summon-busy' || e.outcome === 'summon-too-far')).map(e => `${e.speaker}/${e.outcome}: ${e.lines.map(l => `"${l}"`).join(' | ')}`).join('\n    ');
    parts.push(`\n== Lexicon, one of each:\n  needs (${lexiconNeeds(prules).length}): ${lexiconNeeds(prules).join(', ')}: for each, \`noun\` ("debt"), \`own\` ("my debt": what a person says) and \`met\` (a clause: "the debt is gone"); lower case, no article rule but sense.\n  positions (${[...new Set(Object.values(dr.groups).flatMap(g => g.covers))].length}), by voice group:\n    ${byGroup}\n    for each: \`job\` (after "I": "pour the coffee") and \`noun\` (what the cell calls them, with its article: "the server"). A position is a post, never a person: no names.\n  reads: for each temperament (${Object.keys(prules.temperaments).join(', ')}) 2–4 lines of the Scout after a Listen ({need} {own} {met} allowed): terse, observant, never warm, and they may name the temperament in words ("Proud. Loyal to the post."). The Handover's:\n    ${reads}\n  debrief: one set of 2–4 lines for each rulebook debrief id; a line must carry every field of its id as a {slot} (the cause is what the debrief reports), at most ${dr.limits.debriefMax} characters with each field at its widest ${j(dr.lexicon.debriefFill)}. Ids and fields (JSON): ${j(fields)}. Each line reports with its cause, plainly; the cell's voice, never a person's.\n  summons (${dr.lexicon.summonKeys.length} keys: ${dr.lexicon.summonKeys.join(', ')}): the lovers' call. For each key \`caller\` (the turned contact who called, telling the Fixer why the visit will not happen, or in \`recalled\`, sending the visitor back) and \`summoned\` (the one called, saying it); 2–3 lines each; slots {other} (the lover's given name) and {other_noun}. The keys are the engine's refusal codes: busy, came once, alarm up (alert), saw violence, too far, off the street, no visit; and recalled: the alarm is up mid-visit. The Handover's:\n    ${summon}`);
  }
  return parts.join('\n') + `\n\nSeed for picks at play time: the reference pickIndex; you write ${s.blocks.join(' + ')} only. The checker (rules D-0..D-12) expands your tables for all ${people.persons.length} people of the district and checks every filled line.`;
}

/** A small sample of what the tables expand to, for Keel: per group and temperament, one person's lines on a few situations, so slots and voices can be read as the player will. */
export function sampleView(lines: import('../../../contracts/dialogue/voices.schema').Lines | undefined, n = 8): string {
  if (!lines?.persons.length) return '(nothing expanded this round)';
  const seen = new Set<string>(), out: string[] = [];
  for (const p of lines.persons) {
    const k = `${p.group}/${p.temperament}`;
    if (seen.has(k) || out.length >= n * 3) continue;
    seen.add(k);
    const pick = (e: string, verb?: string, outcome?: string) => p.entries.find(x => x.speaker === 'person' && x.event === e && (verb === undefined || x.verb === verb) && (outcome === undefined || x.outcome === outcome));
    const rows = [pick('reply', 'help', 'turned'), pick('reply', 'pressure', 'turned') ?? pick('reply', 'ask', 'refused'), pick('confirm', 'call'), pick('questioned', undefined, 'gives')].filter(Boolean);
    out.push(`${p.id} ${p.group}/${p.temperament}, need ${p.need}${p.secret ? `, secret ${p.secret}` : ''}, says "${p.address}":\n${rows.map(e => `    ${[e!.event, e!.verb, e!.outcome].filter(Boolean).join('/')}: ${e!.lines.map(l => `"${l}"`).join(' | ')}`).join('\n')}`);
  }
  const sc = lines.persons.flatMap(p => p.scenes.map(s => ({ p, s }))).filter(x => x.s.verb === 'listen').slice(0, 3);
  out.push(...sc.map(({ p, s }) => `scene listen/${s.tie} on ${p.id} (${p.temperament}): ${s.variants[0]!.beats.map(b => `${b.speaker === p.id ? 'focus' : b.speaker}: "${b.line}"`).join(' / ')}\n    ${s.result}`));
  return out.join('\n');
}
