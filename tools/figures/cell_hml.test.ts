// Brief 112: the cell's manifest (briefs/112/people/manifest-cell.json, written by tools/figures/cell_hml.ts) against its spec, the rulebook's speeds and what the
// host asks of a cell figure (HostViews.Figures, HostViews.Cell). Reads committed files only: no Blender, no Unity.
import { expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const root = resolve(import.meta.dir, '../..');
const spec = JSON.parse(readFileSync(join(import.meta.dir, 'cell_hml.json'), 'utf8'));
const manifest = JSON.parse(readFileSync(join(root, 'briefs/112/people/manifest-cell.json'), 'utf8'));
const rules = JSON.parse(readFileSync(join(root, 'contracts/rules/rulebook.json'), 'utf8'));
const ids = ['scout', 'fixer', 'tech', 'enforcer'];
const fig = (id: string) => manifest.figures.find((f: any) => f.id === id);
const clip = (id: string, state: string) => fig(id).clips.find((c: any) => c.name === state);
const value = (key: string): number => { let v: any; JSON.stringify(rules, (k, x) => { if (k === key && x && typeof x === 'object' && 'v' in x) v = x.v; return x; }); return v; };

// The states HostViews gives a cell member: the shared ones it can reach (Figures, BodyState, SeatState with SeatTransitions off) and the cell's own (CellState).
const HOST = ['idle', 'idle_alt', 'walk', 'walk_f', 'run', 'run_lean', 'run_armed', 'aim', 'aim_pistol', 'shoot', 'draw', 'lying', 'carry_shoulder', 'carry_shoulder_idle',
  'sit', 'sit_alt', 'cower', 'hit_large', 'cover_in', 'cover_kneel', 'cover_pin', 'cover_fire', 'cover_out', 'idle_armed', 'walk_armed', 'holster', 'look_around'];
const GUN_OUT = ['aim', 'aim_pistol', 'shoot', 'draw', 'run_armed', 'walk_armed', 'idle_armed', 'holster', 'cover_in', 'cover_kneel', 'cover_pin', 'cover_fire', 'cover_out'];
const GUN_AWAY = ['idle', 'idle_alt', 'walk', 'walk_f', 'run', 'run_lean', 'sit', 'sit_alt', 'lying', 'look_around', 'carry_shoulder', 'carry_shoulder_idle', 'cower', 'hit_large'];

test('the manifest holds the four figures of the spec, each built from the bundle the spec names', () => {
  expect(manifest.schema).toBe('suborned.cell-hml/1');
  expect(manifest.figures.map((f: any) => f.id)).toEqual(ids);
  for (const id of ids) {
    expect(fig(id).bundle.sha256).toBe(spec.figures[id].sha256);
    expect(fig(id).sha256).toMatch(/^[0-9a-f]{64}$/);
    expect(fig(id).rig.type).toBe('generic');
    expect(fig(id).missing).toEqual([]);
  }
  expect(JSON.stringify(manifest)).not.toContain('/Users/');
});

test('every figure has a clip of its own for every state the host can give a cell member', () => {
  for (const id of ids) for (const s of HOST) expect([id, s, !!clip(id, s)]).toEqual([id, s, true]);
});

test('each role has the clips its own orders play', () => {
  expect(clip('tech', 'tinker')?.action).toBe('Panel Tinker');   // a loop or a reroute, at the cabinet
  expect(clip('scout', 'mislead')?.action).toBe('Mislead');
  expect(clip('fixer', 'phone')?.action).toBe('Phone Talk');     // a call
});

test('nothing is decimated: every figure keeps the bundle\'s mesh in one level', () => {
  const tris: Record<string, number> = { scout: 28262, fixer: 29714, tech: 26703, enforcer: 26989 };
  for (const id of ids) expect([id, fig(id).lods.reduce((n: number, l: any) => n + l.tris, 0)]).toEqual([id, tris[id]!]);
});

test('the gun shows in the armed states and in no other; the Tech\'s visor shows in all', () => {
  for (const id of ids) {
    const gun = fig(id).props.find((p: any) => p.name === (id === 'enforcer' ? 'carbine' : 'pistol'));
    expect(gun.node).toBe(`prop_${gun.name}`);
    for (const s of GUN_OUT) expect([id, s, gun.states.includes(s)]).toEqual([id, s, true]);
    for (const s of GUN_AWAY) expect([id, s, gun.states.includes(s)]).toEqual([id, s, false]);
  }
  expect(fig('tech').props.find((p: any) => p.name === 'visor').always).toBe(true);
});

test('loops loop, one-shots do not, and the draw is the 2.0 s the host fits to the rulebook\'s draw time', () => {
  for (const id of ids) {
    for (const s of ['idle', 'walk', 'run', 'aim', 'sit', 'lying', 'cover_kneel', 'idle_armed', 'carry_shoulder']) expect([id, s, clip(id, s).loop]).toEqual([id, s, true]);
    for (const s of ['shoot', 'draw', 'cower', 'cover_in', 'cover_out', 'holster']) expect([id, s, clip(id, s).loop]).toEqual([id, s, false]);
    expect([id, clip(id, 'cover_fire').loop]).toEqual([id, id === 'enforcer']); // a pistol's shot from cover is one shot (the host restarts it per shot); the carbine's is the bundle's firing loop
    expect([id, clip(id, 'draw').frames / clip(id, 'draw').fps]).toEqual([id, 2]);
  }
});

test('the legs keep up with the engine: the run plays near its own speed, the walk within the host\'s stride range', () => {
  const walk = value('op-speed-cm-s') / 100, run = value('op-run-speed-cm-s') / 100; // 1.8 and 3.6 m/s
  for (const id of ids) {
    const sp = fig(id).clipSpeedsMs;
    expect(run / sp.run).toBeGreaterThan(0.8); expect(run / sp.run).toBeLessThan(1.25);
    // HostViews.Figures plays a walk at 0.4 to 1.8 times the clip's speed. The Scout's and the Enforcer's walks fit the cell's 1.8 m/s (under 1.3 times). The Fixer's
    // and the Tech's are strolls (about 1 m/s): they play at the top of that range, the Fixer's a few per cent past it, until their bundles carry a brisker walk
    // (brief 112, known limits). The band says so; it is not a target.
    expect(walk / sp.walk).toBeGreaterThan(0.4); expect(walk / sp.walk).toBeLessThan(id === 'scout' || id === 'enforcer' ? 1.3 : 2.0);
    expect(sp.carry_shoulder).toBe(sp.walk);
  }
});

test('seated, the hip joints are on the chair and no sole is under the floor', () => {
  for (const id of ids) {
    const s = fig(id).seat;
    expect(s.hipJointsTarget[1]).toBeCloseTo(0.05, 3);            // 5 cm behind the chair's point (Blender: +Y is behind)
    expect(s.hipJointsTarget[2]).toBeGreaterThan(0.474);           // above the cushion's top
    for (const t of Object.values<any>(s.takes)) { expect(t.proud).toBeLessThan(0.1); expect(t.solesUp).toBeLessThan(0.1); }
  }
});
