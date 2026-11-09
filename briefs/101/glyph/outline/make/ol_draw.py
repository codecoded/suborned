"""Brief 101, hover outline (Glyph): the outline drawn from a thing's mask as Unity's edge pass would draw it, at 4x then down to the frame
(anti-aliased), in the three treatments:
  A  lit edge     : a cream line on the thing's own edge pixels (inside), and the thing lifted a little; never on a neighbour;
  B  ink keyline  : an ink line just outside the edge (1.5 px), with a fine cream keyline outside that (the street labels' keyline), crisp;
  C  turquoise    : a turquoise-light line outside the edge with a soft glow; shows faintly through what covers the thing.
Every line sits at the thing's own depth: what stands in front of the thing hides the line too (the `front` masks), except C's x-ray part.
Widths are in 1080p px; `k` is the frame's px per 1080p px (1 for a 1920 x 1080 capture, 1.726 for Sky's 2880 x 1864 one). Glyph. Mock only."""
import numpy as np, cv2
from PIL import Image

INK = (0x17, 0x30, 0x2f); INK_DEEP = (0x0b, 0x16, 0x15); CREAM = (0xf7, 0xf4, 0xec); TURQ = (0x60, 0xe0, 0xea)  # kit.uss / the art bible's HUD colours
SS = 4  # supersampling

STYLES = {
    'A': dict(kind='inner', col=CREAM, w=1.5, alpha=0.95, lift=0.07, xray=0.0),
    'B': dict(kind='outer', col=INK, w=1.5, alpha=0.92, key=CREAM, kw=1.0, key_alpha=0.85, xray=0.0),
    'C': dict(kind='glow', col=TURQ, w=1.5, alpha=1.0, glow=5.0, glow_alpha=0.45, xray=0.35),
}


def _up(m, s=SS): return cv2.resize(m.astype(np.float32), (m.shape[1] * s, m.shape[0] * s), interpolation=cv2.INTER_LINEAR)


def _down(a, shape): return cv2.resize(a, (shape[1], shape[0]), interpolation=cv2.INTER_AREA)


def _band(d, lo, hi):
    """Coverage of the distance band [lo, hi] (px at 4x), with a one-pixel soft edge at each side."""
    return np.clip(d - lo + 0.5, 0, 1) * np.clip(hi - d + 0.5, 0, 1)


def fields(full, crop):
    """Distance outside and inside the mask (px at 4x) over a crop box (x0, y0, x1, y1) of the frame."""
    x0, y0, x1, y1 = crop; m = _up(full[y0:y1, x0:x1]) > 0.5
    out = cv2.distanceTransform((~m).astype(np.uint8), cv2.DIST_L2, 5); inn = cv2.distanceTransform(m.astype(np.uint8), cv2.DIST_L2, 5)
    return m, out, inn


def layer(full, style, k=1.0, front=None, pad=24):
    """The outline as an RGBA float layer the frame's size (premultiplied colour, alpha). `front`: what stands in front of the thing (0..1)."""
    st = STYLES[style] if isinstance(style, str) else style; H, W = full.shape
    ys, xs = np.where(full > 0.02)
    if len(xs) == 0: return np.zeros((H, W, 4), np.float32)
    p = int(pad * k) + 4; x0, y0, x1, y1 = max(0, xs.min() - p), max(0, ys.min() - p), min(W, xs.max() + p + 1), min(H, ys.max() + p + 1)
    m, out, inn = fields(full, (x0, y0, x1, y1)); s = SS * k; cols, alps = [], []
    if st['kind'] == 'inner':
        cols.append(st['col']); alps.append(_band(inn, 0.5, st['w'] * s) * m * st['alpha'])
    elif st['kind'] == 'outer':
        w = st['w'] * s; cols.append(st['col']); alps.append(_band(out, 0.5, w + 0.5) * st['alpha'])
        if st.get('key'): cols.append(st['key']); alps.append(_band(out, w + 0.5, w + 0.5 + st['kw'] * s) * st['key_alpha'])
    else:  # glow: a soft halo under a crisp line
        w = st['w'] * s; g = np.exp(-((np.maximum(out - w, 0)) / (st['glow'] * s)) ** 2) * (out > 0) * st['glow_alpha']
        cols.append(st['col']); alps.append(g); cols.append(st['col']); alps.append(_band(out, 0.5, w + 0.5) * st['alpha'])
    L = np.zeros((y1 - y0, x1 - x0, 4), np.float32)
    for c, a in zip(cols, alps):
        a = _down(a, (y1 - y0, x1 - x0)); c = np.array(c, np.float32) / 255
        L[:, :, :3] = L[:, :, :3] * (1 - a[:, :, None]) + c * a[:, :, None]; L[:, :, 3] = L[:, :, 3] * (1 - a) + a
    if front is not None:  # the line sits at the thing's depth: what stands in front hides it (C keeps a faint x-ray)
        f = np.clip(front[y0:y1, x0:x1], 0, 1); keep = 1 - f * (1 - st.get('xray', 0.0)); L[:, :, :3] *= keep[:, :, None]; L[:, :, 3] *= keep
    out_ = np.zeros((H, W, 4), np.float32); out_[y0:y1, x0:x1] = L; return out_


def lift(frame, vis, amount):
    """The thing's own pixels lifted toward cream (A's 'lights a little'), on what is visible of it."""
    f = frame.astype(np.float32) / 255; a = (vis * amount)[:, :, None]; c = np.array(CREAM, np.float32) / 255
    return (np.clip(f * (1 - a) + c * a, 0, 1) * 255).astype(np.uint8)


def compose(frame, layers, hud=None, base=None):
    """The frame with the outline layers over it, under today's HUD: the HUD draws after the world, so where it is opaque the capture's own
    pixels stay, and where it is see-through the outline shows by (1 - alpha). `base` is the world to draw on (lifted for A), else the frame.
    (The .hud.png stores tinted sprites untinted, so the HUD's colour is taken from the capture itself, never from the layer.)"""
    f0 = frame.astype(np.float32) / 255; f = (base if base is not None else frame).astype(np.float32) / 255
    for L in layers: f = f * (1 - L[:, :, 3:]) + L[:, :, :3]
    if hud is not None:
        a = hud[:, :, 3:].astype(np.float32) / 255; f = f0 + (f - f0) * (1 - a)
    return (np.clip(f, 0, 1) * 255).astype(np.uint8)


def cursor(img, x, y, k=1.0):
    """The OS pointer, its tip at (x, y): white with an ink edge, 19 px tall at 1080p (drawn at 4x, then down)."""
    pts = np.array([(0, 0), (0, 17), (4.2, 13.2), (7.2, 19.6), (10, 18.4), (7.1, 12.2), (12.4, 12.2)], np.float32) * k
    s = SS; pad = int(26 * k); x0, y0 = int(x) - 4, int(y) - 4; W, H = pad + 8, pad + 8
    big = np.zeros((H * s, W * s), np.uint8); edge = big.copy(); P = ((pts + (x - x0, y - y0)) * s).astype(np.int32)
    cv2.fillPoly(big, [P], 255); cv2.polylines(edge, [P], True, 255, thickness=max(1, int(round(2.4 * k * s))), lineType=cv2.LINE_AA); cv2.fillPoly(edge, [P], 255)
    e = _down(edge.astype(np.float32) / 255, (H, W)); b = _down(cv2.erode(big, np.ones((3, 3), np.uint8)).astype(np.float32) / 255, (H, W))
    reg = img[y0:y0 + H, x0:x0 + W].astype(np.float32); hh, ww = reg.shape[:2]; e, b = e[:hh, :ww, None], b[:hh, :ww, None]
    reg = reg * (1 - e) + np.array(INK_DEEP, np.float32) * e; reg = reg * (1 - b) + 255 * b; img[y0:y0 + H, x0:x0 + W] = reg.astype(np.uint8); return img
