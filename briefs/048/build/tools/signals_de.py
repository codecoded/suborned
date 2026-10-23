# ΔE2000 of the signal pixels (the mask) between haze on and haze off, per distance and variant; and the mask's blob count.
import sys, json, numpy as np
from PIL import Image
d = sys.argv[1]
def lab(img):
    a = np.asarray(img.convert('RGB'), dtype=np.float64) / 255.0
    a = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    M = np.array([[0.4124564, 0.3575761, 0.1804375], [0.2126729, 0.7151522, 0.0721750], [0.0193339, 0.1191920, 0.9503041]])
    xyz = a @ M.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)
def de2000(l1, l2):
    L1, a1, b1 = l1[..., 0], l1[..., 1], l1[..., 2]; L2, a2, b2 = l2[..., 0], l2[..., 1], l2[..., 2]
    C1 = np.hypot(a1, b1); C2 = np.hypot(a2, b2); Cb = (C1 + C2) / 2
    G = 0.5 * (1 - np.sqrt(Cb ** 7 / (Cb ** 7 + 25 ** 7))); a1p = (1 + G) * a1; a2p = (1 + G) * a2
    C1p = np.hypot(a1p, b1); C2p = np.hypot(a2p, b2); h1p = np.degrees(np.arctan2(b1, a1p)) % 360; h2p = np.degrees(np.arctan2(b2, a2p)) % 360
    dLp = L2 - L1; dCp = C2p - C1p; dh = h2p - h1p; dh = np.where(dh > 180, dh - 360, np.where(dh < -180, dh + 360, dh)); dh = np.where(C1p * C2p == 0, 0, dh)
    dHp = 2 * np.sqrt(C1p * C2p) * np.sin(np.radians(dh / 2)); Lbp = (L1 + L2) / 2; Cbp = (C1p + C2p) / 2
    hs = h1p + h2p; hbp = np.where(np.abs(h1p - h2p) > 180, (hs + 360) / 2, hs / 2); hbp = np.where(C1p * C2p == 0, hs, hbp)
    T = 1 - 0.17 * np.cos(np.radians(hbp - 30)) + 0.24 * np.cos(np.radians(2 * hbp)) + 0.32 * np.cos(np.radians(3 * hbp + 6)) - 0.20 * np.cos(np.radians(4 * hbp - 63))
    dth = 30 * np.exp(-(((hbp - 275) / 25) ** 2)); Rc = 2 * np.sqrt(Cbp ** 7 / (Cbp ** 7 + 25 ** 7))
    Sl = 1 + 0.015 * (Lbp - 50) ** 2 / np.sqrt(20 + (Lbp - 50) ** 2); Sc = 1 + 0.045 * Cbp; Sh = 1 + 0.015 * Cbp * T; Rt = -np.sin(np.radians(2 * dth)) * Rc
    return np.sqrt((dLp / Sl) ** 2 + (dCp / Sc) ** 2 + (dHp / Sh) ** 2 + Rt * (dCp / Sc) * (dHp / Sh))
if __name__ == "__main__":
    out = {}
    for dist in (150, 250):
        m = np.asarray(Image.open(f'{d}/signals-{dist}-mask.png').convert('L')) > 24
        # the overlay variant draws the fan over everything (the overlay clears depth); where the as-built frame matches it, the fan is in plain view
        ab = np.asarray(Image.open(f'{d}/signals-{dist}-asbuilt-nohaze.png').convert('RGB')).astype(int); ov = np.asarray(Image.open(f'{d}/signals-{dist}-overlay-nohaze.png').convert('RGB')).astype(int)
        m = m & (np.abs(ab - ov).sum(-1) <= 18)
        row = {'visibleFanPixels': int(m.sum())}
        if m.sum() == 0: out[dist] = row; continue
        for v in ('asbuilt', 'overlay'):
            a = lab(Image.open(f'{d}/signals-{dist}-{v}-haze.png')); b = lab(Image.open(f'{d}/signals-{dist}-{v}-nohaze.png'))
            de = de2000(a[m], b[m]); mean_de = float(de2000(a[m].mean(0), b[m].mean(0)))
            row[v] = {'meanColourDeltaE2000': round(mean_de, 2), 'pixelDeltaE2000Median': round(float(np.median(de)), 2), 'pixelDeltaE2000P90': round(float(np.percentile(de, 90)), 2), 'pass': mean_de <= 5}
        out[dist] = row
    print(json.dumps(out, indent=1))
