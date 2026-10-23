// Suture, brief 027, engine 0.20.0: writes the attempt plans (plans/<id>.plan.json), each derived from the loud reference
// (contracts/missions/handover/routes/plans/loud.plan.json, copied as plans/base.plan.json). Every attempt keeps one or more agents
// holstered ("clean": never drawn, never fired) through some or all of the final fight and tries to profit from it.
// Base step indices (0-based): 11-13 scout draw/stance/lock1, 14-16 fixer, 17-19 tech, 20-22 the blocker locks (S, F, T),
// 23-24 scout rivals + flank, 25-26 fixer rivals + flank, 27 enforcer out (rivals.retreat), 28-29 S, F back, 30 enforcer board,
// 31-33 S, F, T to the bay, 34-36 their boards, 37 go.
import { readFileSync, writeFileSync, readdirSync, unlinkSync } from 'node:fs';
import { join } from 'node:path';
type Step = Record<string, any>;
const DIR = import.meta.dir;
const base = JSON.parse(readFileSync(join(DIR, 'plans/base.plan.json'), 'utf8'));
const B: Step[] = base.steps;
const C = (i: number, o: Step = {}) => ({ ...structuredClone(B[i]!), ...o });
const fightOps = { scout: [11, 12, 13, 20], fixer: [14, 15, 16, 21], tech: [17, 18, 19, 22] } as const;
const rivalRole = { scout: [23, 24, 28], fixer: [25, 26, 29] } as const;
type Op = 'scout' | 'fixer' | 'tech';

/** Build a plan: `clean` agents never take part in the response-unit fight; `rivals` lists who does the rival crew (each given one of
 *  the base's two flank roles in order: scout's, then fixer's); `extra` replaces/inserts steps by a hook. */
function plan(id: string, note: string, o: { clean?: Op[]; rivals?: Op[]; edit?: (s: Step[]) => Step[]; delay?: Partial<Record<Op, string>> }) {
  const clean = new Set(o.clean ?? []), rivals = o.rivals ?? (['scout', 'fixer'] as Op[]).filter(x => !clean.has(x));
  const steps: Step[] = [];
  for (let i = 0; i < B.length; i++) {
    const s = B[i]!;
    // the response-unit fight
    const fo = (Object.entries(fightOps) as [Op, readonly number[]][]).find(([, ix]) => ix.includes(i));
    if (fo) {
      if (clean.has(fo[0])) continue;
      const st = C(i);
      if (s.do === 'draw' && o.delay?.[fo[0]]) { st.at = o.delay[fo[0]]; st.why = `late draw: no earlier than ${o.delay[fo[0]]}`; }
      steps.push(st); continue;
    }
    // the rival crew (two roles, the scout's at 23/24/28, the fixer's at 25/26/29), recast
    const rr = (Object.entries(rivalRole) as [Op, readonly number[]][]).find(([, ix]) => ix.includes(i));
    if (rr) {
      const role = rr[0] === 'scout' ? 0 : 1, who = rivals[role];
      if (!who) continue;
      const st = C(i, { op: who });
      if (st.label) st.label = st.label.replace(rr[0], who);
      if (st.after) st.after = st.after.map((a: string) => a === rr[0] ? who : a.replace(`${rr[0]}-`, `${who}-`));
      if (i === 28 || i === 29) st.to = who === 'tech' ? [725, -4625] : who === 'scout' ? [975, -4625] : [1175, -4825]; // back to its own wait
      steps.push(st); continue;
    }
    steps.push(C(i));
  }
  const out = { ...base, note: `Suture 0.20 attempt ${id}: ${note} (derived from loud.plan.json).`, steps: o.edit ? o.edit(steps) : steps };
  writeFileSync(join(DIR, 'plans', `${id}.plan.json`), JSON.stringify(out, null, 1) + '\n');
}
const idx = (s: Step[], f: (x: Step) => boolean) => { const k = s.findIndex(f); if (k < 0) throw new Error('no step'); return k; };
const insertBefore = (s: Step[], f: (x: Step) => boolean, ...n: Step[]) => { s.splice(idx(s, f), 0, ...n); return s; };

for (const f of readdirSync(join(DIR, 'plans'))) if (f !== 'base.plan.json') unlinkSync(join(DIR, 'plans', f));

// --- one gun fewer: an agent kept out of the whole final fight, boarding with the rest
plan('clean-T', 'the Tech never draws: holstered at his car wait (cv-003) through the whole fight, boards with the rest', { clean: ['tech'] });
plan('clean-F', 'the Fixer never draws: holstered at cv-006 (2 m from the response unit as it passes); the Scout alone drives the crew', { clean: ['fixer'] });
plan('clean-F-tech-rivals', 'the Fixer never draws; the Tech takes his flank on the crew', { clean: ['fixer'], rivals: ['scout', 'tech'] });
plan('clean-S', 'the Scout never draws; the Fixer and the Tech take the two flanks on the crew', { clean: ['scout'], rivals: ['tech', 'fixer'] });
plan('clean-SF', 'only the Tech fights (response unit and crew); the Scout and the Fixer holstered at the car', { clean: ['scout', 'fixer'], rivals: ['tech'] });
plan('clean-all', 'nobody draws: the Enforcer walks Vale to the car at once (the crew never driven back), the three holstered board', {
  clean: ['scout', 'fixer', 'tech'], rivals: [],
  edit: s => { const k = idx(s, x => x.label === 'out'); s[k] = { ...s[k], when: 'fight.loud', why: 'no gun covers him: straight to the car' }; return s; },
});
// --- splash, body-block, line of fire
plan('splash-S', 'the Scout holstered on the Fixer\'s exact spot (cv-006, 11.75,-48.25) all fight, never draws; the Fixer fights beside him, the Tech takes the Scout\'s flank', {
  clean: ['scout'], rivals: ['tech', 'fixer'],
  edit: s => { s[idx(s, x => x.op === 'scout' && x.do === 'move' && Array.isArray(x.to) && x.to[0] === 975)]!.to = [1175, -4825]; return s; },
});
plan('escort-S', 'the Scout holstered walks beside the Enforcer: to the service door before the doors open, then with him and Vale to the car (a body-block / decoy); never draws', {
  clean: ['scout'], rivals: ['tech', 'fixer'],
  edit: s => {
    const k = idx(s, x => x.op === 'tech' && x.do === 'move' && Array.isArray(x.to) && x.to[0] === 725);
    s.splice(k + 1, 0, { op: 'scout', do: 'move', to: [6250, -1700], after: ['enter'], why: 'holstered, to the service door to stand by the Enforcer' });
    insertBefore(s, x => x.label === 'in', { op: 'scout', do: 'move', to: 'd:Getaway bay', after: ['out'], why: 'beside the Enforcer as he walks Vale to the car' });
    const b = idx(s, x => x.op === 'scout' && x.do === 'move' && x.to === 'd:Getaway bay' && x.when === 'board.in'); s.splice(b, 1);
    return s;
  },
});
plan('line-S', 'the Scout holstered stands between the café wall officers\' stands (-6,-47.4) and the car, at (1,-47.4), from the crew\'s retreat until he boards: in their line of fire on the boarders', {
  clean: ['scout'], rivals: ['tech', 'fixer'],
  edit: s => { insertBefore(s, x => x.label === 'out', { op: 'scout', do: 'move', to: [100, -4740], after: ['enter'], when: 'security.down', why: 'into the line between the officers and the car' }); return s; },
});
// --- holstering after firing: does a dirty agent become clean again?
plan('dirty-holster-T', 'the Tech fires in the response-unit volley, holsters once it is down, and walks to the car and stands there (a fired agent holstered: still a foe?)', {
  edit: s => {
    const k = idx(s, x => x.op === 'tech' && x.do === 'fire' && x.target === 'security-1');
    s.splice(k + 1, 0, { op: 'tech', do: 'holster', after: ['tech'], when: 'security.down', why: 'holstered once the unit is down' },
      { op: 'tech', do: 'move', to: 'd:Getaway bay', why: 'stands at the car, holstered' });
    const m = idx(s, x => x.op === 'tech' && x.do === 'move' && x.to === 'd:Getaway bay' && x.when === 'board.in'); s.splice(m, 1);
    return s;
  },
});
plan('clean-T-early', 'the Tech never draws, walks to the car as the unit falls and stands there holstered; boards with the rest', {
  clean: ['tech'],
  edit: s => {
    insertBefore(s, x => x.label === 'out', { op: 'tech', do: 'move', to: 'd:Getaway bay', after: ['enter'], when: 'security.down', why: 'clean: to the car early' });
    const m = idx(s, x => x.op === 'tech' && x.do === 'move' && x.to === 'd:Getaway bay' && x.when === 'board.in'); s.splice(m, 1);
    return s;
  },
});
plan('clean-T-boarded', 'the Tech never draws and boards the car as the doors open (in the car through the fight)', {
  clean: ['tech'],
  edit: s => {
    insertBefore(s, x => x.label === 'out', { op: 'tech', do: 'move', to: 'd:Getaway bay', after: ['enter'], when: 'fight.loud', why: 'clean: to the car' }, { op: 'tech', do: 'board', target: 'car', label: 'tech-in', every: 0.5 });
    const m = idx(s, x => x.op === 'tech' && x.do === 'move' && x.to === 'd:Getaway bay' && x.when === 'board.in'); s.splice(m, 1);
    const b = s.findLastIndex(x => x.op === 'tech' && x.do === 'board'); s.splice(b, 1);
    return s;
  },
});
// --- late draws: free positioning time, then a first volley
for (const d of ['3:46.5', '3:47.5', '3:48.5', '3:50.5'])
  plan(`late-all-${d.replace(':', '').replace('.', '')}`, `all three holstered after the doors open (3:45.5), drawing no earlier than ${d}`, { delay: { scout: d, fixer: d, tech: d } });
plan('late-F-3470', 'the Fixer (nearest the unit, 2 m) draws 1.5 s after the others', { delay: { fixer: '3:47.0' } });
plan('late-F-3480', 'the Fixer draws 2.5 s after the others', { delay: { fixer: '3:48.0' } });
// --- walk up holstered, draw at point blank
plan('assassin-sec1', 'all three holstered as the doors open; the Scout and the Tech walk up to the blocker\'s planter cover (security-1, 18.1,-50.8) before drawing; the Fixer draws on security-2 beside him', {
  delay: { scout: '3:49.0', tech: '3:49.0' },
  edit: s => {
    insertBefore(s, x => x.op === 'scout' && x.do === 'draw', { op: 'scout', do: 'move', to: [1650, -5000], after: ['enter'], when: 'fight.loud', why: 'holstered, up to the blocker' });
    insertBefore(s, x => x.op === 'tech' && x.do === 'draw', { op: 'tech', do: 'move', to: [1650, -5150], after: ['enter'], when: 'fight.loud', why: 'holstered, up to the blocker' });
    return s;
  },
});
// --- a clean gun waiting by the crew's press stop (52.6,-25.8): first volley at point blank
for (const [who, at, pt] of [['scout', '3:58', [5000, -2900]], ['scout', '4:02', [5000, -2900]], ['scout', '4:02', [5550, -2700]], ['tech', '4:02', [5000, -2900]], ['tech', '4:02', [5550, -2700]]] as const) {
  const others: Op[] = (['scout', 'fixer', 'tech'] as Op[]).filter(x => x !== who);
  plan(`ambush-${who[0]!.toUpperCase()}-${pt[0]}-${at.replace(':', '')}`, `the ${who} kept out of the response-unit fight, waits holstered at (${pt[0] / 100},${pt[1] / 100}) by the crew's press stop, then locks the crew from point blank at ${at}; the ${others.join(' and ')} drop the unit and the Fixer flanks as before`, {
    clean: [who], rivals: ['fixer'],
    edit: s => {
      const mv = idx(s, x => x.op === who && x.do === 'move' && Array.isArray(x.to) && (x.to[0] === 975 || x.to[0] === 725));
      s[mv] = { op: who, do: 'move', to: [...pt], after: [who === 'tech' ? 'enter' : 'shot'], why: 'holstered, to wait by the crew\'s press stop' };
      insertBefore(s, x => x.label === 'out', { op: who, do: 'fire', target: 'rivals', at, after: [who], label: `${who}-ambush`, why: 'clean until now: draw and lock the crew from point blank' });
      insertBefore(s, x => x.label === 'in', { op: who, do: 'move', to: 'd:Getaway bay', after: [`${who}-ambush`], when: 'rivals.retreat' });
      const b = idx(s, x => x.op === who && x.do === 'move' && x.to === 'd:Getaway bay' && x.when === 'board.in'); s.splice(b, 1);
      return s;
    },
  });
}
console.log(readdirSync(join(DIR, 'plans')).length, 'plans');

// --- round 2: dirty to the police (fired), clean to the crew (whose causes are drawn, Vale, seen, never fired): holster once the unit is
// down, close in holstered on the crew's press stop (52.6,-25.8), draw and lock it from point blank. Twins: the same walk drawn.
const P2: Record<string, [number, number]> = { scout: [4960, -2700], fixer: [5050, -2900], tech: [4900, -2950] };
function closeIn(id: string, holster: boolean, run: boolean, gap: number, withTech = false, pts = P2) {
  const who: Op[] = withTech ? ['scout', 'fixer', 'tech'] : ['scout', 'fixer'];
  plan(id, `once the response unit is down the ${who.join(', ')} ${holster ? 'holster and ' : ''}${run ? 'run' : 'walk'} ${holster ? 'holstered ' : 'drawn '}to 3-4 m of the crew's press stop, then lock it ${gap} s after setting off`, {
    edit: s => {
      for (const op of who) {
        const role = op === 'tech' ? null : op;
        const fire = idx(s, x => x.op === op && x.do === 'fire' && x.target === (role ? 'rivals' : 'security-1'));
        const mk: Step[] = [
          ...(holster ? [{ op, do: 'holster', after: [op], when: 'security.down', why: 'the unit down: holstered, clean to the crew' }] : []),
          { op, do: 'move', to: pts[op], run, after: [op], when: 'security.down', label: `${op}-flank`, why: 'to point blank of the crew\'s press stop' },
          { op, do: 'fire', target: 'rivals', after: [`${op}-flank`], gap, label: `${op}-rivals`, why: 'draw and lock the crew from point blank' },
        ];
        if (role) { s.splice(fire, 2, ...mk); } else s.splice(fire + 1, 0, ...mk);
      }
      if (withTech) { // the Tech back to his wait when the crew retreats, before the board
        insertBefore(s, x => x.label === 'in', { op: 'tech', do: 'move', to: [725, -4625], after: ['tech-flank'], when: 'rivals.retreat' });
      }
      return s;
    },
  });
}
for (const g of [14, 17, 20, 24]) { closeIn(`close-H-run-g${g}`, true, true, g); closeIn(`close-D-run-g${g}`, false, true, g); }
for (const g of [30, 36]) { closeIn(`close-H-walk-g${g}`, true, false, g); closeIn(`close-D-walk-g${g}`, false, false, g); }
closeIn('close-H-run-g17-T', true, true, 17, true); closeIn('close-D-run-g17-T', false, true, 17, true);
console.log(readdirSync(join(DIR, 'plans')).length, 'plans');

// --- round 3: the flicker. A drawn agent that never fired is clean again once holstered (in the final fight a draw is never seen as a
// face). The Fixer, nearest the unit (2 m), draws with the doors, holsters before its first shots, then draws again with a lock.
for (const h of ['3:45.6', '3:46.0']) for (const f of ['3:46.5', '3:47.0', '3:47.5']) {
  plan(`flicker-F-h${h.replace(/[:.]/g, '')}-f${f.replace(/[:.]/g, '')}`, `the Fixer draws with the doors, holsters at ${h} (never fired: clean again), then locks security-2 at ${f}`, {
    edit: s => {
      const k = idx(s, x => x.op === 'fixer' && x.do === 'fire' && x.target === 'security-2');
      s.splice(k, 0, { op: 'fixer', do: 'holster', at: h, why: 'flicker: holstered before the unit fires' });
      s[k + 1] = { ...s[k + 1], at: f };
      return s;
    },
  });
}
console.log(readdirSync(join(DIR, 'plans')).length, 'plans');

// --- round 4: the close-in from inside the crew's cone (between its press stop (52.6,-25.8) and the Enforcer at the service door
// (62.5,-16.8)), where a drawn agent is in its sight: holstered (clean to the crew) against drawn.
const IN: Record<string, Record<string, [number, number]>> = { // cells inside the crew's cone at 4:04 (trace/Program.cs cone mode: crew at 52.6,-25.8)
  a: { scout: [5350, -2300], fixer: [5300, -2500], tech: [0, 0] }, // 2-3 m in front of the crew, both cells in its cone
  b: { scout: [5600, -1900], fixer: [5700, -1800], tech: [0, 0] }, // 8-9 m out, between the crew and the Enforcer
  c: { scout: [5300, -2500], fixer: [5200, -2500], tech: [0, 0] }, // 1 m from the crew (its near sense too)
};
for (const [k, pts] of Object.entries(IN)) for (const g of [16, 20]) { closeIn(`cone-${k}-H-g${g}`, true, true, g, false, pts); closeIn(`cone-${k}-D-g${g}`, false, true, g, false, pts); }
console.log(readdirSync(join(DIR, 'plans')).length, 'plans');
