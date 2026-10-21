// One shared state object, read by every module.
export const G = {
  preset: 'hold', marker: 'pill', camMode: 'leash', view: 'cmd', actScale: 4,
  sel: 0, group: [true, false, false, false], carry: null, squadAct: 'best', runMode: 'dbl', chargeS: 0.4, fire: { drawS: 0.6, aimS: 1.0, cadence: 1.5, range: 20 }, beat: null, stopped: null, ordered: null, doneInfo: null, stoppedInfo: null, tpAim: 'fire', harmHold: 'fire', camFollow: false, edgePan: 'off', crowd: 'none', stick: null, tabEnt: null, holdEnt: null, followFire: 'with', sessions: [], paused: false, planMode: 'sync', plan: new Map(), timeScale: 1, slowUntil: 0, flushing: false, attScale: 1, vale: null, charge: null, aim: false, tpSquad: 'follow', box: null, hover: null, selTarget: null, ring: null, prompt: null,
  trail: 0, alert: 0, cash: 100000, timeMode: 'total', clock: 0,
  world: null, rig: null, keys: new Set(), ptr: { x: 0, y: 0, over: false }, log: [],
};
// The click log: what each click did, so a misclick shows.
export const say = (text, tone = '') => { G.log.push({ text, tone, at: performance.now() }); if (G.log.length > 40) G.log.shift(); };
