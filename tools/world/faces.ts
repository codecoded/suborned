// bun run world:faces [--world out/world/<id>.world.json] [--kit contracts/world/kit.json] [--plan plan.json] [--list N]
// Brief 072-C, "no flat face at play zoom": exits 1 if any building wall face in the export has no kit (tools/world/rules/faces-cover.ts). Prints the modules
// placed per piece (play streets and the whole district), faces and undressed faces per area, and each undressed face with the generator's reason
// (out/world/<id>.faces.json, written by `bun run world`: the pieces that would dress it, or why none fits).
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Kit } from '../../contracts/world/kit.schema';
import type { World } from '../../contracts/world/world.schema';
import type { Plan } from '../plan/plan';
import { faceCoverage } from './rules/faces-cover';
import type { FaceLog } from './gen/faces';

const root = resolve(import.meta.dir, '../..'), args = process.argv.slice(2), opt = (k: string) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
const plan: Plan = JSON.parse(readFileSync(resolve(root, opt('--plan') ?? 'contracts/world/harbour-district.plan.json'), 'utf8'));
const worldFile = resolve(root, opt('--world') ?? `out/world/${plan.id}.world.json`), logFile = worldFile.replace(/\.world\.json$/, '.faces.json');
if (!existsSync(worldFile)) { console.error(`world:faces: no export at ${worldFile} (bun run world)`); process.exit(1); }
const w: World = JSON.parse(readFileSync(worldFile, 'utf8')), kit: Kit = JSON.parse(readFileSync(resolve(root, opt('--kit') ?? 'contracts/world/kit.json'), 'utf8'));
const byId = new Map(kit.assets.map(a => [a.id, a])), caps = (plan.standards.buildings as any).caps ?? {};
const plotArea = new Map(plan.plots.map((p: any) => [p.id, p.area as string])), siteArea = new Map(((plan.standards.buildings as any).towers?.sites ?? []).map((s: any) => [`tower/${s.id}`, plotArea.get(s.plot) ?? '?']));
const civicArea = plan.plots.find((p: any) => p.civic === 'square')?.area ?? 'records';
const areaOf = (b: string) => (b.startsWith('bld/') ? plotArea.get(b.split('/')[1]!) ?? '?' : b.startsWith('tower/') ? (siteArea.get(b) as string) ?? '?' : civicArea);
const play = (a: string) => !!caps[a]?.play;
const cov = faceCoverage(w, id => byId.get(id), areaOf, play);
const log: FaceLog[] = existsSync(logFile) ? JSON.parse(readFileSync(logFile, 'utf8')).log : [];
const why = new Map<string, FaceLog[]>(); for (const l of log) if (l.why) (why.get(l.building) ?? why.set(l.building, []).get(l.building)!).push(l);

const fmt = (r: Record<string, number>) => Object.entries(r).sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0])).map(([k, v]) => `${k} ${v}`).join(', ') || 'none';
const FACE_PIECE = /^(SideBay|SideBayStack|CornerPier|AlleyWall|SidePilaster|ShopFill|ShopStack)_/;
const sum = (r: Record<string, number>, f: (k: string) => boolean) => Object.entries(r).filter(([k]) => f(k)).reduce((s, [, v]) => s + v, 0);
console.log(`faces: ${cov.faces} wall faces, ${cov.metres.toFixed(0)} m; dressed ${cov.dressedMetres.toFixed(0)} m (${((100 * cov.dressedMetres) / Math.max(1, cov.metres)).toFixed(1)}%); undressed faces ${cov.undressed.length} (play streets ${cov.undressed.filter(g => g.play).length})`);
console.log(`modules, play streets: every-face ${sum(cov.perPiece.play, k => FACE_PIECE.test(k))}, all ${sum(cov.perPiece.play, () => true)}: ${fmt(cov.perPiece.play)}`);
console.log(`modules, whole district: every-face ${sum(cov.perPiece.all, k => FACE_PIECE.test(k))}, all ${sum(cov.perPiece.all, () => true)}: ${fmt(cov.perPiece.all)}`);
console.log('per area (faces / undressed, m / undressed m, modules): ' + Object.entries(cov.perArea).sort((a, b) => a[0].localeCompare(b[0])).map(([a, r]) => `${a}${play(a) ? '*' : ''} ${r.faces}/${r.undressed}, ${r.metres.toFixed(0)}/${r.undressedMetres.toFixed(0)} m, ${r.modules}`).join(' · ') + '  (* play street)');
const blocked: Record<string, number> = {}, reasons: Record<string, number> = {};
for (const l of log) if (l.why) { for (const b of l.blocked ?? []) blocked[b] = (blocked[b] ?? 0) + 1; if (!l.blocked?.length) reasons[l.why.replace(/[\d.]+ m/g, 'N m')] = (reasons[l.why.replace(/[\d.]+ m/g, 'N m')] ?? 0) + 1; }
if (log.length) console.log(`generator log: ${log.length} faces, ${log.filter(l => l.why).length} gaps; blocked by pieces not adopted (faces): ${fmt(blocked)}; other reasons: ${fmt(reasons)}`);
// placed triangles per LOD level (catalogue tris and lods[].tris; a piece without a level keeps its L0 there) and columns, for Redline's district summary
const tris = [0, 0, 0], trisNew = [0, 0, 0], cols = new Set<string>();
for (const f of w.facades) { const a = byId.get(f.kit.id); if (!a) continue; const t = [a.tris, a.lods?.[0]?.tris ?? a.tris, a.lods?.[1]?.tris ?? a.lods?.[0]?.tris ?? a.tris]; for (let i = 0; i < 3; i++) { tris[i]! += t[i]!; if (f.column) trisNew[i]! += t[i]!; } if (f.column) cols.add(f.column.id); }
const M = (x: number) => (x / 1e6).toFixed(2);
console.log(`placed tris L0 / L1 / L2 (M, catalogue): all facades ${M(tris[0]!)} / ${M(tris[1]!)} / ${M(tris[2]!)}; every-face ${M(trisNew[0]!)} / ${M(trisNew[1]!)} / ${M(trisNew[2]!)} in ${cols.size} columns`);
const covFile = worldFile.replace(/\.world\.json$/, '.coverage.json');
writeFileSync(covFile, JSON.stringify({ faces: cov.faces, metres: Math.round(cov.metres), dressedMetres: Math.round(cov.dressedMetres), undressed: cov.undressed, perPiece: cov.perPiece, perArea: cov.perArea, columns: cols.size, tris: { all: tris, everyFace: trisNew } }, null, 1));
console.log(`coverage → ${covFile} (per face: the generator's dressedBy in ${logFile})`);
const n = Number(opt('--list') ?? 20);
for (const g of cov.undressed.slice(0, n)) console.log(`  undressed ${g.face} (${g.area}${g.play ? ', play' : ''}) ${g.length} m, z ${g.from}-${g.to} at ${g.at.join(',')}${why.get(g.building)?.[0] ? `: ${why.get(g.building)![0]!.why}` : ''}`);
if (cov.undressed.length > n) console.log(`  … ${cov.undressed.length - n} more (--list N)`);
if (cov.undressed.length) { console.error(`world:faces: FAIL ${cov.undressed.length} wall faces have no kit (no flat face at play zoom, brief 072)`); process.exit(1); }
console.log('world:faces: every wall face is kit');
