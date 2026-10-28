"""Road surface textures from the 2.5D testbed (brief 026, Vela): the powder-blue "streamline" enamel and the white pearl marble of the testbed's road, ported to the budget.
Brief 075-B (2026-10-28): the powder-blue carriageway (roadenamel) is no longer the testbed's enamel webp but the old DecoKit road sheet in the same colour: see ENAMEL and enamel().

python3 tools/decokit/road_tex.py [out_dir]     (no bpy; default out/decokit-ground, beside ground_tex.py's set: export_kit copies every ground_* file there, and mats.tex_path finds them). Each PNG gets its .meta from ground_tex.meta_text.

Sources (read-only, ~/Projects/the-suborned-store/testbed-art/district-road/, provenance.json there): enamel-base/normal/orm-v1.webp, marble-base/normal/orm-v2.webp. The testbed
multiplied the enamel by #aed0e6 after dividing by the sheet's own mean (civic-road-finish.js `richMaterial`); the same is done here: the albedo is re-centred on the target hex, so the
grain and the two-tone drift stay and the mean is exactly the hex. Writes ground_<role>_v1[_normal|_mask].png in the ground set's packing (R metallic 0, G occlusion, A smoothness),
every map at the source's own size (no downscale: Sky, 20 Oct, compression goes through Sky): enamel 1024 / 1024 / 512, pearl 1024 / 512 / 256, limestone 2048 all three.
Roles: roadenamel is now #8FAFC1 (0.83 x the testbed's #AED0E6: the game's lit road matches the testbed's, brief 028 enamel fix), roadasphalt (brief 075 remap: the old DecoKit road DK_Asphalt_Road, its sheet and normal byte for byte, 16 m tile; see asphalt(); was brief 055's made #505359 sheet), roadenamel (#AED0E6, the testbed's: the carriageway, Sky's pick 20 Oct), roadpearl (#F1EEE6: kerb cap, lozenges, tree-pit surround), pavelimestone (the testbed's terrace and plaza
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


# roadasphalt (brief 075 remap, Vela; Sky 2026-10-28 on briefs/075/flyby/grid-remap.png: the old asphalt road back, overriding 075-B's "keep powder blue"): the old DecoKit road, ported as it was.
# The old DecoKitGroundMesh.Road() = Sheet("DK_Asphalt_Road", "asphalt_road_v1", 16f, .28f): URP Lit, _BaseColor white, the sheet's albedo and its normal (bump 1) tiled every 16 m, smoothness .28 flat,
# no mask, no detail. Both PNGs are copied byte for byte (2048 px each, the old size: no downscale) and must match the sha256 pinned here; the old .mat's values are in ASPHALT_OLD.
# The 055 made sheet (seed 55, #505359, 4 m) is retired: Sky's reference is the old reel's road, which is this sheet.
OLD_GAME = Path(os.environ.get("SUBORNED_OLD", "~/Projects/the-suborned")).expanduser() / "game/Assets/TheSuborned/Art/DecoKit"
ASPHALT_SRC = {  # our file -> (old file, sha256): the old game first, the store's derived copy (same bytes) as the fallback
    "ground_roadasphalt_v1.png": ("Textures/asphalt_road_v1.png", "292e38ddd19a211b8e02e0817a530da1981dab0375bc413e4c8ce9f5d1c3a11c", "a"),
    "ground_roadasphalt_v1_normal.png": ("Textures/asphalt_road_v1_normal.png", "3da4dfe81988ec5ab9d30c73c12c1cb0aa5c2bcd34391370191a8b53f55416c4", "n"),
}
ASPHALT_OLD = dict(mat="Generated/DK_Asphalt_Road.mat", code="Editor/DecoKit/DecoKitGroundMesh.cs Road()", tile=16.0, smoothness=0.28, bump=1.0, baseColor="#FFFFFF", albedoGuid="c7c88ded8fc0e405d888ffaa976c543e",
                   normalGuid="9228c667c324043479dc005642aec34f")
ASPHALT_TILE = ASPHALT_OLD["tile"]


def asphalt():
    """roadasphalt: the old DecoKit road sheet and its normal, byte for byte, hash-checked (see ASPHALT_SRC). Writes the two PNGs and their .metas; drops the 055 sheet's mask (the old road had none)."""
    import hashlib
    store = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit/Textures/derived"
    files = {}
    for ours, (old, sha, kind) in ASPHALT_SRC.items():
        src = next((c for c in (OLD_GAME / old, store / Path(old).name) if c.exists()), None)
        if src is None:
            raise SystemExit(f"[road_tex] the old road sheet {old} is in neither the old game nor the store (set SUBORNED_OLD or SUBORNED_STORE)")
        data = src.read_bytes()
        got = hashlib.sha256(data).hexdigest()
        if got != sha:
            raise SystemExit(f"[road_tex] {src}: sha256 {got} is not the old road's {sha}")
        (OUT / ours).write_bytes(data)
        px = Image.open(OUT / ours).size
        (OUT / f"{ours}.meta").write_text(meta_text(ours, kind, px))
        files[ours] = dict(source=str(src).replace(str(Path.home()), "~"), sha256=sha, px=list(px))
    for stale in ("ground_roadasphalt_v1_mask.png",):  # the 055 sheet's mask: the old road is smoothness .28 flat, no mask
        for f in (OUT / stale, OUT / f"{stale}.meta"):
            f.unlink(missing_ok=True)
    t = sum(v["px"][0] * v["px"][1] for v in files.values())
    m = l2s(s2l(np.asarray(Image.open(OUT / "ground_roadasphalt_v1.png").convert("RGB")).astype(np.float32) / 255).reshape(-1, 3).mean(0))
    return {"roadasphalt": dict(source="the old DecoKit DK_Asphalt_Road (asphalt_road_v1 + _normal), byte for byte", brief="075 remap asphalt", files=files, old=ASPHALT_OLD,
                                mean="#%02X%02X%02X" % tuple(int(round(x * 255)) for x in m), tile=[ASPHALT_TILE, ASPHALT_TILE], texels=t, bc7MiBWithMips=round(t * 4 / 3 / 2 ** 20, 2))}


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
