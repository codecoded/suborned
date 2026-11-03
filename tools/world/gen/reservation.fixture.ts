// Brief 075 test fixture: the old Boulevard section on today's plan, for the rules that guard a tram reservation (the lawn track, its quarter-round ends, KR-5).
// The committed plan has no reservation since the 1950s tram street (sections.tramStreet, Sky 2026-10-28): the Boulevard is 20 m with its tracks in setts. These
// rules still hold for any plan that carries a reservation (a pinned plan, a later parkway), so their tests run on this copy: the Boulevard keeps its 20 m but takes
// the old 10 m reservation between two 5 m carriageways (the double track's swept envelope, 3.625 a side, fits inside it), and the plan drops sections.tramStreet so
// its stops are platforms again, not boarding islands. Not a plan anyone ships: a test copy only.
import type { Plan } from '../../plan/plan';

export function reservationPlan(plan: Plan): Plan {
  const p = structuredClone(plan) as any;
  const b = p.roads.find((r: { id: string }) => r.id === 'boulevard'); b.reservation = 10;
  delete p.standards.streetscape.sections.tramStreet;
  return p as Plan;
}
