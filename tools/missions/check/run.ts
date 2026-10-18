// The mission checker's runner (brief 011 B): load a mission's route scripts, play each one and its variations headless on Keel's
// engine, judge them (judge.ts), and record the check so it replays. A plug-in step for the generation pipeline (brief 003): the
// runner takes a mission folder, not the Handover by name, and `assessment()` (judge.ts) speaks the pipeline's Assessment.
// Recorded checks: runs/missions/<mission>/cNNN/ holds run.json (engine commit and source hash, the sha256 of every input, every
// play's digest, the report), the route manifest and scripts as played, and report.html. `replay` rebuilds the recorded engine,
// replays every play from the recorded scripts and says whether each lands on the same final hash; an input that changed since is
// drift, named.
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { Routes } from '../../../contracts/missions/routes.schema';
import { ROOT, engineAt, engineInputs, playAll, type EngineAt, type Script } from './engine';
import { digest, judge, clock, caseTrailEqOf, type Getaway, type Plays, type Report, type Slack } from './judge';
import { contactsOf, lateFrom, stepsOf, withoutContact } from './vary';

const sha = (s: string | Buffer) => createHash('sha256').update(s).digest('hex');
/** The longest slip a step is tried for (s). [S] */
export const SLACK_MAX_S = 120;
const TOOL = ['tools/missions/check/engine.ts', 'tools/missions/check/vary.ts', 'tools/missions/check/judge.ts', 'tools/missions/check/blocker.ts', 'tools/missions/check/run.ts', 'contracts/missions/routes.schema.ts'];

export type Loaded = { mission: string; manifest: Routes; texts: Record<string, string>; scripts: Record<string, Script> };
/** A mission's routes: `dir` holds routes.json and the scripts it names (contracts/missions/<id>/routes, or a recorded copy). */
export function load(dir: string): Loaded {
  const texts: Record<string, string> = { 'routes.json': readFileSync(join(dir, 'routes.json'), 'utf8') };
  const manifest = Routes.parse(JSON.parse(texts['routes.json']!));
  const scripts: Record<string, Script> = {};
  for (const s of manifest.scripts) {
    texts[s.file] ??= readFileSync(join(dir, s.file), 'utf8');
    const sc = JSON.parse(texts[s.file]!) as Script;
    if (sc.schema !== 'suborned.sim.script/1' || sc.mission !== manifest.mission) throw new Error(`${s.file}: not a suborned.sim.script/1 script for ${manifest.mission}`);
    scripts[s.id] = sc;
  }
  return { mission: manifest.mission, manifest, texts, scripts };
}

export type Check = { mission: string; engine: EngineAt; inputs: Record<string, string>; tool: string; scripts: Record<string, string>; plays: Plays; slack: Slack[]; report: Report };

/** Play the base scripts and the contact variations, then scan each primary quiet route's steps for slack. */
export async function check(L: Loaded, e: EngineAt = engineAt(), log: (m: string) => void = () => {}, o: { slack?: boolean } = {}): Promise<Check> {
  const m = L.manifest, roles = m.contacts.map(c => c.role), plays: Plays = {};
  const jobs: { name: string; script: Script }[] = [];
  for (const s of m.scripts.filter(x => !x.pending)) {
    jobs.push({ name: s.id, script: L.scripts[s.id]! });
    if (s.kind === 'quiet') for (const role of contactsOf(L.scripts[s.id]!, roles)) jobs.push({ name: `${s.id}~no-${role}`, script: withoutContact(L.scripts[s.id]!, role) });
  }
  log(`playing ${jobs.length} scripts and contact variations on the engine at ${e.rev === 'tree' ? 'the working tree' : e.commit.slice(0, 12)}`);
  const out = await playAll(jobs, e);
  jobs.forEach((j, i) => { plays[j.name] = digest(out[i]!, sha(JSON.stringify(j.script)), !j.name.includes('~')); });

  const slack: Slack[] = [];
  for (const s of m.scripts.filter(x => o.slack !== false && x.kind === 'quiet' && x.primary && !x.pending && plays[x.id]?.outcome === 'success')) {
    // the plan's steps, up to the one that secures Vale (the route's closing event reads it); the final fight after it is fought, not planned
    const sc = L.scripts[s.id]!, secured = s.closes ? plays[s.id]!.marks[s.closes.secured] : undefined, steps = stepsOf(sc).filter(t => secured === undefined || t <= secured);
    log(`late from each of ${s.id}'s ${steps.length} planned steps, 1 s at a time to ${SLACK_MAX_S} s`);
    const rows = await Promise.all(steps.map(async tick => {
      for (let d = 1; d <= SLACK_MAX_S; d++) {
        const v = lateFrom(sc, tick, d), name = `${s.id}~late@${tick}+${d}s`;
        const [p] = await playAll([{ name, script: v }], e, 1);
        plays[name] = digest(p!, sha(JSON.stringify(v)), false);
        if (p!.outcome !== 'success') return { tick, slackS: d - 1, breaksAs: p!.outcome === 'failure' ? p!.fail : p!.outcome };
      }
      return { tick, slackS: SLACK_MAX_S };
    }));
    slack.push({
      script: s.id, maxS: Math.min(...rows.map(r => r.slackS)),
      steps: rows.map(r => ({ ...r, orders: sc.orders.filter(o => o.tick === r.tick).map(o => `${o.op ?? ''} ${o.do}${o.target ? ` ${o.target}` : ''}`.trim()).join(', ') })),
    });
  }
  // what the engine read (at its commit), and the working tree's mission file the site was made from
  const inputs = { ...Object.fromEntries(engineInputs(e.dir).map(f => [f, sha(readFileSync(join(e.dir, f)))])), [`tree:contracts/missions/${L.mission}/mission.json`]: sha(readFileSync(join(ROOT, 'contracts/missions', L.mission, 'mission.json'))) };
  const tool = sha(TOOL.map(f => `${f}\n${readFileSync(join(ROOT, f), 'utf8')}`).join('\n'));
  return { mission: L.mission, engine: e, inputs, tool, scripts: Object.fromEntries(Object.entries(L.texts).map(([f, t]) => [f, sha(t)])), plays, slack, report: judge(L.manifest, L.scripts, plays, slack, { caseTrailEq: caseTrailEqAt(e.dir), getaway: getawayAt(e.dir, L.mission) }) };
}

// ---------------------------------------------------------------- record and replay

/** `case-trail-eq` from the rulebook the engine read (0 if it has none: a check recorded before rev 4). */
export const caseTrailEqAt = (dir: string) => { const f = join(dir, 'contracts/rules/rulebook.json'); return existsSync(f) ? caseTrailEqOf(JSON.parse(readFileSync(f, 'utf8'))) : 0; };

/** The getaway the engine read (brief 018 §1): the car, fight-hold-m, the site's exit lane and the final fight's posts; undefined for an
 *  engine without an exit lane (a check recorded before brief 018), which MC-11 then leaves out. */
export function getawayAt(dir: string, mission: string): Getaway | undefined {
  const sf = join(dir, 'sim/data', `${mission}.site.json`), rf = join(dir, 'contracts/rules/rulebook.json');
  if (!existsSync(sf) || !existsSync(rf)) return undefined;
  const site = JSON.parse(readFileSync(sf, 'utf8')), hold = JSON.parse(readFileSync(rf, 'utf8')).values?.['fight-hold-m']?.v, exit = site.getaway?.exit;
  if (!exit || typeof hold !== 'number') return undefined;
  const at = (id: string): [number, number] => { const p = site.places?.[id]?.at; if (!p) throw new Error(`${mission}.site.json: no place ${id} (unknown = reject)`); return p; };
  const posts = [...(site.fight?.ambush ?? []), ...(site.fight?.police ?? [])].map((id: string) => ({ id, at: at(id) }));
  return { car: at(site.getaway.car), holdCm: Math.round(hold * 100), lane: exit.polyline, laneHalfCm: Math.round(exit.w * 50), posts };
}

export const runsDir = (mission: string) => join(ROOT, 'runs/missions', mission);
const nextId = (mission: string) => { const d = runsDir(mission); const n = existsSync(d) ? readdirSync(d).filter(x => /^c\d{3}$/.test(x)).length : 0; return `c${String(n + 1).padStart(3, '0')}`; };

/** Write the check under runs/missions/<mission>/cNNN/: run.json, the routes as played, report.html. Returns the folder. */
export function record(c: Check, L: Loaded, note: string): string {
  const id = nextId(c.mission), dir = join(runsDir(c.mission), id);
  mkdirSync(join(dir, 'routes'), { recursive: true });
  for (const [f, t] of Object.entries(L.texts)) writeFileSync(join(dir, 'routes', f), t);
  const run = {
    schema: 'suborned.mission.check/1', id, mission: c.mission, note, verdict: c.report.ok ? 'SHIPS' : 'HOLDS',
    engine: { commit: c.engine.commit, sourceSha256: c.engine.hash, inputs: c.engine.inputs }, tool: c.tool, inputs: c.inputs, routes: c.scripts,
    conditions: c.report.conditions, slack: c.slack, plays: c.plays,
  };
  // one play per line: the record stays readable and diffable without growing a line per field
  const body = JSON.stringify({ ...run, plays: '@plays' }, null, 1).replace('"@plays"', `{\n${Object.entries(c.plays).map(([k, v]) => `  ${JSON.stringify(k)}: ${JSON.stringify(v)}`).join(',\n')}\n }`);
  writeFileSync(join(dir, 'run.json'), body + '\n');
  writeFileSync(join(dir, 'report.html'), html(c, id));
  return dir;
}

/** The script a recorded play name stands for: `<script>`, `<script>~no-<role>`, `<script>~late@<tick>+<s>s` (vary.ts). */
export function scriptOf(L: Loaded, name: string): Script {
  const [id, v] = name.split('~') as [string, string | undefined], base = L.scripts[id];
  if (!base) throw new Error(`recorded play ${name}: no script ${id}`);
  if (!v) return base;
  const no = v.match(/^no-([a-z-]+)$/), late = v.match(/^late@(\d+)\+(\d+)s$/);
  if (no) return withoutContact(base, no[1]!);
  if (late) return lateFrom(base, Number(late[1]), Number(late[2]));
  throw new Error(`recorded play ${name}: unknown variation ${v}`);
}

/** Replay a recorded check: the same engine commit, the recorded scripts, every recorded play rebuilt from its name and compared
 *  by its final hash and outcome; then the verdict judged again from the replayed plays (a tool changed since is drift, named). */
export async function replay(dir: string, log: (m: string) => void = () => {}): Promise<{ ok: boolean; lines: string[] }> {
  const run = JSON.parse(readFileSync(join(dir, 'run.json'), 'utf8')), lines: string[] = [];
  const L = load(join(dir, 'routes'));
  const e = run.engine.commit === 'tree' ? engineAt('tree') : engineAt(run.engine.commit, run.engine.inputs ?? 'commit');
  for (const [f, h] of Object.entries(run.inputs as Record<string, string>)) {
    const tree = f.startsWith('tree:'), path = join(tree ? ROOT : e.dir, f.replace(/^tree:/, ''));
    const now = existsSync(path) ? sha(readFileSync(path)) : 'missing';
    lines.push(now === h ? `ok    input ${f}` : `${tree ? 'drift' : 'FAIL '} input ${f} changed since the check (${h.slice(0, 12)} → ${now.slice(0, 12)})`);
  }
  lines.push(e.hash === run.engine.sourceSha256 ? `ok    engine ${String(run.engine.commit).slice(0, 12)} source ${e.hash.slice(0, 12)}` : `drift engine source ${e.hash.slice(0, 12)}, recorded ${String(run.engine.sourceSha256).slice(0, 12)}`);
  const rec = run.plays as Plays, names = Object.keys(rec).sort();
  log(`replaying ${names.length} recorded plays on the engine at ${String(run.engine.commit).slice(0, 12)}`);
  const jobs = names.map(name => ({ name, script: scriptOf(L, name) })), out = await playAll(jobs, e), now: Plays = {};
  let same = 0;
  jobs.forEach((j, i) => {
    const a = rec[j.name]!, b = digest(out[i]!, sha(JSON.stringify(j.script)), !j.name.includes('~'));
    now[j.name] = b;
    if (a.finalHash === b.finalHash && a.outcome === b.outcome && a.fail === b.fail && a.tick === b.tick) same++;
    else lines.push(`FAIL  play ${j.name}: recorded ${a.outcome} ${a.fail} ${clock(a.tick)} ${a.finalHash}, now ${b.outcome} ${b.fail} ${clock(b.tick)} ${b.finalHash}`);
  });
  lines.push(`${same === names.length ? 'ok   ' : 'FAIL '} ${same}/${names.length} plays land on their recorded final hash`);
  const tool = sha(TOOL.map(f => `${f}\n${readFileSync(join(ROOT, f), 'utf8')}`).join('\n'));
  const verdictNow = judge(L.manifest, L.scripts, now, run.slack as Slack[], { caseTrailEq: caseTrailEqAt(e.dir), getaway: getawayAt(e.dir, L.mission) }).ok ? 'SHIPS' : 'HOLDS';
  if (verdictNow === run.verdict) lines.push(`ok    verdict ${run.verdict}${tool === run.tool ? '' : ' (judged by the current checker; its code changed since the check)'}`);
  else lines.push(`${tool === run.tool ? 'FAIL ' : 'drift'} verdict ${verdictNow} by the current checker, recorded ${run.verdict}${tool === run.tool ? '' : ' (the checker\'s code changed since the check)'}`);
  return { ok: !lines.some(l => l.startsWith('FAIL')), lines };
}

// ---------------------------------------------------------------- the report page

const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
export function html(c: Check, id: string): string {
  const r = c.report, td = (x: unknown) => `<td>${esc(String(x))}</td>`;
  const cond = r.conditions.map(x => `<tr class="${x.ok ? 'ok' : 'bad'}">${td(x.id)}${td(x.ok ? 'ok' : 'FAIL')}${td(x.rule)}${td(x.value)}<td class="d">${esc(x.detail).replace(/; /g, '<br>')}</td></tr>`).join('');
  const rows = r.scripts.map(x => `<tr class="${x.ok ? 'ok' : 'bad'}">${[x.id, x.kind, x.route ?? '', x.expects, x.got, x.at, x.trail, x.alert, x.shots, x.fight ? `${x.fight}, ${x.fightShots} shots` : '', `$${x.cash.toLocaleString('en-US')}`, x.replay ? 'identical' : 'DIFFERS'].map(td).join('')}</tr>`).join('');
  const slack = r.slack.map(s => `<h3>${esc(s.script)}: every planned step can slip ${s.maxS} s</h3><table><tr><th>step</th><th>orders</th><th>slack</th><th>then</th></tr>${s.steps.map(x => `<tr>${td(clock(x.tick))}${td(x.orders)}${td(`${x.slackS} s`)}${td(x.breaksAs ?? '')}</tr>`).join('')}</table>`).join('');
  return `<!doctype html><meta charset="utf-8"><title>Mission check ${esc(c.mission)} ${id}</title>
<style>body{font:14px/1.4 -apple-system,sans-serif;margin:24px;max-width:1200px}table{border-collapse:collapse;margin:8px 0 20px}td,th{border:1px solid #ccc;padding:4px 8px;text-align:left;vertical-align:top}tr.bad td{background:#fde8e8}tr.ok td:nth-child(2){color:#17663a}.d{font-size:12px}code{font-size:12px}</style>
<h1>Mission check: ${esc(c.mission)} · ${id} · ${r.ok ? 'SHIPS' : 'HOLDS'}</h1>
<p>GDD §09 mission checker (brief 011 B). Engine <code>${c.engine.commit.slice(0, 12)}</code> (source ${c.engine.hash.slice(0, 12)}); ${Object.keys(c.plays).length} plays. Replay: <code>bun run missions:check replay ${esc(c.mission)} ${id}</code>.</p>
<h2>Conditions</h2><table><tr><th>id</th><th></th><th>rule</th><th>value</th><th>detail</th></tr>${cond}</table>
<h2>Scripts</h2><table><tr><th>script</th><th>kind</th><th>route</th><th>expects</th><th>got</th><th>at</th><th>T</th><th>A</th><th>shots before the fight</th><th>final fight</th><th>cash</th><th>replay</th></tr>${rows}</table>
<p>Pending (not played): ${esc(r.pending.join(', ') || 'none')}</p>
<h2>How late each step can be</h2>${slack}
<h2>Inputs</h2><table>${Object.entries(c.inputs).map(([f, h]) => `<tr>${td(f)}<td><code>${h}</code></td></tr>`).join('')}</table>`;
}
