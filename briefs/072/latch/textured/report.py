# Brief 072-F (Latch): textured against untextured, same v4 export, same batchmode estimator.
# Inputs: briefs/072/latch/after/offscreen.json (v4 untextured), briefs/072/latch/textured/offscreen.json (v4 textured),
#         texmem-before.json / texmem.json (this folder), briefs/072/texture/area.json (before) and area-after.json (after),
#         shaders.json (this folder; Shaders072: SRP batcher lines and texture reads per shader, 2026-10-28).
# usage: python3 -I briefs/072/latch/textured/report.py   → report.md beside it
import json, os
here = os.path.dirname(os.path.abspath(__file__))
J = lambda *p: json.load(open(os.path.join(here, *p)))
before, after = J('..', 'after', 'offscreen.json'), J('offscreen.json')
tb, ta = J('texmem-before.json'), J('texmem.json')
ab, aa = J('..', '..', 'texture', 'area.json'), J('..', '..', 'texture', 'area-after.json')
n = lambda x: f"{x:,.0f}"
d = lambda a, b: f"{'+' if a - b >= 0 else ''}{a - b:,.0f}"
L = [f"# 072-F textured against untextured (v4, batchmode offscreen), {after['when']}", "",
     f"Untextured: {before['export']} ({before['when']}). Textured: {after['export']} ({after['when']}). Same views, camera and estimator ({after['camera']}).", "",
     "**Nothing here is UnityStats.** " + after['notUnityStats'], "",
     "## Counts, as built (every face): untextured → textured", "",
     "| view | yaw | main tris | materials | SetPass est. (fwd + dn + shadow) | casters | shadow tris | batchmode ms (median) |",
     "|---|---:|---:|---:|---:|---:|---:|---:|"]
bv = {(v['view'], int(v['yaw'])): v for v in before['modes']['every']}
for v in after['modes']['every']:
    k = (v['view'], int(v['yaw'])); b = bv[k]; sb, sa = b['setPassEstimate'], v['setPassEstimate']
    L.append(f"| {k[0]} | {k[1]} | {n(b['mainTriangles'])} → {n(v['mainTriangles'])} ({d(v['mainTriangles'], b['mainTriangles'])}) | {b['materials']} → {v['materials']} | "
             f"{sb['total']} → {sa['total']} ({sa['forward']} + {sa['depthNormals']} + {sa['shadow']}; {d(sa['total'], sb['total'])}) | {b['shadow']['uniqueCasters']} → {v['shadow']['uniqueCasters']} | "
             f"{n(b['shadow']['triangles'])} → {n(v['shadow']['triangles'])} | {b['frameMs']['median']} → {v['frameMs']['median']} |")
MiB = lambda x: f"{x:.2f} MiB"
L += ["", "## Texture memory of the scene (every texture an enabled renderer's material binds)", "",
      "| | textures | GPU (format x size x mips x layers) | Editor runtime (Profiler.GetRuntimeMemorySizeLong) | surface_* set GPU |", "|---|---:|---:|---:|---:|",
      f"| untextured (v4 scene, {tb['when']}) | {tb['textures']} | {MiB(tb['gpuMiB'])} | {MiB(tb['runtimeMiB'])} | {MiB(tb['surfaceSet']['gpuMiB'])} |",
      f"| textured ({ta['when']}) | {ta['textures']} | {MiB(ta['gpuMiB'])} | {MiB(ta['runtimeMiB'])} | {MiB(ta['surfaceSet']['gpuMiB'])} |",
      f"| delta | {ta['textures'] - tb['textures']:+d} | {ta['gpuMiB'] - tb['gpuMiB']:+.2f} MiB | {ta['runtimeMiB'] - tb['runtimeMiB']:+.2f} MiB | budget 24 MiB |", "",
      "New textures: " + ", ".join(sorted({r['texture'] for r in ta['rows']} - {r['texture'] for r in tb['rows']})) + ".", ""]
def kinds(a):
    out = {}
    for m in a['materials']:
        k = m['maps']['kind']; o = out.setdefault(k, [0, 0.0]); o[0] += 1; o[1] += m['meanShare']
    return out
kb, ka = kinds(ab), kinds(aa)
L += ["## Screen area by map kind (mean of the 8 play views; all materials drawn opaque)", "",
      "| kind | materials before → after | mean share before → after |", "|---|---:|---:|"]
for k in sorted(set(kb) | set(ka), key=lambda k: -max(kb.get(k, [0, 0])[1], ka.get(k, [0, 0])[1])):
    b, a = kb.get(k, [0, 0.0]), ka.get(k, [0, 0.0])
    L.append(f"| {k} | {b[0]} → {a[0]} | {b[1] * 100:.2f} % → {a[1] * 100:.2f} % |")
flat_b = sum(v[1] for k, v in kb.items() if k in ('flat', 'swatch-atlas', 'swatch-atlas?'))
flat_a = sum(v[1] for k, v in ka.items() if k in ('flat', 'swatch-atlas', 'swatch-atlas?'))
L += ["", f"Flat colour left (kinds flat + swatch-atlas without detail): {flat_b * 100:.2f} % → {flat_a * 100:.2f} % of the frame. "
      "`flat` still counts excluded rows (glass film, emissives, contact shades, the crossing glow) and any kit row whose .mat has no detail pair; "
      "`swatch-atlas+detail` still holds FacadeLit's excluded classes (glass 3, neon 5), which the census cannot split from the textured classes.", "",
      "Largest flat rows left:", "", "| material | shader | mean share |", "|---|---|---:|"]
for m in [m for m in aa['materials'] if m['maps']['kind'] in ('flat', 'swatch-atlas')][:15]:
    L.append(f"| {m['material']} | {m['shader']} | {m['meanShare'] * 100:.2f} % |")
sh = J('shaders.json')
L += ["", f"## SRP batcher and texture reads per shader ({sh['when']}, Unity {sh['unity']})", "",
      "Method: " + sh['method'] + ".", "",
      "SRP batcher (the Inspector's line, per subshader):", ""]
for x in sh['shaders']:
    L.append(f"- `{x['shader']}` ({x.get('path', '')}): " + "; ".join(x.get('srpBatcher', ['missing'])))
L += ["", "Texture reads in the forward fragment, per keyword set the materials use (play-view pipeline keywords added). "
      "`shadow compares` are the main light's soft-shadow taps (High: a 7×7 tent, 16 compares); `samples` are the material's own maps and the detail fetches.", "",
      "| shader | local keywords | materials | samples | shadow compares | total | textures bound |", "|---|---|---:|---:|---:|---:|---:|"]
for x in sh['shaders']:
    for v in x.get('variants', []):
        if 'error' in v: L.append(f"| {x['shader']} | {v['localKeywords'] or '(none)'} | {v['materials']} | error: {v['error']} | | | |"); continue
        L.append(f"| {x['shader']} | {v['localKeywords'] or '(none)'} | {v['materials']} | {v['samples']} | {v['shadowCompares']} | {v['total']} | {v['textureBindings']} |")
open(os.path.join(here, 'report.md'), 'w').write('\n'.join(L) + '\n')
print('\n'.join(L))
