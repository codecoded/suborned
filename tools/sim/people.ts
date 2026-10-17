// The city's people as the C# engine reads them (brief 011 A, N12; Keel): sim/data/handover.people.json. Read from the people file
// `bun run people` writes from the promoted cast (out/people/<plan>.people.json), never from the expander's code, as Aster asked
// while Pulse changes the day-building. Block frame, integer centimetres; routine times in mission seconds from 0:00 (mission.json
// clock.startsAt). A stop is out on the street unless the person is at home or working inside (people.schema.ts post "inside");
// "beyond" is off the sheet. The four people the Handover casts by role stay in the file; the engine plays them by their role.
// Run: bun run sim:people (writes the file); bun test tools/sim (checks it against the people file, when there is one).
// Open needs (brief 017, phase 1; suborned.sim.people/2): each person carries their need's record, its holder ("beyond": nobody on the sheet;
// else a person's id, bound in phase 2) and its size; `needs` holds the catalogue's records (kind, label, size) and the sizes the need kinds
// name (contracts/people/needs.rules.json), so the engine checks every record against its kind (unknown = reject) and the HUD reads the label.
// The people file names the hash of the kinds and the catalogue it was made with; a catalogue that moved since is refused here.
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';
import { catalogueFileOf, NEEDS_RULES_FILE, parseNeeds } from '../people/needs';

const root = join(import.meta.dir, '..', '..');
export const PEOPLE_OUT = 'sim/data/handover.people.json';
export const PEOPLE_IN = 'out/people/harbour-district.people.json';
const PLAN = 'contracts/world/harbour-district.plan.json';
const MISSION = 'contracts/missions/handover/mission.json';

type V = [number, number];
type InPerson = {
  id: string; name: string; tier: string; temperament: string; need: string; home: { plot: string; place: string };
  start: { bravery: number; resentment: number; fear: number; loyalty: number };
  work: { place: string; post: 'door' | 'inside' | 'bay'; position?: string; role?: string; employer: string } | null;
  routine: { variant: string; stops: { place: string; doing: string; from: number; to: number }[] };
  ties: { kind: string; with: string }[];
  needRecord?: string;
};

export function buildPeople(src: { people: string; fixture?: string } = { people: PEOPLE_IN }): string {
  const peopleText = readFileSync(join(root, src.people), 'utf8');
  const plan = JSON.parse(readFileSync(join(root, PLAN), 'utf8'));
  const mission = JSON.parse(readFileSync(join(root, MISSION), 'utf8'));
  const people = JSON.parse(peopleText) as { schema: string; scope: string; inputs: { needs?: string }; places: { id: string; at: V }[]; persons: InPerson[] };
  if (people.schema !== 'suborned.people/1') throw new Error(`${PEOPLE_IN} is ${people.schema}, expected suborned.people/1`);
  // the need kinds and the catalogue the people were made with (brief 017): the same texts, or the file is stale
  const needsFiles = [NEEDS_RULES_FILE, catalogueFileOf(plan.id)];
  const [needsRulesText, catalogueText] = needsFiles.map((f) => readFileSync(join(root, f), 'utf8')) as [string, string];
  const needsSha = createHash('sha256').update(`${needsRulesText}\n${catalogueText}`).digest('hex');
  if (people.inputs.needs !== needsSha) throw new Error(`${src.people} was made ${people.inputs.needs ? 'with other need kinds or another catalogue' : 'without the need kinds and a catalogue'} than ${needsFiles.join(' and ')} today (bun run people)`);
  const needs = parseNeeds({ rules: needsRulesText, catalogue: catalogueText });
  const records = new Map(needs.catalogue.needs.map((r) => [r.id, r]));
  const o: V = plan.civic.blockToPlan.origin, ax: V = plan.civic.blockToPlan.x, ay: V = plan.civic.blockToPlan.y;
  const det = ax[0] * ay[1] - ax[1] * ay[0];
  const toBlockCm = (p: V) => { const dx = p[0] - o[0], dy = p[1] - o[1]; return [Math.round(((dx * ay[1] - dy * ay[0]) / det) * 100), Math.round(((ax[0] * dy - ax[1] * dx) / det) * 100)]; };
  const [h, m, s] = String(mission.clock.startsAt).split(':').map(Number);
  const startsAt = h! * 3600 + m! * 60 + (s ?? 0);
  const places: Record<string, number[]> = {};
  for (const p of [...people.places].sort((a, b) => (a.id < b.id ? -1 : 1))) places[p.id] = toBlockCm(p.at);
  const persons = [...people.persons].filter((p) => p.tier === 'simulated').sort((a, b) => (a.id < b.id ? -1 : 1)).map((p) => {
    const stops = p.routine.stops.map((st) => {
      if (st.place !== 'beyond' && !places[st.place]) throw new Error(`${p.id}: place ${st.place} is not in the people file`);
      const inside = st.doing === 'home' || (p.work?.post === 'inside' && st.place === p.work.place && (st.doing === 'work' || st.doing === 'meal'));
      return [st.place, st.from * 60 - startsAt, st.to * 60 - startsAt, inside || st.place === 'beyond' ? 0 : 1];
    });
    const rec = p.needRecord ? records.get(p.needRecord) : undefined;
    if (!rec || rec.kind !== p.need) throw new Error(`${p.id}: need record ${p.needRecord ?? '(none)'} is not a ${p.need} record of ${needsFiles[1]}`);
    if ((rec.holder ?? 'beyond') !== 'beyond') throw new Error(`${p.id}: record ${rec.id} names holder ${rec.holder}; binding a holder to a person is phase 2 (brief 017)`);
    return {
      id: p.id, name: p.name, job: p.work ? (p.work.role ?? p.work.position ?? 'staff') : 'resident', temperament: p.temperament, need: p.need,
      home: p.home.plot !== 'beyond' && p.home.place !== 'beyond' ? 1 : 0, work: p.work ? 1 : 0, // what a replacement need may require (rulebook need.replaced)
      record: rec.id, size: rec.size, holder: 'beyond', // phase 1: no record names a holder on the sheet (checked above)
      start: [p.start.bravery, p.start.resentment, p.start.fear, p.start.loyalty],
      ties: p.ties.map((t) => [t.kind, t.with]),
      stops,
    };
  });
  const out = {
    schema: 'suborned.sim.people/2',
    from: [src.people, src.fixture ? `FIXTURE: ${src.fixture}` : `written by bun run people from contracts/people/${plan.id}.cast.json (the promoted cast)`, PLAN, MISSION, ...needsFiles],
    scope: people.scope,
    frame: 'block frame, integer centimetres; stop = [place, from, to, out]: from and to in mission seconds (0 = mission.json clock.startsAt), out 1 = on the street, 0 = at home, working inside, or beyond the sheet',
    startsAt,
    places,
    needs: {
      sizes: Object.keys(needs.rules.sizes).sort(),
      records: Object.fromEntries([...needs.catalogue.needs].sort((a, b) => (a.id < b.id ? -1 : 1)).map((r) => [r.id, [r.kind, r.label, r.size]])),
      frame: 'record = [kind, label, size]: the catalogue\'s records (brief 017); a person\'s record is one of them, of their need\'s kind; holder "beyond" = nobody on the sheet (phase 1)',
    },
    people: persons,
  };
  const flat = (x: unknown): boolean => typeof x === 'number' || typeof x === 'string' || (Array.isArray(x) && x.every(flat));
  const pretty = (v: unknown, ind = ''): string => {
    if (Array.isArray(v)) return v.every(flat) ? JSON.stringify(v) : `[\n${v.map((x) => ind + ' ' + pretty(x, ind + ' ')).join(',\n')}\n${ind}]`;
    if (v && typeof v === 'object') return `{\n${Object.entries(v as Record<string, unknown>).map(([k, x]) => `${ind} ${JSON.stringify(k)}: ${pretty(x, ind + ' ')}`).join(',\n')}\n${ind}}`;
    return JSON.stringify(v);
  };
  return pretty(out) + '\n';
}

export const havePeople = () => existsSync(join(root, PEOPLE_IN));

// Written together with the site by tools/sim/site.ts (bun run sim:site), so the cast and the crowd come from one people file.
