// Waymark, brief 027 (engine 0.17.0): rewrite the five loud plans' fight around the 0.17 tools. Writes contracts/missions/handover/routes/plans/
// loud*.plan.json from one shared head (the waits behind the getaway car, the focus locks) and each plan's own tail (its intent). Run once;
// the plans, not this file, are the reviewed source.
import { readFileSync, writeFileSync } from 'node:fs';
const DIR = '/Users/sky/Projects/suborned/contracts/missions/handover/routes/plans';
const RE = 'Brief 027 (Waymark, 2026-10-21; engines 0.17.0 and 0.18.0, tuning.md §11-§13: police answer every operative who fired on them in the last 3 s, so they no longer flip between the cell\'s shooters): the three wait behind the getaway car instead of the car\'s east end and the pavilion piers (there the passing response unit downed the Scout at 2 m and the Fixer had no line past his pier), and fight with locks: as the doors open all three lock the nearest officer of the response unit, then, once it is down, the blocker. Focus is what wins it: the same waits firing at will leave an op behind or fail in 26 of 48 half-metre and metre moves of one wait (engine 0.18.0), with the locks in 11 of 48; the first locks must be given within 0.5 s of the doors opening, so pause (briefs/027/waymark/loud-robust-0.18.txt).';
const head = (scoutWhy = 'the run is loud now, so running is free: to the getaway car\'s north side, east end (cover point cv-005), shielded from the lane the response unit walks along') => [
  { op: 'tech', do: 'move', to: 'cabinet', run: true },
  { op: 'enforcer', do: 'move', to: [5650, -4750], why: 'brief 031 (Waymark): to wait on the forecourt 9.8 m from the public door, behind the entrance camera\'s cone and the patrol\'s sweep, outside the guard\'s door cone and more than 3 m from him (2.4 m clear of every cone at any time before 3:20): by the door, in the camera\'s view until the loop lands, it grew wary of him (29 s; reported at 54 s on the loud route)', run: true },
  { op: 'tech', do: 'loop', target: 'entrance' },
  { op: 'tech', do: 'reroute', target: 'patrol', label: 'reroute' },
  { op: 'enforcer', do: 'move', to: [6950, -4250], after: ['reroute'], when: 'patrol.rerouted', why: 'brief 031: once the patrol has left its sweep (the entrance camera is looped by then), from the forecourt to wait by the service end of the west face, 5.6 m south of the public door, out of the guard\'s cone and more than 3 m from him: nobody who keeps a clock sees him there' },
  { op: 'enforcer', do: 'fire', target: 'guard', after: ['reroute', 'enforcer'], at: '2:31', label: 'shot', why: 'once the patrol has gone and just after the guard\'s 2:30 check-in mark (brief 013: his marks are 1:00, 2:30, 4:00; a shot before a mark lets the missed mark call the response unit early, to the car before the Enforcer is out): fire until the guard is down' },
  { op: 'scout', do: 'move', to: [975, -4625], run: true, after: ['shot'], why: scoutWhy },
  { op: 'fixer', do: 'move', to: [1175, -4825], run: true, after: ['shot'], why: 'brief 027: to the getaway car\'s east end (cover point cv-006), the car between him and the café wall\'s police stands' },
  { op: 'enforcer', do: 'move', to: 'd:Office entrance', after: ['shot'], gap: 4, run: true },
  { op: 'enforcer', do: 'enter', clear: 1, label: 'enter', why: 'Alert 2: the entry takes 68 s' },
  { op: 'tech', do: 'move', to: [725, -4625], after: ['enter'], why: 'once the Enforcer is inside the loop can go: the Tech walks (no run while his loop holds) to the getaway car\'s north side, west end (cover point cv-003)' },
  ...(['scout', 'fixer', 'tech'] as const).flatMap(op => [
    { op, do: 'draw', when: 'fight.loud', after: ['enter'], why: 'draws when the final fight opens, not before: holstered, he is passed by the response unit on its way to the door (a drawn weapon seen on the street is a witnessed incident)' },
    { op, do: 'stance', stance: 'free', why: 'the final fight is on: once his locks are spent, fire at will (its shots are untraced)' },
    { op, do: 'fire', target: 'security-2', label: `${op}-lock1`, why: 'brief 027 (tuning.md §11.3, focus): as the doors open all three lock the same officer, the response unit\'s nearest (security-2, 2-4 m from the car as it passes): three fresh guns put three hits on its 3 health in one volley. Given late (1.0 s after the doors open), the Scout and the Fixer are downed' },
  ]),
  { op: 'enforcer', do: 'move', to: 'd:Getaway bay', label: 'out', after: ['enter'], when: 'fight.loud', why: 'with Vale to the car: the cell covers him from behind it' },
  ...(['scout', 'fixer', 'tech'] as const).map(op => ({ op, do: 'fire', target: 'security-1', after: [`${op}-lock1`], when: 'security.officer-down', every: 0.5, why: 'the first officer down: lock the blocker (security-1, going for the road at the planter run 11 m east of the car), the one that would shoot the Enforcer as he walks Vale in (a window of about 1.5 s before the at-will fire downs it: every 0.5 s)' })),
];
const tail = {
  board: [
    { op: 'enforcer', do: 'board', target: 'car', after: ['out'], label: 'in', why: 'Vale in first (Board, 4 s carrying; brief 018 §2)' },
    { op: 'scout', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in', why: 'the blocker down and the Enforcer in with Vale (board.in): to the car (the officers at their stands do not hold it). Walking to the bay while he boards, the three stand in the café wall officers\' reach for 4 s and are downed' },
    { op: 'fixer', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in' },
    { op: 'tech', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in' },
    { op: 'scout', do: 'board', target: 'car', label: 'scout-in', every: 0.5, why: 'boarded the moment he stands within 1.5 m of the car (every 0.5 s: stood beside it longer, he is in the café wall officers\' line)' },
    { op: 'fixer', do: 'board', target: 'car', label: 'fixer-in', every: 0.5 },
    { op: 'tech', do: 'board', target: 'car', label: 'tech-in', every: 0.5 },
    { op: 'cell', do: 'go', after: ['scout-in', 'fixer-in', 'tech-in'], gap: 1, unless: 'captured', why: 'once the last board lands (1 s): the seal clock runs, so anyone left standing is captured (brief 018 §3); the whole cell leaves' },
  ],
  fight: [
    { op: 'scout', do: 'move', to: [1175, -4775], after: ['enter'], when: 'security.down', every: 0.5, why: 'fight it out: with the response unit down, the Scout steps to the pavilion centre pier\'s south face (cv-094), which has a line over the car to the officers coming to the café wall; from behind the car\'s north side the west pier blocks it both ways' },
    { op: 'tech', do: 'move', to: [1025, -4625], after: ['enter'], when: 'security.down', every: 0.5, why: 'and the Tech to the same pier\'s west face (cv-093); the Fixer keeps the car\'s east end. All three fire at will on the officers as they come' },
    { op: 'enforcer', do: 'board', target: 'car', after: ['out'], label: 'in', why: 'Vale in first (Board, 4 s carrying; brief 018 §2)' },
    { op: 'scout', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'fight.over', why: 'fight it out: the three stay at the car, firing at will, until the officers at the café wall are down too' },
    { op: 'scout', do: 'board', target: 'car', label: 'scout-in', every: 0.5 },
    { op: 'fixer', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'fight.over' },
    { op: 'fixer', do: 'board', target: 'car', label: 'fixer-in', every: 0.5 },
    { op: 'tech', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'fight.over' },
    { op: 'tech', do: 'board', target: 'car', label: 'tech-in', every: 0.5 },
    { op: 'cell', do: 'go', after: ['scout-in', 'fixer-in', 'tech-in'], gap: 1, unless: 'captured' },
  ],
  downed: [
    { op: 'scout', do: 'move', to: [100, -4500], when: 'security.down', after: ['enter'], why: 'the run that leaves a downed op (brief 018 §5): with the response unit down the Scout walks out of cover at the officers coming to the café wall, is downed, and nobody carries him' },
    { op: 'enforcer', do: 'board', target: 'car', after: ['out'], label: 'in', why: 'Vale in first (Board, 4 s carrying; brief 018 §2)' },
    { op: 'fixer', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in' },
    { op: 'tech', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in' },
    { op: 'fixer', do: 'board', target: 'car', label: 'fixer-in', every: 0.5 },
    { op: 'tech', do: 'board', target: 'car', label: 'tech-in', every: 0.5 },
    { op: 'cell', do: 'go', after: ['in', 'fixer-in', 'tech-in'], gap: 4 },
  ],
  standing: [
    { op: 'enforcer', do: 'board', target: 'car', after: ['out'], label: 'in', why: 'Vale in first (Board, 4 s carrying; brief 018 §2)' },
    { op: 'fixer', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in' },
    { op: 'tech', do: 'move', to: 'd:Getaway bay', after: ['in'], when: 'board.in' },
    { op: 'fixer', do: 'board', target: 'car', label: 'fixer-in', every: 0.5 },
    { op: 'tech', do: 'board', target: 'car', label: 'tech-in', every: 0.5 },
    { op: 'cell', do: 'go', after: ['in', 'fixer-in', 'tech-in'], gap: 4, why: 'the Scout is not called to the car: left standing at his post with the officers at the café wall still up, he is captured fighting (abandoned)' },
  ],
};
const plans: [string, keyof typeof tail][] = [['loud', 'board'], ['loud-fast', 'board'], ['loud-fight', 'fight'], ['loud-left-downed', 'downed'], ['loud-left-standing', 'standing']];
for (const [id, t] of plans) {
  const f = `${DIR}/${id}.plan.json`, old = JSON.parse(readFileSync(f, 'utf8'));
  const note = old.note.replace(/ Brief 027 \(Waymark.*$/, '') + ' ' + RE;
  const steps = [...head(), ...tail[t]];
  const body = ['{', ' "schema": "suborned.mission.plan/1",', ` "mission": "handover",`, ` "note": ${JSON.stringify(note)},`, ` "start": ${JSON.stringify(old.start ?? {})},`, ` "until": ${old.until},`, ' "steps": [',
    ...steps.map((s, i) => `  ${JSON.stringify(s).replace(/":/g, '": ').replace(/,"/g, ', "')}${i < steps.length - 1 ? ',' : ''}`), ' ]', '}'];
  writeFileSync(f, body.join('\n') + '\n');
  console.log(`wrote ${id}.plan.json: ${steps.length} steps`);
}
