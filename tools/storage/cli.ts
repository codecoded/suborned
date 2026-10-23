// usage: bun run storage [--json] [--html] [--top n]            size report: out/, ignored screenshots in briefs/, other ignored folders, the leaks
//        bun run storage --prune [--yes] [--rule a,b] [--list]   what the retention policy (tools/storage/policy.ts) would delete; DRY RUN unless --yes
//        flags that move a number: --scratch-days n  --build-days n  --zips-keep n  --engine-keep n  --image-days n
// Brief 050. Writes out/storage/report.json (and report.html with --html). Deleting needs --yes; a prune is logged in out/storage/pruned-<time>.json.
import { existsSync, mkdirSync, readdirSync, realpathSync, rmSync, writeFileSync } from 'node:fs';
import { basename, join, relative, resolve } from 'node:path';
import { classify, DEFAULT, line, plan, type Plan, type Policy, type Tree } from './policy';
import { commitExists, imageRefs, outRefs, pinnedEngines, summarise, touching, tracked } from './refs';
import { day, DAY, human, measure, scan, type Node } from './scan';

export const ROOT = resolve(import.meta.dir, '../..');
const args = process.argv.slice(2), has = (f: string) => args.includes(f), val = (f: string) => { const i = args.indexOf(f); return i >= 0 ? args[i + 1] : undefined; };
const IMG = /\.(png|jpe?g|webp|mp4|mov)$/i;

/** Read the disk: out/ in full, briefs/ for ignored images, unity/Logs. */
export function readTree(root = ROOT, now = Date.now()) {
  const { files: trackedFiles, dirs: trackedDirs } = tracked(root);
  const keeps = new Set<string>(), growth = new Map<string, number>(), images: Tree['images'] = [];
  const FILE_LEVEL = new Set(['out', 'out/missions/plays', 'out/build/play', 'out/batch', 'out/batch/logs']);
  const outNode = scan(join(root, 'out'), {
    base: root, depth: 5, fileLevel: r => FILE_LEVEL.has(r),
    onFile: (p, b, m) => {
      const r = relative(root, p);
      if (basename(p) === 'KEEP') keeps.add(r.slice(0, r.lastIndexOf('/')));
      const top = r.split('/')[1]!;
      const k = `${top}|${new Date(m).toISOString().slice(0, 10)}`; growth.set(k, (growth.get(k) ?? 0) + b);
    },
  });
  let briefsAll = 0, briefsAllFiles = 0;
  const briefs = scan(join(root, 'briefs'), {
    base: root, depth: 2,
    onFile: (p, b, m) => {
      const r = relative(root, p); briefsAll += b; briefsAllFiles++;
      if (basename(p) === 'KEEP') keeps.add(r.slice(0, r.lastIndexOf('/')));
      if (IMG.test(p) && !trackedFiles.has(r)) images.push({ rel: r, bytes: b, mtime: m });
    },
  });
  const unityLogs: Tree['unityLogs'] = [];
  const logDir = join(root, 'unity/Logs');
  if (existsSync(logDir)) for (const f of readdirSync(logDir)) { const n = measure(join(logDir, f), { base: root }); if (n && n.files === 1) unityLogs.push({ rel: `unity/Logs/${f}`, bytes: n.bytes, mtime: n.newest }); }
  const at = (rel: string): Node | undefined => {
    if (rel === 'out') return outNode;
    let n: Node | undefined = outNode; for (const part of rel.split('/').slice(1)) { n = n?.kids.get(part); if (!n) return undefined; } return n;
  };
  const tree: Tree = {
    root, now, out: outNode, at, refs: outRefs(root), engines: pinnedEngines(root), hasCommit: sha => commitExists(root, sha), trackedFiles, trackedDirs, keeps, images, imageNames: imageRefs(root), unityLogs,
  };
  return { tree, briefs, briefsAll, briefsAllFiles, growth };
}

/** Folders git ignores outside out/ and briefs/ (caches, builds, node_modules, shots), measured. */
function otherIgnored(root: string) {
  const r = Bun.spawnSync(['git', 'ls-files', '--others', '--ignored', '--exclude-standard', '--directory'], { cwd: root, stdout: 'pipe', stderr: 'pipe', maxBuffer: 256 * 1024 * 1024 });
  const dirs = r.stdout.toString().split('\n').filter(d => d && d.endsWith('/') && !d.startsWith('out/') && !d.startsWith('briefs/') && !d.includes('node_modules/') || d === 'node_modules/');
  const out: { rel: string; bytes: number; files: number; newest: number; oldest: number }[] = [];
  for (const d of dirs) { const n = measure(join(root, d), { base: root, depth: 0 }); if (n && n.bytes > 10 << 20) out.push({ rel: d.replace(/\/$/, ''), bytes: n.bytes, files: n.files, newest: n.newest, oldest: n.oldest }); }
  return out.sort((a, b) => b.bytes - a.bytes);
}

function mcpProcesses() {
  const r = Bun.spawnSync(['ps', '-axo', 'pid=,etime=,command='], { stdout: 'pipe' });
  return r.stdout.toString().split('\n').filter(l => /\/unity mcp\s*$/.test(l.trim())).map(l => { const m = l.trim().match(/^(\d+)\s+(\S+)/)!; return { pid: Number(m[1]), etime: m[2]! }; });
}

const CAUSES: Record<string, string> = {
  'plays-tmp': 'tools/missions/check/engine.ts wrote one temp script per play into out/missions/plays and never removed it (a composer run is 15,000 plays; the folder holds 600,000 files at 4 KB each). Fixed in engine.ts: the file is removed after the play.',
  'build-zips': 'bun run game:pack keeps every numbered player zip (300-530 MB each) beside the app.',
  'build-old': 'SubornedPerf builds for past frame-time studies; each is a full player (900 MB) that nothing reads again.',
  'player-copies': 'agents copy the whole built player into their scratch tree to run it (out/latch-0266/app, out/redline/048/app, sim-unity players); each copy is 0.5-1.5 GB and stays.',
  'unity-caches': 'out/old-world/unity is a whole copy of the old Unity project including its Library import cache (7.3 GB).',
  'repo-copies': 'a materialised copy of the tree (briefs/, tools/, runs/ ...) made for a base-versus-tree check (out/keel038/mcbase, 5.5 GB, mostly briefs/ screenshots).',
  'engine-pins': 'one engine build per checked commit (3-8 MB each); small, rebuilt from git on demand.',
  'scratch': 'agent working folders out/<agent><brief>, per-brief probe folders and model-generation runs: every round adds a folder, nothing removes one.',
  'unity-logs': 'unity/Logs/Editor.log reached 963 MB on Oct 20 (one RobotIdle.Yaw exception thrown 1 M times in play mode); Editor-prev.log is another 97 MB.',
  'brief-images': 'screenshots saved under briefs/<n>/ (git-ignored since Oct 19). Native Studio captures are 6016 x 3384; bun run shots:shrink makes them 1920 wide.',
};

function report(root: string, o: { top: number; policy: Policy }) {
  const { tree, briefs, briefsAll, briefsAllFiles, growth } = readTree(root);
  const p = plan(tree, o.policy);
  const lines: string[] = [];
  const outTotal = tree.out.bytes;
  const imgTotal = tree.images.reduce((a, i) => a + i.bytes, 0);
  const others = otherIgnored(root);

  lines.push(`STORAGE ${new Date(tree.now).toISOString().slice(0, 16)}Z   out/ ${human(outTotal)} (${tree.out.files.toLocaleString()} files)   briefs/ ${human(briefsAll)} total, ${human(imgTotal)} in ${tree.images.length} ignored images   prune would free ${human(p.units.reduce((a, u) => a + u.bytes, 0))}`);
  lines.push('');
  lines.push('out/ BY ENTRY (size on disk; new/old = newest/oldest file mm-dd; deps: P = pinned by runs/contracts, C = named in code, D = named in docs; "frees" = what --prune would delete)');
  const rows = [...tree.out.kids].map(([k, n]) => ({ k, n, c: classify(k) }));
  for (const f of tree.out.list) rows.push({ k: basename(f.path), n: { path: f.path, bytes: f.bytes, files: 1, newest: f.mtime, oldest: f.mtime, kids: new Map(), names: [], list: [f] }, c: classify(basename(f.path)) });
  rows.sort((a, b) => b.n.bytes - a.n.bytes);
  const unitBytes = (rel: string) => p.units.filter(u => u.rel === rel || u.rel.startsWith(rel + '/')).reduce((a, u) => a + u.bytes, 0);
  const heldBytes = (rel: string) => p.held.filter(u => u.rel === rel || u.rel.startsWith(rel + '/')).reduce((a, u) => a + u.bytes, 0);
  const shown = rows.slice(0, o.top), rest = rows.slice(o.top);
  lines.push('  size       files   new    old   kind            deps        frees     entry   ← made by');
  for (const r of shown) {
    const d = summarise(touching(tree.refs, 'out/' + r.k));
    const deps = `${d.pin.length ? 'P' + d.pin.length : '  '} ${d.code.length ? 'C' + d.code.length : '  '} ${d.doc.length ? 'D' + d.doc.length : '  '}`;
    const fr = unitBytes('out/' + r.k), hd = heldBytes('out/' + r.k);
    lines.push(`${human(r.n.bytes).padStart(8)} ${String(r.n.files).padStart(8)}  ${day(r.n.newest)}  ${day(r.n.oldest)}   ${r.c.kind.padEnd(14)}  ${deps.padEnd(9)} ${(fr ? human(fr) : hd ? '(' + human(hd) + ' held)' : '').padStart(11)}   out/${r.k}   ← ${r.c.maker}`);
  }
  if (rest.length) lines.push(`${human(rest.reduce((a, r) => a + r.n.bytes, 0)).padStart(8)} ${String(rest.reduce((a, r) => a + r.n.files, 0)).padStart(8)}   ... ${rest.length} smaller entries (--top ${rows.length} for all)`);

  // by kind
  lines.push('', 'BY KIND');
  const kinds = new Map<string, { bytes: number; n: number }>();
  for (const r of rows) { const k = kinds.get(r.c.kind) ?? { bytes: 0, n: 0 }; k.bytes += r.n.bytes; k.n++; kinds.set(r.c.kind, k); }
  for (const [k, v] of [...kinds].sort((a, b) => b[1].bytes - a[1].bytes)) lines.push(`${human(v.bytes).padStart(8)}  ${String(v.n).padStart(4)} entries  ${k}`);

  // growth: bytes whose files were last written on each of the last 10 days, by kind
  lines.push('', 'WHAT WAS WRITTEN WHEN (size of files last written that day; later rewrites move a file to the later day)');
  const dayKeys = [...new Set([...growth.keys()].map(k => k.split('|')[1]!))].sort().slice(-12);
  const byKind = new Map<string, Map<string, number>>();
  for (const [k, b] of growth) { const [top, d] = k.split('|') as [string, string]; const kind = classify(top).kind; const m = byKind.get(kind) ?? new Map(); m.set(d, (m.get(d) ?? 0) + b); byKind.set(kind, m); }
  lines.push('kind            ' + dayKeys.map(d => d.slice(5).padStart(7)).join(''));
  for (const [k, m] of [...byKind].sort((a, b) => [...b[1].values()].reduce((x, y) => x + y, 0) - [...a[1].values()].reduce((x, y) => x + y, 0)).slice(0, 10)) lines.push(k.padEnd(16) + dayKeys.map(d => (m.get(d) ? human(m.get(d)!).replace(' ', '') : '.').padStart(7)).join(''));

  // briefs
  lines.push('', 'briefs/ IGNORED IMAGES BY BRIEF (screenshots are git-ignored: nothing but this machine holds them)');
  const per = new Map<string, { bytes: number; n: number; newest: number; oldest: number; named: number; namedBytes: number }>();
  for (const i of tree.images) {
    const b = i.rel.split('/').slice(0, 2).join('/'), v = per.get(b) ?? { bytes: 0, n: 0, newest: 0, oldest: Infinity, named: 0, namedBytes: 0 };
    v.bytes += i.bytes; v.n++; v.newest = Math.max(v.newest, i.mtime); v.oldest = Math.min(v.oldest, i.mtime);
    if (tree.imageNames.paths.has(i.rel) || tree.imageNames.names.has(basename(i.rel))) { v.named++; v.namedBytes += i.bytes; }
    per.set(b, v);
  }
  lines.push('  size   images   new    old   named in a tracked file   brief');
  for (const [b, v] of [...per].sort((a, b) => b[1].bytes - a[1].bytes).slice(0, 14)) lines.push(`${human(v.bytes).padStart(8)} ${String(v.n).padStart(6)}  ${day(v.newest)}  ${day(v.oldest)}   ${(v.named + ' (' + human(v.namedBytes) + ')').padStart(22)}   ${b}`);
  const big = tree.images.filter(i => i.bytes > 8 << 20);
  lines.push(`  ${big.length} images over 8 MB hold ${human(big.reduce((a, i) => a + i.bytes, 0))}; briefs/ also holds ${human(briefsAll - imgTotal)} in ${(briefsAllFiles - tree.images.length).toLocaleString()} tracked or non-image files`);

  lines.push('', 'OTHER GIT-IGNORED FOLDERS (over 10 MB)');
  for (const f of others.slice(0, 12)) lines.push(`${human(f.bytes).padStart(8)} ${String(f.files).padStart(8)} files  ${day(f.newest)}  ${f.rel}`);
  for (const l of tree.unityLogs.filter(l => l.bytes > 20 << 20)) lines.push(`${human(l.bytes).padStart(8)}          1 file   ${day(l.mtime)}  ${l.rel}`);

  const mcp = mcpProcesses();
  lines.push('', `SIDE NOTE: ${mcp.length} \`unity mcp\` processes are running (${mcp.map(m => m.pid + ' ' + m.etime).slice(0, 8).join(', ')}${mcp.length > 8 ? ', ...' : ''}); each is a tool server an agent session left behind. They hold no disk, only memory and sockets.`);

  lines.push('', 'LEAKS (what grows and is never cleaned; the policy rule that would take it back)');
  for (const [id, r] of p.byRule) {
    const heldB = r.heldBytes;
    lines.push(`${human(r.bytes).padStart(8)}  ${id.padEnd(14)} ${r.units} units, ${r.files.toLocaleString()} files${heldB ? ` (+${human(heldB)} held)` : ''}`);
    lines.push(`          ${CAUSES[id] ?? r.title}`);
  }
  const unclaimed = tree.out.bytes - p.units.reduce((a, u) => a + (u.rel.startsWith('out/') ? u.bytes : 0), 0);
  lines.push('', `AFTER A PRUNE out/ would be about ${human(unclaimed)} (now ${human(outTotal)}).`);

  const json = {
    schema: 'suborned.storage/1', at: new Date(tree.now).toISOString(), out: { bytes: outTotal, files: tree.out.files },
    briefs: { bytes: briefsAll, imageBytes: imgTotal, images: tree.images.length },
    entries: rows.map(r => ({ entry: 'out/' + r.k, bytes: r.n.bytes, files: r.n.files, newest: r.n.newest, oldest: r.n.oldest, kind: r.c.kind, maker: r.c.maker, refs: summarise(touching(tree.refs, 'out/' + r.k)), frees: unitBytes('out/' + r.k) })),
    perBrief: [...per].map(([b, v]) => ({ brief: b, ...v })), other: others,
    prune: summary(p), held: p.held.map(h => ({ rule: h.rule, rel: h.rel, bytes: h.bytes, reason: h.reason, detail: h.detail })),
  };
  return { text: lines.join('\n'), json, plan: p, tree };
}

const summary = (p: Plan) => ({ bytes: p.units.reduce((a, u) => a + u.bytes, 0), units: p.units.length, files: p.units.reduce((a, u) => a + u.files, 0), byRule: Object.fromEntries([...p.byRule].map(([k, v]) => [k, v])) });

function html(json: ReturnType<typeof report>['json']): string {
  const esc = (s: string) => s.replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' })[c]!);
  const tr = (cells: (string | number)[]) => '<tr>' + cells.map(c => `<td>${esc(String(c))}</td>`).join('') + '</tr>';
  return `<!doctype html><meta charset=utf-8><title>Storage ${json.at}</title><style>body{font:14px system-ui;margin:2em}td,th{padding:2px 10px;text-align:left;border-bottom:1px solid #ddd}table{border-collapse:collapse}</style>
<h1>Storage ${json.at.slice(0, 16)}Z</h1><p>out/ ${human(json.out.bytes)} in ${json.out.files} files; briefs/ ${human(json.briefs.bytes)} (${json.briefs.images} ignored images, ${human(json.briefs.imageBytes)}). Prune would free ${human(json.prune.bytes)}.</p>
<h2>out/ by entry</h2><table><tr><th>size<th>files<th>newest<th>oldest<th>kind<th>pinned by<th>entry<th>made by<th>frees</tr>${json.entries.map(e => tr([human(e.bytes), e.files, day(e.newest), day(e.oldest), e.kind, e.refs.pin.join(' '), e.entry, e.maker, e.frees ? human(e.frees) : ''])).join('')}</table>
<h2>Per brief (ignored images)</h2><table>${json.perBrief.sort((a, b) => b.bytes - a.bytes).map(b => tr([b.brief, human(b.bytes), b.n, day(b.newest), day(b.oldest), b.named])).join('')}</table>
<h2>Held by the policy</h2><table>${json.held.map(h => tr([h.rule, h.rel, human(h.bytes), h.reason, h.detail])).join('')}</table>`;
}

export function prune(root: string, yes: boolean, policy: Policy, only?: string[], listAll = false) {
  const { tree, plan: p0 } = (() => { const r = readTree(root); return { tree: r.tree, plan: plan(r.tree, policy, only) }; })();
  const p = p0;
  const out: string[] = [yes ? 'PRUNE (deleting)' : 'PRUNE DRY RUN (nothing is deleted; --yes to delete)', ''];
  for (const [id, r] of p.byRule) {
    const us = p.units.filter(u => u.rule === id).sort((a, b) => b.bytes - a.bytes), hs = p.held.filter(h => h.rule === id);
    out.push(`${human(r.bytes).padStart(8)}  ${id}: ${r.title}   [${us.length} units, ${r.files.toLocaleString()} files]`);
    for (const u of (listAll ? us : us.slice(0, 8))) out.push('  ' + line(u) + '   ' + u.why);
    if (us.length > 8 && !listAll) out.push(`          ... ${us.length - 8} more (--list)`);
    for (const h of hs.sort((a, b) => b.bytes - a.bytes).slice(0, listAll ? 99 : 4)) out.push(`  HELD ${h.reason}: ${line(h)}   ${h.detail}`);
    if (hs.length > 4 && !listAll) out.push(`  HELD ... ${hs.length - 4} more`);
  }
  const total = p.units.reduce((a, u) => a + u.bytes, 0), files = p.units.reduce((a, u) => a + u.files, 0);
  out.push('', `${yes ? 'FREEING' : 'WOULD FREE'} ${human(total)} in ${files.toLocaleString()} files (${p.units.length} units); held back ${human(p.held.reduce((a, h) => a + h.bytes, 0))}`);
  console.log(out.join('\n'));
  if (!yes) return 0;

  // delete: only paths that really sit under out/, briefs/ or unity/Logs of this repo, and never a tracked file
  const real = realpathSync(root), okRoots = ['out', 'briefs', 'unity/Logs'].map(r => join(real, r));
  let freed = 0, gone = 0; const log: string[] = [], failed: string[] = [];
  for (const u of p.units) for (const path of u.paths) {
    let rp: string; try { rp = realpathSync(path); } catch { continue; }
    const r = relative(real, rp);
    if (!okRoots.some(o => rp.startsWith(o + '/')) || tree.trackedFiles.has(r) || tree.trackedDirs.has(r)) { failed.push(`refused ${path}`); continue; }
    try { rmSync(rp, { recursive: true, force: true }); gone++; log.push(r); } catch (e) { failed.push(`${path}: ${(e as Error).message}`); }
  }
  freed = total;
  mkdirSync(join(root, 'out/storage'), { recursive: true });
  const file = join(root, `out/storage/pruned-${new Date().toISOString().replace(/[:.]/g, '-')}.json`);
  writeFileSync(file, JSON.stringify({ schema: 'suborned.storage.pruned/1', at: new Date().toISOString(), policy, freed, removed: gone, units: p.units.map(u => ({ rule: u.rule, rel: u.rel, bytes: u.bytes, files: u.files })), failed }, null, 1) + '\n');
  console.log(`deleted ${gone} paths, about ${human(freed)}; ${failed.length} refused or failed; log ${relative(root, file)}`);
  for (const f of failed.slice(0, 10)) console.log('  ' + f);
  return failed.length ? 1 : 0;
}

if (import.meta.main) {
  const policy: Policy = { ...DEFAULT };
  const num = (f: string, k: keyof Policy) => { const v = val(f); if (v !== undefined) policy[k] = Number(v); };
  num('--scratch-days', 'scratchDays'); num('--build-days', 'buildDays'); num('--zips-keep', 'zipsKeep'); num('--engine-keep', 'engineKeep'); num('--image-days', 'briefImageDays');
  const only = val('--rule')?.split(',');
  if (has('--prune')) process.exit(prune(ROOT, has('--yes'), policy, only, has('--list')));
  const r = report(ROOT, { top: Number(val('--top') ?? 30), policy });
  mkdirSync(join(ROOT, 'out/storage'), { recursive: true });
  writeFileSync(join(ROOT, 'out/storage/report.json'), JSON.stringify(r.json, null, 1) + '\n');
  if (has('--html')) writeFileSync(join(ROOT, 'out/storage/report.html'), html(r.json));
  console.log(has('--json') ? JSON.stringify(r.json, null, 1) : r.text);
}
