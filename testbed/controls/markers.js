// The one marker for the main action. Four styles to compare.
//   ring  : coloured ring on the ground plus an icon badge and the time
//   pill  : one pill in the act's colour carrying the whole price line
//   badge : icon only; the line opens after a short dwell
//   built : the 0.2.34 look, for comparison (three icons on the ring, a name tag, a plate of numbers)
import * as THREE from './vendor/three.module.min.js';
import { G } from './state.js';
import { chargeFrac, fightOn } from './fire.js';
import { KINDS, ACTS, ROLES, PRESETS, priceLine, phrase } from './data.js';
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

// Who breaks away: a round face with the agent's colour rim, one per agent who will act (stacked when several).
export const facesHtml = (list) => (list?.length ? `<span class="faces">${list.map((a) => `<img src="faces/${a.name.toLowerCase()}_calm.png" style="border-color:${ROLES.find((r) => r.key === a.role).color}" alt="${a.name}">`).join('')}</span>` : '');

export class Marker {
  constructor(scene, el) {
    this.el = el; this.solid = groundRing(scene); this.dashed = groundRing(scene, true); this.key = ''; this.dwell = 0; this.target = null; this.ring = null;
    this.reach = groundRing(scene); this.reach.material.opacity = 0.7;
    this.line = new THREE.Line(new THREE.BufferGeometry(), new THREE.LineDashedMaterial({ dashSize: 0.4, gapSize: 0.28, depthTest: false, transparent: true })); this.line.renderOrder = 9; this.line.visible = false; this.line.frustumCulled = false; scene.add(this.line);
  }
  hide() { this.line.visible = false; this.reach.visible = false; this.el.style.display = 'none'; this.solid.visible = this.dashed.visible = false; this.target = null; this.dwell = 0; }
  update(dt, ent, act, walkS, project, path, faces = null, run = false, charge = null, extra = null, status = null) {
    if (!ent || !act) return this.hide();
    if (ent !== this.target) { this.target = ent; this.dwell = 0; } else this.dwell += dt;
    const kind = KINDS[act.kind], style = G.marker, harm = act.kind === 'harm';
    const parts = priceLine(act, ent, walkS, true, '', run); if (PRESETS[G.preset]?.holdActs && !charge && !(act.fire && fightOn())) parts.unshift('Hold'); const time = parts[parts.length > 2 && parts[0] === 'Hold' ? 2 : 1];
    const dwelled = this.dwell > 0.5;
    const key = [status?.text, status?.items?.map((i) => i.a.id + i.text).join(), extra?.seen, extra?.fresh, extra?.cx, charge ? 'chg' + !ent.hostile + !!charge.ready : '', ent.id, act.id, style, time, faces?.map((a) => a.id).join(), run, G.timeMode, dwelled && style === 'badge'].join('|');
    if (key !== this.key) { this.key = key; let html = status ? `<div class="pill st">${status.items ? status.items.map((i) => facesHtml([i.a]) + '<span>' + i.text + '</span>').join('<i>·</i>') : '<span>' + status.text + '</span>' + facesHtml(status.agents)}</div>` : charge ? `<div class="pill chg${charge.ready ? ' ready' : ''}"><i class="fill"></i><span>${charge.ready ? 'Let go to ' + phrase(charge.way, charge.ent) : 'Hold to ' + charge.label}${charge.bystander ? ' · bystander' : ''}${charge.extra && !charge.ready ? ' · ' + charge.extra : ''}</span></div>` : this.html(style, kind, act, ent, parts, dwelled, facesHtml(faces) + (extra?.seen ? '<span class="seen">seen there</span>' : ''));
      if (!status && extra?.fresh) html = html.replace('<span>', '<em class="new"></em><span>'); // brief 043: a lit pip on an act that just opened
      if (!status && extra?.cx) html += `<div class="cxw">${extra.cx}</div>`; // brief 043: what this order would drop, before you commit
      this.el.innerHTML = html; this.el.className = `mk mk-${style}${harm ? ' harm' : ''}`; this.el.style.setProperty('--c', kind.color); }
    if (charge) { const f = this.el.querySelector('.fill'); if (f) f.style.width = `${chargeFrac(charge) * 100}%`; }
    // Watch: how far this agent can watch from, around the target
    this.reach.visible = !!extra?.reach && extra.reach < 60;
    if (this.reach.visible) { this.reach.position.set(ent.g.position.x, 0.05, ent.g.position.z); this.reach.scale.setScalar(extra.reach * 2.286); this.reach.material.color.set(kind.color); }
    // the walk as a dashed line on the ground (Time: Walk on ground)
    const showLine = G.timeMode === 'line' && path;
    this.line.visible = !!showLine;
    if (showLine) { this.line.geometry.dispose(); this.line.geometry = new THREE.BufferGeometry().setFromPoints(path.map(([x, z]) => new THREE.Vector3(x, 0.07, z))); this.line.computeLineDistances(); this.line.material.color.set(kind.color); }
    // ground ring
    const ring = harm ? this.dashed : this.solid; (harm ? this.solid : this.dashed).visible = false;
    ring.visible = true; // the target always gets a ring on the ground, never a lift
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
  html(style, kind, act, ent, parts, dwelled, fc) {
    const line = parts.join('<i>·</i>');
    if (style === 'pill') return `<div class="pill">${icon(act.icon, 20)}<span>${line}</span>${fc}</div>`;
    if (style === 'badge') return `<div class="badge">${icon(act.icon, 24)}</div>${dwelled ? `<div class="pill side"><span>${line}</span>${fc}</div>` : ''}`;
    if (style === 'built') return `<div class="tag">${ent.name}</div><div class="three">${icon('walk', 18)}${icon(act.icon, 18)}${icon('dots', 18)}</div><div class="plate">${act.t}<b>${icon('finger', 14)} ${act.trail || 0}</b><b>${icon('siren', 14)} ${act.alert ? '2' : '0'}</b></div>`;
    return `<div class="badge">${icon(act.icon, 24)}</div><div class="chip">${parts[parts[0] === 'Hold' ? 2 : 1]}${fc}</div>`; // ring
  }
}
