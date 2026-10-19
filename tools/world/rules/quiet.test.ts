// R-HIDE, R-STASH, R-REPORT, R-DOCK on the live world (brief 013 C): each passes on the world the generator makes and fails on one thing put back, on a copy of it.
// Also the placement rules themselves: one container per service door where the street rules let one stand, deterministic, off the walking zone, on the side away from the camera.
import { describe, expect, setDefaultTimeout, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { World } from '../../../contracts/world/world.schema';
import { civic } from '../../plan/civic';
import { derive, type Plan } from '../../plan/plan';
import { toBlockFn } from '../../plan/walk-grid';
import { ctxOf, generated } from '../../plan/world-ctx';
import { cameraDir, QUIET } from '../civic/quiet';
import { SITING, cornerOf, junctionCorners, opensOnFootway } from '../civic/siting';
import { type V, lineDist } from '../../plan/geom';
import { r4 } from '../gen/common';
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
  test('the live world passes R-HIDE, R-STASH, R-REPORT, R-REPORT/site and R-DOCK, on the walk grid where a built plot blocks only behind its building line (Starline\'s pavements are ground)', () => {
    expect(Object.keys(failing())).toEqual([]);
    expect(t.coverage.report).toMatchObject({ within: 1000, worstM: 39.8 });
    expect(t.coverage.origin).toMatchObject({ within: 1000, worstM: 39.8 });
  });

  test('the north-west mouth of the block (Starline Street north of Garden Row) has a call box on its own pavement, and the box, the post and the dock do not bunch at the square\'s corner (brief 023)', () => {
    const toBlock = toBlockFn(plan.civic), items = base.w.street.filter(s => s.id.startsWith('street/civic/')).map(s => ({ id: s.id, kind: s.kind, p: toBlock(s.at) as [number, number] }));
    const north = items.filter(i => i.kind === 'phone-booth' && i.p[0] > -27 && i.p[0] < -20 && i.p[1] > 4);
    expect(north.length, 'a call box on the pavement of Starline Street north of Garden Row').toBeGreaterThanOrEqual(1);
    const corner: [number, number] = [-26, -8]; // where Garden Row leaves Starline Street on the square's side: the box, the post and the dock used to stand within 7 m of it
    expect(items.filter(i => Math.hypot(i.p[0] - corner[0], i.p[1] - corner[1]) < 12).length, 'at most one placed box or post within 12 m of that corner').toBeLessThanOrEqual(1);
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

  test('a call box, a police post or a dock keeps `apart` m off every other, of its kind or not', () => {
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

  test('call boxes stand where a real city puts them: on a corner\'s pavement, off the corner, never in a carriageway or a side road\'s mouth, opening on the footway (brief 023 research)', () => {
    const roads = d.roads, corners = junctionCorners(roads, plan.nodes), boxes = base.w.street.filter(s => /^street\/civic\/call-box-\d+$/.test(s.id));
    expect(boxes.length).toBe(6); // the walk grid before 2026-10-19 (a built plot blocked to its edge, pavements and all) made 4; the pavements are ground now
    for (const b of boxes) {
      const r = roads.find(o => `civic/${o.id}` === b.edge)!, k = cornerOf(r, b.at as V, corners)!;
      expect(k, b.id).toBeDefined();
      expect(k.along, b.id).toBeGreaterThanOrEqual(SITING.setback - 0.5);
      expect(k.along, b.id).toBeLessThanOrEqual(SITING.zone + 0.5);
      expect(roads.some(o => lineDist(b.at as V, o.line) < o.w / 2), `${b.id} in a carriageway`).toBe(false);
      expect(opensOnFootway(r.line, b.at as V, b.yaw), `${b.id} faces the road`).toBe(true);
    }
    // the two the old rule stood in a side road's mouth, (-186,-260) in Rocket Lane's and (-134,-205) in Garden Row's, are gone, and nothing else stands there
    for (const mouth of [[-186, -260], [-134, -205]] as V[]) expect(base.w.street.some(s => s.kind === 'phone-booth' && Math.hypot(s.at[0] - mouth[0], s.at[1] - mouth[1]) < 4)).toBe(false);
  });

  test('a corner exists only where a street leaves: Garden Row meets Starline Street on its south pavement, two corners, and none on the far side', () => {
    const cs = junctionCorners(d.roads, plan.nodes).filter(c => c.node === 'r_garden_n');
    expect(cs.map(c => c.at.map(Math.round).join(',')).sort()).toEqual(['-126,-204', '-138,-204']);
    expect(junctionCorners(d.roads, plan.nodes).some(c => c.at[1] > -198 && c.at[1] < -190 && c.at[0] > -140 && c.at[0] < -124)).toBe(false);
  });

  test('R-REPORT/site: a box in a side road\'s mouth, one that opens on the road, one on a frontage no corner serves', () => {
    const mouth = failing(w => { const b = w.street.find(s => s.id === 'street/civic/call-box-0')!; b.at = [-186, -260]; });
    expect(mouth['R-REPORT/site']).toMatch(/call-box-0 stands in the carriageway of Rocket Lane/);
    const gm = failing(w => { const b = w.street.find(s => s.id === 'street/civic/call-box-1')!; b.at = [-134, -205]; });
    expect(gm['R-REPORT/site']).toMatch(/call-box-1 stands in the carriageway of Garden Row/);
    const road = failing(w => { const b = w.street.find(s => s.id === 'street/civic/call-box-0')!; b.yaw = r4(b.yaw + Math.PI); });
    expect(road['R-REPORT/site']).toMatch(/call-box-0 opens on the carriageway of Orbit Walk, not on the footway/);
    const far = failing(w => { const b = w.street.find(s => s.id === 'street/civic/call-box-0')!; b.edge = 'civic/garden-row'; b.at = [-139, -232.5]; });
    expect(far['R-REPORT/site']).toMatch(/call-box-0 stands 2\d\.\d m from the corner at .*: a corner's box stands 6\.\.25 m off it/);
    const near = failing(w => { const b = w.street.find(s => s.id === 'street/civic/call-box-2')!; b.at = [-178, -260]; });
    expect(near['R-REPORT/site']).toMatch(/call-box-2 stands [0-5]\.\d m from the corner/);
    expect(Object.keys(mouth).includes('R-REPORT/site')).toBe(true);
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
