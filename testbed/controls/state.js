// One shared state object, read by every module.
export const G = {
  preset: 'lead', marker: 'ring', camMode: 'snap', view: 'cmd', actScale: 4,
  sel: 0, group: [true, false, false, false], carry: null, squadAct: 'best', tpSquad: 'follow', box: null, hover: null, selTarget: null, ring: null, prompt: null,
  trail: 0, alert: 0, cash: 100000, timeMode: 'total', clock: 0,
  world: null, rig: null, keys: new Set(), ptr: { x: 0, y: 0, over: false }, log: [],
};
// The click log: what each click did, so a misclick shows.
export const say = (text, tone = '') => { G.log.push({ text, tone, at: performance.now() }); if (G.log.length > 40) G.log.shift(); };
