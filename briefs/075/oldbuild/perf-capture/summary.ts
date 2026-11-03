// Brief 075 Perf A/B item A (Latch): the counts table from the Studio captures (OldTownSwap.PerfAB → <scene>-<pipeline>.json beside this file).
//   bun briefs/075/oldbuild/perf-capture/summary.ts   → counts.md
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';

const dir = import.meta.dir;
const runs: [string, string][] = [
  ['harbour-ours', 'HarbourDistrict, our pipeline'],
  ['oldtown-old', 'OldTown, old pipeline (as built: shadows 50 m)'],
  ['oldtown-old-sd200', 'OldTown, old pipeline, shadows 200 m (as SwapShots rendered the frames Sky saw)'],
  ['oldtown-ours', 'OldTown, our pipeline'],
];
const J = (k: string) => (existsSync(resolve(dir, `${k}.json`)) ? JSON.parse(readFileSync(resolve(dir, `${k}.json`), 'utf8')) : null);
const n = (x: number) => (x ?? 0).toLocaleString('en-US');
const pc = (x: number) => `${Math.round((x ?? 0) * 1000) / 10}%`;
const ms = (v: any, m: string) => v.frameMs?.[m]?.median ?? '–';
const out: string[] = ['# Brief 075 Perf A/B · Studio counts (batchmode, offscreen; estimates, not UnityStats)', ''];
const data = Object.fromEntries(runs.map(([k]) => [k, J(k)]));
const any = Object.values(data).find(Boolean) as any;
out.push(`Machine: ${any?.facts.machine}. Method: After072's estimator via After072.AbCapture (OldTownSwap.PerfAB), 1920×1080; draws = sub-meshes before batching or instancing; ms = batchmode Camera.Render with the GPU waited on, indicative only. Command: \`bun briefs/075/oldbuild/swap/tools/unityrun.ts <log> -- -executeMethod Suborned.OldTown.Editor.Swap.OldTownSwap.PerfAB -abScene OldTown|HarbourDistrict [-abPipeline old|ours] [-abShadow 200]\`, then \`bun briefs/075/oldbuild/perf-capture/summary.ts\`.`, '');
for (const [k, label] of runs) {
  const d = data[k]; if (!d) { out.push(`## ${label}`, '', '(not captured)', ''); continue; }
  const f = d.facts;
  out.push(`## ${label}`, '', `Asset ${f.urpAsset} (${f.renderer}); MSAA ${f.msaa}, shadows ${f.cascades} cascades at ${f.shadowDistance} m, depth texture ${f.depthTexture}, opaque texture ${f.opaqueTexture}, lodBias ${f.lodBias}, GRD as built ${String(f.drawerAsBuilt).split(' ')[0]}. Export: ${f.export}.`, '');
  out.push('| view | SetPass est. (fwd/dn/shadow) | draws main / shadow | main renderers | main tris | shadow casters | shadow tris | materials | shaders | crowd renderers / tris | ground tiles renderers / tris | probes | ms GRD off | ms GRD on |');
  out.push('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|');
  for (const v of d.views) {
    const sp = v.setPassEstimate, g = Object.values(v.groups) as any[], t = Object.values(v.ground) as any[];
    const cr = g.reduce((a, x) => a + x.renderers, 0), ct = g.reduce((a, x) => a + x.triangles, 0);
    const gr = t.reduce((a, x) => a + x.renderers, 0), gt = t.reduce((a, x) => a + x.triangles, 0);
    out.push(`| ${v.view} | ${sp.total} (${sp.forward}/${sp.depthNormals}/${sp.shadow}) | ${n(v.draws.main)} / ${n(v.draws.shadow)} | ${n(v.mainRenderers)} | ${n(v.mainTriangles)} | ${n(v.shadow.casters)} | ${n(v.shadow.triangles)} | ${v.materials} | ${v.shaders} | ${n(cr)} / ${n(ct)} (${pc(v.mainRenderers ? cr / v.mainRenderers : 0)} / ${pc(v.mainTriangles ? ct / v.mainTriangles : 0)}) | ${n(gr)} / ${n(gt)} | ${v.probesInFrame} | ${ms(v, 'Disabled')} | ${ms(v, 'InstancedDrawing')} |`);
  }
  const tx = f.textures;
  const tm = J(`texmem-${k}`);
  out.push('', `Textures: probe cubemaps ${tx.probeCubemaps.count} (${tx.probeCubemaps.gpuMiB} MiB GPU), glTF crowd textures ${tx.crowdTextures.count} (${tx.crowdTextures.gpuMiB} MiB GPU); texmem (every texture the enabled renderers bind): ${tm ? `${tm.textures} textures, ${tm.gpuMiB} MiB GPU (${tm.runtimeMiB} MiB Editor runtime)` : 'n/a'} (texmem-${k}.json).`, '');
}
// Redline's three questions, read from the old-pipeline capture
const o = data['oldtown-old'], h = data['harbour-ours'];
if (o) {
  const q = o.facts.questions, play = o.views.filter((v: any) => v.kind === 'play');
  const shown = (v: any) => `${v.view} ${v.shadow.casters}`;
  out.push('## Redline\'s three questions (from the OldTown capture under the old pipeline)', '',
    `1. **TacticalShadows / SSAO on the old play camera: no TacticalShadows.** The scene's one camera: ${q.cameras.join('; ')}; TacticalShadows in the scene: ${q.tacticalShadowsInScene}. So nothing refits the cascades and nothing turns SSAO off at ortho: the old renderer's ${q.rendererFeatures.join(', ')} runs on the play view as on any other. The cascades are the asset's (${o.facts.cascades} at ${o.facts.shadowDistance} m, measured from the camera): with the play camera 160 m back (SwapShots' pose, the stand-off the old-town plan resolves) almost nothing in the frame casts (shadow casters per play view: ${play.map(shown).join(', ')}; ours: ${h ? h.views.filter((v: any) => v.kind === 'play').map(shown).join(', ') : 'n/a'}). The old town as built would play nearly shadowless; the frames Sky saw were rendered at 200 m (DecoKitStage.WithShadows), the oldtown-old-sd200 rows. Add the depth-normals prepass (dn column) and SSAO to its cost.`,
    `2. **The sun flare at play: enabled, never on screen.** ${q.flare}; ${q.sun}. At every play view the sun is off the frame (angle from the view axis: ${[...new Set(play.map((v: any) => v.sun.angleFromViewAxis))].join('° / ')}°; an orthographic frame only holds directions along its axis), and at the golden and street poses too (${o.views.filter((v: any) => v.kind !== 'play').map((v: any) => `${v.view} ${v.sun.inFrame ? 'in frame' : 'off frame'}`).join(', ')}). URP skips an off-screen flare (no allowOffScreen), so its cost at play is the depth texture it asks for, which the old asset keeps on anyway. Ours has ${h ? h.facts.questions.flare : 'n/a'}.`,
    `3. **GRD with the old shader copies: yes.** OldTown/DecoKit/Lit Cutaway and Complex Lit Cutaway include URP's DOTS.hlsl (DOTS_INSTANCING_ON in their keyword space), as does the glTF crowd's shader: ${q.grdShaders.slice(0, 6).join('; ')}. Enabled mesh renderers whose every shader has DOTS instancing: ${q.grdRenderers.allShadersDots}; with a shader without it: ${q.grdRenderers.someShaderWithoutDots} (our Signal Dots, Cross Glow, Contact Shade and the old RobotFace); with a MaterialPropertyBlock: ${q.grdRenderers.withPropertyBlock}. Renderer: ${q.renderingPath.join(', ')} (GRD needs Forward+). With GRD set to Instanced Drawing the drawer reported ${o.views[0].frameMs.InstancedDrawing?.drawerActive} in batchmode. Its ms effect offscreen is within this capture's noise (the off/on columns); cut 4 needs the Air.`,
    '', 'Render counters (ProfilerRecorder: SetPass, Draw Calls, Batches, Triangles, Shadow Casters) read 0 around offscreen Camera.Render in batchmode, so every count above is the estimator\'s. UnityStats needs the visible Game view (not run: offscreen only).', '');
}
writeFileSync(resolve(dir, 'counts.md'), out.join('\n') + '\n');
console.log(out.join('\n'));
