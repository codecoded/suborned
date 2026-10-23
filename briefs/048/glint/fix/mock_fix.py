# Glint, brief 048 fixes 1-3: before/after crops on the real build frames.  python3 -I mock_fix.py
# NOT an engine render of the city (the city scene is Latch's; Glint does not touch it). Two real-engine parts feed it, both rendered in the scratch
# project out/glint048/shaderproj with the runtime data (Resources/Atmosphere) before and after:
#   - the play grade: a 12 x 12 x 12 sRGB colour cube through the play-tier camera, old data vs new data -> a measured per-colour delta, applied to
#     the real play frame (hold-watch-reach, graded with the OLD data) by nearest-neighbour inverse distance in the old output space; HUD pixels kept.
#   - the haze: the horizon sky/fog colour rendered before and after -> a shift applied where the real frame is fog-dominated (the pixel's distance
#     from the old rendered haze colour estimates the fog share). The fog share is an estimate; read it for the look and the amounts.
import numpy as np
from PIL import Image, ImageDraw
SP = '/Users/sky/Projects/suborned/out/glint048/shaderproj/'
B = '/Users/sky/Projects/suborned/briefs/048/'
def px(a): return np.asarray(a).astype(np.float32) / 255
def chart(d):
    im = px(Image.open(SP + d + '/chart-on.png').convert('RGB'))
    out = np.zeros((1728, 3), np.float32)
    for i in range(1728):
        cx, cy = 160 + (i % 48) * 20 + 10, (i // 48) * 20 + 10
        out[i] = im[cy, cx]
    return out
old, new = chart('before'), chart('after')
src = np.array([[(i % 12), (i // 12) % 12, i // 144] for i in range(1728)], np.float32) / 11
print('chart check: black', old[0], new[0], 'white', old[-1], new[-1])
delta = new - old
def regrade(a, k=6):
    h, w, _ = a.shape; flat = a.reshape(-1, 3); out = np.empty_like(flat)
    for s in range(0, len(flat), 40000):
        c = flat[s:s + 40000]
        d2 = ((c[:, None, :] - old[None, :, :]) ** 2).sum(-1)
        idx = np.argpartition(d2, k, axis=1)[:, :k]
        dd = np.take_along_axis(d2, idx, 1) + 1e-5
        wgt = 1 / dd; wgt /= wgt.sum(1, keepdims=True)
        out[s:s + 40000] = c + (delta[idx] * wgt[..., None]).sum(1)
    return np.clip(out.reshape(h, w, 3), 0, 1)
# rendered horizon haze colour (scratch, level camera, sky just above the horizon)
def horizon(d): return px(Image.open(SP + d + '/on-level.png').convert('RGB'))[395, 100]
Ho, Hn = horizon('before'), horizon('after'); print('haze rendered', Ho, Hn)
def rehaze(a, d0=0.2):
    f = np.clip(1 - np.linalg.norm(a - Ho[None, None, :], axis=-1) / d0, 0, 1) ** 1.2
    return np.clip(a + f[..., None] * (Hn - Ho)[None, None, :], 0, 1)
def label(a, t):
    im = Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 520, 24], fill=(23, 48, 47)); d.text((8, 6), t, fill=(247, 244, 236)); return px(im)
def pair(a, b, name, ta, tb, vertical=False):
    a, b = label(a, ta), label(b, tb)
    s = np.concatenate([a, b], axis=0 if vertical else 1)
    Image.fromarray((s * 255 + .5).astype(np.uint8)).save(name); print('wrote', name)
# 1. tp-street-long: haze (a crop of the far band and towers)
im = Image.open(B + 'build/tp-street-long.jpg').convert('RGB'); im = im.resize((1440, int(im.height * 1440 / im.width)), Image.LANCZOS)
a = px(im); crop = lambda x: x[60:560]
pair(crop(a), crop(rehaze(a)), 'long-before-after.png', 'BEFORE  haze #ecd9b4 (build 0.2.79 frame)', 'AFTER  haze #f4e3c3 (mock on the frame)', vertical=True)
# 2. hold-watch-reach: the play grade
p = Image.open('/Users/sky/Projects/suborned/out/scratch/latch/proof2160/hold-watch-reach.png').convert('RGB'); p = p.resize((1600, int(p.height * 1600 / p.width)), Image.LANCZOS)
m = Image.open('/Users/sky/Projects/suborned/out/scratch/latch/proof2160/hold-watch-reach.hud.png'); m = np.asarray(m.convert('RGBA').resize(p.size, Image.NEAREST))[..., 3] > 0
a = px(p); g = regrade(a); g[m] = a[m]
c = lambda x: x[80:800, 200:1400]
pair(c(a), c(g), 'grade-before-after.png', 'BEFORE  grade sat 16, lift warmth 0.6 #e9d9b8', 'AFTER  sat 14, lift warmth 0.15 #f2cfc2 (mock)', vertical=True)
# 3. teal: the real street frame at the avenue end, haze, next to the scratch engine teal sky
t = Image.open(B + 'build/tp-street-teal.jpg').convert('RGB'); t = t.resize((1440, int(t.height * 1440 / t.width)), Image.LANCZOS)
a = px(t); crop = lambda x: x[150:560, 400:1100]
sb, sa = px(Image.open(SP + 'before/teal-level.png').convert('RGB')), px(Image.open(SP + 'after/teal-level.png').convert('RGB'))
sb = np.asarray(Image.fromarray((sb * 255).astype(np.uint8)).resize((700, 394)))/255; sa = np.asarray(Image.fromarray((sa * 255).astype(np.uint8)).resize((700, 394)))/255
fa, fb = crop(a), crop(rehaze(a))
top = np.concatenate([label(fa[:394], 'BEFORE  real frame, teal sky'), label(fb[:394], 'AFTER  haze (mock on the frame)')], axis=1)
bot = np.concatenate([label(sb, 'BEFORE  engine render, teal sky + haze (scratch)'), label(sa, 'AFTER  engine render (scratch)')], axis=1)
Image.fromarray((np.concatenate([top, bot], axis=0) * 255 + .5).astype(np.uint8)).save('teal-before-after.png'); print('wrote teal-before-after.png')
