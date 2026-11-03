// Fingers on the canvas. One finger: a tap is a click, a short press and hold is the left hold (the pill fills, lift to go), a drag is a pan
// (third person: a drag on the left half is a walking stick, on the right half a look). Two fingers: pinch zooms, a twist is Q or E, the middle pans.
// A finger that drifts while it holds keeps holding (80 px cancels it, as the mouse does). The press and release logic itself is app.js's, shared with the mouse.
import { G } from './state.js';

const TH = 10, HOLD_MS = 130, TWIST = 38 * Math.PI / 180, R = 56;

export function initTouch(canvas, api) {
  const pts = new Map(); let one = null, two = null, dead = false, held = new Set();
  const stick = document.getElementById('stick'), knob = stick.firstElementChild;
  const wrap = (a) => { while (a > Math.PI) a -= 2 * Math.PI; while (a < -Math.PI) a += 2 * Math.PI; return a; };
  const setKeys = (want) => { for (const k of ['KeyW', 'KeyA', 'KeyS', 'KeyD', 'ShiftLeft']) { if (want.has(k)) { G.keys.add(k); held.add(k); } else if (held.has(k)) { G.keys.delete(k); held.delete(k); } } };
  const stopStick = () => { setKeys(new Set()); stick.style.display = 'none'; };
  const drive = (dx, dy) => {
    const raw = Math.hypot(dx, dy), k = raw > R ? R / raw : 1, x = dx * k, y = dy * k, t = 0.3 * R, want = new Set();
    if (y < -t) want.add('KeyW'); if (y > t) want.add('KeyS'); if (x > t) want.add('KeyD'); if (x < -t) want.add('KeyA'); if (raw > R * 1.25) want.add('ShiftLeft'); // pushed well past the edge runs
    setKeys(want); knob.style.transform = `translate(${x}px,${y}px)`;
  };
  const two2 = () => { const [a, b] = [...pts.values()]; return { d: Math.hypot(b.x - a.x, b.y - a.y) || 1, a: Math.atan2(b.y - a.y, b.x - a.x), mx: (a.x + b.x) / 2, my: (a.y + b.y) / 2 }; };
  const endOne = (act) => { if (!one) return; clearTimeout(one.timer); const o = one; one = null; if (o.kind === 'press') { if (act) api.release(o.x, o.y); else api.drop(); } else if (o.kind === 'stick') stopStick(); };

  canvas.addEventListener('pointerdown', (e) => {
    if (e.pointerType !== 'touch') return; e.preventDefault();
    pts.set(e.pointerId, { x: e.clientX, y: e.clientY });
    if (pts.size === 1) {
      dead = false; one = { id: e.pointerId, x0: e.clientX, y0: e.clientY, x: e.clientX, y: e.clientY, kind: 'press' };
      api.press(e.clientX, e.clientY); one.timer = setTimeout(() => { if (one && one.kind === 'press') api.hold(); }, HOLD_MS);
    } else if (pts.size === 2 && !dead) { endOne(false); stopStick(); two = two2(); two.acc = 0; }
    else dead = true;
  });
  canvas.addEventListener('pointermove', (e) => {
    if (e.pointerType !== 'touch' || !pts.has(e.pointerId)) return;
    const p = pts.get(e.pointerId); p.x = e.clientX; p.y = e.clientY;
    if (two && pts.size === 2) {
      const n = two2(), r = n.d / two.d; if (Math.abs(r - 1) > 0.004) { api.pinch(r); two.d = n.d; }
      if (api.view() === 'cmd') {
        api.pan(n.mx - two.mx, n.my - two.my);
        two.acc += wrap(n.a - two.a); if (Math.abs(two.acc) > TWIST) { api.turn(two.acc > 0 ? 1 : -1); two.acc = 0; } // clockwise twist = E, the scene turns with the fingers
      }
      two.a = n.a; two.mx = n.mx; two.my = n.my; return;
    }
    if (!one || e.pointerId !== one.id) return;
    const dx = e.clientX - one.x, dy = e.clientY - one.y; one.x = e.clientX; one.y = e.clientY;
    if (one.kind === 'press') {
      if (api.holding()) { api.move(e.clientX, e.clientY); return; } // a running hold: the finger may drift
      if (Math.hypot(e.clientX - one.x0, e.clientY - one.y0) <= TH) return;
      clearTimeout(one.timer); api.drop();
      const [W] = api.size(); one.kind = api.view() === 'cmd' ? 'pan' : one.x0 < W * 0.5 ? 'stick' : 'orbit';
      if (one.kind === 'stick') { Object.assign(stick.style, { display: 'block', left: one.x0 + 'px', top: one.y0 + 'px' }); drive(e.clientX - one.x0, e.clientY - one.y0); return; }
      if (one.kind === 'pan') api.pan(e.clientX - one.x0, e.clientY - one.y0); else api.orbit(e.clientX - one.x0, e.clientY - one.y0);
      return;
    }
    if (one.kind === 'pan') api.pan(dx, dy); else if (one.kind === 'orbit') api.orbit(dx, dy); else drive(e.clientX - one.x0, e.clientY - one.y0);
  });
  const up = (cancel) => (e) => {
    if (e.pointerType !== 'touch' || !pts.has(e.pointerId)) return;
    const mine = one && e.pointerId === one.id; pts.delete(e.pointerId);
    if (mine) { one.x = e.clientX; one.y = e.clientY; endOne(!cancel && !dead); }
    if (two && pts.size < 2) { two = null; dead = true; }
    if (!pts.size) { dead = false; two = null; one = null; stopStick(); }
  };
  canvas.addEventListener('pointerup', up(false)); canvas.addEventListener('pointercancel', up(true));

  // third person: the Fire button is a held right press at the middle of the screen (the nearest hostile ahead, or what is under the middle)
  const fb = document.getElementById('tpfire'); let fid = null;
  fb.addEventListener('pointerdown', (e) => { e.preventDefault(); fid = e.pointerId; try { fb.setPointerCapture(fid); } catch {} fb.classList.add('on'); api.fireDown(); });
  const fup = (e) => { if (e.pointerId !== fid) return; fid = null; fb.classList.remove('on'); api.fireUp(); };
  fb.addEventListener('pointerup', fup); fb.addEventListener('pointercancel', fup);
}
