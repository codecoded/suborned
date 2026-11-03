"""Brief 077 leaf shader (Latch): writes briefs/077/shader/proof.json (the before/after proof, summarised from the Unity proof runs in
out/scratch/latch/077shader/proof/<tag>/proof.json) and briefs/077/shader/index.html (self-contained: captures embedded as JPEG; the full PNGs
stay local in captures/). Reads numbers.json, which LeafShader077.Review writes.
usage: python3 -I briefs/077/shader/make_index.py"""
import base64
import html
import io
import json
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
PROOF = REPO / "out/scratch/latch/077shader/proof"
N = json.loads((HERE / "numbers.json").read_text())


def load(tag):
    return json.loads((PROOF / tag / "proof.json").read_text())


def cmp_summary(tag):
    d = load(tag)
    c = d["compare"]
    return {
        "tag": tag, "against": c["against"], "framesIdentical": c["identical"], "framesDiffering": c["differing"],
        "compiledProgramsCompared": c.get("compiledProgramsCompared"),
        "compiledProgramsChanged": c.get("compiledProgramsChanged") if isinstance(c.get("compiledProgramsChanged"), int) else len(c.get("compiledProgramsChanged") or []),
        "changedPrograms": c.get("compiledProgramsChanged") if isinstance(c.get("compiledProgramsChanged"), list) else None,
        "frames": [{k: r[k] for k in ("name", "differing", "maxChannelDiff", "bbox")} for r in c["rows"]],
        "shaderSha256": d["shader"]["sha256"], "existingMaterialsOnTheSibling": d.get("existingMaterialsOnTheSibling", False),
    }


runs = [
    ("before-b", "control: the same state twice (before-a, before-b) gives the run-to-run noise floor"),
    ("after", "REJECTED route: the two keywords added to DK_LitCutaway.shader itself, against before-b"),
    ("revert", "DK_LitCutaway.shader put back to HEAD, against the rejected route: the old frames come back"),
    ("sibling", "stress test (not the shipping set-up): every existing DecoKit/Lit Cutaway material moved in memory onto the sibling, against revert"),
    ("final", "SHIPPING state: DK_LitCutaway.shader byte-identical to HEAD, the sibling and leaf includes present, against before-b"),
]
a = load("before-a")
final = load("final")
proof = {
    "schema": "suborned.leaf077.proof/1", "brief": "077", "author": "Latch",
    "method": "Unity batch (briefs/075/oldbuild/swap/tools/unityrun.ts), LeafShader077.Proof: (1) every material asset on DecoKit/Lit Cutaway and its keyword set; (2) each pass's vertex and fragment program "
              "compiled for Metal / StandaloneOSX for every existing keyword set, with no globals and with a play set, sha256 of the bytes; (3) 1920x1080 captures (2x supersampled) of the swap "
              "(OldTown.unity: our play look at the café square and the Office front, yaw 30 and 210; the presentation look at the same poses and at street level) and our district "
              "(HarbourDistrict.unity, its play camera, the same four poses and the Office front at night), each pixel-diffed against an earlier run. Neither scene is saved.",
    "existingMaterials": {k: a["materials"][k] for k in ("count", "withLeafKeywords", "byCull")} | {"keywordSets": len(a["materials"]["keywordSets"])},
    "headShaderSha256": final["shader"]["sha256"],
    "compiledPrograms": final["compiled"]["programs"],
    "leafVariants": {"programs": final["leafCompiled"]["programs"], "failed": final["leafCompiled"]["failed"]},
    "runs": [{"what": w} | cmp_summary(t) for t, w in runs],
    "keywordSpace": {"original": len(final["shader"]["keywordSpace"]), "sibling": len(final["leafShader"]["keywordSpace"]),
                     "inserted": [x for i, x in enumerate(final["leafShader"]["keywordSpace"]) if x in ("_DK_VERTEX_COLOR", "_DK_TWO_SIDED")],
                     "note": "the two keywords enter the keyword space at index 33-34, ahead of the global (overridable) keywords, so every global's index shifts by 2"},
}
(HERE / "proof.json").write_text(json.dumps(proof, indent=1) + "\n")


def jpg(path, w=640, crop=None, q=82):
    im = Image.open(path).convert("RGB")
    if crop:
        im = im.crop(crop)
    im = im.resize((w, round(w * im.height / im.width)), Image.LANCZOS)
    b = io.BytesIO()
    im.save(b, "JPEG", quality=q, optimize=True)
    return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()


def img(path, cap, w=640, crop=None):
    return f"<figure><a href='{html.escape(str(path.relative_to(HERE)) if str(path).startswith(str(HERE)) else str(path))}'><img src='{jpg(path, w, crop)}' width='{w}'></a><figcaption>{html.escape(cap)}</figcaption></figure>"


E = html.escape
cap = HERE / "captures"
views = [("play-y030", "play zoom, yaw 30"), ("play-y210", "play zoom, yaw 210"), ("close-run-y030", "close: PlanterRun_4_A_TB, yaw 30"), ("close-run-y210", "close: PlanterRun_4_A_TB, yaw 210"), ("close-arc-y030", "close: PlanterArc_TB, yaw 30")]
play_crop = (480, 240, 1440, 780)  # the play-zoom frames: the two pieces, at 1:1 in the middle of the frame
parts = []
for look, lname in (("play", "the swap's play look (OldTownPlayLook)"), ("presentation", "the presentation look (the study camera, the old stage's full look)")):
    for phase in ("day", "night"):
        rows = []
        for v, vname in views:
            crop = play_crop if v.startswith("play") else None
            rows.append("<div class='row'>" + img(cap / f"{look}-{phase}-on-{v}.png", f"ON · {vname}" + (" (crop 960x540 of the 1080p frame, 1:1)" if crop else ""), 560, crop)
                        + img(cap / f"{look}-{phase}-off-{v}.png", f"OFF (today's DecoKit/Lit Cutaway) · {vname}", 560, crop) + "</div>")
        parts.append(f"<h3>{E(lname)} · {phase}</h3>" + "".join(rows))
iso = "<div class='row'>" + "".join(img(cap / f"play-day-{s}-close-run-y210.png", t, 420, (560, 300, 1360, 750)) for s, t in
                                    (("off", "off: today's shader"), ("colour", "vertex colour only"), ("facing", "back faces only"), ("on", "both (the leaf shader)"))) + "</div>"
iso += "<div class='row'>" + "".join(img(cap / f"play-day-{s}-close-arc-y030.png", t, 420, (420, 220, 1500, 860)) for s, t in
                                     (("off", "off"), ("colour", "vertex colour only"), ("facing", "back faces only"), ("on", "both"))) + "</div>"
rej = "<div class='row'>" + img(PROOF / "before-b/harbour-play-office-y210.png", "HarbourDistrict, Office front yaw 210: before", 560) + img(PROOF / "after/harbour-play-office-y210.png", "the same frame with the keywords added to DK_LitCutaway.shader itself (rejected)", 560) + "</div>"

c_on, c_off = N["counts"]["shaderOn"], N["counts"]["shaderOff"]
def sp(e): return e["setPassEstimate"]
def ms(e): return " / ".join(f"{m} {e['frameMs'][m]['median']}" for m in e["frameMs"])
count_rows = "".join(f"<tr><td>{st}</td><td>{e['view']}</td><td>{sp(e)['forward']}</td><td>{sp(e)['depthNormals']}</td><td>{sp(e)['shadow']}</td><td><b>{sp(e)['total']}</b></td><td>{e['draws']['main']}</td><td>{e['draws']['shadow']}</td><td>{e['mainTriangles']:,}</td><td>{e['shadow']['triangles']:,}</td><td>{ms(e)}</td></tr>"
                     for st, c in (("on", c_on), ("off", c_off)) for e in c["estimator"])
prog = lambda d: "".join(f"<tr><td>{E(k)}</td><td>{v}</td></tr>" for k, v in d.items())
mesh_rows = ""
for m in N["meshes"]:
    for r in m["submeshes"]:
        mesh_rows += f"<tr><td>{m['id']}</td><td>{r['renderer'][-4:]}</td><td>{r['material']}</td><td>{r['triangles']:,}</td><td>{r['vertices']:,}</td><td>{r['uvV'][0]}..{r['uvV'][1]}</td><td>{r['colourFormat']}</td><td>{r.get('colourMin', '-')[:3] if r.get('colourMin') else '-'}</td><td>{r.get('colourMax', '-')[:3] if r.get('colourMax') else '-'}</td><td>{r.get('colourMean', '-')[:3] if r.get('colourMean') else '-'}</td></tr>"
mat_rows = "".join(f"<tr><td>{m['name']}</td><td>{m['shader']}</td><td>{E(m['keywords'])}</td><td>{m['cull']}</td><td>{m['baseColour']}</td><td>{m['bumpScale']}</td><td>{m['shadowCaster']}</td><td>" +
                   "<br>".join(f"{k} {t['size']} {t['format']} {t['mips']} mips {t['MiB']} MiB {t['wrap']}" for k, t in m["textures"].items()) + "</td></tr>" for m in N["materials"])
run_rows = ""
for r in proof["runs"]:
    diff = [f for f in r["frames"] if f["differing"]]
    run_rows += (f"<tr><td>{E(r['tag'])}</td><td>{E(r['against'])}</td><td>{E(r['what'])}</td><td>{r['framesIdentical']} / {r['framesIdentical'] + r['framesDiffering']}</td>"
                 f"<td>{r['compiledProgramsCompared']}</td><td><b>{r['compiledProgramsChanged']}</b></td><td>" + "<br>".join(f"{E(f['name'])}: {f['differing']:,} px, max {f['maxChannelDiff']}" for f in diff[:6]) + (" …" if len(diff) > 6 else "") + "</td></tr>")
lod_rows = "".join(f"<tr><td>{l['id']}</td><td>{l['size']} m</td><td>{', '.join(str(h) for h in l['heights'])}</td><td>{l['playZoomRelativeHeight']}</td></tr>" for l in N["lodGroups"])
lods_play = next(c["lods"] for c in N["captures"] if c["view"] == "play-y030" and c["shader"] == "on")
lods_close = next(c["lods"] for c in N["captures"] if c["view"] == "close-run-y030" and c["shader"] == "on")
masks = N["build"]["masks"]

page = f"""<!doctype html><html><head><meta charset='utf-8'><title>077 · the leaf shader</title>
<style>body{{font:14px/1.45 -apple-system,Helvetica,sans-serif;max-width:1180px;margin:24px auto;padding:0 16px;color:#17302f}}h1{{font-size:22px}}h2{{margin-top:34px;border-bottom:1px solid #d2e6e2}}
.box{{background:#f7f4ec;border:1px solid #d2e6e2;padding:10px 14px;margin:12px 0}}.warn{{background:#fff4e0;border-color:#f0a020}}table{{border-collapse:collapse;margin:8px 0;font-size:12.5px}}td,th{{border:1px solid #d2e6e2;padding:3px 7px;vertical-align:top;text-align:left}}
.row{{display:flex;gap:8px;flex-wrap:wrap}}figure{{margin:4px 0}}figcaption{{font-size:12px;color:#456}}code{{background:#eef5f3;padding:0 3px}}img{{display:block}}</style></head><body>
<h1>077 · the leaf shader: vertex colour, two-sided leaves, the ORM gloss</h1>
<p>Latch, {N['date']}. Unity {N['unity']}. The testbed's leaves and petals (three.js <code>MeshStandardMaterial</code>, <code>vertexColors</code>, <code>side: DoubleSide</code>, roughness 0.49 / 0.64 x ORM.g) in our DecoKit cutaway shader, on two of Vela's candidates in an isolated review scene. Review only: nothing is placed in the game, no catalogue, plan, sim, collider or nav change; OldTown.unity and HarbourDistrict.unity were opened read-only and never saved.</p>

<div class='box'><b>Verdict.</b> The three missing pieces are in, as a <b>sibling shader</b>, <code>DecoKit/Lit Cutaway Leaf</code>, with two opt-in keywords (<code>_DK_VERTEX_COLOR</code>, <code>_DK_TWO_SIDED</code>) and a build-time mask map from the testbed ORM. Existing materials render <b>byte-identically</b>: DK_LitCutaway.shader is unchanged (sha256 {proof['headShaderSha256'][:16]}…, = HEAD), all {proof['compiledPrograms']} of its compiled Metal programs are unchanged, and 13 of the 14 proof frames are bit-identical. The 14th is a frame that is already noisy run to run. SRP Batcher: compatible (code 0) for both shaders. Cost in the review scene at play zoom (estimate): +1 forward, +1 depth-normals, +1 shadow SetPass; draws and triangles unchanged. The leaves now read as the testbed's: darker, varied greens from the vertex tints, and coral and gold cups that keep their colour from every side. With today's shader the petals go pale blue-white, because their back faces reflect the sky.</div>

<div class='box warn'><b>Why a sibling, not a keyword in the existing shader.</b> I tried the keyword route first: the same two keywords in DK_LitCutaway.shader, its four pass includes switched, line count unchanged. Every program of the passes I edited compiled to the same bytes. But the ShadowCaster and DepthOnly programs with the cutaway's global <code>_DK_CUT</code> did not: 36 programs changed, in passes the edit never touched. In HarbourDistrict, every frame changed. The towers' shadows lost their glass strips (below: 1.5 M of 2.07 M pixels differ). Putting the shader back to HEAD restored the old frames exactly. The cause is in the data: the two new local keywords enter the shader's keyword space at index 33 and 34, ahead of every global keyword, so each global's index shifts by 2. The variant the shadow pass picks under <code>_DK_CUT</code> then changes. A sibling leaves DecoKit/Lit Cutaway's keyword space alone, and it costs the same forward SetPass (a new variant and a new shader both get their own SRP batch).{rej}</div>

<h2>What changed</h2><ul>
<li><code>unity/Assets/Suborned/Shaders/DecoKit/DK_LitCutawayLeaf.shader</code> (new, generated): DK_LitCutaway.shader renamed, with its four pass includes pointed at the leaf includes. Same properties and UnityPerMaterial.</li>
<li><code>DK_LeafForward.hlsl</code>, <code>DK_LeafGBuffer.hlsl</code>, <code>DK_LeafDepthNormals.hlsl</code>, <code>DK_LeafMeta.hlsl</code>, <code>DK_Leaf.hlsl</code> (new, generated): with no leaf keyword each includes URP's own pass file unchanged. With a keyword, each is URP 17.6.0's pass file plus marked edits: COLOR through Attributes and Varyings, and albedo x colour.rgb (forward, GBuffer, Meta). The fragment's facing comes in as Shader Graph's <code>cullFace</code> (FRONT_FACE_SEMANTIC, fragment stage only, last in Varyings). On a back face the normal and the whole tangent frame (tangent and sign) flip (forward, GBuffer, DepthNormals), as three's <code>faceDirection</code>. The ShadowCaster stays URP's, Cull from <code>_Cull</code> (0 on leaves and petals).</li>
<li><code>briefs/077/shader/make_leaf_hlsl.py</code>: writes all of the above from the URP package files and DK_LitCutaway.shader, each edit asserted to match once. Re-run it when either changes.</li>
<li><code>unity/Assets/Suborned/Editor/OldTown/Swap/LeafShader077.cs</code> (new): Proof, Build (the review scene and the mask derivation), Review (captures and numbers).</li>
<li>DK_LitCutaway.shader: <b>unchanged</b> (it was edited and then put back to HEAD during the proof).</li>
<li>Review scene (git-ignored): <code>unity/Assets/Scenes/probe-leafshader077.unity</code> + <code>probe-leafshader077/</code> (Vela's two FBXs, sha256-checked; the testbed maps; the masks; three materials; the stone-only meshes; night.json).</li></ul>

<h2>The before/after proof</h2>
<p>{E(proof['method'])} Existing materials on DecoKit/Lit Cutaway: {proof['existingMaterials']['count']} in {proof['existingMaterials']['keywordSets']} keyword sets ({proof['existingMaterials']['byCull']}), {proof['existingMaterials']['withLeafKeywords']} with a leaf keyword.</p>
<table><tr><th>run</th><th>against</th><th>what</th><th>frames identical</th><th>programs compared</th><th>programs changed</th><th>frames that differ</th></tr>{run_rows}</table>
<p>The noise floor (before-a against before-b, identical state): 2 frames differ run to run, a sign at the HarbourDistrict café y210 (3 px) and the swap's street-level view (fountain and water, 8,423 px, max 10). In the final run only the street view differs (78 px, max 1), within that floor. The sibling stress test moves every existing material onto the sibling, so it is not the shipping set-up. Its frames differ by more than 3 levels in at most 1,924 px a frame, all along a few silhouette edges (the café roof's rim, a kerb line: draw order changes once the shader differs). The rest differ by 1 to 3 levels. Nothing in the shadow pass changes. Leaf variants compile clean for Metal: {proof['leafVariants']['programs']} programs, {proof['leafVariants']['failed']} errors. Compile messages, both shaders: only the existing DK_SurfaceDetail.hlsl:63 "potentially uninitialized" warning. Console: no errors or exceptions; LeafShader077.cs has no warnings.</p>

<h2>The mask (ORM roughness to URP smoothness)</h2>
<p>URP Lit with <code>_METALLICSPECGLOSSMAP</code> reads metallic from R and smoothness from A (x <code>_Smoothness</code> = 1). The mask is derived at build time from the testbed ORM, deterministic, at the source's own size: R G B = 0, A = round(255 - k x ORM.g). One mask serves leaves and petals because the atlas keeps them apart: leaves are rows 0-3 (v 0.5-1, k = 0.49), petals rows 4-7 (v 0-0.5, k = 0.64). The import confirms it on every LOD: leaf UVs v 0.506-0.994, petal UVs 0.006-0.494. Soil: k = 1. It's a format conversion, not a texture cut. The ORM is not imported, so texture memory stays Vela's 9.0 MiB.</p>
<table><tr><th>mask</th><th>from</th><th>rule</th><th>size</th><th>png sha256</th></tr>{''.join(f"<tr><td>{k}</td><td>{v['from']}</td><td>{E(v['rule'])}</td><td>{v['size']}</td><td><code>{v['pngSha256'][:16]}…</code></td></tr>" for k, v in masks.items())}</table>

<h2>Each fix on its own</h2><p>Play look, day, close. Vertex colour darkens and varies the leaves (the tints and the root-to-tip shade). The back-face flip brings back the petals' colour: without it a cupped petal seen from its back lights as if facing away and mirrors the sky.</p>{iso}

<h2>Captures</h2><p>Play zoom = orthographic 14, pitch 38, stand-off 160, yaw 30 and 210, 1920x1080 (2x supersampled); the two play-zoom columns show the middle 960x540 at 1:1. Close = orthographic 2.6 (run) and 4.2 (arc) at the same pitch. Night = our night (HarbourDistrict's LightPhase: moon, trilight ambient, night sky, night reflection cube, night grade on the play look), plus one street-lamp light per piece as a stand-in (light.json night.lamps street: spot 85°, range {N['night']['lampRange']} m, intensity {N['night']['lampIntensity']}). The old stage has no night: the presentation night is its own volumes under our night light. Click any picture for the full PNG (local).</p>
{''.join(parts)}

<h2>Numbers</h2>
<p><b>SRP Batcher</b> after a render: DecoKit/Lit Cutaway {E(N['srpBatcherAfterRender'])}, DecoKit/Lit Cutaway Leaf {E(N['srpBatcherLeafAfterRender'])}.</p>
<p><b>SetPass and draws</b>: After072's estimator (the 075 Perf A/B method: distinct shader variants per pass, draws = sub-meshes before batching), the play camera, the review scene (the two pieces, their stone, the ground). These are estimates. UnityStats needs the visible Game view, and the render counters read 0 offscreen. Redline measures in Unity and on the Air in the placement round. Frame ms: batch mode, offscreen, only a rough guide.</p>
<table><tr><th>shader</th><th>view</th><th>forward</th><th>depth-normals</th><th>shadow</th><th>SetPass</th><th>draws</th><th>shadow draws</th><th>tris</th><th>shadow tris</th><th>ms (GRD off / on)</th></tr>{count_rows}</table>
<p>The review pieces' programs at play zoom, shader on (what the SRP Batcher batches by):</p><table><tr><th>forward</th><th>draws</th></tr>{prog(c_on['pieces']['forwardPrograms'])}</table><table><tr><th>shadow</th><th>draws</th></tr>{prog(c_on['pieces']['shadowPrograms'])}</table>
<p>Leaves and petals share one forward variant, so the sibling adds one program per pass, not one per material. The keyword route would have shared the shadow program with today's (the shadow pass has no leaf keyword), so the sibling costs one extra shadow SetPass. That's the price of leaving the existing shader alone.</p>
<p><b>LODs</b> (imported as LODGroups from Vela's _LODn names; heights from the testbed's own rule, a leaf of 0.4 m (troughs) or 0.55 m (fountain) at its pixel thresholds, at 1080 rows):</p>
<table><tr><th>piece</th><th>LODGroup size</th><th>heights</th><th>relative height at play zoom</th></tr>{lod_rows}</table>
<p>At play zoom: {E(str(lods_play))}. Close (run): {E(str(lods_close))}.</p>
<p><b>Imported meshes</b> (vertex colour unconverted: Unity keeps the linear values as UNorm8. Vela's source floats for PlanterRun_4_A gameplay foliage are min (0.1225, 0.1755, 0.1538), max (0.5776, 0.6308, 0.46). The 8-bit step costs at most 0.002.):</p>
<table><tr><th>piece</th><th>LOD</th><th>material</th><th>tris</th><th>verts</th><th>uv v</th><th>colour</th><th>min</th><th>max</th><th>mean</th></tr>{mesh_rows}</table>
<p><b>Materials</b>:</p><table><tr><th>material</th><th>shader</th><th>keywords</th><th>cull</th><th>base</th><th>bump</th><th>shadow pass</th><th>textures</th></tr>{mat_rows}</table>

<h2>Look read</h2><ul>
<li><b>Day, play look.</b> With the shader on, the troughs read as the testbed's: a dense, dark, varied green with coral and gold cups standing clear. Off, the green is lighter and flatter, and the blooms vanish into pale blue-white specks. That's the "flat and too light" in the brief. At play zoom the planting is small (a 4 m run is about 150 px), so the bloom colour is most of what reads, and it only reads with the flip on.</li>
<li><b>Gloss.</b> The mask gives the testbed's glossy leaves: sharp sun highlights on the broad leaves. Under our play sun they show as small white glints at play zoom, a little hotter than the testbed's (three's light units differ). Glint should judge them in the grade before Sky sees it.</li>
<li><b>Darker than the domes</b>, as Vela warned. The vertex tints average 0.40-0.49 linear, so the leaves sit well below today's candy domes. That's the testbed's own look, not a bug.</li>
<li><b>Night.</b> Under the lamp pools the leaves stay dark green with a lamp glint and the petals read warm. Off, the petals glow pale cyan, the clearest failure of the old shader. Outside the pools the planting goes nearly black in the play look. Glint's call, with the real lamps, in the placement round.</li>
<li><b>Presentation look</b> (old grade and bloom): the same story, warmer and softer. The pale strap and fan leaves read as cream (the atlas's variegated region), as in the testbed.</li></ul>

<h2>Open issues</h2><ul>
<li><b>Measurements</b>: SetPass and draws are estimates. Redline measures the placed set in Unity and on the Air (next round).</li>
<li><b>Soil casts a shadow</b>: Vela's spec says the soil doesn't, but its ShadowCaster pass flag doesn't stick (URP's material validation re-enables it), and soil shares the renderer with the leaves. Cheap (2 to 54 triangles), but it's a shadow draw per piece. Vela can split the soil out, or the kit writer can set it in the .mat. The soil has no vertex colour in the joined mesh (it imports as 0): it must never take <code>_DK_VERTEX_COLOR</code>.</li>
<li><b>The keyword-space finding</b> (DK_LitCutaway's <code>_DK_CUT</code> shadow variant moves when local keywords are added ahead of the globals) applies to any future edit of DK_LitCutaway, DK_ComplexLitCutaway and FacadeLit. Redline and Splice should know: add keywords at the end, or prove the shadow pass after.</li>
<li><b>The sibling must follow DK_LitCutaway.shader</b>: it's generated from it (its header records the source sha256). A check that fails when they drift belongs to Splice.</li>
<li><b>SH per vertex</b>: with EVALUATE_SH_VERTEX / MIXED, URP evaluates part of the ambient per vertex with the front normal. The flip covers the per-pixel part (our desktop setting).</li>
<li>Placement, the kit materials (unity_mats.py / the catalogue) and the dome rejection are the next round (Vela's pieces, Splice's rule, then Latch).</li></ul>
<p>Next owner: Glint (the look in the grade, the gloss and the night), then Vela and Splice (kit materials on DecoKit/Lit Cutaway Leaf, the soil split), then Latch places everything default-off and Redline measures.</p>
</body></html>"""
(HERE / "index.html").write_text(page)
print("index.html", round(len(page) / 1e6, 2), "MB; proof.json", len(json.dumps(proof)))
