// What a camera and a guard can see: view cones on the ground (cones only, no all-round view for machines).
// Ranges are scaled to this small street. The camera sweeps; the guard watches the street.
import * as THREE from './vendor/three.module.min.js';
import { G } from './state.js';

const SRC = [
  { id: 'camera', color: 0xff5a4a, range: 13, half: 0.55, heading: (t) => { const a = G.noSweep ? 0 : Math.sin(t * 0.45) * 0.9; return [Math.sin(a), -Math.cos(a)]; }, on: (e) => !e.used.has('loop') },
  { id: 'guard', color: 0xffb02e, range: 9, half: 0.9, heading: () => [-0.5, -1], on: (e) => e.state === 'ok' },
];

export function makeCones(world) {
  const cones = SRC.map((s) => {
    const g = new THREE.Group(), geo = new THREE.CircleGeometry(s.range, 28, -s.half, s.half * 2);
    const fan = new THREE.Mesh(geo, new THREE.MeshBasicMaterial({ color: s.color, transparent: true, opacity: 0.26, depthTest: false, depthWrite: false, side: THREE.DoubleSide, toneMapped: false }));
    fan.rotation.x = -Math.PI / 2; fan.renderOrder = 4; g.add(fan); world.group.add(g); return { ...s, g, ent: world.byId[s.id], dir: [0, -1] };
  });
  return {
    update(t) {
      for (const c of cones) {
        c.dir = c.heading(t); const on = c.on(c.ent); c.g.visible = on; c.active = on;
        c.g.position.set(c.ent.g.position.x, 0.05, c.ent.g.position.z); c.g.rotation.y = -Math.atan2(c.dir[1], c.dir[0]);
      }
    },
    // Which source sees this ground point, if any.
    seenAt(x, z) {
      for (const c of cones) {
        if (!c.active) continue;
        const dx = x - c.ent.g.position.x, dz = z - c.ent.g.position.z, d = Math.hypot(dx, dz);
        if (d <= c.range && d > 0.01 && Math.acos(Math.max(-1, Math.min(1, (dx * c.dir[0] + dz * c.dir[1]) / d))) <= c.half) return c.id;
      }
      return null;
    },
  };
}
