// The little street: capsules and boxes, a grid for walking, and the walkers.
import * as THREE from './vendor/three.module.min.js';
import { TARGETS, ROLES } from './data.js';

const CELL = 0.5, X0 = -24, Z0 = -9, NX = 100, NZ = 50;
const M = (c) => new THREE.MeshLambertMaterial({ color: c });
const box = (w, h, d, c) => new THREE.Mesh(new THREE.BoxGeometry(w, h, d), M(c));

class Heap {
  constructor() { this.a = []; }
  push(f, i) { const a = this.a; a.push([f, i]); let k = a.length - 1; while (k > 0) { const p = (k - 1) >> 1; if (a[p][0] <= a[k][0]) break; [a[p], a[k]] = [a[k], a[p]]; k = p; } }
  pop() { const a = this.a, t = a[0], l = a.pop(); if (a.length) { a[0] = l; let k = 0; for (;;) { let c = 2 * k + 1; if (c >= a.length) break; if (c + 1 < a.length && a[c + 1][0] < a[c][0]) c++; if (a[k][0] <= a[c][0]) break; [a[k], a[c]] = [a[c], a[k]]; k = c; } } return t; }
  get size() { return this.a.length; }
}

// A walkable grid with boxes cut out of it. Paths are found with A*, then pulled straight.
export class Nav {
  constructor() { this.b = new Uint8Array(NX * NZ); this.inf = new Uint8Array(NX * NZ); }
  box(cx, cz, w, d) {
    for (let iz = Math.floor((cz - d / 2 - Z0) / CELL); iz <= Math.floor((cz + d / 2 - Z0) / CELL); iz++)
      for (let ix = Math.floor((cx - w / 2 - X0) / CELL); ix <= Math.floor((cx + w / 2 - X0) / CELL); ix++)
        if (ix >= 0 && iz >= 0 && ix < NX && iz < NZ) this.b[iz * NX + ix] = 1;
  }
  bake() {
    for (let iz = 0; iz < NZ; iz++) for (let ix = 0; ix < NX; ix++) {
      let v = ix === 0 || iz === 0 || ix === NX - 1 || iz === NZ - 1 ? 1 : 0;
      for (let dz = -1; dz <= 1 && !v; dz++) for (let dx = -1; dx <= 1; dx++) { const x = ix + dx, z = iz + dz; if (x >= 0 && z >= 0 && x < NX && z < NZ && this.b[z * NX + x]) { v = 1; break; } }
      this.inf[iz * NX + ix] = v;
    }
  }
  cell(x, z) { return [Math.floor((x - X0) / CELL), Math.floor((z - Z0) / CELL)]; }
  at(i) { return [X0 + (i % NX + 0.5) * CELL, Z0 + (Math.floor(i / NX) + 0.5) * CELL]; }
  free(x, z) { const [ix, iz] = this.cell(x, z); return ix >= 0 && iz >= 0 && ix < NX && iz < NZ && !this.inf[iz * NX + ix]; }
  los(ax, az, bx, bz) { const n = Math.ceil(Math.hypot(bx - ax, bz - az) / 0.2); for (let i = 1; i < n; i++) { const t = i / n; if (!this.free(ax + (bx - ax) * t, az + (bz - az) * t)) return false; } return true; }
  nearestFree(x, z, r = 3) { if (this.free(x, z)) return [x, z]; let best = null, bd = 1e9; for (let dz = -r; dz <= r; dz += CELL) for (let dx = -r; dx <= r; dx += CELL) if (dx * dx + dz * dz < bd && this.free(x + dx, z + dz)) { bd = dx * dx + dz * dz; best = [x + dx, z + dz]; } return best; }
  // Waypoints from (fx, fz) to anywhere within `reach` of (gx, gz). [] = already there, null = no way.
  path(fx, fz, gx, gz, reach) {
    if (Math.hypot(gx - fx, gz - fz) <= reach * 0.85) return [];
    let [sx, sz] = this.cell(fx, fz), s = sz * NX + sx;
    if (this.inf[s]) { let best = -1, bd = 1e9; for (let dz = -4; dz <= 4; dz++) for (let dx = -4; dx <= 4; dx++) { const i = (sz + dz) * NX + sx + dx; if (sx + dx >= 0 && sx + dx < NX && sz + dz >= 0 && sz + dz < NZ && !this.inf[i] && dx * dx + dz * dz < bd) { bd = dx * dx + dz * dz; best = i; } } if (best < 0) return null; s = best; }
    const g = new Float32Array(NX * NZ).fill(1e9), par = new Int32Array(NX * NZ).fill(-1), done = new Uint8Array(NX * NZ), h = new Heap();
    const hf = (i) => { const [x, z] = this.at(i); return Math.max(0, Math.hypot(gx - x, gz - z) - reach * 0.85); };
    g[s] = 0; h.push(hf(s), s); let end = -1;
    while (h.size) {
      const [, i] = h.pop(); if (done[i]) continue; done[i] = 1;
      const [x, z] = this.at(i);
      if (Math.hypot(gx - x, gz - z) <= reach * 0.85) { end = i; break; }
      const ix = i % NX, iz = (i / NX) | 0;
      for (let dz = -1; dz <= 1; dz++) for (let dx = -1; dx <= 1; dx++) {
        if (!dx && !dz) continue;
        const nx = ix + dx, nz = iz + dz; if (nx < 0 || nz < 0 || nx >= NX || nz >= NZ) continue;
        const j = nz * NX + nx; if (this.inf[j] || done[j]) continue;
        if (dx && dz && (this.inf[iz * NX + nx] || this.inf[nz * NX + ix])) continue;
        const ng = g[i] + (dx && dz ? 1.414 : 1) * CELL;
        if (ng < g[j]) { g[j] = ng; par[j] = i; h.push(ng + hf(j), j); }
      }
    }
    if (end < 0) return null;
    const pts = []; for (let i = end; i >= 0; i = par[i]) pts.push(this.at(i)); pts.reverse();
    const out = []; let cur = [fx, fz], k = 0;
    while (k < pts.length) { let far = k; for (let j = pts.length - 1; j > k; j--) if (this.los(cur[0], cur[1], pts[j][0], pts[j][1])) { far = j; break; } out.push(pts[far]); cur = pts[far]; k = far + 1; }
    return out;
  }
  length(fx, fz, pts) { let l = 0, x = fx, z = fz; for (const p of pts) { l += Math.hypot(p[0] - x, p[1] - z); x = p[0]; z = p[1]; } return l; }
}

export function buildWorld(scene) {
  const group = new THREE.Group(); scene.add(group);
  const nav = new Nav(), ents = [], byId = {};
  const add = (o) => { ents.push(o); byId[o.id] = o; group.add(o.g); return o; };
  const solid = (cx, cz, w, d, h, c, y = 0) => { const m = box(w, h, d, c); m.position.set(cx, y + h / 2, cz); group.add(m); return m; };

  // ground and buildings
  const ground = (x0, x1, z0, z1, c, y = 0) => { const m = box(x1 - x0, 0.2, z1 - z0, c); m.position.set((x0 + x1) / 2, y - 0.1, (z0 + z1) / 2); group.add(m); };
  ground(-26, 28, -9, 0, 0x454a55); // road
  ground(-26, 28, 0, 7, 0xeadfc6); // pavement
  ground(-26, 28, 7, 18, 0xd9cfb8);
  for (let x = -22; x < 28; x += 8) { const m = box(3, 0.02, 0.25, 0xf6f0de); m.position.set(x, 0.01, -4.5); group.add(m); }
  solid(9, 12, 22, 10, 8, 0xbfe3d6); nav.box(9, 12, 22, 10); // records office, front at z = 7
  solid(-14, 11, 14, 8, 4.8, 0xf2c9a8); nav.box(-14, 11, 14, 8); // cafe, front at z = 7
  solid(9, 7.05, 22, 0.3, 0.5, 0xd0b88a, 3.2); // band over the door
  const kerb = box(54, 0.15, 0.3, 0xcfc4a8); kerb.position.set(1, 0.07, 0); group.add(kerb);

  const prop = (id, kind, x, z, build, r, h, o = {}) => {
    const g = new THREE.Group(); g.position.set(x, 0, z); const pick = [];
    build(g, pick);
    return add({ id, kind, ...TARGETS[kind], g, r, h, reach: r + 1.5, fixed: true, pick, used: new Set(), tint: [], ...o });
  };
  prop('door', 'door', 12, 7, (g, pick) => { const leaf = box(2, 3, 0.2, 0x6b8f86); leaf.position.set(0, 1.5, -0.1); g.add(leaf); pick.push(leaf); g.leaf = leaf; }, 1.0, 3.2, { reach: 2.6 });
  prop('cabinet', 'cabinet', 3, 1.8, (g, pick) => { const b = box(1.3, 1.5, 0.8, 0x2d4a52); b.position.y = 0.75; const t = box(1.4, 0.2, 0.9, 0xb89f72); t.position.y = 1.6; g.add(b, t); pick.push(b, t); g.tintMesh = b; }, 0.8, 1.7);
  nav.box(3, 1.8, 1.3, 0.8);
  prop('camera', 'camera', 6.5, 1.2, (g, pick) => { const p = new THREE.Mesh(new THREE.CylinderGeometry(0.12, 0.15, 4.5, 10), M(0xb9c4c8)); p.position.y = 2.25; const hd = box(0.9, 0.45, 0.45, 0x8fa3a8); hd.position.set(0, 4.6, 0.3); const big = new THREE.Mesh(new THREE.CylinderGeometry(0.6, 0.6, 5, 8), new THREE.MeshBasicMaterial({ visible: false })); big.position.y = 2.5; g.add(p, hd, big); pick.push(p, hd, big); g.tintMesh = hd; }, 0.4, 5);
  nav.box(6.5, 1.2, 0.4, 0.4);
  prop('car', 'car', 14, -3, (g, pick) => { const b = box(4.4, 0.9, 1.7, 0x3d9a96); b.position.y = 0.7; const c = box(2.2, 0.8, 1.5, 0x9fd8d2); c.position.set(-0.2, 1.45, 0); g.add(b, c); for (const [dx, dz] of [[-1.4, 0.9], [1.4, 0.9], [-1.4, -0.9], [1.4, -0.9]]) { const w = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.35, 0.25, 12), M(0x22262b)); w.rotation.x = Math.PI / 2; w.position.set(dx, 0.35, dz); g.add(w); } pick.push(b, c); g.tintMesh = b; }, 1.4, 1.9, { reach: 3.2 });
  nav.box(14, -3, 4.4, 1.7);
  nav.bake();

  const person = (id, kind, x, z, color, o = {}) => {
    const g = new THREE.Group(), pose = new THREE.Group(); g.add(pose); g.position.set(x, 0, z);
    const body = new THREE.Mesh(new THREE.CapsuleGeometry(0.32, 1.0, 4, 10), M(color)); body.position.y = 0.82;
    const nose = box(0.16, 0.12, 0.3, 0xffffff); nose.position.set(0, 1.25, 0.3);
    pose.add(body, nose); g.pose = pose;
    return add({ id, kind, ...TARGETS[kind], g, r: 0.4, h: 1.95, reach: 1.9, pick: [body, nose], used: new Set(), tint: [], body, state: 'ok', ...o });
  };
  person('guard', 'guard', 10.4, 5.2, 0xd9a441);
  person('inez', 'inez', -9.5, 5.4, 0xe88aa0);
  person('driver', 'driver', 11.2, -0.9, 0x2f8f8a);
  person('police1', 'police', -3, -3, 0x34508f); person('police2', 'police', -1, -5, 0x34508f);
  person('rival1', 'rival', 20.5, -1.5, 0x8a5cf6); person('rival2', 'rival', 22.5, -4.5, 0x8a5cf6);
  [[-18, 17, 1.6], [16, -14, 3.4], [-10, 20, 5.6], [22, -6, 2.4]].forEach(([a, b, z], i) => {
    const w = person(`walker${i + 1}`, 'walker', a + (b - a) * (i / 5), z, [0xc9c2b0, 0xb8a9c9, 0xa9c4b0, 0xc9b79c][i], { walker: { a, b, z, dir: i % 2 ? -1 : 1, wait: 0 } });
    w.g.rotation.y = w.walker.dir > 0 ? Math.PI / 2 : -Math.PI / 2;
  });
  const agents = ROLES.map((r, i) => {
    const e = person(`cell${i}`, 'cell', -7 + i * 1.4, 2.4 + (i % 2) * 1.3, new THREE.Color(r.color).getHex(), { acts: [], name: r.name, role: r.key, agent: true, speed: 0, order: null });
    e.g.rotation.y = Math.PI / 2; e.ringMark = true; return e;
  });

  const tmp = new THREE.Vector3();
  function update(dt, t) {
    for (const e of ents) {
      if (e.walker && e.state === 'ok' && !e.frozen) {
        const w = e.walker; if (w.wait > 0) { w.wait -= dt; continue; }
        const p = e.g.position, to = w.dir > 0 ? w.b : w.a;
        p.x += w.dir * 1.1 * dt; p.z = w.z;
        if ((to - p.x) * w.dir <= 0) { w.dir *= -1; w.wait = 1 + ((t * 7 + e.id.length) % 2); e.g.rotation.y = w.dir > 0 ? Math.PI / 2 : -Math.PI / 2; }
      }
      if (e.id === 'door') e.g.leaf.position.x += ((e.open ? 1.9 : 0) - e.g.leaf.position.x) * Math.min(1, dt * 4);
    }
  }
  const raise = (e, s) => { e.g.pose && (e.g.pose.rotation.x = s ? -Math.PI / 2 : 0, e.g.pose.position.y = s ? 0.4 : 0); };
  const tint = (e, c) => { const m = (e.body || e.g.tintMesh); if (m) m.material.emissive = new THREE.Color(c); };
  return { group, ents, byId, agents, nav, update, raise, tint, dispose: () => scene.remove(group) };
}
