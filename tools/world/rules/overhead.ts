// TS-W0, no tram overhead (brief 075, Sky 2026-10-28: "no overhead wires though!"). The trams draw power another way: the export carries no overhead of any kind.
// Fails on: a `transit.wires` block (span or contact wires, hangers, rosettes); any kit ref anywhere in the export to a TramWire_* piece (rejected in kit.json);
// any street item of kind `span-pole`, or with a tram span id (`tram/<line>/<path>/span-<n>`, the retired wire rule's ids).
// Failing examples: rules/overhead.test.ts (a span wire block put back, a TramWire_Hanger ref on a ground piece, a span pole in the street).
import type { World } from '../../../contracts/world/world.schema';
import type { Check } from '../../plan/checks';
import { type WCtx, rule } from '../../plan/world-ctx';

const SOURCE = 'brief 075 (Sky 2026-10-28: "no overhead wires though!"; Aster: remove span wires, contact wire, hangers, rosettes and wire poles district-wide) · kit.json TramWire_* status rejected · failing examples: tools/world/rules/overhead.test.ts';
const SPAN_ID = /(^|\/)tram\/[^/]+\/[^/]+\/span-\d+/;

/** Every overhead fault in an export: where (a JSON path) and what. */
export function overheadFaults(w: World): { faults: string[]; scanned: number } {
  const faults: string[] = []; let scanned = 0;
  const t = (w as { transit?: Record<string, unknown> }).transit;
  if (t && 'wires' in t) faults.push('transit.wires: an overhead block is exported');
  const walk = (o: unknown, path: string) => {
    if (Array.isArray(o)) { o.forEach((v, i) => walk(v, `${path}[${i}]`)); return; }
    if (!o || typeof o !== 'object') return;
    const r = o as Record<string, unknown>, kit = r.kit as { id?: unknown } | undefined;
    if (typeof r.id === 'string' || kit) scanned++;
    if (kit && typeof kit.id === 'string' && /^TramWire_/.test(kit.id)) faults.push(`${path} (${String(r.id ?? '?')}): kit ${kit.id}`);
    if (r.kind === 'span-pole') faults.push(`${path} (${String(r.id ?? '?')}): a span pole`);
    else if (typeof r.id === 'string' && SPAN_ID.test(r.id)) faults.push(`${path}: ${r.id} is a tram span`);
    for (const [k, v] of Object.entries(r)) if (k !== 'kit' || typeof v === 'object') walk(v, path ? `${path}.${k}` : k);
  };
  walk(w, '');
  return { faults, scanned };
}

/** TS-W0: no overhead wire, wire piece or span item anywhere in the export. */
export function overheadRules(c: WCtx): Check[] {
  const r = overheadFaults(c.w);
  return [rule('TS-W0', 'Transit', 'No tram overhead: no transit.wires, no TramWire_* kit ref, no span pole or tram span item anywhere in the export', Math.max(r.scanned, r.faults.length), r.faults, '0 faults', SOURCE)];
}
