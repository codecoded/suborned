// V-1's per-point occluders at the plan's play view, for the Unity yaw study to compare against (Study.cs, V1.cs).
//   bun tools/unity/v1.ts [plan.json] [--out out/unity/v1.json]
// Blockers use the scene's names: "plot <id>" for a plot prism the ray passes under the top of (margin < 0), else the
// Handover building or structure id. The export does not carry this, so the graybox pipeline writes it beside the world.
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { cliArgs, load, root } from '../plan/load';
import { visibility } from '../plan/view';

export function writeV1(src: string, out: string) {
  const { plan, d, civ, planHash } = load(src);
  const points = visibility(plan, d, civ).map(s => ({
    name: s.name,
    blockers: [...(s.plot && s.plot.margin < 0 ? [`plot ${s.plot.id}`] : []), ...s.others.map(o => o.id)].sort(),
  }));
  mkdirSync(dirname(out), { recursive: true });
  writeFileSync(out, JSON.stringify({ planHash, yaw: plan.view.yaw, pitch: plan.view.pitch, points }, null, 1) + '\n');
  return points;
}

if (import.meta.main) {
  const { src, opt } = cliArgs(process.argv.slice(2));
  const out = resolve(root, opt('--out') ?? 'out/unity/v1.json');
  const pts = writeV1(src, out);
  console.log(`v1: ${pts.filter(p => !p.blockers.length).length}/${pts.length} unoccluded → ${out}`);
}
