# Glint, brief 048 "Old city atmosphere": MOCKS of the proposed atmosphere over real captures. No engine render (Latch owns the editor).
# Inputs are display-referred screenshots, treated as scene-linear after the sRGB decode, so this is a look sketch, not a measurement.
# Depth is ESTIMATED (ground plane analytic + hand bands for standing masses), as in mock.py. Read it for the look and the amounts.
#   python3 -I atmos_mock.py <outdir>
import sys, math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = sys.argv[1]
R = '/Users/sky/Projects/suborned'
W, H = 1920, 1080

def lin(c): return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def srgb(c):
    c = np.clip(c, 0, None)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
def hexc(h): return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], np.float32) / 255.0
def load(p, size=(W, H)):
    im = Image.open(p).convert('RGB')
    if im.size != size: im = im.resize(size, Image.LANCZOS)
    return np.asarray(im).astype(np.float32) / 255.0
def save(a, p): Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8)).save(p)

def blur(a, r):
    """gaussian of an HDR float image (3 ch) by 8-bit passes of value/scale"""
    s = max(float(a.max()), 1.0)
    im = Image.fromarray((np.clip(a / s, 0, 1) * 255).astype(np.uint8))
    sm = im.resize((max(1, im.width // 4), max(1, im.height // 4)), Image.BILINEAR).filter(ImageFilter.GaussianBlur(r / 4))
    return np.asarray(sm.resize(im.size, Image.BILINEAR)).astype(np.float32) / 255.0 * s

def bloom(c, thr, inten, scatter, tint):
    lum = c @ np.array([.2126, .7152, .0722], np.float32)
    bright = c * np.clip((lum - thr) / max(1e-3, 1.0 - thr), 0, 1.5)[..., None]
    g = 0
    for r, w in ((6, 1.0), (18, scatter + .2), (48, scatter + .1), (110, scatter)):
        g = g + blur(bright, r) * w
    return c + g * inten * tint[None, None, :]

def aces(x):  # Narkowicz fit: the grade is ACES in game; used here only to hold highlights
    x = np.clip(x, 0, None)
    return np.clip((x * (2.51 * x + .03)) / (x * (2.43 * x + .59) + .14), 0, 1)

def sat(c, s):
    l = (c @ np.array([.2126, .7152, .0722], np.float32))[..., None]
    return l + (c - l) * s

def label(a, text, h=34):
    img = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    try: ft = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 24)
    except Exception: ft = ImageFont.load_default()
    d.rectangle([0, img.height - h, img.width, img.height], fill=(23, 48, 47)); d.text((12, img.height - h + 4), text, fill=(247, 244, 236), font=ft)
    return np.asarray(img).astype(np.float32) / 255.0

def sheet(panels, texts, path, w=1280):
    ims = []
    for a, t in zip(panels, texts):
        im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS)
        ims.append(np.asarray(im).astype(np.float32) / 255.0)
        ims[-1] = label(ims[-1], t)
    hmax = max(i.shape[0] for i in ims)
    out = np.full((hmax * len(ims) + 8 * (len(ims) - 1), w, 3), 23 / 255, np.float32)
    y = 0
    for i in ims: out[y:y + i.shape[0]] = i; y += i.shape[0] + 8
    save(out, path)

# ---------------------------------------------------------------- the proposal as numbers (lens.json carries the same)
HAZE = hexc('#f3e8d4')            # contract palette_world.roles.haze (light.json has #f3eee4: 4 to 10 levels whiter)
HAZEW = hexc('#ecd9b4')            # proposed light.haze: the old horizon as it reads in the old renders (sand cream), not a near-white
HAZE_OLD = np.array([246, 229, 195], np.float32) / 255   # the old sky's horizon, DecoKitSky.Apply
TINT = hexc('#fff0d6')

def sky_gradient(e_sin, zen, mid, haze, mid_h=.95, haze_h=.10, aur=None):
    h = np.clip(e_sin, 0, 1)
    upper = mid + (zen - mid) * (np.clip(h / mid_h, 0, 1) ** 1.5)[..., None]
    hz = np.exp(-h / haze_h)[..., None]
    col = upper * (1 - hz) + haze * hz
    if aur is not None: col = col + aur
    return col

def atmosphere(im, depth, skymask, YH, F, fog, haze, zen, mid, gain=1.0, bl=(.8, .5, .7), grade_sat=1.08, haze_h=.10, dof=None, skyrebuild=True, flare=None):
    c = lin(im)
    hz = lin(haze) * gain
    kind, p = fog
    if kind == 'exp2': f = 1 - np.exp(-(depth * p) ** 2)
    elif kind == 'exp': f = 1 - np.exp(-depth * p)
    elif kind == 'lin': f = np.clip((depth - p[0]) / (p[1] - p[0]), 0, 1)
    else: f = np.zeros_like(depth)
    f = np.where(skymask, 0, f)[..., None]
    c = c * (1 - f) + hz[None, None, :] * f
    if dof is not None:  # gaussian far blur by depth: three levels blended (a sketch of URP Gaussian DoF)
        s, e, rad = dof
        t = np.clip((depth - s) / (e - s), 0, 1)[..., None]
        b1, b2 = blur(c, rad * 2.2), blur(c, rad * 5.0)
        c = np.where(t < .5, c * (1 - 2 * t) + b1 * 2 * t, b1 * (2 - 2 * t) + b2 * (2 * t - 1))
    if skyrebuild:
        ys = np.mgrid[0:im.shape[0], 0:im.shape[1]][0].astype(np.float32)
        e = np.sin(np.arctan(np.maximum(YH - ys, 0) / F))
        sk = lin(sky_gradient(e, zen, mid, haze, haze_h=haze_h)) * gain
        sk = np.where((ys[..., None] < YH + 1), sk, hz[None, None, :])
        # a soft key between sky and city so the rebuilt sky does not halo the roofline
        m = skymask[..., None].astype(np.float32)
        m = np.asarray(Image.fromarray((m[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6))).astype(np.float32)[..., None] / 255
        c = c * (1 - m) + sk * m
    thr, inten, sc = bl
    c = bloom(c, thr, inten, sc, TINT)
    if flare is not None:
        fx, fy, fi = flare
        yy, xx = np.mgrid[0:im.shape[0], 0:im.shape[1]].astype(np.float32)
        r = np.hypot(xx - fx, yy - fy)
        glow = (np.exp(-(r / 70) ** 2) * 1.2 + np.exp(-(r / 340) ** 1.2) * .35)[..., None] * fi
        ang = np.arctan2(yy - fy, xx - fx)
        star = (np.exp(-r / 420) * (np.abs(np.cos(ang * 8)) ** 40))[..., None] * .5 * fi   # 16-ray starburst
        c = c + (glow + star) * np.array([1.0, .93, .8], np.float32)[None, None, :]
    c = sat(c, grade_sat)
    return srgb(c / (1 + np.clip(c - .85, 0, None) * 1.1))   # soft shoulder: highlights roll off, nothing blows to flat white

# ---------------------------------------------------------------- boulevard / third person (today's tp-worst, the long avenue)
def boulevard():
    im = load(f'{R}/briefs/048/runs/tp-worst.jpg')
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    YH, F, CAMH = 415.0, 1037.0, 3.06   # follow cam: 55 deg vertical fov, 3.06 m up, pitched ~7 deg down
    ground = np.where(ys > YH + 1.5, CAMH * F / np.maximum(ys - YH, 1.5), 1e5)
    band = np.full((H, W), 1e5, np.float32)
    def box(x0, x1, y0, y1, d): band[int(y0):int(y1), int(x0):int(x1)] = d
    box(0, 280, 0, 470, 62); box(280, 700, 0, 440, 78); box(700, 810, 0, 440, 95)
    box(810, 1060, 120, 440, 215); box(1060, 1110, 0, 440, 145); box(1110, 1350, 0, 440, 108)
    box(1350, W, 0, H, 14)
    k = 31; ker = np.ones(k, np.float32) / k
    pad = np.pad(band, ((0, 0), (k // 2, k // 2)), mode='edge')
    cs = np.cumsum(np.pad(pad, ((0, 0), (1, 0))), axis=1); band = ((cs[:, k:] - cs[:, :-k]) / k).astype(np.float32)
    depth = np.minimum(band, ground)
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    sky = (b > r + .12) & (b > .55) & (ys < 440) & (xs > 790) & (xs < 1330)
    D = math.sqrt(-math.log(1 - .97)) / 350
    inforce = atmosphere(im, depth, sky, YH, F, ('exp2', D), hexc('#f3eee4'), hexc('#3f86de'), hexc('#8fc2ee'), gain=1.0, bl=(.95, .0, .5), grade_sat=1.0, skyrebuild=False)
    save(inforce, f'{OUT}/blvd-inforce-exp2.png')
    out = atmosphere(im, depth, sky, YH, F, ('lin', (90, 350)), HAZEW, hexc('#3f86de'), hexc('#8fc2ee'), gain=1.0, bl=(.86, .35, .7), grade_sat=1.07, haze_h=.07)
    # the sky above the avenue: the zenith colour is lit from the top of frame; keep today's blue up there
    save(im, f'{OUT}/blvd-today.png'); save(out, f'{OUT}/blvd-proposed.png'); return im, out

# ---------------------------------------------------------------- harbour, looking back (aerial: today's baseline skyline-aerial)
def harbour():
    im = load(f'{R}/briefs/026/baseline/skyline-aerial.png')
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    YH, F, CAMH = 480.0, 1037.0, 70.0          # rooftops about 70 m under the camera: roof-plane depth by row
    depth = np.where(ys > YH + 1.5, CAMH * F / np.maximum(ys - YH, 1.5), 3200.0)
    depth = np.clip(depth, 20, 2800)
    # sky: pixels that match the row's open-sky colour (read from the right-hand columns, above the sea)
    ref = np.median(im[:, 1560:1900, :], axis=1)             # H x 3
    close = np.abs(im - ref[:, None, :]).max(axis=2) < .075
    sky = (close & (ys < 478)) | (ys < 240)
    sky = np.asarray(Image.fromarray((sky * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))).astype(np.float32) > 128
    # horizon cards (left) and the far spires: kilometres out
    cards = (~sky) & (ys < 556) & (xs < 1000) & (ys > 330)
    depth = np.where(cards, 3200.0, depth)
    far = (~sky) & (ys < 480) & (xs >= 1000)
    depth = np.where(far, 1800.0, depth)
    depth = np.where(sky, 1e5, depth)
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    sea = (g > r + .03) & (b > r + .03) & (ys > 470) & (ys < 650) & (xs > 1100) & (~sky)
    save(im, f'{OUT}/harb-today.png')
    def sea_deep(c, d):   # the old water: deep teal-blue body, fresnel lift toward the haze with distance
        deep = lin(hexc('#0b4458')); k = (1 - np.exp(-d * .0012))[..., None]
        s = deep * (1 - k) + lin(HAZEW) * k * .8
        return np.where(sea[..., None], s, c)
    def run(zen, mid, haze, fog, dof, seaon=True):
        o = atmosphere(im, depth, sky, YH, F, fog, haze, zen, mid, gain=1.0, bl=(.9, .3, .85), grade_sat=1.1, haze_h=.12, dof=dof)
        return o
    prop = run(hexc('#3f86de'), hexc('#8fc2ee'), HAZEW, ('exp', .0005), (300, 1800, 3.0))
    propB = run(hexc('#2f93a8'), hexc('#6ec6c4'), HAZEW, ('exp', .0005), (300, 1800, 3.0))
    # the sea, recoloured on the finished image (a sketch): deep body, haze with distance
    def sea_on(o):
        k = np.clip((ys - YH) / 160.0, 0, 1)
        d = CAMH * F / np.maximum(ys - YH, 1.5)
        deep = hexc('#0a4f74'); hz = HAZEW
        f = (1 - np.exp(-d * .0005))[..., None]
        s = srgb(lin(deep)[None, None, :] * (1 - f) + lin(hz)[None, None, :] * f * .85)
        m = sea[..., None].astype(np.float32)
        m = np.asarray(Image.fromarray((m[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))).astype(np.float32)[..., None] / 255
        return o * (1 - m) + s * m
    prop = sea_on(prop); propB = sea_on(propB)
    save(prop, f'{OUT}/harb-proposed.png'); save(propB, f'{OUT}/harb-proposed-teal.png')
    save(sky.astype(np.float32)[..., None].repeat(3, 2), f'{OUT}/harb-skymask.png')
    return im, prop, propB

# ---------------------------------------------------------------- play zoom: unchanged by fog / DoF / flare; grade only
def playzoom():
    im = load(f'{R}/briefs/048/runs/overhead-open.jpg')
    save(im, f'{OUT}/play-today.png')
    # grade-only variant for the play camera (no fog, no DoF, no flare): saturation up, shadows lifted a little, warm wide bloom on the brightest stone only
    c = lin(im)
    c = bloom(c, .92, .22, .8, TINT)
    c = sat(c, 1.16)
    lum = (c @ np.array([.2126, .7152, .0722], np.float32))[..., None]
    lift = (.035 * np.clip(1 - lum / .25, 0, 1)) * lin(hexc('#e9d9b8'))[None, None, :] * 2.2     # a warm lift under about 0.25 linear
    c = c + lift
    out = srgb(c / (1 + np.clip(c - .88, 0, None) * 1.0))
    save(out, f'{OUT}/play-proposed.png')
    return im, out

def boards():
    old1 = load(f'{OUT}/../old/sky-still-1-boulevard.png', (2000, 1069)); old2 = load(f'{OUT}/../old/sky-still-2-harbour.png', (2000, 1138))
    ig = load(f'{OUT}/../old/iso-tactical.jpg')
    L = lambda n: load(f'{OUT}/{n}.png')
    sheet([old1, L('blvd-today'), L('blvd-inforce-exp2'), L('blvd-proposed')],
          ['OLD CITY flyby, "The boulevard" (Sky\'s still)', 'GAME TODAY: third person, long avenue, build 0.2.65 (real capture)',
           'MOCK: haze.json as written now (exp2, far clip 350, haze #f3eee4)', 'MOCK, proposed: linear 90 to 350 m, haze #ecd9b4, warm bloom, sky band'],
          f'{OUT}/../board-boulevard.png', 1100)
    sheet([old2, L('harb-today'), L('harb-proposed'), L('harb-proposed-teal')],
          ['OLD CITY flyby, "Out over the harbour, looking back" (Sky\'s still)', 'GAME TODAY: baseline skyline-aerial, 2026-10-19 (real capture)',
           'MOCK, proposed (flyby): exp .0005, haze #ecd9b4, bloom, far DoF, deeper sea',
           'MOCK, option B: the same with the old city\'s teal sky'],
          f'{OUT}/../board-harbour.png', 1100)
    sheet([ig, L('play-today'), L('play-proposed')],
          ['OLD CITY iso tactical (old project render; ortho, ACES, sat 25, bloom .6)', 'GAME TODAY: play zoom 14, yaw 30, build 0.2.65 (real capture)',
           'MOCK, play camera: no fog, DoF or flare; grade only (sat +16 %, warm lift)'],
          f'{OUT}/../board-playzoom.png', 1100)

if __name__ == '__main__' and False: pass

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    boulevard(); harbour(); playzoom(); boards()
