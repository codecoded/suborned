/** Read-only, opt-in source topology. Never invokes world/build or writes a world/walk. */
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname, sep } from 'node:path';
import { createHash } from 'node:crypto';
import { isDeepStrictEqual } from 'node:util';
import { derive, type Plan } from '../plan/plan';
import { civic } from '../plan/civic';
import { type V, dist } from '../plan/geom';
import { ctxOf, FLAT_FOOT, r3, r4, rv } from './gen/common';
import { tramGround, newTramLog, type TramSourceItem, type TramSourceSegment } from './gen/street/tramstreet';
import type { StreetItem } from '../../contracts/world/world.schema';

const sub = (a: V, b: V): V => [a[0] - b[0], a[1] - b[1]];
const add = (a: V, u: V, s: number): V => [a[0] + u[0] * s, a[1] + u[1] * s];
const cross = (a: V, b: V) => a[0] * b[1] - a[1] * b[0];
const sha = (p: string) => createHash('sha256').update(readFileSync(p)).digest('hex');
export const DEFAULT_INPUTS = { plan: 'contracts/world/harbour-district.plan.json', block: 'contracts/world/civic-block.json', world: 'out/world/harbour-district.world.json', rejected: 'briefs/075/recovery/tram-joins/rejected-joins.json' };
export function captureTram(plan: Plan, block: any) {
  const ctx = ctxOf(plan, derive(plan), civic(plan.civic, block)), segments: TramSourceSegment[] = [], items: TramSourceItem[] = [];
  const log = newTramLog();
  const ground = tramGround(ctx, log, FLAT_FOOT, { segment: s => segments.push(s), item: s => items.push(s) });
  return { ctx, segments, items, ground, log };
}
export function topology(inputs = DEFAULT_INPUTS) {
  const read = (p: string) => JSON.parse(readFileSync(p, 'utf8'));
  const plan = read(inputs.plan) as Plan, block = read(inputs.block), world = read(inputs.world);
  const { segments, items, ground } = captureTram(plan, block);
  const pinned: StreetItem[] = world.ground.pieces.filter((p: StreetItem) => p.kind === 'tram-rail');
  // Full object equality, including ids, order, kit, height, sizes and scales. Fail closed on stale inputs.
  if (!isDeepStrictEqual(ground?.rails, pinned)) throw new Error('source rails do not exactly equal pinned world rails');
  const tiles = items.flatMap(item => {
    const p = item.exported; if (!p) return [];
    if (!isDeepStrictEqual(rv(item.center), p.at) || r4(item.yawExact) !== p.yaw || r3(item.lengthExact) !== p.size[0] || r3(item.zExact) !== p.z || r4(item.scaleExact) !== p.kit?.scale[0]) throw new Error(`rounding mismatch ${p.id}`);
    const n = Math.max(1, Math.round(p.size[0] / 2)), exactStep = item.lengthExact / n, roundedStep = p.size[0] / n;
    const roundedTangent: V = [Math.cos(p.yaw), Math.sin(p.yaw)];
    return Array.from({ length: n }, (_, i) => {
      const center = add(item.center, item.tangent, -item.lengthExact / 2 + exactStep * (i + .5));
      const roundedCenter = add(p.at, roundedTangent, -p.size[0] / 2 + roundedStep * (i + .5));
      return { id: `${p.id}#${i}`, parent: p.id, track: item.track, segment: item.segment, subdivision: i, subdivisions: n,
        interval: [item.interval[0] + i * exactStep, item.interval[0] + (i + 1) * exactStep], center, tangent: item.tangent, step: exactStep, z: item.zExact,
        rounded: { center: roundedCenter, tangent: roundedTangent, step: roundedStep },
        error: { center: dist(center, roundedCenter), tangent: dist(item.tangent, roundedTangent), step: Math.abs(exactStep - roundedStep) } };
    });
  });
  // Prove complete, nonoverlapping source interval coverage before declaring any run continuous.
  for (const segment of segments) {
    const source = items.filter(i => i.track === segment.track && i.segment === segment.segment);
    let end = 0;
    for (const item of source) {
      if (Math.abs(item.interval[0] - end) > 1e-9 || item.interval[1] <= item.interval[0]) throw new Error('source interval gap/overlap');
      end = item.interval[1];
    }
    if (Math.abs(end - segment.length) > 1e-9) throw new Error('incomplete source interval coverage');
  }
  const byId = new Map(tiles.map(t => [t.id, t]));
  const tracks = [...new Set(segments.map(s => s.track))].map(track => {
    const ss = segments.filter(s => s.track === track), tt = tiles.filter(t => t.track === track);
    const boundaries = ss.slice(1).map((s, i) => ({ fromSegment: ss[i]!.segment, toSegment: s.segment, separation: dist(ss[i]!.end, s.start), tangentCross: cross(ss[i]!.endTangent, s.startTangent) }));
    const omitted = items.filter(i => i.track === track && !i.exported).map(i => ({ segment: i.segment, interval: i.interval, reason: i.omitted }));
    // Connectivity comes from source interval coverage and segment endpoints, never helper acceptance.
    const continuous = boundaries.every(b => b.separation < 1e-9) && omitted.length === 0;
    const endpoints = [{ at: ss[0]!.start, tangent: ss[0]!.startTangent, end: 'start' }, { at: ss.at(-1)!.end, tangent: ss.at(-1)!.endTangent, end: 'end' }];
    return { id: track, segments: ss, boundaries, omitted, continuous, closed: dist(endpoints[0]!.at, endpoints[1]!.at) < 1e-9,
      endpoints, orderedTiles: tt.map(t => t.id), neighbors: tt.slice(1).map((t, i) => ({ from: tt[i]!.id, to: t.id, sourceAdjacent: t.segment === tt[i]!.segment ? Math.abs(t.interval[0]! - tt[i]!.interval[1]!) < 1e-9 : t.segment === tt[i]!.segment + 1 && boundaries.some(b => b.fromSegment === tt[i]!.segment && b.separation < 1e-9) })) };
  });
  const endpoints = tracks.flatMap(t => t.endpoints.map(e => ({ ...e, track: t.id })));
  const graph = endpoints.map(e => ({ ...e, joins: endpoints.filter(f => f.track !== e.track && dist(e.at, f.at) < 1e-9).map(f => ({ track: f.track, end: f.end, tangentDot: e.tangent[0] * f.tangent[0] + e.tangent[1] * f.tangent[1] })) }));
  const rejected = (read(inputs.rejected) as string[]).map(message => {
    const match = message.match(/(ground\/tram\/rail-[\w]+#\d+) -> (ground\/tram\/rail-[\w]+#\d+)/);
    if (!match) throw new Error(`unparsed rejection ${message}`);
    const a = byId.get(match[1]!)!, b = byId.get(match[2]!)!;
    if (!a || !b) throw new Error(`unmapped rejection ${message}`);
    const track = tracks.find(t => t.id === a.track)!;
    const adjacent = track.neighbors.some(n => n.from === a.id && n.to === b.id && n.sourceAdjacent);
    const ae = add(a.center, a.tangent, a.step / 2), bs = add(b.center, b.tangent, -b.step / 2);
    const offset = cross(a.tangent, sub(b.center, a.center));
    const parallel = message.includes('parallel displaced');
    const segA = segments.find(s => s.track === a.track && s.segment === a.segment)!, segB = segments.find(s => s.track === b.track && s.segment === b.segment)!;
    const capBound = (s: TramSourceSegment, step: number) => {
      if (s.geometry.kind === 'line') return 0;
      const radius = s.length / Math.abs(s.geometry.sweep), theta = step / (2 * radius);
      return radius * Math.hypot(1 - Math.cos(theta), theta - Math.sin(theta));
    };
    const classification = !adjacent ? 'unsupported-source-disconnection' : parallel ? 'rounded-yaw-displacement; exact-lines-coincident' : 'continuous-source-arc/short-line-transition; helper-validation-bound-too-small';
    if (parallel && (Math.abs(offset) > 1e-9 || Math.abs(cross(a.tangent, b.tangent)) > 1e-9)) throw new Error(`unexpected exact parallel geometry ${message}`);
    return { message, from: a.id, to: b.id, track: a.track, fromSegment: a.segment, toSegment: b.segment, sourceAdjacent: adjacent, classification,
      roundedTransverseOffset: cross(a.rounded.tangent, sub(b.rounded.center, a.rounded.center)),
      roundedTransverseBound: Math.abs(offset) + a.error.center + b.error.center + dist(a.center, b.center) * a.error.tangent + 1e-10,
      exactTransverseOffset: offset, exactTangentCross: cross(a.tangent, b.tangent), exactTangentEndSeparation: dist(ae, bs),
      helperValidationLimit: .008 + .05 * Math.min(a.step, b.step),
      tangentEndSeparationBound: capBound(segA, a.step) + capBound(segB, b.step) + 1e-10,
      capBoundFormula: 'R*hypot(1-cos(h/R),h/R-sin(h/R)), h=step/2; sum adjacent caps plus 1e-10m floating arithmetic allowance',
      source: { from: segments.find(s => s.track === a.track && s.segment === a.segment), to: segments.find(s => s.track === b.track && s.segment === b.segment), fromInterval: a.interval, toInterval: b.interval } };
  });
  const max = (f: (t: typeof tiles[number]) => number) => Math.max(0, ...tiles.map(f));
  return { schema: 'suborned.tram-topology/1', inputs: Object.fromEntries(Object.entries(inputs).map(([k, p]) => [k, { path: p, sha256: sha(p) }])),
    policy: 'Source arclength adjacency; no snapping, bridging or helper-driven splitting. Plan frame. Sidecar alone is not rendering acceptance. G-3a 59 > 58; sight HOLD.',
    rounding: { code: 'tools/world/gen/common.ts: Math.round(n*1000)/1000 (rv/r3), Math.round(n*10000)/10000 (r4)', centerAxisBound: .0005, centerEuclideanBound: Math.SQRT2 * .0005, yawBoundRadians: .00005, tangentVectorBound: 2 * Math.sin(.00005 / 2), parentLengthBound: .0005, scaleBound: .00005,
      subdivision: 'Use pinned n=max(1,round(exportedLength/2)); exact centre=sourceCenter+tangent*(-exactLength/2+exactLength/n*(i+.5)). It is not independently rounded: rounded counterpart is reconstructed from the exactly matched parent export.',
      maxParentCenterError: Math.max(...items.filter(i => i.exported).map(i => dist(i.center, i.exported!.at))),
      maxParentYawError: Math.max(...items.filter(i => i.exported).map(i => Math.abs(i.yawExact - i.exported!.yaw))),
      maxParentLengthError: Math.max(...items.filter(i => i.exported).map(i => Math.abs(i.lengthExact - i.exported!.size[0]))),
      maxSubdivisionCenterError: max(t => t.error.center), maxTangentError: max(t => t.error.tangent), maxStepError: max(t => t.error.step),
      subdivisionCenterBound: max(t => Math.SQRT2 * .0005 + .00025 + (t.step * t.subdivisions / 2) * 2 * Math.sin(.00005 / 2)) },
    sourceHashes: Object.fromEntries(['contracts/world/kit.json', 'tools/world/gen/common.ts', 'tools/world/gen/street/tramstreet.ts', 'tools/plan/geom.ts', 'unity/Assets/Suborned/Editor/OldTown/Swap/SwapTramJoin.cs'].map(p => [p, sha(p)])),
    counts: { parents: pinned.length, tiles: tiles.length, omitted: items.filter(i => !i.exported).length, classified: rejected.length, parallel: rejected.filter(r => r.message.includes('parallel displaced')).length, maxExactParallelOffset: Math.max(...rejected.filter(r => r.message.includes('parallel displaced')).map(r => Math.abs(r.exactTransverseOffset))) },
    continuousRuns: tracks.filter(t => t.continuous).map(t => ({ id: t.id, closed: t.closed, orderedTiles: t.orderedTiles, endpointProvenance: graph.filter(e => e.track === t.id) })),
    tracks, graph, items, tiles, rejected,
    unsupported: rejected.filter(r => !r.message.includes('parallel displaced')).map(r => ({ from: r.from, to: r.to, reason: r.classification, action: 'Latch: validate adjacency from source provenance, then review helper validation independently. Current helper still rejects lossless inputs; do not raise tolerance or split runs.' })) };
}
export function outputPath(p: string) {
  const dest = resolve(p);
  if (!['out/scratch/splice/tram-topology', 'briefs/075/recovery/tram-topology'].some(root => dest.startsWith(resolve(root) + sep))) throw new Error('--out must be inside the tram-topology scratch/evidence folder');
  return dest;
}
if (import.meta.main) {
  const args = process.argv.slice(2);
  if (args.length !== 2 || args[0] !== '--out') throw new Error('usage: bun tools/world/tram-topology.ts --out <explicit tram-topology path>');
  const dest = outputPath(args[1]!), result = topology();
  mkdirSync(dirname(dest), { recursive: true }); writeFileSync(dest, JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({ out: dest, counts: result.counts, rounding: result.rounding, tracks: result.tracks.map(t => ({ id: t.id, continuous: t.continuous, closed: t.closed })) }, null, 2));
}
