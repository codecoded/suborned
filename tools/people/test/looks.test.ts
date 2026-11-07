// Who wears which figure (Pulse, brief 067 I): tools/people/looks.ts replays the host's choice of a figure for every person; this holds the rules the mapping is held to (only guards wear
// the duty suit; every key has a figure; the crew's looks are the crew's) and pins the replay to the host's own source, so a change to HostViews*.cs or the hover's names fails here and the
// audit is made again, not trusted. The host's views are listed call by call: a new place that dresses a view is a place the audit does not replay, and fails the list.
import { describe, expect, test } from 'bun:test';
import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { bodyFigure, CREW_KEYS, CREW_POOL, crewPlan, crowdLabel, dressCrowd, faults, famOf, fnv, hostileFigure, klassOf, loadMapping, loadPeople, modelOf, personFigure, personFigureOf, poolOf, rangeBystanderFigure, RIVAL_LOOKS, rivalFigure, roleFigure, SECURITY_JOBS, sharedModels } from '../looks';

const root = join(import.meta.dir, '..', '..', '..');
const play = join(root, 'unity/Assets/Suborned/Runtime/Play');
const src = (f: string) => readFileSync(join(root, f), 'utf8');
const host = readFileSync(join(play, 'HostViews.Figures.cs'), 'utf8'), views = readFileSync(join(play, 'HostViews.cs'), 'utf8');
const allViews = readdirSync(play).filter(f => /^HostViews.*\.cs$/.test(f)).sort().map(f => readFileSync(join(play, f), 'utf8')).join('\n');
const hostUi = readFileSync(join(play, 'HandoverHost.cs'), 'utf8');
const library = src('unity/Assets/Suborned/Runtime/Play/FigureLibrary.cs'), build = src('unity/Assets/Suborned/Editor/World/People/FigureBuild.cs');
const model = src('sim/Suborned.Sim/Model.cs');
const m = loadMapping(), { walkers, cast } = loadPeople(), crowd = dressCrowd(m, walkers);

/** A C# source without its comments (a string or char literal is kept whole: a `//` inside one is not a comment). */
function code(s: string): string {
  let out = '', i = 0;
  while (i < s.length) {
    const c = s[i]!;
    if (c === '/' && s[i + 1] === '/') { while (i < s.length && s[i] !== '\n') i++; continue; }
    if (c === '/' && s[i + 1] === '*') { const e = s.indexOf('*/', i + 2); i = e < 0 ? s.length : e + 2; continue; }
    if (c === '"' || c === "'") { out += c; i++; while (i < s.length && s[i] !== c) { if (s[i] === '\\') { out += s[i]!; i++; } out += s[i] ?? ''; i++; } out += c; i++; continue; }
    out += c; i++;
  }
  return out;
}
/** The arguments, as written, of every call `name(…)` in the code of a C# source (a declaration, `void name(` or `string name(`, is not a call), whitespace made one space, sorted. */
function calls(s: string, name: string): string[] {
  const text = code(s), out: string[] = [], re = new RegExp(`(?<![\\w.])${name.replace(/\./g, '\\.')}\\(`, 'g');
  for (let hit: RegExpExecArray | null; (hit = re.exec(text)); ) {
    if (/\b(void|string|bool|int)\s+$/.test(text.slice(0, hit.index))) continue;
    let depth = 1, j = hit.index + hit[0].length, quote = ''; const from = j;
    for (; j < text.length && depth > 0; j++) {
      const c = text[j]!;
      if (quote) { if (c === '\\') j++; else if (c === quote) quote = ''; continue; }
      if (c === '"' || c === "'") quote = c; else if (c === '(') depth++; else if (c === ')') depth--;
    }
    out.push(text.slice(from, j - 1).replace(/\s+/g, ' ').trim());
  }
  return out.sort();
}

describe('the replay is the host\'s choice (each line of it pinned to the source)', () => {
  test('RoleFigure: the cell, the cast, the response and the rivals have the figures the audit says', () => {
    expect(host).toContain('"op/scout" => "scout", "op/fixer" => "fixer", "op/tech" => "tech", "op/enforcer" => "enforcer",');
    expect(host).toContain('"waitress" => "waitress", "guard" => "guard", "driver" => "driver", "councillor" => "councillor",');
    expect(host).toContain('"patrol" => null, _ when key.StartsWith("security/") || key.StartsWith("unit/") => "security", _ when IsCrewId(key) || key.StartsWith("body/rival-") => RivalFigure(key),');
    expect(roleFigure('patrol')).toBeNull();
    expect(roleFigure('unit/police-3')).toBe('security'); expect(roleFigure('security/1')).toBe('security');
    expect(roleFigure('rival-4')).toBe('rival_c'); expect(roleFigure('body/rival-2')).toBe('rival_b'); expect(roleFigure('crowd/7')).toBeNull();
    expect(views).toContain('foreach (var k in new List<string>(things.Keys)) if (RoleFigure(k) is string fid) Dress(k, fid);');
    // the crew's five views are made (RivalViewsInit) before the fixed keys are dressed, so each is dressed `rival` at the start
    const rivals = src('unity/Assets/Suborned/Runtime/Play/HostViews.Rivals.cs');
    expect(rivals).toContain('public static readonly string[] RivalKeys = { "rival-1", "rival-2", "rival-3", "rival-4", "rival-5" };');
    expect(rivals).toContain('foreach (var k in RivalKeys) Person(k, host.rivalBodyColour);');
    expect(views.indexOf('RivalViewsInit();')).toBeGreaterThan(0); expect(views.indexOf('RivalViewsInit();')).toBeLessThan(views.indexOf('RoleFigure(k) is string fid'));
    expect(CREW_KEYS).toEqual(['rival-1', 'rival-2', 'rival-3', 'rival-4', 'rival-5']);
  });
  test('RivalFigure: the crew is dressed by member number, never by hash (Forma 067 W1): the table, the number read from the id, the fallback, the women\'s walk', () => {
    expect(host).toContain('static readonly string[] RivalLooks = { "rival", "rival_b", "rival_d", "rival_c", "rival" };');
    expect(host).toContain('int i = id?.Length ?? 0; while (i > 0 && char.IsDigit(id[i - 1])) i--;');
    expect(host).toContain('int n = i < (id?.Length ?? 0) && int.TryParse(id[i..], out var v) ? v : 1;');
    expect(host).toContain('return RivalLooks[(Mathf.Max(n, 1) - 1) % RivalLooks.Length];');
    expect(RIVAL_LOOKS).toEqual(['rival', 'rival_b', 'rival_d', 'rival_c', 'rival']);
    expect(CREW_KEYS.map(k => rivalFigure(k))).toEqual(RIVAL_LOOKS); expect(CREW_KEYS.map(k => rivalFigure('body/' + k))).toEqual(RIVAL_LOOKS);
    expect([rivalFigure('rival-9'), rivalFigure('rival-12'), rivalFigure('rival-0'), rivalFigure('rival'), rivalFigure('')]).toEqual(['rival_c', 'rival_b', 'rival', 'rival', 'rival']); // wraps at 5; no digits or zero is 1
    expect(new Set(RIVAL_LOOKS)).toEqual(new Set(m.pools.get(CREW_POOL))); // the five wear every look of the pool, and no look outside it
    expect(allViews).toContain('if (lib.Find(id) == null && id.StartsWith("rival_")) id = "rival";'); // a library without the look dresses `rival`
    expect(src('unity/Assets/Suborned/Editor/World/People/FigureBuild.cs')).toMatch(/static readonly HashSet<string> Women = new\(\) \{[^}]*"rival_d"/); // the woman walks walk_f
    expect(calls(allViews, 'RivalFigure')).toEqual(['h.Id', 'h.Id', 'key', 'p.Id']); // the fight's and the range's hostiles, RoleFigure, a rival's body: the four sites
  });
  test('the crowd faces the way the engine says (engine 0.34.0, Game.FacingNow): one standing alone, after the spread\'s knots; the figure and the cone agree', () => {
    expect(views).toContain('if (SpreadFace(i) is { } sf) faces[crowdKeys[i]] = sf;');
    expect(views).toContain('else if (StillFacing(g, i) is { } ef) faces[crowdKeys[i]] = ef;');
    expect(views).toContain('if (!things.TryGetValue(crowdKeys[i], out var th)) return null; var d = th.to - th.from; d.y = 0f; if (d.sqrMagnitude > 1e-4f) return null;');
    expect(views).toContain('try { f = g.FacingNow(g.Walkers[i].P.Id); } catch (System.Exception) { return null; }');
    expect(views).toContain('return f is Pt p && (p.X != 0 || p.Y != 0) ? frame.Dir(p) : null;');
    expect(src('sim/Suborned.Sim/GameNotice.cs')).toContain('public Pt? FacingNow(string id) => PersonById(id) is Person p ? FacingOf(p) : null;');
  });
  test('PersonFigure: a person\'s job gives a family, the family a pool, the person\'s id the figure', () => {
    expect(host).toContain('string fam = job is "server" or "cook" or "waitress" ? "cafe" : job is "clerk" or "attendant" or "councillor" ? "office" : job == "guard" ? "guard" : job is "driver" or "official-driver" ? "driver" : "passer";');
    expect(host).toContain('return lib.Pick("job/" + job, p.Id + salt) ?? lib.Pick("family/" + fam, p.Id + salt);');
    expect(host).toContain('if (p.Role.Length > 0 && RoleFigure(p.Role) is string r) return r;');
    expect([famOf('server'), famOf('clerk'), famOf('guard'), famOf('official-driver'), famOf('keeper'), famOf('resident')]).toEqual(['cafe', 'office', 'guard', 'driver', 'passer', 'passer']);
    expect(library).toContain('uint h = 2166136261; foreach (var c in seed ?? "") { h ^= c; h *= 16777619; }');
    expect(library).toContain('return ids[h % (uint)ids.Length];');
    expect(fnv('')).toBe(2166136261); expect(fnv('a')).toBe(0xe40c292c); // FNV-1a 32 of "a"
    expect(personFigureOf(m, { id: 'p-x', job: 'server', role: 'waitress' })).toBe('waitress'); expect(personFigureOf(m, { id: 'p-x', job: 'clerk' })).toBe(personFigure(m, 'p-x', 'clerk'));
  });
  test('the pools are the manifest\'s: `family/<first word>` for each family, each job block as written (the crew\'s `role/rival` among them, drawn from by no job)', () => {
    expect(build).toContain('keys.Add("family/" + kv.Key.Split(\' \')[0])');
    expect(build).toContain('foreach (var kv in J.O(J.Get(m, "jobBlocks"))) { keys.Add(kv.Key);');
    expect([...m.pools.keys()].filter(k => k.startsWith('family/')).sort()).toEqual(['family/cafe', 'family/driver', 'family/guard', 'family/office', 'family/passer']);
    expect(poolOf(m, 'resident')).toBe('job/resident'); expect(poolOf(m, 'clerk')).toBe('family/office'); expect(poolOf(m, 'keeper')).toBe('family/passer');
    expect(m.pools.has(CREW_POOL)).toBe(true); expect(poolOf(m, 'rival')).toBe('family/passer'); // the engine's crew job is `rival`: a job with no pool of its own, so nothing draws a crew member from the crowd's pools by job; the host never asks (below)
    expect(calls(allViews, 'RoleFigure')).toEqual(['k', 'p.Role']); expect(code(allViews)).not.toContain(CREW_POOL);
  });
  test('DressCrowd: a re-pick (`#1`..`#7`) stays in the person\'s own pool, so a clerk is never a guard and a guard never a clerk', () => {
    expect(host).toContain('string grp = w.Place.Length > 0 && w.From.Length > 0 ? w.Place[0] + "@" + w.From[0] : "";');
    expect(host).toContain('for (int t = 1; t < 8 && fid != null && grp.Length > 0 && set.Contains(fid); t++) fid = PersonFigure(w.P, "#" + t);');
    expect(crowd.length).toBe(walkers.length);
    for (const c of crowd) { const pool = m.pools.get(c.pool)!; expect(pool, `${c.id} ${c.figure}`).toContain(c.figure); expect(pool).toContain(c.first); }
    // the group is the day's first stop, which everyone who lives at one home shares at 0:00, so most re-picks only shuffle a job's own few looks (the office pool has two)
    expect(crowd.filter(c => c.figure !== c.first).length).toBeGreaterThan(0);
  });
  test('every place the host dresses a view is one the audit replays: a new `Dress(` or figure pick fails this list', () => {
    expect(calls(allViews, 'Dress')).toEqual([
      'crowdKeys[i], fid',                                                // DressCrowd (HostViews.Figures.cs): the walkers
      'crowdKeys[i], PersonFigure(w.P)',                                  // GrowCrowd (HostViews.Traffic.cs, brief 098 step 1): a passing car's driver who got out, the plain pick by job
      'hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security"', // the fight's hostiles: the range's rivals and the response's officers (two sites: the range, the Handover), each rival in his number's look
      'hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security"',
      'k, fid',                                                           // the fixed keys, RoleFigure
      'key, RivalFigure(p.Id)',                                           // a crew member's body
      'key, "security"',                                                  // a response unit's officer
      'key, BodyFigure(p)',                                               // any other body
      'key, host.figures.Pick("family/passer", b.Id)',                    // the range's bystanders
    ].sort());
    expect(calls(allViews, 'lib.Pick')).toEqual(['"family/" + fam, p.Id + salt', '"job/" + job, p.Id + salt']);
    expect(calls(allViews, 'host.figures.Pick')).toEqual(['"family/passer", b.Id']);
    expect(calls(allViews, 'figures.Find').concat(calls(allViews, 'lib.Find'))).toEqual(['id', 'id']); // the one place a figure is looked up by name: Dress (twice: the fallback for a library without a crew look, then the lookup)
  });
  test('the response, the rivals, the bodies and the range\'s passers are dressed as the audit says', () => {
    const hostile = 'Dress(hostileKeys[i], h.Id.StartsWith("rival") ? RivalFigure(h.Id) : "security")';
    expect(views.split(hostile).length - 1).toBe(2);
    expect(hostileFigure('rival-9')).toBe('rival_c'); expect(hostileFigure('rival-3')).toBe('rival_d'); expect(hostileFigure('security-1')).toBe('security'); expect(hostileFigure('hostile-0')).toBe('security');
    expect(views).toContain('if (IsCrewId(p.Id)) { Person(key, host.rivalBodyColour); Dress(key, RivalFigure(p.Id)); }'); // a crew member's body is his own look
    expect(views).toContain('else { Person(key, host.securityColour); Dress(key, "security"); }'); // a response unit's officer
    expect(views).toContain('if (host.figures != null) Dress(key, host.figures.Pick("family/passer", b.Id));');
    // brief 098 step 1: a driver who gets out of a passing car joins the crowd mid-game (the engine's walker, job `visitor` from contracts/rules/population.json
    // traffic.driver), dressed by the plain pick: no group to re-pick against, as nobody sets out with them. A visitor has no pool of its own: a passer-by's look
    expect(src('unity/Assets/Suborned/Runtime/Play/HostViews.Traffic.cs')).toContain('if (!things.ContainsKey(crowdKeys[i])) { Person(crowdKeys[i], Family(w.P.Job), false, true, Shape(w.P.Job)); Dress(crowdKeys[i], PersonFigure(w.P)); }');
    expect(poolOf(m, 'visitor')).toBe('family/passer');
    for (const id of ['x-driver-01', 'x-driver-02', 'x-driver-03']) expect(m.pools.get('family/passer'), id).toContain(personFigure(m, id, 'visitor')!);
    for (const id of ['b-1', 'b-2', 'b-3', 'b-4', 'b-5', 'b-6']) expect(m.pools.get('family/passer'), id).toContain(rangeBystanderFigure(m, id)!);
  });
  test('BodyFigure: a body wears the figure its walker was drawn in, so nobody changes clothes when knocked out', () => {
    expect(views).toContain('else { Person(key, Family(job), shape: Shape(job)); Dress(key, BodyFigure(p)); }');
    expect(views).toContain('string BodyFigure(Person p) => walkerIndex.TryGetValue(p, out var bw) && bw < crowdKeys.Length && figs.TryGetValue(crowdKeys[bw], out var wf) ? wf.id : PersonFigure(p);');
    expect(views).toContain('if (walkerIndex.Count == 0) for (int i = 0; i < g.Walkers.Count; i++) walkerIndex[g.Walkers[i].P] = i;'); // the walkers are indexed before the first body is dressed
    const repicked = crowd.filter(c => c.figure !== c.first);
    expect(repicked.length).toBeGreaterThan(0); // the people the plain pick would have changed: DressCrowd gave them another figure than their own
    for (const c of crowd) expect(bodyFigure(m, crowd, { id: c.id, job: c.job }), c.id).toBe(c.figure);
    for (const c of repicked) expect(personFigureOf(m, { id: c.id, job: c.job }), c.id).not.toBe(c.figure);
    // the cast by their role, a crew member as the crew, anyone not drawn by their plain pick
    expect(bodyFigure(m, crowd, { id: 'p-x', job: 'server', role: 'waitress' })).toBe('waitress'); expect(bodyFigure(m, crowd, { id: 'p-y', job: 'guard', role: 'guard' })).toBe('guard');
    expect(bodyFigure(m, crowd, { id: 'p-z', job: 'official-driver', role: 'driver' })).toBe('driver'); expect(bodyFigure(m, crowd, { id: 'p-v', job: 'councillor', role: 'councillor' })).toBe('councillor');
    expect(bodyFigure(m, crowd, { id: 'rival-3', job: 'rival' })).toBe('rival_d'); expect(bodyFigure(m, crowd, { id: 'rival-2', job: 'rival' })).toBe('rival_b'); // a crew member's body is the look he stood in
    expect(bodyFigure(m, crowd, { id: 'p-nobody', job: 'clerk' })).toBe(personFigure(m, 'p-nobody', 'clerk'));
  });
  test('the hover names a crowd person by Person.Civilian: "a guard" for a guard, "an officer" for any other security job, "a passer-by" for everyone else (Latch, on the audit\'s W2)', () => {
    expect(hostUi).toContain('string CrowdSecurityName(Person p) => p.Job == "guard" ? Text?.TargetName("crowd-guard", "a guard") ?? "a guard" : Text?.TargetName("crowd-officer", "an officer") ?? "an officer";');
    expect(hostUi).toContain('bool sec = cp != null && !cp.Civilian; return new Target { Kind = "crowd", Key = n, Label = sec ? CrowdSecurityName(cp) : "a passer-by", Id = cp?.Id ?? "", Security = sec };');
    expect(crowdLabel('guard')).toBe('a guard'); expect(crowdLabel('investigator')).toBe('an officer'); expect(crowdLabel('clerk')).toBe('a passer-by'); expect(crowdLabel('keeper')).toBe('a passer-by');
    // the look and the name agree: everyone drawn in the duty suit is named a guard, and nobody else is
    for (const c of crowd) expect(crowdLabel(c.job) !== 'a passer-by', `${c.id} (${c.job}) wears ${c.figure}`).toBe(modelOf(m, c.figure) === 'guard');
  });
  test('the engine\'s security jobs are the ones the audit holds to the duty suit', () => {
    expect(model).toContain('public bool Civilian => Job != "guard" && Job != "patrol-officer" && Job != "investigator" && Job != "watcher" && Role != "guard" && Job != RivalJob;');
    expect(SECURITY_JOBS).toEqual(['guard', 'patrol-officer', 'investigator', 'watcher']);
  });
});

describe('the rules', () => {
  test('L-1, L-2 and L-4: only guards wear the duty suit; every key and every pool names a figure that exists; the crew\'s looks are the crew\'s', () => {
    expect(faults(m, crowd)).toEqual([]);
    for (const c of crowd) expect(modelOf(m, c.figure) === 'guard', `${c.id} (${c.job}) wears ${c.figure}`).toBe(SECURITY_JOBS.includes(c.job));
    expect(crowd.filter(c => c.job === 'guard').length).toBe(15); // the Office's other guards: 16 less the door guard, who is played by his role
    expect(crowd.filter(c => c.job === 'clerk').length).toBe(58);
  });
  test('the cast people are played by their role: waitress, guard, driver, councillor; the figures are theirs alone', () => {
    expect([...cast.values()].sort()).toEqual(['councillor', 'driver', 'guard', 'waitress']);
    for (const r of ['waitress', 'guard', 'driver', 'councillor']) expect(klassOf(m, roleFigure(r)!)).toBe(r === 'guard' ? 'security' : 'cast');
    for (const c of crowd) expect(['waitress', 'driver', 'councillor']).not.toContain(c.figure);
  });
  test('a rule fails on its example: a civilian pool holding the duty suit, a clerk mapped to it, a key with no figure', () => {
    const bad = { ...m, pools: new Map(m.pools).set('family/passer', ['c6', 'security']) };
    expect(faults(bad, crowd).some(f => f.rule === 'L-1' && /family\/passer holds security/.test(f.what))).toBe(true);
    const clerk = crowd.find(c => c.job === 'clerk')!;
    expect(faults(m, [{ ...clerk, figure: 'guard' }]).some(f => f.rule === 'L-1' && /clerk\) wears guard/.test(f.what))).toBe(true);
    const gone = { ...m, figures: new Map([...m.figures].filter(([id]) => id !== 'councillor')) };
    expect(faults(gone, crowd).some(f => f.rule === 'L-2' && /councillor/.test(f.what))).toBe(true);
    expect(personFigure({ pools: new Map() }, 'p-x', 'guard')).toBeNull();
  });
  test('L-4 fails on its examples: a crew look in a passers\' pool, on a clerk, as a recolour of the duty suit, outside its pool, or no pool at all', () => {
    const has = (fs: ReturnType<typeof faults>, re: RegExp) => fs.some(f => f.rule === 'L-4' && re.test(f.what));
    const clerk = crowd.find(c => c.job === 'clerk')!, crew = m.pools.get(CREW_POOL)!;
    expect(has(faults({ ...m, pools: new Map(m.pools).set('family/passer', [...m.pools.get('family/passer')!, 'rival_b']) }, crowd), /family\/passer holds rival_b, a look of the crew/)).toBe(true);
    expect(has(faults(m, [{ ...clerk, figure: 'rival_c' }]), /clerk\) wears rival_c, a look of the crew/)).toBe(true);
    expect(has(faults({ ...m, variantOf: { ...m.variantOf, rival_c: 'guard' } }, crowd), /rival_c is a recolour of the duty suit/)).toBe(true);
    expect(has(faults({ ...m, figures: new Map(m.figures).set('rival_z', { id: 'rival_z', role: 'rival', label: 'x' }) }, crowd), /rival_z is a rival figure outside role\/rival/)).toBe(true);
    expect(has(faults({ ...m, pools: new Map([...m.pools].filter(([k]) => k !== CREW_POOL)) }, crowd), /no pool role\/rival/)).toBe(true);
    expect(has(faults({ ...m, pools: new Map(m.pools).set(CREW_POOL, [...crew, 'd5c']) }, crowd), /role\/rival holds d5c, whose role is townsperson/)).toBe(true);
    expect(has(faults({ ...m, pools: new Map(m.pools).set(CREW_POOL, ['rival_b', 'rival_c']) }, crowd), /rival-1 is drawn as rival, which is not in role\/rival/)).toBe(true); // the host's look for a member must be one of the pool's
  });
  test('L-3, the models role figures share with the crowd, named: nobody but the driver wears Commuter D2 (brief 067 took it out of the passers\' pools), the rival crew is Commuter D4 in three violet colourways and Commuter C6 in a fourth (brief 067 W1, Forma) and the officer the guard', () => {
    const by = Object.fromEntries(sharedModels(m).map(x => [x.role, x]));
    expect(Object.keys(by).sort()).toEqual(['driver', 'rival', 'rival_b', 'rival_c', 'rival_d', 'security']); // (the councillor was listed as a recolour of D4 until brief 067 dropped the stale entry: Vale C has his own model, brief 063)
    expect(by.driver).toMatchObject({ model: 'd2m', crowd: [] });
    expect(by.security).toMatchObject({ model: 'guard', crowd: ['family/guard'] });
    // the rivals left Worker D5 (hard hat, amber trim, a dark uniform: the police to the eye) for D4's body, which the clerks draw too, and C6's, which three residents draw, so the colour alone tells a rival from a clerk: violet and pale, no cap, no gold
    for (const id of ['rival', 'rival_b', 'rival_c']) { expect(by[id]!.model, id).toBe('d4'); expect(by[id]!.crowd, id).toContain('family/office'); }
    expect(by.rival_d!.model).toBe('c6'); expect(by.rival_d!.crowd).toContain('family/passer');
    expect(crowd.filter(c => c.figure === 'd2m')).toEqual([]); // no keeper and no resident is in the driver's uniform and visor helmet
    expect(crowd.filter(c => ['rival', 'rival_b', 'rival_c', 'rival_d'].includes(c.figure))).toEqual([]); // and no passer-by is in the crew's violet: the pool role/rival is the crew's alone
  });
  test('the crew\'s table by member (roles.json playViews, Forma) is the host\'s RivalLooks: four looks for five people, three different ones for the three posted, the lookout and the wheelman not the same, and every member the same alive, down and in a fight', () => {
    const plan = crewPlan(m), pool = m.pools.get(CREW_POOL)!;
    expect(Object.keys(plan)).toEqual(CREW_KEYS);
    for (const k of CREW_KEYS) expect(pool, k).toContain(plan[k]);
    expect(CREW_KEYS.map(k => plan[k])).toEqual(RIVAL_LOOKS);
    expect(new Set(['rival-1', 'rival-2', 'rival-3'].map(k => plan[k])).size).toBe(3);
    expect(new Set(CREW_KEYS.map(k => roleFigure(k))).size).toBe(4); expect(roleFigure('rival-4')).not.toBe(roleFigure('rival-5'));
    for (const k of CREW_KEYS) { expect(roleFigure(k), k).toBe(plan[k]); expect(roleFigure('body/' + k), k).toBe(plan[k]); expect(hostileFigure(k), k).toBe(plan[k]); }
    expect(m.figures.get('rival_d')!.role).toBe('rival'); expect(modelOf(m, 'rival_d')).toBe('c6'); // the woman, on the body the residents draw: she walks walk_f (FigureBuild.Women)
    for (const k of CREW_KEYS) expect(['security', 'guard']).not.toContain(roleFigure(k)); // none reads as police: not the duty suit's model, nor in its pool
  });
});
