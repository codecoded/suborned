// usage: bun run missions:check [check] <mission> [--record "<note>"] [--engine HEAD|tree|<commit>]
//        bun run missions:check replay <mission> <cNNN>
// check: plays the mission's route scripts (contracts/missions/<mission>/routes) and their variations headless, prints the GDD §09
// conditions, writes out/missions/<mission>/check.{json,html}; --record also writes runs/missions/<mission>/cNNN/. Exit 0 only if
// the mission ships (every condition holds). replay: replays a recorded check; exit 0 only if every play lands on its recorded hash.
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, engineAt, hasDotnet } from './engine';
import { check, html, load, record, replay, runsDir } from './run';

const args = process.argv.slice(2), flag = (k: string) => { const i = args.indexOf(k); return i >= 0 ? args.splice(i, 2)[1] : undefined; };
const note = flag('--record'), rev = flag('--engine') ?? 'HEAD';
const cmd = args[0] === 'replay' || args[0] === 'check' ? args.shift()! : 'check';
if (!hasDotnet()) { console.error('the mission checker needs the .NET SDK (dotnet on PATH; DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec on the Studio)'); process.exit(2); }
const log = (m: string) => console.error(`· ${m}`);

if (cmd === 'replay') {
  const [mission, id] = args;
  if (!mission || !id) { console.error('usage: replay <mission> <cNNN>'); process.exit(2); }
  const r = await replay(join(runsDir(mission), id), log);
  for (const l of r.lines) console.log(l);
  console.log(r.ok ? `REPLAY OK ${mission} ${id}` : `REPLAY FAILED ${mission} ${id}`);
  process.exit(r.ok ? 0 : 1);
}

const mission = args[0] ?? 'handover', L = load(join(ROOT, 'contracts/missions', mission, 'routes'));
const t0 = performance.now(), c = await check(L, engineAt(rev), log);
const out = join(ROOT, 'out/missions', mission);
mkdirSync(out, { recursive: true });
writeFileSync(join(out, 'check.json'), JSON.stringify({ ...c, engine: { ...c.engine, dir: undefined } }, null, 1) + '\n');
writeFileSync(join(out, 'check.html'), html(c, 'unrecorded'));
for (const x of c.report.conditions) console.log(`${x.ok ? 'ok  ' : 'FAIL'}  ${x.id}  ${x.rule}: ${x.value}\n        ${x.detail.replace(/; /g, '\n        ')}`);
for (const s of c.slack) console.log(`slack ${s.script}: every step can slip ${s.maxS} s (${s.steps.map(x => `${Math.floor(x.tick / 600)}:${String(Math.floor(x.tick / 10) % 60).padStart(2, '0')} ${x.slackS} s${x.breaksAs ? ` → ${x.breaksAs}` : ''}`).join(', ')})`);
console.log(`${c.report.ok ? 'SHIPS' : 'HOLDS'} ${mission}: ${Object.keys(c.plays).length} plays on engine ${c.engine.commit.slice(0, 12)} in ${((performance.now() - t0) / 1000).toFixed(1)} s; report out/missions/${mission}/check.html`);
if (note) console.log(`recorded ${record(c, L, note).replace(ROOT + '/', '')}`);
process.exit(c.report.ok ? 0 : 1);
