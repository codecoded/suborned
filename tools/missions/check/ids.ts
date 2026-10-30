// Brief 078 B1 (Keel, 2026-10-30): a mission's words and its points must name ids the site has. Brief 075's stable zone ids renamed the
// square's mast camera (street/records-square/mast-camera-314 → -z0, so the sim's camera mast-314 → mast-z0) and every `camera-mast-314`
// key in text.json silently stopped matching: no name, no hover text, no order lines on the mast. This guard reads the site the sim
// loads (sim/data/<mission>.*.json) and fails, naming each id, when
//   · a text target key that stands for a site thing (camera-<id>, stand-<id>, car-<id>) has no such thing in the site;
//   · any key the text uses as a target (names.targets, actions.*.onTarget / onTargetBy, dialogue `about`, names.who maps, WATCHED)
//     is not one of the text's own `targets` (or names.targets) keys;
//   · a site camera or getaway stand has no `targets` entry or no name (the reverse: a rename on the site side drops the words);
//   · mission.json names a camera, point or destination the site does not carry.
// The host's key for a target is HandoverHost.TextKey: camera → `camera-<Camera.Id>`, stand → `stand-<stand id>`, car → `car-<key>`.
// Runs in `bun run test` (ids.test.ts) and before any play in `bun run missions:check`.
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = join(import.meta.dir, '../../..');
const read = (p: string) => JSON.parse(readFileSync(p, 'utf8'));

/** The ids the sim's site carries, read the way Site.cs reads them. */
export type SiteIds = { cameras: Set<string>; stands: Set<string>; cars: Set<string>; places: Set<string>; doors: Set<string> };

export function siteIds(mission: string, root = ROOT): SiteIds {
  const site = read(join(root, 'sim/data', `${mission}.site.json`));
  const tdPath = join(root, 'sim/data', `${mission}.takedown.json`), td = existsSync(tdPath) ? read(tdPath) : undefined;
  const cameras = new Set<string>((site.cameras ?? []).map((c: { id: string }) => c.id));
  // Site.cs: a takedown watcher of kind mast-camera is a camera named by its street id's last segment, "-camera" dropped
  for (const w of td?.watchers ?? []) if (w.kind === 'mast-camera') cameras.add(String(w.id).slice(String(w.id).lastIndexOf('/') + 1).replace('-camera', ''));
  const stands = new Set<string>((site.getaway?.stands ?? []).map((s: { id: string }) => s.id));
  const cars = new Set<string>([...(site.getaway ? ['getaway'] : []), ...(site.car ? ['official'] : []), ...(site.rivals?.car ? ['rivals'] : [])]);
  const places = new Set<string>(Object.keys(site.places ?? {}));
  const doors = new Set<string>([...(td?.entrances ?? []).map((e: { id: string }) => e.id), ...(td?.stashes ?? []).filter((s: { kind: string }) => s.kind === 'door').map((s: { of: string }) => s.of)]);
  return { cameras, stands, cars, places, doors };
}

/** Every problem, one line each; empty = the mission's ids all resolve. */
export function missionIdProblems(mission: string, root = ROOT): string[] {
  const dir = join(root, 'contracts/missions', mission), bad: string[] = [];
  const s = siteIds(mission, root);
  const textPath = join(dir, 'text.json');
  if (existsSync(textPath)) {
    const t = read(textPath);
    const targets = new Set<string>([...Object.keys(t.targets ?? {}), ...Object.keys(t.names?.targets ?? {})]);
    // a key that stands for a site thing must name one the site has
    const resolve = (k: string, where: string) => {
      const m = /^(camera|stand|car)-(.+)$/.exec(k);
      if (!m) return;
      const [, kind, id] = m as unknown as [string, 'camera' | 'stand' | 'car', string];
      const set = kind === 'camera' ? s.cameras : kind === 'stand' ? s.stands : s.cars;
      if (!set.has(id)) bad.push(`${where}: '${k}' names ${kind} '${id}', but the site has no such ${kind} (site ${kind}s: ${[...set].join(', ') || 'none'})`);
    };
    const used = (k: string, where: string) => { if (!targets.has(k)) bad.push(`${where}: '${k}' is no target key of the text (targets / names.targets)`); resolve(k, where); };
    for (const k of Object.keys(t.targets ?? {})) resolve(k, `text.json targets`);
    for (const k of Object.keys(t.names?.targets ?? {})) resolve(k, `text.json names.targets`);
    const actions = (a: Record<string, { onTarget?: Record<string, string>; onTargetBy?: Record<string, Record<string, string>> }> | undefined, at: string) => {
      for (const [v, x] of Object.entries(a ?? {})) {
        for (const k of Object.keys(x.onTarget ?? {})) used(k, `text.json ${at}.${v}.onTarget`);
        for (const [g, m] of Object.entries(x.onTargetBy ?? {})) for (const k of Object.keys(m)) used(k, `text.json ${at}.${v}.onTargetBy.${g}`);
      }
    };
    actions(t.actions, 'actions');
    for (const e of t.dialogue ?? []) if (typeof e.about === 'string' && /^(camera|stand|car)-/.test(e.about)) used(e.about, `text.json dialogue (${e.event} ${e.verb} ${e.speaker})`);
    // `who` maps (debrief and the like): role → the word; a camera/stand/car key there is a target key too
    const walk = (o: unknown, at: string): void => {
      if (!o || typeof o !== 'object') return;
      if (Array.isArray(o)) { o.forEach((x, i) => walk(x, `${at}[${i}]`)); return; }
      for (const [k, v] of Object.entries(o)) {
        if (k === 'who' && v && typeof v === 'object' && !Array.isArray(v)) for (const r of Object.keys(v)) if (/^(camera|stand|car)-/.test(r)) used(r, `text.json ${at}.who`);
        walk(v, at ? `${at}.${k}` : k);
      }
    };
    walk({ debrief: t.debrief, cards: t.cards, less: t.less, briefing: t.briefing }, '');
    const schemaPath = join(dir, 'text.schema.ts');
    if (existsSync(schemaPath)) {
      const w = /export const WATCHED = \[([^\]]*)\]/.exec(readFileSync(schemaPath, 'utf8'));
      for (const k of w ? [...w[1]!.matchAll(/'([^']+)'/g)].map((x) => x[1]!) : []) used(k, 'text.schema.ts WATCHED');
      // the reverse: every site camera is Watchable
      for (const c of s.cameras) if (w && !w[1]!.includes(`'camera-${c}'`)) bad.push(`text.schema.ts WATCHED: site camera '${c}' has no 'camera-${c}'`);
    }
    // the reverse: every camera and stand the site has has words (a site-side rename drops them otherwise)
    for (const c of s.cameras) for (const [sec, o] of [['targets', t.targets], ['names.targets', t.names?.targets]] as const) if (!o?.[`camera-${c}`]) bad.push(`text.json ${sec}: site camera '${c}' has no 'camera-${c}' entry`);
    for (const st of s.stands) if (!t.targets?.[`stand-${st}`] && !t.names?.targets?.[`stand-${st}`]) bad.push(`text.json targets: site getaway stand '${st}' has no 'stand-${st}' entry`);
  }
  const missionPath = join(dir, 'mission.json');
  if (existsSync(missionPath)) {
    const m = read(missionPath);
    for (const c of m.cameras ?? []) if (!s.cameras.has(c.id)) bad.push(`mission.json cameras: '${c.id}' is no site camera (site: ${[...s.cameras].join(', ')})`);
    for (const p of m.points ?? []) if (!s.places.has(p.id)) bad.push(`mission.json points: '${p.id}' is no site place`);
    for (const d of m.destinations ?? []) if (!s.places.has(`d:${d.name}`)) bad.push(`mission.json destinations: '${d.name}' is no site place (d:${d.name})`);
  }
  return bad;
}
