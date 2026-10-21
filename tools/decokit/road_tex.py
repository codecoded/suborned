"""Road surface textures from the 2.5D testbed (brief 026, Vela): the powder-blue "streamline" enamel and the white pearl marble of the testbed's road, ported to the budget.

python3 tools/decokit/road_tex.py [out_dir]     (no bpy; default out/decokit-ground, beside ground_tex.py's set: export_kit copies every ground_* file there, and mats.tex_path finds them). Each PNG gets its .meta from ground_tex.meta_text.

Sources (read-only, ~/Projects/the-suborned-store/testbed-art/district-road/, provenance.json there): enamel-base/normal/orm-v1.webp, marble-base/normal/orm-v2.webp. The testbed
multiplied the enamel by #aed0e6 after dividing by the sheet's own mean (civic-road-finish.js `richMaterial`); the same is done here: the albedo is re-centred on the target hex, so the
grain and the two-tone drift stay and the mean is exactly the hex. Writes ground_<role>_v1[_normal|_mask].png in the ground set's packing (R metallic 0, G occlusion, A smoothness),
every map at the source's own size (no downscale: Sky, 20 Oct, compression goes through Sky): enamel 1024 / 1024 / 512, pearl 1024 / 512 / 256, limestone 2048 all three.
Roles: roadenamel (#AED0E6, the testbed's: the carriageway, Sky's pick 20 Oct), roadpearl (#F1EEE6: kerb cap, lozenges, tree-pit surround), pavelimestone (the testbed's terrace and plaza
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
    "roadenamel": ("enamel", "v1", 1024, 1024, 512, "#AED0E6"),
    "roadpearl": ("marble", "v2", 1024, 512, 256, "#F1EEE6"),
}


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
    report.update(extra())
    (OUT / "road.json").write_text(json.dumps(report, indent=1))
    print("[road_tex]", OUT, {k: v["bc7MiBWithMips"] for k, v in report.items()}, "MiB")


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
