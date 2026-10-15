"""Builds index.html (self-contained: images embedded as downsized JPEG, each linked to its full file). python3 -I build_index.py"""
import base64, io, json, html
from PIL import Image
D = "/Users/sky/Projects/suborned/briefs/012/cell/tech-final/"
M = json.load(open(D + "proxy/metrics.json"))["results"]
PL = json.load(open(D + "proxy/plan-sections.json"))
cache = {}

def uri(name, w=1300, q=82):
    k = (name, w)
    if k in cache: return cache[k]
    im = Image.open(D + name).convert("RGB")
    if im.width > w: im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
    b = io.BytesIO(); im.save(b, "JPEG", quality=q); cache[k] = "data:image/jpeg;base64," + base64.b64encode(b.getvalue()).decode(); return cache[k]

def uri_png(name):
    return "data:image/png;base64," + base64.b64encode(open(D + name, "rb").read()).decode()

def img(name, w=1300, cap="", cls=""):
    return f'<figure class="{cls}"><a href="{name}"><img src="{uri(name, w)}" alt="{html.escape(name)}"></a>' + (f"<figcaption>{cap}</figcaption>" if cap else "") + "</figure>"

def pose(name, notes, cap):
    dots = "".join(f'<span class="dot" style="left:{x/15.36:.2f}%;top:{y/10.24:.2f}%">{i}</span>' for i, (x, y, t) in enumerate(notes, 1))
    li = "".join(f"<li><b>{i}</b> {t}</li>" for i, (x, y, t) in enumerate(notes, 1))
    return f'<div class="pose"><div class="pimg"><a href="{name}"><img src="{uri(name, 1200)}"></a>{dots}</div><div class="ptxt"><h3>{cap[0]}</h3><p class="verdict">{cap[1]}</p><ol>{li}</ol></div></div>'

def cm(v): return "none" if v < 0.0005 else f"{v*100:.1f} cm"

def mtab():
    poses = [("stand", "standing, idle"), ("walk_contact", "walk, mid-stride"), ("walk_pass", "walk, legs passing"), ("run_flight", "run, mid-stride"), ("run_pass", "run, legs passing"),
             ("crouch_low", "crouch, low"), ("crouch_deep", "crouch, deep"), ("fight", "fight stance"), ("kerb", "kerb step-up")]
    cols = [("AD", "as drawn, thigh + shin only"), ("FXn", "inner side narrowed, thigh + shin only"), ("FX10", "inner side narrowed + 10 secondary bones"), ("BELL", "round 4 bell shorts (rigid)")]
    h = "<table class='m'><tr><th rowspan=2>pose</th>" + "".join(f"<th colspan=2>{t}</th>" for _, t in cols) + "<th colspan=2>slim trousers (control)</th></tr><tr>" + "<th>leg vs leg</th><th>into thigh</th>" * 5 + "</tr>"
    slim = {"stand": (0, 0), "walk_contact": (0, 0), "walk_pass": (0, 0), "run_flight": (0, 1.3), "run_pass": (0, 2.3), "crouch_low": (0, 2.3), "crouch_deep": (0, 5.8), "fight": (0, 0), "kerb": (0, 0)}
    for p, lab in poses:
        h += f"<tr><td>{lab}</td>"
        for v, _ in cols:
            m = M[v][p]["metrics"]
            for k in ("leg_leg", "through_leg"):
                x = m[k][0]; h += f"<td class={'bad' if x > 0.02 else 'ok'}>{cm(x)}</td>"
        a, b = slim[p]
        h += f"<td class=ok>none</td><td class={'bad' if b > 2 else 'ok'}>{'none' if b == 0 else f'{b:.1f} cm'}</td></tr>"
    return h + "</table>"

POSE_NOTES = {
 "TF-pose-walk.jpg": ([(330, 690, "The bone panel on the front leg swings forward with the thigh and its hem opens; the hem stays at mid-shin."), (205, 735, "Rear leg: the panel trails behind the calf and its hem folds against the shin, clear of the front leg."),
                       (1100, 800, "Three-quarter view: the two legs overlap in silhouette only; the front-leg hem lies in front of the rear-leg hem, nothing passes through.")],
                      ("Walk, mid-stride", "Reads cleanly. The flare opens on the outside of each leg; the gap between the legs is kept by the stride.")),
 "TF-pose-run.jpg": ([(130, 510, "Front knee driven up: the thigh panel bunches over the knee and the hem is pulled back along the shin."), (645, 520, "Rear leg: outer bone panel and hem stream out behind, flat and clear of the other leg."),
                      (470, 700, "In profile the near leg's cloth covers the far leg; the far leg passes behind it. No fold crosses through the other leg.")],
                     ("Run, mid-stride", "The most cloth in motion in the set, and it still reads as a runner. Needs the flap baked into the clip: nothing here is a collision problem.")),
 "TF-pose-crouch.jpg": ([(300, 470, "Front panels and pleats bunch across the thighs; the pleats fold at the hip crease."), (330, 700, "Back of the knee: the lower-leg cloth folds between calf and thigh. The one place where cloth layers meet (see the measured test: 4 to 6 cm)."),
                         (220, 735, "The hem sits on the shin, a hand above the pavement."), (1010, 730, "Front view: knees apart, the inner hems stay clear of each other.")],
                        ("Crouched at a junction box", "Works. Heaviest bunching of any pose, but it stays on the thigh and nothing clips through the legs.")),
 "TF-pose-fight.jpg": ([(235, 720, "Rear leg: the outer bone panel is pulled taut along the leg; the stance opens the split without showing the thigh."), (545, 700, "Front knee bent: the thigh cloth lifts into folds and the hem hangs close to vertical."),
                        (1100, 790, "Front view: wide stance, the inner hems are far apart. The wide cut helps here.")],
                       ("Fight stance (braced pistol)", "The pose where the cut is strongest: the legs are apart, so the inner hems never meet.")),
 "TF-pose-kerb.jpg": ([(480, 430, "Lead thigh raised: the front panel folds at the hip crease."), (260, 790, "Trailing leg: the hem comes level with the kerb top in this drawing. In the rig the hem is 30 cm above the road and the kerb is 18 cm, so it clears by 12 cm: do not lengthen the hem."),
                       (1250, 700, "Lead foot on the pavement: the hem hangs clear above the shoe.")],
                      ("Step up onto a kerb", "Fine. The only pose where hem length matters: mid-shin is as long as it can go.")),
}

CSS = """body{font:16px/1.5 -apple-system,Helvetica,Arial,sans-serif;max-width:1240px;margin:0 auto;padding:24px;color:#1d1f22;background:#faf8f3}
h1{margin:0 0 4px}h2{margin:44px 0 8px;border-top:2px solid #1d1f22;padding-top:14px}h3{margin:0 0 4px}
.sub{color:#666;margin:0 0 18px}.box{background:#fff;border:2px solid #1d1f22;padding:14px 18px;margin:14px 0}.box.yes{border-color:#1a6b45;background:#eef8f2}
figure{margin:12px 0}figure img{max-width:100%;display:block;border:1px solid #ccc}figcaption{font-size:14px;color:#555;margin-top:4px}
table{border-collapse:collapse;font-size:14px}td,th{border:1px solid #ccc;padding:4px 8px;text-align:left}th{background:#eee}
table.m td,table.m th{text-align:center}table.m td:first-child{text-align:left}td.bad{background:#fbe3f6;color:#8a0070;font-weight:600}td.ok{color:#1a6b45}
.pose{display:grid;grid-template-columns:1.7fr 1fr;gap:18px;margin:22px 0;align-items:start}.pimg{position:relative}.pimg img{width:100%;display:block;border:1px solid #ccc}
.dot{position:absolute;transform:translate(-50%,-50%);background:#fff;border:2px solid #1d1f22;border-radius:50%;width:26px;height:26px;text-align:center;font:700 14px/22px sans-serif}
.ptxt ol{padding-left:20px;font-size:14px}.ptxt li{margin:6px 0;list-style:none;margin-left:-20px}.verdict{font-weight:600;margin:0 0 8px}
.row{display:flex;gap:14px;flex-wrap:wrap;align-items:flex-start}.row>*{flex:1 1 300px}code,pre{background:#eee;padding:1px 4px;font-size:13px}pre{padding:10px;overflow:auto}
.small{font-size:13px;color:#555}.plan li{margin:5px 0}.alt img{image-rendering:pixelated}"""

def strip_pair(kind, view, cap):
    n = f"sil50-{kind}-{view}"
    return f'<figure><div class="alt"><img src="{uri_png(n + "-colour-x5.png")}"><br><img src="{uri_png(n + "-black-x5.png")}"></div><figcaption>{cap} Shown 5x; the real size is {Image.open(D + n + "-colour.png").width} x {Image.open(D + n + "-colour.png").height} px, 50 px per figure.</figcaption></figure>'

H = []
H.append(f"<!doctype html><meta charset=utf-8><title>The Tech, final: trousers motion test, rig plan, portrait</title><style>{CSS}</style>")
H.append("<h1>The Tech, final</h1><p class=sub>Brief 012 · Forma · 2026-10-16 · for Aster and Sky · the Tech is the round 3 cosplay sheet (<code>round3/cos-tech.jpg</code>) inside the A white team. Drawings by the Codex image tool (free); the numbers by a skinned proxy in Blender. Nothing installed in the HUD, nothing committed, GDD and style contract untouched.</p>")

H.append("""<div class="box yes"><h2 style="border:0;margin:0 0 6px;padding:0">Verdict: yes, with one change</h2>
<p><b>His trousers will not be a problem to animate, with one change: hang the inner side of each leg straight and about 3.5 cm nearer the leg.</b> The flare, the bone panels, the piping and the mid-shin hem all stay. Drawing in section 5.</p>
<ul><li><b>As drawn,</b> the two legs' inner hems sit about 15 cm from each leg's axis. Standing or walking past each other they overlap by 4 to 5 cm. That is about 1.5 px at play size (1 px is 3.3 cm), so you would see it as a shimmer on the inner hem, not as a hole. It is the only real fault in the cut.</li>
<li><b>With the change</b> (inner side 15 to 11.5 cm from the axis) the legs never touch in any of nine poses, with plain thigh-and-shin skinning and no secondary bones at all. The secondary bones are there for how it moves, not to stop clipping.</li>
<li><b>One thing is not the trousers' fault:</b> in a deep crouch the back of the knee folds 4 to 6 cm into the thigh. A slim trouser does the same (5.8 cm), so it is the standard knee corrective, not the cut.</li>
<li><b>Against the round 4 bell shorts:</b> the same test gives the bell 3 to 7 cm leg to leg in idle, walk and run, so round 4's warning was right and the bell is no easier than these trousers as drawn (4 to 5 cm). Both clear with the same kind of change; the bell needs the bigger one (inner side 15 to 10.5 cm at the thigh).</li></ul></div>""")

H.append("<h2>1. Motion test: the outfit in the poses the game needs</h2><p class=small>Drawn by the Codex image tool from the cosplay sheet (side view left, three-quarter front view right; prompt asks for heavy fabric under gravity and no tidying). These are drawings of plausible cloth, not a simulation: they show where the panels go. Whether anything clips is measured in section 2, because a drawing can hide a collision. Full-size files are linked.</p>")
for n, (notes, cap) in POSE_NOTES.items():
    H.append(pose(n, notes, cap))

H.append("<h2>2. Measured: a skinned proxy of the trousers</h2>")
H.append("""<p>The drawings cannot show a clip, so the same poses were run through a proxy: a 1.85 m skeleton (pelvis, thigh, shin) in the sheet's A-pose, two trouser legs built from the sheet's dimensions (hem at mid-shin, 15 cm to the inner hem and 20 cm to the outer flare from each leg's axis), four panels per leg, linear blend skinning (the same maths as Unity's skinned mesh), nine poses. No cloth physics, so every number is exactly repeatable. Script: <code>scripts/tf_proxy.py</code>, <code>tf_run.py</code> (Blender 5.2).</p>
<p class=small>Assumptions that could move the numbers: leg lengths (thigh 45, shin 43 cm), the sheet's hem width read from a 2x crop of the front view (about 33 cm per leg), a 4 cm gap between the idle feet (3 degrees of abduction), cloth taken as a rigid tube that cannot squash (real cloth compresses, so these are upper bounds). Leg vs leg = how far one leg's cloth sits inside the other's. Into thigh = cloth inside the body capsule. Under 2 cm is invisible at play size. Pink = more than 2 cm.</p>""")
H.append(mtab())
H.append(f"<p class=small>Hem to ground: at least 10 cm in every pose (the lowest is the run). Kerb step-up (kerb 18 cm, the world's value): the trailing hem clears the kerb by 27 cm. Belly test (front panel into the torso) reads none everywhere but is not a discriminating test in this proxy: the crease at the groin is handled by the waist weights. Skin weights: at most 4 influences per vertex (Unity's default limit).</p>")
H.append("<h3>Why the inner side: the legs seen from above</h3><p class=small>A horizontal slice through both legs at the height where they overlap most, seen from above (forward is up). Magenta = cloth inside the other leg's cloth. The long thin strips are a horizontal shin or thigh cut lengthwise.</p>")
H.append(img("plan-sections.png", 1300, f"Overlap in cm2. Standing: as drawn {PL['stand']['AD']:.0f}, proposed {PL['stand']['FX10']:.0f}, bell {PL['stand']['BELL']:.0f}. Walk passing: {PL['walk_pass']['AD']:.0f} / {PL['walk_pass']['FX10']:.0f} / {PL['walk_pass']['BELL']:.0f}. Run passing: {PL['run_pass']['AD']:.0f} / {PL['run_pass']['FX10']:.0f} / {PL['run_pass']['BELL']:.0f}."))
H.append("<h3>The sweep that picked the change</h3><p>Over 48 combinations of inner hem width (15, 13, 11.5, 10 cm), hem height (mid-shin, 5 cm higher), back depth and drivers on or off: <b>only the inner width matters.</b> At 15 cm the legs overlap 5.5 cm worst case, at 13 cm 2.7, at 11.5 cm none, at 10 cm none. Hem height moved it by about 1 cm. Inner splay and back-of-knee bulge drivers were tried and dropped: no gain once the inner side is narrowed. So the fix is not a shorter hem or a deeper split, and the hem should not get shorter than it needs: mid-shin is the longest that clears an 18 cm kerb by a hand.</p>")
H.append("<h3>Pose by pose, as drawn beside proposed</h3><p class=small>Mannequin proxy (grey body, charcoal and bone cloth, pink where the cloth is more than 1 cm inside a leg). The last column is the game camera (yaw 30, pitch 38).</p>")
H.append(img("proxy-sheet-1.png", 1490)); H.append(img("proxy-sheet-2.png", 1490))
H.append("<h3>The round 4 bell shorts, same proxy</h3>"); H.append(img("proxy-sheet-bell.png", 1230, "Bell shorts rebuilt from the round 4 sheet: rigid, every point on the thigh bone, hem 30 cm from the hip, about 32 cm per leg across. Same poses, same tests."))

H.append("<h2>3. Play-size silhouette strips, 50 px</h2><p class=small>Figures cut from the Codex strips, each scaled to 50 px tall (the tallest frame) and squashed by cos 38 for the 38 degree camera, the method of rounds 3 and 4. Shown at 5x with nearest-neighbour; colour above, black fill below.</p>")
H.append(strip_pair("walk", "side", "Walk, side view: contact, passing, contact, passing."))
H.append(strip_pair("walk", "front", "Walk, three-quarter front."))
H.append(strip_pair("run", "side", "Run, side view."))
H.append(strip_pair("run", "front", "Run, three-quarter front."))
H.append("""<ul><li><b>Run reads at once:</b> the lean, the arms and the streaming panels give it away from the black fill alone.</li><li><b>Walk reads as a walker, but through the feet:</b> in the two passing frames the trousers close into one column, so the stride lives in the shoes and the bone flare. At play zoom an idle Tech and a walking Tech differ by a few pixels, which is the same for any long wide garment. The A-shaped bell over two feet is the cell's most nameable silhouette (below).</li></ul>""")
H.append("<h3>The team at play scale (tallest 56 px, as in round 3)</h3><div class='row alt'>" + "".join(f'<figure><img src="{uri_png(n)}"><figcaption>{c}</figcaption></figure>' for n, c in (("sil56-lineup-old-colour-x5.png", "A white before (cargo shorts)"), ("sil56-lineup-new-colour-x5.png", "A white with the new Tech"), ("sil56-lineup-old-black-x5.png", "before, black fill"), ("sil56-lineup-new-black-x5.png", "after, black fill"))) + "</div>")
H.append("<p>The new Tech is the most distinct shape of the four: the wide A-flare under a narrow torso. Silhouette overlap with the Enforcer is 0.66 (was 0.63), with the Fixer 0.45 (0.48), with the Scout 0.55 (0.54): the pairs are as separable as before.</p>")

H.append("<h2>4. Rig plan</h2><div class='box plan'><ol>")
for t in [
 "<b>Build:</b> one trouser mesh, two legs by four panels (outer bone diagonal, front, back, inner), skinned to pelvis, thigh and shin with at most 4 influences per vertex. Proxy size: 2,464 tris; target 3,000 at close LOD and about 800 at play zoom.",
 "<b>Secondary bones:</b> 5 per leg, 10 in all: the outer panel gets an upper and a hem bone, front, back and inner get a hem bone each. At play-zoom LOD 4 (the inner and outer hem of each leg). Hand-weighted; the proxy skinning is the starting weight map.",
 "<b>Motion:</b> the hem swing and outer-panel flap are baked into the clips in Blender (keyed or an offline cloth bake), so they cost nothing at run time. One stateless driver on top: each hem bone leans back toward the vertical by 0.4 of the shin's world lean, read off the posed shin. It has no state, no time step and no random, so the same pose gives the same pixels on every replay.",
 "<b>Not allowed:</b> runtime cloth, spring bones, colliders or hit tests on panels. Animation is presentation only; the sim, the 10 Hz tick and replay never read a trouser bone.",
 "<b>Cost (an estimate, unmeasured):</b> about 2.5k extra skinned triangles and 10 bones on one of the four hero figures, plus a driver of 10 matrix operations a frame. Under 0.1 ms of the 4.5 ms people-and-vehicles GPU budget (about 2 percent), and no cost for the crowd, which never wears this cut. People are still capsules in the build (<code>HostViews.cs</code>), so there is nothing real to measure yet; Latch checks it with the perf harness (Tech with and without trousers) when the figure lands.",
 "<b>Main risk and fix:</b> the inner hems meeting (4 to 5 cm as drawn). Fix: hang the inner side straight, 11.5 cm from the axis instead of 15 (section 5). Result: 0 cm in all nine poses.",
 "<b>Second risk:</b> back-of-knee fold in the deepest crouch, 4 to 6 cm; hidden under the cloth layers of the thigh, the same as slim trousers. Fix: the usual knee corrective in the weights; recheck in the real rig. Hem length is the third limit: mid-shin is the longest that clears an 18 cm kerb by a hand; do not lengthen it.",
 "<b>Against the round 4 bell shorts:</b> in the same proxy the bell clips leg to leg too, 3 to 7 cm in idle, walk and run (round 4's warning was right), about the same as these trousers as drawn. It was not reproduced as a thigh clip (1.4 cm, deep crouch only). It clears with the same kind of fix, but a bigger one: the hem sits on the thigh, 30 cm from the pivot and 22 cm from its twin, so the inner side has to go from 15 to 10.5 cm. The trousers' hem hangs 60 cm down where there is room (15 to 11.5 cm) and the bell is rigid, so it has no hem swing; these have 10 bones (round 4 estimated +4 to +6 for the bell).",
]: H.append(f"<li>{t}</li>")
H.append("</ol></div>")

H.append("<h2>5. The one change, drawn</h2>")
H.append(img("TF-change-inner-hem.png", 1300, "Front, back and three-quarter, feet a hand apart. Only the inner side of each leg changed: it hangs straight down from the knee, two clean vertical hem edges with a gap between them. The outer flare, the bone panels, the teal piping and the mid-shin hem are as on the cosplay sheet."))
H.append("<table><tr><th>Half-width from the leg axis</th><th>as drawn</th><th>proposed</th></tr><tr><td>inner, at the knee</td><td>14 cm</td><td>11.5 cm</td></tr><tr><td>inner, at the hem</td><td>15 cm</td><td>11.5 cm</td></tr><tr><td>outer flare at the hem</td><td>20 cm</td><td>20 cm (unchanged)</td></tr><tr><td>front / back at the hem</td><td>13 cm</td><td>13 cm (unchanged)</td></tr><tr><td>hem height</td><td>mid-shin, 30 cm above the road</td><td>unchanged</td></tr></table>")
H.append("<p class=small>The Codex redraw shows the intent; it exaggerates the gap a little. The numbers are the specification.</p>")

H.append("<h2>6. HUD portrait and the team</h2>")
H.append("<p>The Tech in the new outfit, drawn with the same prompt, framing, light and finish as <code>A-codex-portrait-*-white</code> (references: the cosplay sheet for face and outfit, the old Tech and Fixer portraits for framing). Three candidates were made; this is the one whose chin-up, amused look is closest to the old portrait. Not installed in the HUD.</p>")
H.append(img("TF-portraits-four.jpg", 1300, "Scout, Fixer, Tech (new), Enforcer: the first, second and fourth are the existing A-codex-portrait-*-white files, unchanged."))
H.append("<div class='row'>" + img("TF-portrait-tech-white-chosen.png", 560, "Chosen: <code>TF-portrait-tech-white-chosen.png</code> (1024 x 1536)") + img("TF-portrait-tech-white-v1.png", 420, "Alternate v1") + img("TF-portrait-tech-white-v3.png", 420, "Alternate v3") + "</div>")
H.append("<h3>The four, full length</h3>")
H.append(img("TF-lineup-white-chosen.png", 1400, "Chosen lineup (v1). The Scout, Fixer and Enforcer are redrawn unchanged from the A white lineup; only the Tech is new. Alternates: v2, v3 below."))
H.append("<div class='row'>" + img("TF-lineup-white-v2.png", 650, "Alternate v2") + img("TF-lineup-white-v3.png", 650, "Alternate v3") + "</div>")
H.append("<p class=small>The Tech's pale-teal piping (about #b3d9d8, sampled from 23 pixels of the sheet, so approximate) is now the only colour in the white team. Its dE2000 to the signal colours: cones 50.5, guard 36.7, rivals 40.6, so it clears the gate of 20 by a wide margin; it sits 13 from the city's mint, which at hairline width is fine. If the team must be strictly white on charcoal, the piping can go bone-white: that is Sky's call.</p>")

H.append("<h2>7. Where the style contract no longer matches the look (not changed)</h2><ul>")
for t in ["<code>contracts/style/suborned.style.json</code> guide, the player-cell line (also in the machine copy of GDD §15 people): <i>\"black, cream and gold with a violet accent\"</i>. The look is now black, bone white and chrome, no gold and no violet; the Tech adds one pale-teal piping. GDD §15's own cell line already says no gold trim, so the contract and the GDD disagree with each other too. Violet is also the rivals' signal colour, so a violet cell accent was always a hazard.",
          "The same contract holds probes that still say \"white coat over black and gold with a violet accent\" (the Enforcer A-pose, about line 757) and \"a single violet light line ... gold details\" (the rifle, line 762), and its reference board still points at the old picks (<code>Cell/picks/*.png</code>) whose clothes were gold and violet.",
          "GDD §15 people rule: \"hems knee or above, nothing sheer/trailing/cloak-like\"; the contract says the cell inherit the people rules. The Tech's trousers end at mid-shin and flare wide. They are not sheer, trailing or cloak-like and the proxy shows they animate, but they break the letter of the hem rule. My own round 4 rule (\"hems at hip or mid-thigh\") is also superseded.",
          "GDD §15 cell: \"matte technical fabrics, fitted layers ... no deco motifs\": the Tech's cut is loose, not fitted. Pleated hakama-cut trousers over a fitted wrap vest; the team is otherwise fitted. Worth saying in the contract so the next prompt does not fight it."]:
    H.append(f"<li>{t}</li>")
H.append("</ul>")

H.append("<h2>8. Open issues and next owners</h2><ul>")
for t in ["<b>Sky:</b> sign off the one change (inner side 15 to 11.5 cm) and the portrait, and say whether the teal piping stays on a team that is otherwise colourless.",
          "<b>Aster, then Sky:</b> a GDD / contract edit for section 7 (new version, then <code>bun run crew</code>); I did not touch them.",
          "<b>Glyph and Latch (after sign-off):</b> install the Tech portrait. The HUD kit builds portraits with states and cameos (<code>portraits/portrait_&lt;role&gt;_&lt;state&gt;</code>, <code>cameo_&lt;role&gt;_normal</code>); the new Tech needs the same set cut from <code>TF-portrait-tech-white-chosen.png</code>.",
          "<b>Not proven:</b> the proxy is a stand-in. The real test is the built rig in Blender with the real mesh and weights (knee crease, outer-panel flap, hem hang), then Latch's perf A/B. I would not call the trousers done until both exist.",
          "<b>Generation limits:</b> Codex is unseeded, so these drawings do not repeat from the prompt; prompts and reference files are in <code>PROVENANCE.json</code>. Strip frames 1 and 3 of the walk are near copies (the model's choice). Combat animation beyond walk and run is still the project's top animation risk (§14): this test covered stance and shoot only as a pose, not a clip."]:
    H.append(f"<li>{t}</li>")
H.append("</ul>")

H.append("<h2>Files</h2><p class=small>All in <code>briefs/012/cell/tech-final/</code>: <code>TF-pose-*.jpg</code>, <code>TF-strip-*.jpg</code>, <code>sil50-*</code>, <code>sil56-*</code>, <code>proxy-sheet-*.png</code>, <code>plan-sections.png</code>, <code>TF-change-inner-hem.png</code>, <code>TF-lineup-white-*.png</code>, <code>TF-portrait-tech-white-*.png</code>, <code>TF-portraits-four.jpg</code>, <code>proxy/</code> (metrics.json, metrics-table.txt, plan-sections.json), <code>scripts/</code>, <code>PROVENANCE.json</code>. Reproduce the numbers: <code>/Applications/Blender.app/Contents/MacOS/Blender -b --python scripts/tf_run.py -- work/proxy --render --variants=AD,FX10,BELL</code> then <code>python3 -I scripts/tf_plan.py work/proxy/verts.npz work/proxy</code>.</p>")
open(D + "index.html", "w").write("\n".join(H)); import os; print(os.path.getsize(D + "index.html") / 1e6, "MB")
