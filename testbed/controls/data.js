import { G } from './state.js';
// Everything a designer tunes: acts and their prices, who can be acted on, and the presets.
// Right button timing (brief 041; Latch mirrors these in Unity). Under RIGHT_TAP_S it is a tap: the ring, no charge, no pill, no draw.
// After it the "Hold to fire" charge shows and fills for FIRE_CHARGE_S; the shot goes at RIGHT_TAP_S + FIRE_CHARGE_S (0.4 s). Letting go before that cancels.
export const RIGHT_TAP_S = 0.2, FIRE_CHARGE_S = 0.2;
G.chargeS = RIGHT_TAP_S + FIRE_CHARGE_S; // the total from the press; the lab and the left-hold charge read this too
// Left-hold acts (hold preset): the pill fills over the same G.chargeS (0.4 s), then says "Let go to ...". 'release' = the order is given when the button comes up (after full);
// 'full' = the order is given the moment the pill is full (the old way). Fire is never affected: it keeps going while held. Default and the cue length are here.
export const ACT_COMMIT_DEFAULT = 'release', READY_POP_S = 0.35; // READY_POP_S: the pill pop length; the portrait flash and cursor last while the pill says "Let go"
G.actCommit = ACT_COMMIT_DEFAULT;
export const WALK = 1.4, RUN = 2.8; // m/s: the engine's walk and run (sim/cli/DriveSession.cs, Handover chip: run 2x walk)

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
  watch: { ing: 'Watching', doneText: 'learned their round', verb: 'Watch', kind: 'look', icon: 'eye', t: 20, by: 'SFTE', again: true, reachBy: { S: 30, T: 24, F: 12, E: 3 } }, // brief 031: how far each agent can watch from
  listen: { ing: 'Listening', doneText: 'heard them', verb: 'Listen', kind: 'look', icon: 'ear', t: 42, by: 'S' }, // brief 043: the Handover's Listen (42 s, the Scout)
  pay: { ing: 'Paying', doneText: 'paid', verb: 'Pay', kind: 'talk', icon: 'coin', t: 50, by: 'F', cost: 70000, trail: 2 },
  call: { ing: 'Calling', doneText: 'call made', verb: 'Call', kind: 'talk', icon: 'phone', t: 0, by: 'F', trail: 1, noWalk: true },
  help: { ing: 'Helping', doneText: 'helped', verb: 'Help', kind: 'talk', icon: 'heart', t: 50, by: 'F', cost: 40000, trail: 1 },
  press: { ing: 'Pressuring', doneText: 'pressed', verb: 'Pressure', kind: 'talk', icon: 'talk', t: 30, by: 'F', trail: 1 },
  take: { ing: 'Taking', doneText: 'taken', verb: 'Take', kind: 'take', icon: 'hand', t: 38, by: 'FE', trail: 2, turnedFree: true },
  loop: { ing: 'Looping the camera', doneText: 'camera looped', verb: 'Loop camera', phrase: 'loop the camera', bare: true, kind: 'tech', icon: 'loop', t: 62, by: 'T', trail: 1 },
  board: { ing: 'Boarding', doneText: 'aboard', verb: 'Board', kind: 'take', icon: 'car', t: 4, by: 'SFTE' },
  open: { ing: 'Opening', doneText: 'open', verb: 'Open', kind: 'tech', icon: 'door', t: 12, by: 'T' },
  knock: { ing: 'Knocking out', doneText: 'knocked out', verb: 'Knock out', kind: 'harm', icon: 'knock', t: 6, by: 'E', trail: 2, found: true, alert: true },
  kill: { ing: 'Killing', doneText: 'done', verb: 'Kill', kind: 'harm', icon: 'skull', t: 3, by: 'E', trail: 3, found: true, alert: true },
  fire: { ing: 'Firing', doneText: '', verb: 'Fire at', kind: 'harm', icon: 'gun', t: 4, by: 'SFTE', noWalk: true, alert: true, fire: true },
};

// Ways on each kind of target, best first. The first one a selected agent can do is the main action.
export const TARGETS = {
  guard: { name: 'the guard', acts: ['watch', 'listen', 'press', 'knock', 'kill'] },
  inez: { name: 'Inez', acts: ['pay', 'watch', 'listen', 'help', 'press', 'call', 'knock'] },
  driver: { name: 'the driver', acts: ['take', 'watch', 'listen', 'pay', 'help', 'call', 'knock', 'kill'] },
  camera: { name: 'the camera', acts: ['loop', 'watch'] },
  cabinet: { name: 'the cabinet', acts: ['loop', 'open'] },
  door: { name: 'the door', acts: ['open', 'watch'] },
  car: { name: 'the car', acts: ['take', 'watch', 'board'] },
  walker: { name: 'the passer-by', acts: ['watch', 'pay', 'take', 'knock'] },
  vale: { name: 'Vale', acts: ['take', 'watch'] },
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
  hold: {
    label: 'Click walks, hold acts', marker: 'pill', ring: 'calm', runMode: 'dbl',
    left: { thing: 'walk', ground: 'walk' }, dbl: { thing: 'run', ground: 'run' },
    leftHold: false, right: 'tap', eHold: false, agentClick: 'none', shiftRuns: true, thingWalk: true, holdActs: true, rightFires: true,
    about: 'Click walks there, on a thing too; double click runs. Hold the left button on a thing to do its main action; Shift + hold runs there first. Right tap (under 0.2 s) opens the other ways; right hold on a target fills for 0.4 s, and when you let go the agent fires on its own until it is down (X or × stops it). Fights (Syndicate): once shots are fired a right click on a hostile focuses every selected agent on it, no hold, drawn agents engage on their own (G toggles weapons free), and the fakes shoot back for show.',
  },
  runs: {
    label: 'Click acts, double click runs', marker: 'pill', ring: 'calm', runMode: 'dbl',
    left: { thing: 'act', ground: 'walk' }, dbl: { thing: 'run', ground: 'run' },
    leftHold: false, right: 'tap', eHold: false, agentClick: 'none', shiftRuns: true, bothTimes: true,
    about: 'Click does the main action, walking there. Double click runs there and does it; on the ground, click walks and double click runs. Shift runs too. Select on the portraits.',
  },
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
export function priceLine(act, tgt, walkS, withTarget = true, who = '', run = false) {
  const both = PRESETS[G.preset]?.bothTimes && !run && !act.noWalk && Math.round(walkS) > 0;
  const walk = Math.round(act.noWalk ? 0 : walkS), mode = G.timeMode;
  const fmt = (n) => (n === 0 ? 'now' : `${n} s`);
  const r = run && walk > 0;
  const time = mode === 'split' && walk > 0 ? `${walk} s ${r ? 'run' : 'walk'} + ${fmt(act.t)}` : mode === 'line' ? fmt(act.t) : fmt(walk + act.t) + (r ? ' (run)' : '');
  const head = act.bare ? act.verb : `${act.verb}${withTarget ? ' ' + tgt.name : ''}`;
  const runT = both ? ` · run ${Math.round(walkS * WALK / RUN + act.t)} s` : '';
  const parts = [head, (act.fire ? `down in ${time}` : time) + runT];
  if (act.cost) parts.push(`$${act.cost.toLocaleString('en-US')}`);
  const trail = act.turnedFree && tgt?.used?.has('pay') ? 0 : act.trail;
  if (trail) parts.push(`+${trail} Trail${act.found ? ' if found' : ''}`);
  if (act.alert) parts.push('raises Alert');
  if (who) parts.push(who);
  return parts;
}
export const phrase = (act, tgt) => act.phrase ?? `${act.verb.toLowerCase()} ${tgt.name}`;
