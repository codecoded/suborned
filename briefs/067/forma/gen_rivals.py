# Writes the three rival entries of tools/figures/figures.json (scratch generator, brief 067). python3 -I out/scratch/forma/067/gen_rivals.py
import json, subprocess
R = '/Users/sky/Projects/suborned/'
orig = subprocess.run(['git', '-C', R, 'show', 'HEAD:tools/figures/figures.json'], capture_output=True, text=True, check=True).stdout
def hexf(h): h = h.lstrip('#'); return [round(int(h[i:i + 2], 16) / 255.0, 3) for i in (0, 2, 4)]
HEAD = {"mask": "masks/d4_head.png"}; LEGS = {"mask": "masks/d4_legs.png"}; NOTLEGS = {"mask": "masks/d4_legs.png", "invert": True}; HAIRONLY = {"mask": "masks/d4_hair.png", "invert": True}
NAVY = {"from": 215, "width": 40, "minSat": 0.3}
def rival(fid, tag, jacket, seam, ribbing, trousers, boots, hair, label, notes):
    return {
        "id": fid, "role": "rival", "kind": "transfer", "label": label,
        "donor": "Citizens3D/rigged/corporation_C1__meshy7_walk.glb", "noMetal": True,
        "source": "Citizens3D/citizens/commuters_D4__hunyuan_pro.glb (50k, decimated here to 8000, 3000 and 1500; Hunyuan 3.1 pro, static; skinned from the C1 Meshy rig); recoloured here: " + tag,
        "provenance": ["citizens/commuters_D4__hunyuan_pro.glb"],
        "lods": [{"src": "Citizens3D/citizens/commuters_D4__hunyuan_pro.glb", "tex": 2048, "decimateTo": 8000}, {"from": 0, "tris": 3000, "tex": 1024}, {"from": 0, "tris": 1500, "tex": 512}],
        "smoothness": 0.2, "matSmoothness": 0.05, "noNormal": True, "notes": notes,
        "repair": [
            {"op": "flatten", "sat": [0.0, 0.16], "val": [0.5, 1.0], "to": 0.72, "k": 0.4},
            {"op": "recolour", "hue": [0, 360], "sat": [0.0, 0.16], "val": [0.5, 1.0], "to": jacket, "keep": 0.75, "exact": True, "protect": [HEAD]},
            {"op": "recolour", "hue": [0, 360], "sat": [0.0, 0.16], "val": [0.12, 0.5], "to": seam, "keep": 0.8, "exact": True, "protect": [HEAD, LEGS]},
            {"op": "recolour", "hue": [0, 360], "sat": [0.0, 0.3], "val": [0.0, 0.28], "to": boots, "keep": 0.5, "only": [{"mask": "masks/d4_boots.png"}]},
        ],
        "tint": [
            dict(NAVY, **trousers, keep=[NOTLEGS]),
            dict(NAVY, **ribbing, keep=[LEGS]),
            dict({"from": 44, "width": 16, "minSat": 0.18}, **hair, keep=[HAIRONLY]),
            # the deepest brown shades of skin and hair (V .30 to .36, just under the stats' skin rule) go a notch darker: they are shade, not trim, and only a measure takes them for amber
            {"from": 28, "width": 14, "minSat": 0.3, "minVal": 0.30, "maxVal": 0.36, "shift": 0, "sat": 1.0, "val": 0.8},
        ],
    }
NOTE = "brief 067 (W1): replaces the dark violet Worker D5 (hard hat, amber trim, median V 0.27). Concept: paid/067-rival-look/codex/rival_lineup_1.png (Codex, $0). Masks: tools/figures/masks/d4_*.png (zmask.py, per texel from the mesh)."
A = rival('rival', 'mid-violet velvet jacket, deep-violet ribbing and tunic, ivory trousers, pearl boots, dark brown hair',
          hexf('#7a58b8'), hexf('#4b3578'), {"shift": 55, "sat": 0.6, "val": 1.4}, {"shift": -170, "sat": 0.09, "val": 2.7}, hexf('#a9a8b4'), {"shift": -12, "sat": 1.0, "val": 0.55},
          "Rival crew A (the Velvet Set): Commuter D4's bomber in mid-violet velvet, deep-violet ribbing and tunic, ivory trousers, pearl boots, dark hair; bare head, no cap, no gold", NOTE)
B = rival('rival_b', 'deep-violet jacket (the art bible violet_set), plum ribbing, pale grey trousers, platinum hair',
          hexf('#6b4f8f'), hexf('#43306b'), {"shift": 60, "sat": 0.55, "val": 1.25}, {"shift": 15, "sat": 0.06, "val": 2.6}, hexf('#9a9aa6'), {"shift": 0, "sat": 0.12, "val": 1.9},
          "Rival crew B: Commuter D4's bomber in deep violet (violet_set #6b4f8f), plum ribbing and tunic, pale grey trousers, platinum hair; bare head, no cap, no gold", "brief 067 (W1): the second colourway of the crew: the darkest jacket, pale grey legs, silver hair.")
C = rival('rival_c', 'lavender-violet jacket, mid-violet ribbing, pearl trousers, auburn hair',
          hexf('#a58bd8'), hexf('#6b4f9a'), {"shift": 52, "sat": 0.55, "val": 1.65}, {"shift": -165, "sat": 0.07, "val": 2.8}, hexf('#b4b2bc'), {"shift": -22, "sat": 1.4, "val": 1.0},
          "Rival crew C: Commuter D4's bomber in lavender-violet velvet, mid-violet ribbing and tunic, pearl trousers, auburn hair; bare head, no cap, no gold", "brief 067 (W1): the third colourway of the crew: the lightest jacket, auburn hair.")

# ---- rival_d: the crew's woman, on Commuter C6's body (a silver hooded jacket over a patterned mini dress, white boots): the hood is the silhouette no D4 has
FACE = {"mask": "masks/c6_face.png"}; BOOTS6 = {"mask": "masks/c6_boots.png"}
D = {
    "id": "rival_d", "role": "rival", "kind": "transfer",
    "label": "Rival crew D: Commuter C6's hooded jacket in violet velvet, deep-violet lining, pale dress, pearl boots; the crew's woman; bare head (the hood is down), no cap, no gold",
    "donor": "Citizens3D/rigged/corporation_C1__meshy7_walk.glb", "noMetal": True,
    "source": "Citizens3D/bakeoff/commuters_C6__hunyuan_pro.glb (50k, decimated here to 8000, 3000 and 1500; Hunyuan 3.1 pro, static; skinned from the C1 Meshy rig); recoloured here: violet hooded jacket, deep-violet lining and skirt hem, white dress, pearl boots",
    "provenance": ["bakeoff/commuters_C6__hunyuan_pro.glb"],
    "lods": [{"src": "Citizens3D/bakeoff/commuters_C6__hunyuan_pro.glb", "tex": 2048, "decimateTo": 8000}, {"from": 0, "tris": 3000, "tex": 1024}, {"from": 0, "tris": 1500, "tex": 512}],
    "smoothness": 0.2,
    "notes": "brief 067 (W1): a fourth look for the crew, a woman, so the three posted are not three of one build; hood and jacket violet, the dress stays pale. Masks: tools/figures/masks/c6_*.png (zmask.py, per texel from the mesh).",
    "repair": [
        {"op": "recolour", "hue": [0, 32], "sat": [0.1, 0.3], "val": [0.8, 1.0], "to": [0.9, 0.88, 0.84], "keep": 0.4, "boxes": [[0.723, 0.044, 0.796, 0.098], [0.903, 0.256, 0.977, 0.342]]},
        {"op": "recolour", "hue": [0, 360], "sat": [0.0, 0.16], "val": [0.5, 0.86], "to": hexf('#8a68c4'), "keep": 0.75, "exact": True, "protect": [FACE, BOOTS6]},
        {"op": "recolour", "hue": [0, 360], "sat": [0.0, 0.16], "val": [0.12, 0.5], "to": hexf('#4f3a80'), "keep": 0.8, "exact": True, "protect": [FACE, BOOTS6]},
        {"op": "recolour", "hue": [0, 360], "sat": [0.0, 0.2], "val": [0.55, 1.0], "to": hexf('#b4b2bc'), "keep": 0.5, "only": [BOOTS6]},
    ],
    "tint": [
        dict(NAVY, shift=55, sat=1.5, val=1.7),   # C6's navy is duller than D4's: the skirt hem and the dress's dark chevrons go a clear deep violet
        # the dress's coral chevrons go lilac (coral beside a violet crew is the amber the spec leaves out); only on the dress (UV mask), so no skin is touched
        {"from": 15, "width": 32, "minSat": 0.28, "minVal": 0.25, "shift": 262, "sat": 0.8, "val": 1.0, "keep": [{"mask": "masks/c6_dress.png", "invert": True}]},
        {"from": 28, "width": 14, "minSat": 0.3, "minVal": 0.30, "maxVal": 0.36, "shift": 0, "sat": 1.0, "val": 0.8},
    ],
}
i = orig.index('  {\n   "id": "rival",')
blocks = [('\n'.join('  ' + l for l in json.dumps(f, indent=1, ensure_ascii=True).split('\n'))) for f in (A, B, C, D)]
text = orig[:i] + ',\n'.join(blocks) + '\n ]\n}\n'
json.loads(text); open(R + 'tools/figures/figures.json', 'w').write(text); print('written', len(text))
