"""Surface detail for every flat-colour surface (brief 072-F, Vela; Sky 2026-10-27: "Anywhere with a flat color, no texture, we need to give it one, as long as it doesn't
hurt performance"). Texture, not palette: every map here is colourless (mean neutral), so the hue stays the swatch's or the material's and today's colour rules hold.

One set of surface layers, made from the old DecoKit's own maps (the store's source-art, read-only) where it has one, written twice:

  surface_detail_v1.png             the FacadeLit layer array: a vertical flipbook of LAYERS (1024 x 1024 each, 1024 x 6144 in all), imported by its .meta as ONE Texture2DArray
                                    (textureShape 4 = Texture2DArray, flipbookRows 6, flipbookColumns 1), LINEAR, BC7, mips, aniso 8, repeat. Channels: R albedo detail (x2: 128 = neutral), G height (0..1, for the
                                    shader's existing derivative bump, so no tangents and no vertex change), B smoothness offset (128 = none), A 255 (spare).
                                    Indexed by surface class in FacadeLit (see LAYERS[...]["classes"]); the point-sampled swatch atlas keeps the colour.
  surface_<layer>_detail_v1.png     for the untextured DecoKit rows (URP Lit / Complex Lit): the same layer as a detail albedo (LINEAR, x2: 128 neutral, grey RGB) ...
  surface_<layer>_detail_v1_normal.png  ... and a tangent normal (OpenGL, from the same height), for the materials' existing _DetailAlbedoMap / _DetailNormalMap slots
                                    (_DETAIL_MULX2), as the robot and car enamels already do: no new material, the same .mat per colour. Only KIT_PAIRS get a pair.
  surface-tex.json                  layers, sources (path + sha256), tile metres, per-file texels and MiB (BC7 / BC5 with mips), the budget, stats.

Deterministic: fixed seeds, no clock; the same sources give the same bytes. python3 tools/decokit/surface_tex.py [out_dir] (run.ts runs it before Blender; export_kit copies surface_*).
"""

import hashlib
import json
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import OUT, meta_text, normal_from_height  # noqa: E402

STORE = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit/Textures"
N = 1024
BUDGET_MIB = 24.0  # the whole 072-F set (array + kit pairs); stated before Redline measures

# Layer order is the array index FacadeLit reads. classes = the FacadeLit surface classes (ProcPaint.Cls) that sample it; "up" = only up-facing faces of that class.
# sd = target albedo standard deviation (fraction of the mean), hgt = height contrast, sm = smoothness spread; tile = metres per repeat on the wall plane (FcPlane).
# hp = high-pass width in texels (blur(px) keeps a Gaussian of spatial sigma px / 2; Glint, 072-G): variation coarser than this is taken out of the albedo and the height before sd is set, so what survives the
# play mip (zoom 14: about 2.6 cm a pixel, a 2 m tile is about 77 px) is fine grain, not 0.3-1 m tonal blotches; cloud = the macro cloud's blur width in texels (default N/10).
# Glint 072-G (2026-10-27): the textured Records Office read blotchy, not honed. Measured on the v1 sheets: limestone kept 3.4 % albedo sd in 25 cm patches and 2.4 % in
# 50 cm ones (render 2.4 / 1.8 at 38 / 75 cm, roof 3.8 / 2.7 at 19 / 38 cm), the source's tonal clouds plus the macro cloud. Now high-passed, the macro cloud fainter and
# 2.5x finer, sd raised so the pixel-scale grain still reads: limestone 4.35 / 3.86 / 3.35 / 2.36 % -> 2.15 / 1.06 / 0.62 / 0.34 % at 3 / 12 / 25 / 50 cm; render 3.41 / 2.88 /
# 2.37 / 1.76 -> about 2.0 / 0.9 / 0.4 / 0.25 at 5 / 19 / 38 / 75 cm; roof 5.04 / 4.65 / 3.81 / 2.65 -> 2.11 / 1.39 / 0.79 / 0.43 at 2 / 9 / 19 / 38 cm. Enamel, metal, panel unchanged.
LAYERS = [
    dict(name="render", src="gen/limestone_polished_v1.png", nrm="derived/limestone_polished_v1_normal.png", rot=1, tile=3.0, sd=.04, macro=.01, cloud=40, hp=90, hgt=.5, sm=.04,
         classes=["0 matte (walls, far LODs, megastructures, gates, civic style kit, service doors)"],
         why="jointless trowelled render: the limestone sheet turned 90 degrees and softened, so matte walls carry tonal cloud and grain but no courses"),
    dict(name="limestone", src="gen/limestone_polished_v1.png", nrm="derived/limestone_polished_v1_normal.png", rot=0, tile=2.0, sd=.065, macro=.012, cloud=40, hp=90, hgt=.8, sm=.05,
         classes=["1 stone (ProcFacade walls, plinths, cornices)"],
         why="DecoKit's honed limestone (fossil flecks, tonal clouds); FacadeLit keeps its procedural ashlar joints and drops its two-noise grain for this sample"),
    dict(name="enamel", src="gen/glazed_tile_ivory_v1.png", nrm="derived/glazed_tile_ivory_v1_normal.png", rot=0, tile=1.0, sd=.025, macro=.015, hgt=.5, sm=.08,
         classes=["2 enamel (bands, fascias, faience plinths)"],
         why="DecoKit's glazed ivory: mottled glaze and pinholes, the vitreous-enamel ripple that breaks a highlight"),
    dict(name="metal", src="gen/brushed_steel_v1.png", nrm="derived/brushed_steel_v1_normal.png", rot=0, tile=0.5, sd=.05, hgt=.35, sm=.12,
         classes=["4 chrome (at 0.35 strength)", "6 brass"],
         why="DecoKit's brushed steel: straight brushing; chrome takes it faint (polished), brass full (satin)"),
    dict(name="roof", src="derived/asphalt_fine_v1.png", nrm="derived/asphalt_fine_v1_normal.png", rot=0, tile=1.5, sd=.10, macro=.02, cloud=40, hp=110, hgt=1.0, sm=.03,
         classes=["0 matte, up-facing (roof caps, roof boxes' tops)"],
         why="DecoKit's fine asphalt as a roof membrane / fine gravel: roofs are the largest flat area the play camera sees"),
    dict(name="panel", src="derived/facade_panel_v1.png", nrm="derived/facade_panel_v1_normal.png", rot=0, tile=2.0, sd=.05, hfrom="albedo", hgt=1.0, sm=.04,
         classes=["7 panel (new: QuadKit walls Latch tags as stone panel, so far LODs, gates and the civic style kit match DK_Limestone_Tex beside them)"],
         why="DK_Limestone_Tex's own 1 x 2 m precast panels, colourless: a procedural wall next to a kit wall reads as the same stone"),
]
# Kit detail pairs: layer -> the DecoKit kinds (mats.KIND) whose untextured rows take it, and the bump the .mat would carry.
KIT_PAIRS = {"enamel": dict(kinds=["enamel", "ceramic"], bump=.25), "metal": dict(kinds=["metal", "chrome", "brass", "satin"], bump=.2),
             "limestone": dict(kinds=["stone"], bump=.25)}  # Glint 072-G: .35 -> .25 (honed, not rough)


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def load(rel, size=N):
    im = Image.open(STORE / rel).convert("RGB")
    if im.size != (size, size):
        im = im.resize((size, size), Image.LANCZOS)
    return np.asarray(im, dtype=np.float64) / 255.0


def lin(c):
    return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)


def periodic(u):
    """Moisan's periodic component: removes the edge discontinuity so the sheet tiles without a seam, leaves the interior detail."""
    h, w = u.shape
    v = np.zeros_like(u)
    v[0, :] += u[-1, :] - u[0, :]
    v[-1, :] += u[0, :] - u[-1, :]
    v[:, 0] += u[:, -1] - u[:, 0]
    v[:, -1] += u[:, 0] - u[:, -1]
    q, r = np.meshgrid(np.arange(w), np.arange(h))
    den = 2 * np.cos(2 * np.pi * q / w) + 2 * np.cos(2 * np.pi * r / h) - 4
    den[0, 0] = 1
    s = np.fft.fft2(v) / den
    s[0, 0] = 0
    return u - np.real(np.fft.ifft2(s))


def height_from_normal(n):
    """Frankot-Chellappa: the periodic height whose gradient best matches an OpenGL tangent normal map (green up)."""
    nx, ny, nz = n[..., 0] * 2 - 1, n[..., 1] * 2 - 1, np.maximum(n[..., 2] * 2 - 1, .2)
    p, q = -nx / nz, ny / nz
    h, w = p.shape
    wx, wy = np.meshgrid(np.fft.fftfreq(w) * 2 * np.pi, np.fft.fftfreq(h) * 2 * np.pi)
    den = wx ** 2 + wy ** 2
    den[0, 0] = 1
    z = (-1j * wx * np.fft.fft2(p) - 1j * wy * np.fft.fft2(q)) / den
    z[0, 0] = 0
    z = np.real(np.fft.ifft2(z))
    return (z - z.mean()) / max(z.std(), 1e-9)


def blur(a, px):
    fy, fx = np.fft.fftfreq(a.shape[0])[:, None], np.fft.fftfreq(a.shape[1])[None, :]
    return np.real(np.fft.ifft2(np.fft.fft2(a) * np.exp(-(fx * fx + fy * fy) * (px * np.pi) ** 2 / 2)))


def layer(spec):
    rgb = load(spec["src"])
    nrm = load(spec["nrm"])
    if spec["rot"]:
        rgb, nrm = np.rot90(rgb, spec["rot"]).copy(), np.rot90(nrm, spec["rot"]).copy()
        nrm = nrm[..., [1, 0, 2]] * np.array([-1, 1, 1]) + np.array([1, 0, 0])  # a 90 degree turn swaps the slopes: x' = -y, y' = x
    y = lin(rgb) @ np.array([.2126, .7152, .0722])
    y = periodic(y)
    ratio = y / max(y.mean(), 1e-6)
    if spec["name"] == "render":
        ratio = 1 + (blur(ratio, 2.0) - 1)  # softer than stone: the fine fleck is the trowel, not the fossil
    if spec.get("hp"):
        ratio = 1 + (ratio - blur(ratio, spec["hp"]))  # Glint 072-G: keep the fleck, drop the tonal clouds coarser than hp texels
    if spec["sd"] is not None:
        ratio = 1 + (ratio - 1) * (spec["sd"] / max(ratio.std(), 1e-9))
    if spec.get("macro"):
        # the play camera sees about 2.6 cm a pixel, so the sheets' millimetre grain mips away to flat: a soft tonal cloud (about a fifth of the tile) is what survives at zoom 14
        rng = np.random.default_rng(int(hashlib.sha256(spec["name"].encode()).hexdigest()[:8], 16))
        cloud = blur(rng.standard_normal(ratio.shape), spec.get("cloud", N / 10))
        ratio = ratio + spec["macro"] * cloud / max(cloud.std(), 1e-9)
    ratio = np.clip(ratio, .6, 1.4)
    alb = np.clip(.5 * ratio, 0, 1)
    # height: from the normal map (Frankot-Chellappa), or for the panel sheet from its albedo (the joints are its darkest lines; its normal integrates to almost flat)
    hgt = periodic(height_from_normal(nrm)) if spec.get("hfrom") != "albedo" else ratio - blur(ratio, 40.0)
    if spec.get("hp"):
        hgt = hgt - blur(hgt, spec["hp"])  # the derivative bump and the kit normal: no slow undulation that shades a wall in patches
    hgt = (hgt - hgt.mean()) / max(hgt.std(), 1e-9)
    h01 = np.clip(.5 + hgt * .16 * spec["hgt"], 0, 1)
    # smoothness: crevices (low height) and dark flecks a touch rougher; brushed metal streaks follow the albedo
    smo = np.clip(.5 + spec["sm"] * (.6 * np.tanh(hgt) + .4 * (ratio - 1) / max((ratio - 1).std(), 1e-9) * .5), 0, 1)
    return alb, h01, smo, dict(albedoSdPercent=round(float((alb / .5).std() * 100), 2), meanLevel=round(float(alb.mean() * 255 * 2), 1),
                               heightSd=round(float(h01.std()), 3))


def save(arr, path, mode):
    Image.fromarray((np.clip(arr, 0, 1) * 255 + .5).astype(np.uint8), mode).save(path, optimize=True)


def array_meta(file, rows):
    t = meta_text(file, "da", (N, N * rows))
    t = t.replace("  textureShape: 1\n", "  textureShape: 4\n").replace("  flipbookRows: 1\n", f"  flipbookRows: {rows}\n")
    t = t.replace("    aniso: 16\n", "    aniso: 8\n")
    return t.replace("  alphaUsage: 0\n", "  alphaUsage: 0\n", 1)


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    files, stats, sources = [], {}, {}
    strip = []
    made = {}
    for spec in LAYERS:
        alb, h01, smo, st = layer(spec)
        made[spec["name"]] = (alb, h01)
        stats[spec["name"]] = st
        sources[spec["name"]] = {k: dict(path=str(Path("source-art/DecoKit/Textures") / spec[k]), sha256=sha(STORE / spec[k])) for k in ("src", "nrm")}
        strip.append(np.dstack([alb, h01, smo, np.ones_like(alb)]))
    arr_name = "surface_detail_v1.png"
    save(np.vstack(strip), OUT / arr_name, "RGBA")
    (OUT / f"{arr_name}.meta").write_text(array_meta(arr_name, len(LAYERS)))
    files.append(dict(file=arr_name, size=[N, N * len(LAYERS)], kind="array", layers=len(LAYERS), format="BC7 linear", MiB=round(N * N * len(LAYERS) * 4 / 3 / 2 ** 20, 2)))
    for name, pair in KIT_PAIRS.items():
        alb, h01 = made[name]
        a = f"surface_{name}_detail_v1.png"
        n = f"surface_{name}_detail_v1_normal.png"
        save(np.dstack([alb] * 3), OUT / a, "RGB")
        (OUT / f"{a}.meta").write_text(meta_text(a, "da", (N, N)))
        save(normal_from_height(h01, 6.0), OUT / n, "RGB")
        (OUT / f"{n}.meta").write_text(meta_text(n, "n", (N, N)))
        files += [dict(file=a, size=[N, N], kind="detail albedo (linear x2)", format="BC7", MiB=round(N * N * 4 / 3 / 2 ** 20, 2)),
                  dict(file=n, size=[N, N], kind="detail normal", format="BC5", MiB=round(N * N * 4 / 3 / 2 ** 20, 2))]
    total = round(sum(f["MiB"] for f in files), 2)
    rep = dict(schema="suborned.surfacetex/1", note="brief 072-F: colourless detail for flat-colour surfaces; texture, not palette", budgetMiB=BUDGET_MIB, totalMiB=total,
               layers=[dict(index=i, **{k: v for k, v in s.items() if k not in ("src", "nrm", "rot", "hfrom")}, sources=sources[s["name"]], stats=stats[s["name"]]) for i, s in enumerate(LAYERS)],
               kitPairs=KIT_PAIRS, files=files)
    (OUT / "surface-tex.json").write_text(json.dumps(rep, indent=1))
    if total > BUDGET_MIB:
        raise SystemExit(f"[surface-tex] {total} MiB over the {BUDGET_MIB} MiB budget")
    print(f"[surface-tex] {len(LAYERS)} layers + {len(KIT_PAIRS)} kit pairs, {total} MiB of {BUDGET_MIB} -> {OUT}")


if __name__ == "__main__":
    build()
