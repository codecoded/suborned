// A valid cast for a scope, made by code: what a preview answers for Pulse (no model call), and the fixture the tests expand.
// It is not a good cast (the weights are flat); it is a cast the checker accepts, so a run end to end can be tried offline.
import type { Cast, Row } from '../../contracts/people/cast.schema';
import type { Rules } from './rules';
import { positionsOf } from './rules';
import type { Scope, Unit } from './scope';

const G: Record<string, string[]> = {
  anglo: ['Edith', 'Walter', 'Harriet', 'Oswin', 'Clara', 'Felix', 'Ada', 'Rupert', 'Nell'],
  celtic: ['Fionn', 'Maeve', 'Rhys', 'Siobhan', 'Declan', 'Brenna', 'Cormac', 'Niamh', 'Aled'],
  french: ['Margaux', 'Etienne', 'Odile', 'Luc', 'Solene', 'Gaspard', 'Mireille', 'Anselme', 'Colette'],
  iberian: ['Ines', 'Mateo', 'Lucia', 'Rafael', 'Pilar', 'Joaquin', 'Marisol', 'Teodoro', 'Xavier'],
  italian: ['Chiara', 'Matteo', 'Livia', 'Bruno', 'Fiorella', 'Dario', 'Gianna', 'Tullio', 'Serena'],
  germanic: ['Greta', 'Anselm', 'Liesel', 'Konrad', 'Hedwig', 'Matthias', 'Ilse', 'Dieter', 'Freya'],
  slavic: ['Milena', 'Tomas', 'Zora', 'Radek', 'Katya', 'Bohdan', 'Irena', 'Luka', 'Vesna'],
  greek: ['Eleni', 'Nikos', 'Ariadne', 'Stavros', 'Thea', 'Kosta', 'Melina', 'Dimitri', 'Sofia'],
};
const F: Record<string, string[]> = {
  anglo: ['Ashworth', 'Pemberton', 'Hale', 'Whitlock', 'Carrow', 'Fenwick', 'Thorne', 'Marsh', 'Calloway', 'Dunmore', 'Penrose'],
  celtic: ['Kearney', 'Macrae', 'Doherty', 'Llewellyn', 'Fitzgerald', 'Brennan', 'Gallagher', 'Tierney', 'Morgan', 'Cadogan', 'Rowan'],
  french: ['Delacroix', 'Fontaine', 'Moreau', 'Lefevre', 'Girard', 'Beaumont', 'Roussel', 'Vasseur', 'Duclos', 'Marchand', 'Pelletier'],
  iberian: ['Navarro', 'Ibarra', 'Quintero', 'Salazar', 'Montoya', 'Cardenas', 'Villareal', 'Arroyo', 'Ferrer', 'Lozano', 'Prieto'],
  italian: ['Bellini', 'Ferraro', 'Conti', 'Marchetti', 'Santoro', 'Giordano', 'Rinaldi', 'Moretti', 'Vitale', 'Corsi', 'Galli'],
  germanic: ['Brandt', 'Keller', 'Vogel', 'Lindner', 'Hartmann', 'Reinhardt', 'Sauer', 'Falk', 'Wendt', 'Kessler', 'Obermann'],
  slavic: ['Novak', 'Kowalski', 'Horvat', 'Petrov', 'Svoboda', 'Dvorak', 'Kovac', 'Marek', 'Zelenka', 'Volkov', 'Bartos'],
  greek: ['Papas', 'Kostas', 'Laskaris', 'Vlahos', 'Drakos', 'Mavros', 'Anagnos', 'Zervas', 'Karras', 'Stavrou', 'Raptis'],
};
const pool = (t: Record<string, string[]>) => Object.entries(t).flatMap(([origin, names]) => names.map(name => ({ name, origin })));

/** Position weights and posts per category (a civic employer's roles come from the rules). */
const POSTS: Record<string, Record<string, ['door' | 'inside' | 'bay', number]>> = {
  food: { server: ['door', 5], cook: ['inside', 3] }, kiosk: { attendant: ['door', 3] }, retail: { assistant: ['door', 4] }, service: { operator: ['door', 3] },
  trade: { hand: ['inside', 4] }, nightlife: { server: ['door', 4], door: ['door', 1], entertainer: ['inside', 2] }, cinema: { usher: ['door', 3], projectionist: ['inside', 1] },
  lobby: { porter: ['door', 3] }, market: {},
};
const CIVIC: Record<string, ['door' | 'inside' | 'bay', number]> = { clerk: ['inside', 17], guard: ['door', 1.5], 'official-driver': ['bay', 1], councillor: ['inside', 0.5] };

export function standInCast(scope: Scope, rules: Rules): Cast {
  const needs = Object.keys(rules.needs.list).map(need => ({ need, weight: 1 })), tm = Object.keys(rules.temperaments);
  const ties = [{ kind: 'workmate', share: 0.6 }, { kind: 'regular', share: 0.3 }, { kind: 'family', share: 0.2 }, { kind: 'debtor', share: 0.1 }, { kind: 'rival', share: 0.1 }];
  const worker = (id: string, commute: 'walk' | 'tram', meal: 'on-site' | 'square' | 'none', preShift: 'none' | 'cafe', weight: number) => ({ id, skeleton: 'worker' as const, weight, leaveBeforeMin: [5, 25], commute, meal, preShift, evening: 'home' as const });
  const rows: Row[] = [];
  const seen = new Set<string>();
  for (const u of scope.units as Unit[]) {
    const key = `${u.area}/${u.trade}`; if (seen.has(key)) continue; seen.add(key);
    const positions = u.kind === 'civic'
      ? Object.entries(CIVIC).filter(([r]) => rules.temperamentsBy.roles[r]?.employer === u.employer).map(([position, [post, weight]]) => ({ position, weight, post }))
      : [...Object.entries(POSTS[u.category] ?? {}).filter(([p]) => positionsOf(rules, u.category).includes(p)).map(([position, [post, weight]]) => ({ position, weight, post })), ...(positionsOf(rules, u.category).includes('keeper') ? [{ position: 'keeper', weight: 1, post: 'door' as const }] : [])];
    const routines = u.kind === 'civic'
      ? [worker('office-hours', 'walk', 'on-site', 'none', 5), worker('door-guard', 'walk', 'on-site', 'cafe', 2), worker('from-the-tram', 'tram', 'square', 'none', 3)]
      : [worker('local', 'walk', 'on-site', 'none', 4), worker('from-the-tram', 'tram', 'square', 'none', 2)];
    rows.push({ area: u.area, occupation: u.trade, why: `${u.name}: ${u.staffPerShift} staff per shift over ${u.shifts.length} shift(s) (life.json)`, positions, temperaments: tm.map(temperament => ({ temperament, weight: 1 })), needs, ties, routines });
  }
  for (const a of [...new Set(scope.homes.map(h => h.area))]) {
    rows.push({ area: a, occupation: 'resident', why: 'residents of the home plots near the block', positions: [{ position: 'resident', weight: 1, post: 'door' }], temperaments: tm.map(temperament => ({ temperament, weight: 1 })), needs, ties: [{ kind: 'family', share: 0.5 }, { kind: 'regular', share: 0.4 }, { kind: 'debtor', share: 0.1 }], routines: [{ id: 'errand-and-evening', skeleton: 'resident', weight: 3, leaveBeforeMin: [0, 0], commute: 'walk', meal: 'none', preShift: 'none', evening: 'square' }, { id: 'errand', skeleton: 'resident', weight: 2, leaveBeforeMin: [0, 0], commute: 'walk', meal: 'none', preShift: 'none', evening: 'home' }] });
  }
  rows.sort((a, b) => (`${a.area}/${a.occupation}` < `${b.area}/${b.occupation}` ? -1 : 1));
  return { schema: 'suborned.cast/1', scope: scope.id, rows, names: { given: pool(G), family: pool(F) } };
}
