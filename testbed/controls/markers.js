// The one marker for the main action. Four styles to compare.
//   ring  : coloured ring on the ground plus an icon badge and the time
//   pill  : one pill in the act's colour carrying the whole price line
//   badge : icon only; the line opens after a short dwell
//   built : the 0.2.34 look, for comparison (three icons on the ring, a name tag, a plate of numbers)
import * as THREE from './vendor/three.module.min.js';
import { G } from './state.js';
import { KINDS, ACTS, priceLine } from './data.js';
import { icon } from './icons.js';

function ringTexture(dashed) {
  const c = document.createElement('canvas'); c.width = c.height = 256; const g = c.getContext('2d');
  g.strokeStyle = '#fff'; g.lineWidth = dashed ? 16 : 14; g.lineCap = 'round'; if (dashed) g.setLineDash([34, 26]);
  g.beginPath(); g.arc(128, 128, 112, 0, Math.PI * 2); g.stroke(); const t = new THREE.CanvasTexture(c); t.anisotropy = 4; return t;
}
export function groundRing(scene, dashed = false) {
  const m = new THREE.Mesh(new THREE.PlaneGeometry(1, 1), new THREE.MeshBasicMaterial({ map: ringTexture(dashed), transparent: true, depthTest: false, depthWrite: false, toneMapped: false }));
  m.rotation.x = -Math.PI / 2; m.renderOrder = 10; m.visible = false; scene.add(m); return m;
}

export class Marker {
  constructor(scene, el) {
    this.el = el; this.solid = groundRing(scene); this.dashed = groundRing(scene, true); this.key = ''; this.dwell = 0; this.target = null; this.ring = null;
    this.line = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineDashedMaterial({ dashSize: 0.4, gapSize: 0.28, depthTest: false, transparent: true })); this.line.renderOrder = 9; this.line.visible = false; this.line.frustumCulled = false; scene.add(this.line);
  }
  hide() { this.line.visible = false; this.el.style.display = 'none'; this.solid.visible = this.dashed.visible = false; this.target = null; this.dwell = 0; }
  update(dt, ent, act, walkS, project, path, who = '') {
    if (!ent || !act) return this.hide();
    if (ent !== this.target) { this.target = ent; this.dwell = 0; } else this.dwell += dt;
    const kind = KINDS[act.kind], style = G.marker, harm = act.kind === 'harm';
    const parts = priceLine(act, ent, walkS, true, who), time = parts[1];
    const dwelled = this.dwell > 0.5;
    const key = [ent.id, act.id, style, time, who, G.timeMode, dwelled && style === 'badge'].join('|');
    if (key !== this.key) { this.key = key; this.el.innerHTML = this.html(style, kind, act, ent, parts, dwelled); this.el.className = `mk mk-${style}${harm ? ' harm' : ''}`; this.el.style.setProperty('--c', kind.color); }
    // the walk as a dashed line on the ground (Time: Walk on ground)
    const showLine = G.timeMode === 'line' && path;
    this.line.visible = !!showLine;
    if (showLine) { this.line.geometry.setFromPoints(path.map(([x, z]) => new THREE.Vector3(x, 0.07, z))); this.line.computeLineDistances(); this.line.material.color.set(kind.color); }
    // ground ring
    const ring = harm ? this.dashed : this.solid; (harm ? this.solid : this.dashed).visible = false;
    ring.visible = style === 'ring' || style === 'built';
    if (ring.visible) {
      const s = (ent.r + 0.55) * 2.2 * (style === 'ring' ? 1 + 0.04 * Math.sin(performance.now() / 180) : 1);
      ring.position.set(ent.g.position.x, 0.04, ent.g.position.z); ring.scale.set(s, s, 1);
      ring.material.color.set(style === 'built' ? '#f1ead8' : kind.color);
    }
    const p = project(ent.g.position.x, ent.h + 0.35, ent.g.position.z); if (!p) return this.hide();
    const g = style === 'built' ? project(ent.g.position.x, 0, ent.g.position.z) : p;
    const gx = Math.max(style === 'pill' ? 190 : 60, Math.min(g.x, innerWidth - (style === 'pill' ? 190 : 340)));
    this.el.style.display = 'block'; this.el.style.transform = `translate(${Math.round(gx)}px,${Math.round(g.y)}px)`;
  }
  html(style, kind, act, ent, parts, dwelled) {
    const line = parts.join('<i>·</i>');
    if (style === 'pill') return `<div class="pill">${icon(act.icon, 20)}<span>${line}</span></div>`;
    if (style === 'badge') return `<div class="badge">${icon(act.icon, 24)}</div>${dwelled ? `<div class="pill side"><span>${line}</span></div>` : ''}`;
    if (style === 'built') return `<div class="tag">${ent.name}</div><div class="three">${icon('walk', 18)}${icon(act.icon, 18)}${icon('dots', 18)}</div><div class="plate">${act.t}<b>${icon('finger', 14)} ${act.trail || 0}</b><b>${icon('siren', 14)} ${act.alert ? '2' : '0'}</b></div>`;
    return `<div class="badge">${icon(act.icon, 24)}</div><div class="chip">${parts[1]}</div>`; // ring
  }
}
