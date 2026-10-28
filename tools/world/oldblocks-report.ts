// Brief 075, the old frontage port (tools/world/gen/oldblocks.ts): what it built and what fell back, per plot, with the lengths the grammar wants (Datum's input).
// Run: bun tools/world/oldblocks-report.ts [--json <file>]  (prints the table; --json writes the log and counts, e.g. briefs/075/old/oldblocks.json)
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { civic } from '../plan/civic';
import { derive } from '../plan/plan';
import { worldOf } from './build';
import { oldLogOf } from './gen/oldblocks';
import { ctxOf } from './gen/common';

const root = resolve(import.meta.dir, '../..'), read = (p: string) => JSON.parse(readFileSync(resolve(root, p), 'utf8'));
const plan = read('contracts/world/harbour-district.plan.json'), d = derive(plan), civ = civic(plan.civic, read(plan.civic.block));
const { world: w } = worldOf(plan, d, civ);
const log = oldLogOf(ctxOf(plan, d, civ))!;
const one = (s: number[]) => s.every(x => Math.abs(x - 1) < 1e-6);
const oldIds = new Set(w.buildings.filter(b => (b as any).id && log.plots.some(p => p.fit === 'blocks' && b.id.startsWith(`bld/${p.plot}/`))).map(b => b.id.split('/').slice(0, 3).join('/')));
const street = w.facades.filter(f => f.form === 'bay' || f.form === 'end' || f.form === 'corner');
const all = { facades: w.facades.length, trueSize: w.facades.filter(f => one(f.kit.scale)).length, scaled: w.facades.filter(f => !one(f.kit.scale)).length,
  streetPieces: street.length, streetTrue: street.filter(f => one(f.kit.scale)).length, streetScaled: street.filter(f => !one(f.kit.scale)).length };
const out = { counts: log.counts, classes: log.classes, export: all, oldBuildings: oldIds.size, plots: log.plots };
for (const p of log.plots) console.log(`${p.fit.padEnd(8)} ${p.plot.padEnd(20)} ${p.why ?? ''}${p.blocks ? ` blocks ${p.blocks}` : ''}${p.buildings ? ` buildings ${p.buildings}` : ''}${p.crescentBays ? ` crescent bays ${p.crescentBays[0]} of ${p.crescentBays[1]}` : ''}`);
console.log('counts', JSON.stringify(log.counts), 'classes', JSON.stringify(log.classes));
console.log('export facades', JSON.stringify(all));
const j = process.argv.indexOf('--json');
if (j > 0) { const f = resolve(root, process.argv[j + 1]!); mkdirSync(dirname(f), { recursive: true }); writeFileSync(f, JSON.stringify(out, null, 1) + '\n'); console.log('→', f); }
