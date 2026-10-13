//: estimate pedestrians/m2 per street at the mission time of day, from life.json rules (same constants as rules.ts)
import life from '../../contracts/world/harbour-district.life.json';
import { streetDensity } from './model';
import { K, CAT, PROM } from './rules';
const J: any = life;
if (J.pedestrians.spillDepth !== K.spillDepth || J.pedestrians.feederShare !== K.feederShare || J.pedestrians.category.food.spillRho !== CAT.food.spillRho || J.pedestrians.promenade.mall.peak !== PROM.mall.peak) throw new Error('life.json out of date: run bun run life');
const h0 = 17.0, h1 = 17 + 12 / 60;
const rows = streetDensity((h0 + h1) / 2); // one band, so draw is constant across the window
const busy = (r: any) => r.cls === 'district' || r.cls === 'mall' || r.cls === 'walk';
const f = (n: number, d = 3) => n.toFixed(d);
console.log(`mission window 17:00-17:12, band peak. density = people per m2 of foot ground; corr = over the whole corridor`);
console.log('street'.padEnd(18), 'class'.padEnd(9), 'len'.padStart(5), 'foot'.padStart(5), 'spill walk  thru  prom', ' dens   corr  people');
for (const r of rows) { const p = r.spill + r.walk + r.through + r.prom; console.log(r.id.padEnd(18), r.cls.padEnd(9), String(Math.round(r.len)).padStart(5), String(r.foot).padStart(5), f(r.spill, 2).padStart(5), f(r.walk, 2).padStart(4), f(r.through, 2).padStart(5), f(r.prom, 2).padStart(5), f(p / r.foot).padStart(6), f(p / r.corridor).padStart(6), String(Math.round(p * r.len)).padStart(6)); }
const b = rows.filter(busy), len = b.reduce((s, r) => s + r.len, 0);
const wd = b.reduce((s, r) => s + (r.spill + r.walk + r.through + r.prom) * r.len, 0) / b.reduce((s, r) => s + r.foot * r.len, 0);
console.log(`\nbusy streets (district + mall + walk): length-weighted density ${f(wd)} people/m2 over ${Math.round(len)} m; target 0.12`);
for (const r of b) console.log(`  ${r.id}: ${f((r.spill + r.walk + r.through + r.prom) / r.foot)} ${((r.spill + r.walk + r.through + r.prom) / r.foot >= 0.1) ? 'reaches' : 'below'} ~0.12`);
// share that comes from derived demand rather than a top-up: none needed where dens >= 0.12; top-up elsewhere
const top = b.map(r => ({ id: r.id, topUp: Math.max(0, 0.12 - (r.spill + r.walk + r.through + r.prom) / r.foot) * r.foot * r.len }));
console.log('background top-up to 0.12 (people):', top.filter(t => t.topUp > 1).map(t => `${t.id} ${Math.round(t.topUp)}`).join(', ') || 'none');
// lively vs quiet: same streets at other times
console.log('\nLantern Lane / Boulevard / Dock Road density by time (foot basis):');
for (const h of [3, 7, 10, 14, 17.1, 21, 23.5]) { const rs = streetDensity(h); const g = (id: string) => { const r = rs.find(x => x.id === id)!; return f((r.spill + r.walk + r.through + r.prom) / r.foot); };
  console.log(`  ${String(h).padStart(5)}h  lantern-lane ${g('lantern-lane')}  boulevard ${g('boulevard')}  dock-road ${g('dock-road')}  crown-parade ${g('crown-parade')}`); }
