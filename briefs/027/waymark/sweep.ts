// scratch: sweep the loud player's waits and lock rules on the pinned engine; prints wins that leave nobody behind
const E = '/Users/sky/Projects/suborned/out/waymark027/e18', BASE = process.env.BASE ?? '/Users/sky/Projects/suborned/contracts/missions/handover/routes/loud.json';
const site = JSON.parse(await Bun.file('/Users/sky/Projects/suborned/sim/data/handover.site.json').text());
const ids = (process.env.PTS ?? 'cv-001,cv-002,cv-003,cv-004,cv-005,cv-006,cv-093,cv-094,cv-095,cv-096,cv-097,cv-098,cv-099,cv-100,cv-077,cv-082,cv-088,cv-087').split(',');
const at = (id: string) => { if (id.includes(',')) return id; const p = site.cover.points.find((x: any) => x.id === id); return `${p.at[0]},${p.at[1]}`; };
const rules = (process.env.RULES ?? 'near,threat,focus,blocker').split(','), enfs = (process.env.ENFS ?? 'go').split(',');
const extra = Object.fromEntries((process.env.EXTRA ?? '').split(' ').filter(Boolean).map(x => x.split('=') as [string, string]));
type J = { env: Record<string, string>; key: string };
const jobs: J[] = [];
for (const s of ids) for (const f of ids) for (const t of ids) {
  if (s === f || s === t || f === t) continue;
  for (const r of rules) for (const en of enfs) jobs.push({ key: `S=${s} F=${f} T=${t} ${r} ${en}`, env: { S: at(s), F: at(f), T: at(t), RELOCK: r, ENF: en, ...extra } });
}
console.error(`${jobs.length} plays`);
let next = 0; const out: string[] = [];
await Promise.all(Array.from({ length: 16 }, async () => {
  while (next < jobs.length) {
    const j = jobs[next++]!;
    const p = Bun.spawn(['dotnet', '../player/bin/player.dll', BASE], { cwd: E, env: { ...process.env, DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec', ...j.env }, stdout: 'pipe' });
    const r = (await new Response(p.stdout).text()).trim(); out.push(`${r} | ${j.key}`);
  }
}));
const wins = out.filter(l => l.startsWith('Success') && l.includes('down[]'));
console.log(`${wins.length} clean wins of ${out.length}; successes ${out.filter(l => l.startsWith('Success')).length}`);
for (const l of wins.sort((a, b) => Number(a.match(/hp-lost (\d+)/)![1]) - Number(b.match(/hp-lost (\d+)/)![1])).slice(0, 40)) console.log(l);
await Bun.write(process.env.OUT ?? '/Users/sky/Projects/suborned/out/waymark027/sweep.txt', out.join('\n') + '\n');
