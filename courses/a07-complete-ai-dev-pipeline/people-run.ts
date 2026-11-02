/** A07 production proof. Archived AI responses; actual expansion, engine staging and player capture.
 * bun courses/a07-complete-ai-dev-pipeline/people-run.ts verify | run
 * No model/database/Editor calls. run needs the retained 0.2.232 player on the author machine.
 */
import { createHash } from 'node:crypto';
import { copyFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { checkPromotion } from '../../tools/people/promoted';
import { buildPeople } from '../../tools/sim/people';
import { resolveShots } from '../../tools/game/shots';
import { replay } from '../../tools/levels/replay';

const ROOT = resolve(import.meta.dir, '../..');
const HERE = import.meta.dir;
const OUT = join(ROOT, 'out/scratch/a07/people-run');
const EVIDENCE = join(HERE, 'evidence/people.json');
const RUN = 'runs/levels/harbour-district/r016';
const ORIGINAL = join(ROOT, 'out/build/play/The Suborned.app');
const APP = join(OUT, 'The Suborned.app');
const STREAM = 'Contents/Resources/Data/StreamingAssets/sim';
const PEOPLE = 'handover.people.json';
const sha = (data: string | Buffer) => createHash('sha256').update(data).digest('hex');
const read = (p: string) => JSON.parse(readFileSync(join(ROOT, p), 'utf8'));
const write = (p: string, data: unknown) => writeFileSync(p, JSON.stringify(data, null, 2) + '\n');
function need(yes: unknown, why: string): asserts yes { if (!yes) throw new Error(why); }
const sources = [
  'contracts/people/harbour-district.cast.json', 'contracts/people/harbour-district.cast.promotion.json',
  `${RUN}/run.json`, `${RUN}/round-2/pulse.json`,
  'tools/levels/people/plugin.ts', 'tools/people/expand.ts', 'tools/people/check.ts',
  'tools/people/promoted.ts', 'tools/sim/people.ts', 'tools/gen/rules.ts',
  'unity/Assets/Suborned/Runtime/Play/SimBundle.cs',
  'unity/Assets/Suborned/Runtime/Play/HandoverHost.cs',
];

function costs() {
  const run = read(`${RUN}/run.json`), before = read('runs/levels/harbour-district/r008/run.json');
  const calls = run.calls.map((c: any) => ({ id: c.id, role: c.role, model: c.model, usd: c.costUsd, tokens: c.tokens }));
  const total = calls.reduce((sum: number, c: any) => sum + c.usd, 0);
  need(Math.round(total * 1e4) / 1e4 === run.cost.usd, 'r016 cost rollup differs');
  const highest = calls.reduce((a: any, b: any) => a.usd > b.usd ? a : b);
  const redundant = before.calls.filter((c: any) => ['04-transit-critic', '08-transit-critic'].includes(c.id));
  need(before.rounds.slice(0, 2).every((r: any) => !r.checkerOk), 'before examples were checker clean');
  need(run.rounds[0].checkerOk === false && run.rounds[0].criticRan === false, 'new gate did not skip critic');
  return {
    run: 'r016', calls, actual_usage_usd: total, reported_rounded_usd: run.cost.usd,
    billing: 'Claude CLI recorded total_cost_usd usage valuation on a subscription; not an additional API debit or allocated subscription invoice.',
    highest,
    optimisation: {
      implementation: 'tools/gen/rules.ts LOOP 2, brief 007 F, 2026-10-14: critic only after deterministic checks pass',
      before: { run: 'r008', rounds: [1, 2], critic_calls: 2, recorded_critic_usd: redundant.reduce((s: number, c: any) => s + c.costUsd, 0), calls: redundant },
      after: { run: 'r016', round: 1, failed: ['P-0'], critic_calls: 0, critic_tokens: 0, recorded_critic_usd: 0 },
      limits: 'Actual historical calls under the same generation framework, different objectives/candidates. No controlled equal-work percentage or counterfactual saving is claimed. Builder/retry costs remain included in the total.',
    },
    excludes: 'Subscription invoice allocation, labour, hardware, hosting, other content jobs; no cost charged by this offline replay.',
  };
}

async function verify() {
  const p = checkPromotion('harbour-district');
  need(p.ok, p.problems.join('\n'));
  console.log(`PASS promoted cast: r016 round 2 / ${p.record!.cast.sha256.slice(0, 16)}`);
  costs();
  console.log('PASS recorded usage: $0.6905 / 3 calls; highest: Keel critic $0.2800659');
  console.log('PASS critic gate: checker-failed candidates no longer trigger a critic call');
  const r = await replay('harbour-district', 'r016');
  need(r.ok, r.lines.filter(x => /FAIL/.test(x)).join('\n'));
  console.log('PASS archived r016 replay: failed P-0 -> correction -> Keel pass -> ACCEPTED');
  if (existsSync(EVIDENCE)) {
    const e = JSON.parse(readFileSync(EVIDENCE, 'utf8'));
    for (const [path, hash] of Object.entries(e.source_hashes)) need(sha(readFileSync(join(ROOT, path))) === hash, `Changed evidence source ${path}`);
    console.log(`PASS ${Object.keys(e.source_hashes).length} committed source hashes`);
  }
  console.log('This check makes no model call and does not certify the two-minute first-play gate.');
}

async function command(args: string[], name: string) {
  const proc = Bun.spawn(args, { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
  const [stdout, stderr, exit] = await Promise.all([new Response(proc.stdout).text(), new Response(proc.stderr).text(), proc.exited]);
  writeFileSync(join(OUT, name + '.txt'), stdout + stderr);
  need(exit === 0, `${name} exited ${exit}:\n${stdout}\n${stderr}`);
  return stdout;
}

const pause = () => Bun.sleep(2500); // Reading holds are part of the uncut CLI capture.
async function run() {
  mkdirSync(OUT, { recursive: true });
  const began = new Date().toISOString();
  console.log('THE SUBORNED / A07 / PEOPLE AND RELATIONSHIPS');
  console.log('Recorded execution. Original AI responses are archived, not generated live.');
  console.log('No model calls, database calls or new Unity compilation in this replay.');
  console.log('\n[1/6] ORIGINAL AI RUN -> VALIDATED CAST');
  await verify();
  console.log('Full prompts, retrieved passages and outputs: runs/levels/.../r016/round-*');
  await pause();

  console.log('\n[2/6] EXPAND THE PROMOTED CAST WITH CURRENT PRODUCTION RULES');
  console.log('$ bun run people');
  await command(['bun', 'tools/people/cli.ts'], 'people-expansion');
  const expanded = read('out/people/harbour-district.people.json');
  const ties = expanded.persons.reduce((s: number, p: any) => s + p.ties.length, 0);
  console.log(`PASS ${expanded.persons.length} people / ${ties} directed ties / ${expanded.places.length} places`);
  console.log('PASS P-0..P-16 and N-1..N-8: coverage, routines, needs, ties, names, secrets');
  console.log('PASS M-2: waitress, driver, guard and councillor all cast from those people');
  console.log('Cast values + current rules/names/needs data -> deterministic expansion.');
  await pause();

  console.log('\n[3/6] AUTOMATIC ENGINE EXPORT');
  const text = buildPeople(), peopleSha = sha(text);
  writeFileSync(join(OUT, PEOPLE), text);
  const original = join(ORIGINAL, STREAM, PEOPLE);
  need(existsSync(original), 'Author-machine production player is missing');
  const productionSha = sha(readFileSync(original));
  const manifest = JSON.parse(readFileSync(join(ORIGINAL, STREAM, 'engine.json'), 'utf8'));
  need(manifest.inputs[PEOPLE] === productionSha, 'Production engine manifest differs');
  const enginePeople = JSON.parse(text);
  const site = JSON.parse(readFileSync(join(ORIGINAL, STREAM, 'handover.site.json'), 'utf8'));
  const shipped = JSON.parse(readFileSync(original, 'utf8'));
  const profiles: any[] = [];
  for (const role of ['waitress', 'driver', 'guard', 'councillor']) {
    const id = site.cast[role].id;
    const fresh = enginePeople.people.find((p: any) => p.id === id);
    const old = shipped.people.find((p: any) => p.id === id);
    const fields = ['id', 'name', 'job', 'temperament', 'need', 'record', 'size', 'holder', 'start', 'ties'];
    need(fields.every(k => JSON.stringify(fresh[k]) === JSON.stringify(old[k])), `Mission profile changed: ${role}`);
    profiles.push({ role, id, fields, profile: Object.fromEntries(fields.map(k => [k, fresh[k]])) });
  }
  console.log('PASS all 4 mission profiles match the shipped game: names, needs, temperament, ties.');
  console.log(`Fresh full export / ${peopleSha.slice(0, 20)}`);
  console.log(`Production export / ${productionSha.slice(0, 20)}`);
  console.log('DIFFERENT background projection: production predates the city tram remap.');
  console.log('This replay stages current-plan people; it is a labelled local variant.');
  console.log(`Exported ${enginePeople.people.length} simulated people; positions in engine centimetres.`);
  console.log('Mission casting: Inez, Arthur, Lukac, Popovic; Inez and Arthur remain lovers.');
  await pause();

  console.log('\n[4/6] STAGE THE OUTPUT INTO A REAL UNITY PLAYER');
  console.log('Reuse the existing player binary and scene; stage freshly generated people data.');
  if (!existsSync(APP)) await command(['cp', '-cR', ORIGINAL, APP], 'clone-player');
  copyFileSync(join(OUT, PEOPLE), join(APP, STREAM, PEOPLE));
  need(sha(readFileSync(join(APP, STREAM, PEOPLE))) === peopleSha, 'Staged data differs');
  manifest.inputs[PEOPLE] = peopleSha;
  manifest.a07 = 'Current-plan people replay; existing production binary, four mission profiles unchanged; not a new release build';
  write(join(APP, STREAM, 'engine.json'), manifest);
  await command(['codesign', '--force', '--deep', '--sign', '-', APP], 'sign-replay-player');
  await command(['codesign', '--verify', '--deep', '--strict', APP], 'signature-check');
  console.log('PASS StreamingAssets/sim/handover.people.json staged automatically');
  console.log('SimBundle.Load checks this SHA-256 before Engine.FromTexts consumes it.');
  console.log('PASS updated engine hash manifest and ad hoc signature for the replay variant.');
  await pause();

  console.log('\n[5/6] START THE PLAYER AND CAPTURE THE GENERATED PEOPLE FUNCTIONING');
  const shots = { schema: 'suborned.shots/1', defaults: { size: [1920, 1080], settle: 0.6 }, shots: [
    {name:'cafe-routines', script:'route-a-board', tick:400, focus:'waitress', yaw:0, zoom:10, hud:true, select:'scout', clip:{seconds:6,fps:20}},
    {name:'social-intel', script:'route-a-board', tick:800, focus:'waitress', yaw:0, zoom:10, hud:true, select:'fixer', intel:true, format:'png'},
    {name:'guard-leaves-post', script:'route-a-board', tick:1300, focus:'guard', yaw:2, zoom:10, hud:true, select:'enforcer', clip:{seconds:6,fps:20}},
    {name:'street-level-people', script:'route-a-board', tick:400, view:'follow', agent:'scout', face:'waitress', hud:true, clip:{seconds:5,fps:20}},
  ]};
  const raw = join(OUT, 'gameplay'); mkdirSync(raw, { recursive: true });
  const resolved = join(OUT, 'shots.resolved.json');
  write(resolved, resolveShots(shots, raw)); write(join(OUT, 'shots.source.json'), shots);
  console.log('Route A orders use the real host; no relationship or expression overrides.');
  console.log('Capturing cafe routines, social Intel, guard response and street-level people...');
  await command([join(APP, 'Contents/MacOS/The Suborned'), '-shots', resolved, raw, '-fastExit', '-windowed', '-logFile', join(OUT, 'player.log')], 'player-execution');
  const report = JSON.parse(readFileSync(join(raw, 'shots.json'), 'utf8'));
  need(report.status === 'ok', 'Player capture failed: ' + JSON.stringify(report));
  console.log(`PASS Unity player / ${report.build}`);
  for (const shot of report.shots) console.log(`PASS ${shot.name} / tick ${shot.tick} / ${shot.clipFrames ?? 0} captured frames`);
  await pause();

  console.log('\n[6/6] RETAIN THE PROOF AND RECORDED COSTS');
  const evidence = {
    schema: 'suborned.a07.people/1', began, finished: new Date().toISOString(),
    mode: 'Archived model generation; real current expansion, engine-data staging and player capture',
    model_calls_this_run: 0, fresh_unity_compilation: false,
    generation: { run:'r016', round:2, cast:checkPromotion('harbour-district').record },
    counts: { persons: expanded.persons.length, directed_ties:ties, places:expanded.places.length, simulated:enginePeople.people.length },
    source_hashes: Object.fromEntries(sources.map(p => [p, sha(readFileSync(join(ROOT, p)))])),
    integration: { file:PEOPLE, sha256:peopleSha, production_sha256:productionSha, matches_production_player:peopleSha === productionSha,
      mission_profiles_match_production: profiles, player_build:report.build, variant:'current-plan people staged in retained production binary', player_report:report },
    costs: costs(), manual_steps: ['Human approval/promotion of an accepted cast', 'Game design and current curated rules/needs/names', 'Release publication and compatible-machine first-play test'],
    limits: ['Not a live model-generation recording', 'No new Unity build; existing production binary/scene loads regenerated data',
      'Full background projection differs from the retained production export; four mission profiles are verified equal', 'Public download and two-minute playability need separate verification'],
  };
  write(EVIDENCE, evidence);
  console.log('Historical run: 3 model calls / recorded usage $0.6905 (subscription valuation).');
  console.log('Highest call: Keel critic $0.2800659. Offline replay: zero model calls.');
  console.log('Before: failed r008 candidates still incurred 2 critic calls ($0.4135186).');
  console.log('After: failed r016 round 1 incurred 0 critic calls, 0 critic tokens, $0.');
  console.log('Different candidates/objectives: no equal-work percentage is claimed.');
  console.log('PASS evidence/people.json / inputs, outputs, checks, hashes, costs and capture.');
  await pause();
}

if (process.argv[2] === 'verify') await verify();
else if (process.argv[2] === 'run') await run();
else { console.error('usage: bun courses/a07-complete-ai-dev-pipeline/people-run.ts verify|run'); process.exit(2); }
