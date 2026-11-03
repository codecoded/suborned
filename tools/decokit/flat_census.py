"""Brief 072-F list (Vela): every surface that renders as a flat colour with no texture, and what textures it. No bpy, no Unity: reads the sources and writes
briefs/072/texture/flat.json + flat.md. python3 tools/decokit/flat_census.py

Rows come from three places:
  facade   the FacadeLit surface classes (ProcPaint.Cls; FacadeSurface.hlsl): the swatch atlas, point-sampled, colour only, plus the shader's procedural detail per class
  look     the builder's own URP Lit materials (look.json surfaces with no `material`: colour only), drawn by Look.Mat for roads, ground, megastructure parts, glazing
  kit      DecoKit .mat files in unity/Assets/Suborned/Kit/Materials with no texture bound (the untextured enamel, metal, stone and emit rows unity_mats.py writes)
Screen share per view comes from Latch's census, briefs/072/texture/area.json, when it exists: {"views": [{"view": "office-front", "yaw": 30, "materials": {"<material>": <share 0..1>}}]}
(material = the Unity material name: `facade`, `facade-gloss`, a look surface, a DK_ name). Until then `share` is null and `placements` (kit pieces placed in the
world export that carry the material) is the ranking proxy for kit rows."""

import hashlib
import json
import re
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUTDIR = REPO / "briefs/072/texture"
KITMATS = REPO / "unity/Assets/Suborned/Kit/Materials"
TABLE = REPO / "out/decokit/decokit_materials.json"
KIT = REPO / "contracts/world/kit.json"
WORLD = REPO / "out/world/harbour-district.world.json"
LOOK = REPO / "unity/Assets/Suborned/Editor/World/look.json"
AREA = OUTDIR / "area.json"
SHADERS = {"2529b42165e1d45a8bcfb0013bb5aba3": "URP Lit", "206122040e60d470e92cddeccf1aacf6": "URP Complex Lit", "0a6625b81c13d56024a16fdc589a1954": "DecoKit GlassSky",
           "0d01e3": "DK_RobotFace"}

FACADE = [  # class, name, made in, where it shows, the shader's detail today, proposal
    (0, "matte", "Proc/ProcFacade.cs (roof caps), QuadKit.cs (FacadeKit far LODs, MegaKit, gate towers, the civic style kit), RoofKit.cs, FabricKit.cs (awnings, banners), ProcWindows.cs (mullions)",
     "roofs (the largest flat area in the play view), every QuadKit wall (megastructures, gate towers, far LODs, civic style kit), awnings, service doors",
     "none: flat swatch x vertex AO", "surface_detail_v1: layer 4 roof on up-facing faces (n.y > 0.7), else layer 0 render; QuadKit stone walls pick layer 5 panel and FabricKit cloth layer 0 through the free vertex-colour B channel (design.md)"),
    (1, "stone", "Proc/ProcFacade.cs (walls, plinths, cornices)", "procedural building walls", "procedural ashlar joints + two value-noise grain octaves (fades out above 3 cm/px)",
     "layer 1 limestone replaces the two noise octaves (one array sample instead of 8 hashes); joints stay procedural"),
    (2, "enamel", "Proc/ProcFacade.cs, ProcShop.cs, FabricKit.cs (bands, fascias, faience plinths)", "bands, fascias, plinths", "procedural 12 cm mosaic grid, gloss 0.78 (grid fades above 9 cm/px: flat at play zoom)",
     "layer 2 enamel (glaze mottle + ripple), grid kept"),
    (3, "glass", "Proc/ProcWindows.cs, ProcFacade.cs", "procedural windows", "procedural fake-sky mirror, per-pane room, fresnel (brief 048)", "excluded: already shaded per pixel; a detail would fight the film"),
    (4, "chrome", "FabricKit.cs, ProcShop.cs, ProcFacade.cs (rails, masts, frames)", "rails, masts, frames (thin)", "none: metallic 1, smoothness 0.82", "layer 3 metal at 0.35 strength"),
    (5, "neon", "ProcShop.cs (signs)", "shop signs at night", "emission = swatch x 1.7", "excluded: emissive; a sign stays one clean colour"),
    (6, "brass", "ProcFacade.cs, FabricKit.cs (hairlines, beads, frames)", "hairlines and beads (SF-21: thin lines only)", "none: metallic 1, smoothness 0.7", "layer 3 metal, full strength"),
]

EXCLUDE = [  # (regex on name, reason) for kit rows; first match wins
    (r"Signal|Beacon|Cross_Light|Road_Lens|Lamp|Bulb|Neon|Glow|Screen|Exhaust|Plume|Fountain_(Jet|Light|Foam)|Spray|RobotEye|Dial", "emissive or a light volume: a signal or lamp stays one clean colour (signals readable unlit, §15)"),
    (r"Glass|GlassDark", "transparent glass: shaded as glass (GlassSky film, clear panes)"),
    (r"^DK_Face_", "robot display face: its own shader (dot matrix)"),
    (r"Planet|Moon", "sky body: horizon_tex.py maps (6d7772b2) belong on these; check the binding (listed untextured here)"),
    (r"Water|Pool", "water: the Water shader's job (ripple), not a detail map"),
    (r"Leaf|Foliage|Bloom|GroundCover|Petiole|Stem|Coconut|Bark", "foliage and bark: a shared leaf / bark detail is a later layer (not in DecoKit's map set); listed"),
    (r"Skin|Pastry|Cola|Leather|Velvet|Mood|Cafe_TB_|Cafe_", "café interior / prop dressing behind glass, small at play zoom: no texture this round (reason: area)"),
]
PAIR = {"enamel": "surface_enamel pair (detail albedo + normal, bump 0.25)", "ceramic": "surface_enamel pair (glaze)", "metal": "surface_metal pair (brushed)",
        "chrome": "surface_metal pair (brushed, faint: bump 0.1)", "brass": "surface_metal pair (satin)", "satin": "surface_metal pair", "stone": "surface_limestone pair",
        "rubber": "surface_limestone pair at 0.5 (a matte grain)", "roadmetal": "surface_metal pair"}

LOOK_PROPOSAL = {
    "pavement": "draws ground/pavement (DK_Ground_Pavement, textured) where the layer has it; the base surface is colour only: switch the base to the ground material",
    "quay": "DK_Ground_Pavement or DK_Pave_Civic (DecoKit flags) via `material`", "foot": "as pavement", "open": "as pavement",
    "circus/island": "DK_Ground_Lawn or DK_Pave_Civic via `material`", "park": "ground/park (DK_Ground_Lawn) exists: switch the base", "median": "ground/median (lawn) exists: switch the base",
    "beyond": "beyond the district (horizon): low priority; DK_Ground_Pavement at its tile", "beyond_ground": "as beyond", "beyond_green": "DK_Ground_Lawn", "beyond_hill": "DK_Ground_Lawn",
    "bay": "kerb bay: DK_Ground_RoadPearl or DK_Ground_Asphalt via `material`", "track": "transit track: surface_metal pair on a shared track material",
    "pylon": "surface_metal pair (URP Lit detail slots on the Look material: no new material)", "column": "as pylon", "limestone": "surface_limestone pair",
    "deck": "DK_Pave_Civic or surface_limestone pair", "stair": "surface_limestone pair", "glass": "excluded: transparent glazing", "trim": "surface_metal pair",
    "crossing": "excluded: crossing eye is signal-adjacent; light in the road stays emissive", "canopy": "surface_metal pair", "plate": "surface_metal pair", "hoop": "surface_metal pair",
    "booth": "surface_enamel pair", "tub": "surface_limestone pair", "crane": "surface_enamel pair", "girder": "surface_metal pair",
    "beam": "surface_enamel pair (coral transit beam)", "platform": "surface_limestone pair or DK_Pave_Civic", "water": "excluded: Water shader",
}


SURFACE_GUIDS = {hashlib.md5(f"suborned.decokit.tex/surface_{p}_detail_v1{n}.png".encode()).hexdigest(): p for p in ("enamel", "metal", "limestone") for n in ("", "_normal")}  # = unity_mats.tex_guid


def kit_mats():
    rows = {}
    for f in sorted(KITMATS.glob("*.mat")):
        t = f.read_text(errors="ignore")
        tex = len(re.findall(r"m_Texture: \{fileID: 2800000", t))
        g = re.search(r"m_Shader: \{fileID: \d+, guid: ([0-9a-f]+)", t)
        sh = g.group(1) if g else ""
        surf = sorted({SURFACE_GUIDS[g] for g in re.findall(r"guid: ([0-9a-f]{32})", t) if g in SURFACE_GUIDS})  # 072-F: the surface pair (unity_mats.surface_detail) is detail, not a colour map
        rows[f.stem] = dict(textures=tex, shader=SHADERS.get(sh, SHADERS.get(sh[:6], sh[:8])), surface=surf)
    return rows


def placements():
    kit = json.loads(KIT.read_text())["assets"]
    mats_of = {a["id"]: a.get("materials", []) for a in kit}
    w = json.loads(WORLD.read_text()) if WORLD.exists() else {}
    c = Counter()

    def walk(o):
        if isinstance(o, dict):
            k = o.get("kit") or o.get("piece")
            k = k if isinstance(k, str) else (k.get("id") if isinstance(k, dict) else None)
            if isinstance(k, str):
                c[k] += 1
            for v in o.values():
                walk(v)
        elif isinstance(o, list):
            for v in o:
                walk(v)
    walk(w)
    per = Counter()
    pieces = {}
    for piece, n in c.items():
        for m in mats_of.get(piece, []):
            per[m] += n
            pieces.setdefault(m, Counter())[piece] += n
    return per, pieces


def shares():
    """Latch's census (suborned.072-material-area/1): material -> {mean, opaqueOnly, byYaw, views, kind, shader, layer}; None until it exists."""
    if not AREA.exists():
        return None
    a = json.loads(AREA.read_text())
    return {m["material"]: dict(mean=m["meanShare"], opaqueOnly=m.get("meanShareOpaqueOnly"), byYaw=m.get("byYaw"), views=m.get("share"), kind=m["maps"]["kind"],
                                shader=m.get("shader"), layer=(m.get("where") or [{}])[0].get("layer")) for m in a["materials"]}


def main():
    km = kit_mats()
    table = {r["name"]: r for r in json.loads(TABLE.read_text())["materials"]} if TABLE.exists() else {}
    per, pieces = placements()
    sh = shares()
    look = json.loads(LOOK.read_text())["surfaces"]
    rows = []
    for cls, name, made, where, today, prop in FACADE:
        rows.append(dict(group="facade", material=f"facade / facade-gloss, class {cls} {name}", shader="Suborned/FacadeLit", madeIn=made, showsAt=where,
                         share=sh and {"facade (all classes)": (sh.get("facade") or {}).get("mean"), "facade-gloss (all classes)": (sh.get("facade-gloss") or {}).get("mean")}, maps="atlas (16 x 16 point-sampled swatch, colour only)",
                         today=today, proposal=prop, status="excluded" if prop.startswith("excluded") else "texture"))
    for s, e in look.items():
        if e.get("material"):
            continue
        rows.append(dict(group="look", material=s, shader="URP Lit (Look.Mat)", madeIn="Editor/World/Look.cs from look.json", showsAt="builder surface `" + s + "`",
                         share=(sh or {}).get(s), maps="none (colour only)", today=f"palette {e.get('palette')}, smoothness {e.get('smoothness')}",
                         proposal=LOOK_PROPOSAL.get(s, "facade masses: these keys colour the atlas swatches (FacadeSpec / MegaKit / ProcSkin), textured through the facade rows; a direct Look.Mat use gets the pair of its kind"),
                         status="excluded" if LOOK_PROPOSAL.get(s, "").startswith("excluded") else "texture"))
    for n, m in sorted(km.items(), key=lambda kv: (-per.get(kv[0], 0), kv[0])):
        if m["textures"] and not m["surface"]:
            continue
        kind = table.get(n, {}).get("kind")
        if m["surface"]:  # 072-F step 2: the surface pair is bound (unity_mats.surface_detail); the textures it counts are that pair and nothing else
            rows.append(dict(group="kit", material=n, shader=m["shader"], madeIn="tools/decokit/unity_mats.py (surface_detail)", showsAt=", ".join(f"{p} {c}" for p, c in pieces.get(n, Counter()).most_common(3)) or "not placed in the world export",
                             placements=per.get(n, 0), share=(sh or {}).get(n), maps="surface detail pair", kind=kind, today=f"{kind or '?'} {table.get(n, {}).get('color', '')}",
                             proposal=f"done: surface_{m['surface'][0]} pair bound (detail albedo x2 + normal); colour unchanged", status="done"))
            continue
        why = next((r for p, r in EXCLUDE if re.search(p, n)), None)
        prop = f"excluded: {why}" if why else (PAIR.get(kind) or "review: kind unknown (legacy row), take the pair of its look")
        if n.startswith("DK_Cloth_"):  # soft goods (umbrellas, cushions): a weave belongs here, which DecoKit's set lacks
            prop = "surface_limestone pair at half bump (a matte grain) until a fabric layer exists"
        if re.search(r"Window_(Film|Teal|Tower)", n):  # opaque enamel standing in for glass: a glaze mottle could read as dirty glass
            prop = "open (Glint): the surface_enamel pair at half bump on this opaque glass stand-in, or none; judge in game light (held in unity_mats.SURFACE_HOLD)"
        top = ", ".join(f"{p} {c}" for p, c in pieces.get(n, Counter()).most_common(3))
        rows.append(dict(group="kit", material=n, shader=m["shader"], madeIn="tools/decokit/unity_mats.py (mats.LIB row)" if n in table else "legacy DecoKit .mat (store)",
                         showsAt=top or "not placed in the world export", placements=per.get(n, 0), share=(sh or {}).get(n), maps="none (colour only)", kind=kind,
                         today=f"{kind or '?'} {table.get(n, {}).get('color', '')}", proposal=prop, status="excluded" if why else ("review" if prop.startswith("review") else "open" if prop.startswith("open") else "texture")))
    seen = {r["material"] for r in rows}
    for n, a in sorted((sh or {}).items()):  # anything the census saw drawn flat that the sources above missed (contact shades, the crossing glow, ...)
        if a["kind"] == "textured" or n in seen or n in ("facade", "facade-gloss"):
            continue
        why = ("crossing glow or dots (Cross Glow / signal shaders): light in the road, signal-adjacent" if "Cross" in (a["shader"] or "") or "cross" in n.lower() else None) \
            or ("contact shadow: a darkening decal, not a surface" if n.startswith("contact/") else None) or next((r for p, r in EXCLUDE if re.search(p, n)), None)
        note = {"DK_RobotEnamel_": "already detailed: the robot enamel detail pair (robot_tex.py) sits in the detail slots, which the census does not count as a colour map",
                "DK_Fountain_": "open (Glint): the testbed fountain's polished gold / silver (fountain_tex.py mask + polish normal on a 4 x 4 white albedo, the testbed's look); its UVs are the globe's 0..1, not metres, so the brushed metal pair would smear and read as brushed, not polished: Glint judges whether it needs grain at all"}
        hit = next((v for k, v in note.items() if n.startswith(k)), None)
        if hit and not why:
            rows.append(dict(group="census", material=n, shader=a["shader"], madeIn=a["layer"], showsAt=a["layer"], share=a, maps=a["kind"], today="seen by the census only",
                             proposal=hit, status="done" if hit.startswith("already") else "open" if hit.startswith("open") else "texture"))
            continue
        rows.append(dict(group="census", material=n, shader=a["shader"], madeIn=a["layer"], showsAt=a["layer"], share=a, maps=a["kind"], today="seen by the census only",
                         proposal=f"excluded: {why}" if why else "review: not in the kit tables or look.json", status="excluded" if why else "review"))
    for r in rows:  # the census's mean share as a flat number for ranking (and the per-view numbers kept in `share`)
        a = (sh or {}).get(r["material"])
        if a and r["group"] != "census":
            r["share"] = a
        r["meanShare"] = (r["share"] or {}).get("mean") if isinstance(r["share"], dict) and "mean" in (r["share"] or {}) else (
            sum(v for v in r["share"].values() if v) if isinstance(r["share"], dict) and r["group"] == "facade" else None)
    rows.sort(key=lambda r: ({"facade": 0, "look": 1, "kit": 2, "census": 3}[r["group"]], -(r["meanShare"] or 0), -(r.get("placements") or 0)))
    OUTDIR.mkdir(parents=True, exist_ok=True)
    counts = Counter((r["group"], r["status"]) for r in rows)
    doc = dict(schema="suborned.flat/1", brief="072-F", area="briefs/072/texture/area.json" if sh else None,
               note="share per view from Latch's area.json when present; until then kit rows rank by placements (pieces placed x carrying the material)",
               textureSet="tools/decokit/surface_tex.py -> out/decokit-ground/surface-tex.json", counts={f"{g}/{s}": c for (g, s), c in sorted(counts.items())}, rows=rows)
    (OUTDIR / "flat.json").write_text(json.dumps(doc, indent=1) + "\n")
    md = ["# 072-F · flat-colour surfaces", "",
          "Generated by `python3 tools/decokit/flat_census.py` (do not edit). Texture, not palette: every proposal is a colourless detail; colours stay as today.",
          f"Screen share: {'from area.json' if sh else 'pending Latch area.json (kit rows ranked by placements until then)'}.", "",
          "Counts: " + ", ".join(f"{k} {v}" for k, v in doc["counts"].items()), ""]
    fac = [r for r in rows if r["group"] == "facade" and r["status"] != "excluded"]
    top = sorted([r for r in rows if r["group"] != "facade" and r["status"] != "excluded" and r["meanShare"]]
                 + ([dict(material="facade + facade-gloss (atlas, all classes)", meanShare=fac[0]["meanShare"], proposal="surface_detail_v1 by class (FacadeLit rows below)")] if fac and fac[0]["meanShare"] else []),
                 key=lambda r: -r["meanShare"])[:12]
    if top:
        md += ["## Biggest flat area first (mean share of the 8 play views)", "", "| material | mean share | proposal |", "|---|---:|---|"]
        md += [f"| {r['material']} | {r['meanShare'] * 100:.2f} % | {r['proposal']} |" for r in top] + [""]
    for g, title in (("facade", "FacadeLit classes (swatch atlas)"), ("look", "Builder materials (look.json, colour only)"), ("kit", "Untextured DecoKit rows"), ("census", "Seen only by the census")):
        md += [f"## {title}", "", "| material | shows at play zoom | mean share | placements | today | proposal |", "|---|---|---|---:|---|---|"]
        for r in rows:
            if r["group"] != g:
                continue
            share = "pending" if sh is None else ("0" if r["meanShare"] is None else f"{r['meanShare'] * 100:.2f} %" + (" (both atlas materials, all classes)" if g == "facade" else ""))
            md.append(f"| {r['material']} | {r['showsAt']} | {share} | {r.get('placements', '')} | {r['today']} | {r['proposal']} |")
        md.append("")
    (OUTDIR / "flat.md").write_text("\n".join(md))
    print(f"[flat] {len(rows)} rows -> {OUTDIR / 'flat.json'}; " + ", ".join(f"{k} {v}" for k, v in doc["counts"].items()))


if __name__ == "__main__":
    main()
