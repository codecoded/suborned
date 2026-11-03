"""Portable review board for the material pilot (embedded images, no server)."""
import base64
import html
import json
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(sys.argv[1]) if len(sys.argv)>1 else ROOT/'out/scratch/codex/material-pilot'
manifest=json.loads((OUT/'manifest.json').read_text());checks=json.loads((OUT/'checks.json').read_text())
style='''body{margin:0;font:15px/1.55 system-ui;color:var(--foreground,#dce5e9)}h2{font-size:23px;margin:0 0 10px}p{margin:12px 0}select{font:inherit;background:var(--card,#253239);color:inherit;border:1px solid var(--border,#53676c);padding:8px;border-radius:6px}section{margin:24px 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}img{display:block;width:100%;border-radius:8px}.muted{color:var(--muted-foreground,#9cabb0)}figcaption{font-size:13px}figure{margin:0}.verdict{border-left:3px solid #8cc4b9;padding-left:14px}strong{font-weight:650}details{margin:18px 0}code{font-size:12px}button{padding:8px}'''
observations={'cafe':'Hold for Unity: weave is restrained and nearly disappears at tactical size. No new façade treatment duplicates the already-landed 072/075 work.', 'planter':'Do not roll out this leaf candidate: it adds mottling but leaves the rounded toy-like masses intact. A silhouette/leaf-cluster change is needed next.', 'scout':'Hold for Unity: bounded AO/smoothness at the 512 tier, with metallic kept at zero. Close rest-pose evidence only; movement, indirect AO and game lighting still need verification.'}
images={}
for p in OUT.glob('*.png'):
 if any(p.name.startswith(x+'-') for x in observations) and ('-before.' in p.name or '-candidate.' in p.name):
  images[p.stem]='data:image/png;base64,'+base64.b64encode(p.read_bytes()).decode()
body='<h2>First material pass: compare before rollout</h2><p>The current façades already have the later texture/restyle work. These candidates test the remaining cloth, civic planting and Scout tactical-tier response on unchanged installed meshes.</p><p><label for="view">View </label><select id="view"><option value="day-close">Day · close</option><option value="night-close">Night · close</option><option value="day-tactical">Day · tactical field</option></select></p>'
for key in observations:
 body+=f'<section><h3>{html.escape(key.title())}</h3><div class="pair"><figure><img id="{key}-before" alt="{key} baseline"><figcaption>Before</figcaption></figure><figure><img id="{key}-candidate" alt="{key} candidate"><figcaption>Candidate</figcaption></figure></div><p class="verdict">{html.escape(observations[key])}</p></section>'
body+='<p><strong>Implemented source fix:</strong> character normal maps preserve unchanged source bytes; resized normals use PNG. Colour maps keep the existing JPEG policy. A lazy-image export failure discovered by the check was also fixed.</p><p><strong>Checks passed:</strong> source normal byte preservation, lossless resizing, linear normal import, unchanged colour compression, unchanged pilot source files and 18 matched renders. Candidate maps/materials regenerate deterministically.</p><p class="muted">Blender material previews, not Unity screenshots. The preview does not multiply AO into albedo to imitate Unity indirect occlusion. The glass, grade and lights are approximations. All five Unity material candidates remain isolated and uninstalled; no scene, model, collision or mission files changed. Additional candidate texture budget is approximately '+str(manifest['estimatedAddedBCMiBWithMips'])+' MiB with block compression/mips, before an actual Unity residency measurement. M4 Air frame times remain unmeasured.</p>'
script='<script>const images='+json.dumps(images)+';function draw(){const v=document.getElementById("view").value;for(const k of ["cafe","planter","scout"])for(const t of ["before","candidate"])document.getElementById(k+"-"+t).src=images[k+"-"+v+"-"+t]}document.getElementById("view").addEventListener("change",draw);draw();</script>'
page='<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Material-quality pilot</title><style>'+style+'</style>'+body+script+'</html>'
(OUT/'compare.html').write_text(page)
# Same HTML and behaviour, with local image references for efficient tool input.
for key,uri in images.items():page=page.replace(uri,str(OUT.resolve()/(key+'.png')))
(OUT/'preview.html').write_text(page)
print('Wrote compare.html and preview.html')
