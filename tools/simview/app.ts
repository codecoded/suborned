// The look tool's page (brief 081): plays dumps of real engine runs in low-poly 3D. Reads only what the dump wrote; it decides nothing about the game.
// A run is {script, seed}; seed is null until brief 080 (the picker's slot is in the run list).
// @ts-ignore vendored three has no types
import * as THREE_ from '../decokit/vendor/testbed/vendor/three-0.186.1/three.module.min.js';
import type { Scene as WorldScene } from './scene';
const THREE: any = THREE_;

const $ = (id: string) => document.getElementById(id)!;
const el = (tag: string, cls = '', html = '') => { const e = document.createElement(tag); if (cls) e.className = cls; if (html) e.innerHTML = html; return e; };
const esc = (s: string) => s.replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]!));
const clock = (t: number) => { const s = Math.floor(t / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`; };
const clockT = (t: number) => `${clock(t)}.${Math.floor(t % 10)}`;
const manifest = JSON.parse($('manifest').textContent!);

async function unpack(id: string): Promise<any> {
  const bin = Uint8Array.from(atob($(id).textContent!), c => c.charCodeAt(0));
  const stream = new Blob([bin]).stream().pipeThrough(new DecompressionStream('gzip'));
  return JSON.parse(await new Response(stream).text());
}

// ---- colours by side
const SIDE: Record<string, { c: string; name: string }> = {
  cell: { c: '#18c8ff', name: 'Cell' }, guard: { c: '#ff9f1c', name: 'Guard / patrol' }, police: { c: '#2747ff', name: 'Police' }, robot: { c: '#c8ced4', name: 'Robots' },
  rival: { c: '#a23cf0', name: 'Rivals' }, vale: { c: '#ffe100', name: 'Vale' }, named: { c: '#27c65c', name: 'Inez / driver' }, civ: { c: '#bdb7ab', name: 'Civilians' },
};
const FAN: Record<string, number> = { camera: 0xff3b30, guard: 0xff9f1c, patrol: 0xff9f1c, security: 0x2747ff, rivals: 0xa23cf0 };
const DOWN = 1, DRAWN = 2, RUNNING = 4, DEAD = 8, TURNED = 16;
const ALERT = ['routine', 'inquiry', 'search', 'lockdown'];

// plan (metres) to three: X = x, Z = -y, so a map read from above is not mirrored. Block centimetres (the dump) to plan by the walk file's frame.
const bx = (y: number) => -130 + y / 100, bz = (x: number) => 230 + x / 100;

// ---- geometry: one flat-shaded mesh per group, by hand (triangles with vertex colours)
class Tris {
  pos: number[] = []; col: number[] = [];
  private tmp = new THREE.Color();
  tri(a: number[], b: number[], c: number[], col: string) {
    this.pos.push(...a, ...b, ...c); this.tmp.set(col); const { r, g, b: bl } = this.tmp; for (let i = 0; i < 3; i++) this.col.push(r, g, bl);
  }
  ring(ring: [number, number][], z: number, col: string) {
    const r = ccw(ring); const pts = r.map(p => new THREE.Vector2(p[0], p[1]));
    for (const f of THREE.ShapeUtils.triangulateShape(pts, [])) this.tri(v3(r[f[0]]!, z), v3(r[f[1]]!, z), v3(r[f[2]]!, z), col);
  }
  prism(ring: [number, number][], base: number, top: number, col: string, shade = 0.9) {
    const r = ccw(ring); this.ring(r, top, col);
    const side = '#' + new THREE.Color(col).multiplyScalar(shade).getHexString();
    for (let i = 0; i < r.length; i++) {
      const a = r[i]!, b = r[(i + 1) % r.length]!;
      this.tri(v3(a, base), v3(b, base), v3(b, top), side); this.tri(v3(a, base), v3(b, top), v3(a, top), side);
    }
  }
  box(x: number, y: number, z: number, sx: number, sy: number, sz: number, yaw: number, col: string) {
    const c = Math.cos(yaw), s = Math.sin(yaw), hx = sx / 2, hy = sy / 2;
    const ring: [number, number][] = [[-hx, -hy], [hx, -hy], [hx, hy], [-hx, hy]].map(([u, v]) => [x + u! * c - v! * s, y + u! * s + v! * c]);
    this.prism(ring, z, z + sz, col);
  }
  cone(x: number, y: number, z: number, r: number, h: number, col: string, n = 6) {
    for (let i = 0; i < n; i++) {
      const a = i / n * Math.PI * 2, b = (i + 1) / n * Math.PI * 2;
      this.tri(v3([x + r * Math.cos(a), y + r * Math.sin(a)], z), v3([x + r * Math.cos(b), y + r * Math.sin(b)], z), v3([x, y], z + h), col);
    }
  }
  mesh(mat: any) {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(this.pos, 3)); g.setAttribute('color', new THREE.Float32BufferAttribute(this.col, 3)); g.computeVertexNormals();
    return new THREE.Mesh(g, mat);
  }
}
const v3 = (p: [number, number] | number[], z: number) => [p[0]!, z, -p[1]!];
function ccw(r: [number, number][]) { let a = 0; for (let i = 0; i < r.length; i++) { const p = r[i]!, q = r[(i + 1) % r.length]!; a += p[0] * q[1] - q[0] * p[1]; } return a >= 0 ? r : [...r].reverse(); }

// ---- the static world
const renderer = new THREE.WebGLRenderer({ canvas: $('gl'), antialias: true, preserveDrawingBuffer: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2)); renderer.autoClear = false; renderer.setScissorTest(true);
const scene3 = new THREE.Scene(); scene3.background = new THREE.Color(0xa8c8d8);
scene3.add(new THREE.HemisphereLight(0xffffff, 0xb0a890, 1.9));
const sun = new THREE.DirectionalLight(0xfff2dc, 1.7); sun.position.set(-60, 120, 40); scene3.add(sun);
const flatMat = new THREE.MeshLambertMaterial({ vertexColors: true });
let wallsGroup: any, gridMesh: any, coverMesh: any, sceneData: WorldScene;

function buildWorld(s: WorldScene) {
  sceneData = s;
  const ground = new Tris(), walls = new Tris(), props = new Tris();
  for (const [ring, z, col] of s.flats) ground.ring(ring, z, col);
  for (const [ring, base, h, col] of s.solids) props.prism(ring, base, base + h, col);
  for (const [ring, base, h, col] of s.walls) walls.prism(ring, base, base + h, col);
  for (const [x, y, z, sx, sy, sz, yaw, col] of s.boxes) (z > 6 ? walls : props).box(x, y, z, sx, sy, sz, yaw, col);
  for (const [x, y, z, h] of s.trees) { props.box(x, y, z, 0.3, 0.3, h * 0.45, 0, '#7a5a3c'); props.cone(x, y, z + h * 0.3, h * 0.28, h * 0.72, '#5aa860'); }
  for (const c of s.cameras) { props.box(c.at[0], c.at[1], 0, 0.25, 0.25, 4, 0, '#444'); props.box(c.at[0], c.at[1], 4, 0.8, 0.8, 0.5, 0, '#ff3b30'); }
  scene3.add(ground.mesh(flatMat)); scene3.add(props.mesh(flatMat));
  const can = new Tris(); for (const [ring, base, top, col] of s.canopies) can.prism(ring, base, top, col);
  const canM = can.mesh(new THREE.MeshLambertMaterial({ vertexColors: true, transparent: true, opacity: 0.3, depthWrite: false })); canM.renderOrder = 3; scene3.add(canM);
  wallsGroup = walls.mesh(flatMat); scene3.add(wallsGroup);
  // the walk grid (a debug overlay): what the engine's A* can use
  const cv = document.createElement('canvas'); cv.width = s.grid.nx; cv.height = s.grid.ny; const cx = cv.getContext('2d')!;
  s.grid.rows.forEach((row: string, ix: number) => { for (let iy = 0; iy < row.length; iy++) if (row[iy] === '#') { cx.fillStyle = 'rgba(200,20,60,.55)'; cx.fillRect(ix, s.grid.ny - 1 - iy, 1, 1); } });
  const tex = new THREE.CanvasTexture(cv); tex.magFilter = tex.minFilter = THREE.NearestFilter;
  const cell = s.grid.cell; // block frame: rows run along block y, columns along block x (Grid.cs); the plane is placed by its corners
  const gm = new THREE.BufferGeometry();
  const X0 = s.grid.x0, Y0 = s.grid.y0, X1 = X0 + cell * s.grid.nx, Y1 = Y0 + cell * s.grid.ny, H = 0.24;
  const c = (x: number, y: number) => [bx(y), H, bz(x)];
  gm.setAttribute('position', new THREE.Float32BufferAttribute([...c(X0, Y0), ...c(X1, Y0), ...c(X1, Y1), ...c(X0, Y1)], 3));
  gm.setAttribute('uv', new THREE.Float32BufferAttribute([0, 0, 1, 0, 1, 1, 0, 1], 2)); gm.setIndex([0, 1, 2, 0, 2, 3]);
  gridMesh = new THREE.Mesh(gm, new THREE.MeshBasicMaterial({ map: tex, transparent: true, side: THREE.DoubleSide, depthWrite: false })); gridMesh.visible = false; scene3.add(gridMesh);
  const cov = new Tris(); for (const [x, y] of s.cover) cov.box(x, y, 0.2, 0.35, 0.35, 0.3, Math.PI / 4, '#00d0a0');
  coverMesh = cov.mesh(flatMat); coverMesh.visible = false; scene3.add(coverMesh);
}

// ---- a run, decoded
type Ent = { id: string; label: string; side: string; cls: string; detail: string; hpMax: number; step: number; spans: { t: number; xs: Float32Array; zs: Float32Array }[]; stT: number[]; stF: number[]; stX: string[]; hpT: number[]; hpV: number[]; faceT: number[]; faceV: number[]; idx: number; shotT: number[]; shotI: number[] };
function ub(a: number[], t: number) { let lo = 0, hi = a.length; while (lo < hi) { const m = (lo + hi) >> 1; if (a[m]! <= t) lo = m + 1; else hi = m; } return lo - 1; }
class Run {
  ents: Ent[] = []; byId = new Map<string, Ent>(); d: any; meta: any; end = 0;
  shotT: number[] = []; logT: number[] = []; hudT: number[]; hudV: string[][]; winT: number[]; carKeyT: number[][] = []; fans: { id: string; kind: string; t: number[]; v: string[]; polys: Map<number, any> }[] = [];
  constructor(d: any, meta: any) {
    this.d = d; this.meta = meta; this.end = d.lastSample;
    for (const e of d.entities) {
      const spans = e.spans.map((s: number[]) => {
        const n = (s.length - 3) / 2 + 1, xs = new Float32Array(n), zs = new Float32Array(n); let x = s[1]!, y = s[2]!;
        xs[0] = bx(y); zs[0] = bz(x);
        for (let i = 1; i < n; i++) { x += s[1 + 2 * i]!; y += s[2 + 2 * i]!; xs[i] = bx(y); zs[i] = bz(x); }
        return { t: s[0]!, xs, zs };
      });
      const ent: Ent = { id: e.id, label: e.label, side: e.side, cls: e.cls, detail: e.detail, hpMax: e.hpMax, step: e.step, spans, stT: e.st.map((x: any[]) => x[0]), stF: e.st.map((x: any[]) => x[1]), stX: e.st.map((x: any[]) => x[2]), hpT: [], hpV: [], faceT: [], faceV: [], idx: this.ents.length, shotT: [], shotI: [] };
      for (let i = 0; i < e.hp.length; i += 2) { ent.hpT.push(e.hp[i]); ent.hpV.push(e.hp[i + 1]); }
      for (let i = 0; i < e.face.length; i += 2) { ent.faceT.push(e.face[i]); ent.faceV.push(e.face[i + 1]); }
      this.ents.push(ent); this.byId.set(e.id, ent);
    }
    d.shots.forEach((s: any[], i: number) => {
      this.shotT.push(s[0]); const by = this.byId.get(s[1]); if (by) { by.shotT.push(s[0]); by.shotI.push(i); }
      const tg = this.byId.get(s[2]); // the crowd's health comes from the shots (the dump never makes a crowd person's gun)
      if (tg && s[7] && tg.side === 'civ') { const now = tg.hpV.length ? tg.hpV[tg.hpV.length - 1]! : tg.hpMax; tg.hpT.push(s[0]); tg.hpV.push(now - s[8]); }
    });
    this.logT = d.log.map((l: any[]) => l[0]); this.hudT = d.hud.map((h: any[]) => h[0]); this.hudV = d.hud.map((h: any[]) => h[1].split(','));
    this.winT = d.windows.map((h: any[]) => h[0]);
    this.carKeyT = d.cars.map((c: any) => c.keys.map((k: number[]) => k[0]));
    this.fans = d.fans.map((f: any) => ({ id: f.id, kind: f.kind, t: f.keys.map((k: any[]) => k[0]), v: f.keys.map((k: any[]) => k[1]), polys: new Map() }));
  }
  pos(e: Ent, t: number, out: { x: number; z: number }): boolean {
    for (let i = e.spans.length - 1; i >= 0; i--) {
      const s = e.spans[i]!; if (t < s.t) continue;
      const u = (t - s.t) / e.step, k = Math.floor(u), n = s.xs.length;
      if (k >= n) return false;
      if (k === n - 1) { if (u - k > 1 && e.step === 1) return false; out.x = s.xs[k]!; out.z = s.zs[k]!; return true; }
      const f = u - k; out.x = s.xs[k]! + (s.xs[k + 1]! - s.xs[k]!) * f; out.z = s.zs[k]! + (s.zs[k + 1]! - s.zs[k]!) * f; return true;
    }
    return false;
  }
  st(e: Ent, t: number) { const k = Math.max(0, ub(e.stT, t)); return { f: e.stF[k] ?? 0, x: e.stX[k] ?? '' }; }
  hp(e: Ent, t: number) { const k = ub(e.hpT, t); return k < 0 ? e.hpMax : e.hpV[k]!; }
}
const runs: (Run | null)[] = manifest.runs.map(() => null);
async function getRun(i: number): Promise<Run> { if (!runs[i]) runs[i] = new Run(await unpack(`run-${i}`), manifest.runs[i]); return runs[i]!; }

// ---- figures
function figureGeo() {
  const t = new Tris(); const col = '#ffffff';
  // a low-poly person from a few prisms (plan x forward): legs/body tapering, head, a nose so the facing reads. Unit height 1.8 m
  const ring = (r: number, n = 6): [number, number][] => Array.from({ length: n }, (_, i) => [r * Math.cos(i / n * Math.PI * 2), r * Math.sin(i / n * Math.PI * 2)]);
  t.prism(ring(0.2), 0, 1.0, col, 0.82); t.prism(ring(0.27), 1.0, 1.42, col, 1);
  t.prism(ring(0.14), 1.42, 1.78, '#ffe8d4', 0.95);
  t.box(0.2, 0, 1.5, 0.22, 0.12, 0.1, 0, '#333333');
  return t;
}
const figGeoT = figureGeo();
const makeGeo = (t: Tris) => { const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(t.pos, 3)); g.setAttribute('color', new THREE.Float32BufferAttribute(t.col, 3)); g.computeVertexNormals(); return g; };
const figGeo = makeGeo(figGeoT);
const ringGeo = (() => { const g = new THREE.RingGeometry(0.42, 0.62, 16); g.rotateX(-Math.PI / 2); return g; })();
const boxGeo = new THREE.BoxGeometry(1, 1, 1);
const gunGeo = (() => { const g = new THREE.BoxGeometry(0.75, 0.09, 0.09); g.translate(0.45, 0, 0); return g; })();
const tmpM = new THREE.Matrix4(), tmpQ = new THREE.Quaternion(), tmpE = new THREE.Euler(), tmpS = new THREE.Vector3(), tmpP = new THREE.Vector3(), tmpC = new THREE.Color();
const FIG_SCALE = 1.25;

// ---- the shared view
const view = { target: new THREE.Vector3(-156, 0, 262), yaw: 210, pitch: 38, dist: 105, follow: '', cones: false, labels: true, crowd: true, walls: 'half', paths: true, grid: false, orders: false };
const VIEWS: Record<string, { yaw: number; pitch: number; dist: number }> = { top: { yaw: 0, pitch: 89.5, dist: 150 }, play: { yaw: 210, pitch: 38, dist: 62 } };
const time = { t: 0, playing: true, speed: 2 };
const panels: Panel[] = [];

class Panel {
  run: Run; group = new THREE.Group(); cam = new THREE.PerspectiveCamera(32, 1, 1, 1200); rect = { x: 0, y: 0, w: 1, h: 1 };
  figs: any; rings: any; guns: any; tracers: any; cars: any[] = []; flames: any; blast: any; beacon: any; pathPast: any; pathFuture: any; pathFor = '';
  root: HTMLElement; hud: HTMLElement; ticker: HTMLElement; result: HTMLElement; tags = new Map<string, HTMLElement>(); places: HTMLElement[] = []; tagBox: HTMLElement;
  fanMeshes = new Map<string, any>(); fanGroup = new THREE.Group(); lastHud = ''; lastTick = ''; focus = new THREE.Vector3(); pv = { x: 0, z: 0 };
  constructor(run: Run) {
    this.run = run; const n = run.ents.length;
    const figMat = new THREE.MeshLambertMaterial({ vertexColors: true });
    this.figs = new THREE.InstancedMesh(figGeo, figMat, n); this.figs.frustumCulled = false;
    this.rings = new THREE.InstancedMesh(ringGeo, new THREE.MeshBasicMaterial({ transparent: true, opacity: 0.95, depthWrite: false }), n); this.rings.frustumCulled = false;
    this.guns = new THREE.InstancedMesh(gunGeo, new THREE.MeshBasicMaterial({ color: 0x15181c }), n); this.guns.frustumCulled = false;
    this.tracers = new THREE.InstancedMesh(boxGeo, new THREE.MeshBasicMaterial({ transparent: true, depthWrite: false }), 96); this.tracers.frustumCulled = false;
    for (let i = 0; i < n; i++) { const e = run.ents[i]!; tmpC.set(SIDE[e.side]?.c ?? '#fff'); this.figs.setColorAt(i, tmpC); this.rings.setColorAt(i, tmpC); }
    this.group.add(this.figs, this.rings, this.guns, this.tracers, this.fanGroup);
    const L = run.d.carLenCm / 100, W = run.d.carWCm / 100;
    for (const c of run.d.cars) {
      const col = c.id === 'car:getaway' ? '#0e8fbf' : c.id === 'car:rivals' ? '#6b1fb0' : '#20242c';
      const t = new Tris(); t.box(0, 0, 0.25, L, W, 0.65, 0, col); t.box(-L * 0.08, 0, 0.9, L * 0.5, W * 0.85, 0.5, 0, '#b8dbe8'); t.box(L * 0.42, 0, 0.5, 0.15, W * 0.9, 0.2, 0, '#ffe9a0');
      const m = new THREE.Mesh(makeGeo(t), new THREE.MeshLambertMaterial({ vertexColors: true })); m.visible = false; this.cars.push(m); this.group.add(m);
    }
    this.flames = new THREE.Group();
    const fl = new THREE.Mesh(new THREE.ConeGeometry(0.9, 2.6, 6), new THREE.MeshBasicMaterial({ color: 0xff7a1a })); fl.position.y = 2.2; this.flames.add(fl); this.flames.visible = false; this.group.add(this.flames);
    this.blast = new THREE.Mesh(new THREE.IcosahedronGeometry(1, 1), new THREE.MeshBasicMaterial({ color: 0xffb030, transparent: true, opacity: 0.6, depthWrite: false })); this.blast.visible = false; this.group.add(this.blast);
    this.beacon = new THREE.Mesh(new THREE.OctahedronGeometry(0.42), new THREE.MeshBasicMaterial({ color: 0xffe100 })); this.beacon.visible = false; this.group.add(this.beacon);
    this.pathPast = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineBasicMaterial({ color: 0xffffff })); this.pathFuture = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.35 }));
    this.pathPast.frustumCulled = this.pathFuture.frustumCulled = false; this.group.add(this.pathPast, this.pathFuture);
    scene3.add(this.group);
    this.root = el('div', 'panel'); this.hud = el('div', 'hud'); this.ticker = el('div', 'ticker'); this.result = el('div', 'result'); this.tagBox = el('div');
    this.root.append(this.tagBox, this.hud, this.ticker, this.result); $('overlays').append(this.root);
    for (const [x, y, name, kind] of [...sceneData.labels].sort((a, b) => +(b[3] === 'big') - +(a[3] === 'big') || +(b[3] === 'door') - +(a[3] === 'door'))) { const d = el('div', 'place' + (kind === 'big' ? ' big' : ''), esc(name)); (d as any)._w = [x, y]; this.places.push(d); this.tagBox.append(d); }
  }
  dispose() {
    scene3.remove(this.group); this.root.remove();
    for (const m of [this.figs, this.rings, this.guns, this.tracers]) m.dispose?.();
  }
  setRect(x: number, y: number, w: number, h: number, totalH: number) {
    this.rect = { x, y: totalH - y - h, w, h }; this.cam.aspect = w / h; this.cam.updateProjectionMatrix();
    Object.assign(this.root.style, { left: x + 'px', top: y + 'px', width: w + 'px', height: h + 'px' });
  }
  facing(e: Ent, t: number): number {
    // shooting: at the target; else the way the dump's facing says, else the way they last moved
    const k = ub(e.shotT, t);
    if (k >= 0 && t - e.shotT[k]! < 25) { const s = this.run.d.shots[e.shotI[k]!]; return Math.atan2(-(bz(s[5]) - bz(s[3])), bx(s[6]) - bx(s[4])); }
    const a = { x: 0, z: 0 }, b = { x: 0, z: 0 };
    if (this.run.pos(e, t, b)) for (const back of [3, 8]) if (this.run.pos(e, t - back, a) && (a.x !== b.x || a.z !== b.z)) { const dx = b.x - a.x, dz = b.z - a.z; if (dx * dx + dz * dz > 0.01) return Math.atan2(-dz, dx); }
    const f = ub(e.faceT, t);
    // the dump's facing is in the block frame (x south, y east): plan dir = (fy, -fx)
    if (f >= 0) { const d = e.faceV[f]! * Math.PI / 180, fx = Math.cos(d), fy = Math.sin(d); return Math.atan2(-fx, fy); }
    return 0;
  }
  update(t: number) {
    const run = this.run, tick = Math.min(t, run.end), n = run.ents.length, p = this.pv;
    const sel = view.follow;
    this.beacon.visible = false;
    for (let i = 0; i < n; i++) {
      const e = run.ents[i]!, vis = run.pos(e, tick, p) && (view.crowd || e.side !== 'civ' || e.id === sel);
      if (!vis) { tmpM.makeScale(0, 0, 0); this.figs.setMatrixAt(i, tmpM); this.rings.setMatrixAt(i, tmpM); this.guns.setMatrixAt(i, tmpM); continue; }
      const st = run.st(e, tick), down = !!(st.f & DOWN), yaw = this.facing(e, tick);
      tmpP.set(p.x, 0.18, p.z);
      const sc = e.side === 'civ' ? 1.05 : FIG_SCALE;
      if (down) { tmpE.set(0, yaw, -Math.PI / 2, 'YXZ'); tmpP.y = 0.32; tmpC.set(SIDE[e.side]?.c ?? '#fff').multiplyScalar(st.f & DEAD ? 0.28 : 0.5); }
      else { tmpE.set(0, yaw, 0, 'YXZ'); tmpC.set(SIDE[e.side]?.c ?? '#fff'); }
      tmpQ.setFromEuler(tmpE); tmpS.setScalar(sc); tmpM.compose(tmpP, tmpQ, tmpS); this.figs.setMatrixAt(i, tmpM); this.figs.setColorAt(i, tmpC);
      // the ground ring: side colour for everyone who matters; white for the followed; none for the crowd unless picked
      const showRing = e.side !== 'civ' || e.id === sel || (st.f & TURNED);
      const rs = !showRing || down && e.id !== sel ? 0 : e.id === sel ? 1.8 : 1.15;
      tmpP.set(p.x, 0.26, p.z); tmpQ.identity(); tmpS.setScalar(rs); tmpM.compose(tmpP, tmpQ, tmpS); this.rings.setMatrixAt(i, tmpM);
      if (e.id === sel) tmpC.set('#ffffff'); else tmpC.set(SIDE[e.side]?.c ?? '#fff'); this.rings.setColorAt(i, tmpC);
      if (st.f & DRAWN && !down) { tmpP.set(p.x, 1.45 * sc, p.z); tmpE.set(0, yaw, 0, 'YXZ'); tmpQ.setFromEuler(tmpE); tmpS.setScalar(sc); tmpM.compose(tmpP, tmpQ, tmpS); } else tmpM.makeScale(0, 0, 0);
      this.guns.setMatrixAt(i, tmpM);
      if (e.side === 'vale') { this.beacon.visible = true; this.beacon.position.set(p.x, down ? 1.4 : 3.1, p.z); this.beacon.rotation.y = t * 0.08; }
    }
    this.figs.instanceMatrix.needsUpdate = this.rings.instanceMatrix.needsUpdate = this.guns.instanceMatrix.needsUpdate = true;
    this.figs.instanceColor.needsUpdate = this.rings.instanceColor.needsUpdate = true;
    this.updateCars(tick); this.updateTracers(t); this.updateFans(tick); this.updatePath(tick);
  }
  updateCars(t: number) {
    const run = this.run; this.flames.visible = false; this.blast.visible = false;
    run.d.cars.forEach((c: any, i: number) => {
      const m = this.cars[i], ks = run.carKeyT[i]!, k = ub(ks, t); const key = k >= 0 ? c.keys[k] : null;
      if (!key || key.length < 5) { m.visible = false; return; }
      let x = key[1], y = key[2]; const nxt = k + 1 < ks.length ? c.keys[k + 1] : null;
      if (nxt && nxt.length >= 5 && nxt[0] - key[0] === 1) { const f = t - key[0]; x += (nxt[1] - x) * f; y += (nxt[2] - y) * f; }
      m.visible = true; m.position.set(bx(y), 0.05, bz(x));
      // heading in the block frame: plan direction (hy, -hx); the car's mesh runs along plan x
      m.rotation.y = Math.atan2(-key[3], key[4]);
      const stK = c.st.filter((s: any[]) => s[0] <= t); const st = stK.length ? stK[stK.length - 1][1] : 'ok', since = stK.length ? stK[stK.length - 1][0] : 0;
      m.material.color.set(st === 'blown' ? 0x151515 : st === 'burning' ? 0x884422 : st === 'disabled' ? 0x999999 : 0xffffff);
      if (st === 'burning') { this.flames.visible = true; this.flames.position.copy(m.position); this.flames.scale.setScalar(0.9 + 0.25 * Math.sin(t * 0.9)); }
      if (st === 'blown' && t - since < 14) { this.blast.visible = true; this.blast.position.copy(m.position); const u = (t - since) / 14; this.blast.scale.setScalar(Math.max(2.5, run.d.carBlastCm / 100 * u)); this.blast.material.opacity = 0.75 * (1 - u); }
    });
  }
  updateTracers(t: number) {
    const shots = this.run.d.shots, T = this.run.shotT; const LIFE = Math.max(3.5, time.speed * 1.2); let k = 0;
    const hi = ub(T, t); const col = new THREE.Color();
    for (let i = hi; i >= 0 && k < 96; i--) {
      const s = shots[i], age = t - s[0]; if (age > LIFE) break;
      const hit = !!s[7], ax = bx(s[4]), az = bz(s[3]); let tx = bx(s[6]), tz = bz(s[5]);
      if (!hit) { const off = ((s[0] * 7 + i * 13) % 11 - 5) * 0.28, off2 = ((s[0] * 3 + i * 5) % 9 - 4) * 0.28; tx += off; tz += off2; } // a miss lands beside the target: the engine records only that it missed
      const dx = tx - ax, dz = tz - az, len = Math.hypot(dx, dz) || 0.01, th = hit ? 0.14 : 0.06;
      tmpP.set((ax + tx) / 2, 1.5, (az + tz) / 2); tmpE.set(0, Math.atan2(-dz, dx), 0, 'YXZ'); tmpQ.setFromEuler(tmpE); tmpS.set(len, th, th); tmpM.compose(tmpP, tmpQ, tmpS);
      this.tracers.setMatrixAt(k, tmpM); col.set(hit ? '#ff3020' : '#f4f4f4'); this.tracers.setColorAt(k, col); k++;
    }
    this.tracers.count = k; this.tracers.instanceMatrix.needsUpdate = true; if (this.tracers.instanceColor) this.tracers.instanceColor.needsUpdate = true;
  }
  updateFans(t: number) {
    const sel = view.follow; this.fanGroup.visible = view.cones || !!sel;
    if (!this.fanGroup.visible) return;
    for (const f of this.run.fans) {
      let m = this.fanMeshes.get(f.id); const on = view.cones || f.id === sel;
      const k = on ? ub(f.t, t) : -1; const v = k >= 0 ? f.v[k]! : '';
      if (!v) { if (m) m.visible = false; continue; }
      if (!m) { m = new THREE.Mesh(new THREE.BufferGeometry(), new THREE.MeshBasicMaterial({ transparent: true, depthWrite: false, side: THREE.DoubleSide })); m.renderOrder = 2; this.fanMeshes.set(f.id, m); this.fanGroup.add(m); }
      m.visible = true;
      if (m._key !== k) {
        m._key = k; let g = f.polys.get(k);
        if (!g) {
          const live = v[0] === '1', nums = v.slice(2).split(',').map(Number); const pts: any[] = [], flat: number[] = [];
          for (let i = 0; i < nums.length; i += 2) { const X = bx(nums[i + 1]! * 10), Z = bz(nums[i]! * 10); pts.push(new THREE.Vector2(X, -Z)); flat.push(X, 0.3, Z); }
          const g2 = new THREE.BufferGeometry(); g2.setAttribute('position', new THREE.Float32BufferAttribute(flat, 3));
          g2.setIndex(THREE.ShapeUtils.triangulateShape(pts, []).flat());
          g = { g: g2, live }; f.polys.set(k, g);
        }
        m.geometry = g.g; m.material.color.setHex(g.live ? (FAN[f.kind] ?? 0xffffff) : 0x777777); m.material.opacity = g.live ? 0.22 : 0.1;
      }
    }
  }
  updatePath(t: number) {
    const sel = view.follow, e = sel ? this.run.byId.get(sel) : undefined;
    if (!e || !view.paths) { this.pathPast.visible = this.pathFuture.visible = false; return; }
    if (this.pathFor !== sel) {
      this.pathFor = sel; const past: number[] = [];
      for (const s of e.spans) for (let i = 0; i < s.xs.length; i += 5) past.push(s.xs[i]!, 0.4, s.zs[i]!);
      const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(past, 3)); this.pathPast.geometry = g; this.pathFuture.geometry = g.clone();
    }
    let cut = 0; for (const s of e.spans) { if (t < s.t) break; cut += Math.min(s.xs.length, Math.ceil((t - s.t) / e.step / 5) + 1); }
    const total = this.pathPast.geometry.attributes.position.count;
    this.pathPast.geometry.setDrawRange(0, Math.min(cut, total)); this.pathFuture.geometry.setDrawRange(Math.max(0, Math.min(cut, total) - 1), total);
    this.pathPast.visible = this.pathFuture.visible = true;
  }
  /** the HUD, ticker, tags: DOM, redrawn only when their text moves */
  overlays(t: number) {
    const run = this.run, tick = Math.min(t, run.end), ht = Math.max(0, ub(run.hudT, tick)), h = run.hudV[ht]!, a = +h[0]!;
    const vale = run.byId.get('councillor'), valeSt = vale ? run.st(vale, tick).x.replace(/^Vale: /, '').split(' · ')[0] : '';
    const wk = Math.max(0, ub(run.winT, tick)); const wins = (run.d.windows[wk][1] as string).split(' ').filter(Boolean).map(w => { const [k, th, u] = w.split(':'); return `<span class="chip">${esc(k!)} ${esc(th!)} ${+u! >= 0 ? 'to ' + clock(+u!) : 'held'}</span>`; }).join(' ');
    const cnt: Record<string, number> = {};
    for (const e of run.ents) if (e.side !== 'civ' && e.side !== 'vale' && (run.st(e, tick).f & DOWN)) cnt[e.side] = (cnt[e.side] ?? 0) + 1;
    const downs = Object.entries(cnt).map(([k, v]) => `${SIDE[k]?.name ?? k} ${v}`).join(', ');
    const s = `<span class="name">${esc(run.meta.title)}</span><span class="clock">${clockT(tick)}</span>` +
      `<span class="kv a${a}">Alert <b>${a} ${ALERT[a]}</b></span><span class="kv">Trail <b>${h[1]}</b></span><span class="kv">Cash <b>$${(+h[2]!).toLocaleString("en-US")}</b></span><span class="kv">Standing <b>${h[3]}</b></span><span class="kv">Shots <b>${h[4]}</b></span>` +
      `<span class="kv">Vale <b>${esc(valeSt ?? '')}</b></span>${downs ? `<span class="kv">Down <b>${esc(downs)}</b></span>` : ''} ${wins}`;
    if (s !== this.lastHud) { this.hud.innerHTML = s; this.lastHud = s; }
    const hi = ub(run.logT, tick); const lines: string[] = []; const want = panels.length > 1 ? 4 : 9;
    for (let i = hi; i >= 0 && lines.length < want; i--) {
      const [lt, code, text] = run.d.log[i]; if (!view.orders && /^(order\.|fire\.|run\.seen|ai\.)/.test(code)) continue;
      const cls = /fail|seen|down|dead|lost|alarm|blown|security\.called|seal/.test(code) ? 'bad' : /turned|enter\.|take|board|car\.leaves|success/.test(code) ? 'good' : /fight|shot|hit/.test(code) ? 'fightl' : /^order/.test(code) ? 'order' : '';
      lines.unshift(`<div class="${cls}" title="${esc(code)}"><i>${clock(lt)}</i>${esc(text)}</div>`);
    }
    const l = lines.join(''); if (l !== this.lastTick) { this.ticker.innerHTML = l; this.lastTick = l; }
    if (t >= run.end) {
      const o = run.d.outcome; this.result.style.display = 'block';
      const r = `<span class="${o === 'success' ? 'ok' : o === 'failure' ? 'fail' : 'wd'}">${o === 'none' ? 'RUN ENDS' : o.toUpperCase()} at ${clock(run.d.tick)}</span>${run.d.fail || run.d.failText ? `<small>${esc(run.d.failText || run.d.fail)}</small>` : ''}`;
      if (this.result.innerHTML !== r) this.result.innerHTML = r;
    } else this.result.style.display = 'none';
    // tags
    const w = this.rect.w, H = this.rect.h, used = new Set<string>(), v = new THREE.Vector3(), p = this.pv;
    if (view.labels || view.follow) for (const e of run.ents) {
      const st = run.st(e, tick); const armed = st.f & DRAWN; const major = e.side === 'cell' || e.side === 'vale' || e.side === 'named' || (e.side === 'guard') || (armed && e.side !== 'civ');
      if (!(e.id === view.follow || (view.labels && major))) continue;
      if (!run.pos(e, tick, p)) continue;
      v.set(p.x, 2.7, p.z).project(this.cam); if (v.z > 1 || v.x < -1 || v.x > 1 || v.y < -1 || v.y > 1) continue;
      let d = this.tags.get(e.id); if (!d) { d = el('div', 'tag'); d.style.color = SIDE[e.side]?.c ?? '#fff'; this.tags.set(e.id, d); this.tagBox.append(d); }
      used.add(e.id); const hp = run.hp(e, tick), txt = `${e.side === 'cell' ? e.label : e.label.split(' ').slice(-1)[0]} ${hp}/${e.hpMax}${st.f & DOWN ? ' ✕' : ''}`;
      if ((d as any)._t !== txt) { d.textContent = txt; (d as any)._t = txt; }
      d.classList.toggle('sel', e.id === view.follow); d.style.display = 'block'; d.style.left = ((v.x + 1) / 2 * w).toFixed(1) + 'px'; d.style.top = ((1 - v.y) / 2 * H).toFixed(1) + 'px';
    }
    for (const [k, d] of this.tags) if (!used.has(k)) d.style.display = 'none';
    const placed: [number, number][] = [];
    for (const d of this.places) { // big labels first (buildScene's order is doors, big, places: sorted at construction); a label that would sit on another is dropped
      const [x, y] = (d as any)._w; v.set(x, 1, -y).project(this.cam);
      const sx = (v.x + 1) / 2 * w, sy = (1 - v.y) / 2 * H;
      if (!view.labels || v.z > 1 || v.x < -1 || v.x > 1 || v.y < -1 || v.y > 1 || placed.some(q => Math.abs(q[0] - sx) < 80 && Math.abs(q[1] - sy) < 16)) { d.style.display = 'none'; continue; }
      placed.push([sx, sy]); d.style.display = 'block'; d.style.left = sx.toFixed(1) + 'px'; d.style.top = sy.toFixed(1) + 'px';
    }
  }
  render(t: number) {
    const cam = this.cam, y = view.yaw * Math.PI / 180, pi = view.pitch * Math.PI / 180; const tick = Math.min(t, this.run.end);
    this.focus.copy(view.target);
    if (view.follow) { const e = this.run.byId.get(view.follow), p = this.pv; if (e && this.run.pos(e, tick, p)) { this.focus.set(p.x, 0, p.z); view.target.copy(panels.length === 1 ? this.focus : view.target); } }
    const fx = Math.cos(pi) * Math.sin(y), fy = -Math.sin(pi), fz = -Math.cos(pi) * Math.cos(y);
    // Unity yaw: forward (sin, cos) in (x, z), and z here is -plan y's sign flipped: three z = -plan y
    cam.position.set(this.focus.x - fx * view.dist, this.focus.y - fy * view.dist, this.focus.z - fz * view.dist); cam.up.set(0, 1, 0); cam.lookAt(this.focus);
    cam.updateMatrixWorld();
    this.update(t); this.overlays(t);
    for (const p of panels) p.group.visible = p === this;
    const r = this.rect; renderer.setViewport(r.x, r.y, r.w, r.h); renderer.setScissor(r.x, r.y, r.w, r.h);
    renderer.clear(); renderer.render(scene3, cam);
  }
}

// ---- layout
function layout() {
  const st = $('stage').getBoundingClientRect(), W = Math.floor(st.width), H = Math.floor(st.height); renderer.setSize(W, H, false);
  const n = panels.length, cols = n >= 3 ? 2 : n, rows = n >= 3 ? 2 : 1, cw = Math.floor(W / Math.max(cols, 1)), ch = Math.floor(H / rows);
  panels.forEach((p, i) => p.setRect((i % cols) * cw, Math.floor(i / cols) * ch, cw - (cols > 1 ? 1 : 0), ch - (rows > 1 ? 1 : 0), H));
  drawTimeline();
}
new ResizeObserver(layout).observe($('stage'));

// ---- run picker
let selected: number[] = [];
async function show(ids: number[]) {
  selected = ids.slice(0, 4);
  const loaded = await Promise.all(selected.map(getRun));
  panels.splice(0).forEach(p => p.dispose());
  for (const r of loaded) panels.push(new Panel(r));
  time.t = Math.min(time.t, maxEnd());
  layout(); drawRuns(); drawInfo(); drawTimeline();
}
const maxEnd = () => panels.reduce((a, p) => Math.max(a, p.run.end), 0);
function drawRuns() {
  const box = $('runs'); box.innerHTML = '';
  box.append(el('h3', '', 'Runs · click one, tick up to 4 to compare'));
  manifest.runs.forEach((r: any, i: number) => {
    const row = el('div', 'run' + (selected.includes(i) ? ' on' : ''));
    const cb = el('input') as HTMLInputElement; cb.type = 'checkbox'; cb.checked = selected.includes(i);
    cb.onclick = ev => { ev.stopPropagation(); const s = selected.includes(i) ? selected.filter(x => x !== i) : [...selected, i]; if (s.length >= 1 && s.length <= 4) void show(s); else drawRuns(); };
    const cls = r.outcome === 'success' ? 'ok' : r.outcome === 'failure' ? 'fail' : 'wd';
    const txt = el('div', '', `<div class="t">${esc(r.title)}</div><div class="m"><span class="${cls}">${r.outcome}${r.fail ? ' · ' + esc(r.fail) : ''}</span> · ${clock(r.tick)} · ${r.shots} shots · T${r.trail} · A${r.alert}</div><div class="m" title="final hash">${r.hashOk ? '<span class="ok">hash ok</span>' : r.hash === 'unchecked' ? '<span title="the record holds this route as an adaptive plan across seeds, or not at all">no recorded run</span>' : '<span class="fail">hash differs</span>'} ${r.finalHash.slice(0, 8)} · seed ${r.seed ?? 'none yet'}</div><div class="m">${esc(r.blurb)}</div>`);
    row.append(cb, txt); row.onclick = () => void show([i]); box.append(row);
  });
  box.append(el('h3', '', 'Compare'));
  const pre = (name: string, scripts: string[]) => { const b = el('button', '', name); b.style.margin = '0 3px 3px 0'; b.onclick = () => { time.t = 0; void show(scripts.map(s => manifest.runs.findIndex((r: any) => r.script === s)).filter(x => x >= 0)); }; box.append(b); };
  pre('A · B · G · Loud', ['route-a', 'route-b', 'route-g', 'loud']); pre('Loud · Fight', ['loud', 'loud-fight']); pre('Fails', ['careless-takedown-seen-camera', 'fail-rivals-reach-vale', 'fail-enforcer-down', 'fail-vale-dead']);
}

// ---- controls
const btns: Record<string, HTMLElement> = {};
function controls() {
  const box = $('controls');
  const add = (id: string, label: string, fn: () => void, title = '') => { const b = el('button', '', label); b.title = title; b.onclick = fn; btns[id] = b; box.append(b); };
  const sep = () => box.append(el('div', 'sep'));
  add('play', 'Pause', togglePlay, 'Space');
  add('start', '⏮', () => { time.t = 0; });
  for (const s of [1, 2, 4, 8]) add('x' + s, s + '×', () => { time.speed = s; syncButtons(); });
  sep();
  add('top', 'Top-down', () => setView('top'), 'T'); add('angle', 'Play angle', () => setView('play'), 'P');
  add('home', 'Whole block', () => { view.target.set(-156, 0, 262); view.dist = 105; view.follow = ''; drawInfo(); });
  sep();
  add('cones', 'Cones', () => { view.cones = !view.cones; syncButtons(); }, 'C. Sight fans: red cameras, amber guard and patrol, blue police, violet rivals. The fans here leave the crowd out; grey = dead (looped, rerouted).');
  add('walls', 'Walls low', () => { view.walls = view.walls === 'full' ? 'half' : view.walls === 'half' ? 'low' : view.walls === 'low' ? 'off' : 'full'; syncButtons(); }, 'W');
  add('labels', 'Labels', () => { view.labels = !view.labels; syncButtons(); }, 'L');
  add('crowd', 'Crowd', () => { view.crowd = !view.crowd; syncButtons(); });
  add('paths', 'Path', () => { view.paths = !view.paths; syncButtons(); }, 'The followed person\'s whole walk: solid so far, faint to come');
  add('grid', 'Walk grid', () => { view.grid = !view.grid; syncButtons(); }, 'The engine\'s blocked walk cells in red, cover points in green');
  add('orders', 'Log: orders', () => { view.orders = !view.orders; syncButtons(); for (const p of panels) p.lastTick = ''; }, 'Show every order and fire line in the ticker');
  syncButtons();
}
function setView(k: string) { const v = VIEWS[k]!; view.yaw = v.yaw; view.pitch = v.pitch; view.dist = v.dist; if (!view.follow) view.target.set(-156, 0, 262); }
function togglePlay() { if (time.t >= maxEnd()) time.t = 0; time.playing = !time.playing; syncButtons(); }
function syncButtons() {
  btns.play!.textContent = time.playing ? 'Pause' : 'Play';
  for (const s of [1, 2, 4, 8]) btns['x' + s]!.classList.toggle('on', time.speed === s);
  for (const k of ['cones', 'labels', 'crowd', 'paths', 'grid', 'orders'] as const) btns[k]!.classList.toggle('on', !!(view as any)[k]);
  btns.walls!.textContent = 'Walls: ' + view.walls; btns.walls!.classList.toggle('on', view.walls !== 'full');
  if (wallsGroup) { wallsGroup.visible = view.walls !== 'off'; wallsGroup.scale.y = view.walls === 'low' ? 0.14 : view.walls === 'half' ? 0.4 : 1; }
  if (gridMesh) gridMesh.visible = coverMesh.visible = view.grid;
}
addEventListener('keydown', (e: any) => {
  if (e.key === ' ') { e.preventDefault(); togglePlay(); } else if (e.key === 'ArrowRight') time.t = Math.min(maxEnd(), time.t + 50); else if (e.key === 'ArrowLeft') time.t = Math.max(0, time.t - 50);
  else if (e.key === 't') setView('top'); else if (e.key === 'p') setView('play'); else if (e.key === 'c') btns.cones!.click(); else if (e.key === 'l') btns.labels!.click(); else if (e.key === 'w') btns.walls!.click();
  else if (e.key === 'q') view.yaw = (view.yaw + 90) % 360; else if (e.key === 'e') view.yaw = (view.yaw + 270) % 360; else if (e.key === 'Escape') { view.follow = ''; drawInfo(); }
});

// ---- mouse: left-drag orbit, right-drag or shift-drag pan, wheel zoom, click a person
const stage = $('gl');
let drag: { x: number; y: number; pan: boolean; moved: number } | null = null;
stage.addEventListener('contextmenu', e => e.preventDefault());
stage.addEventListener('pointerdown', e => { drag = { x: e.clientX, y: e.clientY, pan: e.button !== 0 || e.shiftKey, moved: 0 }; stage.setPointerCapture(e.pointerId); });
stage.addEventListener('pointermove', e => {
  if (!drag) return; const dx = e.clientX - drag.x, dy = e.clientY - drag.y; drag.x = e.clientX; drag.y = e.clientY; drag.moved += Math.abs(dx) + Math.abs(dy);
  if (drag.pan) { const y = view.yaw * Math.PI / 180, k = view.dist * 0.0016; const rx = Math.cos(y), rz = Math.sin(y), fx = Math.sin(y), fz = -Math.cos(y); view.target.x += (-dx * rx + dy * fx) * k; view.target.z += (-dx * rz + dy * fz) * k; view.follow = ''; }
  else { view.yaw = (view.yaw + dx * 0.35 + 360) % 360; view.pitch = Math.max(10, Math.min(89.5, view.pitch + dy * 0.25)); }
});
stage.addEventListener('pointerup', e => { const was = drag; drag = null; if (was && was.moved < 5) pick(e.clientX, e.clientY); });
stage.addEventListener('wheel', e => { e.preventDefault(); view.dist = Math.max(8, Math.min(320, view.dist * Math.exp(e.deltaY * 0.001))); }, { passive: false });
function pick(cx: number, cy: number) {
  const r = stage.getBoundingClientRect(); const mx = cx - r.left, my = cy - r.top, H = r.height;
  const panel = panels.find(p => mx >= p.rect.x && mx < p.rect.x + p.rect.w && (H - my) >= p.rect.y && (H - my) < p.rect.y + p.rect.h); if (!panel) return;
  let best = '', bd = 20 * 20; const v = new THREE.Vector3(), p = { x: 0, z: 0 }, tick = Math.min(time.t, panel.run.end);
  for (const e of panel.run.ents) {
    if (!panel.run.pos(e, tick, p)) continue;
    v.set(p.x, 1, p.z).project(panel.cam); const sx = (v.x + 1) / 2 * panel.rect.w + panel.rect.x, sy = H - ((v.y + 1) / 2 * panel.rect.h + panel.rect.y);
    let d = (sx - mx) ** 2 + (sy - my) ** 2; if (e.side === 'civ') d *= 1.6; if (d < bd) { bd = d; best = e.id; }
  }
  view.follow = best; drawInfo();
}

// ---- the followed person's card
let lastInfo = '';
function drawInfo() { lastInfo = ''; infoTick(); }
function infoTick() {
  const box = $('info'); const sel = view.follow; let h = '';
  if (!sel) h = `<h3>Follow</h3><p>Click anyone on the street to follow them and see their stats. Esc lets go.</p><h3>Controls</h3><p>Space play · ←/→ 5 s · T top · P play angle · Q/E quarter turn · C cones · W walls · L labels<br>Drag: orbit · Right or Shift drag: pan · Wheel: zoom</p><h3>What this is</h3><p>Real engine runs (${esc(manifest.engine)}) read tick by tick and drawn in low poly. Positions are true; bodies, cars and buildings are stand-ins. Nothing is decided here.</p>`;
  else for (const panel of panels) {
    const e = panel.run.byId.get(sel); if (!e) { h += `<h3>${esc(panel.run.meta.title)}</h3><p>Not in this run.</p>`; continue; }
    const t = Math.min(time.t, panel.run.end), st = panel.run.st(e, t), hp = panel.run.hp(e, t), on = panel.run.pos(e, t, panel.pv);
    const fired = ub(e.shotT, t) + 1; let hits = 0, dealt = 0; for (let i = 0; i < fired; i++) { const s = panel.run.d.shots[e.shotI[i]!]; if (s[7]) { hits++; dealt += s[8]; } }
    let taken = 0, hitBy = 0; for (const s of panel.run.d.shots) { if (s[0] > t) break; if (s[2] === e.id) { taken++; if (s[7]) hitBy++; } }
    const pct = e.hpMax ? Math.max(0, hp) / e.hpMax * 100 : 0;
    h += `<h3>${esc(panel.run.meta.title)}</h3><div style="color:${SIDE[e.side]?.c};font-weight:700;font-size:14px">${esc(e.label)}</div><div>${esc(SIDE[e.side]?.name ?? e.side)} · ${esc(e.cls)}</div>` +
      `<div class="hpbar"><div style="width:${pct}%"></div></div><div class="stat"><div>HP</div><div>${e.hpMax ? `${hp} / ${e.hpMax}` : 'n/a'}</div><div>Doing</div><div>${esc(st.x || (on ? 'standing' : 'off the street'))}</div><div>Shots</div><div>${fired} fired, ${hits} hit, ${dealt} damage</div><div>Shot at</div><div>${taken}, ${hitBy} hit</div><div>Where</div><div>${on ? `${panel.pv.x.toFixed(1)}, ${(-panel.pv.z).toFixed(1)} m` : 'off the street'}</div><div>Who</div><div>${esc(e.detail)}</div></div>`;
  }
  if (h !== lastInfo) { box.innerHTML = h; lastInfo = h; }
}

// ---- timeline: a lane per panel, alert bands behind, events as marks
const tl = $('tl') as HTMLCanvasElement, tip = $('tip');
const LANE = 26;
type Mark = { t: number; lane: number; text: string };
let marks: Mark[] = [];
const KEY = /^(turn\.[a-z]+\.turned|enter\.in|enter\.out|take\.|taken|board|car\.leaves|go\.|fight\.(sprung|over|loud|warn)|alert|takedown\.done|security\.called|seal\.|rivals\.(coming|drop|reach)|fail|loop\.on|call\.)/;
function drawTimeline() {
  const n = Math.max(panels.length, 1), cssW = tl.parentElement!.clientWidth, cssH = n * LANE + 14, dpr = Math.min(devicePixelRatio, 2);
  tl.style.height = cssH + 'px'; if (tl.width !== cssW * dpr || tl.height !== cssH * dpr) { tl.width = cssW * dpr; tl.height = cssH * dpr; }
  const c = tl.getContext('2d')!; c.setTransform(dpr, 0, 0, dpr, 0, 0); c.clearRect(0, 0, cssW, cssH);
  const end = Math.max(maxEnd(), 1), X = (t: number) => t / end * cssW; marks = [];
  c.font = '10px ui-monospace,Menlo,monospace'; c.fillStyle = '#889'; c.textBaseline = 'top';
  for (let s = 0; s * 10 <= end; s += 60) { const x = X(s * 10); c.fillStyle = '#556'; c.fillRect(x, 0, 1, cssH); c.fillStyle = '#889'; c.fillText(`${s / 60}:00`, x + 2, cssH - 12); }
  panels.forEach((p, i) => {
    const y = i * LANE, run = p.run, A = ['#23402b', '#4a4a20', '#6a3c18', '#6e2020'];
    run.hudT.forEach((t, k) => { const a = +run.hudV[k]![0]!, nx = k + 1 < run.hudT.length ? run.hudT[k + 1]! : run.end; c.fillStyle = A[a]!; c.fillRect(X(t), y + 1, X(nx) - X(t), LANE - 2); });
    c.fillStyle = '#d8d8d0'; c.fillText(run.meta.title.slice(0, 40), 3, y + 2);
    for (const o of run.d.orders) { const x = X(o[0]); c.fillStyle = o[2] ? '#6fb8e8' : '#e66'; c.fillRect(x, y + LANE - 7, 1, 5); marks.push({ t: o[0], lane: i, text: `order: ${o[1].op ?? ''} ${o[1].do} ${o[1].target ?? ''}${o[2] ? '' : ' REFUSED ' + o[3]}` }); }
    for (const s of run.d.shots) { const x = X(s[0]); c.fillStyle = s[7] ? '#ff4030' : '#ddd'; c.fillRect(x, y + 13, 1.2, s[7] ? 7 : 4); marks.push({ t: s[0], lane: i, text: `${s[1]} → ${s[2]}: ${s[7] ? `hit, ${s[8]} damage${s[9] ? ', down' : ''}` : 'miss'}` }); }
    for (const l of run.d.log) if (KEY.test(l[1])) { const x = X(l[0]); c.fillStyle = '#ffd36a'; c.beginPath(); c.moveTo(x, y + 12); c.lineTo(x + 3, y + 16); c.lineTo(x, y + 20); c.lineTo(x - 3, y + 16); c.fill(); marks.push({ t: l[0], lane: i, text: l[2] }); }
    const ex = X(run.d.tick); c.fillStyle = run.d.outcome === 'success' ? '#7ddc8a' : '#ff6a5e'; c.fillRect(ex - 2, y + 1, 3, LANE - 2);
  });
  c.fillStyle = '#fff'; c.fillRect(X(time.t) - 0.5, 0, 2, cssH - 12);
}
let scrubbing = false;
const scrub = (e: PointerEvent) => { const r = tl.getBoundingClientRect(); time.t = Math.max(0, Math.min(maxEnd(), (e.clientX - r.left) / r.width * maxEnd())); };
tl.addEventListener('pointerdown', e => { scrubbing = true; tl.setPointerCapture(e.pointerId); scrub(e); });
tl.addEventListener('pointerup', () => { scrubbing = false; });
tl.addEventListener('pointerleave', () => { tip.style.display = 'none'; });
tl.addEventListener('pointermove', e => {
  if (scrubbing) scrub(e);
  const r = tl.getBoundingClientRect(), x = e.clientX - r.left, y = e.clientY - r.top, lane = Math.floor(y / LANE), end = Math.max(maxEnd(), 1);
  let best: Mark | null = null, bd = 5; for (const m of marks) if (m.lane === lane) { const d = Math.abs(m.t / end * r.width - x); if (d < bd) { bd = d; best = m; } }
  const here = (x / r.width * end); tip.style.display = 'block'; tip.style.left = Math.min(x, r.width - 300) + 'px'; tip.innerHTML = best ? `${clock(best.t)} ${esc(best.text)}` : clock(here);
});

// ---- the page's plain statement, the legend, and the loop
function banner() {
  const ok = manifest.runs.filter((r: any) => r.hashOk).length, all = manifest.runs.filter((r: any) => r.hash !== 'unchecked').length;
  $('banner').innerHTML = `Engine <b>${esc(manifest.engine.replace('suborned.sim/', ''))}</b> · record ${esc(manifest.record)} · built ${esc(manifest.built)} · final hash <b class="${ok === all ? '' : 'bad'}">${ok}/${all}</b> match the record · <b>Seeds aren't in yet</b> (brief 080): the same orders play identically, so runs differ only by route.`;
  $('legend').innerHTML = Object.values(SIDE).map(s => `<span style="background:${s.c}"></span>${s.name}`).join('') + '<span style="background:#ff3020;border-radius:0;height:3px;vertical-align:2px"></span>hit<span style="background:#f4f4f4;border-radius:0;height:2px;vertical-align:2px"></span>miss';
}
let lastFrame = performance.now(), frame = 0;
function loop(now: number) {
  const dt = Math.min(0.1, (now - lastFrame) / 1000); lastFrame = now;
  if (time.playing && !scrubbing && panels.length) { time.t += dt * 10 * time.speed; if (time.t >= maxEnd()) { time.t = maxEnd(); time.playing = false; syncButtons(); } }
  renderAll(); requestAnimationFrame(loop);
}
function renderAll() {
  renderer.setScissor(0, 0, 99999, 99999); renderer.clear();
  for (const p of panels) p.render(time.t);
  if (frame++ % 12 === 0) { drawTimeline(); infoTick(); }
}

async function boot() {
  buildWorld(await unpack('scene')); banner(); controls();
  const q = new URLSearchParams(location.hash.slice(1)); // for looking at it headless: #runs=route-a,loud&t=2300&view=play&cones=1&walls=low&follow=enforcer&play=0
  const ids = (q.get('runs') ?? 'route-a').split(',').map(s => manifest.runs.findIndex((r: any) => r.script === s)).filter((i: number) => i >= 0);
  if (q.get('view')) setView(q.get('view')!);
  if (q.get('t')) time.t = +q.get('t')!;
  if (q.get('play') === '0') time.playing = false;
  if (q.get('cones')) view.cones = true;
  if (q.get('walls')) view.walls = q.get('walls')!;
  if (q.get('dist')) view.dist = +q.get('dist')!;
  if (q.get('yaw')) view.yaw = +q.get('yaw')!;
  if (q.get('tx')) view.target.set(+q.get('tx')!, 0, -q.get('ty')!);
  if (q.get('grid')) view.grid = true;
  if (q.get('nolabels')) view.labels = false;
  if (q.get('speed')) time.speed = +q.get('speed')!;
  await show(ids.length ? ids : [0]);
  if (q.get('follow')) view.follow = q.get('follow')!;
  syncButtons(); drawInfo();
  document.title = 'The Suborned · sim view · ready';
  (window as any).__simview = { panels, time, view, renderAll };
  requestAnimationFrame(loop);
}
boot().catch(e => { $('banner').innerHTML = `<b class="bad">Failed to start: ${esc(String(e))}</b>`; console.error(e); });
