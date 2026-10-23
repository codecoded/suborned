import { buildGuide, gddNames, gddNamesFromFiles, gddPair, hash, pick, readerFor, readerFromFiles, TEXT_FILE, type Read, type Sha } from './guide';
import type { Brief, Line } from './types';

/** The Inez chain (brief 054): one existing Handover scene per conversation. Scene numbers index text.json scenes.items. */
const CHAIN: { scene: number; verb: string; temperament: string | null; outcome?: string }[] = [
  { scene: 0, verb: 'watch', temperament: null },
  ...['idealist', 'mercenary', 'fearful', 'loyal'].map((temperament, i) => ({ scene: 4 + i, verb: 'listen', temperament })),
  { scene: 29, verb: 'help', temperament: 'idealist', outcome: 'turned' }, { scene: 33, verb: 'help', temperament: 'mercenary', outcome: 'turned' },
  { scene: 36, verb: 'help', temperament: 'fearful', outcome: 'turned' }, { scene: 39, verb: 'help', temperament: 'loyal', outcome: 'turned' },
  { scene: 42, verb: 'pressure', temperament: 'idealist', outcome: 'turned' }, { scene: 44, verb: 'pressure', temperament: 'mercenary', outcome: 'turned' },
  { scene: 45, verb: 'pressure', temperament: 'fearful', outcome: 'turned' }, { scene: 46, verb: 'pressure', temperament: 'loyal', outcome: 'suspicious' },
  ...['idealist', 'mercenary', 'fearful', 'loyal'].map((temperament, i) => ({ scene: 90 + i, verb: 'call', temperament })),
];
const PLAN = 'anything about the cell\'s plan: Vale, the Office door, the Take or the getaway';

export function buildBriefs(read: Read, names: string[], sha?: Sha): Brief[] {
  const guide = buildGuide(read, names, sha), gdd = read(gddPair(names).agent.path), text = JSON.parse(read(TEXT_FILE));
  // Act seconds come from the GDD (§04 approaches, §07 route A); each bubble takes guide.bubble.seconds (§10).
  const [watch] = pick(gdd, /routine known \(Watch (\d+) s/, 'the Watch time (§04)').map(Number);
  const [listen, help] = pick(gdd, /\| Help \(clear debt\) \| need known \(Listen (\d+) s\) \| [\d,]+ \| (\d+) s/, 'the Listen and Help times (§04)').map(Number);
  const [pressure] = pick(gdd, /\| Pressure \| need known \| \d+ \| (\d+) s/, 'the Pressure time (§04)').map(Number);
  const [, stays] = pick(gdd, /guard walks (\d+) s, stays (\d+) s/, 'the guard\'s stay on the call (§07)').map(Number);
  const seconds: Record<string, number> = { watch: watch!, listen: listen!, help: help!, pressure: pressure!, call: stays! };
  const temperament = (t: string) => {
    const name = t[0]!.toUpperCase() + t.slice(1);
    const [respondsTo, hero, menace] = pick(gdd, new RegExp(`\\| ${name}(?: \\(institution\\))? \\| ([^|]+) \\| ([^|]+) \\| ([^|]+) \\|`), `the ${name} row (§04)`)
      .map(x => x.trim());
    // §04 scales: the Loyal temperament is loyalty to an employer or the civic order, not to a person.
    const loyal = t === 'loyal' ? `; ${pick(gdd, /(loyalty to an employer or the civic order is the Loyal temperament)/, 'the Loyal definition (§04)')[0]}` : '';
    return `${t} (GDD §04: responds to ${respondsTo}; with a Hero cell: ${hero}; with a Menace cell: ${menace}${loyal})`;
  };
  const range = (s: number): [number, number] => { const max = Math.floor(s / guide.bubble.seconds); return [Math.ceil(max * 0.6), max]; };
  const half = ([min, max]: [number, number]): [number, number] => [Math.ceil(min / 2), Math.floor(max / 2)];

  return CHAIN.map(({ scene: index, verb, temperament: t, outcome }) => {
    const scene = text.scenes.items[index];
    if (!scene || scene.about !== 'waitress' || scene.verb !== verb || (scene.temperament ?? null) !== t || (outcome && scene.outcome !== outcome))
      throw new Error(`Scene ${index} is no longer Inez's ${verb}${t ? ` (${t})` : ''}; update the chain`);
    const lines = range(seconds[verb]!);
    const current: Line[] = scene.variants[0].beats.map((b: { speaker: string; line?: string; ref?: string }) =>
      ({ speaker: b.speaker, line: b.line ?? `(the ${b.ref} line from the dialogue table)` }));
    const base = { id: `inez/${verb}/${t ?? 'any'}`, scene: index, verb, temperament: t, outcome: scene.outcome ?? null, seconds: seconds[verb]!, lines,
      result: scene.result ?? null, current };
    const shows = t ? [`her temperament shows in what she will and won't take: ${temperament(t)}`] : [];
    if (verb === 'watch') return { ...base, kind: 'overheard', cast: ['waitress', 'guard', 'keeper', 'customer'],
      parts: [{ with: 'keeper, customers and the guard', reveals: [], lines }],
      reveals: ['her routine: she works the café terrace all evening, the window tables and the door, and can see the Office steps',
        'the door guard stops at her café for coffee before his shift ends'],
      withholds: ['her debt or any money trouble', 'the affair: to an onlooker he is a regular and she is his waitress', 'her temperament', PLAN],
      leadsTo: 'Listen, close by, to learn who she is. Knowing her routine also opens Pay.' };
    if (verb === 'listen') return { ...base, kind: 'overheard', cast: ['waitress', 'guard', 'customer'],
      parts: [
        { with: 'guard', reveals: ['Inez and the door guard are lovers, kept quiet from the café and the Office', 'he comes to the café before his shift ends to see her'], lines: half(lines) },
        { with: 'customer', reveals: ['Inez owes a lender and is struggling to pay; the lender\'s man comes round'], lines: half(lines) },
      ],
      reveals: ['the affair with the door guard (her secret)', 'her debt to the lender (her need)', ...shows], withholds: [PLAN],
      leadsTo: 'Two levers: Help (the Fixer clears her debt) or Pressure (the Fixer threatens to expose the affair). Turned either way, she can call the guard off his door.' };
    if (verb === 'help') return { ...base, kind: 'approach', cast: ['fixer', 'waitress'], parts: [{ with: 'fixer', reveals: [], lines }],
      reveals: ['the Fixer knows about her debt to the lender and offers to clear it, asking only a favour later', ...shows, 'she accepts and agrees to help when the cell calls on her'],
      withholds: [PLAN, 'the affair (that is Pressure\'s lever)'],
      leadsTo: 'Inez is turned: the Fixer can now Call her to pull the guard off the door.' };
    if (verb === 'pressure') return { ...base, kind: 'approach', cast: ['fixer', 'waitress'], parts: [{ with: 'fixer', reveals: [], lines }],
      reveals: ['the Fixer lets her know he knows about her and the door guard, and what the Office would make of it', ...shows,
        scene.outcome === 'turned' ? 'she gives in, frightened and resentful, and will do as she is asked when called' : 'she refuses and makes plain she will report the approach to the Office'],
      withholds: [PLAN],
      leadsTo: scene.outcome === 'turned' ? 'Inez is turned, under threat: the Fixer can now Call her to pull the guard off the door, but she resents it.'
        : 'Her report raises Alert when it lands; the front door gets harder, and the guard or the pickup are left.' };
    return { ...base, kind: 'call', cast: ['fixer', 'waitress', 'guard'], parts: [
      { with: 'fixer', reveals: ['the Fixer telephones Inez at the café: now is the time'], lines: [2, 4] },
      { with: 'guard', reveals: ['she calls the guard over from his door', 'a spilled coffee keeps him at her table'], lines: [lines[0] - 2, lines[1] - 2] },
    ], reveals: ['the guard leaves his door for Inez', ...shows], withholds: [PLAN + ' (the guard never learns why she called)'],
      leadsTo: 'The door is unguarded for about two minutes while he stays: the Enforcer goes in.' };
  });
}

export const loadBriefs = (base: string) => buildBriefs(readerFor(base), gddNames(base));
export const briefsFromFiles = (files: Record<string, string>, shas: Record<string, string>) =>
  buildBriefs(readerFromFiles(files), gddNamesFromFiles(shas), file => shas[file] ?? hash(readerFromFiles(files)(file)));

/** Today's scene as the player sees it, with `ref` beats looked up in the dialogue table. For the report only: briefs keep what the writer was shown. */
export function todaysLines(read: Read, scene: number): Line[] {
  const text = JSON.parse(read(TEXT_FILE)), s = text.scenes.items[scene];
  const fits = (value: unknown, want: unknown) => value === undefined || value === want;
  return s.variants[0].beats.map((b: { speaker: string; line?: string; ref?: string }) => {
    if (b.line) return { speaker: b.speaker, line: b.line };
    const row = text.dialogue.find((d: Record<string, unknown>) => d.event === b.ref && d.speaker === b.speaker
      && fits(d.verb, s.verb) && fits(d.outcome, s.outcome) && fits(d.temperament, s.temperament));
    return { speaker: b.speaker, line: row?.lines?.[0] ?? `(${b.ref})` };
  });
}
