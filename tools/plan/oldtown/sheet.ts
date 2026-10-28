// Brief 075 remap: the comparison sheet. The remapped plan (draft or live) drawn top-down with the old town transcribed over it (old walls, crescents, Southside blocks,
// sky rail routes and landmarks, from plan.oldtown, which tools/plan/oldtown/transcribe.ts read from the old C#), next to the old town's own renders (Vela's iso views).
//   bun tools/plan/oldtown/sheet.ts [plan.json] [--out briefs/075/remap/sheet.html] [--checks out/scratch/datum-remap/w.txt,out/scratch/datum-remap/ov.txt]
// Self-contained html: the renders are embedded (downscaled JPEG via sips), nothing is fetched.
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { execFileSync } from 'node:child_process';
import { derive, type Plan } from '../plan';
import { filleted, sample } from '../geom';

const ROOT = resolve(import.meta.dir, '../../..');
const args = process.argv.slice(2), opt = (k: string, d: string) => (args.includes(k) ? args[args.indexOf(k) + 1]! : d);
const SRC = args.find(a => a.endsWith('.json') && !a.startsWith('--')) ?? 'contracts/world/harbour-district.oldtown.plan.json';
const OUT = resolve(ROOT, opt('--out', 'briefs/075/remap/sheet.html'));
const plan: any = JSON.parse(readFileSync(resolve(ROOT, SRC), 'utf8')), d = derive(plan as Plan), o = plan.oldtown;
const [X0, X1] = plan.sheet.x as [number, number], [Y0, Y1] = plan.sheet.y as [number, number];
const f = (v: number) => v.toFixed(1), pt = (p: number[]) => `${f(p[0]!)},${f(-p[1]!)}`;
const poly = (ps: number[][], cls: string, extra = '') => `<polygon class="${cls}" points="${ps.map(pt).join(' ')}" ${extra}/>`;
const line = (ps: number[][], cls: string, extra = '') => `<polyline class="${cls}" points="${ps.map(pt).join(' ')}" ${extra}/>`;
const esc = (s: string) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
const rectPts = (r: number[]) => [[r[0]!, r[2]!], [r[1]!, r[2]!], [r[1]!, r[3]!], [r[0]!, r[3]!]];

// ---- the plan
const useCls: Record<string, string> = { park: 'park', open: 'open', square: 'open', civic: 'civic', residential: 'res', mixed: 'mix', commercial: 'com', industrial: 'ind', port: 'ind' };
const zone = plan.standards.blocks?.zone as number[] | undefined;
let g = '';
g += plan.beyond.map((b: number[][]) => poly(b, 'beyond')).join('') + plan.water.map((w: number[][]) => poly(w, 'water')).join('') + plan.quay.map((q: number[][]) => poly(q, 'quay')).join('');
g += d.plots.map(p => poly(p.polyV, `plot ${useCls[p.use] ?? 'res'}${p.civic ? ' civic' : ''}`, `data-id="${p.id}"`)).join('');
g += plan.foot.map((w: any) => line(w.pts, 'foot', `stroke-width="${w.w}"`)).join('');
g += d.roads.map(r => line(r.line, `road ${r.class}`, `stroke-width="${r.w}"`)).join('');
g += `<circle class="island" cx="${plan.circus.c[0]}" cy="${-plan.circus.c[1]}" r="${plan.circus.island}"/>`;
g += (plan.blocks ?? []).flatMap((b: any) => b.runs.map((r: any) => line([r.a, r.b], `run cap-${r.caps.join('-')}`))).join('');
for (const c of plan.circus.crescents ?? []) { const ps: number[][] = []; for (let k = 0; k <= c.bays; k++) { const a = (c.deg[0] + Math.sign(c.deg[1] - c.deg[0]) * k * c.stepDeg) * Math.PI / 180; ps.push([c.rEnds * Math.cos(a), c.rEnds * Math.sin(a)]); } g += line(ps, 'run crescent'); }
g += plan.landmarks.map((l: any) => `<circle class="landmark" cx="${l.c[0]}" cy="${-l.c[1]}" r="${l.r}"/><text class="lab lm" x="${l.c[0]}" y="${-l.c[1] - l.r - 3}">${esc(l.name)}</text>`).join('');
g += (plan.signatures ?? []).map((s: any) => poly(rectPts(s.ground), 'sig') + `<text class="lab lm" x="${s.c[0]}" y="${-s.c[1]}">${esc(s.name.split('·')[0])}</text>`).join('');
g += (plan.megastructures ?? []).flatMap((m: any) => m.footprint.map((r: number[]) => poly(rectPts(r), 'mega'))).join('');
g += d.tram.map(p => line(p.samples.map(s => s.p), 'tram')).join('') + d.sky.map(p => line(p.samples.map(s => s.p), 'sky')).join('');
g += d.pylons.map(p => `<circle class="pylon" cx="${f(p.p[0])}" cy="${f(-p.p[1])}" r="1.6"/>`).join('');
for (const l of plan.tram) for (const s of l.stops ?? []) g += `<circle class="stop" cx="${s.at[0]}" cy="${-s.at[1]}" r="4"/><text class="lab st" x="${s.at[0] + 6}" y="${-s.at[1]}">${esc(s.name)}</text>`;
for (const l of plan.skyrail) for (const s of l.stations ?? []) g += `<rect class="station" x="${s.at[0] - 25}" y="${-s.at[1] - 4}" width="50" height="8"/><text class="lab st" x="${s.at[0]}" y="${-s.at[1] - 8}">${esc(s.name)} (${l.id})</text>`;
if (zone) g += poly(rectPts(zone), 'zone') + `<text class="lab zl" x="${zone[0] + 4}" y="${-zone[3] + 12}">Handover zone (kept byte for byte)</text>`;
g += d.roads.filter(r => r.length > 60).map(r => { const m = r.line[Math.floor(r.line.length / 2)]!; return `<text class="lab rd" x="${f(m[0])}" y="${f(-m[1])}">${esc(r.name)}</text>`; }).join('');

// ---- the old town over it (plan frame)
let og = '';
if (o) {
  const dir = { N: [0, 1], S: [0, -1], E: [1, 0], W: [-1, 0] } as Record<string, number[]>;
  for (const w of o.walls) { const L = (w.caps[0] === 'Corner' || w.caps[0] === 'Tower' ? 8 : 0) + 6 * w.segs.reduce((t: number, s: any) => t + s.n, 0) + (w.caps[1] === 'Corner' || w.caps[1] === 'Tower' ? 8 : 0), u = dir[w.dir]!; og += line([w.start, [w.start[0] + u[0]! * L, w.start[1] + u[1]! * L]], 'oldwall'); }
  for (const b of o.southside) { const a = b.phi * Math.PI / 180, ax = [Math.sin(a), Math.cos(a)], sd = [Math.cos(a), -Math.sin(a)], hw = b.w / 2, hd = b.d / 2, c = b.c;
    const corner = (u: number, v: number) => [c[0] + ax[0]! * u + sd[0]! * v, c[1] + ax[1]! * u + sd[1]! * v];
    og += poly([corner(-hw, -hd), corner(hw, -hd), corner(hw, hd), corner(-hw, hd)], `oldblock ${b.built.startsWith('gone') ? 'gone' : 'kept'}`) + `<text class="lab old" x="${f(c[0])}" y="${f(-c[1])}">${b.name}</text>`; }
  const spl = (pts: number[][], closed: boolean) => { // Catmull-Rom through the old control points, as SkyPath did
    const P = pts.map(p => [p[0]!, p[2]!]), n = P.length, out: number[][] = [];
    for (let i = 0; i < (closed ? n : n - 1); i++) { const p0 = P[(i - 1 + n) % n]!, p1 = P[i]!, p2 = P[(i + 1) % n]!, p3 = P[(i + 2) % n]!;
      for (let t = 0; t < 1; t += 0.1) { const t2 = t * t, t3 = t2 * t; out.push([0, 1].map(k => 0.5 * (2 * p1[k]! + (-p0[k]! + p2[k]!) * t + (2 * p0[k]! - 5 * p1[k]! + 4 * p2[k]! - p3[k]!) * t2 + (-p0[k]! + 3 * p1[k]! - 3 * p2[k]! + p3[k]!) * t3))); } }
    return out; };
  og += line(spl(o.skyrail.main.pts, true), 'oldsky') + line(spl(o.skyrail.harbour.pts, true), 'oldsky');
  for (const q of o.props.puts) og += `<circle class="oldput" cx="${f(q.at[0])}" cy="${f(-q.at[1])}" r="3"/><text class="lab old" x="${f(q.at[0] + 4)}" y="${f(-q.at[1] + 10)}">${q.kit}</text>`;
  for (const k of ['S1', 'S2'] as const) { const L = o.skyrail.planned?.[k]; if (!L) continue; for (const p of L.paths) { try { og += line(sample(filleted(p.pts), 1).map(s => s.p), 'planned'); } catch { /* not a valid path */ } } }
}

// ---- the old renders (Vela)
const shot = (p: string) => {
  const src = resolve(ROOT, p); if (!existsSync(src)) return '';
  const tmp = resolve(ROOT, 'out/scratch/datum-remap', p.split('/').pop()!.replace(/\.png$/, '.jpg'));
  mkdirSync(dirname(tmp), { recursive: true });
  execFileSync('sips', ['-s', 'format', 'jpeg', '-s', 'formatOptions', '70', '-Z', '1400', src, '--out', tmp], { stdio: 'ignore' });
  return `data:image/jpeg;base64,${readFileSync(tmp).toString('base64')}`;
};
const shots = ['out/scratch/vela075/old-iso/vela075_district.png', 'out/scratch/vela075/old-iso/vela075_satellite.png', 'out/scratch/vela075/old-iso/vela075_boulevard.png', 'out/scratch/vela075/old-iso/vela075_square.png'].map(p => ({ p, src: shot(p) })).filter(s => s.src);

// ---- the checks, read from the CLI's output if given
const checks = opt('--checks', '').split(',').filter(Boolean).map(p => resolve(ROOT, p)).filter(existsSync).map(p => {
  const t = readFileSync(p, 'utf8'), pass = (t.match(/^PASS/gm) ?? []).length, fail = (t.match(/^FAIL/gm) ?? []).length;
  const keep = t.split('\n').filter(l => /^(PASS|FAIL)\s+(C-0|C-1|C-3|M-1|M-2|M-3|V-1|W-1|BG-\d|S-S2|N-C1|N-R1|T-S1|MS-1)\b/.test(l) || l.startsWith('route ')).map(l => l.slice(0, 220));
  return { p: p.replace(ROOT + '/', ''), pass, fail, keep };
});

const W = X1 - X0, H = Y1 - Y0;
const html = `<!doctype html><html><head><meta charset="utf-8"><title>Brief 075 remap: the old town around the Handover (${esc(plan.id)} v${esc(plan.version)})</title>
<style>
body{font:14px/1.45 -apple-system,Helvetica,Arial,sans-serif;margin:0;background:#f4efe6;color:#2a2a2a}
header{padding:16px 24px;background:#24333a;color:#f4efe6} h1{margin:0;font-size:20px} h2{font-size:16px;margin:20px 0 8px}
main{display:grid;grid-template-columns:minmax(0,1.25fr) minmax(0,1fr);gap:16px;padding:16px 24px}
.panel{background:#fffaf2;border:1px solid #d8cfc0;border-radius:6px;padding:12px}
svg{width:100%;height:auto;background:#efe8da;border:1px solid #cbbfa8}
.beyond{fill:#ddd5c5}.water{fill:#9cc7cf}.quay{fill:#e7dcc6;stroke:#b9a988;stroke-width:.5}
.plot{stroke:#8d8270;stroke-width:.6}.res{fill:#f0d9c4}.mix{fill:#efcfb6}.com{fill:#e8c4a6}.ind{fill:#d7cbbb}.park{fill:#bfdcae}.open{fill:#f7efe0}.civic{fill:#e9a98f;stroke:#7a2d18;stroke-width:1.2}
.foot{stroke:#f9f3e8;stroke-linecap:butt;fill:none;opacity:.9}.road{stroke:#7d8389;fill:none;stroke-linecap:butt}.primary{stroke:#5f666d}.district{stroke:#737a81}.access{stroke:#959ba1}
.island{fill:#bfdcae;stroke:#7d8389}.run{stroke:#3a2a1a;stroke-width:1.4;fill:none}.crescent{stroke:#7a2d18;stroke-width:2}
.landmark{fill:#f6d58e;stroke:#a07a2c;stroke-width:.8;opacity:.85}.sig{fill:#f2c76b;stroke:#a07a2c;opacity:.8}.mega{fill:none;stroke:#7a4fa0;stroke-width:1.5;stroke-dasharray:4 2}
.tram{stroke:#c0392b;stroke-width:1.6;fill:none}.sky{stroke:#e67e22;stroke-width:2.4;fill:none}.pylon{fill:#e67e22}.stop{fill:#c0392b}.station{fill:#e67e22;opacity:.8}
.zone{fill:url(#hatch);stroke:#7a2d18;stroke-width:2;stroke-dasharray:6 3}
.lab{font-size:7px;fill:#333;text-anchor:middle}.rd{font-size:6px;fill:#444}.lm{font-size:7px;font-weight:600}.st{font-size:6px;fill:#8e2a1f}.zl{font-size:9px;fill:#7a2d18;text-anchor:start;font-weight:700}
.old .oldwall{stroke:#1d6fa5;stroke-width:1.2;fill:none}.old .oldblock{fill:none;stroke:#1d6fa5;stroke-width:.9}.old .gone{stroke-dasharray:3 2;stroke:#5b8fb5}.old .oldsky{stroke:#1d6fa5;stroke-width:1.4;fill:none;stroke-dasharray:5 3}
.old .oldput{fill:#1d6fa5}.old .lab.old{fill:#1d6fa5;font-size:6px}.old .planned{stroke:#16a085;stroke-width:1.6;fill:none;stroke-dasharray:2 2}
label{margin-right:12px;font-size:13px} img{width:100%;border-radius:4px;margin-bottom:8px} ul{padding-left:18px;margin:6px 0} li{margin:3px 0} code{font-size:12px}
.k{display:inline-block;width:14px;height:8px;margin-right:4px;vertical-align:middle}
</style></head><body>
<header><h1>Brief 075 · the remap: the old DecoKit town around the Handover</h1><div>${esc(plan.id)} v${esc(plan.version)} · ${esc(SRC)} · transcribed by <code>tools/plan/oldtown/transcribe.ts</code> from the old C# (${Object.keys(o?.sources ?? {}).length} files, sha256 in <code>plan.oldtown.sources</code>). Plan frame = old Unity (x, z − 206): Crown Circus at the origin, every old coordinate unscaled.</div></header>
<main>
<section class="panel"><h2>The plan, top-down, with the old town over it</h2>
<div><label><input type="checkbox" checked onchange="document.getElementById('old').style.display=this.checked?'':'none'"> old town (blue: walls, Southside blocks, dashed = gone; sky rail routes; landmark Puts)</label>
<label><input type="checkbox" checked onchange="document.querySelectorAll('.planned').forEach(e=>e.style.display=this.checked?'':'none')"> old sky rail as planned (green dotted, not built)</label></div>
<svg viewBox="${X0} ${-Y1} ${W} ${H}" xmlns="http://www.w3.org/2000/svg"><defs><pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="8" height="8" fill="#e9a98f22"/><line x1="0" y1="0" x2="0" y2="8" stroke="#7a2d18" stroke-width="1.2" opacity=".35"/></pattern></defs>
<g>${g}</g><g id="old" class="old">${og}</g></svg>
<p><span class="k" style="background:#c0392b"></span>T1 tram · <span class="k" style="background:#e67e22"></span>S1 sky rail and pylons · <span class="k" style="background:#3a2a1a"></span>block runs (plan.blocks, old walls exact on the boulevard) · <span class="k" style="background:#7a2d18"></span>crescents · <span class="k" style="background:#1d6fa5"></span>old town (transcribed) · hatched: the Handover zone (capstone zone)</p>
<p>The CAD sheet the checker draws: <code>bun tools/plan/cli.ts ${esc(SRC)}</code> → <a href="plan-sheet.html">plan-sheet.html</a> (beside this file).</p>
${checks.map(c => `<h2>Checks · ${esc(c.p)}: ${c.pass} pass, ${c.fail} fail</h2><ul>${c.keep.map(l => `<li><code>${esc(l)}</code></li>`).join('')}</ul>`).join('')}
</section>
<section class="panel"><h2>The old town (Vela's renders of the old scene, brief 075)</h2>
${shots.map(s => `<img src="${s.src}" alt="${esc(s.p)}"><div style="font-size:12px;margin:-4px 0 10px">${esc(s.p)}</div>`).join('')}
<h2>What was decided by hand (MANUAL, ${o?.manual?.length ?? 0})</h2><ul>${(o?.manual ?? []).map((m: string) => `<li>${esc(m)}</li>`).join('')}</ul>
</section>
</main></body></html>`;
mkdirSync(dirname(OUT), { recursive: true });
writeFileSync(OUT, html);
console.log(`sheet → ${OUT} (${(html.length / 1e6).toFixed(1)} MB, ${shots.length} renders)`);
