"""Building colourways (brief 074-B, Vela; Sky 2026-10-28: "Do building colors"; 074-H, Sky 2026-10-28: walls and plinths only, "back to the old world"). No bpy.

074-H: a colourway tints only the walls (DK_Limestone_Tex, DK_Render_White) and the plinth (DK_Stone_Dark). The band (DK_Enamel_Seafoam) and fascia
(DK_Enamel_Teal) are seafoam and teal on every building, no variants (Redline: this meets every draw mark). The wall is softened: each family's wall swatch
lerped in CIELAB toward the old reel's cream (the old facade_panel_v1 sheet's mean colour, what the old DecoKit city's walls read as) by WALL_SOFTEN, so a
street of colourways holds together the way the old reel's cream does. The table below is 074-B's; band and fascia are no longer slots (BAND_*/FASCIA_* kept
for the record, unused).

The every-face kit modules (SideBay*, SidePilaster*, CornerPier*, AlleyWall*, ShopFill*, ShopStack*, and the street fronts: StreamBay/End/Corner, PortholeBay, BalconyBay,
DecoBay, ArcadeBay ...) draw every building in one colourway: cream stone, deep teal fascia, seafoam bands, granite plinth. A colourway is one of the 12 wall families of
unity/Assets/Suborned/Editor/World/facade.json (cream ... seagreen; `civic` is not one: it keeps the base materials). Per family it is one material set over five base slots:

  slot     base material        family colour (facade.json swatches: _w wall, _p plinth/faience accent)
  wall     DK_Limestone_Tex     _w, as the textured mean: the panel sheet is neutral (below), so the tint lands the wall hex for any family, blue and lilac included
  wall     DK_Render_White      _w (flat colour under its 072-F limestone detail pair, mean neutral), so a render front and its stone sides read one building
  band     DK_Enamel_Seafoam    _p lifted toward the wall: CIELAB L = L_p + 0.55 (L_w - L_p), chroma x 0.8, the accent's hue (no muddy wall/accent mix)
  fascia   DK_Enamel_Teal       _p deepened: L = clamp(L_p - 14, 28, 50), the accent's hue and chroma (the base's dark teal fascia under a mid band)
  plinth   DK_Stone_Dark        _p as it is (facade.json grammar 2a: the plinth is a faience accent, not a darker stone)

Each set differs from its base only in base colour: the base .mat's own text (shader, keywords, textures, the 072-F detail pair, smoothness, instancing flag) with
m_Name, _BaseColor and _Color replaced. Same shader + same keywords = no new variant and no new SetPass; the SRP batcher keeps batching across them.
Names: <base>__<family> (DK_Limestone_Tex__lilac); guids derived from the name, so the same table gives the same bytes.

Neutral panel sheet: wall_panel_neutral_v1.png replaces facade_panel_v1.png in DK_Limestone_Tex (same 2048 px, same import settings: no texture memory added). It is the
old sheet's linear luminance ratio (its cream hue taken out), the joints and dark flecks kept at full depth and the slab's light grain softened into a
shoulder below 1 (KNEE), so the neutral mean (about 0.95 linear) leaves every family's tint a plain colour (<= 1, no HDR): the brightest wall, apricot, needs 0.947. DK_Limestone_Tex's own tint is the
old sheet's mean colour over the neutral mean, so the base (and `civic`) reads as today (the per-pixel dE2000 is reported). Blender keeps facade_panel_v1 for the base row
(mats.TEXTURES; FBX bytes unchanged); the .mat and the materials table point Unity at the neutral sheet.

  python3 tools/decokit/colourways.py [out_dir]   the neutral sheet + .meta and colourways-tex.json into out/decokit-ground (run.ts runs it before Blender)
  unity_mats.write calls write_mats(): DK_Limestone_Tex.mat (generated, legacy guid kept), the 60 colourway .mats, and Data/colourways.json (the mapping Latch reads
  through contracts/world/kit.json `colourways`).
Deterministic: no clock, no randomness.
"""

import hashlib
import json
import math
import os
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
FACADE = REPO / "unity/Assets/Suborned/Editor/World/facade.json"
GROUND = Path(os.environ.get("DECOKIT_GROUND") or REPO / "out/decokit-ground")
STORE = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit/Textures"
SRC_SHEET = "derived/facade_panel_v1.png"
NEUTRAL = "wall_panel_neutral_v1.png"
REPORT = "colourways-tex.json"
FAMILIES = ("cream", "peach", "mint", "sky", "sand", "ivory", "blush", "powder", "butter", "lilac", "apricot", "seagreen")  # facade.json order; `civic` keeps the base
SLOTS = {"DK_Limestone_Tex": "wall", "DK_Render_White": "wall", "DK_Stone_Dark": "plinth"}  # 074-H: band and fascia stay seafoam and teal everywhere
RETIRED = ("DK_Enamel_Seafoam", "DK_Enamel_Teal")  # 074-B's band and fascia slots: their old <base>__<family>.mats are removed from the generator's output
# Brief 075 (golden view, Splice; tools/world/gen/oldstreet.ts): the old DecoKitColourways high street (scheme plan+livery+soft), exact: its Soft palette's walls with no
# softening, a painted building's eyebrows and canopy (DK_Enamel_Ivory) at Lighter(wall) = halfway in linear to ivory #EFE8D6 (the old rule), the plinth left as the kit's.
# facade.json carries them as families (never drawn by an area: only the generator's old-street rule gives them).
OLD_FAMILIES = ("oldapricot", "oldceladon", "oldrose", "oldochre")
OLD_SLOTS = {"DK_Limestone_Tex": "wall", "DK_Render_White": "wall", "DK_Enamel_Ivory": "trim"}
OLD_IVORY = "#EFE8D6"
WALL_SOFTEN = 0.35  # 074-H: how far each family's wall moves toward the old reel's cream (CIELAB lerp; 0 = the facade.json swatch, 1 = all cream)
BAND_LIFT, BAND_CHROMA = 0.55, 0.8
FASCIA_DROP, FASCIA_L = 14.0, (28.0, 50.0)
KNEE = 0.9  # luminance ratio (to the sheet's mean) above which the slab grain is softened into the shoulder


# ------------------------------------------------------------------ colour maths (sRGB D65, CIELAB)
def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def gam(c):
    c = min(max(c, 0.0), 1.0)
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def rgb_hex(c):
    return "#%02X%02X%02X" % tuple(round(min(max(x, 0.0), 1.0) * 255) for x in c)


_M = ((0.4124, 0.3576, 0.1805), (0.2126, 0.7152, 0.0722), (0.0193, 0.1192, 0.9505))
_MI = ((3.2406, -1.5372, -0.4986), (-0.9689, 1.8758, 0.0415), (0.0557, -0.2040, 1.0570))
_W = (0.95047, 1.0, 1.08883)


def to_lab(h):
    r = [lin(c) for c in hex_rgb(h)]
    xyz = [sum(_M[i][j] * r[j] for j in range(3)) / _W[i] for i in range(3)]
    f = [t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116 for t in xyz]
    return (116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2]))


def from_lab(L, a, b):
    fy = (L + 16) / 116
    fx, fz = fy + a / 500, fy - b / 200
    inv = lambda t: t ** 3 if t ** 3 > 0.008856 else (t - 16 / 116) / 7.787  # noqa: E731
    xyz = [inv(fx) * _W[0], inv(fy) * _W[1], inv(fz) * _W[2]]
    for _ in range(40):  # out of gamut: pull the chroma in until it fits (keeps L and hue)
        r = [sum(_MI[i][j] * xyz[j] for j in range(3)) for i in range(3)]
        if all(-1e-4 <= x <= 1.0001 for x in r):
            break
        a, b = a * 0.95, b * 0.95
        fx, fz = fy + a / 500, fy - b / 200
        xyz = [inv(fx) * _W[0], inv(fy) * _W[1], inv(fz) * _W[2]]
    return rgb_hex([gam(x) for x in r])


def band_of(w, p):
    Lw, _, _ = to_lab(w)
    Lp, ap, bp = to_lab(p)
    return from_lab(Lp + BAND_LIFT * (Lw - Lp), ap * BAND_CHROMA, bp * BAND_CHROMA)


def fascia_of(p):
    Lp, ap, bp = to_lab(p)
    return from_lab(min(max(Lp - FASCIA_DROP, FASCIA_L[0]), FASCIA_L[1]), ap, bp)


def soften(w, cream, s=WALL_SOFTEN):
    """The wall swatch `w` lerped toward `cream` by s in CIELAB (perceptual: L, a, b move together, no hue swing through grey)."""
    if not s:
        return w.upper()
    a, b = to_lab(w), to_lab(cream)
    return from_lab(*[x + s * (y - x) for x, y in zip(a, b)])


def old_cream(old_mean_linear):
    """The old reel's cream: the old facade_panel_v1 sheet's mean colour (linear per channel -> sRGB hex)."""
    return rgb_hex([gam(c) for c in old_mean_linear])


def family_colours(cream=None, s=WALL_SOFTEN):
    """family -> {wall, plinth, wallSwatch} (sRGB hex), from facade.json's swatches; the wall softened toward `cream` by s (none = the swatch).
    Brief 075: the old high-street families (OLD_FAMILIES) keep their swatch exactly and carry `trim` (old Lighter), no plinth."""
    fac = json.loads(FACADE.read_text())
    sw, fam = fac["swatches"], fac["families"]
    out = {}
    for f in FAMILIES:
        w, p = sw[fam[f][0]].upper(), sw[fam[f][1]].upper()
        out[f] = dict(wall=soften(w, cream, s) if cream else w, plinth=p, wallSwatch=w, swatches=dict(w=fam[f][0], p=fam[f][1]))
    for f in OLD_FAMILIES:
        w = sw[fam[f][0]].upper()
        out[f] = dict(wall=w, trim=lighter(w), wallSwatch=w, swatches=dict(w=fam[f][0], p=fam[f][1]))
    return out


def lighter(h):
    """Old DecoKitColourways.Lighter: halfway (linear) from the wall colour to the kit's ivory."""
    return rgb_hex([gam((lin(a) + lin(b)) * 0.5) for a, b in zip(hex_rgb(h), hex_rgb(OLD_IVORY))])


def slots_of(f):
    """The base slots a family recolours (074-H: wall + plinth; brief 075's old high-street families: wall + trim)."""
    return OLD_SLOTS if f in OLD_FAMILIES else SLOTS


# ------------------------------------------------------------------ the neutral panel sheet (numpy + Pillow)
def tex_guid(file):
    return hashlib.md5(f"suborned.decokit.tex/{file}".encode()).hexdigest()


def legacy_tex_root():
    import unity_mats
    return (unity_mats.legacy_mats() / "../Textures").resolve()


def build_tex(out=GROUND):
    import numpy as np
    from PIL import Image
    sys.path.insert(0, str(Path(__file__).parent))
    src = STORE / SRC_SHEET
    a = np.asarray(Image.open(src).convert("RGB"), dtype=np.float64) / 255.0
    al = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    Y = al @ np.array([0.2126, 0.7152, 0.0722])
    ratio = Y / Y.mean()
    # a soft shoulder above the knee: the joints and dark flecks (ratio < KNEE, about 5 % of the texels) keep their full depth, the slab body's light grain is
    # compressed toward 1 (tanh: order kept, never clipped flat), so the neutral mean is high enough that every family's tint stays a colour (<= 1, no HDR)
    nl = np.where(ratio <= KNEE, ratio, KNEE + (1 - KNEE) * np.tanh((ratio - KNEE) / (1 - KNEE)))
    nl = np.clip(nl, 0, 1)
    body = ratio > KNEE
    ns = np.where(nl <= 0.0031308, nl * 12.92, 1.055 * nl ** (1 / 2.4) - 0.055)
    q = np.round(ns * 255).astype(np.uint8)
    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.dstack([q] * 3), "RGB").save(out / NEUTRAL, optimize=True)
    # the meta: the old sheet's own (same size, format, mips, filter, repeat), a name-derived guid
    meta = (legacy_tex_root() / "facade_panel_v1.png.meta").read_text()
    (out / f"{NEUTRAL}.meta").write_text(re.sub(r"guid: [0-9a-f]{32}", f"guid: {tex_guid(NEUTRAL)}", meta, count=1))
    # what Unity multiplies: the quantised neutral (linear) mean; the old sheet's per-channel linear mean
    qn = q.astype(np.float64) / 255.0
    ql = np.where(qn <= 0.04045, qn / 12.92, ((qn + 0.055) / 1.055) ** 2.4)
    nmean = float(ql.mean())
    omean = [float(al[..., i].mean()) for i in range(3)]
    base_tint = [m / nmean for m in omean]
    # dE2000 (sampled, every 8th texel) between the old sheet and neutral x base tint: how far the base drifts from today
    import deltae
    sub = (slice(None, None, 8), slice(None, None, 8))
    o8, n8 = al[sub].reshape(-1, 3), ql[sub].reshape(-1)
    des = sorted(deltae.de(rgb_hex([gam(c) for c in o]), rgb_hex([gam(n * t) for t in base_tint])) for o, n in zip(o8[::7], n8[::7]))
    # the same at the play-zoom mip (zoom 14: about 2.6 cm a pixel, a 2 m tile about 77 px: 16 x 16 texel boxes, averaged in linear)
    k = 16
    om = al.reshape(al.shape[0] // k, k, al.shape[1] // k, k, 3).mean(axis=(1, 3)).reshape(-1, 3)
    nm = ql.reshape(ql.shape[0] // k, k, ql.shape[1] // k, k).mean(axis=(1, 3)).reshape(-1)
    dm = sorted(deltae.de(rgb_hex([gam(c) for c in o]), rgb_hex([gam(n * t) for t in base_tint])) for o, n in zip(om[::5], nm[::5]))
    fam = family_colours(old_cream(omean))
    tints = {f: [lin(c) / nmean for c in hex_rgb(v["wall"])] for f, v in fam.items()}
    over = {f: round(max(t), 4) for f, t in tints.items() if max(t) > 1.0}
    if over or max(base_tint) > 1.0:
        raise SystemExit(f"[colourways] a wall tint over 1 (the neutral mean {nmean:.3f} is too dark): {over}")
    rep = dict(schema="suborned.colourways-tex/1", brief="074-B", source=dict(path=f"source-art/DecoKit/Textures/{SRC_SHEET}", sha256=hashlib.sha256(src.read_bytes()).hexdigest()),
               file=NEUTRAL, guid=tex_guid(NEUTRAL), size=list(q.shape[1::-1]), knee=KNEE, grain=dict(bodySdPercentOld=round(float(ratio[body].std() / ratio[body].mean() * 100), 2), bodySdPercentNeutral=round(float(ql[body].std() / ql[body].mean() * 100), 2), note="slab body (ratio > knee): the light grain softened so every tint fits; joints untouched"),
               neutralMeanLinear=round(nmean, 6), oldMeanLinear=[round(m, 6) for m in omean], baseTintLinear=[round(t, 6) for t in base_tint],
               baseTint=rgb_hex([gam(t) for t in base_tint]), baseDrift=dict(sampleTexels=len(des), median=round(des[len(des) // 2], 2), p95=round(des[int(len(des) * .95)], 2), max=round(des[-1], 2)),
               baseDriftPlayMip=dict(boxTexels=k, samples=len(dm), median=round(dm[len(dm) // 2], 2), p95=round(dm[int(len(dm) * .95)], 2), max=round(dm[-1], 2)),
               memory=dict(replaces="facade_panel_v1.png", note="same 2048 x 2048 RGB, same importer settings: the copy prunes the old sheet, no texture memory added",
                           MiB=round(2048 * 2048 * 4 / 3 / 2 ** 20, 2), format="the old sheet's (default platform, high-quality compression: BC7 on Standalone, 1 byte a texel, mips)"),
               sha256=hashlib.sha256((out / NEUTRAL).read_bytes()).hexdigest())
    (out / REPORT).write_text(json.dumps(rep, indent=1) + "\n")
    print(f"[colourways] {NEUTRAL}: neutral mean {nmean:.3f} linear, base tint {rep['baseTint']}, base drift dE2000 median {rep['baseDrift']['median']} p95 {rep['baseDrift']['p95']} -> {out}")
    return rep


def tex_report():
    p = GROUND / REPORT
    if not p.exists():
        raise SystemExit(f"[DK-EXPORT] no {p}: run python3 tools/decokit/colourways.py (bun run decokit does) before the .mat set")
    return json.loads(p.read_text())


# ------------------------------------------------------------------ the .mat set (called by unity_mats.write)
def _f(x):
    return f"{x:.8g}"


def _recolour(text, name, rgb):
    """`text` (a base .mat) with only its name and base colour changed."""
    col = f"{{r: {_f(rgb[0])}, g: {_f(rgb[1])}, b: {_f(rgb[2])}, a: 1}}"
    text = re.sub(r"(  m_Name: )\S+", rf"\g<1>{name}", text, count=1)
    text, n = re.subn(r"(    - _BaseColor: )\{[^}]*\}", rf"\g<1>{col}", text)
    assert n == 1, f"{name}: no _BaseColor"
    return re.sub(r"(    - _Color: )\{[^}]*\}", rf"\g<1>{col}", text)


def limestone_text(legacy_text, rep):
    """DK_Limestone_Tex on the neutral sheet: the legacy .mat with _BaseMap/_MainTex re-pointed and the base tint as its colour (normal and mask unchanged)."""
    old = re.search(r"_BaseMap:\n\s+m_Texture: \{fileID: 2800000, guid: ([0-9a-f]{32})", legacy_text).group(1)
    t = legacy_text.replace(old, rep["guid"])
    return _recolour(t, "DK_Limestone_Tex", [gam(c) for c in rep["baseTintLinear"]])


def write_mats(dst, legacy, meta_text):
    """DK_Limestone_Tex.mat on the neutral sheet, then <base>__<family>.mat for every slot x family from the base .mat as written (generated) or the legacy one,
    and Data/colourways.json; returns the names this wrote."""
    rep = tex_report()
    d = Path(dst) / "Materials"
    d.mkdir(parents=True, exist_ok=True)
    (d / "DK_Limestone_Tex.mat").write_text(limestone_text((legacy / "DK_Limestone_Tex.mat").read_text(), rep))
    (d / "DK_Limestone_Tex.mat.meta").write_text(meta_text("DK_Limestone_Tex", legacy))
    out = ["DK_Limestone_Tex"]
    cream = old_cream(rep["oldMeanLinear"])
    fam = family_colours(cream)
    for b in RETIRED:  # 074-B's band and fascia sets, left by an earlier run: gone, so nothing stale is catalogued or copied
        for f in FAMILIES:
            for x in (d / f"{b}__{f}.mat", d / f"{b}__{f}.mat.meta"):
                x.unlink(missing_ok=True)
    mapping, hexes = {}, {}
    every = FAMILIES + OLD_FAMILIES
    for base in dict.fromkeys(list(SLOTS) + list(OLD_SLOTS)):
        src = d / f"{base}.mat" if (d / f"{base}.mat").exists() else legacy / f"{base}.mat"
        text = src.read_text()
        mapping[base] = {}
        for f in every:
            slot = slots_of(f).get(base)
            if not slot:
                continue
            name = f"{base}__{f}"
            hx = fam[f][slot]
            rgb = [gam(lin(c) / rep["neutralMeanLinear"]) for c in hex_rgb(hx)] if base == "DK_Limestone_Tex" else list(hex_rgb(hx))
            (d / f"{name}.mat").write_text(_recolour(text, name, rgb))
            (d / f"{name}.mat.meta").write_text(meta_text(name, legacy))
            mapping[base][f] = name
            out.append(name)
    for f in every:
        hexes[f] = {k: fam[f][k] for k in ("wall", "plinth", "trim", "wallSwatch") if k in fam[f]} | dict(swatches=fam[f]["swatches"])
    data = dict(schema="suborned.colourways/1", brief="074-B, 074-H", source="tools/decokit/colourways.py",
                rule="A module's colourway (export `colourway`, a facade.json family) swaps each base material below for its family's; `base` and `civic` keep the base. "
                     "Same shader, keywords and textures as the base: only the base colour differs. 074-H: walls and plinth only; band (DK_Enamel_Seafoam) and fascia (DK_Enamel_Teal) "
                     "keep the base seafoam and teal on every building.",
                families=list(every), keep=["base", "civic"], slots=SLOTS | OLD_SLOTS, materials=mapping,
                old=dict(families=list(OLD_FAMILIES), slots=OLD_SLOTS, ivory=OLD_IVORY, source="brief 075: the old DecoKitColourways high street (Soft palette, Lighter trim), exact, no softening"), colours=hexes, wallSoften=WALL_SOFTEN, oldCream=cream,
                derivation=dict(wall=f"Lab lerp of _w toward the old reel's cream {cream} (the old facade_panel_v1 mean) by {WALL_SOFTEN:g} (DK_Limestone_Tex: tint = linear wall / the neutral sheet's linear mean)",
                                plinth="_p", band="none: the base DK_Enamel_Seafoam everywhere (074-H)", fascia="none: the base DK_Enamel_Teal everywhere (074-H)"),
                neutral=dict(file=rep["file"], guid=rep["guid"], baseTint=rep["baseTint"], baseDrift=rep["baseDrift"], baseDriftPlayMip=rep["baseDriftPlayMip"], grain=rep["grain"], memory=rep["memory"]))
    (Path(dst) / "Data").mkdir(parents=True, exist_ok=True)
    (Path(dst) / "Data" / "colourways.json").write_text(json.dumps(data, indent=1) + "\n")
    return out


if __name__ == "__main__":
    sys.path.insert(0, str(Path(__file__).parent))
    build_tex(Path(sys.argv[1]) if len(sys.argv) > 1 else GROUND)
