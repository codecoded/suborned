"""Ground textures at the budget (brief 026 round 1, Vela): the old DecoKit's asphalt, granite kerb, gutter setts, paving flags and lawn,
ported from the store's derived sheets (SourceArt/DecoKit/Textures/derived, read-only) to the sizes the Air can afford, recoloured to the
26 Sep paintings, plus a park path made here. No bpy: python3 tools/decokit/ground_tex.py [out_dir]  (run.ts runs it before Blender).

Writes <out_dir>/ground_<role>_v1[_normal|_mask].png, the detail pair ground_asphalt_detail_v1[_normal].png, each with a .meta (a guid derived
from the file name, so the same input gives the same bytes; sRGB, normal-map type, BC7 / BC5 on Standalone, aniso 16) and ground.json (the
role table: file sizes, texels, BC7-with-mips MiB, tile metres, colours measured before and after). The budget is a table in this file.

Maps (Unity URP Lit, as the old kit): albedo sRGB; _normal OpenGL tangent (green up); _mask = R metallic 0, G occlusion, A smoothness
(the old pave sheets' packing, read as _MetallicGlossMap + _OcclusionMap). Sheets are periodic, so a mesh with UV = world metres / tile tiles seamlessly.

Colour: the sources were graded for the old city's HDRI. Each is moved (power curve per channel, so contrast and hue structure stay) to the mean of
the painting it must match: the 26 Sep mission block's road #797374 / #696970, its paving #D3CAB5..#E2D4C0, kerb and gutter paler and warmer than the
old blue-grey granite (the paintings' kerb is the pavement stone), lawn a green in the palms-and-hedges family (hue 108, brief 048). TARGET below holds each hex and why.
"""

import hashlib
import json
import math
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from unity_mats import tex_guid  # noqa: E402  (no bpy there)

REPO = Path(__file__).resolve().parents[2]
STORE = Path(os.environ.get("SUBORNED_STORE", "~/Projects/the-suborned-store")).expanduser() / "source-art/DecoKit/Textures"
DER = STORE / "derived"
OUT = Path(sys.argv[1] if len(sys.argv) > 1 else os.environ.get("DECOKIT_GROUND") or REPO / "out/decokit-ground")

# name -> (target mean sRGB hex of the albedo, why)
TARGET = {
    "asphalt": ("#79757A", "painting road samples #797374 / #696970: warm-leaning violet grey; the old sheet was #807B74"),
    "kerb": ("#D0C9BB", "the paintings' kerb is the pavement stone, a touch greyer for form; the old granite was blue-grey #A2A9B7"),
    "gutter": ("#8D8A88", "darker than the kerb, a trace of the old setts' blue (#738497) kept so the kerb line reads"),
    "pavement": ("#D5CBB8", "painting paving #D3CAB5 / #CCBEAC / #E2D4C0; the old flags were #DFD5C7"),
    "lawn": ("#64875A", "hue 108 (brief 048, Glint: the long view sampled hue 71, olive; the old #74875A sat at hue 85): same value and saturation, hue turned toward the palms and hedges. Before: painted foliage olive, less saturated (round 2): the old #638449 and round 1 #6D8A4B read loud; stripes softened to 45 percent in lawn_soften"),
    "path": ("#D8CAAB", "warm sand hoggin, one step darker than the paving so a path reads against it"),
}

# role -> list of (suffix, size (w, h), kind) ; kind: a (albedo) n (normal) m (mask). Texels * 4/3 (mips) * 1 byte (BC7 / BC5).
PLAN = {
    "asphalt": dict(tile=(16.0, 16.0), src="asphalt_road_v1", maps=[("", (2048, 2048), "a"), ("_normal", (512, 512), "n"), ("_mask", (512, 512), "m")]),
    "asphalt_detail": dict(tile=(1.5, 1.5), src="asphalt_fine_v1", maps=[("", (512, 512), "da"), ("_normal", (1024, 1024), "n")]),
    "kerb": dict(tile=(0.5, 0.5), src="granite_kerb_v1", maps=[("", (512, 512), "a"), ("_normal", (512, 512), "n"), ("_mask", (256, 256), "m")]),
    "gutter": dict(tile=(4.0, 0.375), src="gutter_setts_v1", maps=[("", (2048, 192), "a"), ("_normal", (2048, 192), "n"), ("_mask", (2048, 192), "m")]),
    "pavement": dict(tile=(4.0, 4.0), src="pave_flags_v1", maps=[("", (2048, 2048), "a"), ("_normal", (1024, 1024), "n"), ("_mask", (512, 512), "m")]),
    "lawn": dict(tile=(8.0, 8.0), src="lawn_v1", maps=[("", (1536, 1536), "a"), ("_normal", (512, 512), "n")]),
    "path": dict(tile=(2.0, 2.0), src=None, maps=[("", (1024, 1024), "a"), ("_normal", (512, 512), "n")]),
}
# Reused as they are (already in the kit, already counted at their own size; DK_Terrazzo_Ivory is in unity/Assets/Suborned/Kit): the terrazzo pair, 1024 albedo + 1024 normal each.
REUSED = {"terrazzo_ivory_v1": [(1024, 1024), (1024, 1024)], "terrazzo_seafoam_v1": [(1024, 1024), (1024, 1024)]}
BUDGET_MIB = 30.0


def srgb_to_lin(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def hex_rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def mean_hex(a):
    return "#%02X%02X%02X" % tuple(int(round(x * 255)) for x in lin_to_srgb(srgb_to_lin(a).reshape(-1, 3).mean(0)))


def recolour(a, target_hex):
    """Power curve per channel (linear light) so the mean of the image equals the target: contrast and the hue structure stay, nothing clips."""
    lin = srgb_to_lin(a)
    want = srgb_to_lin(hex_rgb(target_hex))
    out = np.empty_like(lin)
    for c in range(3):
        lo, hi = 0.05, 8.0
        for _ in range(40):
            g = (lo + hi) / 2
            if (lin[..., c] ** g).mean() > want[c]:
                lo = g
            else:
                hi = g
        out[..., c] = lin[..., c] ** g
    return lin_to_srgb(out)


KEEP = {"asphalt": .3, "pavement": .5}  # chroma kept from the sheet (granite default .25): the road sheet's warm stains and patches read pink-brown in sun, so its chroma varies less; the mean stays #79757A


def recolour_grey(a, target_hex, keep=.25):
    """For granite (kerb, setts): the sheets' blue cast would turn to orange grout under a per-channel curve. Match the luminance mean with one curve, give it the
    target's hue, keep `keep` of the sheet's own chroma so crystals and joints still differ in colour."""
    keep = KEEP.get(ROLE[0], keep)
    lin = srgb_to_lin(a)
    lum = (lin * np.array([.2126, .7152, .0722])).sum(2)
    want = srgb_to_lin(hex_rgb(target_hex))
    wl = float((want * np.array([.2126, .7152, .0722])).sum())
    lo, hi = .05, 8.0
    for _ in range(40):
        g = (lo + hi) / 2
        if (lum ** g).mean() > wl:
            lo = g
        else:
            hi = g
    l2 = lum ** g
    tint = want / wl
    own = lin / np.maximum(lum[..., None], 1e-4)
    chroma = tint * (1 - keep) + own * keep
    return lin_to_srgb(l2[..., None] * chroma)


ROLE = [""]  # the role being built (recolour_grey reads its KEEP)


def lawn_soften(a, stripes=.45, sat=.78, sigma_px=28):
    """The lawn at play zoom: 1 m mown bands read as hard stripes and the green is loud. Low-frequency luminance (bands, drifts, sigma about 15 cm) is pulled toward the mean
    to `stripes` of its strength (periodic FFT blur, so it stays seamless), and chroma is scaled to `sat`; fine blade detail is untouched."""
    lin = srgb_to_lin(a)
    lum = (lin * np.array([.2126, .7152, .0722])).sum(2)
    h, w = lum.shape
    fy, fx = np.fft.fftfreq(h)[:, None], np.fft.fftfreq(w)[None, :]
    low = np.real(np.fft.ifft2(np.fft.fft2(lum) * np.exp(-2 * (np.pi * sigma_px) ** 2 * (fx * fx + fy * fy))))
    gain = (lum.mean() / np.maximum(low, 1e-4)) ** (1 - stripes)
    lin = lin * gain[..., None]
    l2 = (lin * np.array([.2126, .7152, .0722])).sum(2, keepdims=True)
    lin = l2 + (lin - l2) * sat
    return lin_to_srgb(lin)


def load_rgb(name):
    return np.asarray(Image.open(DER / f"{name}.png").convert("RGB")).astype(np.float32) / 255


def resize(a, size, mode):
    """Lanczos in the right space: albedo in sRGB 8 bit as the import would; normals as vectors, renormalised; masks per channel."""
    w, h = size
    chans = a.shape[2]
    img = Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8), "RGBA" if chans == 4 else "RGB")
    if (w, h) != img.size:
        img = img.resize((w, h), Image.LANCZOS)
    out = np.asarray(img).astype(np.float32) / 255
    if mode == "n":
        v = out[..., :3] * 2 - 1
        v /= np.maximum(np.linalg.norm(v, axis=2, keepdims=True), 1e-6)
        out = v * .5 + .5
    return out


def save(path, a):
    arr = (np.clip(a, 0, 1) * 255 + .5).astype(np.uint8)
    Image.fromarray(arr, "RGBA" if arr.shape[2] == 4 else "RGB").save(path, optimize=True)


def normal_from_height(h, strength):
    """OpenGL tangent normal (green up) from a periodic height field."""
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * .5 * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * .5 * strength
    n = np.dstack([-dx, dy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * .5 + .5


def periodic_noise(n, octaves, rng, lacunarity=2.0, gain=.5):
    """Seamless fractal noise by filtering white noise in the Fourier domain."""
    fy = np.fft.fftfreq(n)[:, None]
    fx = np.fft.fftfreq(n)[None, :]
    r = np.sqrt(fx * fx + fy * fy)
    r[0, 0] = 1
    acc = np.zeros((n, n))
    amp = 1.0
    for o in range(octaves):
        f0 = (2 ** o) / 64
        spec = np.fft.fft2(rng.standard_normal((n, n))) * np.exp(-((r / (f0 * 1.5)) ** 2)) * amp
        acc += np.real(np.fft.ifft2(spec))
        amp *= gain
    return (acc - acc.mean()) / max(acc.std(), 1e-9)


def make_path():
    """Park path: compacted warm hoggin. Fine grit, pale and dark pebbles, soft tonal drift; seamless, 2 m tile."""
    rng = np.random.default_rng(31)
    n = 1024
    drift = periodic_noise(n, 3, rng, gain=.6) * .03
    grit = periodic_noise(n, 6, rng, lacunarity=2.2, gain=.8)
    fine = rng.standard_normal((n, n))
    h = grit * .6 + fine * .5
    pebble = periodic_noise(n, 5, rng, gain=.9)
    pm = (pebble > 1.75).astype(float)  # pebbles: a few percent of the area, a mm or two proud
    h = h + pm * 1.2
    tone = 1 + drift + grit * .035 + fine * .03 - pm * .12 * np.sign(periodic_noise(n, 2, rng))
    base = np.array([.80, .72, .56])
    a = np.clip(tone[..., None] * base, 0, 1)
    a = recolour(a, TARGET["path"][0])
    nrm = normal_from_height((h - h.min()) / (h.max() - h.min()), 4.0)
    return a.astype(np.float32), nrm.astype(np.float32)


def asphalt_mask(alb512, nrm512):
    """The old road sheet had no mask (smoothness a flat .25). Stains and sealed patches are the darker, smoother tone; the grit's cavities occlude.
    A smoothness of .20 +- .08 reads as dry asphalt; R metallic 0."""
    lum = alb512.mean(2)
    dev = (lum - lum.mean()) / max(lum.std(), 1e-6)
    sm = np.clip(.20 - .03 * np.clip(dev, -2.5, 2.5), .12, .28)  # round 3: .18-.50 made glossy patches that read as water at grazing angles; asphalt is rough
    cav = np.clip((1 - nrm512[..., 2]) * 6, 0, 1)
    ao = 1 - .5 * cav
    return np.dstack([np.zeros_like(sm), ao, np.zeros_like(sm), sm])


def detail_albedo(src512):
    """URP detail albedo multiplies at x2 around mid grey: asphalt_fine's grain as luminance, centred on 128 and softened to +-14 percent."""
    lum = src512.mean(2)
    d = (lum - lum.mean()) / max(lum.std(), 1e-6)
    v = np.clip(.5 + d * .07, 0, 1)
    return np.dstack([v, v, v])


def meta_text(file, kind, size):
    """TextureImporter .meta: a name-derived guid, sRGB for albedo, normal-map type, BC7 (BC5 for normals) on Standalone, trilinear, aniso 16, repeat."""
    srgb = kind == "a"  # the detail albedo is LINEAR: URP Lit multiplies it x2, so 128 imports as 0.5 and is neutral; sRGB would read 0.216 and darken the road 2.3x (Latch probe, out/asphalt-probe)
    ttype = 1 if kind == "n" else 0
    fmt = 27 if kind == "n" else 25  # BC5 / BC7 (1 byte a texel each)
    guid = tex_guid(file)
    plat = lambda target, over: (  # noqa: E731
        f"  - serializedVersion: 4\n    buildTarget: {target}\n    maxTextureSize: {max(size)}\n    resizeAlgorithm: 0\n    textureFormat: {fmt if over else -1}\n"
        f"    textureCompression: 3\n    compressionQuality: 100\n    crunchedCompression: 0\n    allowsAlphaSplitting: 0\n    overridden: {1 if over else 0}\n"
        "    ignorePlatformSupport: 0\n    androidETC2FallbackOverride: 0\n    forceMaximumCompressionQuality_BC6H_BC7: 1\n")
    return (f"fileFormatVersion: 2\nguid: {guid}\nTextureImporter:\n  internalIDToNameTable: []\n  externalObjects: {{}}\n  serializedVersion: 13\n  mipmaps:\n"
            f"    mipMapMode: 0\n    enableMipMap: 1\n    sRGBTexture: {1 if srgb else 0}\n    linearTexture: 0\n    fadeOut: 0\n    borderMipMap: 0\n"
            "    mipMapsPreserveCoverage: 0\n    alphaTestReferenceValue: 0.5\n    mipMapFadeDistanceStart: 1\n    mipMapFadeDistanceEnd: 3\n"
            "  bumpmap:\n    convertToNormalMap: 0\n    externalNormalMap: 0\n    heightScale: 0.25\n    normalMapFilter: 0\n    flipGreenChannel: 0\n"
            "  isReadable: 0\n  streamingMipmaps: 0\n  streamingMipmapsPriority: 0\n  vTOnly: 0\n  ignoreMipmapLimit: 0\n  grayScaleToAlpha: 0\n  generateCubemap: 6\n"
            "  cubemapConvolution: 0\n  seamlessCubemap: 0\n  textureFormat: 1\n"
            f"  maxTextureSize: {max(size)}\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 2\n    aniso: 16\n    mipBias: 0\n    wrapU: 0\n    wrapV: 0\n    wrapW: 0\n"
            "  nPOTScale: 1\n  lightmap: 0\n  compressionQuality: 100\n  spriteMode: 0\n  spriteExtrude: 1\n  spriteMeshType: 1\n  alignment: 0\n"
            "  spritePivot: {x: 0.5, y: 0.5}\n  spritePixelsToUnits: 100\n  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n  spriteGenerateFallbackPhysicsShape: 1\n"
            f"  alphaUsage: {1 if kind == 'm' else 0}\n  alphaIsTransparency: 0\n  spriteTessellationMethod: 0\n  spriteTessellationDetail: -1\n  spriteGeometrySubdivision: -1\n  textureType: {ttype}\n"
            "  textureShape: 1\n  singleChannelComponent: 0\n  flipbookRows: 1\n  flipbookColumns: 1\n  maxTextureSizeSet: 1\n  compressionQualitySet: 1\n  textureFormatSet: 1\n"
            "  ignorePngGamma: 0\n  applyGammaDecoding: 0\n  swizzle: 50462976\n  cookieLightType: 0\n  platformSettings:\n"
            + plat("DefaultTexturePlatform", False) + plat("Standalone", True)
            + "  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    customData: \n    physicsShape: []\n    bones: []\n    spriteID: \n"
            "    internalID: 0\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n    spriteCustomMetadata:\n      entries: []\n"
            "    nameFileIdTable: {}\n  mipmapLimitGroupName: \n  pSDRemoveMatte: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    rows, tot_texels, cache = {}, 0, {}
    for role, p in PLAN.items():
        stem = f"ground_{role}_v1" if role != "asphalt_detail" else "ground_asphalt_detail_v1"
        key = role.split("_")[0]
        before = after = None
        if role == "path":
            alb, nrm = make_path()
            srcs = {"a": alb, "n": nrm}
        else:
            alb_src = load_rgb(p["src"])
            nrm_src = load_rgb(p["src"] + "_normal")
            mask_src = None
            mp = DER / f"{p['src']}_mask.png"
            if mp.exists():
                mask_src = np.asarray(Image.open(mp).convert("RGBA")).astype(np.float32) / 255
            before = mean_hex(alb_src)
            ROLE[0] = key
            if role == "lawn":
                alb_src = lawn_soften(alb_src)
            if role == "pavement":  # the slab tone drift repeats every 4 m and reads as a motif from the air: keep 70 percent of it (Latch also offsets / mirrors the UV per patch)
                mu = alb_src.reshape(-1, 3).mean(0)
                alb_src = np.clip(mu + (alb_src - mu) * .7, 0, 1)
            # pavement (brief 048, Vela): the per-channel power curve drove the flags' darkest 5 percent to hue 52 (olive grey #8A8879) while the mean sat at hue 39; in shade that
            # multiplied into the dark olive-brown Glint measured. One luminance curve with the target's hue and half the sheet's own chroma holds every tone at the cream hue.
            alb = alb_src if role == "asphalt_detail" else (recolour_grey if key in ("kerb", "gutter", "asphalt", "pavement") else recolour)(alb_src, TARGET[key][0])
            srcs = {"a": alb, "n": nrm_src, "m": mask_src}
        files = []
        for suffix, size, kind in p["maps"]:
            name = f"{stem}{suffix}.png"
            if kind == "da":
                img = detail_albedo(resize(alb, size, "a"))
            elif role == "asphalt_detail" and kind == "n":  # the fine-aggregate normal tilts up to 70 degrees: at street level on a rough-but-lit surface it ripples like water. Halve the tilt in the texture
                v = resize(data_n := srcs["n"], size, "n") * 2 - 1
                v[..., :2] *= .5
                v /= np.maximum(np.linalg.norm(v, axis=2, keepdims=True), 1e-6)
                img = v * .5 + .5
            elif kind == "m" and srcs.get("m") is None:  # asphalt: derived
                a512 = resize(srcs["a"], size, "a")
                n512 = resize(srcs["n"], size, "n")
                img = asphalt_mask(a512, n512)
            else:
                data = srcs["a" if kind == "a" else kind]
                img = resize(data, size, kind) if data.shape[:2] != size[::-1] else data
            save(OUT / name, img)
            (OUT / f"{name}.meta").write_text(meta_text(name, kind, size))
            bytes_ = os.path.getsize(OUT / name)
            texels = size[0] * size[1]
            files.append(dict(file=name, size=list(size), kind=kind, texels=texels, bc7MiB=round(texels * 4 / 3 / 2 ** 20, 2), pngBytes=bytes_, guid=tex_guid(name)))
            tot_texels += texels
        after = mean_hex(np.asarray(Image.open(OUT / f"{stem}.png").convert("RGB")).astype(np.float32) / 255) if role != "asphalt_detail" else None
        rows[role] = dict(tile=list(p["tile"]), source=p["src"] or "generated here (seeded numpy)", meanBefore=before, meanAfter=after,
                          target=TARGET.get(key, (None, ""))[0] if role != "asphalt_detail" else None, files=files)
    reused = [dict(file=f"{n}{s}.png", size=list(sz), texels=sz[0] * sz[1], bc7MiB=round(sz[0] * sz[1] * 4 / 3 / 2 ** 20, 2))
              for n, v in REUSED.items() for s, sz in zip(("", "_normal"), v)]
    new_mib = tot_texels * 4 / 3 / 2 ** 20
    re_mib = sum(r["bc7MiB"] for r in reused)
    table = dict(schema="suborned.ground/1", budgetMiB=BUDGET_MIB, newMiB=round(new_mib, 2), reusedMiB=round(re_mib, 2), totalMiB=round(new_mib + re_mib, 2),
                 note="BC7 / BC5 are 1 byte a texel; mips add 1/3. Textures are normal PNGs here; Unity compresses on import (Standalone override in each .meta).",
                 roles=rows, reused=reused)
    (OUT / "ground.json").write_text(json.dumps(table, indent=1))
    print(f"[ground] {len(rows)} roles, new {new_mib:.1f} MiB + reused terrazzo {re_mib:.1f} MiB = {new_mib + re_mib:.1f} MiB (budget {BUDGET_MIB}) -> {OUT}")
    return table


if __name__ == "__main__":
    build()
