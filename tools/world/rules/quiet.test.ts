// R-HIDE, R-STASH, R-REPORT, R-DOCK on the live world (brief 013 C): each passes on the world the generator makes and fails on one thing put back, on a copy of it.
// Also the placement rules themselves: one container per service door where the street rules let one stand, deterministic, off the walking zone, on the side away from the camera.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { ctxOf, generated } from '../../plan/world-ctx';
import { cameraDir, QUIET } from '../civic/quiet';
import { quietRules } from './quiet';

setDefaultTimeout(120000);
const root = resolve(import.meta.dir, '../../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan: Plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block)), base = generated(plan, d, civ);
/** The rules on a clone of the live world with `mutate` applied: id → detail of every failing rule. */
const failing = (mutate: (w: World) => void = () => {}): Record<string, string> => {
  const w = structuredClone(base.w) as World;
  mutate(w);
  return Object.fromEntries(quietRules(ctxOf(plan, d, civ, { w, gen: base.gen })).filter(c => !c.ok).map(c => [c.id, c.detail ?? '']));
};
const t = base.w.takedown!;

describe('the takedown places of the civic block', () => {
  test('the live world passes R-HIDE, R-STASH, R-REPORT and R-DOCK', () => {
    expect(Object.keys(failing())).toEqual([]);
  });

  test('every door of the block has its kind; the two service doors are the Office\'s and the café\'s', () => {
    expect(t.entrances.map(e => `${e.id} ${e.kind}`)).toEqual(['cafe-entrance public', 'office-entrance public', 'official-entrance official', 'cafe-kitchen-door service', 'office-service-door service']);
    expect(civ.entrances.map(e => e.id)).toEqual(['cafe-entrance', 'office-entrance', 'official-entrance']); // the cast, the crowd and the people tools see public and official doors only
    expect(civ.serviceDoors.map(e => e.id)).toEqual(['cafe-kitchen-door', 'office-service-door']);
    expect(civ.walkFrom.length).toBe(5); // the checker walks from every door, so a stash nobody can walk to fails W-4 / W-5
  });

  test('one Dumpster stands beside the Office service door, on the side away from the play camera, against the wall; the café\'s kitchen door says why it has none', () => {
    const boxes = base.w.street.filter(s => s.stash), door = t.entrances.find(e => e.id === 'office-service-door')!;
    expect(boxes.map(s => s.id)).toEqual(['street/civic/stash/office-service-door']);
    expect(boxes[0]).toMatchObject({ kind: 'dumpster', stash: { holds: 1 }, sight: 'blocks', source: 'procedural' }); // a candidate piece is never drawn: Vela adopts the Dumpster, the item then takes its kit ref
    const c = boxes[0]!, cam = cameraDir(QUIET.cameraYaw), side = (c.at[0] - door.at[0]) * cam[0] + (c.at[1] - door.at[1]) * cam[1];
    expect(side).toBeLessThan(0); // away from the camera, so it does not stand between the camera and the door
    expect(Math.hypot(c.at[0] - door.at[0], c.at[1] - door.at[1])).toBeGreaterThan(QUIET.doorHalf + QUIET.doorClear); // clear of the jamb
    const bare = t.stashes.find(s => s.id === 'stash/cafe-kitchen-door/door')!;
    expect(bare.bare).toMatch(/SF-1 clear walking zone/);
    expect(t.stashes.filter(s => s.kind === 'container').map(s => s.of)).toEqual(['office-service-door']);
  });

  test('the report points are the three staffed doors, the street\'s own booths in the window and the call boxes the rule placed; the police posts are origins, not report points', () => {
    expect(t.report.filter(r => r.kind === 'door').map(r => r.door)).toEqual(['cafe-entrance', 'office-entrance', 'official-entrance']);
    expect(t.report.some(r => /police-post/.test(r.id))).toBe(false);
    const placed = t.report.filter(r => /^street\/civic\/call-box-\d+$/.test(r.id));
    expect(placed.length).toBeGreaterThanOrEqual(3);
    expect(placed.length).toBeLessThanOrEqual(8); // Pulse measured 7 from the doors alone; the street's own booths take some
    expect(t.origins.filter(o => o.kind === 'dock').map(o => o.id)).toEqual(['street/records-square/civic-robot-dock-315']);
    expect(t.origins.filter(o => o.door).map(o => o.door)).toEqual(['office-entrance', 'official-entrance']); // the Records Office's doors are posts
    expect(t.coverage.report.within).toBe(1000);
    expect(t.coverage.origin.within).toBe(1000);
    expect(t.coverage.report.baselineWithin).toBeLessThan(1000); // the doors alone leave ground out of reach (Pulse: 84%)
  });

  test('a call box and a police post keep `apart` m off every other, and a booth never stands where the dock does', () => {
    const all = [...t.report.filter(r => r.kind === 'call-box'), ...t.origins.filter(o => o.kind !== undefined && o.item)];
    for (const [i, a] of all.entries()) for (const b of all.slice(i + 1)) {
      if (a.item === b.item) continue; // a post is listed once as an origin
      if (!/^street\/civic\//.test(a.id) && !/^street\/civic\//.test(b.id)) continue; // the street's own items were placed by their own rules
      expect(Math.hypot(a.at[0] - b.at[0], a.at[1] - b.at[1]), `${a.id} ${b.id}`).toBeGreaterThanOrEqual(QUIET.apart - 0.01);
    }
  });

  test('R-HIDE: a stash on a bin, a container whose piece lost its tag, a door that is not a service door', () => {
    const bin = failing(w => { w.street.find(s => s.kind === 'bin')!.stash = { holds: 1 }; });
    expect(bin['R-HIDE']).toMatch(/carries no stash tag/);
    const holds = failing(w => { w.street.find(s => s.stash)!.stash!.holds = 3; });
    expect(holds['R-HIDE']).toMatch(/holds 3, its piece Dumpster holds 1/);
    const ghost = failing(w => { w.takedown!.stashes.find(s => s.kind === 'container')!.item = 'street/no-such-item'; });
    expect(ghost['R-HIDE']).toMatch(/is no container/);
    const door = failing(w => { w.takedown!.stashes.find(s => s.kind === 'door')!.of = 'cafe-entrance'; });
    expect(door['R-HIDE']).toMatch(/cafe-entrance is no service door/);
  });

  test('R-STASH: a service door with no stash, with no container and no reason, a container with room for nobody', () => {
    expect(failing(w => { w.takedown!.stashes = w.takedown!.stashes.filter(s => s.of !== 'office-service-door'); })['R-STASH']).toMatch(/Office service door is no stash/);
    expect(failing(w => { const k = w.takedown!.stashes; w.takedown!.stashes = k.filter(s => !(s.kind === 'container')); })['R-STASH']).toMatch(/Office service door has no container and says no why/);
    expect(failing(w => { w.takedown!.stashes.find(s => s.kind === 'container')!.stand = [0, 0]; })['R-STASH']).toMatch(/nobody can stand at its container/); // (0, 0) plan is open water: not free ground on the engine's grid
    expect(failing(w => { const b = w.takedown!.stashes.find(s => s.kind === 'container')!; b.holds = 0; })['R-STASH']).toMatch(/its container holds 0/);
  });

  test('R-REPORT: take the call boxes away and ground is left more than 40 m from a phone; R-DOCK: take the posts away and the Office side is out of reach of a dock', () => {
    const f = failing(w => { w.takedown!.report = w.takedown!.report.filter(r => r.kind === 'door'); });
    expect(f['R-REPORT']).toMatch(/samples farther, e\.g\. block/);
    expect(Object.keys(f)).toEqual(['R-REPORT']);
    const g = failing(w => { w.takedown!.origins = w.takedown!.origins.filter(o => o.kind === 'dock'); });
    expect(g['R-DOCK']).toMatch(/samples farther/);
    expect(Object.keys(g)).toEqual(['R-DOCK']);
  });

  test('R-REPORT / R-DOCK: a point whose street item is not where it says is refused', () => {
    const f = failing(w => { const p = w.takedown!.origins.find(o => o.item && /police-post-0/.test(o.item))!; p.at = [p.at[0] + 5, p.at[1]]; });
    expect(f['R-DOCK']).toMatch(/not where its street item is/);
  });

  test('deterministic: the same plan, block and kit give the same takedown, and a pinned block with no service door gives none', () => {
    const again = generated(plan, d, civ).w.takedown;
    expect(again).toEqual(t);
    const old = JSON.parse(JSON.stringify(read(plan.civic.block)));
    for (const b of old.buildings) b.entrances = b.entrances.filter((e: { kind?: string }) => e.kind !== 'service');
    const civOld = civic(plan.civic, old);
    expect(civOld.serviceDoors).toEqual([]);
  });
});
