// Brief 027 calculator (Motive): today's rules against the proposal (rev. 2, Sky 2026-10-20: fights read as damage and health, not time).
// What the player sees is pips (the chance), damage per hit and the target's health bar. The seconds printed here are INTERNAL TUNING
// ONLY (cadence, fight length targets for Suture): they never reach the chip or the price.
// Same counting rule as the engine (GameCombat.Shoot): an accumulator starts at acc.start (500), each shot adds the accuracy, at 1000
// or more the shot hits and 1000 comes off. No dice.
// Run: bun briefs/027/ttk.ts
type Gun = { name: string; dmg: (d: number) => number; every: number; eff: number; max: number; charge?: number };

function shots(acc: number, health: number, dmg: number): { shots: number; hits: number } {
  if (acc <= 0) return { shots: Infinity, hits: Infinity };
  const need = Math.ceil(health / dmg);
  let a = 500, hits = 0, n = 0;
  while (hits < need && n < 1000) { n++; a += acc; if (a >= 1000) { a -= 1000; hits++; } }
  return { shots: n, hits };
}
const f1 = (x: number) => (x === Infinity ? "never" : x.toFixed(1));

// ---------------------------------------------------------------- today (rulebook values, Rules.Accuracy; health 2–6, pistol 1, carbine 2)
const tPistol = { dmg: 1, every: 1.5, eff: 15, max: 25 }, tCarbine = { dmg: 2, every: 2.0, eff: 20, max: 30 };
function accToday(g: { eff: number; max: number }, d: number, o: Cond) {
  if (d > g.max) return 0;
  let a = d <= 5 ? 1000 : d <= g.eff ? 750 : 400;
  if (o.moving) a = Math.floor((a * 500) / 1000);
  if (o.cover && d > 5) a = Math.floor((a * 500) / 1000);
  if (o.untrained) a = Math.floor((a * 750) / 1000);
  return a;
}

// ---------------------------------------------------------------- the proposal: health ×10, damage per weapon, four pips
type Cond = { moving?: boolean; cover?: boolean; untrained?: boolean; high?: boolean };
const pipAcc = [0, 250, 500, 750, 1000];
const G: Record<string, Gun> = {
  sidearm: { name: "sidearm", dmg: () => 10, every: 0.6, eff: 12, max: 20 },
  machine: { name: "machine pistol", dmg: () => 6, every: 0.25, eff: 10, max: 16, charge: 4 },
  carbine: { name: "carbine", dmg: () => 20, every: 0.8, eff: 20, max: 30, charge: 8 },
  riot: { name: "riot gun", dmg: (d) => (d <= 5 ? 30 : 10), every: 1.0, eff: 6, max: 10 },
  rifle: { name: "long rifle", dmg: () => 25, every: 2.0, eff: 35, max: 50 },
};
function pips(g: Gun, d: number, o: Cond) {
  if (d > g.max) return 0;
  let s = d <= 5 ? 4 : d <= g.eff ? 3 : 2;
  if (d > 5) { if (o.untrained) s--; if (o.cover) s--; }
  if (o.moving) s--;
  if (o.high) s++;
  return Math.max(1, Math.min(4, s));
}

const rows: [string, number, number, Cond][] = [
  // label, distance m, target health today (×10 in the proposal), conditions
  ["rival in the open, 12 m", 12, 3, {}],
  ["rival in the open, 12 m, shooter untrained", 12, 3, { untrained: true }],
  ["rival beside cover, 15 m, untrained", 15, 3, { cover: true, untrained: true }],
  ["rival beside cover, 15 m, untrained, from the deck", 15, 3, { cover: true, untrained: true, high: true }],
  ["officer at 18 m, untrained", 18, 3, { untrained: true }],
  ["officer at 18 m, trained (Enforcer)", 18, 3, {}],
  ["op (4) at 12 m, by a trained officer", 12, 4, {}],
  ["Enforcer (6) at 12 m, by a trained officer", 12, 6, {}],
  ["rival at 4 m (point blank)", 4, 3, {}],
];

console.log("TODAY: chance ‰ · hits to down · [internal s]");
for (const [label, d, hp, o] of rows) {
  const ap = accToday(tPistol, d, o), ac = accToday(tCarbine, d, o);
  const sp = shots(ap, hp, 1), sc = shots(ac, hp, 2);
  console.log(`  ${label}: pistol ${ap} ‰ · ${sp.hits} hits · [${f1(1 + (sp.shots - 1) * 1.5)} s] | carbine ${ac} ‰ · ${sc.hits} hits · [${f1(1 + (sc.shots - 1) * 2)} s]`);
}

console.log("\nPROPOSAL: pips · damage per hit · hits to down (what the chip shows) · [internal s, first shot 0.5 s]");
for (const [label, d, hp0, o] of rows) {
  const hp = hp0 * 10;
  const cells = Object.values(G).map((g) => {
    const p = pips(g, d, o); if (p === 0) return `${g.name} out of range`;
    const dm = g.dmg(d), s = shots(pipAcc[p], hp, dm);
    return `${g.name} ${"●".repeat(p)}${"○".repeat(4 - p)} −${dm} ×${s.hits} [${f1(0.5 + (s.shots - 1) * g.every)}]`;
  });
  console.log(`  ${label} (health ${hp}):\n    ${cells.join("\n    ")}`);
}

// Option (a), the charge: how long a weapon that draws charge can keep firing from a full cell of 100, and how many downs that is.
console.log("\nOPTION (a) CHARGE, 100 per agent, +10 a second while not firing a charged weapon [internal]");
for (const g of [G.machine, G.carbine]) {
  const n = Math.floor(100 / g.charge!);
  const s = shots(pipAcc[3], 30, g.dmg(12));
  console.log(`  ${g.name}: ${n} shots on a full cell [${f1(n * g.every)} s of fire]; a rival at 3 pips takes ${s.shots} shots, so a full cell downs ${Math.floor(n / s.shots)}`);
}
console.log("  field: 50 charge, absorbs up to 30 damage for 3 s (one rival's down's worth), no firing while it holds");
