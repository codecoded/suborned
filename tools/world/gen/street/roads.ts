// Street dressing per street edge (standards.streetscape): crossings and bollards, trees, lamps and the catalogue's
// furniture by the counts and spacings it gives, in the kerb zone, never in the walk. An item that does not fit is skipped.
import { type V, dist, lineDist } from '../../../plan/geom';
import { type Ctx, hash32, rng } from '../common';
import { idOf, kindOf, plotRing } from '../edges';
import { edgeN } from '../geo';
import type { Layer } from './kit';

const ROADS = ['primary', 'district', 'access'];

export function dressRoads(ctx: Ctx, L: Layer): void {
  const S = ctx.S, z = ctx.pav, sparse: Record<string, V[]> = {};
  const far = (k: string, p: V, sep: number) => !(sparse[k] ??= []).some(q => dist(q, p) < sep);
  const avenue = new Set<string>(S.trees.avenues.access.avenue), corners = new Set<string>(S.trees.avenues.access.cornersOnly);
  for (const plot of ctx.d.plots) {
    if (plot.civic) continue;
    const ring = plotRing(ctx, plot), n = ring.p.length, dense = ['commercial', 'mixed', 'industrial', 'port'].includes(plot.use);
    for (let i = 0; i < n; i++) {
      const kind = kindOf(ring.c[i]!); if (!ROADS.includes(kind)) continue;
      const a = ring.p[i]!, b = ring.p[(i + 1) % n]!, len = dist(a, b), u: V = [(b[0] - a[0]) / len, (b[1] - a[1]) / len], nrm = edgeN(a, b), id = `${plot.id}/e${i}`, r = rng(`street/${id}`);
      const at = (s: number, d: number): V => [a[0] + u[0] * s - nrm[0] * d, a[1] + u[1] * s - nrm[1] * d], yaw = Math.atan2(u[1], u[0]), lampYaw = Math.atan2(nrm[1], nrm[0]); // the lamp's arm (kit +X) points along the kerb normal, over the carriageway
      const sec = S.sections[kind], par = nrm[0] + nrm[1] > 0 ? 0 : 1, tree = kind === 'district' || (kind === 'access' && avenue.has(plot.area));
      if (len < 14) continue;
      if (kind === 'primary') { for (let s = 45 + par * 20; s < len - 10; s += dense ? 90 : 150) L.add('hydrant', at(s, 0.8), 0, z, id); continue; }
      // crossings at both ends of district and access edges, flanked by bollards; beacons on district crossings
      for (const s of [5, len - 5]) {
        L.add('crossing', at(s, 0.9), yaw + Math.PI / 2, z, id, { size: [S.crossings.w, 1.8, 0.02], r: 2.2, force: true });
        for (const o of [-2.4, 2.4]) L.add('bollard', at(s + o, sec.kerb - 0.3), yaw, z, id, { size: S.bollards.size, r: 0.2 });
        if (kind === 'district') for (const o of [-2.6, 2.6]) L.add('beacon', at(s + o, 0.7), yaw, z, id, { size: [0.4, 0.4, S.crossings.beacons.h], r: 0.3 });
      }
      const s0 = 9, s1 = len - 9, nT = s1 > s0 ? Math.floor((s1 - s0) / S.trees.spacing) + 1 : 0, t0 = s0 + (s1 - s0 - (nT - 1) * S.trees.spacing) / 2, kerbMid = 0.45 + 0.75;
      if (tree) for (let k = 0; k < nT; k++) L.add('tree', at(t0 + k * S.trees.spacing, kerbMid), 0, z, id, { size: [0.5, 0.5, S.trees.species.washingtonia.top], variant: 'washingtonia', r: 1.5 });
      else if (kind === 'access' && corners.has(plot.area)) for (const s of [9, len - 9]) L.add('tree', at(s, kerbMid), 0, z, id, { size: [0.5, 0.5, S.trees.species.washingtonia.top], variant: 'washingtonia', r: 1.5 });
      // lamps: atom rings midway between trees (district 8 m high, every 48 per side; access 6 m, every 36)
      if (kind === 'district') for (let k = 0, s = s0 + 6 + (par ? 24 : 0); s < s1; k++, s += 48) L.add('lamp', at(s, 0.6), lampYaw, z, id, { size: [0.3, 0.3, 8], variant: 'atom-ring', r: 0.6 });
      if (kind === 'access') for (let s = s0 + (par ? 18 : 0); s < s1; s += 36) L.add('lamp', at(s, 0.6), lampYaw, z, id, { size: [0.3, 0.3, 6], variant: 'atom-ring', r: 0.6 });
      const bench = kind === 'district' ? 36 : avenue.has(plot.area) ? 48 : 0;
      for (let s = 18 + par * 6; bench && s < len - 9; s += bench) { if (L.add('bench', at(s, 0.8), yaw, z, id, { r: 1 })) L.add('bin', at(s + 1.6, 0.8), 0, z, id, { r: 0.4 }); }
      for (let s = 30 + par * 12; s < len - 9; s += dense ? S.catalogue.hydrant.spacing ?? 90 : 150) L.add('hydrant', at(s, 0.8), 0, z, id);
      for (let s = 60 + par * 30; s < len - 9; s += 150) L.add('phone-booth', at(s, 1), yaw, z, id);
      for (let s = 100 + par * 50; s < len - 9; s += 250) L.add('mailbox', at(s, 0.75), yaw, z, id);
      for (let s = 25 + par * 10; s < len - 9; s += kind === 'district' ? 50 : 100) if (L.free(at(s, 0.8), 0.4)) L.add('bin', at(s, 0.8), 0, z, id, { r: 0.4 });
      // corner furniture (staffed kiosks, advertising columns), sparse and chosen by seed, 8 m clear of the junction
      if (kind === 'district' && len > 30) for (const s of [8.5, len - 8.5]) {
        const p = at(s, 0.75);
        if (r.chance(0.3) && far('newsstand', p, 120) && L.add('newsstand', p, yaw, z, id)) sparse.newsstand!.push(p);
        else if (r.chance(0.3) && far('advertising-column', p, 60) && L.add('advertising-column', at(s, 1.0), 0, z, id)) sparse['advertising-column']!.push(p);
        else if (['crescent', 'gardens', 'records'].includes(plot.area) && r.chance(0.5) && far('kiosk-flowers', p, 120) && L.add('kiosk-flowers', p, yaw, z, id)) sparse['kiosk-flowers']!.push(p);
      }
      void hash32; void lineDist;
    }
  }
}
