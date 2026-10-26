"""Intake stage page (brief 069 v2): intake/intake.html, self-contained, from intake.json, intake_tests.json, the base measurements, the mannequins, every
round's stills and their checks.  python3 -I tools/figures/charpipe/intake_html.py <intake dir> <round> [<round> ...]"""
import base64, html, io, json, os, sys
from PIL import Image

D = sys.argv[1]; ROUNDS = sys.argv[2:]
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(HERE)))
IN = json.load(open(os.path.join(HERE, 'intake.json'))); TESTS = json.load(open(os.path.join(HERE, 'intake_tests.json')))['people']
sp = __import__('importlib.util').util.spec_from_file_location('ip', os.path.join(HERE, 'intake_prompt.py')); ip = __import__('importlib.util').util.module_from_spec(sp); sp.loader.exec_module(ip)
E = html.escape


def img(path, w=None, jpeg=True):
    im = Image.open(path).convert('RGB')
    if w: im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
    b = io.BytesIO(); im.save(b, 'JPEG' if jpeg else 'PNG', quality=88)
    return f'<img src="data:image/{"jpeg" if jpeg else "png"};base64,{base64.b64encode(b.getvalue()).decode()}">'


def table(rows, head):
    return '<table><tr>' + ''.join(f'<th>{E(h)}</th>' for h in head) + '</tr>' + ''.join('<tr>' + ''.join(f'<td>{c}</td>' for c in r) + '</tr>' for r in rows) + '</table>'


def main():
    base = json.load(open(os.path.join(D, 'base_pose.json'))); posed = json.load(open(os.path.join(D, 'intake_pose.json')))
    s = []
    # results first
    res = {}
    for r in ROUNDS:
        res[r] = []
        for p in TESTS:
            j = os.path.join(D, 'checks', f'{r}_{p["id"]}.json')
            if os.path.exists(j): res[r].append((p, json.load(open(j))))
    rate = {r: f'{sum(1 for _, c in v if c["verdict"]["pass"])} of {len(v)}' for r, v in res.items()}
    s.append('<p class="lead"><b>Pass rate</b> ' + E(', '.join(f'{r}: {v}' for r, v in rate.items())) + '. Every still is checked by <code>intake_check.py</code> against the base\'s own silhouette in the same pose; the numbers are under each still. Nothing paid: the stills are Codex image generation at $0 through the paid helper\'s ledger (<code>paid/069-intake</code>).</p>')
    # 1 measurements
    keys = ['armDownFromHorizontal', 'armForward', 'elbowBend', 'wristBend', 'fingerSpread', 'legOutFromVertical', 'kneeBend', 'footYawOut', 'footPitchDown']
    rows = [[E(k), base['m']['Left'][k], base['f']['Left'][k]] for k in keys]
    rows += [[E(k), base['m'][k], base['f'][k]] for k in ('hipJointWidthM', 'ankleSpacingM', 'shoulderWidthM', 'ankleSpacingOverHipJoints', 'neckForwardFromVertical', 'headForwardFromVertical', 'trunkForwardFromVertical')]
    s.append('<h2>1 · The base\'s rest pose, measured</h2><p>MPFB\'s own rest for its <code>mixamo_unity</code> rig, man and woman at the reference macros (<code>intake_measure.py</code>, <code>intake/base_pose.json</code>). Degrees; metres.</p>' + table(rows, ['', 'man', 'woman']))
    s.append('<p class="flaw">MPFB\'s rest is an A-pose an image model can\'t be asked for: the elbows bent 39-47 degrees, the legs splayed 5-6 degrees with the ankles at 1.8 times the hip-joint width (the same splay Latch found bowing the walk), and the palms turned out and up. So the intake pose is defined as numbers in <code>intake.json</code> (<code>pose</code>), the base is posed into it by rule (<code>intake_pose.py</code>: arms, forearm twist, fingers, legs and feet aimed; nothing by hand), and that posed base is the wrap target, the pose reference and the check\'s base all at once:</p>')
    pr = [[E(k), posed['m'][k], posed['f'][k]] for k in ('standingHeightM', 'armDownFromHorizontal', 'elbowBend', 'hipJointWidthFraction', 'ankleSpacingFraction', 'footYawOut', 'wristHeightFraction', 'handOutFraction')]
    s.append(table([[E(k), E(str(v))] for k, v in IN['pose'].items()], ['intake pose (intake.json)', 'value']) + table(pr, ['the base in the intake pose, re-measured', 'man', 'woman']))
    s.append('<h3>The mannequin sheets (the pose reference sent with the prompt) and the silhouettes (the check\'s base)</h3>' + img(os.path.join(D, 'mannequin_m.png'), 900) + img(os.path.join(D, 'mannequin_f.png'), 900) + img(os.path.join(D, 'silhouette_m.png'), 450))
    # 2 prompt
    ex = TESTS[2]
    s.append('<h2>2 · The prompt (data: <code>tools/figures/charpipe/intake.json</code>)</h2><p>The template with its slots, then the full prompt for one test person (' + E(ex['id']) + '). Each person fills three slots (person, outfit, palette) from <code>intake_tests.json</code>; the pose, sheet, ground, light, style and clothing rules are fixed. ' + E(IN['prompt']['people_rule']) + '</p>')
    s.append('<h3>Template</h3><pre>' + E(IN['prompt']['template']) + '</pre><h3>Pose words (the key lines)</h3><pre>' + E(IN['prompt']['pose_words']) + '</pre><h3>Full prompt, ' + E(ex['id']) + '</h3><pre>' + E(ip.prompt(ex)) + '</pre>')
    s.append('<h3>The sheet</h3>' + table([[E(k), E(str(v))] for k, v in IN['sheet'].items()], ['', '']))
    cl = IN['clothing']
    s.append('<h3>Clothing</h3>' + table([['allowed', E('; '.join(cl['allowed']))]] + [[f'flagged: {E(k)}', E(v)] for k, v in cl['flagged'].items()] + [['forbidden', E('; '.join(cl['forbidden']))]], ['', '']))
    s.append('<h3>Image-to-3D, later</h3>' + table([[E(k), E(json.dumps(v))] for k, v in IN['imageTo3D'].items()], ['', '']))
    # 3 check
    t = IN['check']
    s.append('<h2>3 · The pose check (<code>intake_check.py</code>)</h2><p>' + E(t['note']) + '</p>' + table([[E(k), E(str(v))] for k, v in t['tolerances'].items()], ['tolerance', 'value']) + '<p>' + E(t['calibration']) + '</p>')
    selfrows = []
    for n in ('self_m', 'self_f', 'wrong_m'):
        c = json.load(open(os.path.join(D, 'checks', n + '.json'))); v = c['verdict']
        selfrows.append([E(n), 'PASS' if v['pass'] else 'FAIL', E(json.dumps({k: v['numbers'][k] for k in ('arm_screenLeft', 'arm_screenRight', 'legSplayDeg', 'footYawDegEst', 'feetSpacingH')})), E('; '.join(v['fails']))])
    s.append(table(selfrows, ['test', 'verdict', 'numbers', 'fails']) + img(os.path.join(D, 'checks', 'wrong_m.png'), 700) + '<p class="cap">The negative test: a mannequin posed wrong (arms at 30 degrees, legs leaning out 6.8, feet turned out 20) with the base\'s silhouette edge in red and the measured arm lines, feet and shins drawn in.</p>')
    # 4 rounds
    for r in ROUNDS:
        s.append(f'<h2>4 · Round {E(r)}: {rate[r]} pass</h2>' + E(ROUND_NOTES.get(r, '')))
        for p, c in res[r]:
            v = c['verdict']; n = v['numbers']
            s.append(f'<h3>{E(p["id"])} · {"PASS" if v["pass"] else "FAIL"}</h3>' + img(os.path.join(D, 'checks', f'{r}_{p["id"]}.png'), 1100) +
                     f'<p class="cap">arms {n["arm_screenLeft"]} / {n["arm_screenRight"]} deg below horizontal (base {c["base_views"]["front"]["armDownFromHorizontal"]}) · legs lean {n["legSplayDeg"]} deg · feet turned out {n["footYawDegEst"]} deg (est.) · feet {n["feetSpacingH"]} H apart (base {n["feetSpacingH_base"]}) · arms apart from the body on {n["armGapRows"]} of the rows · views {n["viewHeightSpreadPct"]} % apart in height. ' +
                     (('<b>Fails:</b> ' + E('; '.join(v['fails']))) if v['fails'] else 'No fails.') + ' Red: the base\'s silhouette, scaled to each view\'s height and soles.</p>')
    # 5 what failed and next
    s.append('<h2>5 · What failed, and why</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in FAILED) + '</ul>')
    s.append('<h2>6 · Next: the wrap test (needs Sky\'s sign-off of this prompt first)</h2><ul>' + ''.join(f'<li>{E(x)}</li>' for x in NEXT) + '</ul>')
    page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Brief 069 v2 · the intake prompt</title><style>
body{{font:14px/1.5 -apple-system,Helvetica,Arial,sans-serif;background:#f7f4ec;color:#17302f;margin:0;padding:20px 32px;max-width:1300px}}
h1{{font-size:24px;margin:0}} h2{{font-size:19px;margin:30px 0 8px;border-bottom:2px solid #d2e6e2}} h3{{font-size:15px;margin:16px 0 4px}}
img{{display:block;max-width:100%;margin:6px 0;border:1px solid #d2e6e2}} pre{{white-space:pre-wrap;background:#fff;border:1px solid #d2e6e2;padding:8px 12px;font-size:12.5px}}
table{{border-collapse:collapse;margin:8px 0;font-size:13px}} td,th{{border:1px solid #cbc6c3;padding:3px 7px;vertical-align:top;text-align:left}} th{{background:#e9efe9}}
.flaw{{background:#fff;border-left:4px solid #f0a020;padding:6px 12px}} .cap{{font-size:12.5px;color:#33504d}} .lead{{background:#fff;border-left:4px solid #60e0ea;padding:8px 12px}} code{{background:#eee8dc;padding:0 3px}}
</style></head><body><h1>Brief 069 v2 · the intake prompt: every generated person stands in the base's pose</h1>
<p class="cap">Forma, 2026-10-26. For Sky's sign-off before any paid image-to-3D or wrap. Generate (Codex still) -> image-to-3D -> wrap onto the MPFB base (its skeleton, weights and rest) -> texture transferred -> recolour by mask. This stage fixes the still. Code: <code>tools/figures/charpipe/intake_measure.py, intake_pose.py, intake_prompt.py, intake_check.py, intake_html.py</code>; data: <code>intake.json, intake_tests.json</code>.</p>
{''.join(s)}</body></html>"""
    open(os.path.join(D, 'intake.html'), 'w').write(page); print('wrote', os.path.join(D, 'intake.html'), round(len(page) / 1e6, 1), 'MB')


ROUND_NOTES = {
 'r1': ' The first prompt: the figure at 58 % of the sheet\'s height, "the feet directly below the hips, one hand\'s width apart". The arms land at 45-49 degrees on every still, the legs straight, the feet forward; three fail because the outstretched fingertips cross into the next view (the crop for image-to-3D would cut them).',
 'r2': ' Changes from r1 (logged in intake.json): the figure at 54 % of the sheet\'s height with the fingertips "well inside its own third", the mannequin re-rendered to match; the stance "no wider than the hips, the inner edges of the feet one hand\'s width apart".',
}
FAILED = [
 'Round 1, 3 of 8: fingertips past their third of the sheet (t4, t5, t6): the prompt\'s 58 % figure height left no room for the hands at 45 degrees. Fixed in r2 by scale (54 %), in both the prompt and the mannequin.',
 'My own check, before it was trusted (all caught on the mannequin and the stills, logged): spread fingers read as separate runs (the arm angle took a fingertip); a shading line split the torso (a sliver counted as arm); the mannequin\'s grey-on-grey edges fell under a fixed threshold (now set by the ground\'s flatness); the feet-spacing gate compared heavier bodies with the reference body (now a loose check; the build-independent leg splay is the gate); boots flare at the sole, so splay is measured up the shin and judged against straight (0), not the bare-footed base.',
 'The foot-yaw estimate is rough: straight booted feet read 1-8 degrees, a 20-degree turn reads about 40 (leaning legs push the feet out too). It gates at 12; a turn under about 6-8 degrees can pass. A cleaner yaw needs the 3D mesh (the wrap test measures it exactly).',
 'Not measured by the check: the hands (palms in, fingers apart) and the side view\'s arm forward angle; they are in the prompt and visible in the pictures.',
]
NEXT = [
 'Which stills: three that pass and cover the risks: the coat (t2: a hem at mid-thigh, the long-garment risk), the skirt (t1) and the guard (t3: a cap, a belt, a yoke); a fourth if the budget allows, the cape collar (t6).',
 'Image-to-3D: Hunyuan3D 3.1 Pro on Runware (the cheapest multi-view model in our ledger, $0.675 a model): the front, left and back thirds of the sheet, ground removed, about 30k faces with texture. 3 stills = about $2.03 (4 with the cape about $2.70; one retry each at most: under $5.40). Through paid.py with an explicit budget; needs Aster and Sky\'s go-ahead.',
 'Align: the generated mesh scaled to the base\'s height and set on the same soles; it already stands in the base\'s pose (that is what this stage is for).',
 'Wrap: the MPFB base (the dense body, not the low proxy), with its sliders fitted to the person (height, then shoulder and hip width from the mesh), is shrink-wrapped onto the generated surface along its normals with smoothing, so it keeps its own topology, UVs, skeleton and weights; where a garment leaves the body (a hem, a hood, an afro, a collar) the part of the generated mesh more than a few cm from the wrapped body is cut out as its own garment piece, weighted by Data Transfer from the body (as the shelved wardrobe\'s rings were), with hem weights blended to the hips and thighs.',
 'Texture: the generated texture is baked onto the wrapped base\'s UVs (Blender Cycles, selected to active, cage from the wrap), and onto each garment piece\'s own UVs; the colour regions are then clustered into a mask (main, base, trim, skin, hair) for recolouring, written to the mask channel the pipeline already carries.',
 'Rest: the wrapped figure is posed from the intake A-pose to the standard rest by its own skeleton; Latch\'s neutral-leg rule (legs straight, feet forward) goes into its avatar, as for every figure.',
 'Gate: the wrap test passes when the wrapped figure plays walk and run with the clip fixes, with no tears at the armpits, crotch or hems, and the texture holds at play zoom and in the follow view.',
]

main()
