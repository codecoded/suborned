// Idle study (brief 012, Motive): busy and idle intervals per operative for every played Handover route.
//   bun tools/missions/idle/idle.ts [script.json …] [--json]   (default: every script in contracts/missions/handover/routes/)
// Builds and runs the sampler (Program.cs: steps the engine one tick at a time and reads Game.ActivityOf, which is pure), checks the
// stepped run ends on the same hash as Engine.Play, then reports per operative: walking, acting, tethered (a held loop, carrying Vale),
// fighting (drawing or weapon out in the final fight), idle; idle %, the longest idle stretch and what ended it (the order that
// followed, and the events at that tick: what the operative was waiting for). 10 ticks a second.
import { $ } from "bun";
import { readdirSync } from "node:fs";
import { join, resolve } from "node:path";

const root = resolve(import.meta.dir, "../../..");
const routesDir = join(root, "contracts/missions/handover/routes");
const args = process.argv.slice(2);
const asJson = args.includes("--json");
let scripts = args.filter((a) => !a.startsWith("--")).map((a) => resolve(a));
if (scripts.length === 0) scripts = readdirSync(routesDir).filter((f) => f.endsWith(".json") && f !== "routes.json").sort().map((f) => join(routesDir, f));

type Seg = [number, number, string, string, string];
type Run = { id: string; outcome: string; fail: string; end: number; hashMatch: boolean; segments: Record<string, Seg[]>; log: [number, string, string][]; orders: [number, string, string, string][] };

await $`dotnet build ${join(import.meta.dir, "idle.csproj")} -c Release --nologo -v q`.quiet();
const out = await $`dotnet ${join(import.meta.dir, "bin/Release/net10.0/suborned-idle.dll")} ${scripts}`.cwd(root).text();
const runs: Run[] = out.trim().split("\n").map((l) => JSON.parse(l));

const OPS = ["scout", "fixer", "tech", "enforcer"] as const;
const bucket = (p: string) => (p === "walking" ? "walking" : p === "acting" ? "acting" : p === "holding" ? "tethered" : p === "drawing" || p === "armed" || p === "firing" ? "fighting" : p === "down" ? "down" : "idle");
const clock = (t: number) => { const s = Math.floor(t / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`; };
const pct = (a: number, b: number) => (b === 0 ? 0 : Math.round((a * 1000) / b) / 10);
const WAIT_IGNORE = /^(order\.|run\.seen|crowd\.|shot\.|fight\.hit|witness\.)/;

function why(r: Run, op: string, from: number, to: number): string {
  if (to >= r.end) return "nothing (idle to the end)";
  const next = r.orders.find((o) => o[0] === to && (o[1] === op || o[1] === "cell"));
  const events = r.log.filter((l) => l[0] === to && !WAIT_IGNORE.test(l[1])).map((l) => l[1]);
  const what = next ? `${next[2]}${next[3] ? " " + next[3] : ""}` : "(no order: the fight drew it)";
  return `${what} after ${events.length ? events.join(", ") : "a clock time (no event)"}`;
}

type OpReport = { op: string; walking: number; acting: number; tethered: number; fighting: number; idle: number; idlePct: number; idleBeforeFightPct: number; longest: { from: string; to: string; s: number; waitingFor: string } | null; idles: { from: string; to: string; s: number; waitingFor: string }[] };
const reports = runs.map((r) => {
  const fightAt = r.log.find((l) => l[1] === "fight.safe" || l[1] === "fight.loud" || l[1].startsWith("fight.police"))?.[0] ?? r.end;
  const ops: OpReport[] = OPS.map((op) => {
    const t: Record<string, number> = { walking: 0, acting: 0, tethered: 0, fighting: 0, idle: 0, down: 0 };
    const idles: OpReport["idles"] = []; let idleBefore = 0;
    for (const [a, b, phase] of r.segments[op]) {
      const k = bucket(phase); t[k] += b - a;
      if (k === "idle") {
        idleBefore += Math.max(0, Math.min(b, fightAt) - a);
        idles.push({ from: clock(a), to: clock(b), s: (b - a) / 10, waitingFor: why(r, op, a, b) });
      }
    }
    const longest = idles.reduce<OpReport["idles"][number] | null>((m, x) => (m && m.s >= x.s ? m : x), null);
    return { op, walking: t.walking / 10, acting: t.acting / 10, tethered: t.tethered / 10, fighting: t.fighting / 10, idle: t.idle / 10, idlePct: pct(t.idle, r.end), idleBeforeFightPct: pct(idleBefore, fightAt), longest, idles };
  });
  // dead air: ticks before the final fight when no operative walks, acts or fights (all idle, or a loop held with nobody else doing anything)
  const live = new Uint8Array(r.end);
  for (const op of OPS) for (const [a, b, phase] of r.segments[op]) { const k = bucket(phase); if (k === "walking" || k === "acting" || k === "fighting") for (let t = a; t < Math.min(b, r.end); t++) live[t]++; }
  // one-thread time: ticks before the fight when at most one operative walks, acts or fights (the rest wait on them or on the clock)
  let solo = 0; for (let t = 0; t < fightAt; t++) if (live[t] <= 1) solo++;
  const dead: { from: string; to: string; s: number; endedBy: string }[] = []; let deadTicks = 0;
  for (let t = 0; t < fightAt; ) { if (live[t]) { t++; continue; } let u = t; while (u < fightAt && !live[u]) u++; deadTicks += u - t; if (u - t >= 50) dead.push({ from: clock(t), to: clock(u), s: (u - t) / 10, endedBy: r.log.filter((l) => l[0] === u && !WAIT_IGNORE.test(l[1])).map((l) => l[1]).join(", ") || (r.orders.find((o) => o[0] === u) ? "an order on a clock time" : "-") }); t = u; }
  const played = r.outcome === "Success";
  // decision gaps: the player's view. Order ticks before the fight (any operative), the longest stretch with no order, and orders a minute
  const ticks = [...new Set(r.orders.map((o) => o[0]).filter((t) => t < fightAt))].sort((a, b) => a - b);
  let gap = { from: 0, to: 0 }; for (let k = 1; k <= ticks.length; k++) { const a = ticks[k - 1], b = k < ticks.length ? ticks[k] : fightAt; if (b - a > gap.to - gap.from) gap = { from: a, to: b }; }
  const decisions = { orderTicks: ticks.length, perMinute: Math.round((ticks.length * 600 / Math.max(1, fightAt)) * 10) / 10, longestGap: `${clock(gap.from)}-${clock(gap.to)} (${(gap.to - gap.from) / 10} s)` };
  return { id: r.id, played, outcome: r.outcome + (r.fail ? ` (${r.fail})` : ""), end: clock(r.end), fightAt: fightAt < r.end ? clock(fightAt) : "-", hashMatch: r.hashMatch, oneThreadPctPreFight: pct(solo, fightAt), decisions, deadAir: { s: deadTicks / 10, pctPreFight: pct(deadTicks, fightAt), stretches: dead }, ops };
});

if (asJson) { console.log(JSON.stringify(reports, null, 1)); process.exit(reports.every((r) => r.hashMatch) ? 0 : 1); }

const pad = (s: string | number, n: number) => String(s).padEnd(n);
for (const r of reports) {
  console.log(`\n${r.id}  ${r.outcome} at ${r.end}  final fight ${r.fightAt}  replay hash ${r.hashMatch ? "matches" : "DIFFERS"}`);
  console.log(`  decisions before the fight: ${r.decisions.orderTicks} order moments, ${r.decisions.perMinute} a minute; longest stretch with no order: ${r.decisions.longestGap}`);
  console.log(`  one-thread time before the fight (≤ 1 operative walks, acts or fights): ${r.oneThreadPctPreFight} %`);
  console.log(`  dead air before the fight (nobody walks, acts or fights): ${r.deadAir.s} s = ${r.deadAir.pctPreFight} %` + (r.deadAir.stretches.length ? "; stretches ≥ 5 s: " + r.deadAir.stretches.map((d) => `${d.from}-${d.to} (${d.s} s, ended by ${d.endedBy})`).join("; ") : ""));
  console.log(`  ${pad("op", 9)}${pad("walk s", 8)}${pad("act s", 8)}${pad("teth s", 8)}${pad("fight s", 9)}${pad("idle s", 8)}${pad("idle %", 8)}${pad("idle % pre-fight", 18)}longest idle`);
  for (const o of r.ops) console.log(`  ${pad(o.op, 9)}${pad(o.walking, 8)}${pad(o.acting, 8)}${pad(o.tethered, 8)}${pad(o.fighting, 9)}${pad(o.idle, 8)}${pad(o.idlePct, 8)}${pad(o.idleBeforeFightPct, 18)}${o.longest ? `${o.longest.from}-${o.longest.to} (${o.longest.s} s): ${o.longest.waitingFor}` : "-"}`);
}
const playedRuns = reports.filter((r) => r.played);
const sum = (f: (o: OpReport) => number, op: string) => { const xs = playedRuns.map((r) => f(r.ops.find((o) => o.op === op)!)); return Math.round((xs.reduce((a, b) => a + b, 0) / xs.length) * 10) / 10; };
console.log(`\nmean over the ${playedRuns.length} winning routes (${playedRuns.map((r) => r.id).join(", ")}); the fail scripts are minimal fixtures, not play:\n  ` + OPS.map((op) => `${op} idle ${sum((o) => o.idlePct, op)} % (pre-fight ${sum((o) => o.idleBeforeFightPct, op)} %)`).join(" · ") + ` · one-thread ${Math.round(playedRuns.reduce((a, r) => a + r.oneThreadPctPreFight, 0) / playedRuns.length * 10) / 10} % · dead air ${Math.round(playedRuns.reduce((a, r) => a + r.deadAir.pctPreFight, 0) / playedRuns.length * 10) / 10} % of pre-fight time`);
process.exit(reports.every((r) => r.hashMatch) ? 0 : 1);
