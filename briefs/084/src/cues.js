// 084 cues: a look prototype. Plays one real engine run (seed 108, the reel's fight) over a backdrop stitched from the reel. It decides nothing about the game.
const D = JSON.parse(document.getElementById('data').textContent), A = {}, $ = id => document.getElementById(id);
const S = 54, K = 0.6157, LIFT = S * 0.788, BX = D.reg.bx, BY = D.reg.by, T0 = D.t0, T1 = D.t1, HZ = 10, W = 1920, H = 1080;
const INK = '#17302f', DEEP = '#0b1615', CREAM = '#f7f4ec', CHROME = '#cbc6c3', TURQ = '#60e0ea', AMBER = '#f0a020', RED = '#e03a2f', BODY = '#0c3434';
const LOOP_FROM = 2665, LOOP_TO = 2905; // the loop is the fight; a jump past it plays on to the end
const PRESETS = { built: 's=built,m=built,c=today,i=off,e=today,w=today,l=play', pick: 's=body,m=ring,c=bracket,i=darts,e=chip,w=hollow,l=one', flash: 's=hot,m=chev,c=thread,i=mark,e=rim,w=gone,l=one', quiet: 's=built,m=ring,c=selected,i=off,e=portrait,w=ring,l=one' };
const cv = $('view'), g = cv.getContext('2d');
const OPT = { s: 'body', m: 'ring', w: 'hollow', l: 'one', e: 'chip', c: 'bracket', i: 'darts' }, CELL = ['scout', 'fixer', 'tech', 'enforcer'];
const st = { t: 2775, play: true, speed: 1, hide: false, sel: new Set(['fixer']), mouse: null, cam: null, camTo: null, way: false, hot: [], watched: true, backAt: null, alt: false };
const camOf = t => { const a = D.cam; let lo = 0, hi = a.length - 1; while (lo < hi) { const m = (lo + hi + 1) >> 1; if (a[m][0] <= t) lo = m; else hi = m - 1; } return a[lo]; };
const proj = (x, y, o, h = 0) => [S * (-0.5 * x - 0.866 * y) + BX + o[0], S * K * (-0.866 * x + 0.5 * y) + BY + o[1] - h * LIFT];
const unproj = (sx, sy, o) => { const u = (sx - BX - o[0]) / S, v = (sy - BY - o[1]) / (S * K); return [-0.5 * u - 0.866 * v, -0.866 * u + 0.5 * v]; };

// ---- the run: positions per tick, flags, health
const E = new Map();
for (const e of D.ents) {
  const n = T1 - T0 + 1, X = new Float32Array(n).fill(NaN), Y = new Float32Array(n).fill(NaN);
  for (const sp of e.spans) {
    let x = sp[1], y = sp[2], t = sp[0];
    const put = (t, x, y) => { if (t >= T0 && t <= T1) { X[t - T0] = x / 100; Y[t - T0] = y / 100; } };
    put(t, x, y);
    for (let i = 3; i < sp.length; i += 2) {
      const nx = x + sp[i], ny = y + sp[i + 1];
      for (let k = 1; k <= e.step; k++) put(t + k, x + (nx - x) * k / e.step, y + (ny - y) * k / e.step);
      x = nx; y = ny; t += e.step;
    }
  }
  E.set(e.id, { ...e, X, Y });
}
const at = (e, t) => { const i = Math.round(Math.max(T0, Math.min(T1, t))) - T0, x = e.X[i], y = e.Y[i]; if (Number.isNaN(x)) return null; const j = Math.min(T1 - T0, i + 1), f = t - Math.floor(t); return Number.isNaN(e.X[j]) ? [x, y] : [e.X[Math.floor(Math.max(T0, t)) - T0] + (e.X[j] - e.X[Math.floor(Math.max(T0, t)) - T0]) * f, e.Y[Math.floor(Math.max(T0, t)) - T0] + (e.Y[j] - e.Y[Math.floor(Math.max(T0, t)) - T0]) * f]; };
const key = (arr, t) => { let r = null; for (const k of arr) { if (k[0] <= t) r = k; else break; } return r; };
const isDown = (e, t) => { const k = key(e.st || [], t); return !!(k && (k[1] & 1)); };
const hpOf = (e, t) => { const a = e.hp || []; let v = e.hpMax; for (let i = 0; i + 1 < a.length; i += 2) { if (a[i] <= t) v = a[i + 1]; else break; } return v; };
const isHostile = e => e.side === 'rival' || e.side === 'police' || e.id === 'guard';

// ---- who fires at whom: the engine's own picks and shots
const aims = [];
for (const s of D.shots) aims.push({ t: s[0], by: s[1], to: s[2], shot: s });
for (const l of D.log) {
  let m;
  if (l[1] === 'fire.free' && (m = l[2].match(/^(\w+) picks ([\w-]+)/))) aims.push({ t: l[0], by: m[1], to: m[2] });
  else if (l[1] === 'ai.why' && (m = l[2].match(/^([\w-]+) takes the (\w+) as its target/))) aims.push({ t: l[0], by: m[1], to: m[2] });
  else if (/^fire\./.test(l[1]) && (m = l[2].match(/^(\w+) fires on ([\w-]+) until down/))) aims.push({ t: l[0], by: m[1], to: m[2], focus: true });
}
aims.sort((a, b) => a.t - b.t);
const fightOn = t => t >= (D.log.find(l => l[1] === 'fire.free-on')?.[0] ?? 1e9) && t < (D.log.find(l => l[1] === 'fight.over')?.[0] ?? 1e9);
function targets(t) {
  const m = new Map();
  for (const a of aims) { if (a.t > t) break; m.set(a.by, { by: a.by, to: a.to, t: a.t, last: a.shot ? a.t : (m.get(a.by)?.last ?? -99), focus: a.focus || (m.get(a.by)?.focus && m.get(a.by).to === a.to) }); }
  for (const [by, v] of m) { const a = E.get(by), b = E.get(v.to); if (!a || !b || isDown(a, t) || isDown(b, t) || t - v.t > 40) m.delete(by); }
  return m;
}

// ---- windows: as WindowsRead keeps them
const isBlind = kind => kind === 'reroute' || kind === 'loop';
const windowsAt = t => { const k = key(D.windows, t), out = []; if (!k || !k[1]) return out; for (const w of k[1].split(' ')) { const [kind, thing, u] = w.split(':'); if (kind === 'seal') continue; const until = st.backAt && isBlind(kind) ? st.backAt : +u; if (until <= t) continue; let from = T0; for (const q of D.windows) { if (q[0] > t) break; if (q[1].includes(`${kind}:${thing}:${u}`)) { from = q[0]; break; } } out.push({ kind, thing, until, from }); } return out; };
const WIN = {
  reroute: { face: 'npc_patrol', says: "Can't see you, back in", key: 'patrol', fan: 'patrol' }, call: { face: 'signal_guard', says: 'Can see you. Back at the door in', key: 'guard' },
  loop: { face: 'npc_watcher', says: "Can't see you, back in", cam: 'entrance', fan: 'camera:entrance' }, mislead: { face: 'signal_rival', says: 'Rivals misled, back in', car: 'car:rivals' },
};
const fmt = s => s >= 60 ? `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}` : String(s);
const carAt = (id, t) => { const c = D.cars.find(c => c.id === id); const k = c && key(c.keys, t); return k ? [k[1] / 100, k[2] / 100] : null; };
function winAnchor(w, t, o, today) {
  const d = WIN[w.kind]; if (!d) return null;
  if (d.key) { const e = E.get(d.key), p = e && at(e, t); if (!p) return null; const h = d.key === 'patrol' ? (today ? 16.6 : 1.7) : 2.45; return { w: p, s: proj(p[0], p[1], o, h), g: proj(p[0], p[1], o) }; }
  if (d.cam) { const c = D.cameras.find(c => c.id === d.cam), p = [c.at[0] / 100, c.at[1] / 100]; return { w: p, s: proj(p[0], p[1], o, 2.35), g: proj(p[0], p[1], o) }; }
  const p = carAt(d.car, t); return p ? { w: p, s: proj(p[0], p[1], o, 1.7), g: proj(p[0], p[1], o) } : null;
}

// ---- drawing helpers
const img = n => A[n];
function nine(n, x, y, w, h, c = 10.3, src = 24) { const m = img(n); if (!m) return; const iw = m.width, ih = m.height; const P = (sx, sy, sw, sh, dx, dy, dw, dh) => g.drawImage(m, sx, sy, sw, sh, dx, dy, dw, dh);
  P(0, 0, src, src, x, y, c, c); P(iw - src, 0, src, src, x + w - c, y, c, c); P(0, ih - src, src, src, x, y + h - c, c, c); P(iw - src, ih - src, src, src, x + w - c, y + h - c, c, c);
  P(src, 0, iw - 2 * src, src, x + c, y, w - 2 * c, c); P(src, ih - src, iw - 2 * src, src, x + c, y + h - c, w - 2 * c, c); P(0, src, src, ih - 2 * src, x, y + c, c, h - 2 * c); P(iw - src, src, src, ih - 2 * src, x + w - c, y + c, c, h - 2 * c); P(src, src, iw - 2 * src, ih - 2 * src, x + c, y + c, w - 2 * c, h - 2 * c); }
function icon(n, x, y, s, tint) { const m = img(n); if (!m) return; if (!tint) { g.drawImage(m, x - s / 2, y - s / 2, s, s); return; } const key = n + tint; let c = tinted[key]; if (!c) { c = document.createElement('canvas'); c.width = m.width; c.height = m.height; const q = c.getContext('2d'); q.drawImage(m, 0, 0); q.globalCompositeOperation = 'source-in'; q.fillStyle = tint; q.fillRect(0, 0, c.width, c.height); tinted[key] = c; } g.drawImage(c, x - s / 2, y - s / 2, s, s); }
const tinted = {};
function cameo(n, x, y, r, rim = CHROME) { const m = img(n); g.save(); g.beginPath(); g.arc(x, y, r, 0, 7); g.clip(); if (m) g.drawImage(m, x - r, y - r, 2 * r, 2 * r); g.restore(); g.strokeStyle = DEEP; g.lineWidth = 3.4; g.beginPath(); g.arc(x, y, r + .6, 0, 7); g.stroke(); g.strokeStyle = rim; g.lineWidth = 2; g.beginPath(); g.arc(x, y, r + .6, 0, 7); g.stroke(); }
function text(s, x, y, size = 14.5, col = CREAM, align = 'left') { g.font = `700 ${size}px Jost, sans-serif`; g.fillStyle = col; g.textAlign = align; g.textBaseline = 'middle'; g.fillText(s, x, y); return g.measureText(s).width; }
function line2(pts, w, col, under = DEEP, ua = 0.55, dash) { g.save(); g.lineCap = 'round'; g.lineJoin = 'round'; if (dash) g.setLineDash(dash); g.beginPath(); pts.forEach((p, i) => i ? g.lineTo(p[0], p[1]) : g.moveTo(p[0], p[1])); g.globalAlpha *= ua; g.strokeStyle = under; g.lineWidth = w + 2.2; g.stroke(); g.globalAlpha /= ua; g.strokeStyle = col; g.lineWidth = w; g.stroke(); g.restore(); }
function ground(p, r, col, w, alpha = 1) { g.save(); g.globalAlpha = alpha; g.strokeStyle = col; g.lineWidth = w; g.beginPath(); g.ellipse(p[0], p[1], r * S, r * S * K, 0, 0, 7); g.stroke(); g.restore(); }
function plate(x, y, parts, opts = {}) {
  // the window marker: plate_dark 38 px, a face or an icon, the count, the bar
  g.font = '700 14.5px Jost, sans-serif'; const tw = g.measureText(parts.txt).width, lead = parts.face ? 24 : 15, w = 11 + lead + 6 + tw + 12, h = 38, l = x - w / 2, t = y - h - (opts.tail ? 11 : 0);
  nine('plate_dark', l, t, w, h);
  if (parts.face) g.drawImage(img(parts.face), l + 9, t + 6.5, 24, 24); else icon(parts.icon, l + 11 + 7.5, t + 18, 15, CREAM);
  text(parts.txt, l + 11 + lead + 6, t + 18, 14.5);
  if (parts.frac != null) { g.fillStyle = 'rgba(11,22,21,.6)'; g.fillRect(l + 11, t + 29, w - 22, 2.5); g.fillStyle = parts.low ? AMBER : TURQ; g.fillRect(l + 11, t + 29, (w - 22) * Math.max(0, Math.min(1, parts.frac)), 2.5); }
  if (opts.tail) { g.fillStyle = CHROME; g.beginPath(); g.moveTo(x - 9, t + h - 2); g.lineTo(x + 9, t + h - 2); g.lineTo(x, t + h + 9); g.fill(); g.fillStyle = BODY; g.beginPath(); g.moveTo(x - 6, t + h - 2.5); g.lineTo(x + 6, t + h - 2.5); g.lineTo(x, t + h + 5); g.fill(); }
  return [l, t, w, h];
}
function tip(s, x, y) { g.font = '700 13.5px Jost, sans-serif'; const w = g.measureText(s).width + 22; nine('plate_dark', x - w / 2, y - 26, w, 26, 8); text(s, x, y - 13, 13.5, CREAM, 'center'); }

// ---- stand-in figures, to scale at zoom 10 (about 80 px). Bodies are not the point here.
const LOOK = { cell: ['#23282c', '#3a4248', '#c99a78'], rival: ['#6d43b8', '#e9e4f4', '#b98a6a'], police: ['#2c2a25', '#3b382f', '#c99a78'], guard: ['#efe9d8', '#caa04c', '#c99a78'], named: ['#d8cdb8', '#7d5a48', '#b98a6a'], vale: ['#3c5a72', '#d9d4c6', '#d3a988'], civ: ['#b9a999', '#8f8478', '#c49a7c'] };
const CELLBODY = { fixer: ['#e4ddcf', '#2a2c2e', '#b98a6a'], tech: ['#23282c', '#2f3438', '#6b4630'], scout: ['#2a2e33', '#3a4046', '#c99a78'], enforcer: ['#3a3632', '#2c2a26', '#d3a988'] }; // roughly as the real four read in the video: not one uniform
const HOT = '#ff7a1a', HOTCORE = '#fff3c4', WHITE = ['#ffffff', '#ffffff', '#ffffff'];
const SHOTS_ON = new Map(), SHOTS_BY = new Map(); for (const s of D.shots) { if (s[7]) (SHOTS_ON.get(s[2]) || SHOTS_ON.set(s[2], []).get(s[2])).push(s); (SHOTS_BY.get(s[1]) || SHOTS_BY.set(s[1], []).get(s[1])).push(s); }
const lastOf = (arr, t) => { let r = null; if (arr) for (const s of arr) { if (s[0] <= t) r = s; else break; } return r; };
function shotDir(s, o) { const a = proj(s[3] / 100, s[4] / 100, o), b = proj(s[5] / 100, s[6] / 100, o), n = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1; return [(b[0] - a[0]) / n, (b[1] - a[1]) / n]; }
function bodyReact(e, t, o) { // what size of motion to ask for: knocked along the shot and flashed white when hit, a small kick back on his own shot
  let dx = 0, dy = 0, white = false; if (OPT.s !== 'body') return { dx, dy, white };
  const h = lastOf(SHOTS_ON.get(e.id), t); if (h) { const age = (t - h[0]) / HZ; if (age < 0.5) { const u = shotDir(h, o), k = age < 0.06 ? age / 0.06 : Math.pow(1 - (age - 0.06) / 0.44, 2); dx += u[0] * 15 * k; dy += u[1] * 15 * k; if (age < 0.12) white = true; } }
  const f = lastOf(SHOTS_BY.get(e.id), t); if (f) { const age = (t - f[0]) / HZ; if (age < 0.14) { const u = shotDir(f, o), k = 1 - age / 0.14; dx -= u[0] * 5 * k; dy -= u[1] * 5 * k; } }
  return { dx, dy, white };
}
function figure(e, p, t, face, firing, white) {
  const [x, y] = p, c = white ? WHITE : (e.side === 'cell' && CELLBODY[e.id]) || LOOK[e.side] || LOOK.civ, civ = e.side === 'civ';
  if (e.id === 'patrol') { g.fillStyle = 'rgba(60,50,30,.28)'; g.beginPath(); g.ellipse(x + 8, y + 2, 26, 12, 0, 0, 7); g.fill(); g.fillStyle = '#26302f'; g.fillRect(x - 21, y - 11, 42, 12); g.fillStyle = '#f1e8d5'; g.beginPath(); g.ellipse(x, y - 24, 20, 17, 0, 0, 7); g.fill(); g.fillStyle = '#e9e3d4'; g.fillRect(x - 16, y - 58, 32, 26); g.fillStyle = '#6fc9b2'; g.fillRect(x - 16, y - 62, 32, 7); return; }
  if (isDown(e, t)) { g.fillStyle = 'rgba(60,50,30,.25)'; g.beginPath(); g.ellipse(x, y + 2, 44, 10, -0.1, 0, 7); g.fill(); g.strokeStyle = c[1]; g.lineWidth = 8; g.lineCap = 'round'; g.beginPath(); g.moveTo(x, y - 3); g.lineTo(x + 34, y - 9); g.stroke(); g.strokeStyle = c[0]; g.lineWidth = 14; g.beginPath(); g.moveTo(x - 24, y); g.lineTo(x + 2, y - 3); g.stroke(); g.fillStyle = c[2]; g.beginPath(); g.arc(x - 34, y + 1, 7, 0, 7); g.fill(); return; }
  const sc = civ ? 0.86 : 1, step = Math.sin(t * 2.2) * ((e._mv || 0) > 0.03 ? 5 : 0.8);
  g.fillStyle = 'rgba(60,50,30,.24)'; g.beginPath(); g.ellipse(x + 14 * sc, y + 2, 26 * sc, 8 * sc, 0.25, 0, 7); g.fill();
  g.lineCap = 'round'; g.strokeStyle = c[1]; g.lineWidth = 6 * sc; g.beginPath(); g.moveTo(x - 4, y - 36 * sc); g.lineTo(x - 5 - step, y); g.moveTo(x + 4, y - 36 * sc); g.lineTo(x + 5 + step, y); g.stroke();
  g.strokeStyle = c[0]; g.lineWidth = 16 * sc; g.beginPath(); g.moveTo(x, y - 40 * sc); g.lineTo(x, y - 60 * sc); g.stroke();
  g.fillStyle = c[2]; g.beginPath(); g.arc(x, y - 74 * sc, 7.5 * sc, 0, 7); g.fill();
  // the arm, along the facing: a raised gun when a target is held
  const fx = face ? face[0] : 0, fy = face ? face[1] : 0, n = Math.hypot(fx, fy) || 1;
  g.strokeStyle = c[0]; g.lineWidth = 5.5 * sc; g.beginPath(); g.moveTo(x, y - 58 * sc);
  if (firing) { g.lineTo(x + fx / n * 20, y - 58 * sc + fy / n * 12); g.stroke(); g.strokeStyle = DEEP; g.lineWidth = 3; g.beginPath(); g.moveTo(x + fx / n * 20, y - 58 * sc + fy / n * 12); g.lineTo(x + fx / n * 29, y - 58 * sc + fy / n * 17); g.stroke(); }
  else { g.lineTo(x + fx / n * 7, y - 36 * sc); g.stroke(); }
}
const muzzle = e => [0, 58];
function star(a, u, r) { const n = [-u[1], u[0]]; for (const [c, k] of [['rgba(23,48,47,.6)', r + 2], ['#fff7dc', r]]) { g.fillStyle = c; g.beginPath(); g.moveTo(a[0] + u[0] * k * 1.8, a[1] + u[1] * k * 1.8); g.lineTo(a[0] + n[0] * k * 0.4, a[1] + n[1] * k * 0.4); g.lineTo(a[0] + n[0] * k, a[1] + n[1] * k); g.lineTo(a[0] - u[0] * k * 0.4, a[1] - u[1] * k * 0.4); g.lineTo(a[0] - n[0] * k, a[1] - n[1] * k); g.lineTo(a[0] + u[0] * k * 0.4, a[1] + u[1] * k * 0.4); g.closePath(); g.fill(); } }

// ---- cones: the engine's own sight shapes. Live: filled and rimmed as built, only when it matters. Blind: per the option.
const FANS = D.fans.map(f => ({ ...f, id: f.id, pts: new Map() }));
const fanPts = (f, s) => { let v = f.pts.get(s); if (!v) { const n = s.split(',').map(Number); v = []; for (let i = 0; i + 1 < n.length; i += 2) v.push([n[i] / 10, n[i + 1] / 10]); f.pts.set(s, v); } return v; };
const inPoly = (p, P) => { let c = false; for (let i = 0, n = P.length; i < n; i++) { const a = P[i], b = P[(i + 1) % n]; if ((a[1] > p[1]) !== (b[1] > p[1]) && p[0] < (b[0] - a[0]) * (p[1] - a[1]) / (b[1] - a[1]) + a[0]) c = !c; } return c; };
const fanCol = f => f.kind === 'guard' ? AMBER : RED;
const facePath = (pts, o) => { g.beginPath(); pts.forEach((p, i) => { const s = proj(p[0], p[1], o); i ? g.lineTo(s[0], s[1]) : g.moveTo(s[0], s[1]); }); g.closePath(); };
function liveCone(pts, o, col, a = 1) { g.save(); facePath(pts, o); g.globalAlpha = 0.17 * a; g.fillStyle = col; g.fill(); g.lineJoin = 'round'; g.globalAlpha = 0.6 * a; g.strokeStyle = DEEP; g.lineWidth = 3.4; g.stroke(); g.globalAlpha = a; g.strokeStyle = col; g.lineWidth = 2; g.stroke(); g.restore(); }
function drawCones(t, o) {
  const m = st.mouse, wins = windowsAt(t);
  for (const f of FANS) {
    const k = key(f.keys, t); if (!k) continue; const on0 = k[1][0] === '1', pts = fanPts(f, k[1].slice(2)); if (pts.length < 3) continue; const win = wins.find(w => WIN[w.kind]?.fan === f.id);
    const realBlind = !on0, blind = realBlind && !(st.backAt && t >= st.backAt); if (f.kind === 'rivals' || f.kind === 'security') continue;
    const apex = proj(pts[0][0], pts[0][1], o); const who = E.get(f.id), wp = who && at(who, t), ws = wp ? proj(wp[0], wp[1], o) : apex;
    const hover = m && Math.abs(m[0] - ws[0]) < 36 && m[1] > ws[1] - 90 && m[1] < ws[1] + 16;
    if (!st.watched) { if (st.alt && !fightOn(t) || hover) { const q = [ws[0], ws[1] - (who ? (f.id === 'patrol' ? 78 : 100) : 110)]; g.drawImage(img('disc_dark_24'), q[0] - 12, q[1] - 12, 24, 24); icon('eye_unk', q[0], q[1], 16, CREAM); } continue; }
    if (!blind) { let want = st.alt || hover || st.showFan === f.id || (realBlind && t - st.backAt < 60); if (!want) for (const c of st.sel) { const p = at(E.get(c), t); if (p && inPoly(p, pts)) want = true; } if (want) liveCone(pts, o, fanCol(f)); continue; }
    const left = win ? (win.until - t) / HZ : 99, mode = OPT.w, col = fanCol(f);
    if (mode === 'today') { g.save(); facePath(pts, o); g.fillStyle = 'rgba(120,120,120,.08)'; g.fill(); g.clip(); g.strokeStyle = 'rgba(90,90,90,.28)'; g.lineWidth = 1.2; g.beginPath(); for (let x = -W; x < W + H; x += 18) { g.moveTo(x, 0); g.lineTo(x + H, H); } g.stroke(); g.restore(); facePath(pts, o); g.strokeStyle = 'rgba(90,90,90,.8)'; g.lineWidth = 1.6; g.stroke(); continue; }
    if (mode === 'ring') continue;
    if (mode === 'hollow') { g.save(); facePath(pts, o); g.lineJoin = 'round'; g.setLineDash([12, 10]); g.globalAlpha = 0.55; g.strokeStyle = DEEP; g.lineWidth = 3.8; g.stroke(); g.globalAlpha = 0.95; g.strokeStyle = CREAM; g.lineWidth = 2; g.stroke(); g.restore(); }
    if (left < 10) { const R0 = 40 * S * (1 - left / 10); g.save(); g.beginPath(); g.ellipse(apex[0], apex[1], R0, R0 * K, 0, 0, 7); g.clip(); liveCone(pts, o, col); g.restore(); }
  }
}

// ---- bracket: four corners round a person; a hit snaps them in, a miss twitches them out
function bracket(s, t, lastShot, col = CREAM, thick = 2.4) {
  const age = lastShot ? (t - lastShot.t) / HZ : 9; let w = 25, h = 54, cy = s[1] - 42, a = 1, c = col;
  if (lastShot && age < 0.35) { const k = 1 - age / 0.35; if (lastShot.hit) { w -= 6 * k; h -= 8 * k; c = '#ffffff'; } else { w += 7 * k * (1 + Math.sin(age * 90) * 0.4); h += 6 * k; a = 0.75; } }
  const L = 11; g.save(); g.globalAlpha = a; g.lineCap = 'square';
  for (const [col2, wd] of [[DEEP, thick + 2.2], [c, thick]]) { g.strokeStyle = col2; g.lineWidth = wd; g.beginPath();
    for (const [sx, sy] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) { const x = s[0] + sx * w, y = cy + sy * h; g.moveTo(x - sx * L, y); g.lineTo(x, y); g.lineTo(x, y - sy * L); } g.stroke(); }
  g.restore();
}

// ---- edge indicators: one system, one list
const onScreen = s => s[0] > 20 && s[0] < W - 30 && s[1] > 60 && s[1] < H - 10 && !(s[0] < 150 && s[1] < 470); // the roster's column counts as off screen
const LOGSEE = D.log.filter(l => l[1] === 'attention.noticed' || l[1] === 'run.seen').map(l => { const m = l[2].match(/^(?:the camera |The )?([\w-]+)(?: camera)? (?:notices|sees)/); return m ? { t: l[0], who: m[1], text: l[2] } : null; }).filter(Boolean);
const LABEL = new Map(); for (const e of E.values()) LABEL.set(e.label, e);
const CALLS = D.log.filter(l => l[1] === 'witness.crowd').map(l => { const e = LABEL.get(l[2].split(' sees ')[0]); return e ? { t: l[0], who: e.id } : null; }).filter(Boolean);
const FACE = id => { const e = E.get(id); if (!e) return 'npc_watcher'; return e.id === 'patrol' ? 'npc_patrol' : e.id === 'guard' ? 'signal_guard' : e.side === 'rival' ? 'signal_rival' : e.side === 'police' ? 'signal_police' : 'npc_passer'; };
function edgeList(t, o, tn) {
  const out = new Map(), HOLD = 25, RANK = { fire: 4, call: 3, come: 2, see: 1, aim: 0 };
  const add = (id, kind, last, on) => { const e = E.get(id), c = D.cameras.find(c => c.id === id); const p = e ? at(e, t) : c ? [c.at[0] / 100, c.at[1] / 100] : null; if (!p || (e && isDown(e, t))) return; const s = proj(p[0], p[1], o); if (onScreen(s)) return; const q = out.get(id); if (!q || RANK[kind] > RANK[q.kind]) out.set(id, { id, kind, last, on, p, s, why: kind }); else q.last = Math.max(q.last, last); };
  for (const a of aims) { if (a.t > t) break; if (!a.shot) continue; const by = E.get(a.by); if (by && by.side !== 'cell' && t - a.t < HOLD) add(a.by, 'fire', a.t, a.to); }
  for (const c of CALLS) if (c.t <= t && t - c.t < HOLD + 15) add(c.who, 'call', c.t);
  for (const s of LOGSEE) if (s.t <= t && t - s.t < HOLD) add(s.who, 'see', s.t);
  for (const s of D.seen) if (s[2] <= t && t <= s[3] + HOLD) add(s[0], 'see', Math.min(t, s[3]), s[1]);
  if (tn && OPT.e === 'chip' && TARGET_MARKS()) for (const [by, v] of tn) if (E.get(by).side === 'cell' && (OPT.c !== 'selected' || st.sel.has(by))) add(v.to, 'aim', t);
  for (const e of E.values()) if (e.side === 'rival' || e.side === 'police') { const k = key(e.st || [], t); if (k && /front|advance|engage|walk|check/.test(k[2] || '') && !(k[1] & 1) && !/spot|lookout|aboard/.test(k[2])) { const p = at(e, t), b = at(e, Math.max(T0, t - 10)); if (p && b && Math.hypot(p[0] - b[0], p[1] - b[1]) > 0.1) add(e.id, 'come', t); } }
  const arr = [...out.values()].sort((a, b) => RANK[b.kind] - RANK[a.kind] || b.last - a.last);
  return { all: arr, shown: arr.slice(0, 4), more: Math.max(0, arr.length - 4) };
}
const TARGET_MARKS = () => OPT.c === 'disc' || OPT.c === 'bracket' || OPT.c === 'selected';
const edgePt = (s, inset = 44) => { const cx = W / 2 + 60, cy = H / 2, dx = s[0] - cx, dy = s[1] - cy, k = Math.min((W - cx - inset) / Math.max(1e-3, Math.abs(dx)), (cy - inset) / Math.max(1e-3, Math.abs(dy))), x = cx + dx * k, y = cy + dy * k; return [y < 470 ? Math.max(190, x) : Math.max(inset, x), Math.min(H - Math.min(inset, 60), y), Math.atan2(dy, dx)]; };
const KIND_ICON = { fire: 'fire', call: 'radio', come: 's_moving', see: 'sawyou' };

function drawEdges(t, o, tn) {
  const mode = OPT.e; st.hot = st.hot.filter(h => h.tag !== 'edge'); if (mode === 'off') return;
  const L = edgeList(t, o, tn), used = [];
  const free = (p, r) => { for (let i = 0; i < 6; i++) { if (!used.some(q => Math.hypot(q[0] - p[0], q[1] - p[1]) < r * 2 + 6)) break; if (p[0] <= 195 || p[0] >= W - 60) p[1] += (r * 2 + 8); else p[0] += (r * 2 + 8); } p[1] = Math.min(H - 36, p[1]); used.push([p[0], p[1]]); return p; };
  if (mode === 'today') { // the docked callout as built: a stage change only, 56 px, a chevron toward it
    for (const s of LOGSEE) { if (s.t > t || t - s.t > 40) continue; const e = E.get(s.who), c = D.cameras.find(c => c.id === s.who), p = e ? at(e, t) : c ? [c.at[0] / 100, c.at[1] / 100] : null; if (!p) continue; const q = proj(p[0], p[1], o); if (q[0] > 150 && q[0] < W && q[1] > 0 && q[1] < H) continue; const a = free(edgePt(q, 46), 34); cameo(FACE(s.who), a[0], a[1], 28); g.strokeStyle = CREAM; g.lineWidth = 2; g.beginPath(); g.arc(a[0], a[1], 28, 0, 7); g.stroke(); st.hot.push({ tag: 'edge', x: a[0] - 30, y: a[1] - 30, w: 60, h: 60, go: p }); }
    return;
  }
  if (mode === 'rim') { // no faces: a wedge on the rim, the kind by its shape, fire red and pulsing
    for (const it of L.all.slice(0, 6)) { const a = free(edgePt(it.s, 12), 44), ang = a[2], fire = it.kind === 'fire', age = (t - it.last) / HZ, pulse = fire ? Math.max(0, 1 - age / 0.5) : 0; g.save(); g.translate(a[0], a[1]); g.rotate(ang); g.globalAlpha = 0.65 + 0.35 * pulse; g.fillStyle = fire ? RED : it.kind === 'call' ? AMBER : CREAM; g.strokeStyle = DEEP; g.lineWidth = 1.6; g.beginPath(); g.moveTo(10, 0); g.lineTo(-10, -30 - 8 * pulse); g.lineTo(-6, 0); g.lineTo(-10, 30 + 8 * pulse); g.closePath(); g.fill(); g.stroke(); g.restore(); st.hot.push({ tag: 'edge', x: a[0] - 30, y: a[1] - 40, w: 60, h: 80, go: it.p }); }
    return;
  }
  if (mode === 'portrait') { // on the roster: whoever has you, beside the agent it has
    const rows = {}; for (const it of L.all) { let on = it.on; if (!on) { let best = 1e9; for (const c of CELL) { const p = at(E.get(c), t); if (p) { const d = Math.hypot(p[0] - it.p[0], p[1] - it.p[1]); if (d < best) { best = d; on = c; } } } } (rows[on] = rows[on] || []).push(it); }
    CELL.forEach((c, i) => { (rows[c] || []).slice(0, 2).forEach((it, j) => { const x = 134 + j * 66, y = 70 + i * 96, s = it.s, ang = Math.atan2(s[1] - y, s[0] - x); nine('plate_dark', x - 12, y - 19, 58, 38); cameo(FACE(it.id), x + 9, y, 12); g.save(); g.translate(x + 33, y); g.rotate(ang); g.fillStyle = it.kind === 'fire' ? RED : CREAM; g.beginPath(); g.moveTo(8, 0); g.lineTo(-6, -6); g.lineTo(-3, 0); g.lineTo(-6, 6); g.fill(); g.restore(); st.hot.push({ tag: 'edge', x: x - 12, y: y - 19, w: 58, h: 38, go: it.p }); }); });
    return;
  }
  // chip: the watcher's face on the edge, an arrow out, a badge for what it's doing; fire rings on each shot
  const shootersOf = id => { const r = []; for (const [by, v] of tn) if (v.to === id && E.get(by).side === 'cell' && (OPT.c !== 'selected' || st.sel.has(by))) r.push(by); return r; };
  const merged = []; for (const it of L.shown) { it.shoot = TARGET_MARKS() ? shootersOf(it.id) : []; const q = merged.find(m => FACE(m.id) === FACE(it.id) && m.kind === it.kind && Math.hypot(m.s[0] - it.s[0], m.s[1] - it.s[1]) < 260); if (q) { q.n = (q.n || 1) + 1; for (const c of it.shoot) if (!q.shoot.includes(c)) q.shoot.push(c); } else merged.push(it); }
  for (const it of merged) {
    const a = free(edgePt(it.s, 60), 26), ang = a[2], age = (t - it.last) / HZ, CC = 26;
    g.save(); g.translate(a[0], a[1]); g.rotate(ang); g.fillStyle = CHROME; g.beginPath(); g.moveTo(44, 0); g.lineTo(20, -14); g.lineTo(20, 14); g.fill(); g.fillStyle = BODY; g.beginPath(); g.moveTo(39, 0); g.lineTo(20, -10); g.lineTo(20, 10); g.fill(); g.restore();
    cameo(FACE(it.id), a[0], a[1], 21);
    if (it.kind === 'fire' && age < 0.5) { g.strokeStyle = RED; g.lineWidth = 3 * (1 - age / 0.5) + 1; g.beginPath(); g.arc(a[0], a[1], 24 + age * 16, 0, 7); g.stroke(); }
    const bx = a[0] + 16, by = a[1] + 16; if (it.kind === 'fire') { g.drawImage(img('disc_light_24'), bx - 12, by - 12, 24, 24); icon(KIND_ICON.fire, bx, by, 12, INK); } else if (it.kind !== 'aim') { g.drawImage(img('disc_dark_24'), bx - 12, by - 12, 24, 24); icon(KIND_ICON[it.kind], bx, by, 12, CREAM); }
    if (it.shoot.length) { // ours are firing at this one: the same faces as on screen, beside its chip
      it.shoot.slice(0, 4).forEach((c, i) => cameo(`cameo_${c}`, a[0] - 13 + i * 15, a[1] + (a[1] < 80 ? 38 : -34), 10, st.sel.has(c) ? TURQ : CHROME));
    }
    if (it.n > 1) { nine('plate_dark', a[0] + 10, a[1] - 40, 26, 20, 6); text(String(it.n), a[0] + 23, a[1] - 30, 12.5, CREAM, 'center'); }
    st.hot.push({ tag: 'edge', x: a[0] - CC, y: a[1] - CC, w: 2 * CC, h: 2 * CC, go: it.p, say: { fire: 'Firing at you', call: 'Calling it in', come: 'Coming to you', see: 'Sees you', aim: 'You are firing here' }[it.kind] });
  }
  if (L.more > 0) { const a = L.shown.length ? used[used.length - 1] : [W - 60, 60]; nine('plate_dark', a[0] - 18, a[1] + 26, 36, 22, 7); text('+' + L.more, a[0], a[1] + 37, 12.5, CREAM, 'center'); }
}

// ---- the frame
let lastFrame = performance.now();
function frame(now) {
  const dt = Math.min(0.1, (now - lastFrame) / 1000); lastFrame = now;
  if (st.play) { st.t += dt * HZ * st.speed; if ((st.t >= LOOP_TO && st.from < LOOP_TO) || st.t >= T1) { st.t = LOOP_FROM; st.from = LOOP_FROM; st.backAt = null; } }
  $('scrub').value = st.t; $('clock').textContent = `${Math.floor(st.t / HZ / 60)}:${String(Math.floor(st.t / HZ) % 60).padStart(2, '0')}`;
  draw(st.t); requestAnimationFrame(frame);
}
function draw(t) {
  const reel = camOf(t); let o = st.cam || [reel[1], reel[2]];
  if (st.camTo) { const c = st.cam || o, k = 0.14; st.cam = [c[0] + (st.camTo[0] - c[0]) * k, c[1] + (st.camTo[1] - c[1]) * k]; o = st.cam; if (Math.hypot(st.camTo[0] - c[0], st.camTo[1] - c[1]) < 1) st.camTo = null; }
  g.setTransform(cv.width / W, 0, 0, cv.height / H, 0, 0);
  g.fillStyle = '#efe6d2'; g.fillRect(0, 0, W, H);
  g.drawImage(img('mosaic'), D.reg.minx + o[0], D.reg.miny + o[1]);
  const tNow = targets(t), hotW = []; st.hot = [];
  // the getaway's ring as built, when the seal or the fight is on (§10)
  if (t >= 2880) { const p = proj(69.35, -15, o); ground(p, 8, '#8fd8cf', 2.2, 0.85); }
  // ground: selected rings
  const faces = new Map();
  const people = [...E.values()].map(e => { const p = at(e, t); if (!p) return null; const s = proj(p[0], p[1], o); if (s[0] < -80 || s[0] > W + 80 || s[1] < -30 || s[1] > H + 120) return null; const b = at(e, Math.max(T0, t - 3)); e._mv = b ? Math.hypot(p[0] - b[0], p[1] - b[1]) : 0; return { e, p, s }; }).filter(Boolean).sort((a, b) => a.s[1] - b.s[1]);
  const fight = fightOn(t), mine = people.filter(q => q.e.side === 'cell' && !isDown(q.e, t));
  for (const q of mine) { const sel = st.sel.has(q.e.id); // as built: HostViews.CellRings: thin 0.42 m ring, thick 0.52 m when selected; in a fight, the selected agent only
    if (sel) { ground(q.s, 0.52, DEEP, 9.5, 0.6); ground(q.s, 0.52, TURQ, 7); }
    else if (OPT.m === 'built') { if (!fight) ground(q.s, 0.42, TURQ, 2.4, 0.7); }
    else if (OPT.m === 'chev') ground(q.s, 0.42, TURQ, 2.4, 0.8);
    else { ground(q.s, 0.46, DEEP, 6, 0.6); ground(q.s, 0.46, TURQ, 3.6); } }
  if (!st.hide) drawCones(t, o);
  if (!st.hide) drawLines(t, o);
  if (!st.hide && OPT.w === 'ring') drawRingClocks(t, o);
  // the incoming marks on the ground, under the figures
  if (!st.hide && OPT.i !== 'off') for (const q of people) if (q.e.side === 'cell') {
    for (const [by, v] of tNow) if (v.to === q.e.id && E.get(by).side !== 'cell') { const bp = at(E.get(by), t); if (!bp) continue; const bs = proj(bp[0], bp[1], o), ang = Math.atan2((bs[1] - q.s[1]) / K, bs[0] - q.s[0]), age = (t - v.last) / HZ, hot = age < 0.45, R = 0.56 * S;
      const P = (r, an) => [q.s[0] + Math.cos(an) * r, q.s[1] + Math.sin(an) * r * K], tip = [P(R + (hot ? 34 : 28), ang), P(R, ang - 0.34), P(R, ang + 0.34)];
      g.save(); g.lineJoin = 'round'; g.fillStyle = RED; g.strokeStyle = DEEP; g.lineWidth = hot ? 2.2 : 1.6; g.beginPath(); g.moveTo(tip[0][0], tip[0][1]); g.lineTo(tip[1][0], tip[1][1]); g.lineTo(tip[2][0], tip[2][1]); g.closePath(); g.stroke(); g.fill(); g.restore();
      if (OPT.i === 'mark' && hot) { const k = 1 - age / 0.45; bracket(bs, t, null, RED, 1.6 + k); }
    }
  }
  // figures
  for (const q of people) { const v = tNow.get(q.e.id); let face = null, firing = false; if (v) { const b = E.get(v.to), bp = b && at(b, t); if (bp) { const bs = proj(bp[0], bp[1], o); face = [bs[0] - q.s[0], bs[1] - q.s[1]]; firing = true; } } else { const b = at(q.e, Math.max(T0, t - 2)); if (b) { const bs = proj(b[0], b[1], o); face = [q.s[0] - bs[0], q.s[1] - bs[1]]; } } const r = bodyReact(q.e, t, o); figure(q.e, r.dx || r.dy ? [q.s[0] + r.dx, q.s[1] + r.dy] : q.s, t, face, firing, r.white); }
  for (const q of mine) { const sel = st.sel.has(q.e.id); if (!sel && OPT.m !== 'chev') continue; const y = q.s[1] - 92, k = sel ? 1 : 0.62; for (const [c, m] of [[DEEP, 2], [TURQ, 0]]) { g.fillStyle = c; g.beginPath(); g.moveTo(q.s[0] - (11 * k + m), y - 8 * k - m * 0.6); g.lineTo(q.s[0] + 11 * k + m, y - 8 * k - m * 0.6); g.lineTo(q.s[0], y + 7 * k + m); g.closePath(); g.fill(); } }
  // the shots. As built: HudToolkit.Fire.cs ShotFx. Stronger: a hot flash that holds on pale paving, a bigger spark, a miss that never outlasts a hit.
  const strong = OPT.s !== 'built', FLASH = strong ? 0.16 : 0.09, SPARK = strong ? 0.3 : 0.2, MISS = strong ? 0.22 : 0.45;
  for (const s of D.shots) { const age = (t - s[0]) / HZ; if (age < 0 || age > 0.5) continue; const by = E.get(s[1]); const a = proj(s[3] / 100, s[4] / 100, o, 1.1), b = proj(s[5] / 100, s[6] / 100, o, 1.1), d = [b[0] - a[0], b[1] - a[1]], n = Math.hypot(d[0], d[1]) || 1, u = [d[0] / n, d[1] / n];
    const cell = by?.side === 'cell';
    if (!st.hide && OPT.c === 'thread' && age < 0.32) { const k = 1 - age / 0.32, end = s[7] ? b : [b[0] + u[0] * 34 - u[1] * 12, b[1] + u[1] * 34 + u[0] * 12]; g.globalAlpha = k; line2([a, end], cell ? 1.8 : 1.5, cell ? CREAM : RED, DEEP, 0.5); g.globalAlpha = 1; }
    if (age < FLASH) { if (!strong) star(a, u, cell ? 9 : 7); else { const k = 1 - age / FLASH, m = [a[0] + u[0] * 20, a[1] + u[1] * 20], r = (cell ? 24 : 21) * (0.65 + 0.35 * k); for (const [c, rr] of [['rgba(11,22,21,.7)', r + 3], [HOT, r], [HOTCORE, r * 0.55]]) { const nn = [-u[1], u[0]]; g.fillStyle = c; g.beginPath(); g.moveTo(m[0] + u[0] * rr * 1.9, m[1] + u[1] * rr * 1.9); g.lineTo(m[0] + nn[0] * rr * 0.35, m[1] + nn[1] * rr * 0.35); g.lineTo(m[0] + nn[0] * rr * 0.85, m[1] + nn[1] * rr * 0.85); g.lineTo(m[0] - u[0] * rr * 0.35 + nn[0] * rr * 0.2, m[1] - u[1] * rr * 0.35 + nn[1] * rr * 0.2); g.lineTo(m[0] - u[0] * rr * 0.35 - nn[0] * rr * 0.2, m[1] - u[1] * rr * 0.35 - nn[1] * rr * 0.2); g.lineTo(m[0] - nn[0] * rr * 0.85, m[1] - nn[1] * rr * 0.85); g.lineTo(m[0] - nn[0] * rr * 0.35, m[1] - nn[1] * rr * 0.35); g.closePath(); g.fill(); } } }
    if (s[7] && age < SPARK) { const k = age / SPARK; g.lineCap = 'round'; const r0 = strong ? 6 + 12 * k : 3 + 6 * k, r1 = strong ? 18 + 18 * k : 9 + 10 * k; for (let i = 0; i < 6; i++) { const an = (i * 60 + 15) * Math.PI / 180; for (const [c, w] of [['rgba(23,48,47,' + 0.5 * (1 - k) + ')', strong ? 6 : 4], [strong ? `rgba(255,255,255,${1 - k})` : 'rgba(255,242,190,' + (1 - k) + ')', strong ? 3.6 : 2.2]]) { g.strokeStyle = c; g.lineWidth = w; g.beginPath(); g.moveTo(b[0] + Math.cos(an) * r0, b[1] + Math.sin(an) * r0); g.lineTo(b[0] + Math.cos(an) * r1, b[1] + Math.sin(an) * r1); g.stroke(); } } }
    if (!s[7] && age < MISS) { const k = age / MISS; if (!strong) { const tail = [a[0] + d[0] * k * 0.8, a[1] + d[1] * k * 0.8]; g.lineCap = 'round'; g.strokeStyle = `rgba(23,48,47,${0.5 * (1 - k)})`; g.lineWidth = 3.6; g.beginPath(); g.moveTo(tail[0], tail[1]); g.lineTo(b[0], b[1]); g.stroke(); g.strokeStyle = `rgba(255,247,219,${0.95 * (1 - k)})`; g.lineWidth = 1.6; g.beginPath(); g.moveTo(tail[0], tail[1]); g.lineTo(b[0], b[1]); g.stroke(); } const q = [b[0] + u[0] * 18, b[1] + u[1] * 18 + 34]; g.fillStyle = `rgba(150,140,120,${0.6 * (1 - k)})`; for (let i = 0; i < 3; i++) { g.beginPath(); g.arc(q[0] + (i - 1) * (3 + 6 * k), q[1] - 2 * k - (i === 1 ? 2 : 0), 3.5 + 8 * k, 0, 7); g.fill(); } }
  }
  // the target cue
  if (!st.hide) drawTargets(t, o, tNow);
  // the focus as built: the lock disc over a target the player named
  if (OPT.c !== 'disc') for (const [by, v] of tNow) if (v.focus && E.get(by).side === 'cell') { const b = E.get(v.to), bp = at(b, t); if (!bp) continue; const s = proj(bp[0], bp[1], o, 2.3); if (!onScreen([s[0], s[1] + 98])) continue; g.drawImage(img('disc_dark_24'), s[0] - 17, s[1] - 17, 34, 34); icon('crosshair', s[0], s[1], 16, CREAM); break; }
  if (!st.hide) { drawWindows(t, o); drawEdges(t, o, tNow); drawPrice(); }
  drawRoster(t, o);
  drawClock(t, o);
  if (!st.hide) hoverTip(t, o);
  if (st.debug) $('readout').textContent = readout(t, o, tNow);
}
function readout(t, o, tn) { const c = []; for (const [by, v] of tn) if (E.get(by).side === 'cell') c.push(`${by} > ${v.to}`); const h = []; for (const [by, v] of tn) if (E.get(by).side !== 'cell') h.push(`${by} > ${v.to}`); return `tick ${Math.floor(t)}  ·  cell: ${c.join(', ') || 'none'}  ·  at the cell: ${h.join(', ') || 'none'}`; }
function drawTargets(t, o, tn) {
  const mode = OPT.c; if (mode === 'today' || mode === 'thread') return;
  const by = new Map(); for (const [s, v] of tn) { if (E.get(s).side !== 'cell') continue; (by.get(v.to) || by.set(v.to, []).get(v.to)).push({ s, v }); }
  for (const [id, ss] of by) {
    const e = E.get(id), p = at(e, t); if (!p) continue; const s = proj(p[0], p[1], o); if (!onScreen(s)) continue;
    const sel = ss.some(x => st.sel.has(x.s)), focus = ss.some(x => x.v.focus);
    if (mode === 'selected' && !sel) { ground(s, 0.62, CREAM, 1.6, 0.7); continue; }
    const last = D.shots.filter(q => q[2] === id && q[0] <= t && E.get(q[1])?.side === 'cell').pop(), ls = last ? { t: last[0], hit: !!last[7] } : null;
    let top;
    if (mode === 'disc') { // the focus disc as built, on every target: a target you named stays full, one they picked themselves is lighter. A hit shrinks it, a miss twitches it.
      const age = ls ? (t - ls.t) / HZ : 9, k = age < 0.3 ? 1 - age / 0.3 : 0, dy = ls && !ls.hit ? Math.sin(age * 60) * 4 * k : 0, r = (focus ? 17 : 15) * (ls && ls.hit ? 1 - 0.22 * k : 1 + 0.12 * k), y = s[1] - 100;
      if (!focus) { g.globalAlpha = 0.82; } g.drawImage(img('disc_dark_24'), s[0] + dy - r, y - r, 2 * r, 2 * r); icon('crosshair', s[0] + dy, y, r * 0.95, ls && ls.hit && k > 0 ? '#ffffff' : CREAM); g.globalAlpha = 1; top = y - r - 14;
    } else { bracket(s, t, ls, CREAM, sel ? 2.8 : 2); top = s[1] - 42 - 54 - 16 - (focus ? 22 : 0); }
    const n = ss.length; ss.slice(0, 4).forEach((x, i) => { cameo(`cameo_${x.s}`, s[0] - (n - 1) * 14 + i * 28, top, 12, st.sel.has(x.s) ? TURQ : CHROME); });
    if (mode === 'selected') for (const x of ss) if (st.sel.has(x.s)) { const a = at(E.get(x.s), t); if (a) { const q = proj(a[0], a[1], o, 1.05); line2([q, [s[0], s[1] - 50]], 1.6, CREAM, DEEP, 0.5, [2, 7]); } }
  }
}
function drawLines(t, o) {
  // Reroute's lines. Today: while the reroute is in play. Hover: only while looking. One: only one of the two.
  const live = t < 3206 && windowsAt(t).some(w => w.kind === 'reroute'), hover = st.way || hoverThing(t, o) === 'patrol', mode = OPT.l, fight = fightOn(t);
  let sweep = false, route = false;
  if (mode === 'play') { sweep = live; route = live; }
  else if (!fight) { if (mode === 'one') { route = hover && (live || st.way); sweep = hover && !route; } }
  const A_ = proj(61.25, -7.25, o), B_ = proj(76.25, -7.25, o), P_ = E.get('patrol');
  if (sweep) { line2([A_, B_], 2, CREAM); for (const q of [A_, B_]) line2([[q[0] - 16, q[1] + 6], [q[0] + 16, q[1] - 6]], 2, CREAM); }
  if (route) {
    const pts = []; for (let k = Math.floor(t); k <= Math.min(T1, 3206); k += 4) { const p = at(P_, k); if (p) pts.push(proj(p[0], p[1], o)); }
    if (t < 3206) pts.push(proj(71.75, -7.4, o));
    if (pts.length > 1) { line2(pts, 2, CREAM, DEEP, 0.55, [11, 13]); let acc = 0; for (let i = 1; i < pts.length; i++) { const dx = pts[i][0] - pts[i - 1][0], dy = pts[i][1] - pts[i - 1][1], L = Math.hypot(dx, dy); acc += L; if (acc > 150) { acc = 0; const u = [dx / L, dy / L], m = pts[i]; g.fillStyle = CREAM; g.strokeStyle = 'rgba(11,22,21,.55)'; g.lineWidth = 1.5; g.beginPath(); g.moveTo(m[0] + u[0] * 11, m[1] + u[1] * 11); g.lineTo(m[0] - u[0] * 6 - u[1] * 8, m[1] - u[1] * 6 + u[0] * 8); g.lineTo(m[0] - u[0] * 6 + u[1] * 8, m[1] - u[1] * 6 - u[0] * 8); g.closePath(); g.stroke(); g.fill(); } } }
  }
}
function winParts(w, t, today) {
  const left = Math.max(0, Math.ceil((w.until - t) / HZ)), total = Math.max(1, Math.ceil((w.until - Math.max(w.from, today ? T0 : w.from)) / HZ)), low = left <= 10;
  return { left, total, low, txt: today ? (total >= 60 ? `${Math.floor(left / 60)}:${String(left % 60).padStart(2, '0')}` : String(left)) : fmt(left), frac: left / total };
}
function drawWindows(t, o) {
  const mode = OPT.w, wins = windowsAt(t);
  for (const w of wins) { const d = WIN[w.kind]; if (!d) continue; const a = winAnchor(w, t, o, mode === 'today'); if (!a) continue; const pr = winParts(w, t, mode === 'today'), s = a.s, say = `${d.says} ${pr.txt}${pr.left < 60 ? ' s' : ''}`;
    if (s[0] < -40 || s[0] > W + 40 || s[1] < -10 || s[1] > H + 60) continue;
    if (mode === 'today') { plate(s[0], s[1] - 12, { icon: 'eye_shut', txt: pr.txt, frac: pr.frac, low: pr.low }); continue; }
    st.hot.push({ tag: 'win', x: a.g[0] - 36, y: a.g[1] - 90, w: 72, h: 106, say, tipAt: [a.g[0], a.g[1] - 96], patrol: d.key === 'patrol' });
    if (st.alt) tip(pr.txt, a.g[0], a.g[1] + 34);
  }
}
function drawPrice() { // what the eye does on an order's price: the sample stands in for the ring's line
  if (!st.way) return; const tx = 'Reroute · 30 s'; g.font = '700 15.5px Jost, sans-serif'; const today = OPT.w === 'today', tw = g.measureText(tx).width, w = tw + 30 + (today ? 24 : 0), x = W / 2 - w / 2, y = H - 96; nine('plate_dark', x, y, w, 38); text(tx, x + 15, y + 19, 15.5); if (today) icon('eye_shut', x + 15 + tw + 15, y + 19, 16, CREAM); }
function drawRingClocks(t, o) {
  if (!st.watched) return;
  for (const w of windowsAt(t)) { const d = WIN[w.kind]; if (!d || !isBlind(w.kind)) continue; const a = winAnchor(w, t, o); if (!a) continue; const pr = winParts(w, t, false), s = a.g; if (s[0] < -40 || s[0] > W + 40 || s[1] < -10 || s[1] > H + 60) continue; const R = 0.72 * S;
    g.lineWidth = 2.4; g.strokeStyle = 'rgba(11,22,21,.35)'; g.beginPath(); g.ellipse(s[0], s[1], R, R * K, 0, 0, 7); g.stroke();
    g.lineCap = 'butt'; for (const [c, wd] of [['rgba(11,22,21,.6)', 6.5], [pr.low ? AMBER : TURQ, 3.6]]) { g.strokeStyle = c; g.lineWidth = wd; g.beginPath(); g.ellipse(s[0], s[1], R, R * K, 0, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * pr.frac); g.stroke(); }
    }
}
function hoverThing(t, o) { const m = st.mouse; if (!m) return ''; for (const h of st.hot) if (h.patrol && m[0] >= h.x && m[0] <= h.x + h.w && m[1] >= h.y && m[1] <= h.y + h.h) return 'patrol'; const p = at(E.get('patrol'), t); if (!p) return ''; const s = proj(p[0], p[1], o); return Math.abs(m[0] - s[0]) < 34 && m[1] > s[1] - 74 && m[1] < s[1] + 14 ? 'patrol' : ''; }
function hoverTip(t, o) { const m = st.mouse; if (!m) return; let done = false;
  for (const h of st.hot) if (h.say && m[0] >= h.x && m[0] <= h.x + h.w && m[1] >= h.y && m[1] <= h.y + h.h) { const y = h.tipAt ? h.tipAt[1] + (h.left ? 13 : 0) : h.y - 6; let x = h.tipAt ? h.tipAt[0] : h.x + h.w / 2; g.font = '700 13.5px Jost'; const w = g.measureText(h.say).width + 22; if (h.left) x -= w / 2; x = Math.max(w / 2 + 180, Math.min(W - w / 2 - 8, x)); tip(h.say, x, Math.max(30, y)); done = true; break; }
  }
function drawRoster(t, o) {
  // the left column, simply: portrait, health, selected; a click selects
  g.fillStyle = 'rgba(12,52,52,.92)'; g.strokeStyle = CHROME; g.lineWidth = 2; g.beginPath(); g.roundRect(16, 18, 92, 390, 16); g.fill(); g.stroke();
  CELL.forEach((c, i) => { const y = 70 + i * 96, e = E.get(c), hp = hpOf(e, t), sel = st.sel.has(c); cameo(`cameo_${c}`, 62, y, 34, sel ? TURQ : CHROME); if (sel) { g.strokeStyle = TURQ; g.lineWidth = 3; g.beginPath(); g.arc(62, y, 38, 0, 7); g.stroke(); } for (let k = 0; k < e.hpMax; k++) { g.fillStyle = k < hp ? CREAM : 'rgba(247,244,236,.22)'; g.beginPath(); g.arc(62 - (e.hpMax - 1) * 4 + k * 8, y + 42, 2.8, 0, 7); g.fill(); } if (st.fireFlash && false) return; st.hot.push({ tag: 'sel', x: 22, y: y - 44, w: 80, h: 88, sel: c }); });
}
function drawClock(t, o) { const k = key(D.windows, t), m = k && k[1].match(/seal:district:(\d+)/); if (!m) return; const left = Math.max(0, Math.ceil((+m[1] - t) / HZ)); nine('plate_dark', W - 124, 18, 104, 38); icon('lock', W - 104, 37, 15, CREAM); text(`${Math.floor(left / 60)}:${String(left % 60).padStart(2, '0')}`, W - 90, 37, 16); }

// ---- input
const toStage = e => { const r = cv.getBoundingClientRect(); return [(e.clientX - r.left) * W / r.width, (e.clientY - r.top) * H / r.height]; };
let drag = null;
cv.addEventListener('pointermove', e => { const m = toStage(e); st.mouse = m; if (drag) { const c = st.cam || [camOf(st.t)[1], camOf(st.t)[2]]; st.cam = [c[0] + m[0] - drag[0], c[1] + m[1] - drag[1]]; st.camTo = null; drag = m; $('follow').hidden = false; } const hit = st.hot.find(h => (h.go || h.sel) && m[0] >= h.x && m[0] <= h.x + h.w && m[1] >= h.y && m[1] <= h.y + h.h); cv.style.cursor = hit ? 'pointer' : drag ? 'grabbing' : 'default'; });
cv.addEventListener('pointerleave', () => st.mouse = null);
cv.addEventListener('pointerdown', e => { const m = toStage(e), hit = st.hot.find(h => m[0] >= h.x && m[0] <= h.x + h.w && m[1] >= h.y && m[1] <= h.y + h.h && (h.go || h.sel)); if (hit && hit.sel) { if (e.shiftKey) { st.sel.has(hit.sel) ? st.sel.delete(hit.sel) : st.sel.add(hit.sel); } else { st.sel = new Set([hit.sel]); } return; } if (hit && hit.go) { const r = camOf(st.t), base = st.cam || [r[1], r[2]], s0 = proj(hit.go[0], hit.go[1], [0, 0]); st.cam = base; st.camTo = [W / 2 + 40 - s0[0], H / 2 - s0[1]]; $('follow').hidden = false; return; } drag = m; cv.setPointerCapture(e.pointerId); });
cv.addEventListener('pointerup', () => drag = null);
addEventListener('keydown', e => { if (e.code === 'Space') { st.play = !st.play; syncPlay(); e.preventDefault(); } else if (e.key === 'ArrowLeft') st.t = Math.max(T0, st.t - 10); else if (e.key === 'ArrowRight') st.t = Math.min(T1, st.t + 10); else if ('1234'.includes(e.key)) usePreset(Object.keys(PRESETS)[+e.key - 1]); else if (e.key === 'h' || e.key === 'H') { st.hide = !st.hide; $('hide').checked = st.hide; } });
const syncPlay = () => { $('play').textContent = st.play ? 'Pause' : 'Play'; };
$('play').onclick = () => { st.play = !st.play; syncPlay(); };
$('scrub').min = T0; $('scrub').max = T1; $('scrub').step = 0.1; $('scrub').oninput = e => { st.t = +e.target.value; st.from = st.t; st.play = false; syncPlay(); };
$('hide').onchange = e => st.hide = e.target.checked;
$('watched').onchange = e => st.watched = e.target.checked;
addEventListener('keydown', e => { if (e.key === 'Alt') { st.alt = true; e.preventDefault(); } }); addEventListener('keyup', e => { if (e.key === 'Alt') st.alt = false; }); addEventListener('blur', () => st.alt = false);
$('way').onchange = e => st.way = e.target.checked;
$('follow').onclick = () => { st.cam = null; st.camTo = null; $('follow').hidden = true; };
for (const b of document.querySelectorAll('[data-speed]')) b.onclick = () => { st.speed = +b.dataset.speed; for (const q of document.querySelectorAll('[data-speed]')) q.classList.toggle('on', q === b); };
for (const b of document.querySelectorAll('[data-jump]')) b.onclick = () => { jump(+b.dataset.jump, b.dataset.frame !== undefined, +b.dataset.back || null, b.dataset.on); if (b.dataset.back) { st.play = true; syncPlay(); } };
function jump(t, frameIt, back, on) { st.showFan = on || null; st.backAt = back || null; st.t = t; st.from = t; st.cam = null; st.camTo = null; $('follow').hidden = true; if (on) { const p = at(E.get(on), t), s = proj(p[0], p[1], [0, 0]); st.cam = [1480 - s[0], 400 - s[1]]; $('follow').hidden = false; } else if (frameIt) { const pts = [...E.values()].filter(e => !isDown(e, t) && (e.side === 'cell' || e.side === 'rival' || e.side === 'police')).map(e => at(e, t)).filter(Boolean).map(p => proj(p[0], p[1], [0, 0])); const cx = pts.reduce((a, p) => a + p[0], 0) / pts.length, cy = pts.reduce((a, p) => a + p[1], 0) / pts.length; st.cam = [W / 2 + 50 - cx, H / 2 + 30 - cy]; $('follow').hidden = false; } }
function setOpt(k, v) { OPT[k] = v; for (const r of document.querySelectorAll(`[data-k="${k}"]`)) r.classList.toggle('on', r.dataset.v === v); for (const n of document.querySelectorAll(`[data-show="${k}"]`)) n.hidden = n.dataset.when !== v; for (const b of document.querySelectorAll('[data-preset]')) b.classList.toggle('on', PRESETS[b.dataset.preset].split(',').every(kv => { const [a, c] = kv.split('='); return OPT[a] === c; })); }
for (const r of document.querySelectorAll('[data-k]')) r.onclick = () => setOpt(r.dataset.k, r.dataset.v);
const usePreset = n => { for (const kv of PRESETS[n].split(',')) { const [k, v] = kv.split('='); setOpt(k, v); } };
for (const b of document.querySelectorAll('[data-preset]')) b.onclick = () => usePreset(b.dataset.preset);
// the shots on the scrub bar
{ const bar = $('marks'), L = T1 - T0; for (const s of D.shots) { const m = document.createElement('i'); m.style.left = ((s[0] - T0) / L * 100) + '%'; m.className = E.get(s[1])?.side === 'cell' ? 'c' : 'h'; bar.appendChild(m); } }
// a page state in the address, so a setting can be shared (and shot)
function fromHash() { const h = new URLSearchParams(location.hash.slice(1)); st.debug = h.has('debug'); $('readout').hidden = !st.debug; if (h.get('p')) usePreset(h.get('p')); for (const k of Object.keys(OPT)) setOpt(k, h.get(k) || OPT[k]); if (h.get('t')) { st.t = +h.get('t'); st.from = st.t; st.play = false; } if (h.get('sel')) st.sel = new Set(h.get('sel').split(/[+ ,]/)); if (h.get('watched') === '0') { st.watched = false; $('watched').checked = false; } if (h.get('back')) st.backAt = +h.get('back'); if (h.get('alt')) st.alt = true; if (h.get('way')) { st.way = true; $('way').checked = true; } if (h.get('mx')) st.mouse = [+h.get('mx'), +h.get('my')]; syncPlay(); }
// ---- start
const load = (n, src) => new Promise(res => { const m = new Image(); m.onload = () => { A[n] = m; res(); }; m.onerror = () => res(); m.src = src; });
Promise.all([...Object.entries(D.img).map(([n, s]) => load(n, s)), document.fonts.load('700 14px Jost')]).then(() => { fromHash(); requestAnimationFrame(frame); });
window.__cue = { st, OPT };
