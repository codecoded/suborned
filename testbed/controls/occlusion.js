// Brief 056: camera occlusion options, switchable live. Buildings between the camera and the people are handled six ways:
// A today's column dither (a port of DK_Cutaway.hlsl), B silhouettes only, C a soft cut round each agent and the cursor,
// D the whole building fades with an edge outline, E the floors above the line of sight come off (ground floor stays),
// F the soft cut plus silhouettes. Behaviour only: the look is restyled for the game.
import * as THREE from './vendor/three.module.min.js';
import { G } from './state.js';

export const MODES = [
  ['column', 'A  Column dither (today)'], ['sil', 'B  Silhouettes only'], ['circle', 'C  Soft circle cut'],
  ['whole', 'D  Whole building'], ['floors', 'E  Floors come off'], ['mix', 'F  Circle + silhouettes'],
];
// the shader's mode: 0 nothing, 1 column (today), 2 circle, 4 floors; the whole-building fade (D) is on the CPU
const SHADER_MODE = { column: 1, sil: 0, circle: 2, whole: 0, floors: 4, mix: 2 };
const SIL_DEFAULT = { sil: true, mix: true };
const MAXP = 8;
const FLOOR_M = 3.2; // one storey

// shared by every patched material
const U = {
  uOccMode: { value: 0 }, uN: { value: 0 },
  uScr: { value: Array.from({ length: MAXP }, () => new THREE.Vector4()) }, uDep: { value: new Array(MAXP).fill(0) }, uRad: { value: new Array(MAXP).fill(1) },
  uWp: { value: Array.from({ length: MAXP }, () => new THREE.Vector4()) }, uFwd: { value: new THREE.Vector3(0, -1, 0) }, uFocus: { value: new THREE.Vector4() },
  uColA: { value: 1 }, // 1 keeps the dither cut at today's strength
};
const VERT_DECL = 'varying vec3 vOccW; varying float vOccD;';
const FRAG_DECL = `
varying vec3 vOccW; varying float vOccD;
uniform int uOccMode; uniform int uN; uniform vec4 uScr[${MAXP}]; uniform float uDep[${MAXP}]; uniform float uRad[${MAXP}];
uniform vec4 uWp[${MAXP}]; uniform vec3 uFwd; uniform vec4 uFocus; uniform float uCutY;
float occBayer(vec2 px){ float m[16] = float[16](0.,8.,2.,10.,12.,4.,14.,6.,3.,11.,1.,9.,15.,7.,13.,5.); ivec2 p = ivec2(mod(px, 4.)); return (m[p.y*4+p.x]+.5)/16.; }
float occIGN(vec2 p){ return fract(52.9829189*fract(dot(p, vec2(0.06711056, 0.00583715)))); }
float occSeg(vec2 c, vec2 a, vec2 b){ vec2 ab=b-a; float t=clamp(dot(c-a,ab)/max(dot(ab,ab),1e-6),0.,1.); return length(a+ab*t-c); }
// today's rule, as DK_Cutaway.hlsl DKCutDisc: a world disc round a ground point, column mode, ghost 0.2, nothing beyond the focus
float occDisc(vec3 p, vec3 f, float radius, float ghost, float feather){
  vec2 k = uFwd.xz / max(-uFwd.y, 1e-3);
  vec2 hit = p.xz + k*(p.y - f.y);
  float d = occSeg(f.xz, p.xz + k*(0. - f.y), hit);
  float outside = clamp((d-(radius-feather))/feather, 0., 1.);
  return mix(ghost, 1., outside);
}
`;
const FRAG_CUT = `
float occVeil = 0.;
if (uOccMode == 1) { // A: today
  float keep = occDisc(vOccW, uFocus.xyz, uFocus.w, 0.2, 2.);
  for (int i = 0; i < ${MAXP}; i++) { if (i >= uN) break; float r = uWp[i].w; bool person = r < 0.; keep = min(keep, occDisc(vOccW, uWp[i].xyz, abs(r), person ? 0. : 0.2, person ? 0.4 : 2.)); }
  if (keep - occBayer(gl_FragCoord.xy) < 0.) discard;
  occVeil = 0.9 * clamp((1. - keep) / 0.8, 0., 1.);
} else if (uOccMode == 2) { // C / F: a soft round cut, only where the wall is in front of the person
  float keep = 1.;
  for (int i = 0; i < ${MAXP}; i++) { if (i >= uN) break;
    float d = occSeg(gl_FragCoord.xy, uScr[i].xy, uScr[i].zw), r = uRad[i];
    float front = step(vOccD, uDep[i] - 0.45);
    float k = smoothstep(r * 0.55, r, d);
    keep = min(keep, mix(1., k, front));
  }
  if (keep < 0.999) { if (keep - occIGN(gl_FragCoord.xy) < 0.) discard; occVeil = 0.55 * (1. - abs(keep * 2. - 1.)); }
} else if (uOccMode == 4) { // E: floors above the cut come off
  if (vOccW.y > uCutY) discard;
}
`;
const FRAG_END = `
#ifdef OCC_VEIL
gl_FragColor.rgb = mix(gl_FragColor.rgb, vec3(0.97, 0.94, 0.88), occVeil * 0.6);
#endif
`;

export function patch(mat, perBuilding) {
  mat.onBeforeCompile = (sh) => {
    Object.assign(sh.uniforms, U); sh.uniforms.uCutY = perBuilding.uCutY;
    sh.vertexShader = VERT_DECL + '\n' + sh.vertexShader.replace('#include <project_vertex>', '#include <project_vertex>\n vOccW = (modelMatrix * vec4(transformed, 1.0)).xyz; vOccD = -mvPosition.z;');
    sh.fragmentShader = FRAG_DECL + '\n' + sh.fragmentShader
      .replace('#include <clipping_planes_fragment>', '#include <clipping_planes_fragment>\n' + FRAG_CUT)
      .replace('#include <dithering_fragment>', '#include <dithering_fragment>\n#define OCC_VEIL\n' + FRAG_END);
  };
  mat.customProgramCacheKey = () => 'occ1';
  return mat;
}

// ---- a building: a windowed box (cream, peach, seafoam), a cap for the cut-off floors, and an outline for the whole-building fade
function facade(wall, glass, band) {
  const c = document.createElement('canvas'); c.width = 128; c.height = 96; const g = c.getContext('2d');
  g.fillStyle = wall; g.fillRect(0, 0, 128, 96);
  g.fillStyle = band; g.fillRect(0, 88, 128, 8); g.fillRect(0, 0, 128, 2);
  g.fillStyle = '#d9d2c2'; g.fillRect(31, 17, 66, 56); // chrome frame
  g.fillStyle = glass; g.fillRect(35, 21, 58, 48);
  g.fillStyle = 'rgba(255,255,255,.28)'; g.fillRect(35, 21, 58, 14);
  g.fillStyle = '#d9d2c2'; g.fillRect(63, 21, 2, 48);
  const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; return t;
}
export function makeBuilding(group, { cx, cz, w, d, h, wall = '#efe6d2', glass = '#2f6e74', band = '#cdbf9f', roof = 0xe9e1cf }) {
  const geo = new THREE.BoxGeometry(w, h, d), uv = geo.attributes.uv;
  for (let f = 0; f < 6; f++) { // a window bay is 4 m by 3.2 m; tops get no windows
    const sx = f < 2 ? d / 4 : f < 4 ? 1 : w / 4, sy = f < 2 || f > 3 ? h / FLOOR_M : 1;
    for (let i = 0; i < 4; i++) { const k = f * 4 + i; uv.setXY(k, uv.getX(k) * sx, uv.getY(k) * sy); }
  }
  const rec = { w, d, h, uCutY: { value: 1e5 }, cutY: h, cx, cz, opacity: 1, mats: [] };
  const side = new THREE.MeshLambertMaterial({ map: facade(wall, glass, band) }), top = new THREE.MeshLambertMaterial({ color: roof });
  const mats = [side, side.clone(), top, top.clone(), side.clone(), side.clone()];
  mats.forEach((m) => { m.transparent = true; }); // from the start: a program compiled opaque keeps alpha 1
  mats.forEach((m) => { m.map = side.map; patch(m, rec); }); rec.mats = mats;
  const mesh = new THREE.Mesh(geo, mats); mesh.position.set(cx, h / 2, cz); group.add(mesh); rec.mesh = mesh;
  const cap = new THREE.Mesh(new THREE.BoxGeometry(w + 0.4, 0.34, d + 0.4), new THREE.MeshLambertMaterial({ color: roof })); cap.visible = false; group.add(cap); rec.cap = cap;
  // the outline for D: the twelve edges as thin bars (a 1 px line vanishes on a sharp screen)
  const edges = new THREE.Group(), em = new THREE.MeshBasicMaterial({ color: 0x17302f, transparent: true, opacity: 0, depthWrite: false, toneMapped: false }), th = 0.16;
  for (const sy of [-1, 1]) for (const sz of [-1, 1]) { const m = new THREE.Mesh(new THREE.BoxGeometry(w, th, th), em); m.position.set(0, sy * h / 2, sz * d / 2); edges.add(m); }
  for (const sx of [-1, 1]) for (const sz of [-1, 1]) { const m = new THREE.Mesh(new THREE.BoxGeometry(th, h, th), em); m.position.set(sx * w / 2, 0, sz * d / 2); edges.add(m); }
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) { const m = new THREE.Mesh(new THREE.BoxGeometry(th, th, d), em); m.position.set(sx * w / 2, sy * h / 2, 0); edges.add(m); }
  edges.children.forEach((m) => { m.renderOrder = 3; });
  edges.position.copy(mesh.position); edges.visible = false; group.add(edges); rec.edges = edges; rec.edgeMat = em;
  rec.box = new THREE.Box3().setFromObject(mesh);
  return rec;
}

// ---- silhouettes: the person drawn again only where something nearer covers it (greater depth), a cream edge round a flat fill
export function addSilhouette(pose, body, nose, color) {
  const g = new THREE.Group(); g.visible = false;
  const mk = (src, mat, s, ro) => { const m = new THREE.Mesh(src.geometry, mat); m.position.copy(src.position); m.rotation.copy(src.rotation); m.scale.setScalar(s); m.renderOrder = ro; return m; };
  const shell = new THREE.MeshBasicMaterial({ color: 0xf7f4ec, depthFunc: THREE.GreaterDepth, depthWrite: false, transparent: true, opacity: 0.95, toneMapped: false });
  const fill = new THREE.MeshBasicMaterial({ color, depthFunc: THREE.GreaterDepth, depthWrite: false, transparent: true, opacity: 1, toneMapped: false });
  g.add(mk(body, shell, 1.3, 4), mk(nose, shell, 1.3, 4), mk(body, fill, 1.03, 5), mk(nose, new THREE.MeshBasicMaterial({ color: 0xffffff, depthFunc: THREE.GreaterDepth, depthWrite: false, toneMapped: false }), 1.03, 5)); // drawn before the people, so only walls (never other people) are in the depth buffer they test against
  pose.add(g); return g;
}

// ---- per frame
const V = new THREE.Vector3(), W = new THREE.Vector3(), plane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0), rc = new THREE.Raycaster();
const wantSil = (e) => e.agent || e.hostile || ['guard', 'police', 'rival', 'inez', 'driver'].includes(e.kind);

export function setMode(m) { G.occ = m; document.body.dataset.occ = m; }

export function occFrame(camera, real, drawBtns) {
  const w = G.world, mode = G.occ, sm = SHADER_MODE[mode] ?? 0, dpr = G.dpr ?? Math.min(devicePixelRatio, 2), H = innerHeight * dpr; // G.dpr is the renderer's own ratio (a phone may run lower)
  const me = w.agents[G.sel] ?? w.agents[0];
  const fwd = camera.getWorldDirection(new THREE.Vector3()); U.uFwd.value.copy(fwd);
  const focus = G.rig.mode === 'cmd' ? G.rig.focus : me.g.position, dist = camera.position.distanceTo(focus);
  U.uFocus.value.set(focus.x, 0, focus.z, Math.min(40, 0.8 * dist * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2))));
  U.uOccMode.value = sm;
  // who must be seen: the selected agents, the other agents, the people who matter, then the cursor's ground point
  const pts = [], seen = new Set(), add = (e) => { if (!e || seen.has(e) || pts.length >= MAXP - 1) return; seen.add(e); pts.push(e); };
  w.agents.forEach((a, i) => { if (G.group[i]) add(a); }); w.agents.forEach(add); for (const e of w.ents) if (!e.agent && wantSil(e) && e.state !== 'out') add(e);
  rc.setFromCamera({ x: G.ptr.x / innerWidth * 2 - 1, y: -(G.ptr.y / innerHeight) * 2 + 1 }, camera);
  const cursor = G.ptr.over && rc.ray.intersectPlane(plane, W) ? W.clone() : null;
  const list = pts.map((e) => ({ foot: e.g.position.clone(), top: e.g.position.clone().setY(e.h ?? 1.9), clean: true, r: 3.2, e }));
  if (cursor) list.push({ foot: cursor.clone(), top: cursor.clone().setY(0.4), clean: false, r: 4, cursor: true });
  U.uN.value = list.length;
  const pxPerM = (depth) => H / (2 * depth * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2)));
  list.forEach((p, i) => {
    const f = V.copy(p.foot).project(camera), t = W.copy(p.top).project(camera);
    const mid = p.foot.clone().lerp(p.top, 0.5).applyMatrix4(camera.matrixWorldInverse), depth = -mid.z;
    U.uScr.value[i].set((f.x * 0.5 + 0.5) * innerWidth * dpr, (f.y * 0.5 + 0.5) * H, (t.x * 0.5 + 0.5) * innerWidth * dpr, (t.y * 0.5 + 0.5) * H);
    U.uDep.value[i] = depth; U.uRad.value[i] = (p.cursor ? 1.5 : 2.0) * pxPerM(depth);
    U.uWp.value[i].set(p.foot.x, p.foot.y, p.foot.z, p.clean ? -p.r : p.r);
  });
  // silhouettes
  const sil = G.sil === 'on' || (G.sil === 'auto' && !!SIL_DEFAULT[mode]);
  for (const e of w.ents) if (e.sil) e.sil.visible = sil && (e.state ?? 'ok') !== 'out';
  // buildings
  const cam = camera.position, k = 1 - Math.exp(-real / 0.14);
  for (const b of w.buildings) {
    // whole building (D): its box is on the sight line to anyone who matters
    let hide = false, needCut = b.h;
    for (const p of list) {
      const tgt = V.copy(p.foot).setY(p.foot.y + 1.2), dir = W.copy(tgt).sub(cam), len = dir.length(); dir.normalize();
      const ray = new THREE.Ray(cam.clone(), dir.clone()), hit = ray.intersectBox(b.box, new THREE.Vector3());
      if (b.box.containsPoint(cam) || (hit && cam.distanceTo(hit) < len - 0.2)) hide = true;
      // floors (E): the sight line to the head, over the building's footprint; the part of the building above where the line leaves it comes off
      const hd = V.copy(p.foot).setY(p.foot.y + 1.9), ax = cam.x, az = cam.z, dx = hd.x - ax, dz = hd.z - az;
      let t0 = 0, t1 = 1;
      for (const [o, dd, lo, hi] of [[ax, dx, b.box.min.x, b.box.max.x], [az, dz, b.box.min.z, b.box.max.z]]) {
        if (Math.abs(dd) < 1e-6) { if (o < lo || o > hi) { t0 = 2; break; } continue; }
        let a = (lo - o) / dd, c = (hi - o) / dd; if (a > c) [a, c] = [c, a]; t0 = Math.max(t0, a); t1 = Math.min(t1, c);
      }
      if (t0 < t1 && !p.cursor) { const y = cam.y + (hd.y - cam.y) * t1; const lim = Math.floor((y - 0.7) / FLOOR_M) * FLOOR_M; needCut = Math.min(needCut, Math.max(FLOOR_M, lim)); }
      else if (t0 < t1 && p.cursor) { const y = cam.y + (hd.y - 1.5 - cam.y) * t1; needCut = Math.min(needCut, Math.max(FLOOR_M, Math.floor((y - 0.7) / FLOOR_M) * FLOOR_M)); }
    }
    // D
    const faded = mode === 'whole' && hide;
    b.opacity += ((faded ? 0.13 : 1) - b.opacity) * k;
    for (const m of b.mats) { m.opacity = b.opacity; m.depthWrite = b.opacity > 0.6; }
    const edgeA = mode === 'whole' ? Math.max(0, (1 - b.opacity) / 0.87) : 0;
    b.edges.visible = edgeA > 0.02; b.edgeMat.opacity = edgeA * 0.85;
    // E
    const goal = mode === 'floors' ? needCut : b.h;
    b.cutY += (goal - b.cutY) * k; if (Math.abs(goal - b.cutY) < 0.01) b.cutY = goal;
    b.uCutY.value = b.cutY < b.h - 0.005 ? b.cutY : 1e5;
    b.cap.visible = b.cutY < b.h - 0.05; b.cap.position.set(b.cx, b.cutY + 0.17, b.cz);
  }
}
