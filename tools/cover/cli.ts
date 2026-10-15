// usage: bun run cover [--check] [--out <file>]
// Writes contracts/world/civic-block.cover.json (the cover points the fight AI reads), runs the cover rules (CV-1, CV-2) and prints where a fight lacks cover and what would give it.
// --check: write nothing; fail if the committed file is not what the inputs make today. Exit 1 on any failed rule.
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { readFileSync } from 'node:fs';
import { checkCover, hashes } from './check';
import { buildCover, COVER_FILE, gridOf } from './gen';
import { findings } from './report';

const root = join(import.meta.dir, '..', '..'), argv = process.argv.slice(2), opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
const { cover, json } = buildCover();
const file = resolve(root, opt('--out') ?? COVER_FILE);
if (argv.includes('--check')) {
  const have = await readFile(file, 'utf8').catch(() => '');
  if (have !== json) { console.error(`${file} is not what the plan, block, kit and site make today: bun run cover`); process.exit(1); }
} else { await mkdir(dirname(file), { recursive: true }); await writeFile(file, json); }
const read = (f: string) => readFileSync(join(root, f), 'utf8'), plan = JSON.parse(read('contracts/world/harbour-district.plan.json')), block = JSON.parse(read(plan.civic.block)), siteText = read('sim/data/handover.site.json'), site = JSON.parse(siteText);
const grid = gridOf(site);
const checks = checkCover(cover, { free: (x, y) => grid.free(Math.floor((x - grid.x0) / grid.cell), Math.floor((y - grid.y0) / grid.cell)), placeAt: id => { const p = site.places[id]; const a = p?.stand ?? p?.at; return a ? [a[0], a[1]] : undefined; }, inputs: hashes({ block: read(plan.civic.block), site: siteText, kit: read('contracts/world/kit.json'), plan: read('contracts/world/harbour-district.plan.json') }) });
for (const c of checks) console.log(`${c.ok ? 'ok  ' : 'FAIL'} ${c.id}  ${c.rule}: ${c.value}${c.ok ? '' : `\n       ${(c.detail ?? '').split('; ').join('\n       ')}`}`);
console.log(`${cover.points.length} cover points → ${file}`);
const fs = findings(cover, plan, block, site);
if (fs.length) {
  console.log('\nposts with no cover that shields them from the car:');
  for (const f of fs) console.log(`  ${f.area} ${f.post} at (${f.at[0]}, ${f.at[1]}) on ${f.on}: nearest shielding point ${f.nearest ? `${f.nearest.id} (${f.nearest.of}) ${f.nearest.distM} m away` : 'none'}; ${f.proposal ? `a ${f.proposal.piece} at (${f.proposal.at.join(', ')}) yaw ${f.proposal.yawDeg}°, ${f.proposal.reason}${f.proposal.thenShielded ? '' : ' (does not shield it)'}` : 'no free spot for a planter run within 4 m'}`);
}
process.exit(checks.every(c => c.ok) ? 0 : 1);
