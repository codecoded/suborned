// reads digest rows and prints Motive's bands (imports the repo's real bands.ts lines)
import { summarize, bands, rigidFromRows } from '../../../tools/missions/check/bands';
for (const f of process.argv.slice(2)) {
  const rows = (await Bun.file(f).text()).trim().split('\n').map(l => JSON.parse(l)).filter((r: any) => !r.error);
  const names = [...new Set(rows.map((r: any) => r.plan))] as string[];
  const s = names.map(n => summarize(rows.filter((r: any) => r.plan === n), 3));
  console.log('# ' + f, rows.length, 'replayIdentical all:', rows.every((r: any) => r.replayIdentical !== false));
  for (const x of s) console.log(`${x.plan.padEnd(16)} win ${x.winPct}% loudwins ${Math.round(x.winsLoud*1000/x.plays)/10}% fight ${x.fightP25}/${x.fightMed}/${x.fightP75}/p95 ${x.fightP95} hp ${x.hpMed} downPerWin ${x.downPerWin} T ${x.tMed} stand ${x.standingMed} fails ${JSON.stringify(x.fails)}`);
  for (const l of [...rigidFromRows(rows, []), ...bands(s, rows, 3)]) if (!l.ok) console.log(`  MISS ${l.id}: ${l.rule}: ${l.value}`);
}
