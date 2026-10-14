#!/usr/bin/env python3
"""Brief 008 B1 delivery data (no Blender): reads out/decokit-008 (the generator's manifest, FBX, materials, textures) and writes
  catalogue.proposal.json   one KitAsset-shaped row per piece (tools/kit/catalogue.ts's fields) + budget, sight, source script, sha256
  civic-colour-roles.json   material per part, gold-as-hairline rule, pool water tint, the facade.json patch, dE2000 tables (incl. textures)
  paving-bands.json         the band design as rules (+ the numbers it was drawn from)
and the Unity .mat / texture .meta for DK_Pave_Civic (derived from the legacy DK_Pave_Flags files, new deterministic GUIDs).
Run: python3 tools/decokit/proposal008.py        Nothing under out/decokit/, contracts/ or the store is written."""

import hashlib
import json
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
from deltae import de, min_signal  # noqa: E402

OUT = REPO / "out/decokit-008"
SRC_TEX = REPO / "out/decokit-008-src/Textures/derived"
LEGACY = [Path.home() / "Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit", Path.home() / "Projects/the-suborned-store/DecoKit"]
STYLE = json.loads((REPO / "contracts/style/suborned.style.json").read_text())
SIGNALS = {k: v.upper() for k, v in STYLE["signal_colours"].items() if k != "rule"}
LOD = {"street": (0.008, 0.003), "trees": (0.08, 0.025)}
r3 = lambda n: round(n * 1000) / 1000  # noqa: E731
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731

# per piece: tags.use, scale range, sight class, source script, budget (parts list, LOD0), painting
MEAS = json.loads((OUT / "measured.json").read_text())  # verify008.py: extents read off the built meshes
SPEC = {
    "CafeBay": dict(use=["frontage:cafe-bay"], scale=[1, 1], sight="glazed", src="civic_cafe.py", budget=8000, note=lambda M: f"Ground storey of the café front (the terrace wall is the separate piece CafeTerrace, same pivot): pivot = centre of the flat on the ring line (block x -10.7, y -29), yaw 90, front -Y = block +x. Canopy reaches x {M['CafeBay']['canopy']['reach_block_x']} (soffit {M['CafeBay']['canopy']['soffit']}, top {M['CafeBay']['canopy']['top']}). Blade sign above the canopy, flush door (no steps). The generator's upper mass starts at z 4.7."),
    "CafeTerrace": dict(use=["frontage:cafe-terrace"], scale=[1, 1], sight="open", src="civic_cafe.py", budget=4500, note=lambda M: f"The café terrace wall, split from CafeBay (14 Oct) so the architecture fade does not ghost it: same pivot (block -10.7, -29), yaw 90, drops onto the bay. Limestone troughs planted with coral and cream (A) and flamingo and peach (B): coping x {M['CafeTerrace']['coping']['block_x'][0]} to {M['CafeTerrace']['coping']['block_x'][1]} (centreline x -4.3, 6.4 m out), planting top z {M['CafeTerrace']['wall_all']['top_z']}; gate {M['CafeTerrace']['gate_clear_all_materials']['width']} m clear of stone and foliage (block y {M['CafeTerrace']['gate_clear_all_materials']['block_y'][0]} to {M['CafeTerrace']['gate_clear_all_materials']['block_y'][1]}); stone ends at block ({M['CafeTerrace']['wall_stone_ends_block'][0][0]}, {M['CafeTerrace']['wall_stone_ends_block'][0][1]}) and ({M['CafeTerrace']['wall_stone_ends_block'][1][0]}, {M['CafeTerrace']['wall_stone_ends_block'][1][1]})."),
    "BedTuft_A": dict(use=["bed:tuft"], scale=[1, 1], sight="open", src="civic_street.py", budget=200, note="Civic bed planting unit, variant A (coral + cream): a clipped hedge cushion 0.62 m across with three blooms, base at z 0. Instanced along a bed's inset ring every 0.55 m (see civic-beds.json); matches PlanterRun_*_A."),
    "BedTuft_B": dict(use=["bed:tuft"], scale=[1, 1], sight="open", src="civic_street.py", budget=200, note="Civic bed planting unit, variant B (flamingo + peach), see BedTuft_A. Matches PlanterRun_*_B."),
    "BedTuft_C": dict(use=["bed:tuft"], scale=[1, 1], sight="open", src="civic_street.py", budget=200, note="Civic bed planting unit, variant C (cream + peach, taller, fewer blooms), see BedTuft_A. Matches PlanterRun_*_C."),
    "RecordsEntrance": dict(use=["entrance:records-public"], scale=[1, 1], sight="glazed", src="civic_records.py", budget=14000, note=lambda M: f"Tower-public north face: pivot = face centre at the pavement (block 52.5, -30), yaw -90, front -Y = block -x. Measured in the block frame: landing x {M['RecordsEntrance']['landing']['xy'][0]} to {M['RecordsEntrance']['landing']['xy'][1]}, y {M['RecordsEntrance']['landing']['xy'][2]} to {M['RecordsEntrance']['landing']['xy'][3]}, top {M['RecordsEntrance']['landing']['top_z']}; steps x {M['RecordsEntrance']['steps']['xy'][0]} to {M['RecordsEntrance']['steps']['xy'][1]}, {M['RecordsEntrance']['steps']['treads']} treads of {M['RecordsEntrance']['steps']['going'][0]}, {M['RecordsEntrance']['steps']['risers']} risers of {M['RecordsEntrance']['steps']['rise']}; cheeks y +-2.56 to x {M['RecordsEntrance']['cheeks']['xy'][0]}; pier plinths x {M['RecordsEntrance']['pier_plinths']['xy'][0]} to {M['RecordsEntrance']['pier_plinths']['xy'][1]}, y {M['RecordsEntrance']['pier_plinths']['xy'][2]} to -33.02 and -26.98 to {M['RecordsEntrance']['pier_plinths']['xy'][3]}; true width {M['RecordsEntrance']['piece']['width']} m (nothing past the plinths), {-M['RecordsEntrance']['piece']['front']} m out, {M['RecordsEntrance']['piece']['height']} m tall with the blades; marquee x {M['RecordsEntrance']['marquee']['xy'][0]} to {M['RecordsEntrance']['marquee']['xy'][1]}, y {M['RecordsEntrance']['marquee']['xy'][2]} to {M['RecordsEntrance']['marquee']['xy'][3]}, z {M['RecordsEntrance']['marquee']['z'][0]} to {M['RecordsEntrance']['marquee']['z'][1]}."),
    "RecordsEntrance_Official": dict(use=["entrance:records-official"], scale=[1, 1], sight="glazed", src="civic_records.py", budget=6000, note=lambda M: f"Flush lean version for tower-official (face x 75.5, door axis y -30): pivot = face centre at the pavement (block 75.5, -30), yaw 90, front -Y = block +x. No landing, steps, cheeks or rails; sill z {M['RecordsEntrance_Official']['door']['sill_z']}, door plane {M['RecordsEntrance_Official']['door']['plane_off_face']} m off the face; canopy = the block's office-boarding-pavilion: x 75.5 (the face; embedded {M['RecordsEntrance_Official']['canopy']['embedded_behind_face']} m behind it) to {M['RecordsEntrance_Official']['piece']['xy'][1]}, y {M['RecordsEntrance_Official']['piece']['xy'][2]} to {M['RecordsEntrance_Official']['piece']['xy'][3]}, soffit {M['RecordsEntrance_Official']['canopy']['z'][0]}, top {M['RecordsEntrance_Official']['canopy']['z'][1]}, on the face, no posts; pilasters {M['RecordsEntrance_Official']['pilasters']['projection']} m proud (x 75.5 to {round(75.5 + M['RecordsEntrance_Official']['pilasters']['projection'], 2)}, embedded {M['RecordsEntrance_Official']['pilasters']['embedded_behind_face']} m; y {M['RecordsEntrance_Official']['pilasters']['y_runs'][0][0]} to {M['RecordsEntrance_Official']['pilasters']['y_runs'][0][1]} and {M['RecordsEntrance_Official']['pilasters']['y_runs'][1][0]} to {M['RecordsEntrance_Official']['pilasters']['y_runs'][1][1]}); nothing under the canopy projects more than {M['RecordsEntrance_Official']['max_projection_under_canopy']} m; width {M['RecordsEntrance_Official']['piece']['width']} m."),
    "Parasol_Stripe": dict(use=["parasol"], scale=[0.9, 1.1], sight="open", src="civic_street.py", budget=850, note="Pole stands from the ground through the table: pivot = pole = table centre of CafeSet_Tulip_Play. 2.6 m across, 2.07 m clear."),
    "CafeSet_Tulip_Play": dict(use=["cafe-set"], scale=[0.5, 1.2], sight="open", src="civic_street.py", budget=1000, note="Play-zoom CafeSet_Tulip (3,984 tris): same table, three chairs, cushions in civic enamels; bud vase dropped."),
    "PlazaSphereLamp_Play": dict(use=["lamp"], scale=[0.7, 1.6], sight="open", src="civic_street.py", budget=2500, note="Play-zoom PlazaSphereLamp (7,932 tris): same 4.6 m standard and pole-origin pivot; seafoam + coral pennants (banner pair) on the two arms."),
    "Cypress_Play": dict(use=["tree"], scale=[0.6, 2.2], sight="open", src="civic_street.py", budget=1500, note="Play-zoom Cypress (5,004 tris): same profile, single-quad cards. Foliage is alpha-cutout (textured)."),
    "PottedTopiary_Play": dict(use=["topiary", "potted"], scale=[0.8, 1.3], sight="open", src="civic_street.py", budget=600, note=lambda m: f"Cone topiary in a cream limestone pot, {m['PottedTopiary_Play']['height']} m tall, {m['PottedTopiary_Play']['width']} m across (measured), three clipped tiers in civic green with a tip, no textures (gp-entrance and mission-block: pairs at the doors). Pivot = pot centre, on the ground."),
    "BollardSphere": dict(use=["bollard"], scale=[0.8, 1.4], sight="open", src="civic_street.py", budget=600, note="Chrome post 1.05 m, seafoam band, sphere top."),
    "BoardingPavilion_Square_v2": dict(use=["shelter:square"], scale=[1, 1], sight="open", src="civic_street.py", budget=2500, note="Locked footprint 8.4 x 2.14, roof top 2.587, long along X. Two chrome posts at local y +0.68, x +-3.22; the block's two PavilionPier_Square piers are the other two supports (local y -1.07..0.05). Bounding box is 2.59 m tall (the adopted v1 box is the slab only)."),
    "InlayBand_06_2": dict(use=["inlay:band"], scale=[1, 1], sight="open", src="civic_paving.py", budget=100, note="2 m module, 0.6 wide, 4 mm proud, flush with the slab."),
    "InlayBand_12_2": dict(use=["inlay:band"], scale=[1, 1], sight="open", src="civic_paving.py", budget=100, note="2 m module, 1.2 wide."),
    "InlayArc_R2_W06": dict(use=["inlay:arc"], scale=[1, 1], sight="open", src="civic_paving.py", budget=150, note="Quarter ring, R 2.0, pivot = arc centre, sweeps +X to +Y."),
    "InlayArc_R4_W06": dict(use=["inlay:arc"], scale=[1, 1], sight="open", src="civic_paving.py", budget=150, note="Quarter ring, R 4.3 (fountain inner ring)."),
    "InlayArc_R9_W12": dict(use=["inlay:arc"], scale=[1, 1], sight="open", src="civic_paving.py", budget=200, note="Quarter ring, R 8.6, 1.2 wide (fountain outer ring)."),
}
for _k in ("2", "4", "Corner", "End"):
    for _v in "ABC":
        SPEC[f"PlanterRun_{_k}_{_v}"] = dict(use=["planter-run", f"planter-run:{_k.lower()}", f"planting:{_v}"], scale=[1, 1], sight="open", src="civic_street.py (planting: civic_common.py)", budget=800,
                                              note={"2": "2 m module along X, centred.", "4": "4 m module along X, centred.", "Corner": "90-degree corner, centreline R 1.0, legs 1.0 m, footprint-centred.", "End": "1.0 m run closed by a half-round end at +X."}[_k]
                                              + f" Planting {_v}: " + {"A": "coral and cream blooms", "B": "flamingo and peach blooms", "C": "cream and peach blooms, taller clipped green"}[_v] + ". Trough 0.62 wide, coping 0.66 high, planting to about 1.0.")


def hexof(rgb):
    return "#%02X%02X%02X" % tuple(round(c * 255) for c in rgb)


def lin(c):
    c = np.asarray(c, dtype=np.float64)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def unlin(c):
    c = np.clip(np.asarray(c), 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def deltas(h):
    return {k: round(de(h, v), 1) for k, v in SIGNALS.items()}


def texture_stats(file, base_hex):
    """Unlit average of a texture (linear-light mean over opaque texels, times the material's base colour as URP does) and the
    share of texels within dE2000 20 of a signal colour (a 96 x 96 sample)."""
    p = SRC_TEX / file
    if not p.exists():
        p = OUT / "Textures" / file
    im = Image.open(p).convert("RGBA")
    a = np.asarray(im.resize((96, 96), Image.BOX), dtype=np.float64) / 255
    w = (a[..., 3] > 0.5) if a[..., 3].min() < 0.99 else np.ones(a.shape[:2], bool)
    rgb = a[..., :3][w]
    base = np.array([int(base_hex[i:i + 2], 16) / 255 for i in (1, 3, 5)]) if base_hex.upper() != "#FFFFFF" else np.ones(3)
    mean = unlin(lin(rgb).mean(0) * lin(base))
    h = hexof(mean)
    sample = rgb[:: max(1, len(rgb) // 1500)]
    near = sum(1 for px in sample if min(de(hexof(unlin(lin(px) * lin(base))), v) for v in SIGNALS.values()) < 20) / len(sample)
    return h, round(100 * near, 1)


def legacy_file(rel):
    return next((r / rel for r in LEGACY if (r / rel).exists()), None)


def mk_pave_unity():
    """DK_Pave_Civic.mat + the three texture .meta files, from the legacy DK_Pave_Flags set with new deterministic GUIDs."""
    mat, mmeta = legacy_file("Materials/DK_Pave_Flags.mat"), legacy_file("Materials/DK_Pave_Flags.mat.meta")
    guid = lambda n: hashlib.md5(f"suborned.decokit.tex/{n}".encode()).hexdigest()  # noqa: E731
    text = mat.read_text()
    old = {"": "pave_flags_v1.png", "_normal": "pave_flags_v1_normal.png", "_mask": "pave_flags_v1_mask.png"}
    (OUT / "Textures").mkdir(parents=True, exist_ok=True)
    for sfx, name in old.items():
        meta = legacy_file(f"Textures/{name}.meta").read_text()
        og = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
        new = name.replace("pave_flags", "pave_civic")
        ng = guid(new)
        text = text.replace(og, ng)
        (OUT / "Textures" / f"{new}.meta").write_text(meta.replace(og, ng))
    text = text.replace("m_Name: DK_Pave_Flags", "m_Name: DK_Pave_Civic")
    (OUT / "Materials").mkdir(parents=True, exist_ok=True)
    (OUT / "Materials/DK_Pave_Civic.mat").write_text(text)
    g = hashlib.md5(b"suborned.decokit.mat/DK_Pave_Civic").hexdigest()
    (OUT / "Materials/DK_Pave_Civic.mat.meta").write_text(re.sub(r"guid: [0-9a-f]{32}", f"guid: {g}", mmeta.read_text()))


def mk_banner_unity():
    """DK_Banner_Comet_Seafoam.mat + the texture .meta, from the legacy DK_Banner_Comet set with new deterministic GUIDs (the generator writes no .mat for textured materials)."""
    mat, mmeta = legacy_file("Materials/DK_Banner_Comet.mat"), legacy_file("Materials/DK_Banner_Comet.mat.meta")
    tmeta = legacy_file("Textures/banner_comet_teal_v1.png.meta")
    text, meta = mat.read_text(), tmeta.read_text()
    og = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
    ng = hashlib.md5(b"suborned.decokit.tex/banner_comet_seafoam_v1.png").hexdigest()
    (OUT / "Textures").mkdir(parents=True, exist_ok=True)
    (OUT / "Textures/banner_comet_seafoam_v1.png.meta").write_text(meta.replace(og, ng))
    text = text.replace(og, ng).replace("m_Name: DK_Banner_Comet", "m_Name: DK_Banner_Comet_Seafoam")
    (OUT / "Materials").mkdir(parents=True, exist_ok=True)
    (OUT / "Materials/DK_Banner_Comet_Seafoam.mat").write_text(text)
    g = hashlib.md5(b"suborned.decokit.mat/DK_Banner_Comet_Seafoam").hexdigest()
    (OUT / "Materials/DK_Banner_Comet_Seafoam.mat.meta").write_text(re.sub(r"guid: [0-9a-f]{32}", f"guid: {g}", mmeta.read_text()))


def asset_row(a, mats_by):
    spec = SPEC[a["name"]]
    note = spec["note"](MEAS) if callable(spec["note"]) else spec["note"]
    cat = a["category"]
    lc = "trees" if cat == "Plants" else "street"
    b = a["bounds"]
    c = [r3((b[0] + b[1]) / 2), r3((b[2] + b[3]) / 2)]
    piv = {"pivot": "origin-ground", "pivotOffset": c} if math.hypot(*c) > 0.05 else {"pivot": "footprint-centre-ground"}
    files = []

    def add(role, rel, gen):
        """gen: under out/decokit-008 (root 'generated'); else the legacy DecoKit root (root omitted), as kit.json does."""
        p = (OUT / rel) if gen else legacy_file(rel)
        if p and p.exists():
            files.append({**({"root": "generated"} if gen else {}), "role": role, "path": rel, "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size})
        else:
            files.append({"role": role, "path": rel, "missing": True})
    add("fbx", a["fbx"], True)
    for m in a["materials"]:
        r = mats_by[m]
        g = not legacy_file(f"Materials/{m}.mat")  # the generator writes only what the DecoKit root lacks
        add("material", f"Materials/{m}.mat", g)
        add("material-meta", f"Materials/{m}.mat.meta", g)
        if r["texture"]:
            for t in (r["texture"], r["texture"].replace(".png", "_normal.png"), r["texture"].replace(".png", "_mask.png")):
                g = not legacy_file(f"Textures/{t}")
                if g and not (OUT / "Textures" / t).exists():
                    continue
                add("texture", f"Textures/{t}", g)
                add("texture-meta", f"Textures/{t}.meta", g)
    lods = [{"file": l["file"], "tris": l["tris"], "screen": LOD[lc][i]} for i, l in enumerate(a.get("lods", []))]
    for l in lods:
        p = OUT / l["file"]
        files.append({"root": "generated", "role": "lod", "path": l["file"], "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size})
    return {"id": a["name"], "category": cat, "size": a["size"], **piv, "front": "+Z", "tris": a["tris"], "materials": a["materials"],
            **({"lods": lods} if lods else {}), "lodCategory": lc, "files": files,
            "tags": {"use": spec["use"], "scale": spec["scale"], "squash": 1, "wave": 2, "sight": spec["sight"]},
            "status": "candidate", "source": "generated", "sourceScript": f"tools/decokit/{spec['src']}",
            "budget": {"tris": spec["budget"], "ok": a["tris"] <= spec["budget"]}, "note": note}


def main():
    man = json.loads((OUT / "decokit_manifest.json").read_text())["assets"]
    table = json.loads((OUT / "decokit_materials.json").read_text())["materials"]
    mats_by = {m["name"]: m for m in table}
    mk_pave_unity()
    mk_banner_unity()
    rows = [asset_row(a, mats_by) for a in man if a["name"] in SPEC]
    (OUT / "catalogue.proposal.json").write_text(json.dumps(dict(
        schema="suborned.kit/1 (proposal: KitAsset rows, status candidate)", brief="008 B1", generator="tools/decokit (Blender 5.2.2), --out out/decokit-008",
        roots={"generated": "out/decokit-008 (this proposal's generator output; adopting = same relative paths under out/decokit)", "(omitted)": "the DecoKit root (tools/kit/store.ts KIT_ROOTS)"},
        note="Rows follow contracts/world/kit.schema.ts (KitAsset) plus sight (open | glazed), sourceScript, budget. tools/kit reads one generated root (out/decokit): adopting these needs either the FBX, LOD, material and texture files copied into that root or a second generated root (B3, Splice). kit.json is not touched.",
        extraMaterials=[dict(name="DK_Pool_Civic", kind="enamel", hex="#3A9AA6", appliesTo="civic/atom-fountain: map AtomFountain's DK_Water slot to this (the pool reads white otherwise)",
                             files=[{"root": "generated", "role": role, "path": f"Materials/DK_Pool_Civic.mat{sfx}", "sha256": sha((OUT / f"Materials/DK_Pool_Civic.mat{sfx}").read_bytes()), "bytes": (OUT / f"Materials/DK_Pool_Civic.mat{sfx}").stat().st_size}
                                    for role, sfx in (("material", ""), ("material-meta", ".meta"))])],
        assets=rows), indent=1) + "\n")
    print(f"catalogue.proposal.json: {len(rows)} assets, {sum(1 for r in rows if not r['budget']['ok'])} over budget")
    roles_and_bands(man, rows, mats_by)


# ----------------------------------------------------------------------------------------------------- roles + bands

def roles_and_bands(man, rows, mats_by):
    used = sorted({m for r in rows for m in r["materials"]} | {"DK_Pave_Civic", "DK_Pool_Civic"})  # the slab and the pool water have no piece of their own: a plot surface and a material slot swap
    table = []
    for m in used:
        r = mats_by[m]
        base = hexof(r["color"])
        if r["texture"]:
            h, near = texture_stats(r["texture"], base)
            table.append(dict(material=m, kind=r["kind"], textured=r["texture"], unlit_mean=h, dE=deltas(h), min=min(deltas(h).values()), texelsUnder20pct=near, used_by=[x["id"] for x in rows if m in x["materials"]] or ["plot surfaces (paving)"]))
        else:
            d = deltas(base)
            table.append(dict(material=m, kind=r["kind"], textured=None, unlit=base, dE=d, min=min(d.values()), emission=r["emission"] or None, used_by=[x["id"] for x in rows if m in x["materials"]] or ["civic/atom-fountain (water slot, mapped from DK_Water)"]))
    per_piece = {x["id"]: sorted({(t["material"], t["min"]) for t in table if t["material"] in x["materials"]}, key=lambda q: q[1])[:3] for x in rows}
    def dE(h):
        return deltas(h)
    roles = {
        "schema": "suborned.civic-colour-roles/1", "brief": "008 B1, corrected 14 Oct (first Unity build)", "date": "2026-10-14",
        "decision": "Sky, 14 Oct: seafoam and deep teal are the kit enamels; coral, flamingo and peach (contract accents) are for banners and flowers; gold is hairline, frame and bead only; the full-height gold fins come off. 14 Oct (first Unity build, against the paintings): ivory stone fields, seafoam bands, deep teal an accent of 0.3 m or less never a field, fascia, cap or band; the hairline gold is the painted brass #99784D, not the world gold #D0C848 (yellow-green in Unity).",
        "roles": {
            "stone": dict(materials=["DK_Limestone", "DK_Enamel_Ivory", "DK_Pave_Civic (slabs)"], hex=["#E7DECA", "#EFE8D6", "#E4DACA mean"], facade_swatch={"civic_w": "#EEE9DA"}, role="walls, piers, planters, kerbs, steps, paving, soffits, the facade fields of the café and the Office",
                         painting="records-office and inez-cutaway: ivory stone fields (shade reads tan in the paintings too: #AE9881 on gp-entrance's door pier); gp-entrance kerb face #E3CEB1 in sun", use="walls, piers, planters, steps, paving, soffits"),
            "band": dict(materials=["DK_Enamel_Seafoam", "DK_Enamel_Mint"], hex=["#72B5A7", "#A8D3BE"], facade_swatch={"e_seafoam": "#72B5A7", "e_seafoam_pale": "#9BCFC2"},
                         role="canopy and marquee fascias, window surround, pier and pilaster caps, bands and fins on the facades, plinth bands, banner cloth, paving inlay (DK_Inlay_Seafoam, same hex)",
                         painting="records-office: the seafoam bands and fins of the tower; inez-cutaway: the café canopy fascia and the wrap-round band; gp-entrance: marquee top #A4B3A1 and fascia #778D84 in sun and shade", use="canopy and marquee fascias, bands, shelter fascia, bollard band, banners (cloth), paving inlay (DK_Inlay_Seafoam, same hex)"),
            "deep": dict(materials=["DK_Enamel_Teal"], hex=["#1F6C67"], rule="an accent of 0.3 m or less: the café blade-sign body, the lamp sleeve, chair cushions, an 8 cm reveal round the Office window. Never a field, fin, fascia, cap or band (Unity read them as dark panels and a dark marquee)",
                         painting="the paintings use it only as thin edge lines and the sign blades", use="sign body, cushions, lamp sleeve, window reveal"),
            "metal": dict(materials=["DK_Chrome", "DK_Steel"], role="posts, balls, rails, globe, handles, blades", painting="all three paintings: chrome balls, rails and the globe", use="posts, balls, rails, globe, handles, blades"),
            "hairline": dict(materials=["DK_Brass_Civic"], hex=["#99784D"], facade_swatch={"gold": "#99784D", "paletteOverride.gold_trim": "#99784D"}, old={"material": "DK_Accent_Gold", "hex": "#D0C848"},
                             rule="only as a bead, frame, mullion or line of 3 to 7 cm; never a panel, fin, fascia or band; at most 6% of any elevation's area. #99784D is dE2000 21.3 from the guard colour: a gold of this hue that is any lighter than L 52 falls inside the gate, so this dark brass is the gold",
                             painting="gp-entrance: the door posts, #99784D in the shade of the marquee (lit brass is lighter; the unlit palette gate reads this value)", use="glazing frames, door frames, fascia lines, marquee edge, banner rods, window grid"),
            "banner": dict(materials=["DK_Banner_Comet_Seafoam", "DK_Cloth_Seafoam", "DK_Cloth_CoralSoft", "DK_Cloth_FlamingoSoft", "DK_Cloth_PeachSoft"], hex=["#72B5A7", "#72B5A7", "#FC9C84", "#E8609A", "#FCC4AC"],
                           old={"material": "DK_Banner_Comet + DK_Fabric_Banner", "hex": "#048A7C (texture field)"},
                           rule="entrance banners: seafoam field, ivory comet (banner_comet_seafoam_v1.png, derive008.py); lamp pennants: seafoam and the contract coral. Never a red-orange: DK_Fabric_Coral #E07560 (dE 12.0 from the cones colour) and DK_Blind_Coral #E58A6E (17.3) stay off this block; the contract accents are 21.7 or more",
                           painting="gp-entrance: the pier banners, ivory comet on a teal field (#406464 as painted; seafoam here because the shaded Unity faces multiply it by about 0.18 in linear light)"),
            "flowers": dict(materials=["DK_Bloom_Coral", "DK_Bloom_Flamingo", "DK_Bloom_Peach", "DK_Bloom_Cream"], hex=["#FC9C84", "#E8609A", "#FCC4AC", "#F6EBCB"],
                            rule="planting variants A coral+cream, B flamingo+peach, C cream+peach; neighbours differ; the beds use the same three (BedTuft_A/B/C)",
                            painting="gp-entrance and mission-block: coral, flamingo and peach flowers in the beds"),
            "leaf": dict(materials=["DK_Leaf_Civic", "DK_Leaf_CivicDark"], hex=["#6A9B5C", "#46794C"], role="cushions, tufts, hedge fill of the beds", painting="gp-entrance: clipped hedges and conifers"),
            "bed": dict(materials=["DK_Limestone", "DK_Leaf_CivicDark", "DK_Leaf_Civic"], hex=["#E7DECA", "#46794C", "#6A9B5C"], rule="recipe in civic-beds.json: a limestone kerb, a clipped hedge fill, BedTuft_A/B/C on top; replaces the plain `park` colour (#A6E29C) on these masses",
                        painting="gp-entrance: low cream kerb, clipped hedge, flowers"),
            "pool": dict(materials=["DK_Pool_Civic"], hex=["#3A9AA6"], rule="opaque enamel water (smoothness 0.8, no alpha) so the sky cannot wash it white; Latch maps AtomFountain's DK_Water slot to it for civic/atom-fountain only",
                         painting="gameplay-plan: the pool is a saturated turquoise under the spray (lit median #4C6C7C with sky and palm reflections)", dE=dE("#3A9AA6")),
            "glass": dict(materials=["DK_GlassDark"], hex=["#35585A"], rule="clear dark teal, no blue curtain wall (facade.json glass_clear)", painting="records-office: dark glazing", use="glass"),
            "lamp": dict(materials=["DK_Lamp_Deco"], hex=["#FFF1C4"], rule="DK_Lamp #FFE2A6 is dE 19.3 from the guard colour: the civic pieces use Lamp_Deco (24.6)"),
        },
        "parts": {
            "CafeBay": dict(stone="piers, coping", band="canopy fascia, base course (seafoam, was deep teal)", deep="blade-sign body", hairline="mullions, transom, sill, head, door frames, fascia lines (brass)", glass="film over a charcoal sheet", lamp="soffit downlights"),
            "CafeTerrace": dict(stone="trough (DK_Limestone)", leaf="cushions and hedge band", flowers="variants A, B"),
            "RecordsEntrance": dict(stone="piers, steps, cheeks, globe plinth", band="marquee fascia and top, window surround, pier cap (seafoam), pier band and marquee rim (mint), banner cloth", deep="an 8 cm reveal round the window", hairline="door frames, window grid, marquee lines, banner rods (brass)", metal="globe, stand, rails, blades, finials", lamp="marquee lanterns"),
            "RecordsEntrance_Official": dict(stone="pilasters, canopy soffit", band="canopy fascia and top, pilaster cap, banner cloth", hairline="door frames, canopy lines, banner rods (brass)", lamp="canopy lanterns"),
            "BoardingPavilion_Square_v2": dict(stone="roof (Enamel_Ivory)", band="fascia", hairline="fascia bead (brass)", metal="posts", lamp="underside strip"),
            "PlazaSphereLamp_Play": dict(deep="sleeve", metal="plinth, pole, sphere, arms", banner="seafoam pennant + contract-coral pennant"),
            "Parasol_Stripe": dict(band="alternate panels", stone="alternate panels (DK_Cloth_Cream #EFE8D6)", metal="pole, ribs, finial"),
            "BollardSphere": dict(metal="post, sphere", band="band"),
            "PlanterRun_*": dict(stone="trough (DK_Limestone)", leaf="cushions and hedge band", flowers="by variant"),
            "BedTuft_*": dict(leaf="hedge cushion", flowers="by variant"),
            "Inlay*": dict(band="DK_Inlay_Seafoam (glazed tile, #72B5A7), 4 mm proud"),
            "paving": dict(stone="DK_Pave_Civic slabs on the plot surfaces of the square, the café terrace and the office apron (world-space UVs, 4 m tile, like DK_Asphalt_Fine)"),
        },
        "pool_water": {
            "material": "DK_Pool_Civic", "surface": "#3A9AA6", "alpha": 1.0, "smoothness": 0.8, "kind": "opaque enamel", "coping": "DK_Limestone with DK_Enamel_Seafoam lip",
            "dE": deltas("#3A9AA6"), "basis": "infer: harbour.water #3d8d93 (lagoon) pushed toward accent_turquoise #1cb9c6; the 26 Sep paintings' pool is a saturated turquoise, but its lit median reads #4C6C7C with sky and palm reflections, which is not a palette value",
            "apply": "AtomFountain's water is one flat disc (R 3.0, z 0.34) in DK_Water, a glass of alpha 0.3 over the pale basin, which the sky turns white. Map that material slot to the kit material DK_Pool_Civic (out/decokit-008/Materials/DK_Pool_Civic.mat) on civic/atom-fountain only; the fountain's own files are unchanged (DK_Water stays for the other three pieces that use it). Not a facade key.",
        },
        "facade_patch": {
            "file": "unity/Assets/Suborned/Editor/World/facade.json (Latch applies; not touched here). Replaces the 14 Oct patch.",
            "why": "First Unity build: the civic masses took the `records` area (ivory 7 / cream 3 families, a seafoam / deep teal / apricot enamel pair) and the ivory family's plinth swatch ivory_p = #D0C848. That gave salmon fins (apricot A), deep-teal bands and fins (deep teal), thick yellow-green base bands (ivory_p) and tan walls (cream_w). The civic masses now get their own area.",
            "swatches_add": {"civic_w": "#EEE9DA", "e_seafoam_pale": "#9BCFC2"},
            "swatches_change": {"ivory_p": {"now": "#d0c848", "to": "#72b5a7"}, "gold": {"now": "#d0c848", "to": "#99784d"}},
            "families_add": {"civic": ["civic_w", "e_seafoam", "roof_e"]},
            "areas_add": {"civic": {"families": {"civic": 1}, "enamel": ["e_seafoam", "e_seafoam_pale"]}},
            "areas.records.enamel": {"now": ["e_seafoam", "e_deepteal", "e_seafoam", "e_apricot"], "to": ["e_seafoam", "e_seafoam_pale"], "why": "the Records district around the block: no deep-teal fins or bands, no apricot (coral, flamingo and peach are for banners and flowers)"},
            "paletteOverride.gold_trim": {"now": "#d0c848", "to": "#99784d", "district_wide": "75 sign boards use the gold_trim palette key"},
            "optional_district_wide": {"e_sun": {"now": "#d0c848", "to": "#f4e27a", "why": "e_sun is the sunshine enamel, not gold; the same hex as the old gold gave gold-looking roof spheres and boxes (roof trim takes the host's enamel pair, `roofItems.trim`: @A / @B) and yellow-green fins; #F4E27A is dE 21.1 from the guard"},
                                     "sand_p": {"now": "#d0c848", "to": "#f4e27a"}, "t_hotel": {"now": "#d0c848", "to": "#f4e27a"}},
            "gate": "every swatch above is at least dE2000 20 from cones #E03A2F, guard #F0A020 and rivals #8A4FD0 (facade_check in out/decokit-008/facade-patch.check.txt)",
        },
        "dE2000_unlit": {"signals": SIGNALS, "rule": ">= 20 from every signal colour, on unlit palette values; textured materials are measured as the linear-light mean of their opaque texels times the base colour (C5)", "materials": table,
                         "below_20": [t["material"] for t in table if t["min"] < 20], "lowest_three_per_piece": {k: v for k, v in per_piece.items()}},
    }
    (OUT / "civic-colour-roles.json").write_text(json.dumps(roles, indent=1) + "\n")
    print("civic-colour-roles.json: below 20:", roles["dE2000_unlit"]["below_20"])
    bands = json.loads((OUT / "paving-bands.json").read_text()) if (OUT / "paving-bands.json").exists() else None
    print("paving-bands.json:", "present" if bands else "missing (write it with tools/decokit/bands008.py)")


if __name__ == "__main__":
    main()
