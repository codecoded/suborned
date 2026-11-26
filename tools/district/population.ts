// Extend the promoted civic graph in its own content-addressed SurrealDB database. The Handover
// keeps its casting; people the player meets elsewhere have ordinary names, routines and social ties.
import type { Person, Place } from '../../contracts/people/people.schema';
import { assignNames } from '../people/names';
import { buildModel } from '../people-graph/model';
import { hashOf, peopleDb, SCHEMA_FILE, type Prepared } from '../people-graph/source';
import { project, type Fixture, type Point, type Venue } from './places';

export const LOCAL = 'p-z-harbour-';
type WalkSpot = (p: Point, v: Venue) => Point;

function tradePost(f: Fixture): Point {
  // Stand at the service side of the counter, outside the customer-facing stock and its walk-grid footprint.
  const a = f.yaw * Math.PI / 180;
  return [f.at[0] + Math.sin(a) * (f.radius + 1.15), f.at[1] + Math.cos(a) * (f.radius + 1.15)];
}

export async function districtPopulation(base: Prepared, venues: Venue[], standing: WalkSpot) {
  const people = structuredClone(base.people),
    original = people.persons.slice(),
    locals: Person[] = [];
  const templates = original.filter(
    (p) =>
      p.work &&
      !p.work.role &&
      !p.secret &&
      ['nothing', 'home', 'work'].includes(base.w.rules.needs.list[p.need]!.requires),
  );
  const groups: Person[][] = [];
  const trades: { worker: Person; role: string }[] = [];
  const residentGeneration = new Map<string, number>();
  for (const [n, v] of venues.entries()) {
    const key = 'street-' + v.id,
      door = key + '-counter',
      unit = key + '-staff';
    const area =
      base.w.plan.areas.find(
        (a) => v.at[0] >= a.rect[0] && v.at[0] <= a.rect[1] && v.at[1] >= a.rect[2] && v.at[1] <= a.rect[3],
      )?.id ?? 'harbour';
    const atPlace = (id: string, kind: Place['kind'], name: string, at: Point) => {
      people.places.push({ id, kind, name, at, area });
      return id;
    };
    atPlace(door, 'stand', v.name, v.at);
    const employer =
      v.style === 'platform'
        ? 'tram'
        : v.style === 'waterfront'
          ? 'harbour'
          : v.style === 'garden'
            ? 'parks'
            : 'shops';
    const shift = { from: 420, to: 1140 };
    people.units.push({
      id: unit,
      plot: key,
      area,
      trade: employer,
      employer,
      name: v.name,
      staffPerShift: v.count,
      hours: [0, 1440],
      shifts: [shift],
    });
    const group: Person[] = [];
    for (let k = 0; k < v.count; k++) {
      const id = LOCAL + v.id + '-' + String(k + 1).padStart(2, '0');
      residentGeneration.set(id, v.generation);
      const t = templates[(n * 17 + k * 7) % templates.length]!;
      // A pair or a queue has distinct standing marks, staggered along the public edge.
      const u = ((k * 0.61803398875 + n * 0.137) % 1) * 2 - 1,
        w = ((k * 0.41421356 + 0.27 * n) % 1) * 2 - 1;
      const counter = v.fixtures.filter(f => f.kit === 'FlowerBarrow' || f.kit === 'ProduceStall')[k-1];
      const occupation = counter ? (counter.kit === 'FlowerBarrow' ? 'flower-seller' : 'fruiterer') : t.position;
      const destination = v.roundEnd ?? v.boarding ?? v;
      const streetStop = project(destination, -u * (v.roundEnd?.span ?? (v.boarding ? 4 : v.span)), ((k % 3) - 1) * v.width * 0.77);
      const home = atPlace(
        id + '-home',
        'home',
        v.name + ' walk-ups',
        standing(counter && v.roundEnd ? streetStop : project(v, u * v.span, w * v.width), v),
      );
      const post = atPlace(
        id + '-post',
        'stand',
        counter ? v.name + (counter.kit === 'FlowerBarrow' ? ' flower barrow' : ' fruit stall')
          : v.roundEnd?.name ?? (v.boarding ? v.name + ' boarding apron' : v.name),
        standing(counter ? tradePost(counter) : streetStop, v),
      );
      const p: Person = {
        id,
        name: 'Local resident',
        tier: 'simulated',
        position: t.position,
        home: { place: home, plot: key },
        work: {
          unit,
          area,
          plot: key,
          employer,
          trade: employer,
          post: 'door',
          position: t.position,
          place: post,
          shift,
        },
        start: { ...t.start },
        temperament: t.temperament,
        need: t.need,
        needWeights: { ...t.needWeights },
        institution: t.institution ? employer : null,
        look: v.roundEnd ? { block: 'job/resident' } : { ...t.look },
        memory: [],
        ties: [],
        routine: {
          variant: 'street-round',
          stops: [
            { place: home, doing: 'home', from: 0, to: 420 },
            { place: post, doing: 'work', from: 420, to: 1140 },
            { place: door, doing: 'evening', from: 1140, to: 1440 },
          ],
        },
      };
      group.push(p);
      locals.push(p);
      if (counter) trades.push({ worker: p, role: occupation });
    }
    for (let k = 0; k < group.length; k++) {
      const a = group[k]!,
        b = group[(k + 1) % group.length]!;
      a.ties.push({ kind: 'workmate', with: b.id });
      b.ties.push({ kind: 'workmate', with: a.id });
    }
    groups.push(group);
  }
  // A regular can relay a request beyond one circle. The stop is at the actual counter during
  // its worker's shift. One resident per venue also calls at a promoted civic workplace.
  function regular(a: Person, b: Person) {
    if (!b.work) return;
    const from = b.work.shift.from,
      until = b.work.shift.to > from ? b.work.shift.to : 1440;
    const minute = Math.max(from, Math.min(750, until - 20));
    a.routine.stops = a.routine.stops.flatMap((s) => {
      if (minute < s.from || minute + 15 > s.to) return [s];
      return [
        { ...s, to: minute },
        { place: b.work!.place, doing: 'meal' as const, from: minute, to: minute + 15 },
        { ...s, from: minute + 15 },
      ].filter((x) => x.to > x.from);
    });
    a.ties.push({ kind: 'regular', with: b.id });
  }
  const generations = [...new Set(venues.map(v => v.generation))].sort((a, b) => a - b);
  for (const generation of generations) {
    const indices = venues.map((v, i) => v.generation === generation ? i : -1).filter(i => i >= 0);
    if (indices.length > 1)
      indices.forEach((n, i) => regular(groups[n]![0]!, groups[indices[(i + 1) % indices.length]!]![1]!));
  }
  groups.forEach((g, n) => {
    regular(g[2]!, templates[(n * 7) % templates.length]!);
  });
  const court = venues.findIndex(v => v.id === 'meridian-court');
  const newsstand = venues.findIndex(v => v.id === 'lantern-papers');
  if (court >= 0 && newsstand >= 0) {
    regular(groups[court]![0]!, groups[newsstand]![1]!);
    regular(groups[newsstand]![3]!, groups[court]![1]!);
  }
  const office = venues.findIndex(v => v.id === 'ferry-landing');
  if (office >= 0) {
    const desk = venues[office]!;
    const platforms = venues.map((v, i) => ({ v, i })).filter(({ v }) => v.style === 'platform');
    platforms.sort((a,b) => Math.hypot(a.v.at[0]-desk.at[0],a.v.at[1]-desk.at[1])
      - Math.hypot(b.v.at[0]-desk.at[0],b.v.at[1]-desk.at[1]));
    const clerk = groups[office]![0]!, porter = groups[office]![1]!;
    const conductor = groups[platforms[0]!.i]![1]!;
    for (const [person, role, uniform] of [[clerk,'booking-clerk','clerk'],[porter,'ferry-porter','attendant'],[conductor,'tram-conductor','attendant']] as const) {
      person.position = role;
      person.work!.position = role;
      const model = templates.find(t => t.position === uniform);
      if (model) person.look = { ...model.look };
    }
    // The clerk and porter already share the desk's workmate tie. The porter's visit to
    // the conductor adds a second introduction, grounded at the actual tram platform.
    regular(porter, conductor);
  }
  const names = base.w.ctx(base.source.cast).names;
  if (!names) throw Error('The city needs the promoted name lists');
  const pool = {
    ...names,
    excluded: { ...names.excluded, full: new Set([...names.excluded.full, ...original.map((p) => p.name)]) },
  };
  // Later openings must keep every person the player already knows in the address book.
  for (const generation of generations) {
    const neighbours = locals.filter(person => residentGeneration.get(person.id) === generation);
    const gaps = assignNames(pool, base.w.rules, neighbours, new Map(), new Map(), 0);
    if (gaps.length) throw Error(gaps.join('\n'));
    for (const person of neighbours) pool.excluded.full.add(person.name);
  }
  // A local taking up a stall keeps their established name, appearance and address-book ID.
  for (const { worker, role } of trades) worker.work!.position = worker.position = role;
  const market = venues.findIndex(v => v.id === 'lantern-market');
  if (court >= 0 && market >= 0) {
    const florist = groups[court]!.find(p => p.position === 'flower-seller');
    const fruiterer = groups[market]!.find(p => p.position === 'fruiterer');
    if (florist && fruiterer) regular(florist, fruiterer);
  }
  people.persons.push(...locals);
  const model = buildModel(people, base.w.rules, base.extras);
  const hash = hashOf(model, await Bun.file(SCHEMA_FILE).text());
  return { ...base, people, model, hash, db: peopleDb(hash), residentGeneration };
}
