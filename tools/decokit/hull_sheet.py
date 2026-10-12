"""Painted hull plating for the landmarks (spheres, discs, rocket liners): the exhaustive hard-surface panelling that
sells colossal scale from 200 m+. One tileable 8 x 8 m sheet at 256 px/m (4 mm) -> derived/hull_panels_v1{,_normal,
_mask}.png. Rows of plates in staggered widths, recessed seams, rivet lines along every seam, a scatter of inset
inspection hatches and odd replacement plates; albedo is a near-white neutral so materials tint it (ivory, silver).

python3 Tools/Blender/decokit/hull_sheet.py
"""

import numpy as np

from pave_sheet import blur, normal_from, save

SIZE, PPM = 8.0, 256
ROWS = (1.2, 1.6, 1.0, 1.4, 1.2, 1.6)  # plate courses (m), sum = SIZE so the sheet tiles
WIDTHS = (1.6, 2.0, 2.4, 3.2)
SEAM, RIVET_STEP, RIVET_R = 0.012, 0.11, 0.0045
SEED = 5


def courses(rng):
    """Per row: (v0, v1, sorted u cuts incl. wrap) with a random phase so vertical seams stagger."""
    out, v = [], 0.0
    for h in ROWS:
        cuts, u = [], rng.uniform(0, SIZE)
        while len(cuts) == 0 or (cuts[0] - u) % SIZE > 0.8 or len(cuts) < 3:
            cuts.append(u % SIZE)
            u += rng.choice(WIDTHS)
            if u - cuts[0] > SIZE - 0.8:
                break
        out.append((v, v + h, np.sort(np.array(cuts))))
        v += h
    return out


def build():
    rng = np.random.default_rng(SEED)
    n = int(SIZE * PPM)
    u = (np.arange(n) + 0.5) / PPM
    U, V = np.meshgrid(u, u[::-1])  # row 0 = top (v = SIZE)
    plate = np.zeros((n, n), np.int32)
    du = np.full((n, n), 9.0, np.float32)  # distance to nearest vertical seam
    dv = np.full((n, n), 9.0, np.float32)  # distance to nearest horizontal seam
    pu = np.zeros((n, n), np.float32)      # position across the plate (m from its left seam)
    pw = np.ones((n, n), np.float32)       # plate width
    ph = np.ones((n, n), np.float32)       # course height
    for r, (v0, v1, cuts) in enumerate(courses(rng)):
        m = (V >= v0) & (V < v1)
        uu = U[m]
        k = np.searchsorted(cuts, uu) % len(cuts)
        plate[m] = r * 100 + k
        d = np.abs(uu[:, None] - cuts[None, :])
        du[m] = np.minimum(d, SIZE - d).min(1)
        dv[m] = np.minimum(V[m] - v0, v1 - V[m])
        left, right = cuts[k - 1], cuts[k]
        pu[m] = (uu - left) % SIZE
        w = (right - left) % SIZE
        pw[m] = np.where(w == 0, SIZE, w)
        ph[m] = v1 - v0
    seam_d = np.minimum(du, dv)

    ids = np.unique(plate)
    tone = dict(zip(ids, rng.normal(1.0, 0.025, len(ids))))
    odd = dict(zip(ids, rng.random(len(ids)) < 0.07))       # replacement plates, a touch warmer / greyer
    hatch = dict(zip(ids, rng.random(len(ids)) < 0.14))     # inset inspection hatch in this plate
    t = np.vectorize(tone.get)(plate).astype(np.float32)
    o = np.vectorize(odd.get)(plate)
    hm = np.vectorize(hatch.get)(plate)

    h = np.zeros((n, n), np.float32)
    h -= 0.004 * np.clip(1 - seam_d / SEAM, 0, 1) ** 0.5                     # recessed seams
    along_u = (np.abs(((U + RIVET_STEP / 2) % RIVET_STEP) - RIVET_STEP / 2))  # rivet lines 4 cm inside each seam
    along_v = (np.abs(((V + RIVET_STEP / 2) % RIVET_STEP) - RIVET_STEP / 2))
    ru = np.hypot(np.abs(du - 0.04), along_v)
    rv = np.hypot(np.abs(dv - 0.04), along_u)
    h += 0.0015 * np.clip(1 - np.minimum(ru, rv) / RIVET_R, 0, 1) ** 0.5
    # hatches: a 0.6 x 0.4 m inset panel centred in the plate (hairline groove); sub-panel hairlines on 40% of plates
    cx, cy = np.abs(pu - pw / 2), np.abs(dv - ph / 2)
    box = hm & (cx < 0.3) & (cy < 0.2)
    groove = box & ((np.abs(cx - 0.3) < 0.007) | (np.abs(cy - 0.2) < 0.007) | (cx > 0.293) | (cy > 0.193))
    split = np.vectorize(dict(zip(ids, rng.integers(0, 4, len(ids)))).get)(plate)  # 0/1: none, 2: halves, 3: thirds
    frac = pu / pw * np.where(split == 3, 3, 2)
    hair = (split >= 2) & (np.abs(frac - np.round(frac)) * pw / np.where(split == 3, 3, 2) < 0.004) & (du > 0.05)
    groove = groove | hair
    h -= 0.002 * groove
    h += 0.0008 * blur(rng.standard_normal((n, n)).astype(np.float32), 2.0)  # paint orange-peel

    base = np.array([0.93, 0.92, 0.9], np.float32)
    albedo = base * t[..., None]
    albedo = np.where(o[..., None], albedo * np.array([1.0, 0.985, 0.96], np.float32), albedo)
    cloud = 1 + 0.03 * blur(rng.standard_normal((n // 8, n // 8)).astype(np.float32), 6.0).repeat(8, 0).repeat(8, 1)
    albedo *= cloud[..., None] / cloud.mean()
    cav = np.clip(1 - seam_d / (SEAM * 1.6), 0, 1)
    albedo *= (1 - 0.55 * cav - 0.3 * groove)[..., None]
    ao = np.clip(1 - 0.6 * cav - 0.3 * groove, 0, 1)
    smooth_map = np.clip(0.62 - 0.25 * cav + 0.04 * (t - 1) * 10, 0, 1)
    save("hull_panels", albedo, normal_from(h, PPM), ao, smooth_map)


if __name__ == "__main__":
    build()
