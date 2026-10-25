# Glyph, brief 063 / 062 (2026-10-25): Vale B's portraits into the kit.
#   python3 -I briefs/063/cameos/portraits_kit.py          -> builds into briefs/063/cameos/kit/ (the install copies from there)
#   python3 -I briefs/063/cameos/portraits_kit.py install  -> copies into unity/Assets/Suborned/UI/Resources/Kit/portraits/ (+ .meta if absent)
# Outputs (all @2x only, the way Sp() loads them: "portraits/<name>" -> Kit/portraits/<name>@2x.png):
#   card_councillor@2x.png            720 x 592 RGBA, the file-card photograph: the painted B crop as a duotone ink-deep #0b1615 -> cream #f7f4ec
#                               (a derived tone, no new colour), 20 px (10 kit px) rounded corners in the alpha. The ink-deep rim is the element behind it.
#   card_councillor@1x.png            360 x 296 (kept beside it for the kit's @1x/@2x pair; the game reads the @2x).
#   face_waitress@2x.png        64 px, Inez A on the roster's grey #b3b0ad, full-bleed square, no ring (Cameo() draws the chrome ring).
#   face_driver@2x.png          64 px, the driver A, same.
#   face_councillor@2x.png      64 px, Vale B painted (Intel, feed, the briefing's way cards).
#   face_councillor_model@2x.png 64 px, Forma's model cameo of the new figure (the live face: the carried Vale, a roster-style chip).
#   cameo_{councillor,waitress,driver}_normal@2x.png, cameo_councillor_model@2x.png  68 px, the same faces with the cell's own ring baked in (drop-ins for portraits/cameo_{who}_normal).
# No colour is added: the ground is the roster's grey, the duotone ends are kit colours.
import os, sys, shutil, uuid
from PIL import Image, ImageDraw, ImageChops
import numpy as np, cv2
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "kit"); os.makedirs(OUT, exist_ok=True)
KIT = os.path.abspath(os.path.join(HERE, "../../../unity/Assets/Suborned/UI/Resources/Kit/portraits/"))
GREY = np.array([179, 176, 173], np.float32); INKDEEP = np.array([11, 22, 21]) / 255.0; CREAM = np.array([247, 244, 236]) / 255.0

def faces():
    # painted faces: flat.py's output (the paper band removed, roster grey in), the 128 px master, down to 64 with a touch of sharpening
    for src, name in (("vale_B", "face_councillor"), ("inez_A", "face_waitress"), ("driver_A", "face_driver")):
        im = Image.open(os.path.join(HERE, "flat", src + "_face@4x.png")).convert("RGB")
        im = im.resize((64, 64), Image.LANCZOS); im.save(os.path.join(OUT, name + "@2x.png"))
    # the model cameo: the disc's ring (r > 238) and corners are the ground
    a = np.array(Image.open(os.path.join(HERE, "vale_model.png")).convert("RGBA")).astype(np.float32)
    ys, xs = np.mgrid[0:512, 0:512]; r = np.hypot(xs - 255.5, ys - 255.5)
    soft = np.clip((r - 232.0) / 6.0, 0, 1)[..., None]; rgb = a[..., :3] * (1 - soft) + GREY * soft
    c = Image.fromarray(np.clip(rgb, 0, 255).astype("uint8")).crop((8, 8, 504, 504)).resize((64, 64), Image.LANCZOS)
    c.save(os.path.join(OUT, "face_councillor_model@2x.png"))

def baked():
    """the ring-baked 68 px cameos, the same pixels as the cell's cameo_*_normal@2x (3 px clear margin, 2 px #d2dcdc light ring, 1 px #28 46 46 line, the face inside):
    drop-ins for every `portraits/cameo_{who}_normal` read. The ring-less `face_*` files above are for Cameo(), which draws its own chrome ring."""
    Q = 8; n = 68 * Q; yy, xx = np.mgrid[0:n, 0:n]; r = np.hypot(xx - n / 2 + 0.5, yy - n / 2 + 0.5) / Q
    for src, dst in (("face_councillor", "cameo_councillor_normal"), ("face_waitress", "cameo_waitress_normal"), ("face_driver", "cameo_driver_normal"), ("face_councillor_model", "cameo_councillor_model")):
        f = Image.open(os.path.join(OUT, src + "@2x.png")).convert("RGB").resize((n, n), Image.LANCZOS); fa = np.array(f).astype(np.float32)
        out = np.zeros((n, n, 4), np.float32); out[r < 30.5] = (210, 220, 220, 255); out[r < 28.5] = (40, 70, 70, 255)
        # the face fills a 55 px disc: scale the 64 px square to 56 and centre it
        sm = 56 * Q; fi = np.array(Image.fromarray(fa.astype("uint8")).resize((sm, sm), Image.LANCZOS)).astype(np.float32); o = (n - sm) // 2
        inner = r < 27.5; blk = np.zeros((n, n, 3), np.float32); blk[o:o + sm, o:o + sm] = fi
        out[inner, :3] = blk[inner]; out[inner, 3] = 255
        Image.fromarray(out.astype("uint8"), "RGBA").resize((68, 68), Image.LANCZOS).save(os.path.join(OUT, dst + "@2x.png"))

def card():
    a = np.array(Image.open(os.path.join(HERE, "vale_card_painted_720x592.png")).convert("RGB")).astype(np.float32)
    a[:16] = a[16:17]                                  # the crop's top 16 rows are the plate's darker paper edge: replicate the row below
    L = (0.30 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2]) / 255.0
    lo, hi = np.percentile(L, 0.5), np.percentile(L, 99.5)
    L = np.clip((L - lo) / (hi - lo), 0, 1) ** 0.78      # the skin is dark: lift the mid-tones so the eyes and the beard keep their edges
    rng = np.random.default_rng(3); L = np.clip(L + cv2.GaussianBlur(rng.normal(0, 0.05, L.shape).astype(np.float32), (0, 0), 0.7), 0, 1)
    out = INKDEEP[None, None, :] * (1 - L[..., None]) + CREAM[None, None, :] * L[..., None]
    h, w = L.shape; yy, xx = np.mgrid[0:h, 0:w]; rr = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h * 0.45) / (h / 2)) ** 2)
    out = out * (1 - 0.30 * np.clip(rr - 0.65, 0, 1))[..., None]   # the file print's vignette (darker at the edges, never past ink-deep)
    rgb = Image.fromarray((np.clip(out, 0, 1) * 255).astype("uint8")).convert("RGBA")
    m = Image.new("L", (w * 4, h * 4), 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, w * 4 - 1, h * 4 - 1), radius=20 * 4, fill=255)
    rgb.putalpha(m.resize((w, h), Image.LANCZOS)); rgb.save(os.path.join(OUT, "card_councillor@2x.png"))
    rgb.resize((360, 296), Image.LANCZOS).save(os.path.join(OUT, "card_councillor@1x.png"))

def meta_for(name):
    """a sprite .meta like the kit's own (the disc.py recipe: the patrol face's meta as template, new guid and sprite ids, this file's name and size)"""
    import re, random
    m = open(os.path.join(KIT, "../badges/npc_patrol_face@2x.png.meta")).read()
    w, h = Image.open(os.path.join(OUT, name)).size; stem = name[:-4]
    m = re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, m, count=1)
    m = m.replace("npc_patrol_face@2x_0", stem + "_0")
    m = re.sub(r"width: 48\n(\s+)height: 48", "width: %d\n\\1height: %d" % (w, h), m)
    m = re.sub(r"spriteID: \w+", lambda _: "spriteID: " + uuid.uuid4().hex[:24] + "08000000", m)
    m = re.sub(r"internalID: -?[1-9]\d*", "internalID: %d" % random.Random(stem).randint(-2**62, 2**62), m)
    return m

def install():
    for n in ("card_councillor@2x.png", "face_councillor@2x.png", "face_waitress@2x.png", "face_driver@2x.png", "face_councillor_model@2x.png",
              "cameo_councillor_normal@2x.png", "cameo_waitress_normal@2x.png", "cameo_driver_normal@2x.png", "cameo_councillor_model@2x.png"):
        d = os.path.join(KIT, n); shutil.copyfile(os.path.join(OUT, n), d)
        if not os.path.exists(d + ".meta"): open(d + ".meta", "w").write(meta_for(n)); print("meta", n)
        else: print("kept meta", n)

if __name__ == "__main__":
    faces(); baked(); card(); print("built ->", OUT)
    if "install" in sys.argv: install()
