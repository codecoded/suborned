// The plan's skyline as retrievable facts (chunker v3): one chunk per signature, per megastructure and per tower site, plus an overview of the sites.
// Prose is capped, nested geometry and regen orders are summarised: this is text for a planner, the plan checker and the generator read the plan itself.
// Added after the first runs: a plan without signatures[] / megastructures[] / tower sites yields nothing, and no existing chunk changes (recorded runs keep their keys).
type Part = [section: string, body: string];
type Rect = [number, number, number, number];

const cap = (s: unknown, n: number) => {
  const t = String(s ?? '').replace(/\s+/g, ' ').trim();
  return t.length > n ? `${t.slice(0, n - 1).trimEnd()}…` : t;
};
const rect = (r: Rect) => `x ${r[0]} to ${r[1]}, y ${r[2]} to ${r[3]}`; // "to", not a dash: coordinates go negative
const lines = (xs: (string | undefined | false)[]) => xs.filter(Boolean).join('\n');
const field = (k: string, v: unknown, n = 400) => (v === undefined || v === null || v === '' ? undefined : `${k}: ${cap(v, n)}`);
const listed = (v: unknown) => (Array.isArray(v) ? v.join(', ') : cap(v, 140));
const spansOf = (s?: Record<string, unknown>) => Object.entries(s ?? {}).filter(([, v]) => !Array.isArray(v) || v.length).map(([k, v]) => `${k} ${listed(v)}`).join('; ');

/** What a megastructure stands on: the zones, the grid, what it never covers, and the portal it leaves clear for the beam. */
function supportsOf(s: any): string | undefined {
  if (!s) return undefined;
  const portal = s.portal && `portal clear half width ${s.portal.clearHalfWidth}${s.portal.note ? ` (${cap(s.portal.note, 120)})` : ''}`;
  return [s.zones && `zones ${s.zones.join(', ')}`, s.grid && `grid ${s.grid} m`, s.never && `never ${cap(s.never, 100)}`, portal].filter(Boolean).join('; ');
}

function opening(o: any): string | undefined {
  if (!o) return undefined;
  const at = o.at ? `x ${o.at.join('–')}` : '';
  return [`clear half width ${o.clearHalfWidth}`, o.z && `z ${o.z.join('–')}`, at, o.head].filter(Boolean).join(', ');
}

/** One declared portal: where a sky rail beam enters a signature or a megastructure. */
function portalText(p: any): string {
  const parts = [p.id, p.kind, p.beams && `${p.beams} beams`, p.line && `line ${p.line}`, p.path, p.rect && rect(p.rect), p.z && `z ${p.z.join('–')}`, p.axis,
    p.soffit !== undefined && `soffit ${p.soffit}`, p.clearHalfWidth !== undefined && `clear half width ${p.clearHalfWidth}`];
  const open = opening(p.opening ?? p.openings), station = p.station && `station ${p.station}`;
  return [...parts, open && `opening ${open}`, station].filter(Boolean).join('; ');
}

const levelsOf = (levels: any[] | undefined) => (levels ?? []).map(l => `${l.level}${l.z ? ` z ${[l.z].flat().join('–')}` : ''}: ${cap(l.what, 150)}`);

function signature(s: any): Part {
  const body = lines([
    `${s.name} (${s.id}): ${s.kind}, ${s.role}`,
    `areas: ${s.areas.join(', ')}`,
    `centre: (${s.c.join(', ')}); ground: ${rect(s.ground)}${s.height ? `; height ${s.height} m` : ''}`,
    field('kit', s.kit ? `${s.kit}${s.regen?.variant ? ` (regenerated as ${s.regen.variant})` : ''}` : undefined),
    field('crown', s.crownType ? `${s.crownType}: ${cap(s.crownWhy, 300)}` : undefined, 340),
    field('spans', spansOf(s.spans)),
    field('portal', s.portal ? portalText(s.portal) : undefined, 500),
    ...levelsOf(s.levels).map(l => `level ${l}`),
    field('cores', s.cores?.where, 200),
    field('yaw', s.yaw, 160),
    field('why', s.why, 500),
    field('source', s.source, 200),
    field('concept', s.concept, 200),
  ]);
  return [`signature › ${s.id}`, body];
}

function megastructure(m: any): Part {
  const body = lines([
    `${m.name} (${m.id}): ${m.kind}`,
    `areas: ${m.areas.join(', ')}`,
    `footprint: ${(m.footprint as Rect[]).map(rect).join(' + ')}${m.height ? `; height ${m.height} m` : ''}${m.storeys ? `, ${m.storeys} storeys` : ''}`,
    field('spans', spansOf(m.spans)),
    field('towers', m.towers?.join(', ')),
    ...levelsOf(m.levels).map(l => `level ${l}`),
    ...(m.bridges ?? []).map((b: any) => `bridge ${b.id} (${b.level ?? b.kind}${b.rect ? `, ${rect(b.rect)}` : ''}${b.axis ? `, ${cap(b.axis, 60)}` : ''}): ${cap(b.joins ?? b.why, 120)}`),
    ...(m.portals ?? []).map((p: any) => `portal ${portalText(p)}${p.why ? ` — ${cap(p.why, 180)}` : ''}`),
    field('supports', supportsOf(m.supports), 300),
    field('connectors', typeof m.connectors === 'string' ? m.connectors : undefined, 220),
    field('why', m.why, 380),
    field('concept', m.concept ? `${m.concept}${m.conceptWhy ? ` — ${cap(m.conceptWhy, 220)}` : ''}` : undefined, 520),
    field('source', m.source, 160),
  ]);
  return [`megastructure › ${m.id}`, body];
}

const tipOf = (t: any) => (t.regen?.tip ? `tip ${t.regen.tip} m` : t.regen?.same ? `tip as ${t.regen.same}` : 'tip not set');

function towerSite(t: any): Part {
  const r = t.regen;
  const body = lines([
    `Tower site ${t.id} on plot ${t.plot}: ${rect(t.rect)}; ${t.storeys} storeys; crown ${t.crownType}`,
    field('crown', t.crownWhy, 420),
    field('why', t.why, 260),
    r && `regen order: from ${r.from}${r.same ? `, the same as ${r.same} (a matched pair)` : ''}${r.body ? `; body ${r.body.join(' × ')} m` : ''}${r.tiers?.length ? `; ${r.tiers.length} tiers` : ''}${r.tip ? `; tip ${r.tip} m` : ''}`,
    field('regen crown', r?.crown, 220),
    field('regen why', r?.why, 380),
  ]);
  return [`tower site › ${t.id}`, body];
}

/** All the sites in one view: the crown and tip each stands for, the sky bridges between them, and the limits the sites share. */
function siteOverview(towers: any): Part {
  const sites: any[] = towers.sites;
  const bridges: any[] = towers.skyBridges ?? [];
  const body = lines([
    `Tower sites (standards.buildings.towers.sites): ${sites.length}. ${cap(towers.rule, 200)}`,
    ...sites.map(t => `- ${t.id}: plot ${t.plot}, ${rect(t.rect)}, ${t.storeys} storeys, crown ${t.crownType}, ${tipOf(t)}, from ${t.regen?.from ?? 'no regen order'}`),
    ...bridges.map(b => `sky bridge ${b.id}: between ${b.between.join(' and ')}; kit ${b.kit}; floor ${b.floor} m, span ${b.span} m; ${cap(b.axis, 80)}`),
    `limits: floorplate max ${towers.floorplateMax?.join(' × ')} m; storeys min ${towers.storeysMin}; crown tip ≤ ${towers.crownTip} m; podium ${towers.podium} storeys`,
  ]);
  return ['tower sites', body];
}

/** The skyline parts of a parsed plan, in plan order. */
export function skylineParts(p: any): Part[] {
  const sites: any[] = p.standards?.buildings?.towers?.sites ?? [];
  return [
    ...(p.signatures ?? []).map(signature),
    ...(p.megastructures ?? []).map(megastructure),
    ...(sites.length ? [siteOverview(p.standards.buildings.towers), ...sites.map(towerSite)] : []),
  ];
}
