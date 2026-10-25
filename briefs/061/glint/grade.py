"""Glint, brief 061: the play camera's grade as URP 17 builds it (LutBuilderHdr ColorGrade + Tonemap, ACES), for a lit emissive pixel.
Mirrors Unity's own shader code (ACES.hlsl, Color.hlsl AcesTonemap): postExposure, WB skipped, contrast in ACEScc, colour filter, SMH (full-strength colours, luma weights),
saturation (luma of the ACEScg value), tonemap. Bloom, vignette, SSAO and the 3D LUT's interpolation are not modelled."""
import numpy as np
M = lambda *r: np.array(r, float).reshape(3, 3)
sRGB_2_AP0 = M(0.4397010, 0.3829780, 0.1773350, 0.0897923, 0.8134230, 0.0967616, 0.0175440, 0.1115440, 0.8707040)
AP0_2_AP1 = M(1.4514393161, -0.2365107469, -0.2149285693, -0.0765537734, 1.1762296998, -0.0996759264, 0.0083161484, -0.0060324498, 0.9977163014)
AP1_2_AP0 = M(0.6954522414, 0.1406786965, 0.1638690622, 0.0447945634, 0.8596711185, 0.0955343182, -0.0055258826, 0.0040252103, 1.0015006723)
AP1_2_XYZ = M(0.6624541811, 0.1340042065, 0.1561876870, 0.2722287168, 0.6740817658, 0.0536895174, -0.0055746495, 0.0040607335, 1.0103391003)
XYZ_2_AP1 = M(1.6410233797, -0.3248032942, -0.2364246952, -0.6636628587, 1.6153315917, 0.0167563477, 0.0117218943, -0.0082844420, 0.9883948585)
XYZ_2_709 = M(3.2409699419, -1.5373831776, -0.4986107603, -0.9692436363, 1.8759675015, 0.0415550574, 0.0556300797, -0.2039769589, 1.0569715142)
D60_2_D65 = M(0.98722400, -0.00611327, 0.0159533, -0.00759836, 1.00186000, 0.0053302, 0.00307257, -0.00509595, 1.0816800)
AP1_Y = np.array([0.272229, 0.674082, 0.0536895])
MID = 0.4135884
def s2l(h):
    c = np.array([int(h[i:i+2], 16) for i in (1, 3, 5)]) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def l2s(c):
    c = np.clip(c, 0, 1); return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
def hexof(c): return '#%02x%02x%02x' % tuple(int(round(v * 255)) for v in np.clip(l2s(c), 0, 1))
def to_cc(x): x = np.maximum(x, 1e-9); return np.where(x < 3.0517578e-5, (np.log2(1.52587890625e-5 + x * 0.5) + 9.72) / 17.52, (np.log2(x) + 9.72) / 17.52)
def from_cc(x): return np.where(x < -0.3013698630, (2 ** (x * 17.52 - 9.72) - 2 ** -16) * 2, 2 ** (x * 17.52 - 9.72))
def smooth(a, b, x): t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)
def aces_tm(acesv):
    sat_ = (max(acesv.max(), 1e-4) - max(acesv.min(), 1e-4)) / max(acesv.max(), 1e-2)
    r, g, b = acesv; k = max(b * (b - g) + g * (g - r) + r * (r - b), 0); yc = (r + g + b + 1.75 * np.sqrt(k)) / 3
    x = (sat_ - 0.4) / 0.2; t = max(1 - abs(x / 2), 0); s = (1 + np.sign(x) * (1 - t * t)) / 2
    gg = 0.05 * s; mid = 0.08
    gain = gg if yc <= 2 / 3 * mid else (0.0 if yc >= 2 * mid else gg * (mid / yc - 0.5))
    acesv = acesv * (1 + gain)
    hue = 0.0 if (acesv[0] == acesv[1] == acesv[2]) else np.degrees(np.arctan2(np.sqrt(3) * (acesv[1] - acesv[2]), 2 * acesv[0] - acesv[1] - acesv[2])) % 360
    ch = (hue + 180) % 360 - 180
    hw = smooth(0, 1, 1 - abs(2 * ch / 135)) ** 2
    acesv = acesv.copy(); acesv[0] += hw * sat_ * (0.03 - acesv[0]) * (1 - 0.82)
    cg = np.maximum(0, AP0_2_AP1 @ acesv)
    cg = AP1_Y @ cg + (cg - AP1_Y @ cg) * 0.96
    a, b_, c, d, e = 0.0245786, 0.000090537, 0.983729, 0.4329510, 0.238081
    post = (cg * (cg + a) - b_) / (cg * (c * cg + d) + e)
    xyz = AP1_2_XYZ @ post; s_ = xyz.sum(); xy = xyz[:2] / max(s_, 1e-4); Y = max(xyz[1], 0) ** 0.9811
    m = Y / max(xy[1], 1e-4); xyz = np.array([xy[0] * m, Y, (1 - xy[0] - xy[1]) * m])
    cv = XYZ_2_AP1 @ xyz
    cv = AP1_Y @ cv + (cv - AP1_Y @ cv) * 0.93
    return XYZ_2_709 @ (D60_2_D65 @ (AP1_2_XYZ @ cv))
LIN2LMS = M(3.90405e-1, 5.49941e-1, 8.92632e-3, 7.08416e-2, 9.63172e-1, 1.35775e-3, 2.31082e-2, 1.28021e-1, 9.36245e-1)
LMS2LIN = M(2.85847e+0, -1.62879e+0, -2.48910e-2, -2.10182e-1, 1.15820e+0, 3.24281e-4, -4.18120e-2, -1.18169e-1, 1.06867e+0)
def wb_coeffs(temp, tint):
    t1, t2 = temp / 65.0, tint / 65.0
    x = 0.31271 - t1 * (0.1 if t1 < 0 else 0.05); y = 2.87 * x - 3.0 * x * x - 0.27509507 + t2 * 0.05
    X, Y, Z = x / y, 1.0, (1 - x - y) / y
    w2 = np.array([0.7328 * X + 0.4296 * Y - 0.1624 * Z, -0.7036 * X + 1.6975 * Y + 0.0061 * Z, 0.0030 * X + 0.0136 * Y + 0.9834 * Z])
    return np.array([0.949237, 1.03542, 1.08728]) / w2
class Grade:
    def __init__(s, exposure=0.15, contrast=6, saturation=36.9, filt='#ffffff', shadows='#c7d1eb', mids='#ffffff', highs='#ffffff', lift=(0.0, 0, 0, 0), temp=0.0, tint=0.0):
        s.wb = wb_coeffs(temp, tint)
        s.ex = 2 ** exposure; s.con = 1 + contrast / 100; s.sat = 1 + saturation / 100; s.filt = s2l(filt); s.sh = s2l(shadows); s.mi = s2l(mids); s.hi = s2l(highs); s.lift = lift
    def __call__(s, lin):
        c = lin * s.ex
        c = LMS2LIN @ (LIN2LMS @ c * s.wb)
        cg = AP0_2_AP1 @ (sRGB_2_AP0 @ c)
        lg = to_cc(AP1_2_AP0 @ cg) if False else to_cc(sRGB_2_AP0 @ c)
        lg = (lg - MID) * s.con + MID
        cg = AP0_2_AP1 @ from_cc(lg)
        cg = cg * s.filt; cg = np.maximum(cg, 0)
        luma = cg @ np.array([0.2126729, 0.7151522, 0.0721750])
        sf = 1 - smooth(0.0, 0.3, luma); hf = smooth(0.55, 1.0, luma); mf = 1 - sf - hf
        cg = cg * s.sh * sf + cg * s.mi * mf + cg * s.hi * hf
        luma = cg @ np.array([0.2126729, 0.7151522, 0.0721750])
        cg = luma + s.sat * (cg - luma)
        cg = np.maximum(cg, 0)
        # lift: offset L toward the tint, white stays white: c x (1 - L) + L
        L = s.lift[0]
        if L: cg = cg * (1 - L) + L * np.array(s.lift[1:4])
        out = aces_tm(AP1_2_AP0 @ cg)
        return np.clip(out, 0, 1.0)
# colour maths
def lab(c):
    c = np.clip(c, 0, 1)
    X = np.array([[0.4124564, 0.3575761, 0.1804375], [0.2126729, 0.7151522, 0.0721750], [0.0193339, 0.1191920, 0.9503041]]) @ c / np.array([0.95047, 1.0, 1.08883])
    f = np.where(X > 0.008856, np.cbrt(X), 7.787 * X + 16 / 116); return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])
def de2000(l1, l2):
    L1, a1, b1 = l1; L2, a2, b2 = l2
    C1, C2 = np.hypot(a1, b1), np.hypot(a2, b2); Cb = (C1 + C2) / 2; G = 0.5 * (1 - np.sqrt(Cb ** 7 / (Cb ** 7 + 25 ** 7)))
    a1p, a2p = (1 + G) * a1, (1 + G) * a2; C1p, C2p = np.hypot(a1p, b1), np.hypot(a2p, b2)
    h1 = np.degrees(np.arctan2(b1, a1p)) % 360; h2 = np.degrees(np.arctan2(b2, a2p)) % 360
    dL = L2 - L1; dC = C2p - C1p; dh = h2 - h1
    if C1p * C2p == 0: dh = 0
    elif dh > 180: dh -= 360
    elif dh < -180: dh += 360
    dH = 2 * np.sqrt(C1p * C2p) * np.sin(np.radians(dh / 2))
    Lb = (L1 + L2) / 2; Cbp = (C1p + C2p) / 2
    hb = (h1 + h2) / 2 if C1p * C2p and abs(h1 - h2) <= 180 else ((h1 + h2 + 360) / 2 if C1p * C2p and h1 + h2 < 360 else ((h1 + h2 - 360) / 2 if C1p * C2p else h1 + h2))
    T = 1 - 0.17 * np.cos(np.radians(hb - 30)) + 0.24 * np.cos(np.radians(2 * hb)) + 0.32 * np.cos(np.radians(3 * hb + 6)) - 0.20 * np.cos(np.radians(4 * hb - 63))
    dth = 30 * np.exp(-(((hb - 275) / 25) ** 2)); Rc = 2 * np.sqrt(Cbp ** 7 / (Cbp ** 7 + 25 ** 7))
    Sl = 1 + 0.015 * (Lb - 50) ** 2 / np.sqrt(20 + (Lb - 50) ** 2); Sc = 1 + 0.045 * Cbp; Sh = 1 + 0.015 * Cbp * T; Rt = -np.sin(np.radians(2 * dth)) * Rc
    return float(np.sqrt((dL / Sl) ** 2 + (dC / Sc) ** 2 + (dH / Sh) ** 2 + Rt * (dC / Sc) * (dH / Sh)))
def hsv(c):
    import colorsys
    r, g, b = l2s(np.clip(c, 0, 1)); h, s, v = colorsys.rgb_to_hsv(r, g, b); return h * 360, s, v
