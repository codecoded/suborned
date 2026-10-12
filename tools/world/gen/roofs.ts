// Roofs: every roof gets one programme from its use's list (standards.buildings.roofs.byUse), tried in a seeded order until
// one fits (minRoof, SF-15), built from roofs.kit: plant, rocket vents, parapet and the programme's items.
import type { Prim, Roof } from '../../../contracts/world/world.schema';
import { type V, dist, sub } from '../../plan/geom';
import { type Ctx, r3, r4, rng, rv } from './common';
import { kindOf } from './edges';
import { type Frame, type Rect, areaOf, fit } from './geo';
import type { Bld } from './masses';
import type { Terrace } from './types';
import { adoptedBy, roofKit } from './kitrefs';

const ACCESSIBLE = new Set(['roof-garden', 'sun-terrace-pool', 'tennis-court', 'hover-pad', 'observation-saucer']);

export function generateRoofs(ctx: Ctx, blds: Bld[], terraces: Terrace[]): Roof[] {
  const R = ctx.B.roofs, kit = R.kit, prog = kit.programmes, always = kit.always, out: Roof[] = [], hb = ctx.B.halfBay as number;
  for (const b of blds) {
    const id = `${b.id}/roof`, r = rng(id), list = r.shuffle(R.byUse[b.tower ? 'tower' : b.use] as string[]);
    const quay = b.ring.c.some(c => kindOf(c) === 'quay');
    const mk = (f: Frame) => {
      const parts: Prim[] = [], P = (n: string, shape: Prim['shape'], surface: string, lu: number, lv: number, size: [number, number, number], base = 0, yawAdd = 0): void => {
        const U: V = [Math.cos(f.yaw), Math.sin(f.yaw)], Vv: V = [-U[1], U[0]];
        parts.push({ id: `${id}/${n}`, shape, surface, c: rv([f.c[0] + U[0] * lu + Vv[0] * lv, f.c[1] + U[1] * lu + Vv[1] * lv]), base: r3(b.z + base), size: [r3(size[0]), r3(size[1]), r3(size[2])], yaw: r4(f.yaw + yawAdd) });
      };
      return { parts, P };
    };
    let chosen = '', frame: Frame | null = null, built: ReturnType<typeof mk> | null = null;
    for (const name of [...list, 'antenna-mast']) {
      const k = prog[name]; if (!k) continue;
      if (name === 'crane-gantry' && !quay) continue;
      if (name === 'observation-saucer' && b.tower === false && b.use !== 'commercial') continue;
      const margin = k.setback === 0 ? 0 : hb, axis = name === 'sawtooth-northlight', f = fit(b.top, margin, axis), [mw, md] = k.minRoof as [number, number];
      const swap = !(f.W >= mw && f.D >= md) && f.W >= md && f.D >= mw;
      if (!((f.W >= mw && f.D >= md) || swap)) continue;
      if (swap) { f.yaw += Math.PI / 2; [f.W, f.D] = [f.D, f.W]; }
      chosen = name; frame = f; built = mk(f); break;
    }
    const f = frame ?? fit(b.top, 0, false), m = built ?? mk(f), { parts, P } = m;
    if (!chosen) chosen = 'antenna-mast';
    const W = f.W, D = f.D, k = prog[chosen];
    let padKit: ReturnType<typeof roofKit>, pad = { c: [0, 0] as V, half: [7, 7] as V }; // hover-pad: where it stands in the roof frame, and its half sizes
    switch (chosen) {
      case 'roof-garden': {
        const rows = Math.max(1, Math.floor((D - 4) / 4));
        for (let i = 0; i < rows; i++) P(`bed-${i}`, 'box', 'park', 0, -D / 2 + 1 + i * ((D - 2) / rows) , [Math.min(W - 2, 12), k.items.bed.w, k.items.bed.h]);
        P('pergola', 'box', 'trim', W / 2 - 3.5, 0, k.items.pergola as [number, number, number]);
        for (let i = 0; i < 3; i++) { P(`tub-${i}`, 'box', 'trim', -W / 2 + 2 + i * 3, D / 2 - 1.5, [1.5, 1.5, 0.9]); P(`palm-${i}`, 'cyl', 'trim', -W / 2 + 2 + i * 3, D / 2 - 1.5, [0.4, 0.4, 4.5], 0.9); P(`crown-${i}`, 'sphere', 'park', -W / 2 + 2 + i * 3, D / 2 - 1.5, [3, 3, 3], 2.4); }
        break; }
      case 'sun-terrace-pool': P('deck', 'box', 'deck', 0, 0, [16, 9, 0.05]); P('pool', 'box', 'water', -1, 0, [12, 5, 0.1], 0.05); P('pergola', 'box', 'trim', 6, 3.6, [6, 3, 3]); P('cabana', 'box', 'awning', -6, -3.6, [3, 3, 2.7]); break;
      case 'hover-pad': { // the kit's SkyPad_Roof (plan programme.kit) stands in the pad's corner of the roof, flush to the parapet band, the kiosk opposite
        const a = adoptedBy(k.kit), [pw, pd] = a ? [a.size[0], a.size[2]] : [14, 14], lu = -W / 2 + pw / 2, lv = a ? -D / 2 + pd / 2 : 0;
        pad = { c: [lu, lv], half: [pw / 2, pd / 2] };
        const at: V = [f.c[0] + Math.cos(f.yaw) * lu - Math.sin(f.yaw) * lv, f.c[1] + Math.sin(f.yaw) * lu + Math.cos(f.yaw) * lv];
        padKit = roofKit(k, f, at, f.yaw); if (!padKit) { pad = { c: [-W / 2 + 7.5, 0], half: [7, 7] }; P('pad', 'cyl', 'trim', -W / 2 + 7.5, 0, [14, 14, 0.6]); }
        P('kiosk', 'box', 'trim', W / 2 - 2.5, D / 2 - 2.5, [3, 3, 3]); break; }
      case 'antenna-mast': { const h = b.tower ? 24 : k.items.mast[2]; P('mast', 'box', 'trim', 0, 0, [2, 2, h]); for (const at of k.items.rings.at as number[]) P(`ring-${at}`, 'cyl', 'trim', 0, 0, [3, 3, 0.3], Math.min(at, h)); break; }
      case 'water-sphere': P('sphere', 'sphere', 'trim', 0, 0, [6, 6, 6], 4); for (let i = 0; i < 4; i++) P(`leg-${i}`, 'box', 'column', i % 2 ? 2 : -2, i < 2 ? 2 : -2, [0.4, 0.4, 4]); break;
      case 'observation-saucer':
        if (b.tower) { P('stem', 'cyl', 'trim', 0, 0, [4, 4, 6]); P('saucer', 'saucer', 'landmark', 0, 0, [16, 16, 4], 6); } else { P('drum', 'cyl', 'trim', 0, 0, [12, 12, 1]); P('dome', 'sphere', 'landmark', 0, 0, [12, 12, 12], 0.5); }
        break;
      case 'sign-frame': { P('leg-0', 'box', 'column', -7, 0, [0.8, 0.8, 2]); P('leg-1', 'box', 'column', 7, 0, [0.8, 0.8, 2]); P('frame', 'box', 'sign', 0, 0, [18, 1, 6], 2); break; }
      case 'tennis-court': P('runoff', 'box', 'deck', 0, 0, [36.6, 18.3, 0.05]); P('court', 'box', 'park', 0, 0, [23.77, 10.97, 0.1], 0.05); P('fence-w', 'box', 'trim', -18.3, 0, [0.1, 18.3, 3.6]); P('fence-e', 'box', 'trim', 18.3, 0, [0.1, 18.3, 3.6]); break;
      case 'sawtooth-northlight': { const n = Math.max(1, Math.floor(D / 6)); for (let i = 0; i < n; i++) P(`tooth-${i}`, 'wedge', 'roof', 0, -D / 2 + 3 + i * 6, [W, 6, 3]); break; }
      case 'barrel-vault': { const span = Math.min(D, 30); P('vault', 'vault', 'roof', 0, 0, [W, span, span / 6]); break; }
      case 'crane-gantry': P('leg-0', 'box', 'trim', 0, -(W + 2) / 2, [1.5, 1.5, 12]); P('leg-1', 'box', 'trim', 0, (W + 2) / 2, [1.5, 1.5, 12]); P('beam', 'box', 'trim', 0, 0, [1.5, W + 2, 1.5], 12); break;
      case 'solar-dish': P('pedestal', 'box', 'column', 0, 0, [2, 2, 4]); P('dish', 'saucer', 'trim', 0, 0, [8, 8, 0.8], 4.2); break;
    }
    // always: plant (one per stair-lift core: one per building), rocket vents on the roof's edge band
    const keep = (x: number, y: number) => chosen !== 'hover-pad' || Math.hypot(Math.max(Math.abs(x - pad.c[0]) - pad.half[0], 0), Math.max(Math.abs(y - pad.c[1]) - pad.half[1], 0)) >= 10; // no vent within 10 m of the pad edge
    if (W >= 12 && D >= 8) P('plant', 'box', 'column', W / 2 - 3, -D / 2 + 3, always.plant.size);
    const nv = Math.max(2, Math.ceil(areaOf(b.top) / 300));
    for (let i = 0; i < nv; i++) { const x = -W / 2 + 1.5 + ((i + 0.5) / nv) * Math.max(0.1, W - 3), y = (i % 2 ? 1 : -1) * Math.max(0, D / 2 - 1.2); if (keep(x, y)) P(`vent-${i}`, 'box', 'trim', x, y, always['rocket-vent'].size); }
    out.push({ id, building: b.id, programme: chosen, surface: 'roof', z: b.z, c: rv(f.c), size: [r3(f.W), r3(f.D)], yaw: r4(f.yaw), parapet: ACCESSIBLE.has(chosen) ? always.parapet.accessible : always.parapet.other, parts, ...(padKit ? { kit: padKit } : {}) });
  }
  // stepped garden terraces: tubs and a planting strip along each step's roof
  for (const t of terraces) {
    const [x0, x1, y0, y1] = t.rect, w = x1 - x0, parts: Prim[] = [], cy = (y0 + y1) / 2, id = `${t.id}/roof`;
    for (let x = x0 + 3, i = 0; x < x1 - 1; x += 6, i++) {
      parts.push({ id: `${id}/tub-${i}`, shape: 'box', surface: 'trim', c: rv([x, y0 + 1.2]), base: t.z, size: [1.5, 1.5, 0.9], yaw: 0 }, { id: `${id}/palm-${i}`, shape: 'cyl', surface: 'trim', c: rv([x, y0 + 1.2]), base: r3(t.z + 0.9), size: [0.4, 0.4, 3.6], yaw: 0 }, { id: `${id}/crown-${i}`, shape: 'sphere', surface: 'park', c: rv([x, y0 + 1.2]), base: r3(t.z + 2.4), size: [3, 3, 3], yaw: 0 });
    }
    parts.push({ id: `${id}/bed`, shape: 'box', surface: 'park', c: rv([(x0 + x1) / 2, y1 - 1]), base: t.z, size: [r3(w - 2), 1.5, 0.9], yaw: 0 });
    out.push({ id, building: t.id, programme: 'roof-garden', surface: 'roof', z: r3(t.z), c: rv([(x0 + x1) / 2, cy]), size: [r3(w), r3(y1 - y0)], yaw: 0, parapet: ctx.B.roofs.kit.always.parapet.accessible, parts, form: 'terrace' });
  }
  void sub; void ({} as Rect);
  return out;
}
