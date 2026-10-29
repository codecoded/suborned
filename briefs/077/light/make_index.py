# Glint, brief 077: briefs/077/light/index.html from the Unity captures in out/scratch/glint/077 (Glint077.Run sweep + final). Self-contained.
import base64, io, os, sys
from PIL import Image
R = '/Users/sky/Projects/suborned/out/scratch/glint/077'
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'index.html')
def img(path, box=None, w=1440, nearest=False):
    im = Image.open(os.path.join(R, path)).convert('RGB')
    if box: im = im.crop(box)
    if im.width != w: im = im.resize((w, round(im.height * w / im.width)), Image.NEAREST if nearest else Image.LANCZOS)
    b = io.BytesIO(); im.save(b, 'JPEG', quality=86)
    return 'data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode()
def fig(path, cap, box=None, w=1440, nearest=False):
    return f"<figure><img src='{img(path, box, w, nearest)}' width='{min(w,1180)}'><figcaption>{cap}<br><code>{path}</code></figcaption></figure>"
def row(*figs): return "<div class='row'>" + ''.join(figs) + "</div>"
F = (420, 180, 1500, 800); BED = (820, 300, 1120, 620); TR = (600, 100, 1300, 560)
h = []
h.append("""<!doctype html><html><head><meta charset='utf-8'><title>077 · globe lamps and leaf gloss (Glint)</title>
<style>body{font:14px/1.45 -apple-system,Helvetica,sans-serif;max-width:1220px;margin:24px auto;padding:0 16px;color:#17302f}h2{margin-top:32px;border-bottom:1px solid #d2e6e2}.box{background:#f7f4ec;border:1px solid #d2e6e2;padding:10px 14px;margin:12px 0}
table{border-collapse:collapse;font-size:12.5px;margin:8px 0}td,th{border:1px solid #d2e6e2;padding:3px 7px;text-align:left;vertical-align:top}.row{display:flex;gap:8px;flex-wrap:wrap}figure{margin:4px 0}figcaption{font-size:12px;color:#456}img{display:block;height:auto}code{background:#eef5f3;padding:0 3px}</style></head><body>
<h1>077 · the globe lamps' night light and the leaf gloss</h1>
<p>Glint, 2026-10-30, Unity 6000.6.2f1. Latch's probe <code>Assets/Scenes/probe-planting077.unity</code> (git-ignored), opened in batch and never saved; every light, material copy and mask texture is in memory (<code>unity/Assets/Suborned/Editor/OldTown/Swap/Glint077.cs</code>). The swap's play camera with our play look (OldTownPlayLook), zoom 14 at 38°. Night is our night: light.json <code>night</code> as HarbourDistrict's NightPhase built it (Latch's read-only copy, probe-leafshader077/night.json: moon, trilight, night sky, night reflection cube, the night grade) plus lens.json's night floor (ambient and moon 0.42, shadow 0.6). The lens lift (0.002 linear offset) is left out. The swap has no night lamps of its own yet, so the four globe pools are the only pools on the square, apart from one street-kind stand-in at the old DK_PlazaSphereLamp.</p>
<div class='box'><b>Lamp values</b> (light.json <code>night.lamps.kinds</code> "globe", <code>night.emissive</code> globeOpal, palette <code>night.lampGlobe</code>): a point light, #fff1d8, <b>intensity 7</b>, <b>range 5.8 m</b>, 2.31 m above each GlobeLamp_TB base, <b>no shadows</b>, no cookie, night only (off by day and at evening, like the street kind). Opal emission = linear(#fff1d2) × level: <b>day 0.06</b>, evening 0.06, <b>night 2.5</b>. The testbed's 28 cd works out to about 9 in URP, because three.js divides diffuse by π and URP doesn't. At 9 the paving between the islands goes near white where the pools overlap, so I used 7. The testbed's opal is 3.5; at that level the bloom spreads wider than the pool, so I used 2.5.<br>
<b>Gloss call:</b> tame it. Leaf mask scale k 0.49 → <b>0.80</b>, petal k 0.64 → <b>0.80</b> (smoothness = 1 − k × ORM.g: leaves about 0.78 → 0.64, petals 0.64 → 0.55). This keeps the testbed's dark glossy leaf. The sharp white sun glints become a soft sheen, by day in both looks, and the wet-white point-light glints at night are gone. k 1.0 (C) goes matte and loses the gloss.<br>
<b>Cost:</b> four unshadowed point lights at night only. PC_Renderer is Forward+, so they add no SetPass, no draws, no shadow maps and no variant (additional lights are already on). They reach 51, 66, 56 and 50 renderers within 5.8 m. The gloss change costs nothing: the same mask, size and format, only different bytes. The opal is one material property.</div>
<h2>Night, the square: before (gloss A, k 0.49) | after (gloss B, k 0.80), lamps on</h2>""")
for y in ('030', '210'):
    h.append(row(fig(f'final/night-A-cafe-square-y{y}.png', f'night, yaw {y[1:] if y=="030" else y}, gloss A (today)', w=1180), fig(f'final/night-B-cafe-square-y{y}.png', f'night, yaw {int(y)}, gloss B (proposed)', w=1180)))
h.append("<h2>Night, the fountain islands at 2x crop: A | B</h2>")
for y in ('030', '210'):
    h.append(row(fig(f'final/night-A-fountain-y{y}.png', f'A, yaw {int(y)}', F, 580), fig(f'final/night-B-fountain-y{y}.png', f'B, yaw {int(y)}', F, 580)))
h.append("<h2>Lamps off (same night, gloss B), for reference</h2>")
h.append(row(fig('final/night-lampsoff-B-cafe-square-y030.png', 'lamps off, yaw 30', w=580), fig('final/night-lampsoff-B-cafe-square-y210.png', 'lamps off, yaw 210', w=580)))
h.append("<h2>Intensity sweep (gloss A, the probe's opal without its _EMISSION keyword in this run, so the globes don't glow)</h2>")
h.append(row(*[fig(f'sweep/{n}.png', c, F, 580) for n, c in [('night-off-fountain-y030', 'off'), ('night-i6-o2.2-fountain-y030', 'intensity 6'), ('night-i9-o3.5-fountain-y030', 'intensity 9 (testbed equivalent)'), ('night-i16-o3.5-fountain-y030', 'intensity 16 (blown)')]]))
h.append(row(*[fig(f'final/{n}.png', c, F, 580) for n, c in [('night-i5-B-fountain-y030', 'intensity 5, gloss B'), ('night-i7-B-fountain-y030', 'intensity 7, gloss B (chosen)')]]))
h.append("<h2>Opal at night (intensity 7, gloss B): 0.06 | 1.5 | 2.5 (chosen) | 3.5 (testbed)</h2>")
h.append(row(*[fig(f'final/night-opal{o}-B-fountain-y030.png', f'opal {o}', F, 580) for o in ('0.06', '1.5', '2.5', '3.5')]))
h.append("<h2>Day gloss, play look: A (k 0.49 / 0.64, today) | B (0.80 / 0.80, proposed) | C (1.0 / 0.90)</h2>")
h.append(row(*[fig(f'sweep/day-play-{k}-close-island-y030.png', f'{k}: island bed, 2x', BED, 380, True) for k in 'ABC']))
h.append(row(*[fig(f'sweep/day-play-{k}-close-troughs-y030.png', f'{k}: café kerb trough', TR, 380) for k in 'ABC']))
h.append(row(*[fig(f'sweep/day-play-{k}-cafe-square-y030.png', f'{k}: play zoom, troughs and islands', (420, 330, 1780, 760), 380) for k in 'ABC']))
h.append(row(fig('final/day-play-A-cafe-square-y210.png', 'A, yaw 210', w=580), fig('final/day-play-B-cafe-square-y210.png', 'B, yaw 210', w=580)))
h.append("<h2>Day gloss, presentation look (the study camera, the old grade): A | B | C</h2>")
h.append(row(*[fig(f'sweep/day-presentation-{k}-close-troughs-y030.png', f'{k}: café kerb trough', TR, 380) for k in 'ABC']))
h.append(row(*[fig(f'sweep/day-presentation-{k}-close-island-y030.png', f'{k}: island bed, 2x', BED, 380, True) for k in 'ABC']))
h.append(row(fig('final/day-presentation-A-cafe-square-y210.png', 'A, yaw 210', w=580), fig('final/day-presentation-B-cafe-square-y210.png', 'B, yaw 210', w=580)))
h.append("""<h2>For Latch</h2><table>
<tr><th>what</th><th>where</th><th>change</th></tr>
<tr><td>globe light</td><td>unity/Assets/Suborned/Editor/World/light.json night.lamps.kinds "globe"</td><td>NightPhase doesn't know "globe" yet, and it ignores unknown kinds, so today's builds are unchanged. Spawn one point light per world.Civic mass with Kit.Id GlobeLamp_TB at (Kit.At.x, Base + 2.31, Kit.At.y), named <code>globe/&lt;id&gt;</code>, through Make() (off by day, LightPhase caps it).</td></tr>
<tr><td>opal emission</td><td>light.json night.emissive.night.globeOpal 2.5, dayResidual.globeOpal 0.06, pieces GlobeLamp_TB</td><td>A runtime copy of DK_Opal_TB at night, _EmissionColor = linear(#fff1d2) × 2.5 (alpha 2.5); day and evening keep the .mat's 0.06. That's an absolute level: ×41.7 is past roadlight.json's emission cap (0..10, which would reject the file), so it needs a new mode or a raised cap, not a glow row as it stands.</td></tr>
<tr><td>gloss</td><td>PlantingSwap077.Mats, the botanical mask lambda <code>v &gt;= .5 ? .49f : .64f</code></td><td>→ <code>.80f : .80f</code>; rerun Mats (out/decokit-077 Textures/botanical-mask-v2.png and its sha), then Splice's catalogue.</td></tr>
<tr><td>bug</td><td>Assets/Scenes/probe-planting077/Kit/Materials/DK_Opal_TB.mat</td><td>The probe's copy has m_ValidKeywords [] (no _EMISSION), so the globes in the 077 day capture had no emission. The kit copy in out/decokit-077/Materials has _EMISSION.</td></tr>
<tr><td>the swap's night</td><td>OldTown.unity has no LightPhase</td><td>The swap has no night of its own. These values land wherever NightPhase runs, and the swap needs its night (and street lamps) before Sky can see it in game.</td></tr></table>
<p>Commands (exit 0): <code>bun briefs/075/oldbuild/swap/tools/unityrun.ts out/scratch/glint/077/sweep.log -- -executeMethod Suborned.OldTown.Editor.Swap.Glint077.Run -glintStage sweep -glintOut out/scratch/glint/077/sweep</code> · <code>… final.log -- … -glintStage final -glintOut out/scratch/glint/077/final -globeI 7 -opalNight 2.5</code> · <code>python3 -I briefs/077/light/make_index.py</code></p></body></html>""")
open(OUT, 'w').write('\n'.join(h)); print(OUT, os.path.getsize(OUT))
