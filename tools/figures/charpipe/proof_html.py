"""Brief 069 proof page: one self-contained proof.html (pictures embedded) from the scratch run.
  python3 -I tools/figures/charpipe/proof_html.py <scratch dir>     (reads final/*.png, numbers.json, determinism.json, CharPipe/manifest.json, research/*.md)"""
import base64, html, io, json, os, statistics as st, sys
from PIL import Image

S = sys.argv[1]
E = html.escape


def img(name, jpeg=False, width=None, alt=''):
    p = os.path.join(S, 'final', name); im = Image.open(p).convert('RGB'); b = io.BytesIO()
    if jpeg: im.save(b, 'JPEG', quality=86); mime = 'jpeg'
    else: im.save(b, 'PNG', optimize=True); mime = 'png'
    w = f' style="width:{width}"' if width else ''
    return f'<img alt="{E(alt or name)}" src="data:image/{mime};base64,{base64.b64encode(b.getvalue()).decode()}"{w}>'


num = json.load(open(os.path.join(S, 'numbers.json')))
det = json.load(open(os.path.join(S, 'determinism.json')))
man = json.load(open(os.path.join(S, 'CharPipe/manifest.json')))


def agg(kind, f):
    v = [f(x) for x in num if x['kind'] == kind]
    return f'{min(v):,} / {round(st.mean(v)):,} / {max(v):,}'


rows = [('figures measured', str(sum(1 for x in num if x['kind'] == 'today')), str(sum(1 for x in num if x['kind'] == 'new'))),
        ('bones per figure', agg('today', lambda x: x['bones']), agg('new', lambda x: x['bones'])),
        ('fingers', 'no (mitten hands)', 'yes: 3 joints x 5 per hand'),
        ('LOD0 triangles (min / mean / max)', agg('today', lambda x: x['lods'][0]['tris']), agg('new', lambda x: x['lods'][0]['tris'])),
        ('LOD0 GPU vertices (after UV and normal splits)', agg('today', lambda x: x['lods'][0]['gpuVerts']), agg('new', lambda x: x['lods'][0]['gpuVerts'])),
        ('play-zoom LOD triangles (the last LOD)', agg('today', lambda x: x['lods'][-1]['tris']), agg('new', lambda x: x['lods'][-1]['tris'])),
        ('play-zoom LOD GPU vertices (what is skinned)', agg('today', lambda x: x['lods'][-1]['gpuVerts']), agg('new', lambda x: x['lods'][-1]['gpuVerts'])),
        ('LODs per figure', agg('today', lambda x: len(x['lods'])), agg('new', lambda x: len(x['lods']))),
        ('materials per LOD (= draw calls per figure, + its shadow pass)', '1', '1'),
        ('skinned mesh renderers drawn per figure', '1 (the active LOD)', '1 (the active LOD)'),
        ('unique materials across the set', '39 (17 figures)', '1 (all 50, both LODs)'),
        ('unique textures across the set', '58, 88.3 Mpx (512-2048 albedo, normal, metal)', '2, 0.13 Mpx (256 px palette + metal)'),
        ('FBX size KB', agg('today', lambda x: x['bytes'] // 1024), agg('new', lambda x: x['bytes'] // 1024))]
table = ''.join(f'<tr><td>{E(a)}</td><td>{E(b)}</td><td>{E(c)}</td></tr>' for a, b, c in rows)
t0 = [f['lods'][0]['tris'] for f in man['figures']]
lic = open(os.path.join(S, 'research/licence-and-candidates.md')).read()


def md(t):
    """just enough markdown for the research note: headings, bullets, tables, paragraphs."""
    out, tbl = [], []
    def flush():
        if tbl:
            r = [l.strip('|').split('|') for l in tbl if not set(l.replace('|', '').strip()) <= set('-: ')]
            out.append('<table>' + ''.join('<tr>' + ''.join(f'<{"th" if i == 0 else "td"}>{E(c.strip())}</{"th" if i == 0 else "td"}>' for c in row) + '</tr>' for i, row in enumerate(r)) + '</table>'); tbl.clear()
    for l in t.splitlines():
        if l.startswith('|'): tbl.append(l); continue
        flush()
        if l.startswith('## '): out.append(f'<h3>{E(l[3:])}</h3>')
        elif l.startswith('# '): continue
        elif l.startswith('- '): out.append(f'<li>{E(l[2:])}</li>')
        elif l.strip(): out.append(f'<p>{E(l)}</p>')
    flush(); return '\n'.join(out).replace('**', '')


page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Brief 069 · character pipeline proof</title>
<style>
body{{font:15px/1.5 -apple-system,Helvetica,Arial,sans-serif;background:#f7f4ec;color:#17302f;margin:0;padding:24px 40px;max-width:1500px}}
h1{{font-size:26px;margin:0 0 4px}} h2{{font-size:20px;margin:34px 0 8px;border-bottom:2px solid #d2e6e2;padding-bottom:4px}} h3{{font-size:16px;margin:18px 0 6px}}
img{{max-width:100%;display:block;margin:8px 0;border:1px solid #d2e6e2;image-rendering:pixelated}} .smooth img{{image-rendering:auto}}
table{{border-collapse:collapse;margin:8px 0;font-size:14px}} td,th{{border:1px solid #cbc6c3;padding:4px 8px;vertical-align:top;text-align:left}} th{{background:#e9efe9}}
.note{{background:#fff;border-left:4px solid #60e0ea;padding:8px 14px;margin:10px 0}} .warn{{border-left-color:#f0a020}} code{{background:#eee8dc;padding:0 3px}}
.cap{{font-size:13px;color:#4a5f5d}}
</style></head><body>
<h1>Brief 069 · one body, one skeleton, a wardrobe, made by rule</h1>
<div class="cap">Proof, scratch only, 2026-10-26 · Forma (pipeline, figures, numbers); still to come: Latch (Unity frames, 4b), Redline (Studio frame cost, 5a), Glyph (the play-zoom read). Nothing is installed; the game is untouched.</div>

<div class="note"><b>In one line:</b> 50 people from four role rules, on one 52-bone Humanoid skeleton with fingers, one shared material, LOD'd FBX in the People manifest's shape, built in about 5 s, byte-identical run to run, with CC0 bodies and $0 spent. They read as job and colour at play zoom. Up close they're plainer than today's textured figures (flat colour, simple faces), and the hands at the play-zoom LOD are mittens. Still owed by others: the Unity frames and the Studio frame cost.</div>

<h2>1 · At play zoom, beside today's crowd</h2>
<p class="cap">True size (1 px = 1 px of a 1080p frame, zoom 14, 38°, yaw 30) in the game's sun, trilight and grade, through <code>tools/figures/proof_run.py</code> (via a copy of proof.py whose retarget keeps the torso upright for the new rig). New figures at LOD1, today's at their last LOD, all on the idle clip's frame 4. Blender's one-frame retarget only approximates Unity's Humanoid, so Latch's Unity frames are the truth.</p>
{img('crowd.png', alt='crowd 1:1')}
<p class="cap">The same at 3x (nearest neighbour):</p>
{img('crowd@3x.png', alt='crowd 3x')}

<h2>2 · One guard group, one rival group: uniform but individual</h2>
<p class="cap">Guards: one amber uniform (jacket, or the senior's car coat; 35% wear the over-tabard), ink or ivory below, chrome line; each a different body, sex, age, skin, hair, cap or none, gloves or none. Rivals (the Velvet Set): violet, each in their own violet and cut (jacket, tunic or coat; trousers or skirt), bare-headed but for one. Below them, today's two guard models and four rival colourways.</p>
{img('groups@3x.png', alt='groups 3x')}

<h2>3 · The 50 up close (LOD0, idle)</h2>
<div class="smooth">{img('close50.png', jpeg=True, alt='close 50')}</div>
<p class="cap">Rows: commuters 1-13, 14-26; guards 1-8 and rivals 1-5; rivals 6-8 and café staff 1-8. EEVEE, a warm sun on cream: the shapes and colours, not the game's light.</p>

<h2>4 · Moving: walk, run, aim_pistol on the clips as they are</h2>
<p class="cap">Skirts (commuter_02), coat (commuter_07), guard with the tabard (guard_02), rival in a coat with a cap (rival_03), café (cafe_01); three-quarter and side. The hem rings are weighted by Data Transfer from the body and blended toward hips and thighs. They follow the stride without tearing in these frames.</p>
<div class="smooth">{img('walk.png', jpeg=True)}{img('run.png', jpeg=True)}{img('aim_pistol.png', jpeg=True)}</div>
<h3>Hands</h3>
<div class="smooth">{img('hands.png', jpeg=True, width='520px')}</div>
<p class="cap">LOD0 (left): five separate fingers on the 52-bone hand, thin and simple. LOD1 (right, what play zoom draws, 3-4 px): a mitten with a thumb.</p>

<h2>5 · Two base bodies, their sliders, and a garment that follows them</h2>
<p class="cap">Rows: the man's base, an outfit on it (jacket, trousers, car coat, crop), the woman's base, an outfit on it (jacket, skirt under a car coat, bob). Columns: reference, height low, height high, weight low, weight high, muscle low, muscle high, old. Each is a shape key on one topology (<code>CharPipe/Master/*.blend</code>; the bases as FBX in <code>CharPipe/Base/</code>), so the garment follows every slider by construction. At build time each person's sliders are baked and MPFB refits the skeleton, so bone lengths follow the body.</p>
<div class="smooth">{img('sliders.png', jpeg=True)}</div>

<h2>6 · Numbers</h2>
<h3>Per figure, today's crowd (the 17 models HostViews draws for passers, office, café, guard, security, rivals) against the 50 new</h3>
<table><tr><th></th><th>today</th><th>new</th></tr>{table}</table>
<p class="cap">Measured from the FBXs by <code>tools/figures/charpipe/numbers.py</code> (<code>numbers.json</code>). GPU vertices = unique (position, normal, UV) corners, what Unity skins. New LOD0 tris: {min(t0):,}-{max(t0):,}.</p>
<div class="note warn"><b>Frame cost (5a) is Redline's, still to come:</b> a 60-person crowd, old against new, on the Studio. What to expect from the table: the same draw calls (one per figure plus its shadow, as today); one material and one shader variant for all, so the SRP Batcher never switches material; about half the vertices to skin at play zoom; textures down from 88 Mpx to 0.13 Mpx. Against that, 52 bones instead of 23, so more Animator and transform work per figure. If the bones cost, two ways out: <code>Optimize Game Objects</code> on import, or a crowd skeleton that drops finger joints 2-3 (32 bones) while the cast keeps all 52.</div>
<h3>Time</h3>
<table><tr><th>what</th><th>measured</th><th>notes</th></tr>
<tr><td>build all 50 (pick, bodies, wardrobe, LODs, FBX, metas, manifest)</td><td>5 s on 6 jobs; 27 s on 1</td><td>Studio, Blender 5.2 headless, MPFB 2.0.18; about 0.5 s a figure</td></tr>
<tr><td>add one new garment: the over-tabard (security concept)</td><td>56 s wall clock, two passes (20:11:34 to 20:12:30)</td><td>one JSON block in <code>wardrobe.json</code> and one line in the guard role. The first pass showed the angle clause cut the panels to a strip, so the generator gained a <code>maxX</code> panel clause (permanent, 6 lines). A paint garment is a rule edit. A new geometry kind (a cape, a backpack) is code: hours, not minutes. A person doing it by hand would also need the look check: say 30-60 min for a paint or tube garment.</td></tr>
<tr><td>add one new role: Records Office clerk</td><td>about 30 s wall clock to six built and rendered (20:12:30 to 20:13:00), one pass</td><td>one role block in <code>roles.json</code> and three palette sets (civic seafoam and deep teal). No code. By hand: about 15 min with the look check.</td></tr>
</table>
<div class="smooth">{img('tabard.png', jpeg=True)}{img('clerks.png', jpeg=True)}</div>
<h3>Determinism</h3>
<p><b>{E('IDENTICAL' if not det['differing'] else 'DIFFER')}</b>: two full builds, every one of {det['files']} files (FBX, metas, palettes, material, rows, specs) compared by sha256, {len(det['differing'])} differ. A third build on 1 job (a different split of people over processes) matched the 6-job build file for file. The FBX header time and object ids are pinned (as the DecoKit export does); every pick is sha256(person id / key), with no random module and no dict or set order. Record: <code>determinism.json</code>.</p>

<h2>7 · Licence and the candidates</h2>
{md(lic)}

<h2>8 · How it works (files)</h2>
<ul>
<li><code>tools/figures/charpipe/roles.json</code>: role rules (extends <code>tools/figures/roles.json</code> by naming the keys each role would serve; that file is untouched): body ranges, slots with weights (split by sex where it matters), colour sources.</li>
<li><code>wardrobe.json</code>: 9 parts in slots (2 tops, 2 legs, 1 coat, 1 over-tabard, 2 hair, 1 hat), plus base layers (body, boots, gloves, eyes). Kinds: paint (body faces pushed out, so nothing pokes through and no triangles are added), tube (hem rings), collar, curtain (hair), dome (cap).</li>
<li><code>palettes.json</code>: every colour with its source (style contract, GDD §15, harbour.json); amber and violet as cloth; brass only as a thin hem line (the picker never puts it on painted trim).</li>
<li><code>pick.py</code> (rules to person specs, pure Python), <code>build.py</code> (Blender: MPFB body, rig cut to 52, two proxies, wardrobe, UV palette, FBX, metas, rows), <code>run.ts</code> (parallel build, manifest, <code>--twice</code>, <code>--master</code>), <code>sheet.py</code> and <code>proof.py</code> (play-zoom sheets), <code>preview.py</code>, <code>slider.py</code>, <code>numbers.py</code>, <code>stitch.py</code>, this page's <code>proof_html.py</code>.</li>
<li>Output: <code>out/scratch/forma/char-pipeline/CharPipe/</code> (Figures, Shared, manifest.json; Base, Master). Latch's notes: <code>handoff-latch.md</code>.</li>
</ul>

<h2>9 · The read</h2>
<h3>What the full rebuild would take</h3>
<ul>
<li><b>Roles:</b> the play scene's keys come to about 9 roles: commuter/passer, office or clerk, café (server, cook, waitress), guard/security (posted and officers), rivals, driver family, market and trade (workers), nightlife, resident. Four exist here and clerk was the timed fifth; each further role is a rule block (minutes) plus a look check with Glyph.</li>
<li><b>Parts:</b> about 25-30 for a city that doesn't repeat: tops 6 (tunic, jacket, zip bomber, sleeveless shift top, knit, work smock), legs 4 (trousers, skirt, shorts, coverall legs), coats 2 (car coat, short cape-collar jacket within the no-long-coats rule), over 3 (tabard, apron, harness), hair 6 (crop, bob, afro, bun, ponytail as a curtain, buzz), hats 4 (cap, visor helmet, pillbox, headband), plus the held props as today. A dress is rules (top and skirt in one colour). Paint and tube garments take minutes each. New geometry kinds (bags, packs, a structured collar, a ponytail) are code: about half a day each.</li>
<li><b>Heroes</b> (the cell, Vale, Inez, the driver; out of this proof): the same base, sliders and skeleton, so every clip, prop attach and fix applies. Each would get its own LOD0 garment meshes modelled from its concept (hand-made, weighted by the same Data Transfer), a face from MPFB's targets, and a real texture set with the cloth/trim/skin masks brief 068 asks for (the cell is 40-60 px at play zoom, but portraits, third person and conversations are close). Estimate: 1-2 days each with the concept to hand, about 7-10 days for all seven. Vale's bib and chain and the Enforcer's coat tails are the hard ones.</li>
<li><b>Swap-over in Latch's figure code:</b> the rows already have People/manifest.json's shape, so FigureBuild takes them as they are. RoleFigure's pools (<code>playRoles</code>, <code>families</code>, <code>jobBlocks</code>) point at generated ids, picked by person id as today. The hue-window recolours (<code>tintedVariants</code>, <code>tools/figures/masks</code>) and the HostViews family tints go away. Per-figure offsets (tray attach in <code>manifest.props[0].attach.perFigure</code>, sit, carry seat) become one value per skeleton, measured once, because every figure shares the bone layout (bone lengths vary with height, so measure against the bones, as the carry seat already does). The LODGroup from names and one AnimatorController stay. The idle-slide fix (0.2.123) is in the clip metas and applies unchanged.</li>
</ul>
<h3>Open risks and what fell short (honest)</h3>
<ul>
<li><b>The look up close is plainer:</b> flat colour per face, no fabric texture or folds, simple faces with dark eye cells. At play zoom that reads cleaner than today's noisy baked textures; in the follow view and close-ups it's a step down. The fix is a mask shader (UV2 already carries the channel) with shared fabric detail maps; it's untested.</li>
<li><b>52 bones against 23:</b> fingers cost bones. Redline's 5a decides; the fallbacks are above.</li>
<li><b>Hands:</b> LOD0 fingers are real but thin and low-poly; LOD1 (play zoom) is a mitten with a thumb.</li>
<li><b>Proportions:</b> MPFB bodies are realistic, so they look taller-legged and wider-hipped than today's stylised figures; at idle the stance is wider. The height ranges were tuned once (men about 1.64-1.96 m including caps, women 1.52-1.68 m; the women's range wants a touch more).</li>
<li><b>Small artefacts:</b> with a car coat over trousers, two 5 mm slivers of thigh show at the coat's front hem in the A-pose close-up (invisible at play zoom). Painted strips on the low proxy are blocky (a belt can break at the sides; the trouser side line is uneven). The tunic's flared hem was dropped because it tore at idle. A coat over a skirt drops the skirt's ring, since two hung rings interpenetrate.</li>
<li><b>Retarget in the pictures:</b> Blender's one-frame retarget here is not Unity's Humanoid. The stoop in the first sheet came from it (Spine2 and Neck swung 16°); the copy keeps the torso unswung. Unity's frames from Latch are what to judge motion on.</li>
<li><b>Unity A-pose avatar:</b> today's A-pose figures map unaided; if the new ones warn, Enforce T-Pose once and the generator writes it into the metas.</li>
<li><b>Dependencies:</b> MPFB 2.0.18 and the 2024 system asset pack in a scratch Blender user dir; pin both (hashes in the research note) and keep them out of Sky's Blender.</li>
<li><b>No Codex art used:</b> garments came from the board's concept sheets by eye (commuters__spaceage_bold, security__spaceage_duty_2). A concept round for the new parts (people prompts without a date) would sharpen the couture before a full rebuild.</li>
</ul>

<h2>For Sky</h2>
<ol><li>Does the new crowd beat today's at play zoom, measured against the concept art? (sections 1-3)</li>
<li>Rebuild the city's people on it? If yes, a GDD version (§14 Forma's delivers, §06/§15 stand-ins).</li>
<li>The cell and Vale: rebuild on the base, or keep their look and only re-rig?</li></ol>
</body></html>"""
open(os.path.join(S, 'proof.html'), 'w').write(page)
print('wrote', os.path.join(S, 'proof.html'), round(len(page) / 1e6, 2), 'MB')
