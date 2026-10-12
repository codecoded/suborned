// A district plan as retrievable facts: one chunk per standard, road class, area (with its plots), transit line, and note list.
// Raw node coordinates and the civic transform stay out: they are geometry for code, not text for a planner.
import { hash, type Chunk } from './corpus';

const show = (v: unknown): string => (Array.isArray(v) && v.every(x => typeof x !== 'object') ? v.join(', ') : typeof v === 'object' ? JSON.stringify(v) : String(v));
const fields = (o: Record<string, unknown>, skip: string[] = []) => Object.entries(o).filter(([k]) => !skip.includes(k)).map(([k, v]) => `${k}: ${show(v)}`).join('\n');

export function planChunks(source: string, text: string): Chunk[] {
  const p = JSON.parse(text), sourceHash = hash(text);
  const meta = { authority: `District plan ${p.id} (${p.schema}, status: ${p.status}); the plan checker owns its rules`, version: `v${p.version} ${p.date}`, kind: 'plan' };
  const parts: [string, string][] = [
    ...Object.entries(p.standards as Record<string, Record<string, unknown>>).map(([k, v]): [string, string] => [`standards › ${k}`, fields(v, ['waived'])]),
    ...['primary', 'district', 'access'].map((c): [string, string] => [`roads › ${c}`, (p.roads as any[]).filter(r => r.class === c).map(r => `${r.name} (${r.id}): ${fields(r, ['id', 'name', 'class'])}`).join('\n')]),
    ['gates', (p.gates as any[]).map(g => `${g.name}: on ${g.road} into ${g.area} at node ${g.node}`).join('\n')],
    ...(p.areas as any[]).map((a): [string, string] => [`area › ${a.name}`, `${fields(a, ['label'])}\nplots:\n${(p.plots as any[]).filter(x => x.area === a.id).map(x => `${x.name} (${x.use}${x.civic ? `, civic ${x.civic}` : ''})`).join('\n')}`]),
    ['foot paths, circus, landmarks, off-sheet links', [...(p.foot as any[]).map(f => `${f.name}: ${f.kind}, ${f.w} m wide`), `circus: ${fields(p.circus)}`, ...(p.landmarks as any[]).map(l => `${l.name}: centre ${l.c}, radius ${l.r} m`), ...(p.links as any[]).map(l => `off-sheet ${l.to} via ${l.via}`)].join('\n')],
    ...[...(p.tram as any[]).map(l => ['tram', l]), ...(p.skyrail as any[]).map(l => ['sky rail', l])].map(([mode, l]): [string, string] => [`${mode} › ${l.id} ${l.name}`, `${fields(l, ['paths', 'stops', 'stations'])}\npaths: ${(l.paths as any[]).map(x => `${x.id} (${x.role}${x.double ? ', double' : ''})`).join('; ')}\n${mode === 'tram' ? 'stops' : 'stations'}: ${((l.stops ?? l.stations) as any[]).map(s => `${s.name} at ${s.at}${s.interchange ? ` (interchange ${s.interchange})` : ''}`).join('; ')}`]),
    ...(p.routes ? [['routes', (p.routes as any[]).map(r => `${r.name}: ${fields(r, ['id', 'name'])}`).join('\n')] as [string, string]] : []),
    ...Object.entries(p.notes as Record<string, string[]>).map(([k, v]): [string, string] => [`notes › ${k}`, v.map(x => `- ${x}`).join('\n')]),
  ];
  return parts.map(([section, body]) => ({ key: hash(`${source}:${section}:${body}`).slice(0, 24), source, sourceHash, section: `${p.name} › ${section}`, lineStart: 0, lineEnd: 0, text: `${p.name} plan › ${section}\n${body}`, ...meta }));
}
