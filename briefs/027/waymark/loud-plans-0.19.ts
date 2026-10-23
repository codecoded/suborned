// Waymark, brief 027 (engine 0.19.0, hostile intent: the rival crew presses for the Vale holder). Rewrites the loud plans' fight so the
// cell protects the Enforcer: the three still drop the response unit at the car as the doors open, but the Enforcer holds Vale at the
// service door (the Office's corner between him and the rivals) while the Scout and Fixer lock the rival crew and walk across the square
// to flank it at 10-11 m; once the crew is driven back he walks Vale to the car. Reads each plan, keeps its head (every step before the
// first draw) and its own tail (every step after the second locks, minus the old walk to the car), and puts the shared protective middle
// between them. Run once; the plans, not this file, are the reviewed source. Evidence: briefs/027/waymark/loud-robust-0.19.txt.
import { readFileSync, writeFileSync } from 'node:fs';
const DIR = '/Users/sky/Projects/suborned/contracts/missions/handover/routes/plans';
const RE = 'Brief 027, engine 0.19.0 (Waymark, 2026-10-22; Pulse hostile-initiative.md, the rival crew drawn by the gunfire presses for the Vale holder): walking Vale straight to the car the Enforcer was downed by the crew from behind at 4:11.9. So the cell protects him: as the doors open he holds Vale at the service door, where the Office\'s west corner is between him and the crew (it stops at its stop point of him, 13 m off, with no line), while the three drop the response unit at the car as before; once it is down the Scout and the Fixer lock the crew and walk across the square to flank it at 10-11 m, inside its own reach, and drive it back (3 hits); then he walks Vale to the car, the Scout and the Fixer walk back to their waits, and the Tech holds the car from his wait, the west pier between him and the café wall officers (briefs/027/waymark/loud-robust-0.19.txt).';
const SCOUT_FLANK = [4300, -3100], FIXER_FLANK = [4400, -3350];
const middle = [
  { op: 'scout', do: 'fire', target: 'rivals', after: ['scout'], when: 'security.down', label: 'scout-rivals', why: 'brief 027, engine 0.19.0: the response unit down, lock the rival crew (it has followed the Enforcer round the Office\'s west corner and stands at its stop point of him, with no line on him while he holds at the service door)' },
  { op: 'scout', do: 'move', to: SCOUT_FLANK, label: 'scout-flank', why: 'and walk (no run) across the square to its west flank, 10-11 m from the crew: inside its own reach (no fire from beyond it), outside its cone, which faces the Enforcer. Locked first, he fires once in reach; the crew is driven back at its third hit' },
  { op: 'fixer', do: 'fire', target: 'rivals', after: ['fixer'], when: 'security.down', label: 'fixer-rivals', why: 'the Fixer with the Scout: two guns drive the crew back in about 3 s (one alone takes 10 s and more)' },
  { op: 'fixer', do: 'move', to: FIXER_FLANK, label: 'fixer-flank', why: 'beside the Scout, 2.6 m south of him' },
  { op: 'enforcer', do: 'move', to: 'd:Getaway bay', label: 'out', after: ['enter'], when: 'rivals.retreat', why: 'held at the service door with Vale since the doors opened: the crew driven back (175 s), he walks Vale to the car' },
  { op: 'scout', do: 'move', to: [975, -4625], after: ['scout-flank'], when: 'rivals.retreat', why: 'the crew gone: back to his wait behind the car (cv-005), ahead of the Enforcer' },
  { op: 'fixer', do: 'move', to: [1175, -4825], after: ['fixer-flank'], when: 'rivals.retreat', why: 'back to his wait at the car\'s east end (cv-006)' },
];
type S = Record<string, unknown> & { op?: string; do: string; label?: string; when?: string; target?: string };
const plans = (process.argv.slice(2).length ? process.argv.slice(2) : ['loud', 'loud-fast', 'loud-fight', 'loud-left-downed', 'loud-left-standing']);
for (const id of plans) {
  const f = `${DIR}/${id}.plan.json`, old = JSON.parse(readFileSync(f, 'utf8')), steps: S[] = old.steps;
  if (steps.some(s => s.target === 'rivals')) { const head0 = Bun.spawnSync(['git', 'show', `HEAD:contracts/missions/handover/routes/plans/${id}.plan.json`], { cwd: '/Users/sky/Projects/suborned' }).stdout.toString(); steps.splice(0, steps.length, ...JSON.parse(head0).steps); } // a re-run starts from the committed (pre-0.19) plan
  const firstDraw = steps.findIndex(s => s.do === 'draw');
  const lastLock2 = steps.map((s, i) => [s, i] as const).filter(([s]) => s.do === 'fire' && s.target === 'security-1').at(-1)![1];
  const head = steps.slice(0, lastLock2 + 1).filter(s => !(s.op === 'enforcer' && s.label === 'out'));
  // a twin's own moves that were given once the response unit was down now wait for the crew driven back (the Scout and the Fixer are on
  // the flank until then), and replace the middle's walk back for that op
  const tail = steps.slice(lastLock2 + 1).map(s => s.do === 'move' && s.when === 'security.down' ? { ...s, when: 'rivals.retreat', after: [s.op === 'tech' ? 'tech' : `${s.op}-flank`] } : s);
  if (id === 'loud-fight') { // fight it out: the moves to the pavilion pier (lines on the café wall) became locks once Vale is in
    const i = tail.findIndex(s => s.op === 'enforcer' && s.do === 'board'), extra: S[] = [];
    for (const op of ['scout', 'fixer', 'tech']) extra.push(
      { op, do: 'fire', target: 'officer-1', after: ['in', op], when: 'board.in', label: `${op}-o1`, why: 'fight it out: Vale in, lock officer-1 at the café wall (the order walks him into his reach, inside theirs); once it is down he fires at will (stance free) on officer-2' });
    tail.splice(0, tail.length, ...tail.filter(s => !(s.do === 'move' && s.when === 'rivals.retreat' && s.op !== 'fixer')).slice(0, i + 1 - tail.slice(0, i).filter(s => s.do === 'move' && s.when === 'rivals.retreat' && s.op !== 'fixer').length), ...extra, ...tail.slice(i + 1));
  }
  const own = new Set(tail.filter(s => s.do === 'move' && s.when === 'rivals.retreat').map(s => s.op));
  const out = [...head, ...middle.filter(s => !(own.has(s.op) && s.when === 'rivals.retreat' && s.do === 'move' && s.op !== 'enforcer')), ...tail];
  if (head.length + 1 !== lastLock2 + 1 || firstDraw < 0) throw new Error(`${id}: the head is not loud's (no single walk to the car)`);
  const note = old.note.replace(/ Brief 027, engine 0\.19\.0 \(Waymark.*$/, '') + ' ' + RE;
  const body = ['{', ' "schema": "suborned.mission.plan/1",', ' "mission": "handover",', ` "note": ${JSON.stringify(note)},`, ` "start": ${JSON.stringify(old.start ?? {})},`, ` "until": ${old.until},`, ' "steps": [',
    ...out.map((s, i) => `  ${JSON.stringify(s).replace(/":/g, '": ').replace(/,"/g, ', "')}${i < out.length - 1 ? ',' : ''}`), ' ]', '}'];
  writeFileSync(f, body.join('\n') + '\n');
  console.log(`wrote ${id}.plan.json: ${out.length} steps (head ${head.length}, middle ${middle.length}, tail ${tail.length})`);
}
