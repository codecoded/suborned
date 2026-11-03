"""Package this single rejected arrangement study; never changes prior evidence."""
import ast, base64, hashlib, html, json, shutil
from pathlib import Path
E=Path(__file__).resolve().parent
ROOT=E.parents[3]
m=json.loads((E/'metrics.json').read_text())
prior=json.loads((E.parent/'foliage-refine/metrics.json').read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
refs=json.loads((E/'reference-hashes.json').read_text())
assert all(sha(ROOT/p)==h for p,h in refs.items()), 'prior evidence changed'
assert all(sha(ROOT/p)==h for p,h in m['sources'].items())
assert all(sha(ROOT/a['fbx'])==a['sha256'] for a in m['assets'].values())
assert m['texture']==prior['texture']
assert sha(Path(m['texture']['path']))==m['texture']['sha256']
for row in m['placements']:
 old=next(x for x in prior['placements'] if x['id']==row['id'])
 if 'kit' in row:assert row['kit']==old['kit'] and row['sourceWorldBounds']==old['sourceWorldBounds']
def func(path,name):
 return ast.dump(next(n for n in ast.parse(path.read_text()).body if isinstance(n,ast.FunctionDef) and n.name==name),include_attributes=False)
assert func(ROOT/'tools/decokit/recovery075_foliage.py','leaf_material')==func(E/'previous_recipe.py','leaf_material')
checks={'priorReferenceHashesUnchanged':len(refs),'sourceHashesMatch':True,'twoNewFbxHashesMatch':True,'textureBytesAndDimensionsUnchanged':True,'leafMaterialFunctionUnchangedAST':True,'placementPosesAndSourceWorldEnvelopesUnchanged':True,'renderCount':len(m['renders']),'mechanicalResult':'PASS','appearanceResult':'REJECT adoption for BOTH selections','independentReview':'NEW Glyph review required; not performed by Vela','constraints':'same source envelopes; actual occupied extrema differ; not Unity import validation'}
(E/'verification.json').write_text(json.dumps(checks,indent=2)+'\n')
shutil.copy2(ROOT/'out/scratch/vela/foliage-arrangement/render.log',E/'render.log')
rows=[]
for name,a in m['assets'].items():
 residual=ast.literal_eval(a['arrangementResidual'])
 rows.append(f"<tr><td>{name}</td><td>{a['previous']['triangles']} → {a['refined']['triangles']}</td><td>{len(a['refined']['materials'])}</td><td>{a['sprays']}</td><td>{a['rigidSprayMaxDistanceErrorMetres']:.3g} m</td><td>{len(residual)}</td></tr>")
def image(file):
 return '<figure><a href="'+file+'"><img src="data:image/png;base64,'+base64.b64encode((E/file).read_bytes()).decode()+'"></a><figcaption>'+html.escape(file)+'</figcaption></figure>'
body='''<!doctype html><html lang="en"><meta charset="utf-8"><title>Vela · arrangement correction · REJECT</title><style>body{max-width:1400px;margin:32px auto;padding:24px;font:16px/1.5 system-ui;background:#f7f4ec;color:#17302f}h1,h2{line-height:1.2}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;height:auto}figcaption{font-size:12px}td,th{border:1px solid #999;padding:8px;text-align:left}table{border-collapse:collapse}a{color:#07645a}pre{white-space:pre-wrap}</style>
<h1>Vela · one arrangement correction · REJECT adoption</h1>
<p><strong>Both selections remain rejected for Unity trial/adoption.</strong> Mechanical checks pass; the requested appearance bar does not. The tuft cream flower clears its former leaf crossing and leaf directions vary, but the rearrangement exposes smooth cores and makes the row resemble repeated cabbage collars. Some trough blooms still read as incomplete strips. At full1080p play framing the changed silhouette is visible as a broken/noisy row, not a convincing improvement over the previous proposal. No further variants or added geometry were generated.</p>
<h2>Scope and method</h2>
<p>Only tools/decokit/recovery075_foliage.py and new artifacts under foliage-arrangement were authored. Two actual selections only: PlanterRun_4_B and BedTuft_A. Existing80 sprays per mound receive rigid rotation/translation, without any new scaling/clamping/deletion. The pre-existing spray shapes are constructed exactly as before; their already-existing recipe sizing is unchanged. A seeded180-pose search per spray rejects poses outside the original lobe/source envelope and scores projected flower clearance at headings30/210. This is finite pose search in build-time tooling, not simulation or runtime behaviour.</p>
<p>All fixed core, bloom and trough polygon positions, topology and material indices remain unchanged. Spray pairwise distances, all UV coordinates, exact triangle/material counts, lobe/flower seed calls, leaf material function, texture bytes/resolution, selected placement poses and source-world bounds are checked. The source-envelope constraints are fixed; actual occupied mesh extrema change, and no identical-extrema claim is made. Bent spray normals rotate with the rigid spray. Current75% civic green/25% source texture mapping, alpha threshold and slots are preserved.</p>
<h2>Counts and rigid-transform proof</h2><table><tr><th>Selection</th><th>Triangles before → after</th><th>Slots unchanged</th><th>Sprays unchanged</th><th>Max pairwise distance error</th><th>Conservative clearance residuals</th></tr>'''+''.join(rows)+'''</table>
<p>The trough's69 residuals are projected bounding-rectangle overlaps from the finite search, not69 confirmed visible cuts or triangle intersections. Tuft has zero such residuals but still fails appearance from exposed core. These diagnostics must not be reported as universal flower-clearance proof.</p>
<h2>Controlled close comparisons · previous left, arrangement right</h2>
<p>All16 full PNGs inspected with view_image(detail=original), including both close and play headings. Same neutral Cycles16 samples/AgX, world.35, area light(-3,-4,9),1800 W, size7; camera38°, yaw30/210, same source-derived framing. These are isolated studies, not Unity/URP lighting or motion proof.</p>'''
for name in ('BedTuft_A','PlanterRun_4_B'):
 for yaw in (30,210):
  body+=f'<h3>{name} · close yaw{yaw}</h3><div class="pair">'+''.join(image(f'{name}-{tag}-close-y{yaw}.png') for tag in ('previous','refined'))+'</div>'
body+='''<h2>Full1080p equivalent play framing · previous left, arrangement right</h2><p>Vertical world span28m, horizontal49.7778m,1080px high. Open the linked PNG at original size; two-column fit is smaller than play. No crop magnification is passed off as play benefit.</p>'''
for name in ('BedTuft_A','PlanterRun_4_B'):
 for yaw in (30,210):
  body+=f'<h3>{name} · play yaw{yaw}</h3><div class="pair">'+''.join(image(f'{name}-{tag}-play-y{yaw}.png') for tag in ('previous','refined'))+'</div>'
body+='''<h2>Appearance findings</h2>
<p>Tuft yaw30: cream front bloom is intact; broad bare green core replaces leaf shelves underneath and to its right. Yaw210: all three floral faces read more clearly, but a wide dark smooth core is exposed beneath the salmon blooms. Leaves form an upright collar; the mass is not convincingly planted. Trough yaw30: fifth shrub pale front bloom clears the former green upper-edge bite, and third/sixth rear flowers read more fully. However many crowns become bare domes enclosed by vertical leaf skirts. Trough yaw210: some lower/rear pink blooms remain partial crescents. The continuous planted-edge/reference quality is not met. The unchanged core may contribute to residual concealment, but this study does not isolate all occluders.</p>
<p>Full play30/210: changed edges and flower marks can be distinguished, especially on the trough; there is no persuasive positive silhouette benefit. Tuft remains a roughly two-dozen-pixel mark. The café reference's directional pointed leaves and small clear flowers above a continuous edge are not attained by this rigid arrangement of broad shrub sprays. This is failure of this specific arrangement, not proof that every possible rearrangement is impossible. Do not claim success from the passing script.</p>
<h2>Proof limits and handoff</h2>
<p>Both original placement-envelope preflights pass within1mm at unchanged poses; cypress stays unchanged5004tri. These are source-space axis/pose preflights, not actual Unity FBX import checks. Net selected proposed geometry remains+7305 against Unity baseline (trough+6356, tuft+949), two potential renderers/eleven plant slots under the earlier strategy. No production integration, GPU/alpha/shadow measurement, motion/LOD proof, source topiary sight change, plan/sim/ratchet update, Air or full player/bot build.</p>
<p><strong>Next: Aster → Glyph for a NEW independent review of this rejected arrangement and its limits.</strong> Inputs: report.html, metrics.json, verification.json,16 PNGs and two scratch Arrangement075 FBXs. Done-check: independently judge the close and full-play failure and confirm that no Unity trial/adoption proceeds from this result. No new crew child was started here. Latch inputs and tram work untouched; production foliage default-off and geometry/sight HOLD persist. Palms/topiary/embedded planting remain open.</p>
<p><a href="README.md">Commands and handoff</a> · <a href="metrics.json">Exact metrics</a> · <a href="verification.json">Checks</a> · <a href="reference-hashes.json">Immutable prior-reference hashes</a></p></html>'''
(E/'report.html').write_text(body)
print(json.dumps(checks,indent=2))
