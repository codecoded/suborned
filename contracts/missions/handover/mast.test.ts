// Brief 101 (Sky 2026-11-09, "Sky picks B, the lamp mast"; Vantage's briefs/101/vantage/square-camera.md): the square's mast camera stands on the terrace's west lamp
// by the café, three heads fanned 60° over the café's side and front, the terrace and the square, its back to the kerb where the cell starts. The engine sees through
// every head at once (Game.cs InHeads), and a loop, a cut or a shot darkens the whole camera (CameraWatching, CameraDark). The ids stay (camera-mast-z0, the sim's
// mast-z0). The round's record is briefs/101/fable/mast-words.json.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { TextFile } from './text.schema.ts';

const read = (...p: string[]) => JSON.parse(readFileSync(join(import.meta.dir, ...p), 'utf8'));
const t = TextFile.parse(read('text.json'));
const site = read('..', '..', '..', 'sim', 'data', 'handover.site.json') as { places: Record<string, { at: [number, number] }> };
const MAST = 'camera-mast-z0', places = t.why!.places;
/** Every line the cell can read about the mast: its Intel, the tips of the orders on it, and its rows of dialogue. */
const mastLines = () => [t.targets[MAST]!.about, t.targets[MAST]!.watched,
  ...Object.values(t.actions).flatMap((a) => [a.onTarget?.[MAST], ...Object.values(a.onTargetBy ?? {}).map((m) => m[MAST])]),
  ...t.dialogue.filter((e) => e.about === MAST).flatMap((e) => e.lines)].filter((s): s is string => !!s);
/** {where} as the host fills it (HostWhy.Names.cs WhyWhere): the nearest of why.places within 13 m of a site place, else `nearby`. */
const where = (id: string) => {
  const [x, y] = site.places[id]!.at;
  let best = places.nearby!, bd = 1300 * 1300;
  for (const [k, w] of Object.entries(places)) {
    const q = site.places[k]?.at; if (k === 'nearby' || !q) continue;
    const d = (q[0] - x) ** 2 + (q[1] - y) ** 2; if (d <= bd) { bd = d; best = w; }
  }
  return best;
};

describe('brief 101: the mast camera on the terrace lamp (Sky picks B)', () => {
  test('its Intel says where it stands, as anyone can see it, and a Watch teaches its three views at once', () => {
    const x = t.targets[MAST]!;
    expect(x.about).toMatch(/lamp post by the café/); expect(x.about).not.toMatch(/over the square|\bsees?\b|cone|cover|point|face/i); // told, never what only a Watch teaches
    expect(x.watched).toMatch(/three lenses watch at once/i); expect(x.watched).toMatch(/café.*terrace.*square/);
  });
  test('no line says it turns, pans or has one lens: every head watches at once, and a Loop or a cut takes all three', () => {
    const lines = mastLines();
    expect(lines.length).toBeGreaterThanOrEqual(10);
    for (const s of lines) expect(s, s).not.toMatch(/\bturns\b|\bpans\b|slow pan|one lens|views/i);
    for (const v of ['loop', 'disable']) expect(t.actions[v]!.onTarget![MAST], v).toMatch(/all three lenses/);
  });
  test('its short name stays the lozenge\'s, and the places under its heads read in the HUD\'s words', () => {
    expect(t.names.targets[MAST]).toBe('the mast camera');
    expect(where('beside-inez')).toBe('on the café terrace'); // route A's Fixer waits beside Inez, under the head over the café's front
    expect(where('d:West side')).toBe('beside the café'); // the head along the café's west side: still the place on the report Sky saw
    for (const w of Object.values(places)) expect(w).not.toMatch(/courtyard/i); // no courtyard is drawn: it was the mission's name for the terrace's middle
  });
});
