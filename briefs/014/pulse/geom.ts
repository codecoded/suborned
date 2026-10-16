import { readTexts, world } from '../../../tools/people/inputs';
import { buildScope } from '../../../tools/people/scope';
import { derive } from '../../../tools/plan/plan';
import { parseRules } from '../../../tools/people/rules';
const t = await readTexts('contracts/world/harbour-district.plan.json');
const plan = JSON.parse(t.plan), block = JSON.parse(t.block), life = JSON.parse(t.life), rules = parseRules(t.rules);
const { scope, civ } = buildScope(plan, block, life, rules, 'civic-block', derive(plan));
const o = plan.civic.blockToPlan.origin, ax = plan.civic.blockToPlan.x, ay = plan.civic.blockToPlan.y, det = ax[0]*ay[1]-ax[1]*ay[0];
const toBlock = (p: number[]) => { const dx=p[0]-o[0], dy=p[1]-o[1]; return [((dx*ay[1]-dy*ay[0])/det), ((ax[0]*dy-ax[1]*dx)/det)]; };
const out = {
  buildings: civ.buildings.map((b: any) => ({ id: b.id, poly: b.poly.map(toBlock) })),
  solids: (civ.solids ?? []).map((s: any) => ({ id: s.id, kind: s.kind, poly: (s.poly ?? s.pts ?? []).map?.(toBlock) ?? [] })),
  objects: civ.objects.map((s: any) => ({ id: s.id, kind: s.kind, poly: (s.poly ?? []).map(toBlock) })),
  keys: Object.keys(civ), solidKeys: civ.solids?.[0] ? Object.keys(civ.solids[0]) : [],
};
await Bun.write('out/pulse/geom.json', JSON.stringify(out));
console.log(out.buildings.map((b: any) => b.id), out.solids.length, out.solidKeys, out.keys);
