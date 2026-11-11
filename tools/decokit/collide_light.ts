// Light-only materials (brief 107, Vela's ask in briefs/107/vela/glows.md and sign-and-edge.md; Splice): a material that draws light and has no body is never measured
// as a solid. The rule is the materials catalogue's kind `glow` (tools/decokit/mats.py: a light volume, no .mat written, drawn additive with Suborned/Cross Glow): the
// crossings' beams, halos and pools, the road pool, the signal rim lens, the planter feet's and the inlays' strips. An `emit` material (a lamp globe, a lens, a sign's
// letters) is a lit body and stays solid. Measuring leaves a light material's triangles out entirely (volumes, open parts and bounds), so a glow lying on the paving or
// up a wall never widens a piece or adds a blocked cell.
// Each measured record names the light materials it left out (`light`), so a record says what it was measured without, the geometry audit reads the same list from
// kit.collide.json (no generator output needed), and a piece whose light set changes (a new glow, a kind changed) is measured again (collide_law.ts planCollide).
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

export const LIGHT_KIND = 'glow';

/** The light-only materials the catalogues under `roots` name (decokit_materials.json, kind glow); undefined when no root has a catalogue (then a record's own list stands). */
export function readLight(roots: string[]): Set<string> | undefined {
  let found = false;
  const out = new Set<string>();
  for (const r of roots) {
    const f = join(r, 'decokit_materials.json');
    if (!existsSync(f)) continue;
    found = true;
    for (const m of JSON.parse(readFileSync(f, 'utf8')).materials as { name: string; kind: string }[]) if (m.kind === LIGHT_KIND) out.add(m.name);
  }
  return found ? out : undefined;
}

/** The light materials a piece carries, sorted. */
export const lightOf = (materials: readonly string[], light: ReadonlySet<string>): string[] => [...new Set(materials.filter(m => light.has(m)))].sort();

/** Same light set: a record without `light` was measured with none. */
export const sameLight = (a: readonly string[] | undefined, b: readonly string[] | undefined): boolean => (a ?? []).join(',') === (b ?? []).join(',');

/** A Blender job argument: `id=fbx`, or `id=fbx|mat,mat` with the light materials to leave out (collide.py, tools/geometry/bounds.py). */
export const jobArg = (id: string, fbx: string, light: readonly string[]): string => (light.length ? `${id}=${fbx}|${light.join(',')}` : `${id}=${fbx}`);
