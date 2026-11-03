// The Handover briefing's mission data (contracts/missions/handover/brief.json, suborned.mission-brief/2) against its mission
// (contracts/missions/handover/mission.json): every id the briefing points at must name something real, unknown = reject.
// Waymark, 2026-10-26 (Glyph N1 on build 0.2.122, briefs/041/port-progress.md): target.at is where the briefing's Target
// leader lands, and it must be a mission destination (Vale's official entrance, not the guard's public door).
// Place ids are the host's (HostBriefing.cs Place): "d:<destination name>", and for a way's `at` also "door" or "car".

export type Brief = {
  schema?: unknown;
  mission?: unknown;
  target?: unknown;
  ways?: unknown;
  clock?: unknown;
  camera?: unknown;
};
export type Mission = {
  roles?: { id: string }[];
  destinations?: { name: string }[];
  timeline?: { at: string }[];
};

const isObj = (x: unknown): x is Record<string, unknown> => typeof x === 'object' && x !== null && !Array.isArray(x);
export const CLOCK_IDS = ['start', 'chime', 'over'] as const;

/** Every reason the brief is rejected; [] when it passes. */
export function checkBrief(brief: Brief, mission: Mission, missionId = 'handover'): string[] {
  const bad: string[] = [];
  const roles = new Set((mission.roles ?? []).map((r) => r.id));
  const dests = new Set((mission.destinations ?? []).map((d) => d.name));
  const times = new Set((mission.timeline ?? []).map((t) => t.at));
  const place = (field: string, v: unknown, extra: readonly string[] = []) => {
    if (typeof v !== 'string') { bad.push(`${field}: not a place id`); return; }
    if (extra.includes(v)) return;
    if (!v.startsWith('d:')) { bad.push(`${field}: "${v}" is not a place id (d:<destination>${extra.length ? ' or ' + extra.join(', ') : ''})`); return; }
    if (!dests.has(v.slice(2))) bad.push(`${field}: "${v}" names no mission.json destination`);
  };

  if (brief.schema !== 'suborned.mission-brief/2') bad.push(`schema: "${String(brief.schema)}" is not suborned.mission-brief/2`);
  if (brief.mission !== missionId) bad.push(`mission: "${String(brief.mission)}" is not ${missionId}`);

  const t = brief.target;
  if (!isObj(t)) bad.push('target: must be { role, at }');
  else {
    for (const k of Object.keys(t)) if (k !== 'role' && k !== 'at') bad.push(`target.${k}: unknown key`);
    if (typeof t.role !== 'string' || !roles.has(t.role)) bad.push(`target.role: "${String(t.role)}" names no mission.json role`);
    place('target.at', t.at);
  }

  if (!Array.isArray(brief.ways) || brief.ways.length === 0) bad.push('ways: must list at least one way in');
  else brief.ways.forEach((w, i) => {
    if (!isObj(w)) { bad.push(`ways[${i}]: not an object`); return; }
    if (typeof w.id !== 'string' || !w.id) bad.push(`ways[${i}].id: missing`);
    if (typeof w.contact !== 'string' || !roles.has(w.contact)) bad.push(`ways[${i}].contact: "${String(w.contact)}" names no mission.json role`);
    if (!Array.isArray(w.steps) || w.steps.length > 3 || w.steps.some((s) => typeof s !== 'string' || !s)) bad.push(`ways[${i}].steps: up to three icon names`);
    if (w.at !== undefined) place(`ways[${i}].at`, w.at, ['door', 'car']);
  });

  if (brief.clock !== undefined) {
    if (!Array.isArray(brief.clock)) bad.push('clock: not a list');
    else brief.clock.forEach((c, i) => {
      if (!isObj(c)) { bad.push(`clock[${i}]: not an object`); return; }
      if (!CLOCK_IDS.includes(c.id as never)) bad.push(`clock[${i}].id: "${String(c.id)}" is not ${CLOCK_IDS.join(' | ')}`);
      if (c.at !== undefined && (typeof c.at !== 'string' || !times.has(c.at))) bad.push(`clock[${i}].at: "${String(c.at)}" is not a mission.json timeline mark`);
    });
  }

  if (brief.camera !== undefined) {
    if (!isObj(brief.camera)) bad.push('camera: not an object');
    else place('camera.place', brief.camera.place);
  }
  return bad;
}
