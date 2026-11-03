// Brief 060 tier 2 (Latch): the bot lane's plan, its findings and its runner, without a player (a stand-in app script plays the part).
import { expect, test } from 'bun:test';
import { chmodSync, existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { CHECK_META, findingsOf, nightSeed, planRows, playerHasBot, runBotLane, type BotReportRow } from './bot';
import { IssueBook } from './issues';
import { replayCommand } from './report';

const file = { schema: 'suborned.bot/1', rows: [{ name: 'random-a', policy: 'random', seed: 1 }, { name: 'greedy-a', policy: 'greedy', seed: 2, minutes: 3 }] };

test('rows: the seed moves with the night, the same night gives the same seeds, --fixed keeps them, the caps come through', () => {
  const a = planRows(file, '20261025'), b = planRows(file, '20261025'), c = planRows(file, '20261026'), f = planRows(file, '20261025', { fixed: true });
  expect(a).toEqual(b); expect(a[0]!.seed).not.toBe(c[0]!.seed); expect(f.map(r => r.seed)).toEqual([1, 2]);
  expect(a.map(r => r.seconds)).toEqual([660, 180]); expect(planRows(file, 'x', { rows: 1, minutes: 2 })).toEqual([{ ...planRows(file, 'x')[0]!, seconds: 120 }]);
  expect(nightSeed('20261025', 1)).toBe(nightSeed('20261025', 1));
  expect(() => planRows({ ...file, rows: [{ name: 'x', policy: 'chaotic' }] }, 'n')).toThrow(); // an unknown policy is refused
});

const row = (over: Partial<BotReportRow> = {}): BotReportRow => ({
  name: 'random-a', policy: 'random', seed: 7, status: 'ok', preset: 'hold', seconds: 600, endTick: 5100, outcome: 'failure', failCode: 'sealed', cardHead: 'x',
  input: { decisions: 200, clicks: 120, rings: 110, ringMisses: 10, taken: 100, moves: 40, runs: 8, pans: 30, turns: 6 },
  orders: { given: 140, refused: 3, codes: { 'agent-busy': 3 } }, frames: { count: 36000, meanMs: 4, p99Ms: 6, maxMs: 40, over50: 0 },
  memory: { startMB: 900, endMB: 920, slopeMBPerMin: 1 }, consoleErrors: 0, feetSamples: 1000, figures: 60, shots: 2, counts: { 'Q-GIVER': 1, 'Q-FEET': 4 },
  anomalies: [
    { check: 'Q-GIVER', key: 'ring', title: 'A downed or unable agent offered as the doer', expected: 'no giver who is down', observed: 'ring:scout down: offered for Carry', tick: 2870, clock: '4:47', at: [1, 2], shot: 'random-a/anomalies/001-Q-GIVER-2870.jpg', script: 'random-a/anomalies/001-Q-GIVER-2870.script.json' },
    { check: 'Q-FEET', key: 'crowd/41', title: 'Feet off the floor: crowd/41', expected: 'feet within 15 cm', observed: 'feet 0.4 m', tick: 3010, clock: '5:01', at: [3, 4], shot: '', script: 'random-a/anomalies/002-Q-FEET-3010.script.json' },
  ], ...over,
});

test('findings: one per kept anomaly, digits folded out of the key, severity and owner by check, shot and replay absolute', () => {
  const fs = findingsOf(row(), 'random-a', '/qa/run/bot');
  expect(fs.map(f => f.key)).toEqual(['Q-GIVER:ring', 'Q-FEET:crowd/#']);
  expect(fs[0]!.severity).toBe(CHECK_META['Q-GIVER']!.severity); expect(fs[0]!.shot).toBe('/qa/run/bot/random-a/anomalies/001-Q-GIVER-2870.jpg');
  expect(fs[0]!.observed).toContain('seed 7'); expect(fs[1]!.shot).toBeUndefined(); expect(fs[1]!.script).toContain('002-Q-FEET');
  expect(findingsOf(undefined, 'greedy-a', '/b')[0]!.key).toBe('Q-BOT:not-run');
  expect(findingsOf(row({ status: 'failed', error: 'no host', anomalies: [] }), 'random-a', '/b').map(f => f.key)).toEqual(['Q-BOT:failed']);
});

test('the issue book keeps the bot shot and script on the exemplar; the replay runs on the build engine', () => {
  const book = new IssueBook(3), fs = findingsOf(row(), 'random-a', '/qa');
  const it = { id: 'random-a', seed: { id: 'bot random seed 7', kind: 'route' as const, file: 'x', script: { schema: 'suborned.sim.script/1' as const, mission: 'handover' as const, start: {}, orders: [] } }, mutations: [], script: { schema: 'suborned.sim.script/1' as const, mission: 'handover' as const, start: {}, orders: [] } };
  book.add('bot', it, fs);
  const giver = book.ranked({ bot: 1 }).find(i => i.check === 'Q-GIVER')!;
  expect(giver.exemplars[0]!.shot).toContain('001-Q-GIVER'); expect(giver.exemplars[0]!.script).toContain('.script.json');
  expect(replayCommand('a.script.json', 'bot')).toContain('--engine build');
});

test('runner: one player per row with -bot, reports merged; it stops starting rows when Sky plays; a row that never reports is a Q-BOT finding', async () => {
  const root = mkdtempSync(join(tmpdir(), 'qa-bot-')), app = join(root, 'The Suborned.app'), bin = join(app, 'Contents/MacOS');
  mkdirSync(bin, { recursive: true }); writeFileSync(join(root, 'build.json'), JSON.stringify({ version: '0.2.999' }));
  // a stand-in player: reads -bot <plan> <dir> and writes a report row for the plan's row
  writeFileSync(join(bin, 'The Suborned'), `#!/bin/sh\nplan="$2"; dir="$3"\nname=$(sed -n 's/.*"name": "\\([^"]*\\)".*/\\1/p' "$plan" | head -1)\nmkdir -p "$dir"\nprintf '{"schema":"suborned.bot.report/1","status":"ok","screen":"1920x1080","build":"x","thresholds":{},"rows":[{"name":"%s","policy":"random","seed":1,"status":"ok","preset":"hold","seconds":1,"endTick":1,"outcome":"success","failCode":"","cardHead":"","input":{"decisions":0,"clicks":0,"rings":0,"ringMisses":0,"taken":0,"moves":0,"runs":0,"pans":0,"turns":0},"orders":{"given":0,"refused":0,"codes":{}},"frames":{"count":0,"meanMs":0,"p99Ms":0,"maxMs":0,"over50":0},"memory":{"startMB":0,"endMB":0,"slopeMBPerMin":0},"consoleErrors":0,"feetSamples":0,"figures":0,"shots":0,"counts":{},"anomalies":[]}]}' "$name" > "$dir/bot.json"\n`);
  chmodSync(join(bin, 'The Suborned'), 0o755);
  const out = join(root, 'out'), say = () => {};
  const r = await runBotLane(out, { app, say, playing: () => [], rows: 2, night: '20261025', hasBot: () => true });
  expect(r.ran).toEqual(['random-a', 'greedy-a']); expect(r.report?.rows.map(x => x.name)).toEqual(['random-a', 'greedy-a']); expect(r.build).toBe('0.2.999');
  const plan = JSON.parse(readFileSync(join(out, 'random-a.plan.json'), 'utf8')); expect(plan.schema).toBe('suborned.bot.plan/1'); expect(plan.rows[0].seconds).toBe(660);
  expect(existsSync(join(out, 'bot.json'))).toBe(true);
  let n = 0; const r2 = await runBotLane(join(root, 'out2'), { app, say, playing: () => (n++ >= 1 ? [{ pid: 1 }] : []), rows: 2, hasBot: () => true });
  expect(r2.ran).toEqual(['random-a']); expect(r2.stoppedForSky).toBe(true);
});

test('a player built before -bot (or none at all) skips the lane with a note: no row starts, no finding', async () => {
  const root = mkdtempSync(join(tmpdir(), 'qa-bot-old-')), app = join(root, 'The Suborned.app'), managed = join(app, 'Contents/Resources/Data/Managed');
  mkdirSync(join(app, 'Contents/MacOS'), { recursive: true }); mkdirSync(managed, { recursive: true }); writeFileSync(join(app, 'Contents/MacOS/The Suborned'), '#!/bin/sh\nexit 9\n'); chmodSync(join(app, 'Contents/MacOS/The Suborned'), 0o755);
  writeFileSync(join(managed, 'Suborned.Runtime.dll'), Buffer.from('suborned.late.report/1', 'utf16le'));
  expect(playerHasBot(app)).toBe(false);
  const notes: string[] = [], r = await runBotLane(join(root, 'out'), { app, say: m => notes.push(m), playing: () => [] });
  expect(r.skipped).toContain('has no -bot'); expect(r.ran).toEqual([]); expect(notes.join(' ')).toContain('skipped');
  writeFileSync(join(managed, 'Suborned.Runtime.dll'), Buffer.from('x suborned.bot.report/1 y', 'utf16le')); expect(playerHasBot(app)).toBe(true);
  expect((await runBotLane(join(root, 'o2'), { app: join(root, 'none.app'), say: () => {}, playing: () => [] })).skipped).toContain('no built player');
});
