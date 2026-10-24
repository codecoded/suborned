# Glyph, brief 057 part 2: the patrol cameo disc, `badges/npc_patrol`.
#   python3 -I briefs/057/patrol/disc.py template            -> patrol/npc_disc_template@2x.png (the npc_* disc with no glyph)
#   python3 -I briefs/057/patrol/disc.py build [FACE.png]    -> patrol/npc_patrol@2x.png (face fitted in the slot; no FACE = the stand-in screen)
#   python3 -I briefs/057/patrol/disc.py build FACE.png --fill  -> face art that is a portrait (fills the field like the cell's cameos) instead of a glyph
#   python3 -I briefs/057/patrol/disc.py install             -> copies both outputs to Kit/badges/ (+ .meta if absent)
# Two outputs, one face:
#   npc_patrol@2x.png       48 px, the whole badge with its baked bezel: where an npc_* disc shows as it is (known-people discs, action panel header, Intel).
#   npc_patrol_face@2x.png  48 px, the field only, square and full-bleed with no bezel: for the HUD's Cameo() helper, which draws the chrome ring itself
#                           (2 px #cbc6c3 over 1 px ink-deep, as the person pills' faces), so the ring is never doubled (Reroute pill, wait-spot cameo, edge chip).
# The disc is the kit's own: the five npc_* badges share one 48 px bezel (chrome ring, ink-deep outer line) and one cream enamel field,
# so the template is the per-pixel brightest of the five (ink glyphs on cream: where one badge has a glyph another has the field).
# Nothing new in colour: the field, the ring and the ink glyph are the kit's pixels; only the glyph layer changes.
import sys, os, shutil, uuid, re
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
KIT = os.path.abspath(os.path.join(HERE, "../../../unity/Assets/Suborned/UI/Resources/Kit/badges/"))
NPC = ["npc_councillor", "npc_driver", "npc_passer", "npc_waitress", "npc_watcher"]
INK = (23, 48, 47)          # #17302f, the glyph ink in every npc_* badge
CREAM = (247, 244, 236)     # #f7f4ec, dots on the screen
S = 8                        # supersample for the stand-in glyph
# the slot, measured on the 48 px badge (@2x): centre (24, 24); field radius 17 (bezel from 17 to 22.5); glyph safe circle radius 14.5
SLOT = {"size": 48, "centre": [24, 24], "field_radius": 17.0, "glyph_safe_radius": 14.5, "glyph_box": [9.5, 9.5, 38.5, 38.5],
        "ink": "#17302f", "note": "face art: ink-on-cream like the other npc_* glyphs (the screen is ink, the dots cream); fits the 29 px box, centred, no rim of its own"}

def load(n): return np.array(Image.open(os.path.join(KIT, n + "@2x.png")).convert("RGBA")).astype(float)

def template():
    st = np.stack([load(n) for n in NPC]); luma = st[..., :3].sum(-1)
    pick = np.argmax(luma, axis=0)            # glyphs are ink on cream: the brightest of the five at each pixel is the field
    med = np.take_along_axis(st, pick[None, ..., None].repeat(4, -1), axis=0)[0]
    # the field is a vertical enamel gradient with a little dither: inside the glyph circle each row takes the 75th percentile of its own pixels
    # (the glyph halos are the dark ones), blended back to the composite over the last two px so the inner bezel edge stays the kit's own
    yy, xx = np.mgrid[0:48, 0:48]; rad = np.hypot(xx - 23.5, yy - 23.5)
    out = med.copy(); rv = np.zeros((48, 4)); has = np.zeros(48, bool)
    for y in range(48):
        ok = rad[y] < 15.0
        if ok.sum() >= 3: rv[y] = np.percentile(med[y][ok], 75, axis=0); has[y] = True
    sm = rv.copy(); rows = np.where(has)[0]            # the enamel is a smooth vertical gradient: a degree-4 fit per channel removes the row jitter
    for c in range(4): sm[:, c] = np.polyval(np.polyfit(rows, rv[rows, c], 4), np.arange(48))
    for y in range(48):
        if not has[y]: continue
        rowv = sm[y]
        for x in np.where(rad[y] < 17.5)[0]:
            w = 1.0 if rad[y, x] < 15.0 else max(0.0, (17.5 - rad[y, x]) / 2.5)
            if med[y, x, :3].sum() < 640 or w > 0: out[y, x] = w * rowv + (1 - w) * med[y, x] if w < 1 else rowv
    med = out
    return Image.fromarray(np.clip(med, 0, 255).astype("uint8"), "RGBA")

def screen_glyph(big=False):
    """stand-in face, drawn like the in-game patrol: an ink screen (rounded rectangle) with a cream dot-matrix smile (two dot eyes, a smile),
    antenna nub on top; sized to the glyph box. Forma's art replaces it."""
    n = 48 * S; im = Image.new("RGBA", (n, n), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = 9.5, 14.0, 38.5, 36.5
    d.rounded_rectangle((x0 * S, y0 * S, x1 * S, y1 * S), radius=4.2 * S, fill=INK + (255,))
    d.rectangle((23.1 * S, 10.8 * S, 24.9 * S, 15 * S), fill=INK + (255,)); d.ellipse((21.6 * S, 8.6 * S, 26.4 * S, 13.4 * S), fill=INK + (255,))   # the mast and its lamp
    r = 1.45
    def dot(cx, cy): d.ellipse(((cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S), fill=CREAM + (255,))
    # eyes: a 2 x 2 cluster each (dot-matrix), smile: five dots on an arc
    for ex in (16.4, 31.6):
        for dx in (-1.7, 1.7):
            for dy in (-1.7, 1.7): dot(ex + dx, 21.2 + dy)
    for cx, cy in ((17.4, 28.4), (20.7, 30.4), (24.0, 31.0), (27.3, 30.4), (30.6, 28.4)): dot(cx, cy)
    return im if big else im.resize((48, 48), Image.LANCZOS)

def fit_face(face, box=None, fill=False, clip_r=17.0):
    """Forma's face -> a 48 px layer: scaled to fit the glyph box (aspect kept), centred, clipped to the field radius.
    fill=True (a portrait): scaled to cover the whole field circle instead."""
    box = box or SLOT["glyph_box"]
    f = Image.open(face).convert("RGBA"); bw, bh = box[2] - box[0], box[3] - box[1]
    if fill: bw = bh = 2 * clip_r
    k = (max if fill else min)(bw * S / f.width, bh * S / f.height); f = f.resize((max(1, round(f.width * k)), max(1, round(f.height * k))), Image.LANCZOS)
    big = Image.new("RGBA", (48 * S, 48 * S), (0, 0, 0, 0)); big.alpha_composite(f, (round(24 * S - f.width / 2), round(24 * S - f.height / 2)))
    m = Image.new("L", big.size, 0); ImageDraw.Draw(m).ellipse(((24 - clip_r) * S, (24 - clip_r) * S, (24 + clip_r) * S, (24 + clip_r) * S), fill=255)
    big.putalpha(Image.fromarray(np.minimum(np.array(big)[..., 3], np.array(m)).astype("uint8")))
    return big.resize((48, 48), Image.LANCZOS)

def build(face=None, fill=False):
    """the badge: template (field + bezel) with the face on top"""
    base = template(); g = fit_face(face, fill=fill) if face else screen_glyph()
    out = base.copy(); out.alpha_composite(g); return out

def build_face(face=None, fill=False, k=1.1):
    """the bezel-less face for Cameo(): a full-bleed square of the field (its vertical enamel gradient, taken from the template's middle columns)
    with the glyph k times larger than on the badge (the chrome ring Cameo() draws takes the outer 2 to 3 px at 24 px)."""
    t = np.array(template()).astype(float); out = np.zeros((48, 48, 4))
    for y in range(48):
        ty = int(round(min(40, max(8, 24 + (y - 24) / 1.5)))); out[y, :] = np.median(t[ty, 19:29], axis=0)
    out[..., 3] = 255
    out = Image.fromarray(out.astype("uint8"), "RGBA")
    if face: g = fit_face(face, box=[24 - 14.5 * k, 24 - 14.5 * k, 24 + 14.5 * k, 24 + 14.5 * k], fill=fill, clip_r=24.0)
    else:
        big = screen_glyph(big=True); n = 48 * S
        g = big.transform(big.size, Image.AFFINE, (1 / k, 0, n / 2 - n / 2 / k, 0, 1 / k, n / 2 - n / 2 / k), resample=Image.BICUBIC).resize((48, 48), Image.LANCZOS)
    out.alpha_composite(g); return out

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "build"
    if cmd == "template": template().save(os.path.join(HERE, "npc_disc_template@2x.png"))
    elif cmd == "build":
        face = next((x for x in sys.argv[2:] if not x.startswith("--")), None); fill = "--fill" in sys.argv
        build(face, fill).save(os.path.join(HERE, "npc_patrol@2x.png")); build_face(face, fill).save(os.path.join(HERE, "npc_patrol_face@2x.png"))
    elif cmd == "install":
        for nm in ("npc_patrol@2x.png", "npc_patrol_face@2x.png"):
            dst = os.path.join(KIT, nm); shutil.copy(os.path.join(HERE, nm), dst)
            if not os.path.exists(dst + ".meta"):
                m = open(os.path.join(KIT, "npc_watcher@2x.png.meta")).read()
                m = re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, m, count=1)
                m = re.sub(r"  internalIDToNameTable:\n  - first:\n.*\n    second: .*\n", "  internalIDToNameTable: []\n", m)
                m = m.replace("npc_watcher@2x_0", nm.replace(".png", "") + "_0"); m = re.sub(r"spriteID: \w+", "spriteID: " + uuid.uuid4().hex[:24] + "08000000", m)
                open(dst + ".meta", "w").write(m)
    print("ok", cmd)
