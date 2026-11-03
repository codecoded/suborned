"""Brief 077 Unity capture (Latch): briefs/077/unity/index.html, self-contained (captures embedded as JPEG; full PNGs local in captures/), from numbers.json
(PlantingSwap077.Capture) and out/scratch/latch/077unity/mats.json (PlantingSwap077.Mats). usage: python3 -I briefs/077/unity/make_index.py"""
import base64, html, io, json
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
N = json.loads((HERE / "numbers.json").read_text())
M = json.loads((REPO / "out/scratch/latch/077unity/mats.json").read_text())
E = html.escape

def jpg(p, w):
    im = Image.open(p).convert("RGB"); im = im.resize((w, round(w * im.height / im.width)), Image.LANCZOS)
    b = io.BytesIO(); im.save(b, "JPEG", quality=84, optimize=True); return "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode()

names = {"cafe-square": "café square", "office-front": "Records Office front", "close-troughs": "close: the café troughs (run-west-cafe)", "close-island": "close: a fountain island", "close-bed": "close: civic bed office-west"}
rows = ""
for v in N["views"]:
    vid = v["id"]; base = vid.rsplit("-y", 1)[0]
    cap = f"{names.get(base, base)}, yaw {int(v['yaw'])}, orthographic {v['ortho']}"
    rows += f"<h3>{E(cap)}</h3><div class='row'>" + "".join(
        f"<figure><a href='captures/{vid}-{s}.png'><img src='{jpg(HERE / 'captures' / f'{vid}-{s}.png', 580)}' width='580'></a><figcaption>{t}</figcaption></figure>"
        for s, t in (("before", "today (domes)"), ("after", "testbed planting"))) + "</div>"

def crow(b, a):
    sp = lambda e: e["setPassEstimate"]["total"]
    return (f"<tr><td>{b['view']}</td><td>{sp(b)} → <b>{sp(a)}</b> ({sp(a) - sp(b):+d})</td><td>{b['draws']['main']} → <b>{a['draws']['main']}</b> ({a['draws']['main'] - b['draws']['main']:+d})</td>"
            f"<td>{b['draws']['shadow']} → <b>{a['draws']['shadow']}</b> ({a['draws']['shadow'] - b['draws']['shadow']:+d})</td><td>{b['mainTriangles']:,} → <b>{a['mainTriangles']:,}</b> ({a['mainTriangles'] - b['mainTriangles']:+,})</td>"
            f"<td>{b['shadow']['triangles']:,} → <b>{a['shadow']['triangles']:,}</b> ({a['shadow']['triangles'] - b['shadow']['triangles']:+,})</td><td>{b['mainRenderers']} → {a['mainRenderers']}</td>"
            f"<td>{b['setPassEstimate']['forward']}/{b['setPassEstimate']['depthNormals']}/{b['setPassEstimate']['shadow']} → {a['setPassEstimate']['forward']}/{a['setPassEstimate']['depthNormals']}/{a['setPassEstimate']['shadow']}</td></tr>")
counts = "".join(crow(b, a) for b, a in zip(N["countsBefore"], N["countsAfter"]))
s = N["swap"]
placed = "".join(f"<tr><td>{E(p['mass'])}</td><td>{E(p['piece'])}</td><td>{p['size']}</td><td>{', '.join(map(str, p['heights']))}</td><td>{', '.join(f'{t:,}' for t in p['trisPerLod'])}</td><td>{E(p['playZoomLod'])}</td><td>{p['shadowOff']}</td><td>{E(p['how'])}</td></tr>" for p in s["placed"])
match = "".join(f"<tr><td>{E(k)}</td><td>{E(v)}</td><td>{E(s['stoneMatch'].get(k, '-'))}</td></tr>" for k, v in s["bakedMatch"].items() if "/tuft-" not in k)
tufts = [v for k, v in s["bakedMatch"].items() if "/tuft-" in k]
mats = "".join(f"<tr><td>{E(m['file'])}</td><td><code>{m['guid']}</code></td><td>{E(m['shader'])}</td><td>{E(m['keywords'])}</td><td>{m['cull']}</td><td>{m['shadowCasterInMat']}</td><td>{E(', '.join(m['textures']))}</td></tr>" for m in M["materials"])
texs = "".join(f"<tr><td>{E(t['file'])}</td><td><code>{t['guid']}</code></td><td>{E(t['from'])}</td><td>{t['kind']}</td><td><code>{t['sha256'][:16]}…</code></td></tr>" for t in M["textures"])
page = f"""<!doctype html><html><head><meta charset='utf-8'><title>077 · the testbed planting in Unity</title>
<style>body{{font:14px/1.45 -apple-system,Helvetica,sans-serif;max-width:1220px;margin:24px auto;padding:0 16px;color:#17302f}}h2{{margin-top:32px;border-bottom:1px solid #d2e6e2}}.box{{background:#f7f4ec;border:1px solid #d2e6e2;padding:10px 14px;margin:12px 0}}
table{{border-collapse:collapse;font-size:12.5px;margin:8px 0}}td,th{{border:1px solid #d2e6e2;padding:3px 7px;text-align:left;vertical-align:top}}.row{{display:flex;gap:10px;flex-wrap:wrap}}figure{{margin:2px 0}}figcaption{{font-size:12px;color:#456}}img{{display:block}}code{{background:#eef5f3;padding:0 3px}}</style></head><body>
<h1>077 · the testbed planting in Unity: today | testbed</h1>
<p>Latch, {N['date']}, Unity {N['unity']}. For Sky, before anything is adopted. A probe copy of the swap (<code>Assets/Scenes/probe-planting077.unity</code>, git-ignored; OldTown.unity opened read-only, never saved). Every dome-family placement of the Handover zone has been swapped for its testbed replacement from <code>out/decokit-077</code> (Vela rev 3), at its catalogue LODs. The swap's play camera with our play look (OldTownPlayLook), by day. <b>No night:</b> the globe lamps' night light hasn't come from Glint yet (READY-splice / READY-pieces list it as Glint's), so the lamps stand unlit and only day is captured.</p>
<div class='box'><b>Read.</b> At play zoom the café square and the Office front now carry the testbed's planting: dark glossy greens with coral and gold blooms in the troughs, cupped coral and gold heads on the fountain islands, the five civic beds filled bed-level, and the Records crown and café roof troughs planted. The domes' bright lime cushions and confetti cubes are gone. It reads darker and denser, closer to a real planted square, and the bloom colour is what carries at play zoom. Cost at the four play stops (After072's estimate, offscreen): +4 SetPass, +24 to +58 draws, +29 to +86 shadow draws, +11.6k to +45.5k triangles (+1.6 % to +8.1 %).</div>
<h2>What changed</h2><ul>
<li><b>Kit materials and textures</b> in <code>out/decokit-077/Materials</code> and <code>Textures</code>, for <code>bun run kit --catalogue --extra out/decokit-077</code>. GUIDs are md5("suborned.kit077/" + file), so they're deterministic. The probe uses its own copies with other GUIDs, so the kit copy never meets a duplicate.</li>
<li><code>unity/Assets/Suborned/Editor/OldTown/Swap/PlantingSwap077.cs</code> (new): <code>Mats</code> and <code>Capture</code>.</li>
<li>Probe scene and its assets (git-ignored): <code>unity/Assets/Scenes/probe-planting077.unity</code> + <code>probe-planting077/</code> (the FBXs, sha256 against the manifest; the edited batch meshes; the probe materials).</li></ul>
<table><tr><th>material</th><th>guid</th><th>shader</th><th>keywords</th><th>cull</th><th>shadow pass in the .mat</th><th>textures</th></tr>{mats}</table>
<table><tr><th>texture</th><th>guid</th><th>from</th><th>kind</th><th>sha256</th></tr>{texs}</table>
<p>Masks: A = round(255 − k × ORM.g) at the source's size. Botanical: k 0.49 on the leaf rows, 0.64 on the petal rows. Soil: k 1. Import settings follow briefs/077/troughs/metrics.json: BC7 colour and masks, BC5 normals, NPOT to nearest, full mips, aniso 8, the atlas clamped and the soil repeating. DK_Opal_TB is today's shader with #fffdf0, smoothness 0.75 and emission #fff1d2 × 0.06 (the testbed's day value; night 3.5 is Glint's). The chrome housing is the kit's DK_Chrome.</p>

<h2>How the swap was made</h2><ul>
<li><b>Targets</b> (the pinned export {E(N['world'])}, the Handover zone): {E(', '.join(f'{k} ×{v}' for k, v in s['targets'].items()))}.</li>
<li><b>Baked pieces</b> (all but CafeUpper_3) leave the civic batches by exact world-triangle match: each piece's LOD0 placed as KitPlacer.Bake placed it, at 1 mm, with a second grid offset by half a millimetre for rounding edges. Matched {E(s['bakedMatchTotal'])}. What's left unmatched is 2 leaf triangles on one island, plus tuft fragments, which the bed rule takes out with the hedge. The rebuild's stone slots take the material the dome's stone was batched in (the restyle's), by slot name. The rebuilds' stonework lands on the dome's exactly (stone match below).</li>
<li><b>CafeUpper_3</b> (a fade piece, an unpacked instance): its renderers are off, and the rebuild takes its transform, materials and rendering layers.</li>
<li><b>Civic beds</b>: the hedge prisms (centroid inside the hedge ring, above the kerb top) and the tufts leave the batches ({E(', '.join(f'{k} {v}' for k, v in s['hedgeTrianglesOut'].items()))} triangles). CivicBedPlanting_* goes in at the manifest's fit, with the soil at the kerb top + 3 mm (beds.ts).</li>
<li><b>Companions</b> at their parents' transform. One GlobeLamp_TB per PlanterArc at the manifest's arc-frame offset.</li>
<li><b>LODGroups</b>: L0 plus each LOD file, switching at the row's own lods[].screen. The last level culls by KitLods' rule, and a level that would switch in below the cull is dropped, as in KitLods (CafeUpper_3's 0.003). lodSize 4 is applied where the row sets it. Every soil object is shadows off.</li></ul>
<table><tr><th>placement</th><th>piece</th><th>LODGroup size</th><th>switch heights</th><th>tris per LOD</th><th>at play zoom</th><th>soil renderers shadow off</th><th>how</th></tr>{placed}</table>
<table><tr><th>baked piece</th><th>triangles matched out of the batches</th><th>the rebuild's L0 stone on the dome's</th></tr>{match}</table>
<p>The 92 tufts matched {sum(int(t.split('/')[0]) for t in tufts):,} of {sum(int(t.split('/')[1].split(' ')[0]) for t in tufts):,} triangles; the rest are inside the hedge rings and went with the hedge.</p>

<h2>Counts at the play stops (before → after)</h2>
<p>After072's estimator (the 075 Perf A/B method): SetPass = distinct shader variants per pass (forward / depth-normals / shadow); draws = sub-meshes before batching; triangles = what the camera draws with the LOD it picks. These are estimates, because UnityStats needs the visible Game view and the render counters read 0 offscreen. Redline measures on the Air.</p>
<table><tr><th>view</th><th>SetPass</th><th>draws</th><th>shadow draws</th><th>triangles</th><th>shadow triangles</th><th>renderers</th><th>SetPass fwd/dn/shadow</th></tr>{counts}</table>
<p>Where the cost comes from. The four SetPass are forward +2 (the leaf shader's variant, and the soil's <code>_METALLICSPECGLOSSMAP _NORMALMAP</code> variant of today's shader, which nothing used before), depth-normals +1 and shadow +1 (the leaf shader's). The draws come from each piece now being its own LODGroup instance (2-3 sub-meshes and a soil renderer at each level), where the domes were merged into the civic batches. The triangles come mostly from the Office front's planted crown trough and civic beds at their gameplay level (RecordsTower_Planting 19,430, the beds 6,151-17,138).</p>

<h2>Captures (day, play look)</h2>{rows}

<h2>Open, and what adoption needs from KitLods (Latch)</h2><ul>
<li><b>KitLods</b>: it needs to build L1-L3 from these rows, honour lodSize, and set the soil objects shadows off from <code>objects[].shadows</code>. Watch out: <code>LODGroup.SetLODs</code> resets the size, so lodSize has to be set after it, which this probe does. The <b>play-zoom guard</b> fails any civic-layer group whose L1 switches nearer than 36 m. These rows switch at play zoom by design (Sky: gameplay LODs at play zoom), so the guard needs an exemption for planting rows. Otherwise the build fails.</li>
<li><b>Batching</b>: the baked domes cost no draws of their own. If the extra draws matter on the Air, the gameplay level of the small pieces could be baked into the civic batches at the price of their LODs. That's Redline's call from the Air numbers.</li>
<li><b>Night</b>: waits on Glint's lamp light (#fff1d8, 5.8 m, decay 2, no shadows, 2.31 m) and the opal's night emission.</li>
<li><b>Before the kit copy</b>: nothing to clean. The probe's GUIDs differ from the kit GUIDs.</li></ul>
</body></html>"""
(HERE / "index.html").write_text(page)
print("index.html", round(len(page) / 1e6, 2), "MB")
