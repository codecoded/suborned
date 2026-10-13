// The DecoKit catalogue (contracts/world/kit.json) as retrievable facts (chunker v3): one chunk per category and one per adopted piece, with
// size, tags and status. Hashes, file lists, LODs and materials stay out: they are for the importer, not for a planner.
// A run pins `kitDigest(kit.json)` (about 50 KB) instead of the 1.3 MB catalogue: the chunks come out identical from either, so a later
// adoption in kit.json does not break the replay of an earlier run.
import { hash, type Chunk } from './corpus';

export const KIT_FILE = 'contracts/world/kit.json';
const DIGEST = 'suborned.kit.digest/1';

type Tags = { use?: string[]; reach?: { axis: string; m: number }; trade?: string[]; storeys?: [number, number]; form?: string; scale?: [number, number]; squash?: number; wave?: number };
export type Piece = {
  id: string; category: string; status: string; size: number[]; tris: number; lodCategory: string; tags?: Tags;
  pivot?: string; pivotOffset?: number[]; order?: string; tip?: number; tipPlan?: number; generated?: true;
};
export type KitDigest = { schema: typeof DIGEST; assets: Piece[] };

const piece = (a: any): Piece => ({
  id: a.id, category: a.category, status: a.status, size: a.size, tris: a.tris, lodCategory: a.lodCategory, tags: a.tags,
  ...(a.pivot !== 'footprint-centre-ground' ? { pivot: a.pivot, pivotOffset: a.pivotOffset } : {}),
  ...(a.order ? { order: a.order.id, tip: a.built?.tip, tipPlan: a.built?.tipPlan } : {}),
  ...(a.source === 'generated' ? { generated: true as const } : {}),
});

/** kit.json (or an earlier digest, unchanged) as the text the chunks are made from. Idempotent: kitDigest(kitDigest(x)) === kitDigest(x). */
export function kitDigest(text: string): string {
  const doc = JSON.parse(text);
  if (doc.schema === DIGEST) return JSON.stringify(doc);
  return JSON.stringify({ schema: DIGEST, assets: (doc.assets as unknown[]).map(piece) } satisfies KitDigest);
}

const n = (x: number) => String(Math.round(x * 100) / 100);
const dims = (size: number[]) => `${n(size[0]!)} × ${n(size[1]!)} × ${n(size[2]!)}`; // x × height × z, metres: the kit frame is y-up
const range = (r?: [number, number]) => (r ? `${r[0]}–${r[1]}` : '');

function tagsOf(t: Tags = {}): string {
  const bits = [
    t.use?.length && `use ${t.use.join(', ')}`, t.trade?.length && `trade ${t.trade.join(', ')}`, t.storeys && `storeys ${range(t.storeys)}`, t.form && `form ${t.form}`,
    t.reach && `reach ${t.reach.axis} ${t.reach.m} m`, t.scale && `scale ${range(t.scale)}`, t.squash !== undefined && `squash ${t.squash}`, t.wave !== undefined && `wave ${t.wave}`,
  ];
  return bits.filter(Boolean).join('; ');
}

function pieceBody(a: Piece): string {
  const pivot = a.pivot === 'origin-ground' ? `origin on the ground, footprint centre at (${(a.pivotOffset ?? []).map(n).join(', ')}) from it` : 'footprint centre on the ground';
  return [
    `${a.id} (${a.status}), category ${a.category}`,
    `size: ${dims(a.size)} m (x × height × z); ${a.tris} tris; LOD category ${a.lodCategory}; pivot ${pivot}; front +Z`,
    `tags: ${tagsOf(a.tags) || 'none'}`,
    a.order && `built to plan order ${a.order}${a.tip !== undefined ? `: tip ${n(a.tip)} m${a.tipPlan !== undefined ? ` (plan ${n(a.tipPlan)} m)` : ''}` : ''}`,
  ].filter(Boolean).join('\n');
}

function categoryBody(category: string, pieces: Piece[]): string {
  const by = (status: string) => pieces.filter(p => p.status === status);
  const counts = ['adopted', 'candidate', 'rejected'].filter(s => by(s).length).map(s => `${by(s).length} ${s}`).join(', ');
  return [
    `category ${category}: ${pieces.length} pieces (${counts}). Only adopted pieces are placed; a candidate waits for adoption, a rejected piece is retired.`,
    by('adopted').length && `adopted (x × height × z m): ${by('adopted').map(p => `${p.id} ${dims(p.size)}`).join('; ')}`,
    by('candidate').length && `candidate: ${by('candidate').map(p => p.id).join(', ')}`,
    by('rejected').length && `rejected: ${by('rejected').map(p => p.id).join(', ')}`,
  ].filter(Boolean).join('\n');
}

export function kitChunks(source: string, text: string): Chunk[] {
  const digest = kitDigest(text), sourceHash = hash(digest), doc: KitDigest = JSON.parse(digest);
  const meta = { authority: 'DecoKit catalogue (suborned.kit/1): only adopted pieces may be placed; Vela generates the pieces', version: 'suborned.kit/1', kind: 'kit' };
  const chunk = (section: string, body: string): Chunk => ({
    key: hash(`${source}:${section}:${body}`).slice(0, 24), source, sourceHash, section: `DecoKit › ${section}`, lineStart: 0, lineEnd: 0, text: `DecoKit catalogue › ${section}\n${body}`, ...meta,
  });
  const categories = [...new Set(doc.assets.map(a => a.category))];
  return categories.flatMap(category => {
    const pieces = doc.assets.filter(a => a.category === category);
    return [chunk(category, categoryBody(category, pieces)), ...pieces.filter(p => p.status === 'adopted').map(p => chunk(`${category} › ${p.id}`, pieceBody(p)))];
  });
}
