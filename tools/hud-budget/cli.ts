// bun tools/hud-budget/cli.ts <shot.png | folder> [--state rest|active|alt] [--hud f.png | --world f.png | --rects f.json] [--manifest f.json] [--debug out.png] [--json]
// A folder is a batch: for each shot.png it looks for shot.hud.png, shot.world.png or shot.rects.json (the HUD mask) and shot.manifest.json (speech and beats, and the state).
// --rects may also be one file for a folder: { "<shot>.png": [rects] | { "state": "active", "rects": [rects] } }.
// Exit 1 if any shot fails: HUD above its coverage limit (rest 15%, active 22%, alt 30%), a line of running text that is not speech, a block of stacked text.
import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { basename, join } from 'node:path';
import { analyse, debugImage, maskOf, type Manifest, type Options, type Rect, type Report, type State } from './budget';
import { decodePng, encodePng } from './png';

const args = process.argv.slice(2), flag = (n: string) => { const i = args.indexOf(n); return i < 0 ? undefined : args[i + 1]; };
const target = args[0]?.startsWith('--') ? undefined : args[0];
if (!target) { console.error('usage: bun tools/hud-budget/cli.ts <shot.png | folder> [--state rest|active|alt] [--hud f] [--world f] [--rects f.json] [--manifest f.json] [--debug out.png] [--json]'); process.exit(2); }
const img = (f: string) => decodePng(readFileSync(f)), json = <T>(f: string) => JSON.parse(readFileSync(f, 'utf8')) as T;

function one(shot: string, base: string, force: Record<string, string | undefined>): { rep: Report; opts: Options } {
  const pick = (k: string, ext: string) => force[k] ?? (existsSync(base + ext) ? base + ext : undefined);
  const hud = pick('hud', '.hud.png'), world = pick('world', '.world.png'), rects = pick('rects', '.rects.json'), man = pick('manifest', '.manifest.json');
  const manifest = man ? json<Manifest>(man) : undefined;
  let rs: Rect[] | undefined, rstate: State | undefined;
  if (rects && !hud && !world) { let j = json<any>(rects); if (j && !Array.isArray(j) && !j.rects) j = j[basename(shot)]; if (!j) throw new Error(`${rects} has no entry for ${basename(shot)}`); rs = Array.isArray(j) ? j : j.rects; rstate = j.state; }
  const state = (force.state ?? manifest?.state ?? rstate ?? /\.(rest|active|alt)\b/.exec(basename(shot))?.[1] ?? 'rest') as State;
  const opts: Options = { state, manifest, hud: hud ? img(hud) : undefined, world: !hud && world ? img(world) : undefined, rects: rs };
  if (!opts.hud && !opts.world && !opts.rects) throw new Error(`${shot}: no HUD mask (give --hud, --world or --rects, or put ${basename(base)}.hud.png beside it)`);
  return { rep: analyse(img(shot), opts), opts };
}

const force = { hud: flag('--hud'), world: flag('--world'), rects: flag('--rects'), manifest: flag('--manifest'), state: flag('--state') };
const shots = statSync(target).isDirectory() ? readdirSync(target).filter(f => f.endsWith('.png') && !/\.(hud|world|debug)\.png$/.test(f)).sort().map(f => join(target, f)) : [target];
if (!shots.length) { console.error(`no .png screenshots in ${target}`); process.exit(2); }
let bad = 0; const all: Record<string, Report> = {};
for (const s of shots) {
  const { rep, opts } = one(s, s.replace(/\.png$/, ''), shots.length > 1 ? { state: force.state, rects: force.rects } : force); all[s] = rep; if (!rep.pass) bad++;
  if (!args.includes('--json')) {
    console.log(`${rep.pass ? 'PASS' : 'FAIL'}  ${basename(s).padEnd(34)} ${rep.state.padEnd(6)} HUD ${rep.coveragePct.toFixed(1).padStart(5)}% of the screen (limit ${rep.limitPct}%, footprint ${rep.footprintPct.toFixed(0)}%)  running text ${rep.sentences.length} line${rep.sentences.length === 1 ? '' : 's'}, ${rep.blocks.length} block${rep.blocks.length === 1 ? '' : 's'}; speech ${rep.speech.length}, beats ${rep.beats.length}  [${rep.mask} mask]`);
    for (const f of rep.failures) console.log(`      - ${f}`); for (const f of rep.warnings) console.log(`      ~ ${f}`);
  }
  const dbg = flag('--debug'); if (dbg && shots.length === 1) { const im = img(s); writeFileSync(dbg, encodePng(debugImage(im, rep, maskOf(im, opts).mask))); }
}
if (args.includes('--json')) console.log(JSON.stringify(all, null, 1));
else if (shots.length > 1) console.log(`${shots.length - bad}/${shots.length} within budget`);
process.exit(bad ? 1 : 0);
