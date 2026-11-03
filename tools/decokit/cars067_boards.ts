// bun tools/decokit/cars067_boards.ts [--scratch out/scratch/vela/067/final] [--out briefs/067/vela] [--quick] [--boards play30,play210,street,marks,close,eye,shape,two,hero]
// Brief 067 (Vela): the pictures for the rival sedan, start to finish: Blender renders the transparent plates (cars067_plates.py: the four cars, the 1.75 m person, the stand-in two ways), then
// cars067_compose.py lays the boards. Needs the piece built (`bun run decokit --only Sedan_Rival`), the studio HDRIs in out/decokit/HDRI and Blender (BLENDER, default /Applications/Blender.app).
// Nothing heavy runs while Sky plays (tools/game/playing.ts): it waits, like `bun run guard`. --quick renders a quarter of the samples (a look, not the boards).
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { waitForSky, timeoutText } from '../game/playing';

const repo = resolve(import.meta.dir, '../..'), argv = process.argv.slice(2), opt = (k: string, d: string) => { const i = argv.indexOf(k); return i >= 0 ? argv[i + 1]! : d; };
const scratch = resolve(repo, opt('--scratch', 'out/scratch/vela/067/final')), out = resolve(repo, opt('--out', 'briefs/067/vela')), q = argv.includes('--quick') ? 4 : 1;
const blender = process.env.BLENDER ?? '/Applications/Blender.app/Contents/MacOS/Blender', plates = resolve(import.meta.dir, 'cars067_plates.py');
const CARS = 'Sedan_Official,Sedan_Getaway,StandIn,Sedan_Rival';
const closeArgs = ['--close-ppm', '140', '--close-w', '760', '--close-h', '520'];
// one entry per plate set: [folder, args]. samples are for the full boards.
const SETS: [string, string[]][] = [
  ['play', ['--ids', `${CARS},Person`, '--yaws', '30,210', '--headings', '315,225,45,135,0', '--views', 'play', '--samples', '96']],        // true scale, 38.57 px per metre
  ['insitu', ['--ids', 'StandIn,Sedan_Rival', '--yaws', '30', '--headings', '30', '--views', 'play', '--samples', '128']],                // laid into the game's frame
  ['close', ['--ids', CARS, '--yaws', '30', '--headings', '315,135', '--views', 'close', '--samples', '128', ...closeArgs]],              // the fitted play angle
  ['tint', ['--ids', 'StandInTint', '--yaws', '30', '--headings', '315,135', '--views', 'close', '--samples', '128', ...closeArgs]],     // the stand-in as its comment means
  ['eye', ['--ids', CARS, '--views', 'eye,top,side', '--samples', '128', '--close-ppm', '120', '--close-w', '760', '--close-h', '420', '--eye-dist', '6.6', '--eye-h', '1.5', '--lens', '38']],
  ['hero', ['--ids', 'Sedan_Rival', '--yaws', '30', '--headings', '315,135', '--views', 'close', '--samples', '192', '--close-ppm', '215', '--close-w', '1100', '--close-h', '1000']],
];

const w = await waitForSky('cars067_boards');
if (w.timedOut) { console.error(timeoutText(w)); process.exit(3); }
mkdirSync(out, { recursive: true });
for (const [dir, args] of SETS) {
  const a = args.map((v, i) => args[i - 1] === '--samples' ? String(Math.max(8, Math.round(Number(v) / q))) : v);
  const r = Bun.spawnSync([blender, '-b', '-t', '1', '-P', plates, '--', '--out', resolve(scratch, dir), ...a], { stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode) { console.error(`cars067_boards: Blender failed on ${dir}\n${r.stderr.toString().slice(-600)}`); process.exit(1); }
  console.log(`plates ${dir}: ${/\[C67\] (\d+) plates/.exec(r.stdout.toString())?.[1] ?? '?'}`);
}
const c = Bun.spawnSync(['python3', '-I', resolve(import.meta.dir, 'cars067_compose.py'), '--plates', scratch, '--out', out, ...(argv.includes('--boards') ? ['--boards', opt('--boards', '')] : [])], { stdout: 'inherit', stderr: 'inherit' });
process.exit(c.exitCode ?? 1);
