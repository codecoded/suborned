import { G } from './state.js';
// Everything a designer tunes: acts and their prices, who can be acted on, and the presets.
export const WALK = 1.8, RUN = 2.8; // m/s (the GDD's running speed)

// Kind colours. Not red, amber or violet: those belong to cones, guards and rivals.
// Harm gets a different shape as well (a diamond badge, a dashed ring).
export const KINDS = {
  look: { color: '#4aa8ff', label: 'Look' },
  talk: { color: '#4cc35f', label: 'Talk' },
  tech: { color: '#1fd1b5', label: 'Tech' },
  take: { color: '#f1ead8', label: 'Take' },
  harm: { color: '#ff4d7d', label: 'Harm' },
};

// by: S scout, F fixer, T tech, E enforcer. t: seconds at Handover prices.
export const ACTS = {
  watch: { verb: 'Watch', kind: 'look', icon: 'eye', t: 20, by: 'SFTE', again: true },
  pay: { verb: 'Pay', kind: 'talk', icon: 'coin', t: 50, by: 'F', cost: 70000, trail: 2 },
  call: { verb: 'Call', kind: 'talk', icon: 'phone', t: 0, by: 'F', trail: 1, noWalk: true },
  press: { verb: 'Pressure', kind: 'talk', icon: 'talk', t: 30, by: 'F', trail: 1 },
  take: { verb: 'Take', kind: 'take', icon: 'hand', t: 38, by: 'FE', trail: 2, turnedFree: true },
  loop: { verb: 'Loop camera', bare: true, kind: 'tech', icon: 'loop', t: 62, by: 'T', trail: 1 },
  open: { verb: 'Open', kind: 'tech', icon: 'door', t: 12, by: 'T' },
  knock: { verb: 'Knock out', kind: 'harm', icon: 'knock', t: 6, by: 'E', trail: 2, found: true, alert: true },
  kill: { verb: 'Kill', kind: 'harm', icon: 'skull', t: 3, by: 'E', trail: 3, found: true, alert: true },
  fire: { verb: 'Fire at', kind: 'harm', icon: 'gun', t: 4, by: 'SFTE', noWalk: true, alert: true, fire: true },
};

// Ways on each kind of target, best first. The first one a selected agent can do is the main action.
export const TARGETS = {
  guard: { name: 'the guard', acts: ['watch', 'press', 'knock', 'kill'] },
  inez: { name: 'Inez', acts: ['pay', 'watch', 'call', 'knock'] },
  driver: { name: 'the driver', acts: ['take', 'watch', 'pay', 'knock', 'kill'] },
  camera: { name: 'the camera', acts: ['loop', 'watch'] },
  cabinet: { name: 'the cabinet', acts: ['loop', 'open'] },
  door: { name: 'the door', acts: ['open', 'watch'] },
  car: { name: 'the car', acts: ['take', 'watch'] },
  walker: { name: 'the passer-by', acts: ['watch', 'pay', 'take', 'knock'] },
  police: { name: 'the officer', acts: ['fire', 'watch'], hostile: true },
  rival: { name: 'the rival', acts: ['fire', 'watch'], hostile: true },
};

export const ROLES = [
  { key: 'S', name: 'Scout', color: '#8fd3c8' },
  { key: 'F', name: 'Fixer', color: '#f2b48a' },
  { key: 'T', name: 'Tech', color: '#9db7ff' },
  { key: 'E', name: 'Enforcer', color: '#e6dfcc' },
];

// A preset is one small object. Add one here and it appears as a tab.
//   left.thing / left.ground: what a single left click does on a thing / on open ground:
//        'act' = the main action, 'select' = pick the thing, 'walk'
//   dbl:  what a double click does ('act', 'run' or null)
//   leftHold: opens the ring after a 0.22 s left hold (the 0.2.34 way): 'tp' = third person only
//   right: 'tap' = ring opens on release of a quick right click; 'press' = opens on press, a second right click takes the lit way
//   eHold: a held E opens the ring (third person)
//   ring: 'calm' (icons, one plate) or 'busy' (a plate beside every wedge)
export const PRESETS = {
  lead: {
    label: "Sky's lead", marker: 'ring', ring: 'calm',
    left: { thing: 'act', ground: 'walk' }, dbl: { thing: null, ground: null },
    leftHold: false, right: 'tap', eHold: false,
    about: 'Left click does the main action. Left click on the ground walks. A quick right click opens the other ways.',
  },
  dbl: {
    label: 'Double click', marker: 'ring', ring: 'calm',
    left: { thing: 'select', ground: 'walk' }, dbl: { thing: 'act', ground: 'run' },
    leftHold: false, right: 'tap', eHold: false,
    about: 'Double click does the main action. A single click selects or walks. A quick right click opens the other ways.',
  },
  built: {
    label: '0.2.34 as built', marker: 'built', ring: 'busy',
    left: { thing: 'select', ground: 'walk' }, dbl: { thing: null, ground: null },
    leftHold: 'tp', right: 'press', eHold: true,
    about: 'Right press opens the ring at once; a second right click takes the lit way. A held left click or held E opens it on the ground view.',
  },
};

// The price line: verb and target, the time, then Trail and Alert.
// G.timeMode: total (walk + act as one number), split ("9 s walk + 20 s"), line (act time only; the walk is drawn on the ground).
export function priceLine(act, tgt, walkS, withTarget = true, who = '') {
  const walk = Math.round(act.noWalk ? 0 : walkS), mode = G.timeMode;
  const fmt = (n) => (n === 0 ? 'now' : `${n} s`);
  const time = mode === 'split' && walk > 0 ? `${walk} s walk + ${fmt(act.t)}` : mode === 'line' ? fmt(act.t) : fmt(walk + act.t);
  const head = act.bare ? act.verb : `${act.verb}${withTarget ? ' ' + tgt.name : ''}`;
  const parts = [head, act.fire ? `down in ${time}` : time];
  if (act.cost) parts.push(`$${act.cost.toLocaleString('en-US')}`);
  const trail = act.turnedFree && tgt?.used?.has('pay') ? 0 : act.trail;
  if (trail) parts.push(`+${trail} Trail${act.found ? ' if found' : ''}`);
  if (act.alert) parts.push('raises Alert');
  if (who) parts.push(who);
  return parts;
}
