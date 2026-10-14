// The facts a cast-table query needs (chunker v4, brief 010 B): life.json (trades with hours and staff, areas with their units and
// homes, routines, civic employers, tiers), the civic block (buildings with entrances, bays, keep-clear zones), the missions
// (clock, timeline, destinations, crew) and the people rules (temperaments, needs, ties, positions, names). One chunk per fact
// a planner would ask for; appended after every earlier chunk, so no v3 chunk changes its key.
import { hash, type Chunk } from './corpus';

const show = (v: unknown): string => (typeof v === 'object' && v !== null ? JSON.stringify(v) : String(v));
const lines = (o: Record<string, unknown>, skip: string[] = []) => Object.entries(o).filter(([k]) => !skip.includes(k)).map(([k, v]) => `${k}: ${show(v)}`).join('\n');
const hrs = (h: unknown) => (Array.isArray(h) ? `${h[0]}:00 to ${h[1]}:00 (wraps past midnight when the close is below the open)` : String(h));

type Made = [string, string][];
const make = (source: string, text: string, kind: string, authority: string, version: string, parts: Made, name: string): Chunk[] => {
  const sourceHash = hash(text);
  return parts.map(([section, body]) => ({ key: hash(`${source}:${section}:${body}`).slice(0, 24), source, sourceHash, section: `${name} › ${section}`, lineStart: 0, lineEnd: 0, text: `${name} › ${section}\n${body}`, authority, version, kind }));
};

export function lifeChunks(source: string, text: string): Chunk[] {
  const l = JSON.parse(text), g = l.generator, parts: Made = [];
  parts.push(['clock and day bands', `${lines(l.clock, ['src'])}`]);
  for (const [t, o] of Object.entries(g.occupations as Record<string, any>)) {
    parts.push([`trade ${t}`, `Trade ${t} (${o.category}): open ${hrs(o.hours)}; ${o.staffPerUnit} staff on shift per unit; mean frontage ${o.meanWidth} m, depth ${o.depth} m; shifts: ${show(o.shifts)}`]);
  }
  for (const [a, e] of Object.entries(g.expected.workersPerArea as Record<string, any>)) {
    const homes = (g.homes as any[]).filter(h => h.area === a);
    parts.push([`area ${a} staffing`, `Area ${a}: ${e.frontageMetres} m of frontage, ${e.units} units, ${e.staff} staff.\ntrades (units): ${Object.entries(e.trades as Record<string, any>).map(([t, v]) => `${t} ${v.units}`).join(', ')}\nhomes: ${homes.map(h => `${h.plot} (${h.use}, ${h.residents} residents)`).join('; ') || 'none'}`]);
  }
  parts.push(['routine skeletons', Object.entries(g.routines as Record<string, string[]>).map(([k, v]) => `${k}: ${v.join(' → ')}`).join('\n') + `\n${g.routinesNote}`]);
  parts.push(['civic employers, station workers, tiers', `civic employers: ${show(g.civicEmployers)}\nstation workers: ${show(g.stationWorkers)}\ntiers: ${show(g.tiers)}\ncommuters: ${g.commuters.rule}`]);
  parts.push(['walking and residents', `walkSpeed ${l.pedestrians.walkSpeed} m/s; last leg ${l.pedestrians.lastLegM} m; reach ${l.pedestrians.reachM} m\nresidents: ${l.residents.rule}; ${l.residents.perHome} per home\ntransit service hours ${show(l.transit.serviceHours)}`]);
  return make(source, text, 'life', `City-life rules ${l.plan.id} (${l.schema}); the life tool owns them`, `plan ${l.plan.version}`, parts, `${l.plan.id} life`);
}

export function blockChunks(source: string, text: string): Chunk[] {
  const b = JSON.parse(text), parts: Made = [];
  parts.push(['frame', `${lines(b.frame)}\nid: ${b.id}`]);
  for (const x of b.buildings as any[]) {
    parts.push([`building ${x.id}`, `Building ${x.id} (${x.name}): height ${x.height} m, z ${x.z}${x.interior ? `, interior ${show(x.interior)}` : ''}\nentrances: ${(x.entrances ?? []).map((e: any) => `${e.id} "${e.name}" at ${show(e.at)}${e.on ? ` on ${e.on}` : ''}`).join('; ')}${x.towers ? `\ntowers: ${x.towers.map((t: any) => `${t.id} at ${show(t.at)} height ${t.height}`).join('; ')}` : ''}${x.heights ? `\nheights: ${show(x.heights)}` : ''}`]);
  }
  parts.push(['bays', (b.bays as any[]).map(x => `${x.id}: ${show(x.polygon)}`).join('\n')]);
  parts.push(['street furniture', (b.objects as any[]).map(o => `${o.id} (${o.kind})`).join(', ')]);
  const kc = (b.dressing?.keepClear ?? []) as any[];
  if (kc.length) parts.push(['keep-clear zones', kc.map(z => `${z.id}: ${z.reason}`).join('\n')]);
  return make(source, text, 'block', `Civic block ${b.id} (${b.schema}); bun run block writes it`, b.schema, parts, `civic block`);
}

export function missionChunks(source: string, text: string): Chunk[] {
  const m = JSON.parse(text), parts: Made = [];
  parts.push(['clock', `${lines(m.clock, ['src', 'contradiction', 'transit'])}`]);
  parts.push(['timeline', (m.timeline as any[]).map(t => `${t.at} ${t.what}`).join('\n')]);
  parts.push(['destinations and crew', `destinations (block frame): ${(m.destinations as any[]).map(d => `${d.name} ${show(d.at)}`).join('; ')}\ncrew: ${(m.crew as any[]).map(d => `${d.name} ${show(d.at)}`).join('; ')}`]);
  parts.push(['events and stands', `events: ${(m.events as any[]).map(e => `${e.id} at ${e.at} in ${e.area}: ${e.who}`).join('; ')}\nstands: ${(m.stands as any[]).map(s => `${s.what} at ${s.where}`).join('; ')}`]);
  parts.push(['routes', (m.routes as any[]).map(r => `${r.id}: ${r.name}`).join('\n')]);
  return make(source, text, 'mission', `Mission ${m.id} (${m.schema}); GDD §07 is the authority for its timeline`, m.schema, parts, `mission ${m.id}`);
}

export function peopleRulesChunks(source: string, text: string): Chunk[] {
  const r = JSON.parse(text), parts: Made = [];
  parts.push(['person and tiers', `${show(r.person)}\ntiers: ${show(r.tiers)}`]);
  parts.push(['scales', Object.entries(r.scales as Record<string, any>).map(([k, v]) => `${k}: ${v.min}-${v.max}${v.steps ? ` (${v.steps.join(', ')})` : ''}; ${v.src}`).join('\n')]);
  for (const [k, t] of Object.entries(r.temperaments as Record<string, any>)) parts.push([`temperament ${k}`, `${t.name}: responds to ${t.respondsTo.join(', ')}; refuses ${t.refuses.join(', ') || 'nothing'}; reports an approach: ${t.reportsApproach}; start ${show(t.start)}\nHero: ${t.hero}. Menace: ${t.menace}.`]);
  parts.push(['approaches', Object.entries(r.approaches as Record<string, any>).filter(([k]) => k !== 'src').map(([k, a]) => `${k}: requires ${a.requires}, $${a.usd}, ${a.timeS} s, Trail +${a.trail}; ${a.leaves}`).join('\n')]);
  parts.push(['needs', `${r.needs.rule}\n${Object.entries(r.needs.list as Record<string, any>).map(([k, n]) => `${k}: met by ${n.metBy.join(', ')}; help is ${n.help}; requires ${n.requires}; ${n.src}`).join('\n')}`]);
  parts.push(['ties', `${Object.entries(r.ties.kinds as Record<string, any>).map(([k, t]) => `${k}: ${t.direction}, at most ${t.maxPerPerson} per person${t.maxIncoming ? `, ${t.maxIncoming} incoming` : ''}, between ${t.between}`).join('\n')}\nper person ${show(r.ties.perPerson)}`]);
  parts.push(['talker', `${show(r.talker.test)}; facts given: ${r.talker.facts.join(', ')}`]);
  parts.push(['positions', `${r.positions.rule}\n${Object.entries(r.positions.byCategory as Record<string, string[]>).map(([c, p]) => `${c}: ${p.join(', ')}`).join('\n')}`]);
  const tb = r.temperamentsBy;
  parts.push(['roles and employers', `${tb.rule}\nroles: ${Object.entries(tb.roles as Record<string, any>).map(([k, v]) => `${k} (${v.employer}) allows ${v.allow.join(', ')}`).join('; ')}\nemployers: ${show(tb.employers)}`]);
  const groups = new Map<string, string[]>();
  for (const [t, v] of Object.entries(tb.trades as Record<string, any>)) groups.set(`${v.category} allows ${v.allow.join(', ')}`, [...(groups.get(`${v.category} allows ${v.allow.join(', ')}`) ?? []), t]);
  parts.push(['trades and temperaments', [...groups].map(([k, v]) => `${k}: ${v.join(', ')}`).join('\n')]);
  parts.push(['names', `${r.names.pool}\nminimums: ${r.names.minGiven} given, ${r.names.minFamily} family, ${r.names.minOrigins} origins, none over ${r.names.maxOriginShare * 100}%; ${r.names.noEraDate}\norigins: ${r.names.origins.join(', ')}\navoid: ${r.names.avoid.join(', ')}\npinned: ${show(r.names.pinned)}`]);
  return make(source, text, 'people-rules', `People rules (${r.schema}); Motive owns them, the checker enforces them`, r.schema, parts, 'people rules');
}
