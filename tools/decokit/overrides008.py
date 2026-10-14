#!/usr/bin/env python3
"""Brief 008 colour round, last part (14 Oct): the civic-scoped colour override table for the district pieces the block places, the street items it cannot reach,
the landing-patch question, and the district facade list. No Blender. Writes out/decokit-008/civic-overrides.json (a proposal: contracts/ is Splice's, applied after the
light pass) and the Unity files of the two materials it needs that no piece draws (DK_Flower_CoralSoft, DK_Flower_Flamingo: the district's coral flower card recoloured by
derive008.py).

Brass (Sky, 14 Oct: "I prefer brass to gold"; street level and facades, the skyline crowns keep their gold) is NOT in this table: DK_Gold, DK_Gold17 and DK_Accent_Gold turned brass
in the kit itself (mats.py, crown.py, brass008.py; out/decokit-brass). What is left here is what is not gold: mustard, turquoise, near-black, amber, coral, the pale lamp.

The mechanism is civic-look.json `materials` (mass id -> {piece slot -> material}); the export carries it as Mass.materials and only civic[] masses have it.
Everything here is read: the export (out/world), kit.json (slots per piece, file hashes), civic-look.json (palette, current overrides), the Unity kit's .mat files (the colour each
slot draws now; out/decokit-brass first for the brass family), the looks/harbour.json palette, facade.json, the style contract (signal colours). Checks, as the plan checker does them:
  CD-2  every slot named is one the piece has; every target is a material Unity already has or this script builds;
  CD-4  every target is >= 20 dE2000 from the cones, guard and rivals colours (unlit; a texture as the linear-light mean of its opaque texels).
Run: python3 tools/decokit/overrides008.py        Deterministic. Exit 1 on any failed check."""

import hashlib
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from deltae import de  # noqa: E402
from proposal008 import LEGACY, OUT, REPO, SIGNALS, SRC_TEX, hexof, legacy_file, lin, unlin  # noqa: E402

KIT = REPO / "unity/Assets/Suborned/Kit"
FACE = {"DK_Face_Amber": "#F28A1E", "DK_Face_Teal": "#3FC9A0"}  # robot_parts.py: the LED colours of the face shader (no base colour in the .mat)
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731
dsig = lambda h: {k: round(de(h, v), 1) for k, v in SIGNALS.items()}  # noqa: E731

# piece -> slot -> (target, painting, why). A list of targets is cycled over the piece's masses in order (neighbours differ).
PAL = "gameplay-plan"
RULES = {
    "PlanterArc": {
        "DK_Faience_Teal": ("DK_Enamel_Seafoam", PAL + " (fountain planters)", "the bright turquoise band (#36B0A8) becomes the kit seafoam band the planter runs and shelter fascias already use; the paintings have cream stone and seafoam, no turquoise"),
        "DK_Leaf_CoreShrub": ("DK_Leaf_CivicDark", PAL + ", gp-cafe", "the solid core of each clipped ball is #222D17, which reads black behind the olive leaf cards; the bed hedge green #46794C shows a clipped boxwood ball"),
        "DK_Flower_Coral": (["DK_Flower_CoralSoft", "DK_Flower_Flamingo", "DK_Flower_CoralSoft", "DK_Flower_Flamingo"], PAL + ", gp-entrance (flowers)", "the card's red-orange blooms sit 19.0 dE from the cones colour; coral #FC9C84 and flamingo #E8609A are the contract's flower colours; alternate arcs differ"),
        "DK_Accent_Gold": ("DK_Brass_Civic", "gp-entrance (door posts)", "REDUNDANT once Splice re-adopts: the contract holds it today (the bead is brass, not #D0C848) and DK_Accent_Gold is itself brass (#99784D) after this change; keep it until then"),
    },
    "PottedPalm_Small": {
        "DK_Enamel_Mustard": ("DK_Enamel_Seafoam", "gp-cafe (palms in the terrace)", "the cone pot is the big mustard (#967509) shape beside the café; seafoam is the enamel the café fins and umbrellas use"),
        "DK_Enamel_Coral": ("DK_Enamel_Ivory", "gp-cafe", "the pot band #E07560 is 12.0 dE from the cones colour; cream between the seafoam cone and the ivory rim"),
    },
    "UtilityCabinet": {
        "DK_Enamel_Mustard": ("DK_Enamel_Seafoam", "none (no cabinet is painted)", "the centre strip down the doors; seafoam keeps it in the block's enamel set and clear of the guard colour (20.8 now)"),
    },
    "RobotButler": {
        "DK_Signal_Amber": ("DK_Glow_Seafoam", PAL + " (robot, seafoam glow)", "the eye and status lamps: amber #FFB21E is 5.2 dE from the guard colour, a robot must not read as a guard"),
        "DK_Face_Amber": ("DK_Face_Teal", PAL + " (robot, seafoam glow)", "the face display's glow, same reason; DK_Face_Teal is the kit's own seafoam LED face (#3FC9A0), already built for RobotWaiter and RobotNanny"),
        "DK_Enamel_Coral": ("DK_Enamel_Coral17", PAL, "the pocket square: #E07560 is 12.0 dE from the cones colour; Coral17 (flamingo pink) is 23.7"),
    },
    "AtomFountain": {
        "DK_Enamel_Turquoise": ("DK_Enamel_Seafoam", PAL + " (basin rim)", "the bright turquoise ring on the basin rim beside the teal pool; the painted rim is stone and seafoam"),
        "DK_Lamp": ("DK_Lamp_Deco", "gp-entrance (lanterns)", "the top lamp: #FFE2A6 is 19.3 dE from the guard colour; Lamp_Deco is 24.6 and is what the civic pieces already draw"),
        "DK_Water": ("DK_Pool_Civic", PAL + " (pool)", "kept from the contract"),
    },
    "PavilionPier_Square": {
        "DK_Enamel_Turquoise": ("DK_Enamel_Seafoam", "gp-shelters (seafoam canopy edge)", "the pier band is turquoise under the v2 pavilion, whose fascia is seafoam"),
    },
}
FLOWER_CARDS = {"DK_Flower_CoralSoft": "flower_cluster_coralsoft_v1_alpha.png", "DK_Flower_Flamingo": "flower_cluster_flamingo_v1_alpha.png"}


def mat_hex(name, cache={}):
    """(unlit hex, note) of a Unity kit material: base colour, or the linear-light mean of its texture's opaque texels (times the base colour)."""
    if name in cache:
        return cache[name]
    if name in FACE:
        cache[name] = (FACE[name], "face glow")
        return cache[name]
    if name in FLOWER_CARDS:
        cache[name] = (tex_mean(SRC_TEX / FLOWER_CARDS[name], np.ones(3)), "texture mean")
        return cache[name]
    brass = REPO / "out/decokit-brass" / f"Materials/{name}.mat"  # the brass family as rebuilt (not yet adopted)
    t = (brass if brass.exists() else KIT / f"Materials/{name}.mat").read_text()
    c = [float(x) for x in re.search(r"_BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", t).groups()]
    base = np.array(c)
    tex = re.search(r"- _BaseMap:\s*\n\s*m_Texture: \{fileID: 2800000, guid: (\w+)", t)
    if tex:
        f = next(m for m in (KIT / "Textures").glob("*.png.meta") if f"guid: {tex.group(1)}" in m.read_text())
        cache[name] = (tex_mean(f.with_suffix(""), base), "texture mean")
    else:
        cache[name] = (hexof(base), "emissive" if re.search(r"_EmissionColor: \{r: ([1-9.]|0\.\d*[1-9])", t) else "flat")
    return cache[name]


def tex_mean(path, base):
    a = np.asarray(Image.open(path).convert("RGBA").resize((96, 96), Image.BOX), dtype=np.float64) / 255
    w = a[..., 3] > 0.5
    return hexof(unlin(lin(a[..., :3][w]).mean(0) * lin(np.asarray(base, dtype=np.float64))))


def texel_share_near(path):
    a = np.asarray(Image.open(path).convert("RGBA").resize((96, 96), Image.BOX), dtype=np.float64) / 255
    s = a[..., :3][a[..., 3] > 0.5]
    s = s[:: max(1, len(s) // 1500)]
    return round(100 * sum(1 for px in s if min(de(hexof(px), v) for v in SIGNALS.values()) < 20) / len(s), 1)


def build_flower_materials():
    """DK_Flower_*.mat/.meta and the texture + .meta, cloned from the legacy DK_Flower_Coral set with deterministic GUIDs (the generator writes no .mat for textured materials)."""
    mat, mmeta, tmeta = legacy_file("Materials/DK_Flower_Coral.mat"), legacy_file("Materials/DK_Flower_Coral.mat.meta"), legacy_file("Textures/flower_cluster_coral_v1_alpha.png.meta")
    text, meta = mat.read_text(), tmeta.read_text()
    og = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
    (OUT / "Materials").mkdir(parents=True, exist_ok=True)
    (OUT / "Textures").mkdir(parents=True, exist_ok=True)
    files = {}
    for name, png in FLOWER_CARDS.items():
        ng = hashlib.md5(f"suborned.decokit.tex/{png}".encode()).hexdigest()
        (OUT / "Textures" / png).write_bytes((SRC_TEX / png).read_bytes())
        (OUT / "Textures" / f"{png}.meta").write_text(meta.replace(og, ng))
        (OUT / "Materials" / f"{name}.mat").write_text(text.replace(og, ng).replace("m_Name: DK_Flower_Coral", f"m_Name: {name}"))
        mg = hashlib.md5(f"suborned.decokit.mat/{name}".encode()).hexdigest()
        (OUT / "Materials" / f"{name}.mat.meta").write_text(re.sub(r"guid: [0-9a-f]{32}", f"guid: {mg}", mmeta.read_text()))
        files[name] = [dict(root="generated", role=r, path=p, sha256=sha((OUT / p).read_bytes()), bytes=(OUT / p).stat().st_size)
                       for r, p in (("material", f"Materials/{name}.mat"), ("material-meta", f"Materials/{name}.mat.meta"), ("texture", f"Textures/{png}"), ("texture-meta", f"Textures/{png}.meta"))]
    return files

HARBOUR = json.loads((REPO / "unity/Assets/Suborned/Editor/World/looks/harbour.json").read_text())["palette"]
FACADE = json.loads((REPO / "unity/Assets/Suborned/Editor/World/facade.json").read_text())
BRASS, LIGHT_BRASS, SUNSHINE = "#99784D", "#AD866F", "#F8E26C"
# facade key -> (new value, kind, why). Brass for thin and small metal; the light brass (DK_Gold17's new colour) for plates, boards and canopies; a large enamel is not brass by default.
FACADE_DISTRICT = {
    "gold": (BRASS, "thin", "a fin or floor-band enamel in `lantern` and `records`, and the accent edge of the jazz-club and pawnbroker trades: linear elements, the brass of DK_Gold"),
    "paletteOverride.gold_trim": (BRASS, "board", "75 sign boards (4.5 x 1.2 m), the same brass as the civic-look.json hairline swatch (both `gold` and `gold_trim` are #99784D there), so a sign on the block and a sign in the district match. If the boards read dark in the lit shot the step up is the light brass #AD866F (21.3), the DK_Gold17 colour; a board is also the largest brass area in the grammar, so Latch keeps its lettering and motif to the existing sign palette"),
    "t_hotel": (LIGHT_BRASS, "canopy", "the hotel-lobby trade colour (canopy and marquee): a brass marquee is the deco hotel; the light brass keeps a 5 m canopy from reading as a dark slab"),
    "sand_p": (BRASS, "plinth", "the sand family's base course (about 0.9 m, under 6% of a 15 m elevation): the family's accent course, as cream has teal and peach coral; a bronze course under sandstone is deliberate. The sand wall (#E5D2B0) leaves no room for a stone tone (a plinth within 5 dE of the wall disappears)"),
    "ivory_p": ("#D8CDB8", "plinth", "the ivory family's base course: the thick base band that read as a gold slab on the Office. Ivory is the pale-limestone family, so its course is a deeper stone (10.3 dE from the wall, 23.2 from the guard colour), not brass"),
    "e_sun": (SUNSHINE, "large enamel", "RECOMMENDED, not brass: roof spheres and boxes (3 m and 2 m), fins, bands and awnings. The palette names sunshine as one of the district's enamel accents (palette_world.rule), the swatch `sunshine` already exists and passes (20.6), and a large pastel-yellow enamel does not read as dirty gold the way olive #D0C848 did; a large brass ball would be a brown blob on the pastel roofline. Fallback if Sky wants no yellow at all: the light brass #AD866F (21.3), as a dome's metal"),
}
ROLES_STILL_GOLD = {
    "look.json harbour.trim / style contract palette_world.roles.trim": ("#D0C848", BRASS, "street furniture (lamps, bollards, benches, hydrants, camera mast), the crossing landing patches and the dock pad: brass poles are right; the 7 m2 landing patches must leave this role (see landing)"),
    "look.json harbour.accent_gold / style contract roles.accent_gold": ("#D0C848", BRASS, "the accent_gold surface key and DK_Accent_Gold's old source: the kit material is already brass (mats.py)"),
    "style contract surface_overrides.gold_trim / facade.json paletteOverride.gold_trim": ("#C9C08E", BRASS, "DK_Gold17's old colour; the kit no longer reads it (DK_Gold17 = #AD866F). civic-look.json already holds #99784D for the civic block"),
}
LANDING = [  # (name, hex, existing surface key or None, note)
    ("civic mint", HARBOUR["harbour.accent_mint"]["hex"], "accent_mint", "RECOMMENDED: the paintings' seafoam inlays, clearly visible on cream paving (dE 24 to 26); an existing key, so no builder change"),
    ("steel plate", "#B9BBBE", None, "stainless-steel tactile plate, a real convention; chrome is in the palette; quiet (12 to 16 dE from paving); needs a new surface key"),
    ("peach", HARBOUR["harbour.accent_peach"]["hex"], "accent_peach", "in the base palette, but 21.0 from the guard colour: no margin"),
    ("kerb stone", HARBOUR["harbour.kerb"]["hex"], "kerb", "safe, and nearly invisible on the civic paving (1.5 dE): tactile in name only"),
    ("brass", BRASS, None, "a 7 m2 brass slab at every crossing (162 in the district): a large brass surface without a reason"),
    ("pale yellow, the convention", "#F4E27A", None, "yellow is only possible as olive #D0C848 (20.9 from guard) or pale #F4E27A (21.1): any warmer yellow is inside the guard's 20"),
]


def street_section(w):
    ids = [s for s in w["street"] if "/records-square/" in s["id"] or "/records-office/" in s["id"]]
    cross = sorted(s["id"] for s in ids if s["kind"] == "crossing")
    mast = sorted(s["id"] for s in ids if s["kind"] == "mast-camera")
    dock = sorted(s["id"] for s in ids if s["kind"] == "civic-robot-dock")
    return {
        "why": "civic-look.json `materials` is read off civic[] masses (a kit ref each). These three kinds are street[] items: procedural stand-ins drawn from Latch's kit.json parts, each part's colour a surface key of looks/harbour.json, not a material slot. UtilityCabinet and RobotButler are NOT in this group: they are civic masses (civic/civic-control-cabinet, civic/civic-service-cabinet-0/1, civic/foreground-civic-robot), reachable and in the table.",
        "kinds": {
            "crossing": dict(ids=cross, now="surface `trim` (looks: harbour.trim #D0C848, metallic 0.7): the yellow landing slabs, 1.8 x 4 m", count_in_district=sum(1 for s in w["street"] if s["kind"] == "crossing")),
            "mast-camera": dict(ids=mast, now="a cylinder with no surface of its own, so the item's `trim`: the yellow pole at the café's east corner (8 m); its camera box is `enamel` cobalt"),
            "civic-robot-dock": dict(ids=dock, now="a `trim` pad (explicit in its parts), a `sign` dome (#FDEB8C), an unmarked cylinder: the yellow disc by the pole"),
        },
        "smallest_first": [
            dict(step=1, what="a per-item `surface` override in street[] (Splice adds `street: {id: surface key}` to civic-look.json and the generator writes it into the export; no builder change, the keys exist)",
                 table={**{i: "accent_mint" for i in cross}, **{i: "pylon" for i in mast}},
                 reaches="the nine landing patches and the pole: both take the item's own surface (their kit parts name none). If harbour.trim becomes brass the pole row is no longer needed; the landing rows stay until Sky decides the landing colour."),
            dict(step=2, what="a civic variant of the kit entry `civic-robot-dock/civic` (pad `kerb`, dome `accent_mint`): Splice exports `variant`, Latch adds the entry to kit.json", reaches="the dock only: its pad and dome are explicit parts, which a per-item surface cannot change"),
            dict(step=3, what="leave the dock: one 1.2 m pad and a 0.5 m dome beside the pole; it follows harbour.trim and `sign` whatever Sky decides", reaches="nothing"),
        ],
    }


def landing_section():
    rows = []
    for name, h, key, note in LANDING:
        d = dsig(h)
        rows.append(dict(option=name, hex=h.upper(), surface_key=key, dE=d, min=min(d.values()), vs_civic_paving_E4DACA=round(de(h, "#E4DACA"), 1), note=note))
    return {
        "question_for_sky": "Crossing landing patches (1.8 x 4 m, one at every kerb crossing, 162 in the district) draw `trim`, now #D0C848 yellow. Tactile paving is a real convention, but the paintings show cream stone and seafoam inlay, and a yellow slab at the Office steps fights them. Which colour should the landing patch be, district-wide?",
        "options": rows,
        "recommendation": "civic mint (accent_mint #7FD0B8, 40.3 from the nearest signal): readable at play zoom, in the paintings' seafoam family, and it frees `trim` to turn brass for lamps and poles. Steel plate is the realistic alternative.",
        "civic_block_meanwhile": "street[] per-item surface override, the nine patches at records-square and records-office -> `accent_mint` (see not_reachable.smallest_first step 1). When Sky answers mint, the override becomes the district rule and the table row is dropped.",
    }


def facade_section():
    sw = FACADE["swatches"]
    uses = []

    def walk(node, path):
        if isinstance(node, dict):
            for k, v in node.items():
                if k != "swatches":
                    walk(v, f"{path}/{k}")
        elif isinstance(node, list):
            for i, v in enumerate(node):
                (uses.append((path, i, v)) if isinstance(v, str) and v in FACADE_DISTRICT else walk(v, f"{path}[{i}]"))
    walk(FACADE, "")
    out, bad = {}, []
    for k, (new, kind, why) in FACADE_DISTRICT.items():
        now = FACADE["paletteOverride"]["gold_trim"] if k.startswith("paletteOverride") else sw[k]
        d = dsig(new)
        if min(d.values()) < 20:
            bad.append(f"facade {k} {new} is {min(d.values())} dE from a signal colour")
        out[k] = dict(old=now.upper(), new=new.upper(), kind=kind, dE=d, min=min(d.values()), used_at=[f"{p}[{i}]" for p, i, v in uses if v == k] or (["75 sign boards (frontage `sign.palette`)"] if k.startswith("paletteOverride") else []), why=why)
    roles = {k: dict(old=o, new=n, dE_new=dsig(n), why=why) for k, (o, n, why) in ROLES_STILL_GOLD.items()}
    return out, roles, bad


def palette_change(brass):
    """civic-look.json palette.materials rows for the gold family (unlit values, CD-4 on each)."""
    cur = {m["material"]: m for m in json.loads((REPO / "contracts/world/civic-look.json").read_text())["palette"]["materials"]}
    used = {}
    for p in brass["brass_pieces"]:
        for m in p["materials"]:
            used.setdefault(m, []).append(p["id"])
    for p in brass["crown_pieces"]:
        for m in p["materials_new"]:
            used.setdefault(m, []).append(p["id"])
    rows = []
    for r in brass["family"]:
        if r["role"] in ("unchanged",) or not r["new"]:
            continue
        rows.append(dict(material=r["material"], row_today=cur.get(r["material"], {}).get("unlit"), unlit=r["new"], dE=r["dE_new"], min=min(r["dE_new"].values()),
                         action="change the existing row" if r["material"] in cur else "add a row", used_by=sorted(used.get(r["material"], []))))
    return rows


def changed_pieces(brass, flower_files, legacy):
    return {
        "re_adopt_fbx_lods_and_mat": [dict(id=p["id"], category=p["category"], files=[f["path"] for f in p["files_changed"]]) for p in brass["crown_pieces"]],
        "re_adopt_mat_only": [dict(id=p["id"], category=p["category"], files=p["files_changed"]) for p in brass["brass_pieces"]],
        "new_materials": {**{n: [f["path"] for f in fs] for n, fs in flower_files.items()}, "DK_Crown_Gold": ["Materials/DK_Crown_Gold.mat", "Materials/DK_Crown_Gold.mat.meta"], "DK_Crown_Gold17": ["Materials/DK_Crown_Gold17.mat", "Materials/DK_Crown_Gold17.mat.meta"]},
        "legacy_copy": sorted(legacy),
        "retuned_mat_same_guid": ["DK_Gold", "DK_Gold17", "DK_Accent_Gold"],
        "copy_from": "out/decokit-brass (the .mat set and the 25 crown pieces' Models/LODs), out/decokit-008 (the two flower materials and cards). Hashes in out/decokit-brass/brass-family.json and each file row above.",
        "no_change": "every other piece and .mat is byte-identical to the adopted file (brass-family.json `drift` is empty)",
    }


def main():
    w = json.loads((REPO / "out/world/harbour-district.world.json").read_text())
    kit = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
    look = json.loads((REPO / "contracts/world/civic-look.json").read_text())
    palette = {m["material"]: m["unlit"] for m in look["palette"]["materials"]}
    files = build_flower_materials()
    masses = {}
    for m in w["civic"]:
        if m.get("kit") and m["kit"]["id"] in RULES:
            masses.setdefault(m["kit"]["id"], []).append(m["id"])
    # ---- the table, one entry per mass; a cycled target follows the mass order of the export
    table, rows, problems = {}, {}, []
    for piece, slots in RULES.items():
        ids = masses.get(piece, [])
        have = kit[piece]["materials"]
        for slot, (to, painting, why) in slots.items():
            if slot not in have:
                problems.append(f"CD-2: {piece} has no slot {slot}")
            for k, mid in enumerate(ids):
                t = to[k % len(to)] if isinstance(to, list) else to
                table.setdefault(mid, {})[slot] = t
            ts = sorted(set(to if isinstance(to, list) else [to]))
            now_h, _ = mat_hex(slot)
            for t in ts:
                h, note = mat_hex(t)
                d = dsig(h)
                if min(d.values()) < 20:
                    problems.append(f"CD-4: {t} {h} is {min(d.values())} dE from a signal colour")
                if t not in palette:
                    rows.setdefault("palette_add", {})[t] = dict(material=t, unlit=h.upper(), basis=note, dE=d, min=min(d.values()), used_by=ids)
            rows.setdefault(piece, {})[slot] = dict(now=now_h, now_dE=dsig(now_h), to=to, targets=[dict(material=t, unlit=mat_hex(t)[0], dE=dsig(mat_hex(t)[0]), min=min(dsig(mat_hex(t)[0]).values())) for t in ts], painting=painting, why=why)
    # ---- what each piece still draws, once its table applies
    residual = {}
    for piece, slots in RULES.items():
        left = []
        for m in kit[piece]["materials"]:
            t = slots.get(m, (m,))[0]
            for t in (t if isinstance(t, list) else [t]):
                h, _ = mat_hex(t)
                if min(dsig(h).values()) < 24:
                    left.append(dict(material=t, unlit=h, min=min(dsig(h).values()), kept="swapped" if m in slots else "own"))
        residual[piece] = left
    # ---- what the existing contract table already holds
    cur = look["materials"]
    added = {mid: {s: t for s, t in sl.items() if cur.get(mid, {}).get(s) != t} for mid, sl in table.items()}
    added = {k: v for k, v in added.items() if v}
    for mid, sl in cur.items():  # nothing already in the contract may be lost or changed
        for s, t in sl.items():
            if table.get(mid, {}).get(s) != t:
                problems.append(f"contract entry {mid} {s} -> {t} is not kept")
    legacy = {}
    for f in kit["RobotWaiter"]["files"]:
        if "Face_Teal" in f["path"]:
            legacy[f["path"]] = dict(sha256=f["sha256"], bytes=f["bytes"], note="adopted from the legacy DecoKit as kit.json already lists it (RobotWaiter, candidate); not in unity/.../Kit until a piece that draws it is adopted")
    flower_stats = {n: dict(unlit_mean=mat_hex(n)[0], dE=dsig(mat_hex(n)[0]), texelsUnder20pct=texel_share_near(SRC_TEX / p)) for n, p in FLOWER_CARDS.items()}
    flower_stats["DK_Flower_Coral (district, for comparison)"] = dict(unlit_mean=mat_hex("DK_Flower_Coral")[0], dE=dsig(mat_hex("DK_Flower_Coral")[0]),
                                                                      texelsUnder20pct=texel_share_near(LEGACY[0] / "Textures/flower_cluster_coral_v1_alpha.png"))
    facade, roles_gold, fbad = facade_section()
    problems += fbad
    brass = json.loads((REPO / "out/decokit-brass/brass-family.json").read_text())
    doc = {
        "schema": "suborned.civic-overrides/1", "brief": "008 colour round, last part (Vela)", "date": "2026-10-14",
        "status": "proposal: contracts/ not edited. Splice replaces civic-look.json `materials` with `materials` below (it keeps every current entry), adds `palette_add` to palette.materials and adopts `new_materials` + `legacy_materials`.",
        "reach": "civic-look.json `materials` reaches civic[] masses only: every piece below is a civic mass in the export (kit ref), so each is reachable; the street[] items in `not_reachable` are not.",
        "by_piece": {p: dict(masses=masses.get(p, []), slots=rows[p]) for p in RULES},
        "materials": {mid: table[mid] for mid in sorted(table)},
        "masses_of": {mid: p for p, ids in masses.items() for mid in ids},
        "added_vs_contract": added,
        "palette_add": sorted(rows.get("palette_add", {}).values(), key=lambda r: r["material"]),
        "new_materials": [dict(name=n, kind="cutout (alpha-clip, as DK_Flower_Coral)", derived_from="DK_Flower_Coral (legacy), red-orange blooms recoloured by tools/decokit/derive008.py", files=f, **flower_stats[n]) for n, f in files.items()],
        "legacy_materials": legacy,
        "flower_cards": flower_stats,
        "residual_near_signal_after_override": residual,
        "not_reachable": street_section(w),
        "landing": landing_section(),
        "facade_district": facade,
        "look_and_contract_roles_still_gold": roles_gold,
        "brass_family": dict(file="out/decokit-brass/brass-family.json", materials=[{k: r[k] for k in ("material", "role", "old", "new", "dE_old", "dE_new")} for r in brass["family"]],
                             crown_pieces=[p["id"] for p in brass["crown_pieces"]], brass_pieces=[p["id"] for p in brass["brass_pieces"]]),
        "not_a_change": {"CafeSet_Tulip_Play": "no override: the piece Latch placed (civic/cafe-table-0..7) draws Ivory, Chrome, Limestone, Seafoam, Teal and Coral17 only (kit.json and the FBX both say so); DK_Enamel_Mustard is on the older CafeSet_Tulip, which the block does not place. Nothing in the piece changed."},
        "palette_change": palette_change(brass),
        "changed_pieces": changed_pieces(brass, files, legacy),
        "checks": {"CD-2 slots": "ok" if not any(p.startswith("CD-2") for p in problems) else "FAIL", "CD-4 targets": "ok" if not any(p.startswith("CD-4") for p in problems) else "FAIL", "problems": problems},
    }
    (OUT / "civic-overrides.json").write_text(json.dumps(doc, indent=1) + "\n")
    print(f"civic-overrides.json: {len(table)} masses, {sum(len(v) for v in table.values())} slot swaps, {len(rows.get('palette_add', {}))} palette rows to add, problems {problems}")
    for p, r in rows.items():
        if p == "palette_add":
            continue
        for s, v in r.items():
            print(f"  {p:20s}{s:22s}{v['now']} (min {min(v['now_dE'].values()):4.1f}) -> {', '.join(f'{t['material']} {t['unlit']} ({t['min']})' for t in v['targets'])}")
    print("  residual (< 24 dE, swapped or kept):", {p: [(r['material'], r['min'], r['kept']) for r in v] for p, v in residual.items() if v})
    print("  flowers:", {k: (v['unlit_mean'], min(v['dE'].values()), v['texelsUnder20pct']) for k, v in flower_stats.items()})
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
