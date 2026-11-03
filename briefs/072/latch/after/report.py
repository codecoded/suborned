# Brief 072-E (Latch): offscreen.json (batchmode after capture) against the baseline (briefs/072/latch/baseline/counts.json) → offscreen.md.
# usage: python3 -I briefs/072/latch/after/report.py
import json, os
here = os.path.dirname(os.path.abspath(__file__))
after = json.load(open(os.path.join(here, 'offscreen.json')))
base = json.load(open(os.path.join(here, '..', 'baseline', 'counts.json')))
bv = {(v['view'], int(v['yaw'])): v for v in base['views']}
ev = after['modes']['every']; tv = {(v['view'], int(v['yaw'])): v for v in after['modes'].get('today', [])}
n = lambda x: f"{x:,.0f}"
def d(a, b): return f"{'+' if a - b >= 0 else ''}{a - b:,.0f}"
L = [f"# 072 after, offscreen (batchmode), {after['when']}", "",
     f"Scene: {after['export']}. {after['camera']}. {after['drawer']}.", "",
     "**Nothing here is UnityStats.** " + after['notUnityStats'], "",
     "The UnityStats rerun (Baseline072.AfterCli: visible Editor, Game view at 1920x1080) is queued until the Studio is free.", "",
     "## Baseline census method: before (export d6f7560d, Editor, 18:13) against after (v4 a50c649a, batchmode)", "",
     "| view | yaw | visible renderers before → after | census tris before → after (Δ) | baseline UnityStats tris / SetPass / casters (before only) |",
     "|---|---:|---:|---:|---:|"]
for v in ev:
    k = (v['view'], int(v['yaw'])); b = bv[k]; s = b['stats']
    L.append(f"| {k[0]} | {k[1]} | {b['census']['visibleRenderers']} → {v['censusVisibleRenderers']} | {n(b['census']['triangles'])} → {n(v['censusTriangles'])} ({d(v['censusTriangles'], b['census']['triangles'])}) | {n(s['triangles'])} / {s['setPassCalls']:.0f} / {s['shadowCasters']:.0f} |")
L += ["", "## Every face against today on the same build (EveryFace off: the procedural walls the modules replace), batchmode estimates", "",
      "| view | yaw | main tris today → every (Δ) | materials | SetPass est. today → every (fwd + dn + shadow) | casters today → every | shadow tris today → every (Δ) | all-pass tris est. every | modules in frustum (columns) | batchmode ms every / today |",
      "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|"]
for v in ev:
    k = (v['view'], int(v['yaw'])); t = tv.get(k)
    sp = v['setPassEstimate']
    if t:
        L.append(f"| {k[0]} | {k[1]} | {n(t['mainTriangles'])} → {n(v['mainTriangles'])} ({d(v['mainTriangles'], t['mainTriangles'])}) | {t['materials']} → {v['materials']} | {t['setPassEstimate']['total']} → {sp['total']} ({sp['forward']} + {sp['depthNormals']} + {sp['shadow']}) | {t['shadow']['uniqueCasters']} → {v['shadow']['uniqueCasters']} | {n(t['shadow']['triangles'])} → {n(v['shadow']['triangles'])} ({d(v['shadow']['triangles'], t['shadow']['triangles'])}) | {n(v['trianglesAllPassesEstimate'])} | {v['modules']['modulesInFrustum']} ({v['modules']['columnsInFrustum']}) | {v['frameMs']['median']} / {t['frameMs']['median']} |")
L += ["", f"Cascades (every, first view): {ev[0]['shadow']['note']}; per cascade casters / tris: " + ", ".join(f"{c['casters']} / {n(c['triangles'])}" for c in ev[0]['shadow']['cascades']) + ".", "",
      "Reading it: the all-pass triangle estimate (main + SSAO depth-normals prepass over opaque + shadow per cascade) is the number to set against UnityStats triangles once the Game-view rerun exists; the baseline's UnityStats SetPass (55-69) against this estimator's 36-41 says the estimator undercounts by the fullscreen and per-camera passes it leaves out, so read its deltas, not its level. ms are Studio batchmode offscreen, indicative only."]
open(os.path.join(here, 'offscreen.md'), 'w').write('\n'.join(L) + '\n')
print('\n'.join(L))
