"""Billboard and poster variants (brief 075-F1, Vela). No bpy.

The old DecoKit town's boards carried one 2048 atlas (old Art/DecoKit/Textures/billboard_ads_v1.png: 8 ads 3:1, 4 posters 2:3, 2 letter boards), which our
kit copies byte for byte (sheet A). Each old piece baked one fixed cell, so every Billboard_Rooftop_A showed the same Mars ad. This makes the art a set the
placement rule can vary by seed:

  sheet A  billboard_ads_v1.png  the old atlas, unchanged (legacy guid; already in the kit with the base pieces)
  sheet B  billboard_ads_v2.png  sheet A's exact layout, new art in the same style (signage_art.py ADS_B / POSTERS_B / BOARD_B; art/posters/PROVENANCE.json)

  ad variants      DK_Billboard_Ads__<ad>, DK_Billboard_Ads_Lit__<ad>  (16 ads: sheet A's 8 + sheet B's 8). The 8 billboards bake sheet A's first cell
                   (signage.py CANON; a pylon's back face the row partner, CANON_BACK), so a variant = the base .mat with its textures pointed at the ad's sheet
                   and the _BaseMap offset moved from the canonical cell to the ad's. The pylon's back face reads the same row's other ad (UV wraps: Repeat).
  sheet variants   DK_Billboard_Ads__sheetB, DK_Billboard_Ads_Lit__sheetB (offset 0): the pieces that keep their old cells (AdColumn's 4 posters, SandwichBoard's
                   2, Marquee_Cinema's letter boards) read sheet B's art in the same places.

Every variant is its base .mat's own text (shader, keywords, colour, emission, smoothness) with only m_Name, the texture guids and the _BaseMap/_MainTex/_EmissionMap
offsets changed. Same shader + same keywords = no new shader variant and no new SetPass (SRP Batcher: _BaseMap_ST sits in the per-material CBUFFER).
Names: <base>__<key>; guids derived from the name. Data/posters.json is what the catalogue copies into contracts/world/kit.json `posters`.
unity_mats.write calls write_mats() after the livery. Deterministic: no clock, no randomness.
"""

import json
import re
import shutil
from pathlib import Path

import signage_art as sa

BASES = ("DK_Billboard_Ads", "DK_Billboard_Ads_Lit")
AD_PIECES = ("Billboard_Rooftop_A", "Billboard_Rooftop_B", "Billboard_Rooftop_C", "Billboard_Wall_A", "Billboard_Wall_B", "Billboard_Wall_C")  # base DK_Billboard_Ads
LIT_PIECES = ("Billboard_Pylon_A", "Billboard_Pylon_B")  # base DK_Billboard_Ads_Lit (front + back)
SHEET_PIECES = {"AdColumn": "DK_Billboard_Ads", "SandwichBoard": "DK_Billboard_Ads", "Marquee_Cinema": "DK_Billboard_Ads_Lit"}
DEFAULTS = {"Billboard_Rooftop_A": "mars", "Billboard_Rooftop_B": "fizz", "Billboard_Rooftop_C": "skyrail", "Billboard_Pylon_A": "planetarium",
            "Billboard_Pylon_B": "cruise", "Billboard_Wall_A": "cinema", "Billboard_Wall_B": "robot", "Billboard_Wall_C": "aircar"}  # = signage.py DEFAULTS (the old cells)
CANON = sa.ADS[0]


def _f(x):
    return f"{x:.8g}"


def offset(ad):
    """UV offset from the canonical cell (sheet A's first) to `ad`'s cell (both sheets share the layout): u right, v up (atlas y is down)."""
    i = (sa.ADS + sa.ADS_B).index(ad) % len(sa.ADS)
    (x0, y0, _, _), (x, y, _, _) = sa.AD[CANON], list(sa.AD.values())[i]
    return (x - x0) / sa.S, (y0 - y) / sa.S


def variant(text, name, guid_old, guid_new, off):
    """`text` (a base .mat) with its name, its texture guid and its _BaseMap/_MainTex/_EmissionMap offsets changed; nothing else."""
    t = re.sub(r"(  m_Name: )\S+", rf"\g<1>{name}", text, count=1)
    t = t.replace(f"guid: {guid_old}", f"guid: {guid_new}")
    for slot in ("_BaseMap", "_MainTex", "_EmissionMap"):
        t, n = re.subn(rf"(    - {slot}:\n\s+m_Texture: \{{fileID: 2800000, guid: {guid_new}, type: 3\}}\n\s+m_Scale: \{{x: 1, y: 1\}}\n\s+m_Offset: )\{{[^}}]*\}}",
                       rf"\g<1>{{x: {_f(off[0])}, y: {_f(off[1])}}}", t)
        textured = re.search(rf"    - {slot}:\n\s+m_Texture: \{{fileID: 2800000, guid: {guid_new}", t) is not None
        assert n == int(textured) and (textured or slot != "_BaseMap"), f"{name}: {slot} offset not set"
    return t


def write_mats(dst, legacy, meta_text, tex_guid):
    """Sheet B (+ .meta, v1's import settings, name-derived guid) into Textures/, the variant .mats (+ .meta) and Data/posters.json; returns the .mat names."""
    dst = Path(dst)
    src_b = sa.ART_B / sa.ATLAS_B
    if not src_b.exists():
        raise SystemExit(f"[posters] {src_b} missing: python3 -I tools/decokit/signage_art.py --pack-b")
    tex = dst / "Textures"
    tex.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src_b, tex / sa.ATLAS_B)
    meta_a = (legacy / f"../Textures/{sa.ATLAS}.meta").resolve().read_text()
    guid_a = re.search(r"guid: ([0-9a-f]{32})", meta_a).group(1)
    guid_b = tex_guid(sa.ATLAS_B)
    (tex / f"{sa.ATLAS_B}.meta").write_text(meta_a.replace(f"guid: {guid_a}", f"guid: {guid_b}", 1))
    d = dst / "Materials"
    d.mkdir(parents=True, exist_ok=True)
    out, ads, sheets = [], {}, {}
    for base in BASES:
        own = d / f"{base}.mat"
        text = (own if own.exists() else legacy / f"{base}.mat").read_text()
        assert f"guid: {guid_a}" in text, f"{base}: not on sheet A"
        for ad in sa.ADS + sa.ADS_B:
            sheet = "A" if ad in sa.ADS else "B"
            off = offset(ad)
            name = f"{base}__{ad}"
            (d / f"{name}.mat").write_text(variant(text, name, guid_a, guid_a if sheet == "A" else guid_b, off))
            (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
            out.append(name)
            row = ads.setdefault(ad, dict(sheet=sheet, words=(sa.WORDS_A if sheet == "A" else sa.WORDS_B)[ad], offset=[round(off[0], 8), round(off[1], 8)], materials={}))
            row["materials"][base] = name
        name = f"{base}__sheetB"
        (d / f"{name}.mat").write_text(variant(text, name, guid_a, guid_b, (0.0, 0.0)))
        (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
        out.append(name)
        sheets.setdefault("B", dict(materials={}))["materials"][base] = name
    row_partner = {a: (sa.ADS + sa.ADS_B)[i ^ 1] for i, a in enumerate(sa.ADS + sa.ADS_B)}  # same sheet, same row, other column
    data = dict(
        schema="suborned.posters/1", brief="075-F1", source="tools/decokit/posters.py",
        rule="Per instance, swap the piece's base slot (DK_Billboard_Ads or DK_Billboard_Ads_Lit) for the chosen key's material. Billboards (`adPieces`, `litPieces`) "
             "take an ad key; `sheetPieces` take sheet A (keep the base) or sheet B. Same shader, keywords and colour as the base: only the texture and its offset differ.",
        sheets=dict(A=dict(texture=f"Textures/{sa.ATLAS}", guid=guid_a, source="the old Art/DecoKit/Textures/billboard_ads_v1.png, byte-identical"),
                    B=dict(texture=f"Textures/{sa.ATLAS_B}", guid=guid_b, source="tools/decokit/art/posters (signage_art.py pack_b; PROVENANCE.json)",
                           materials=sheets["B"]["materials"])),
        ads=ads, adOrder=list(sa.ADS + sa.ADS_B), backFace=row_partner,
        adPieces={p: "DK_Billboard_Ads" for p in AD_PIECES} | {p: "DK_Billboard_Ads_Lit" for p in LIT_PIECES},
        sheetPieces=SHEET_PIECES, defaults=DEFAULTS,
        posters=dict(A=list(sa.POSTERS), B=list(sa.POSTERS_B)), boards=dict(A=[list(v[1]) for v in sa.BOARD.values()], B=[list(v) for v in sa.BOARD_B.values()]))
    (dst / "Data").mkdir(parents=True, exist_ok=True)
    (dst / "Data" / "posters.json").write_text(json.dumps(data, indent=1) + "\n")
    print(f"[posters] {len(out)} variants, sheet B {guid_b} -> {d}")
    return out
