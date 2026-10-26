// Who wears which figure (Pulse, brief 067 I): tools/people/looks.ts replays the host's choice of a figure for every person; this holds the rules the mapping is held to (only guards wear
// the duty suit; every key has a figure) and pins the replay to the host's own source, so a change to HostViews.Figures.cs fails here and the audit is made again, not trusted.
import { describe, expect, test } from 'bun:test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { dressCrowd, faults, famOf, fnv, klassOf, loadMapping, loadPeople, modelOf, personFigure, poolOf, roleFigure, SECURITY_JOBS, sharedModels } from '../looks';

const root = join(import.meta.dir, '..', '..', '..');
const host = readFileSync(join(root, 'unity/Assets/Suborned/Runtime/Play/HostViews.Figures.cs'), 'utf8'), views = readFileSync(join(root, 'unity/Assets/Suborned/Runtime/Play/HostViews.cs'), 'utf8');
const library = readFileSync(join(root, 'unity/Assets/Suborned/Runtime/Play/FigureLibrary.cs'), 'utf8'), build = readFileSync(join(root, 'unity/Assets/Suborned/Editor/World/People/FigureBuild.cs'), 'utf8');
const model = readFileSync(join(root, 'sim/Suborned.Sim/Model.cs'), 'utf8');
const m = loadMapping(), { walkers, cast } = loadPeople(), crowd = dressCrowd(m, walkers);

describe('the replay is the host\'s choice (each line of it pinned to the source)', () => {
  test('RoleFigure: the cell, the cast, the response and the rivals have the figures the audit says', () => {
    expect(host).toContain('"op/scout" => "scout", "op/fixer" => "fixer", "op/tech" => "tech", "op/enforcer" => "enforcer",');
    expect(host).toContain('"waitress" => "waitress", "guard" => "guard", "driver" => "driver", "councillor" => "councillor",');
    expect(host).toContain('"patrol" => null, _ when key.StartsWith("security/") || key.StartsWith("unit/") => "security", _ when IsCrewId(key) || key.StartsWith("body/rival-") => "rival",');
    expect(roleFigure('patrol')).toBeNull();
    expect(roleFigure('unit/police-3')).toBe('security'); expect(roleFigure('security/1')).toBe('security');
    expect(roleFigure('rival-4')).toBe('rival'); expect(roleFigure('body/rival-2')).toBe('rival'); expect(roleFigure('crowd/7')).toBeNull();
  });
  test('PersonFigure: a person\'s job gives a family, the family a pool, the person\'s id the figure', () => {
    expect(host).toContain('string fam = job is "server" or "cook" or "waitress" ? "cafe" : job is "clerk" or "attendant" or "councillor" ? "office" : job == "guard" ? "guard" : job is "driver" or "official-driver" ? "driver" : "passer";');
    expect(host).toContain('return lib.Pick("job/" + job, p.Id + salt) ?? lib.Pick("family/" + fam, p.Id + salt);');
    expect(host).toContain('if (p.Role.Length > 0 && RoleFigure(p.Role) is string r) return r;');
    expect([famOf('server'), famOf('clerk'), famOf('guard'), famOf('official-driver'), famOf('keeper'), famOf('resident')]).toEqual(['cafe', 'office', 'guard', 'driver', 'passer', 'passer']);
    expect(library).toContain('uint h = 2166136261; foreach (var c in seed ?? "") { h ^= c; h *= 16777619; }');
    expect(library).toContain('return ids[h % (uint)ids.Length];');
    expect(fnv('')).toBe(2166136261); expect(fnv('a')).toBe(0xe40c292c); // FNV-1a 32 of "a"
  });
  test('the pools are the manifest\'s: `family/<first word>` for each family, each job block as written', () => {
    expect(build).toContain('keys.Add("family/" + kv.Key.Split(\' \')[0])');
    expect(build).toContain('foreach (var kv in J.O(J.Get(m, "jobBlocks"))) { keys.Add(kv.Key);');
    expect([...m.pools.keys()].filter(k => k.startsWith('family/')).sort()).toEqual(['family/cafe', 'family/driver', 'family/guard', 'family/office', 'family/passer']);
    expect(poolOf(m, 'resident')).toBe('job/resident'); expect(poolOf(m, 'clerk')).toBe('family/office'); expect(poolOf(m, 'keeper')).toBe('family/passer');
  });
  test('DressCrowd: a re-pick (`#1`..`#7`) stays in the person\'s own pool, so a clerk is never a guard and a guard never a clerk', () => {
    expect(host).toContain('string grp = w.Place.Length > 0 && w.From.Length > 0 ? w.Place[0] + "@" + w.From[0] : "";');
    expect(host).toContain('for (int t = 1; t < 8 && fid != null && grp.Length > 0 && set.Contains(fid); t++) fid = PersonFigure(w.P, "#" + t);');
    expect(crowd.length).toBe(walkers.length);
    for (const c of crowd) { const pool = m.pools.get(c.pool)!; expect(pool, `${c.id} ${c.figure}`).toContain(c.figure); expect(pool).toContain(c.first); }
    // the group is the day's first stop, which everyone who lives at one home shares at 0:00, so most re-picks only shuffle a job's own few looks (the office pool has two)
    expect(crowd.filter(c => c.figure !== c.first).length).toBeGreaterThan(0);
  });
  test('the response, the rivals and the bodies are dressed as the audit says', () => {
    expect(views).toContain('Dress(hostileKeys[i], h.Id.StartsWith("rival") ? "rival" : "security")');
    expect(views).toContain('else { Person(key, Family(job), shape: Shape(job)); Dress(key, PersonFigure(p)); }'); // a body takes the plain pick: no DressCrowd salt
    expect(views).toContain('if (IsCrewId(p.Id)) { Person(key, host.rivalBodyColour); Dress(key, "rival"); }');
    expect(views).toContain('else { Person(key, host.securityColour); Dress(key, "security"); }'); // a response unit's officer
  });
  test('the engine\'s security jobs are the ones the audit holds to the duty suit', () => {
    expect(model).toContain('public bool Civilian => Job != "guard" && Job != "patrol-officer" && Job != "investigator" && Job != "watcher" && Role != "guard" && Job != RivalJob;');
    expect(SECURITY_JOBS).toEqual(['guard', 'patrol-officer', 'investigator', 'watcher']);
  });
});

describe('the rules', () => {
  test('L-1 and L-2: only guards wear the duty suit; every key and every pool names a figure that exists', () => {
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
  test('L-3, the models role figures share with the crowd, named: nobody but the driver wears Commuter D2 (brief 067 took it out of the passers\' pools), the rival is still Worker D5 and the officer the guard', () => {
    const by = Object.fromEntries(sharedModels(m).map(x => [x.role, x]));
    expect(Object.keys(by).sort()).toEqual(['driver', 'rival', 'security']); // (the councillor was listed as a recolour of D4 until brief 067 dropped the stale entry: Vale C has his own model, brief 063)
    expect(by.driver).toMatchObject({ model: 'd2m', crowd: [] });
    expect(by.security).toMatchObject({ model: 'guard', crowd: ['family/guard'] });
    expect(by.rival!.model).toBe('workers_d5'); expect(by.rival!.crowd).toContain('family/passer'); // the open one: Forma gives the rival a figure of his own
    expect(crowd.filter(c => c.figure === 'd2m')).toEqual([]); // no keeper and no resident is in the driver's uniform and visor helmet
  });
});
