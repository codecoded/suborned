// Retention policy for out/, the ignored images in briefs/ and Unity's logs (brief 050). Pure but for reading the tree it is handed and, for engine builds,
// asking git whether a commit exists. Every rule names what it finds, how old it must be, and what keeps it; nothing here deletes (cli.ts does, with --yes).
//
// Never pruned, whatever a rule says:
//   pinned    a tracked file under runs/, contracts/ or sim/data names the path (a recorded run, a contract) or an engine commit (a recorded mission run replays through it)
//   KEEP      a file called KEEP in the folder or any folder above it (inside out/ or briefs/)
//   tracked   git tracks a file inside it
//   hot       something in it changed in the last 24 hours (an agent may be using it)
//   live      the built player out/build/play/The Suborned.app and its proof, the batch's own folders, the plan/world/kit pipeline outputs
import { basename } from 'node:path';
import { DAY, human, type Node } from './scan';
import { touching, type Ref } from './refs';

export type Kind =
  | 'agent-scratch' | 'brief-scratch' | 'model-run' | 'decokit' | 'concept' | 'reference-copy' | 'setup' | 'build' | 'missions' | 'sim' | 'batch' | 'shots'
  | 'pipeline' | 'log' | 'loose' | 'other';

const AGENTS = 'aster|datum|fable|forma|glint|glyph|keel|latch|motive|pulse|redline|splice|suture|vantage|vela|waymark|chime|wm';
const AGENT_RE = new RegExp(`^(${AGENTS})(\\d+[a-z]?)?([-_.].*)?$`);

/** What a top-level entry of out/ is, and who makes it. First match wins. */
export function classify(name: string): { kind: Kind; maker: string } {
  const t = (kind: Kind, maker: string) => ({ kind, maker });
  if (name === 'batch') return t('batch', 'bun run batch (tools/game/batch.ts): step logs, resolved shot lists, late-fight runs; shots are rewritten each batch');
  if (/^batch-.*\.log$/.test(name) || /^batch-run/.test(name)) return t('log', 'an agent teeing one batch to a file (batch-<brief><letter>.log)');
  if (name === 'build') return t('build', 'bun run game:build / batch game-build: the player, its zips and proof shots; perf = SubornedPerf builds');
  if (name === 'missions') return t('missions', 'tools/missions/check: engine builds per commit (git archive), play temp files, composed routes');
  if (/^sim(-|$)/.test(name) || name === 'bench-sim') return t('sim', 'tools/sim/unity.ts and the batch match step: the engine inside Unity, scenario results');
  if (name === 'shots') return t('shots', 'agents\' own screenshot rounds (not the batch\'s)');
  if (name === 'concepts') return t('concept', 'concept art runs (style contract concept_prompt)');
  if (/^decokit/.test(name)) return t('decokit', 'bun run decokit (Blender DecoKit pieces and proposals)');
  if (/-(pistol|carbine)|^(pale-carbine|enforcer-|retro-pistol|reference-(mesh|texture)|people-models|scout)/.test(name)) return t('model-run', 'model / texture generation run (Forma, Vela): versioned vN folders with a Package.zip each');
  if (name === 'scratch') return t('agent-scratch', 'the scratch convention: out/scratch/<agent>/<what> (Aster\'s rule in CLAUDE.md)');
  if (name === 'old-world') return t('reference-copy', 'copy of the old monorepo\'s Unity project, captured for look reference (Latch, Oct 19-20)');
  if (/^mac-studio/.test(name)) return t('setup', 'Mac Studio setup and transfer logs');
  if (/^\d{3}$/.test(name)) return t('brief-scratch', 'per-brief screenshot folder written by hand or script (out/<brief>/)');
  if (/^(b|bt|t|v|wm|work)\d{2,3}/.test(name) || /^(less|lovers|idle|loiter-probe|chase-frames|resume|history|play-guard|asphalt-probe|palette|send|gdd\d*)/.test(name)) return t('brief-scratch', 'one-off probe or round folder named for a brief');
  if (AGENT_RE.test(name)) return t('agent-scratch', 'agent scratch folder (out/<agent><brief>): copies of players, trees, shots, probes');
  if (/\.(log|txt|err)$/.test(name)) return t('log', 'a command teed to a file');
  if (/^(perf|batch-proof|unity|bench|bt)/.test(name)) return t('brief-scratch', 'frame-time and probe studies (Latch, Redline): captures, sweeps, scratch scripts');
  if (/^(plan|world|cache|levels|kit|people|dialogue|life|review|art-bible|rag|presentation|deck-publish|controls-site|preview|goals|drift-auto|drift-auto-test|route-diff|closing-hash|verify-overlay\.json|logs|runs|out|play|sessions|research|model-comparison|integration-a05|storage)/.test(name)) return t('pipeline', 'a tool\'s regenerable output (tools/<name>)');
  if (name.includes('.')) return t('loose', 'a loose file');
  return t('other', 'not classified');
}

export type Policy = {
  /** files in out/missions/plays older than this are temp scripts nobody reads */
  playsDays: number;
  zipsKeep: number; zipsDays: number;
  /** perf builds, proof-night, drawcheck, unity player copies and so on */
  buildDays: number;
  playerCopyDays: number;
  libraryDays: number;
  repoCopyDays: number;
  engineKeep: number; engineDays: number;
  scratchDays: number;
  unityLogMb: number;
  briefImageDays: number;
  hotHours: number;
};
export const DEFAULT: Policy = { playsDays: 1, zipsKeep: 2, zipsDays: 3, buildDays: 7, playerCopyDays: 1, libraryDays: 1, repoCopyDays: 1, engineKeep: 6, engineDays: 14, scratchDays: 14, unityLogMb: 50, briefImageDays: 14, hotHours: 24 };

export type Unit = { rule: string; rel: string; paths: string[]; bytes: number; files: number; newest: number; oldest: number; why: string };
export type Held = Unit & { reason: 'pinned' | 'KEEP' | 'tracked' | 'hot' | 'named in a brief' | 'no git commit to rebuild from'; detail: string };

export type Tree = {
  root: string; now: number;
  out: Node;
  /** repo-relative path → node, for the folders the scan kept */
  at(rel: string): Node | undefined;
  refs: Ref[];
  engines: Set<string>;
  /** whether git holds this commit (an engine build is rebuilt from it) */
  hasCommit(sha: string): boolean;
  trackedFiles: Set<string>; trackedDirs: Set<string>;
  /** repo-relative folders holding a file called KEEP */
  keeps: Set<string>;
  /** ignored images/videos of briefs/ */
  images: { rel: string; bytes: number; mtime: number }[];
  imageNames: { paths: Set<string>; names: Set<string> };
  /** ignored files in unity/Logs */
  unityLogs: { rel: string; bytes: number; mtime: number }[];
};

type Rule = { id: string; title: string; run(t: Tree, p: Policy): Unit[] };

const unitOf = (rule: string, rel: string, n: { path: string; bytes: number; files: number; newest: number; oldest: number }, why: string): Unit => ({ rule, rel, paths: [n.path], bytes: n.bytes, files: n.files, newest: n.newest, oldest: n.oldest, why });
const older = (n: { newest: number }, t: Tree, d: number) => n.newest > 0 && t.now - n.newest > d * DAY;

/** Every directory node under `from` (repo-relative path of the node), not entering a node `stop` returns true for. */
function dirs(t: Tree, fromRel: string, f: (rel: string, n: Node) => boolean): void {
  const go = (rel: string, n: Node) => { for (const [k, c] of n.kids) { const r = rel + '/' + k; if (!f(r, c)) go(r, c); } };
  const s = t.at(fromRel); if (s) go(fromRel, s);
}

export const RULES: Rule[] = [
  {
    id: 'plays-tmp', title: 'temp scripts of the mission checker (out/missions/plays)',
    run(t, p) {
      const n = t.at('out/missions/plays'); if (!n) return [];
      const fs = n.list.filter(f => t.now - f.mtime > p.playsDays * DAY);
      if (!fs.length) return [];
      return [{ rule: 'plays-tmp', rel: 'out/missions/plays/*', paths: fs.map(f => f.path), bytes: fs.reduce((a, f) => a + f.bytes, 0), files: fs.length, newest: Math.max(...fs.map(f => f.mtime)), oldest: Math.min(...fs.map(f => f.mtime)), why: `one script per play, never removed (fixed in tools/missions/check/engine.ts); older than ${p.playsDays} d` }];
    },
  },
  {
    id: 'build-zips', title: 'old numbered player zips (out/build/play/TheSuborned-*.zip)',
    run(t, p) {
      const n = t.at('out/build/play'); if (!n) return [];
      const zips = n.list.filter(f => /TheSuborned-.*\.zip$/.test(f.path)).sort((a, b) => b.mtime - a.mtime);
      return zips.slice(p.zipsKeep).filter(f => t.now - f.mtime > p.zipsDays * DAY).map(f => unitOf('build-zips', 'out/build/play/' + basename(f.path), { path: f.path, bytes: f.bytes, files: 1, newest: f.mtime, oldest: f.mtime }, `not one of the newest ${p.zipsKeep}; older than ${p.zipsDays} d (a send is logged in runs/builds; a build is made again from its commit)`));
    },
  },
  {
    id: 'build-old', title: 'old perf builds, night proofs and draw checks (out/build)',
    run(t, p) {
      const out: Unit[] = [];
      for (const [rel, why] of [['out/build/perf', 'SubornedPerf build for a past frame-time study'], ['out/build/perf-aswas', 'SubornedPerf build for a past frame-time study'], ['out/build/play/proof-night', 'night proof shots of a past build'], ['out/build/play/drawcheck', 'draw-distance check of a past build'], ['out/build/play/out', 'nested output of a past build']] as const) {
        const n = t.at(rel); if (n && older(n, t, p.buildDays)) out.push(unitOf('build-old', rel, n, `${why}; older than ${p.buildDays} d, rebuilt by its tool`));
      }
      return out;
    },
  },
  {
    id: 'player-copies', title: 'copies of the built player outside out/build/play (*.app and its BackUpThisFolder)',
    run(t, p) {
      const out: Unit[] = [];
      dirs(t, 'out', (rel, n) => {
        if (rel === 'out/build') return true; // handled by build-zips / build-old; the live app stays
        if (!/\.app$|_BackUpThisFolder_ButDontShipItWithYourGame$/.test(rel)) return false;
        if (older(n, t, p.playerCopyDays)) out.push(unitOf('player-copies', rel, n, `a copy of a built player in a scratch tree; older than ${p.playerCopyDays} d, the build makes it again`));
        return true;
      });
      return out;
    },
  },
  {
    id: 'unity-caches', title: 'Unity Library folders of project copies under out/',
    run(t, p) {
      const out: Unit[] = [];
      dirs(t, 'out', (rel, n) => {
        if (basename(rel) !== 'Library') return false;
        const parent = t.at(rel.slice(0, rel.lastIndexOf('/')));
        if (parent && (parent.names.includes('Assets') || parent.names.includes('ProjectSettings')) && older(n, t, p.libraryDays)) out.push(unitOf('unity-caches', rel, n, `import cache of a Unity project copy; older than ${p.libraryDays} d, Unity rebuilds it on open (slowly)`));
        return true;
      });
      return out;
    },
  },
  {
    id: 'repo-copies', title: 'copies of the repo (a folder holding tools/ and briefs/) under out/',
    run(t, p) {
      const out: Unit[] = [];
      dirs(t, 'out', (rel, n) => {
        if (!(n.names.includes('tools') && n.names.includes('briefs'))) return false;
        if (older(n, t, p.repoCopyDays)) out.push(unitOf('repo-copies', rel, n, `a materialised copy of the tree (a check against a base); older than ${p.repoCopyDays} d, git makes it again`));
        return true;
      });
      return out;
    },
  },
  {
    id: 'engine-pins', title: 'engine builds per commit (out/missions/engine/<commit>)',
    run(t, p) {
      const n = t.at('out/missions/engine'); if (!n) return [];
      const kids = [...n.kids].filter(([k]) => k !== 'tree').sort((a, b) => b[1].newest - a[1].newest);
      const out: Unit[] = [];
      for (const [k, c] of kids.slice(p.engineKeep)) {
        if (!older(c, t, p.engineDays)) continue;
        out.push(unitOf('engine-pins', 'out/missions/engine/' + k, c, `not one of the newest ${p.engineKeep}; older than ${p.engineDays} d; rebuilt by \`git archive ${k.replace('+tree', '')}\` on demand`));
      }
      return out;
    },
  },
  {
    id: 'scratch', title: 'agent and brief scratch folders not touched for a while (out/<agent><n>, out/<brief>, model and concept runs)',
    run(t, p) {
      const out: Unit[] = [];
      for (const [k, n] of t.out.kids) {
        const c = classify(k).kind;
        if (k === 'scratch') continue; // its children are judged below
        if (!['agent-scratch', 'brief-scratch', 'model-run', 'setup', 'reference-copy'].includes(c)) continue;
        if (older(n, t, p.scratchDays)) out.push(unitOf('scratch', 'out/' + k, n, `${c}; nothing changed for ${p.scratchDays}+ d`));
      }
      for (const [a, an] of t.at('out/scratch')?.kids ?? []) for (const [k, n] of an.kids) if (older(n, t, 7)) out.push(unitOf('scratch', `out/scratch/${a}/${k}`, n, 'the scratch convention (out/scratch/<agent>/<what>); nothing changed for 7+ d'));
      for (const [k, n] of t.at('out/concepts')?.kids ?? []) if (older(n, t, p.scratchDays)) out.push(unitOf('scratch', 'out/concepts/' + k, n, `concept run; nothing changed for ${p.scratchDays}+ d`));
      for (const [k, n] of t.at('out/shots')?.kids ?? []) if (older(n, t, 7)) out.push(unitOf('scratch', 'out/shots/' + k, n, 'an agent\'s screenshot round; nothing changed for 7+ d'));
      for (const f of t.at('out')?.list ?? []) if (/\.(log|txt|err|png|json|py|patch|csx)$/.test(f.path) && t.now - f.mtime > 7 * DAY) out.push({ rule: 'scratch', rel: 'out/' + basename(f.path), paths: [f.path], bytes: f.bytes, files: 1, newest: f.mtime, oldest: f.mtime, why: 'a loose log or probe file in out/; older than 7 d' });
      return out;
    },
  },
  {
    id: 'unity-logs', title: 'huge Unity logs (unity/Logs)',
    run(t, p) {
      return t.unityLogs.filter(l => l.bytes > p.unityLogMb * 1048576 && t.now - l.mtime > DAY).map(l => ({ rule: 'unity-logs', rel: l.rel, paths: [t.root + '/' + l.rel], bytes: l.bytes, files: 1, newest: l.mtime, oldest: l.mtime, why: `over ${p.unityLogMb} MB; Unity writes a new one each launch (Editor.log of Oct 20 is 1 M copies of one RobotIdle.Yaw exception)` }));
    },
  },
  {
    id: 'brief-images', title: 'ignored screenshots in briefs/ that no brief or tool names',
    run(t, p) {
      const out: Unit[] = [];
      const per = new Map<string, Unit>();
      for (const im of t.images) {
        if (t.now - im.mtime <= p.briefImageDays * DAY) continue;
        const brief = im.rel.split('/').slice(0, 2).join('/');
        const u = per.get(brief) ?? { rule: 'brief-images', rel: brief + '/**', paths: [], bytes: 0, files: 0, newest: 0, oldest: Infinity, why: `screenshots older than ${p.briefImageDays} d that no tracked file names` };
        u.paths.push(t.root + '/' + im.rel); u.bytes += im.bytes; u.files++; u.newest = Math.max(u.newest, im.mtime); u.oldest = Math.min(u.oldest, im.mtime); per.set(brief, u);
      }
      out.push(...per.values());
      return out;
    },
  },
];

/** Folders whose ignored images a tool reads: verify's HUD budget, the figures install, drift:auto's pinned images. */
export const IMAGE_READERS = ['briefs/014/play', 'briefs/022/latch', 'briefs/020/people', 'briefs/007', 'briefs/008/concepts', 'briefs/009'];

/** Inputs a tool reads from out/ by default (not just a place it writes): kept like a pin. */
export const READ_BY_CODE: { path: string; by: string }[] = [
  { path: 'out/motive027/s15', by: 'tools/sim/batch-proof.ts reads Motive\'s 500-script robustness set' },
  { path: 'out/old-world/faithful', by: 'tools/decokit/palms026.py reads the old-world capture' },
  { path: 'out/presentation', by: 'package.json deck:pack output' },
];

export type Plan = { units: Unit[]; held: Held[]; byRule: Map<string, { title: string; bytes: number; files: number; units: number; heldBytes: number }> };

/** Run the rules and sort every find into prune or hold, with the reason. */
export function plan(t: Tree, p: Policy = DEFAULT, only?: string[]): Plan {
  const units: Unit[] = [], held: Held[] = [], byRule: Plan['byRule'] = new Map(), claimed: string[] = [];
  const keepAbove = (rel: string) => { for (let d = rel; ; d = d.slice(0, d.lastIndexOf('/'))) { if (t.keeps.has(d)) return d; if (!d.includes('/')) break; } return t.keeps.has('') ? '' : undefined; };
  for (const r of RULES) {
    if (only && !only.includes(r.id)) continue;
    const row = { title: r.title, bytes: 0, files: 0, units: 0, heldBytes: 0 }; byRule.set(r.id, row);
    for (const u of r.run(t, p)) {
      const base = u.rel.replace(/\/\*+$/, '');
      if (claimed.some(c => base === c || base.startsWith(c + '/'))) continue; // an earlier rule already has it
      const hold = (reason: Held['reason'], detail: string) => { held.push({ ...u, reason, detail }); row.heldBytes += u.bytes; };
      const k = keepAbove(base);
      if (k !== undefined) { hold('KEEP', `KEEP file in ${k || '.'}`); continue; }
      const rb = READ_BY_CODE.find(x => x.path === base || x.path.startsWith(base + '/') || base.startsWith(x.path + '/'));
      if (rb) { hold('pinned', rb.by); continue; }
      const touched = touching(t.refs.filter(x => x.who === 'pin'), base);
      if (touched.length && u.rule !== 'plays-tmp' && u.rule !== 'unity-logs' && u.rule !== 'brief-images') { hold('pinned', `named by ${[...new Set(touched.map(x => x.by))].slice(0, 3).join(', ')}`); continue; }
      if (u.rule === 'engine-pins') {
        const sha = basename(base).replace('+tree', '');
        if (t.engines.has(sha)) { hold('pinned', 'a recorded mission run replays through this engine'); continue; }
        if (!t.hasCommit(sha)) { hold('no git commit to rebuild from', `git has no commit ${sha}`); continue; }
      }
      if (u.rule === 'brief-images') {
        const keepP: string[] = [], dropP: string[] = []; let kb = 0, db = 0, df = 0, kf = 0;
        for (const ab of u.paths) {
          const rel = ab.slice(t.root.length + 1), img = t.images.find(i => i.rel === rel)!;
          const reader = IMAGE_READERS.some(d => rel.startsWith(d + '/'));
          const named = t.imageNames.paths.has(rel) || t.imageNames.names.has(basename(rel));
          const pinned = t.refs.some(r => r.who === 'pin' && r.path === rel);
          if (reader || named || pinned) { keepP.push(rel); kb += img.bytes; kf++; } else { dropP.push(ab); db += img.bytes; df++; }
        }
        if (kf) held.push({ ...u, paths: keepP.slice(0, 5), bytes: kb, files: kf, reason: 'named in a brief', detail: `${kf} images a brief, tool or verify names (shrink with \`bun run shots:shrink\` instead)` }), row.heldBytes += kb;
        if (!df) continue;
        u.paths = dropP; u.bytes = db; u.files = df;
      }
      if (u.rule !== 'plays-tmp' && u.rule !== 'brief-images' && u.rule !== 'unity-logs' && t.now - u.newest < p.hotHours * 3600000) { hold('hot', `changed ${Math.round((t.now - u.newest) / 3600000)} h ago`); continue; }
      if (u.rule !== 'brief-images' && !u.rel.includes('*') && (t.trackedDirs.has(base) || t.trackedFiles.has(base))) { hold('tracked', 'git tracks a file inside'); continue; }
      units.push(u); row.bytes += u.bytes; row.files += u.files; row.units++;
      if (!u.rel.includes('*')) claimed.push(base);
    }
  }
  return { units, held, byRule };
}

export const line = (u: { rel: string; bytes: number; files: number }) => `${human(u.bytes).padStart(8)}  ${String(u.files).padStart(7)} files  ${u.rel}`;
