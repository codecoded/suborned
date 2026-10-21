// Two views: the commander camera (pitch 38, quarter turns) and third person.
// Third person has two follow models: the 0.2.34 snap and the brief 040 leash.
import * as THREE from './vendor/three.module.min.js';
import { G, say } from './state.js';

const rad = (d) => d * Math.PI / 180, deg = (r) => r * 180 / Math.PI;
const angDiff = (a, b) => { let d = (b - a) % (Math.PI * 2); if (d > Math.PI) d -= Math.PI * 2; if (d < -Math.PI) d += Math.PI * 2; return d; };
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
// Unity's SmoothDamp on a number: critically damped, with a speed cap.
function smoothDamp(cur, target, vel, smooth, maxSpeed, dt) {
  smooth = Math.max(1e-4, smooth); const o = 2 / smooth, x = o * dt, e = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
  const maxChange = maxSpeed * smooth; const change = clamp(cur - target, -maxChange, maxChange);
  const t = cur - change, temp = (vel.v + o * change) * dt; vel.v = (vel.v - o * temp) * e;
  let out = t + (change + temp) * e;
  if ((target - cur > 0) === (out > target)) { out = target; vel.v = (out - target) / dt; }
  return out;
}

// brief 040 recommended defaults, in one place
export const LEASH = { gain: 1.0, arm: 5.5, steadyS: 1.0, steadyDeg: 20, idleS: 2.0, settleS: 1.0, maxDegPerS: 45, minSpeed: 0.5 };
const SNAP = { smoothS: 0.15, lookReturnS: 1.0 };

export class Rig {
  constructor(camera) {
    this.cam = camera; this.mode = 'cmd'; this.blend = 1; this.from = null;
    this.focus = new THREE.Vector3(2, 0, 4.5); this.yaw = rad(210); this.yawTarget = this.yaw; this.yawVel = { v: 0 }; this.dist = 38; this.distGoal = 38; this.focusGoal = null;
    this.f = 0; this.fVel = { v: 0 }; this.arm = LEASH.arm; this.pivot = new THREE.Vector3(); this.pitch = rad(15);
    this.idle = 99; this.steady = 0; this.lastH = 0; this.side = 1; this.frame = null; this.lastTgt = new THREE.Vector3();
  }
  turn(dir) { if (this.mode === 'cmd') this.yawTarget += dir * Math.PI / 2; }
  zoom(dy) { if (this.mode === 'cmd') this.distGoal = clamp(this.distGoal * (1 + dy * 0.001), 30, 46); else this.arm = clamp(this.arm * (1 + dy * 0.001), 3, 10); }
  // The commander view moves by hand: a middle-drag, WASD or the edge of the screen. Any of them ends a follow or a centring.
  pan(dx, dy) {
    if (this.mode !== 'cmd') return;
    const mpp = 2 * Math.tan(rad(15)) * this.dist / innerHeight, sy = Math.sin(rad(38));
    this.focus.x += Math.cos(this.yaw) * -dx * mpp - Math.sin(this.yaw) * dy * mpp / sy; this.focus.z += -Math.sin(this.yaw) * -dx * mpp - Math.cos(this.yaw) * dy * mpp / sy;
    this.handMoved();
  }
  handMoved() { this.focusGoal = null; if (G.camFollow) { G.camFollow = false; say('Follow off'); } }
  centreOn(p) { this.focusGoal = new THREE.Vector3(p.x, 0, p.z); }
  orbit(dx, dy) { if (this.mode !== 'tp') return; this.f -= dx * 0.005; this.pitch = clamp(this.pitch + dy * 0.003, rad(4), rad(55)); this.idle = 0; this.frame = null; this.fVel.v = 0; }
  setMode(m, agent) {
    if (m === this.mode) return;
    this.from = { pos: this.cam.position.clone(), tgt: this.lastTgt.clone(), fov: this.cam.fov }; this.blend = 0;
    if (m === 'tp') { this.f = this.yaw + Math.PI; this.fVel.v = 0; this.pivot.copy(agent.g.position); this.steady = 0; this.idle = 99; this.frame = null; this.pitch = rad(15); }
    else this.focus.copy(agent.g.position);
    this.mode = m;
  }
  // Another agent is taken over while in third person: fly to it.
  refocus(agent) { this.from = { pos: this.cam.position.clone(), tgt: this.lastTgt.clone(), fov: this.cam.fov }; this.blend = 0; this.pivot.copy(agent.g.position); }
  // The yaw that W is relative to. The leash latches it while a key is held, so the path stays straight.
  moveYaw(held) {
    if (held && this.frame === null) this.frame = this.f; if (!held) this.frame = null;
    return this.frame ?? this.f;
  }
  update(dt, agent, vel) {
    const k = G.keys, cam = this.cam;
    let pos, tgt, fov;
    if (this.mode === 'cmd') {
      this.yaw = smoothDamp(this.yaw, this.yawTarget, this.yawVel, 0.12, 20, dt);
      this.dist += (this.distGoal - this.dist) * (1 - Math.exp(-dt / 0.1)); // scroll zoom eases
      const s = 14 * dt * this.dist / 38; let ax = k.has('KeyD') - k.has('KeyA'), az = k.has('KeyW') - k.has('KeyS');
      if (!ax && !az && G.edgePan === 'on' && G.ptr.over) { const m = 24; ax = G.ptr.x > innerWidth - m ? 1 : G.ptr.x < m ? -1 : 0; az = G.ptr.y < m ? 1 : G.ptr.y > innerHeight - m ? -1 : 0; }
      if (ax || az) this.handMoved();
      const goal = G.camFollow ? agent.g.position : this.focusGoal; // a follow or a centring glides there
      if (goal) this.focus.lerp(new THREE.Vector3(goal.x, 0, goal.z), 1 - Math.exp(-dt / 0.15));
      if (this.focusGoal && !G.camFollow && this.focus.distanceTo(this.focusGoal) < 0.05) this.focusGoal = null;
      this.focus.x += (-Math.sin(this.yaw) * az + Math.cos(this.yaw) * ax) * s; this.focus.z += (-Math.cos(this.yaw) * az - Math.sin(this.yaw) * ax) * s;
      this.focus.x = clamp(this.focus.x, -22, 26); this.focus.z = clamp(this.focus.z, -8, 14);
      const p = rad(38); tgt = this.focus.clone();
      pos = tgt.clone().add(new THREE.Vector3(Math.sin(this.yaw) * Math.cos(p), Math.sin(p), Math.cos(this.yaw) * Math.cos(p)).multiplyScalar(this.dist)); fov = 30;
    } else {
      this.idle += dt;
      const speed = Math.hypot(vel.x, vel.z), h = Math.atan2(vel.x, vel.z);
      const right = new THREE.Vector2(-Math.cos(this.f), Math.sin(this.f)), side = vel.x * right.x + vel.z * right.y;
      if (speed > LEASH.minSpeed) {
        if (Math.abs(angDiff(this.lastH, h)) > rad(LEASH.steadyDeg)) { this.steady = 0; this.lastH = h; } else this.steady += dt;
        if (Math.abs(side) > 0.2) this.side = Math.sign(side);
      } else this.steady = 0;
      if (G.camMode === 'leash') {
        if (this.idle > 0.3 && speed > LEASH.minSpeed) this.f -= side / this.arm * LEASH.gain * dt; // sideways motion only
        if (speed > LEASH.minSpeed && this.steady >= LEASH.steadyS && this.idle >= LEASH.idleS) { // slow settle behind
          let d = angDiff(this.f, h); if (Math.abs(d) > rad(150)) d = this.side * Math.abs(d) * -1;
          this.f = smoothDamp(this.f, this.f + d, this.fVel, LEASH.settleS, rad(LEASH.maxDegPerS), dt);
        } else this.fVel.v *= Math.exp(-dt / 0.2);
      } else if (this.idle >= SNAP.lookReturnS && speed > LEASH.minSpeed) { // 0.2.34: swing behind the heading in about 0.5 s
        this.f = smoothDamp(this.f, this.f + angDiff(this.f, h), this.fVel, SNAP.smoothS, 99, dt);
      }
      const a = agent.g.position, kx = 1 - Math.exp(-dt / 0.15), ky = 1 - Math.exp(-dt / 0.5);
      this.pivot.x += (a.x - this.pivot.x) * kx; this.pivot.z += (a.z - this.pivot.z) * kx; this.pivot.y += (a.y - this.pivot.y) * ky;
      tgt = new THREE.Vector3(this.pivot.x, 1.5, this.pivot.z);
      pos = new THREE.Vector3(tgt.x - Math.sin(this.f) * Math.cos(this.pitch) * this.arm, tgt.y + Math.sin(this.pitch) * this.arm, tgt.z - Math.cos(this.f) * Math.cos(this.pitch) * this.arm);
      fov = 60;
    }
    if (this.blend < 1 && this.from) { // eased fly between the views, about 0.5 s
      this.blend = Math.min(1, this.blend + dt / 0.5); const s = this.blend * this.blend * (3 - 2 * this.blend);
      pos = this.from.pos.clone().lerp(pos, s); tgt = this.from.tgt.clone().lerp(tgt, s); fov = this.from.fov + (fov - this.from.fov) * s;
    }
    this.lastTgt.copy(tgt); cam.position.copy(pos); cam.fov = fov; cam.lookAt(tgt); cam.updateProjectionMatrix(); cam.updateMatrixWorld();
  }
  get facingDeg() { return deg(this.f); }
}
