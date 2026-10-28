"""Road surface textures from the 2.5D testbed (brief 026, Vela): the powder-blue "streamline" enamel and the white pearl marble of the testbed's road, ported to the budget.
Brief 075-B (2026-10-28): the powder-blue carriageway (roadenamel) is no longer the testbed's enamel webp but the old DecoKit road sheet in the same colour: see ENAMEL and enamel().

python3 tools/decokit/road_tex.py [out_dir]     (no bpy; default out/decokit-ground, beside ground_tex.py's set: export_kit copies every ground_* file there, and mats.tex_path finds them). Each PNG gets its .meta from ground_tex.meta_text.

Sources (read-only, ~/Projects/the-suborned-store/testbed-art/district-road/, provenance.json there): enamel-base/normal/orm-v1.webp, marble-base/normal/orm-v2.webp. The testbed
multiplied the enamel by #aed0e6 after dividing by the sheet's own mean (civic-road-finish.js `richMaterial`); the same is done here: the albedo is re-centred on the target hex, so the
grain and the two-tone drift stay and the mean is exactly the hex. Writes ground_<role>_v1[_normal|_mask].png in the ground set's packing (R metallic 0, G occlusion, A smoothness),
every map at the source's own size (no downscale: Sky, 20 Oct, compression goes through Sky): enamel 1024 / 1024 / 512, pearl 1024 / 512 / 256, limestone 2048 all three.
Roles: roadenamel is now #8FAFC1 (0.83 x the testbed's #AED0E6: the game's lit road matches the testbed's, brief 028 enamel fix), roadasphalt (#505359, brief 055 road test: the asphalt option's carriageway, generated here: slate binder, three sizes of aggregate, worn and sealed patches, 4 m tile; see asphalt()), roadenamel (#AED0E6, the testbed's: the carriageway, Sky's pick 20 Oct), roadpearl (#F1EEE6: kerb cap, lozenges, tree-pit surround), pavelimestone (the testbed's terrace and plaza
limestone, cafe-terrace/limestone-*.png, re-centred on #E6D8BE: 1 m slabs, 6 mm joints, 2 m tile), tileteal (made here: the testbed's glazed blue inlay tile, #669FAB, 0.72 m pitch, 14 mm pale grout #D7D9CF,
per-tile shade 0.94 to 1.06, bevelled edge: civic-paving-material.js `createCivicBlueTileMaterial`, fountain-plaza.js `tile`; 1.44 m tile = 2 x 2 tiles).
"""

import json
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from ground_tex import meta_text  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
STORE = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "testbed-art/district-road"
OUT = Path(sys.argv[1] if len(sys.argv) > 1 else REPO / "out/decokit-ground")

ROLES = {  # role -> (source stem, version, albedo px, normal px, mask px, target hex)
    "roadpearl": ("marble", "v2", 1024, 512, 256, "#F1EEE6"),
}
# roadenamel (brief 075-B, Vela; Sky 2026-10-28 "back to the old world textures", Aster: "keeping the powder-blue colour but maybe the old texture"): the carriageway keeps #8FAFC1 (brief 028: 0.83 x the
# testbed's #AED0E6, hue kept) but its sheet is the old DecoKit road's (DK_Asphalt_Road: asphalt_road_v1, 16 m tile, the old reel's matte road with its soft stains and sealed patches) instead of the
# testbed's speckled enamel webp, which read as terrazzo at play zoom. Luminance only (lum / mean x the target), so the stains are darker powder blue, never brown. The normal, mask and the fine
# aggregate detail pair are the ground set's own (ground_tex.py: ground_asphalt_v1_normal / _mask, ground_asphalt_detail_v1 from the same old sheets), shared, so only the albedo is new:
# 1536 px (1.04 cm a texel over 16 m; play zoom is ~2.6 cm a pixel) = the texels the enamel's three maps had (3.0 MiB with mips), no memory added.
ENAMEL = dict(src="asphalt_road_v1", px=1536, target="#8FAFC1", tile=16.0, contrast=0.65)  # contrast: the old sheet's stains and sealed patches at 65 percent: at full strength the oil spots read as dirt at play zoom (briefs/075/textures)


def s2l(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def l2s(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def load(name, size, mode="RGB"):
    im = Image.open(STORE / name).convert(mode)
    return im.resize((size, size), Image.LANCZOS) if im.size[0] != size else im


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    report = {}
    for role, (stem, ver, a_px, n_px, m_px, target) in ROLES.items():
        alb = np.asarray(load(f"{stem}-base-{ver}.webp", a_px)).astype(np.float32) / 255
        lin = s2l(alb)
        mean = lin.reshape(-1, 3).mean(0)
        out = lin / mean * s2l(hexrgb(target))  # re-centre on the target: grain and drift kept
        Image.fromarray((l2s(out) * 255 + 0.5).astype(np.uint8)).save(OUT / f"ground_{role}_v1.png")
        Image.open(STORE / f"{stem}-normal-{ver}.webp").convert("RGB").resize((n_px, n_px), Image.LANCZOS).save(OUT / f"ground_{role}_v1_normal.png")
        orm = np.asarray(load(f"{stem}-orm-{ver}.webp", m_px)).astype(np.float32) / 255  # R occlusion, G roughness, B metalness
        mask = np.zeros((m_px, m_px, 4), np.uint8)
        mask[..., 1] = (orm[..., 0] * 255).astype(np.uint8)
        mask[..., 3] = ((1 - orm[..., 1]) * 255).astype(np.uint8)  # smoothness
        Image.fromarray(mask, "RGBA").save(OUT / f"ground_{role}_v1_mask.png")
        for suffix, kind, px in (("", "a", a_px), ("_normal", "n", n_px), ("_mask", "m", m_px)):
            (OUT / f"ground_{role}_v1{suffix}.png.meta").write_text(meta_text(f"ground_{role}_v1{suffix}.png", kind, (px, px)))
        texels = a_px * a_px + n_px * n_px + m_px * m_px
        report[role] = dict(source=f"{stem}-*-{ver}.webp", target=target, tile=[2.0, 2.0], texels=texels, bc7MiBWithMips=round(texels * 4 / 3 / 2 ** 20, 2))
    report.update(enamel())
    report.update(extra())
    report.update(asphalt())
    (OUT / "road.json").write_text(json.dumps(report, indent=1))
    print("[road_tex]", OUT, {k: v["bc7MiBWithMips"] for k, v in report.items()}, "MiB")


ASPHALT_HEX = "#505359"  # the mean of the albedo: a cool slate with a warm lean, light enough to read as stone and dark enough to carry white lines (brief 055 road test)
ASPHALT_TILE = 4.0       # metres; 1024 px = 4 mm a texel


def _blur(a, sigma):
    """Periodic gaussian blur by FFT (the sheet tiles seamlessly)."""
    n = a.shape[0]
    f = np.fft.fftfreq(n)
    k = np.exp(-2 * (np.pi * sigma) ** 2 * (f[:, None] ** 2 + f[None, :] ** 2))
    return np.fft.ifft2(np.fft.fft2(a) * k).real


def _norm(a):
    return (a - a.mean()) / (a.std() + 1e-9)


def asphalt():
    """roadasphalt (brief 055 road test, Vela): the dark carriageway of the asphalt option, made here (no source sheet: the old city's sheet is a flat grey with one crack).
    Character at play zoom, not noise: a slate binder under three sizes of aggregate (pale cream, warm peach-grey, teal-grey and dark chips: the city's stone, the palette's family), tonal drift
    over a few metres (worn patches lighter and rougher, polished tyre lanes darker and glossier), two tar-sealed patches. Periodic, 4 m tile at 1024 px (a texel is 4 mm), re-centred on ASPHALT_HEX
    like the enamel. Normal from the aggregate's height; mask R metallic 0, G occlusion in the crevices, A smoothness 0.18 (worn) to 0.34 (polished)."""
    N = 1024
    rng = np.random.default_rng(55)
    white = lambda: rng.standard_normal((N, N))
    fine, mid, big = _norm(_blur(white(), 0.9)), _norm(_blur(white(), 2.2)), _norm(_blur(white(), 6.0))
    drift = _norm(_blur(white(), 70.0)) * 0.6 + _norm(_blur(white(), 22.0)) * 0.4        # tone over metres
    wear = np.clip(0.5 + _norm(_blur(white(), 60.0)) * 0.35, 0, 1)                      # 0 sealed and dark .. 1 worn, aggregate showing
    # aggregate: chips are where the band-passed noise is high; their class (colour) comes from a second field, so a chip is one colour
    band = _norm(_blur(white(), 1.6)) + 0.5 * fine
    chip = np.clip((band - 0.45) / 0.8, 0, 1)
    cls = _norm(_blur(white(), 1.8))
    lin_base = s2l(hexrgb("#3D4045"))
    lin = np.broadcast_to(lin_base, (N, N, 3)).copy()
    lin *= (1 + 0.05 * fine + 0.04 * mid + 0.045 * drift)[..., None]
    lin *= (1 + 0.10 * wear)[..., None]                                                    # worn tone is paler
    chip_cols = [(cls < -1.4, "#E9D8BE", 0.5), ((cls >= -1.4) & (cls < -0.1), "#8E847E", 0.55), ((cls >= -0.1) & (cls < 0.9), "#7C8889", 0.5), (cls >= 0.9, "#26292E", 0.6)]
    for sel, hx, k in chip_cols:
        w = (chip * sel * (0.7 + 0.3 * wear) * k)[..., None]                             # chips show most where it is worn
        lin = lin * (1 - w) + s2l(hexrgb(hx)) * w
    # two sealed patches (tar-seal rectangles: a little darker and smoother, a hairline darker edge); where they lie is fixed by the seed
    seal = np.zeros((N, N))
    for (x0, y0, w, h) in ((140, 610, 230, 150), (690, 120, 150, 260)):
        seal[y0:y0 + h, x0:x0 + w] = 1
    edge = np.clip(seal - _blur(seal, 1.5) * 1.0, 0, 1) + np.clip(_blur(seal, 1.5) - seal, 0, 1) * 0
    seal_s = _blur(seal, 1.2)
    lin *= (1 - 0.07 * seal_s - 0.04 * np.clip(edge * 4, 0, 1))[..., None]
    out = lin / lin.reshape(-1, 3).mean(0) * s2l(hexrgb(ASPHALT_HEX))
    Image.fromarray((l2s(out) * 255 + 0.5).astype(np.uint8)).save(OUT / "ground_roadasphalt_v1.png")
    # height: aggregate stands proud of the binder; the seal is flat
    h = (0.55 * chip * (1 - 0.7 * seal_s) + 0.18 * fine + 0.10 * mid + 0.04 * big) * 1.0
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    k = 2.2
    nrm = np.dstack([-gx * k, gy * k, np.ones_like(h)])
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    nrm_img = ((nrm * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(nrm_img).save(OUT / "ground_roadasphalt_v1_normal.png")
    M = 512
    sm = lambda a: np.asarray(Image.fromarray(a.astype(np.float32), "F").resize((M, M), Image.BILINEAR))
    mask = np.zeros((M, M, 4), np.uint8)
    mask[..., 1] = (np.clip(1 - 0.35 * sm(chip), 0, 1) * 255).astype(np.uint8)
    smooth = 0.26 - 0.08 * sm(wear) + 0.07 * sm(seal_s) - 0.02 * sm(chip)
    mask[..., 3] = (np.clip(smooth, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(mask, "RGBA").save(OUT / "ground_roadasphalt_v1_mask.png")
    for suffix, kind, px in (("", "a", N), ("_normal", "n", N), ("_mask", "m", M)):
        (OUT / f"ground_roadasphalt_v1{suffix}.png.meta").write_text(meta_text(f"ground_roadasphalt_v1{suffix}.png", kind, (px, px)))
    t = N * N + N * N + M * M
    return {"roadasphalt": dict(source="generated (seed 55)", target=ASPHALT_HEX, tile=[ASPHALT_TILE, ASPHALT_TILE], texels=t, bc7MiBWithMips=round(t * 4 / 3 / 2 ** 20, 2))}


def enamel():
    """roadenamel from the old DecoKit road sheet (see ENAMEL). Writes ground_roadenamel_v1.png (+ .meta) only; returns its report row with the source's sha256."""
    import hashlib
    src = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit/Textures/derived" / f"{ENAMEL['src']}.png"
    a = np.asarray(Image.open(src).convert("RGB").resize((ENAMEL["px"], ENAMEL["px"]), Image.LANCZOS)).astype(np.float32) / 255
    lum = (s2l(a) * np.array([.2126, .7152, .0722])).sum(2)
    rel = 1 + (lum / lum.mean() - 1) * ENAMEL["contrast"]
    out = rel[..., None] * s2l(hexrgb(ENAMEL["target"]))
    Image.fromarray((l2s(out) * 255 + 0.5).astype(np.uint8)).save(OUT / "ground_roadenamel_v1.png")
    (OUT / "ground_roadenamel_v1.png.meta").write_text(meta_text("ground_roadenamel_v1.png", "a", (ENAMEL["px"], ENAMEL["px"])))
    for stale in ("ground_roadenamel_v1_normal.png", "ground_roadenamel_v1_mask.png"):  # the enamel webp's own maps: no longer made (the ground set's asphalt pair is shared)
        for f in (OUT / stale, OUT / f"{stale}.meta"):
            f.unlink(missing_ok=True)
    t = ENAMEL["px"] ** 2
    m = l2s(s2l(np.asarray(Image.open(OUT / "ground_roadenamel_v1.png").convert("RGB")).astype(np.float32) / 255).reshape(-1, 3).mean(0))
    return {"roadenamel": dict(source=f"source-art/DecoKit/Textures/derived/{ENAMEL['src']}.png (the old DecoKit DK_Asphalt_Road sheet)", sha256=hashlib.sha256(src.read_bytes()).hexdigest(),
                               brief="075-B", target=ENAMEL["target"], meanAfter="#%02X%02X%02X" % tuple(int(round(x * 255)) for x in m), tile=[ENAMEL["tile"], ENAMEL["tile"]],
                               shared=["ground_asphalt_v1_normal.png", "ground_asphalt_v1_mask.png", "ground_asphalt_detail_v1.png", "ground_asphalt_detail_v1_normal.png"],
                               texels=t, bc7MiBWithMips=round(t * 4 / 3 / 2 ** 20, 2))}


def extra():
    """pavelimestone (from the testbed's limestone PBR) and tileteal (generated)."""
    cafe = STORE.parent / "cafe-terrace"
    rep = {}
    # limestone
    alb = np.asarray(Image.open(cafe / "limestone-base.png").convert("RGB")).astype(np.float32) / 255
    lin = s2l(alb)
    out = lin / lin.reshape(-1, 3).mean(0) * s2l(hexrgb("#E6D8BE"))
    Image.fromarray((l2s(out) * 255 + 0.5).astype(np.uint8)).save(OUT / "ground_pavelimestone_v1.png")
    Image.open(cafe / "limestone-normal.png").convert("RGB").save(OUT / "ground_pavelimestone_v1_normal.png")
    orm = np.asarray(Image.open(cafe / "limestone-orm.png").convert("RGB")).astype(np.float32) / 255
    mask = np.zeros((2048, 2048, 4), np.uint8)
    mask[..., 1] = (orm[..., 0] * 255).astype(np.uint8)
    mask[..., 3] = ((1 - orm[..., 1]) * 255).astype(np.uint8)
    Image.fromarray(mask, "RGBA").save(OUT / "ground_pavelimestone_v1_mask.png")
    for suffix, kind, px in (("", "a", 2048), ("_normal", "n", 2048), ("_mask", "m", 2048)):
        (OUT / f"ground_pavelimestone_v1{suffix}.png.meta").write_text(meta_text(f"ground_pavelimestone_v1{suffix}.png", kind, (px, px)))
    t = 2048 * 2048 * 3
    rep["pavelimestone"] = dict(source="cafe-terrace/limestone-*.png", target="#E6D8BE", tile=[2.0, 2.0], texels=t, bc7MiBWithMips=round(t * 4 / 3 / 2 ** 20, 2))
    # glazed teal tile: 2 x 2 tiles of 0.72 m pitch = 1.44 m, 512 px
    N, per, g = 512, 256, 5
    ys, xs = np.mgrid[0:N, 0:N]
    fx, fy = xs % per, ys % per
    d = np.minimum(np.minimum(fx, per - 1 - fx), np.minimum(fy, per - 1 - fy)).astype(np.float32)   # px from the tile's edge
    inside = d >= g / 2
    bev = np.clip((d - g / 2) / 4.5, 0, 1)                                                          # bevel ramp 4.5 px (~12 mm)
    rng = np.random.default_rng(26)
    shade = (0.94 + 0.12 * rng.random((2, 2)))[(ys // per) % 2, (xs // per) % 2]
    glaze = 1 + 0.025 * np.sin(xs / 37.0 + ys / 53.0)
    blue, grout = s2l(hexrgb("#669FAB")), s2l(hexrgb("#D7D9CF"))
    col = np.where(inside[..., None], blue * (shade * glaze)[..., None] * (0.9 + 0.1 * bev[..., None]), grout)
    Image.fromarray((l2s(col) * 255 + 0.5).astype(np.uint8)).save(OUT / "ground_tileteal_v1.png")
    h = bev * 1.0
    gx, gy = np.gradient(h)
    nrm = np.dstack([-gy * 0.9, -gx * 0.9, np.ones_like(h)])
    nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    Image.fromarray(((nrm * 0.5 + 0.5) * 255).astype(np.uint8)).save(OUT / "ground_tileteal_v1_normal.png")
    mk = np.zeros((512, 512, 4), np.uint8)
    mk[..., 1] = 255
    small = inside.astype(np.float32)
    mk[..., 3] = (255 * (0.24 + 0.40 * small)).astype(np.uint8)   # grout rough .76, glaze rough .36
    Image.fromarray(mk, "RGBA").save(OUT / "ground_tileteal_v1_mask.png")
    for suffix, kind, px in (("", "a", 512), ("_normal", "n", 512), ("_mask", "m", 512)):
        (OUT / f"ground_tileteal_v1{suffix}.png.meta").write_text(meta_text(f"ground_tileteal_v1{suffix}.png", kind, (px, px)))
    t = 512 * 512 * 3
    rep["tileteal"] = dict(source="generated (testbed tile recipe)", target="#669FAB", tile=[1.44, 1.44], texels=t, bc7MiBWithMips=round(t * 4 / 3 / 2 ** 20, 2))
    return rep


main()
