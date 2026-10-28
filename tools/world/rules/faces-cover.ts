// Brief 072-C, the "no flat face at play zoom" rule (Sky 2026-10-27: DecoKit on "every" face of every building in the district): every wall face of every
// building in the export (plot buildings and tower complexes, tiers included, and the civic block's buildings) is dressed by kit. Read from the export alone:
// a face is the part of a mass ring's edge that no other building's wall stands against (exposure, as the generator measures it), from the height it is
// exposed from to the mass top; it is dressed where placed kit pieces (facades by their footprint and height, a civic kit mass by the piece that replaces it)
// stand over it from foot to top. Megastructures, signatures and landmarks are kit or structures of their own and not counted here. A tower site's hidden stand-in
// (T-K1, brief 075-C) is counted, dressed by the site's kit tower (signatures tower/<site>) when one stands there, else undressed.
import type { World, Mass } from '../../../contracts/world/world.schema';
import type { KitAsset } from '../../../contracts/world/kit.schema';
import { type V, dist, norm, sub } from '../../plan/geom';
import { type Cover, FACES, coveredSpans, exposure, occOf, quadOf } from '../gen/faces';
import { ccw } from '../gen/geo';

export type FaceGap = { face: string; building: string; area: string; play: boolean; length: number; from: number; to: number; at: V };
export type FaceCoverage = {
  faces: number; metres: number; dressedMetres: number; undressed: FaceGap[]; /** T-K1: faces of hidden tower stand-ins counted dressed by the site's kit tower */ byKitTower: number;
  perPiece: { all: Record<string, number>; play: Record<string, number> }; perArea: Record<string, { faces: number; undressed: number; metres: number; undressedMetres: number; modules: number }>;
};

const ownerOf = (id: string) => (id.startsWith('civic/') ? id.split('/').slice(0, 2).join('/') : id.split('/').slice(0, id.startsWith('tower/') ? 2 : 3).join('/'));

/** areaOf(building id) = its area (plot buildings by plot, tower sites by their plot, civic = the block's area); play(area) = a play-street area. */
export function faceCoverage(w: World, catalogue: (id: string) => KitAsset | undefined, areaOf: (building: string) => string, play: (area: string) => boolean): FaceCoverage {
  // T-K1 (brief 075-C): a tower site's stand-in masses are `hidden` where the site's regenerated kit piece stands (signatures tower/<site>, source kit); their faces count
  // as dressed by that piece. A hidden tower mass with no kit piece over it counts every exposed face as undressed: hiding never passes a face by itself.
  const kitTower = new Set(w.signatures.filter(s => s.kind === 'tower' && s.source === 'kit' && s.kit).map(s => s.id));
  const isWall = (m: Mass) => /^(bld|tower)\//.test(m.id) && !/\/band-\d+$/.test(m.id) && m.height > 1;
  const walls: Mass[] = [...w.buildings.filter(m => isWall(m)), ...w.civic.filter(m => m.surface === 'limestone' && !m.hidden && !m.kit && m.height > 1)];
  const civicKit = w.civic.filter(m => m.kit && !m.hidden);
  const occ = [...walls.map(m => occOf(ownerOf(m.id), m.ring, m.base, m.base + m.height)), ...civicKit.filter(m => m.height > 1).map(m => occOf(ownerOf(m.id), m.ring, m.base, m.base + m.height)),
    ...w.civic.filter(m => m.hidden && m.surface === 'limestone').map(m => occOf(ownerOf(m.id), m.ring, m.base, m.base + m.height)),
    ...w.megastructures.flatMap(g => g.parts.map(p => occOf(g.id, p.ring, p.base, p.base + p.height)))];
  const covers = new Map<string, Cover[]>(), add = (owner: string, c: Cover) => (covers.get(owner) ?? covers.set(owner, []).get(owner)!).push(c);
  for (const f of w.facades) { const a = catalogue(f.kit.id); if (a) add(f.building, { quad: quadOf(f.kit.at, a.size, f.kit.scale, f.kit.yaw ?? 0, 0.05), lo: f.base, hi: f.base + a.size[1] * f.kit.scale[1], owner: f.building }); }
  for (const m of civicKit) { const a = catalogue(m.kit!.id); if (!a) continue; const off = a.pivotOffset ?? [0, 0], y = m.kit!.yaw ?? 0, s = m.kit!.scale, c: V = [m.kit!.at[0] + off[0] * s[0] * Math.cos(y) - off[1] * s[2] * Math.sin(y), m.kit!.at[1] + off[0] * s[0] * Math.sin(y) + off[1] * s[2] * Math.cos(y)]; add(ownerOf(m.id), { quad: quadOf(c, a.size, s, y, 0.05), lo: m.kit!.base, hi: m.kit!.base + a.size[1] * s[1], owner: ownerOf(m.id) }); }
  const out: FaceCoverage = { faces: 0, metres: 0, dressedMetres: 0, byKitTower: 0, undressed: [], perPiece: { all: {}, play: {} }, perArea: {} };
  const areaRow = (a: string) => (out.perArea[a] ??= { faces: 0, undressed: 0, metres: 0, undressedMetres: 0, modules: 0 });
  for (const f of w.facades) { const a = areaOf(f.building); out.perPiece.all[f.kit.id] = (out.perPiece.all[f.kit.id] ?? 0) + 1; if (play(a)) out.perPiece.play[f.kit.id] = (out.perPiece.play[f.kit.id] ?? 0) + 1; areaRow(a).modules++; }
  for (const m of walls) {
    const owner = ownerOf(m.id), area = areaOf(owner), ring = ccw(m.ring), z1 = m.base + m.height, cv = covers.get(owner) ?? [], byPiece = !!m.hidden && kitTower.has(owner);
    for (let i = 0; i < ring.length; i++) {
      const a = ring[i]!, b = ring[(i + 1) % ring.length]!, L = dist(a, b); if (L < FACES.minFace) continue;
      const t = norm(sub(b, a));
      for (const x of exposure(a, b, m.base, z1, occ, owner)) {
        const pa: V = [a[0] + t[0] * x.s0, a[1] + t[1] * x.s0], pb: V = [a[0] + t[0] * x.s1, a[1] + t[1] * x.s1], len = x.s1 - x.s0;
        if (len < FACES.minFace) continue;
        const done: [number, number][] = byPiece ? [[0, len]] : m.hidden ? [] : coveredSpans(pa, pb, x.from, z1, cv);
        let open = len, cur = 0; const gaps: [number, number][] = [];
        for (const [c0, c1] of done) { if (c0 - cur >= FACES.minFace) gaps.push([cur, c0]); cur = Math.max(cur, c1); }
        if (len - cur >= FACES.minFace) gaps.push([cur, len]);
        open = gaps.reduce((s, [g0, g1]) => s + g1 - g0, 0);
        out.faces++; out.metres += len; out.dressedMetres += len - open; if (byPiece) out.byKitTower++;
        const row = areaRow(area); row.faces++; row.metres += len;
        if (!gaps.length) continue;
        row.undressed++; row.undressedMetres += open;
        const g0 = gaps[0]!, mid = (g0[0] + g0[1]) / 2;
        out.undressed.push({ face: `${m.id}#${i}`, building: owner, area, play: play(area), length: Math.round(open * 100) / 100, from: Math.round(x.from * 100) / 100, to: Math.round(z1 * 100) / 100, at: [Math.round((pa[0] + t[0] * mid) * 10) / 10, Math.round((pa[1] + t[1] * mid) * 10) / 10] });
      }
    }
  }
  return out;
}
