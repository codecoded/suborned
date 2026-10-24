// The kerb round a kerb return (brief 028, Sky 2026-10-24: rounded pavement corners). The straight kerb stones (capstone.ts G1, the Handover's zone) and the builder's kerb strip stop at the tangent point; this lays the
// pearl kerb (Kerb_Pearl_0/1, the same two stones) along every return's arc, all over the district, so the kerb has no mitre and no gap at a rounded corner.
//   kind `kerb-return`, one stone per chord of the arc (at most 7.5 degrees, never under 0.6 m: 12 stones round a quarter turn of 6 m or more), the stone's long side along the chord, its centre 0.15 m inside the kerb face
//   (the pearl stone has no gutter: its road-side face stands on the arc), z 0 and the height of a G1 stone. It is its own kind, not `kerb`: CAP-1 judges a `kerb` stone against a straight kerb face and CAP-4 keeps the
//   programme to its zone; KR-1 (rules/kerbreturn.ts) judges these. A stone that would stand across a crossing strip or a drive is dropped (the kerb drops there), as G1's are.
// Deterministic: no choice at all; the stone pair alternates by chord.
import type { StreetItem } from '../../../../contracts/world/world.schema';
import { type V, dist } from '../../../plan/geom';
import { type Ctx, r3, r4 } from '../common';
import { adoptedBy } from '../kitrefs';
import { boxRing } from '../../../plan/street-zones';
import { baysOf, chordsOf, returnsOf } from '../kerbreturn';
import { facing, pivotFor } from './capstone';
import { LAYER, PAVE, RULES } from './capstone-rules';
import { overlaps } from './crossings';

export type KerbArcLog = { returns: number; stones: number; arcM: number; droppedM: number; pieces: Record<string, number> };

export function dressKerbArcs(ctx: Ctx, o: { strips: V[][]; drives: V[][] }): { items: StreetItem[]; log: KerbArcLog } | undefined {
  const R = RULES.kerb, stones = R.pieces.map(adoptedBy);
  const list = returnsOf(ctx);
  if (!list.length || stones.some(s => !s)) return undefined;
  const ground = stones[0]!, scaleY = r3((PAVE + LAYER.kerb) / ground.size[1]), items: StreetItem[] = [], log: KerbArcLog = { returns: 0, stones: 0, arcM: 0, droppedM: 0, pieces: {} };
  const lay = (ctr: V, n: V, len: number, edge: string, k: number, mid: number) => { // one stone: its centre `ctr` (0.15 m inside the face), `n` toward the carriageway
    const a = stones[k & 1]!, L = len + 0.02, sx = r3(L / a.size[0]), yaw = facing(n), ring = boxRing(ctr, Math.max(0.1, L - 0.1), R.halfDepth * 2, yaw);
    log.arcM += len; void mid;
    if (o.strips.some(p => overlaps(ring, p)) || o.drives.some(p => overlaps(ring, p))) { log.droppedM += len; return; }
    const piv = pivotFor(a, ctr, yaw, [sx, scaleY, 1]), size: [number, number, number] = [r3(a.size[0] * sx), r3(a.size[2]), r3(a.size[1] * scaleY)];
    items.push({ id: `street/kerbreturn/kerb-return-${items.length}`, kind: 'kerb-return', at: piv, yaw: r4(yaw), z: 0, size, surface: 'trim', edge, sight: 'open', source: 'kit', kit: { id: a.id, scale: [sx, scaleY, 1] } });
    log.stones++; log.pieces[a.id] = (log.pieces[a.id] ?? 0) + 1;
  };
  for (const r of list) {
    if (r.nose) continue; // a median's kerb is its own prism's side: no stone on its ends alone
    log.returns++;
    chordsOf(r, -R.inset).forEach((c, k) => lay(c.at, c.n, c.len, `kerbreturn/${r.id}`, k, c.mid)); // the chord's centre is on the circle R.inset inside the kerb face: the stone's own centre
  }
  // a kerbed bay's straight kerb faces between its arcs (an end, the back, an end): the pearl kerb round the lay-by, never across its mouth (the plot's run stops at the mouth corner's tangent point)
  for (const b of baysOf(ctx)) b.faces.forEach((f, fi) => {
    const len = dist(f.a, f.b); if (len < 0.1) return;
    const m = Math.max(1, Math.round(len / R.module)), u: V = [(f.b[0] - f.a[0]) / len, (f.b[1] - f.a[1]) / len];
    for (let k = 0; k < m; k++) { const s = (k + 0.5) * (len / m); lay([f.a[0] + u[0] * s - f.n[0] * R.inset, f.a[1] + u[1] * s - f.n[1] * R.inset], f.n, len / m, `kerbreturn/layby/${b.id}/${fi}`, k, s); }
  });
  log.arcM = r3(log.arcM); log.droppedM = r3(log.droppedM);
  return { items, log };
}
