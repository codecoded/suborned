"""Tech trouser motion test: a measured proxy, not the final rig (brief 012, Forma, 2026-10-16).

Run:  /Applications/Blender.app/Contents/MacOS/Blender -b --python tf_proxy.py -- <outdir> [--render]

What it is
  A 1.85 m skeleton (pelvis, thigh, shin) in the sheet's A-pose, two trouser legs built from the round 3 cosplay sheet's dimensions
  (hem at mid-shin, hem width about 0.33 m, wide outer flare), four panels per leg (outer = the bone diagonal, front, back, inner),
  skinned by linear blend skinning (the same maths as Unity's SkinnedMeshRenderer), posed for the poses the game needs.
  Variants: AD   as drawn, panels skinned to thigh and shin only
            ADd  as drawn + the secondary-bone drivers (stateless: a function of the pose angles, no state, no integration)
            FX   the proposed change + drivers
            BELL round 4 rigid bell shorts (every vertex on one thigh bone), for comparison
  Metrics (metres, per pose): leg-leg interpenetration (vertex of one leg's cloth inside the other leg's cloth, below the crotch),
  cloth through a thigh or shin (cloth vertex inside the body capsule), cloth through the belly (front panel inside the torso), hem under ground / inside the kerb.
  Cloth thickness 0.01 m is allowed. 1 px at play size is about 3.3 cm (1.85 m = 56 px at zoom 14), so under 2 cm is not visible in the game.
No Blender cloth, no physics: every number is a pose of a skinned mesh, so the result is exactly reproducible.
"""
import sys, json, math, os
import numpy as np
import bpy
from mathutils.bvhtree import BVHTree
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "/tmp/tf_proxy"
RENDER = "--render" in argv
os.makedirs(OUT, exist_ok=True)

# ------------------------------------------------------------------ skeleton
HIP_Z, HIP_X, THIGH, SHIN, ANKLE_H = 0.965, 0.095, 0.45, 0.43, 0.085
A0 = math.radians(11.0)          # rest abduction: the sheet's A-pose
KERB_H = 0.18                    # tools/world/build.ts PROV.kerb.height
PANELS = ["out", "fr", "in", "bk"]
PANEL_DEG = {"out": 0.0, "fr": 90.0, "in": 180.0, "bk": 270.0}


def T(x, y, z):
    m = np.eye(4); m[:3, 3] = (x, y, z); return m
def Rx(a):
    c, s = math.cos(a), math.sin(a); m = np.eye(4); m[1, 1] = c; m[1, 2] = -s; m[2, 1] = s; m[2, 2] = c; return m
def Ry(a):
    c, s = math.cos(a), math.sin(a); m = np.eye(4); m[0, 0] = c; m[0, 2] = s; m[2, 0] = -s; m[2, 2] = c; return m
def Rz(a):
    c, s = math.cos(a), math.sin(a); m = np.eye(4); m[0, 0] = c; m[0, 1] = -s; m[1, 0] = s; m[1, 1] = c; return m
def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

SIDES = {"R": +1, "L": -1}      # R leg at +X (the figure faces +Y)
UP_OFF = 0.15                   # the upper panel bones pivot this far below the hip joint
UP_ONLY = ["out"]               # mode "sec10": the only panel with an upper bone (the bone diagonal, which flares most)


def rest_frames(sg):
    thigh = T(sg * HIP_X, 0, HIP_Z) @ Ry(-sg * A0)
    shin = thigh @ T(0, 0, -THIGH)
    return thigh, shin


# ------------------------------------------------------------------ trousers
# stations: t (m down the leg axis from the hip joint), inner, outer, front, back half-extents about the axis (m)
AD_PROFILE = [(-0.10, 0.095, 0.17, 0.13, 0.14), (0.15, 0.100, 0.18, 0.14, 0.15), (0.45, 0.140, 0.17, 0.15, 0.15), (0.665, 0.150, 0.20, 0.13, 0.13)]
# proposed change (FX): the inner side only: inner half-width 0.14 -> 0.115 at the knee and 0.15 -> 0.115 at the hem (inner hem edge 3.5 cm nearer the leg); outer flare, front, back and hem height unchanged
FX_PROFILE = [(-0.10, 0.095, 0.17, 0.13, 0.14), (0.15, 0.100, 0.18, 0.14, 0.15), (0.45, 0.115, 0.17, 0.15, 0.15), (0.665, 0.115, 0.20, 0.13, 0.13)]
BELL_PROFILE = [(-0.10, 0.095, 0.17, 0.13, 0.14), (0.05, 0.110, 0.18, 0.15, 0.15), (0.30, 0.150, 0.17, 0.17, 0.17)]   # round 4 sheet: hem about 30 cm per leg at mid-thigh
NA, NSEG = 28, 2.6


def station_ts(hem_t):
    a = np.linspace(-0.10, 0.20, 7)
    b = np.linspace(0.20, hem_t, 17)[1:]
    return np.concatenate([a, b])


def section(profile, t):
    ts = [p[0] for p in profile]
    return [float(np.interp(t, ts, [p[i] for p in profile])) for i in (1, 2, 3, 4)]


def build_leg(sg, profile):
    hem_t = profile[-1][0]
    ts = station_ts(hem_t)
    th = np.linspace(0, 2 * math.pi, NA, endpoint=False)
    c, s = np.cos(th), np.sin(th)
    V, TT, TH = [], [], []
    thigh0, _ = rest_frames(sg)
    for t in ts:
        i_, o_, f_, b_ = section(profile, t)
        a = np.where(c >= 0, o_, i_); b = np.where(s >= 0, f_, b_)
        u = a * np.sign(c) * np.abs(c) ** (2 / NSEG); v = b * np.sign(s) * np.abs(s) ** (2 / NSEG)
        local = np.stack([sg * u, v, -np.full_like(u, t), np.ones_like(u)], 1)   # thigh frame: x lateral (outer = sg), y forward, -z down the leg
        V.append((thigh0 @ local.T).T[:, :3]); TT.append(np.full(NA, t)); TH.append(th)
    V = np.concatenate(V); TT = np.concatenate(TT); TH = np.concatenate(TH)
    faces = []
    for i in range(len(ts) - 1):
        for j in range(NA):
            a = i * NA + j; b = i * NA + (j + 1) % NA; c2 = (i + 1) * NA + (j + 1) % NA; d = (i + 1) * NA + j
            faces.append((a, b, c2, d))
    faces = np.array(faces)
    # outward orientation: compare a face normal with the vector from its ring centre
    ring0 = V[:NA].mean(0); f0 = faces[len(faces) // 2]
    n = np.cross(V[f0[1]] - V[f0[0]], V[f0[3]] - V[f0[0]]); cen = V[f0].mean(0); r = cen - V[(f0[0] // NA) * NA:(f0[0] // NA + 1) * NA].mean(0)
    if np.dot(n, r) < 0: faces = faces[:, ::-1]
    return dict(V=V, t=TT, th=TH, faces=faces, nrows=len(ts), hem_t=hem_t)


def sector_w(th):
    w = np.stack([np.maximum(0, np.cos(th - math.radians(PANEL_DEG[p]))) ** 2 for p in PANELS], 1)
    return w / w.sum(1, keepdims=True)


# bones: index map
BONES = ["pelvis"] + [f"{k}_{s}" for s in SIDES for k in ["thigh", "shin"] + [f"{u}_{p}" for p in PANELS for u in ("up", "lo")]]
BI = {b: i for i, b in enumerate(BONES)}


def weights(leg, sg, mode):
    """(N, nbones) weights. mode 'ts' = thigh and shin (and pelvis) only; 'sec' = through the panel bones (identical motion while the drivers are zero);
    'rigid' = bell shorts, the top rows on the pelvis and the rest rigid on the thigh."""
    s = "R" if sg > 0 else "L"
    N = len(leg["t"]); W = np.zeros((N, len(BONES))); t = leg["t"]
    wp = 1 - smooth(-0.10, 0.12, t)
    if mode == "rigid":
        W[:, BI["pelvis"]] = wp; W[:, BI[f"thigh_{s}"]] = 1 - wp; return W
    R = 1 - wp
    W[:, BI["pelvis"]] = wp
    if mode == "ts":
        lo = smooth(0.36, 0.54, t)
        W[:, BI[f"thigh_{s}"]] = R * (1 - lo); W[:, BI[f"shin_{s}"]] = R * lo; return W
    a = smooth(0.10, 0.25, t); lo = smooth(0.36, 0.54, t); sw = sector_w(leg["th"])
    W[:, BI[f"thigh_{s}"]] = R * (1 - a)
    has_up = PANELS if mode == "sec" else UP_ONLY        # 'sec10': only the outer panel has an upper bone; front, back and inner hang from the thigh down to their hem bone
    for k, p in enumerate(PANELS):
        if p in has_up: W[:, BI[f"up_{p}_{s}"]] = R * a * (1 - lo) * sw[:, k]
        else: W[:, BI[f"thigh_{s}"]] += R * a * (1 - lo) * sw[:, k]
        W[:, BI[f"lo_{p}_{s}"]] = R * a * lo * sw[:, k]
    return W


# ------------------------------------------------------------------ pose + drivers
# angles in degrees. flex = hip flexion relative to the pelvis (+ forward), abd = hip abduction (+ outward), knee = knee flexion
POSES = {
    "stand":        dict(R=(0, 3, 0), L=(0, 3, 0), pitch=0, yaw=0),
    "walk_contact": dict(R=(24, 4, 3), L=(-16, 4, 18), pitch=0, yaw=0),
    "walk_pass":    dict(R=(0, 3, 6), L=(24, 3, 62), pitch=0, yaw=0),
    "run_flight":   dict(R=(62, 3, 88), L=(-22, 3, 38), pitch=8, yaw=0),
    "run_pass":     dict(R=(18, 3, 38), L=(52, 2, 100), pitch=8, yaw=0),
    "crouch_low":   dict(R=(62, 8, 100), L=(62, 8, 100), pitch=24, yaw=0),
    "crouch_deep":  dict(R=(85, 14, 125), L=(85, 14, 125), pitch=28, yaw=0),
    "fight":        dict(R=(32, 14, 38), L=(-12, 12, 6), pitch=4, yaw=35),
    "kerb":         dict(R=(62, 3, None), L=(-8, 3, 6), pitch=3, yaw=0, kerb=True),
}


def drive_params(on, P):
    return P if on else None


# the drivers: (pose angles of one leg) -> local rotation of each secondary bone. Stateless: same angles, same result.
P_DEFAULT = dict(k_lo=0.40, sig_in_up=3.0, sig_in_lo=9.0, g_flex=50.0, g_add=0.60, bk_bulge=0.06, bk_knee=100.0)


def drivers(sg, flex, abd, knee, P, shin_pitch):
    """returns {bone_name_without_side: 4x4 local rotation}. shin_pitch = the shin's world pitch (deg, + foot forward), read off the posed shin: stateless."""
    d = {}
    if P is None: return d
    g = np.clip(max(abs(flex) / P["g_flex"], P["g_add"] * (math.degrees(A0) - abd) / math.degrees(A0)), 0, 1)
    for p in PANELS:
        d[f"lo_{p}"] = Rx(-math.radians(P["k_lo"] * shin_pitch))                             # the hem hangs: it leans back toward the vertical by k_lo of the shin's lean
    kb = float(np.clip(knee / P["bk_knee"], 0, 1)) * P["bk_bulge"]                            # the back of the knee bulges out as the knee bends (no collapse into the calf)
    d["up_bk"] = T(0, -kb, 0); d["lo_bk"] = T(0, -kb, 0) @ d["lo_bk"]
    d["up_in"] = Ry(-sg * math.radians(P["sig_in_up"]) * g)                                   # inner panel splays away from the other leg
    d["lo_in"] = Ry(-sg * math.radians(P["sig_in_lo"]) * g) @ d["lo_in"]
    return d


def pose_frames(spec, P):
    """FK -> per-bone pose matrices, plus the posed ankle points."""
    yaw, pitch = math.radians(spec["yaw"]), math.radians(spec["pitch"])
    frames, info = {}, {}
    R_root = Rz(yaw) @ Rx(pitch)
    legs = {}
    for s, sg in SIDES.items():
        flex, abd, knee = spec[s]
        legs[s] = (flex, abd, knee)
    def build(pz, py):
        root = T(0, py, pz) @ R_root
        fr = {"pelvis": root}
        ank = {}
        for s, sg in SIDES.items():
            flex, abd, knee = legs[s]
            thigh = root @ T(sg * HIP_X, 0, 0) @ Rx(math.radians(flex)) @ Ry(-sg * math.radians(abd))
            shin = thigh @ T(0, 0, -THIGH) @ Rx(-math.radians(knee))
            fr[f"thigh_{s}"], fr[f"shin_{s}"] = thigh, shin
            dn = -shin[:3, 2]                                                    # the shin's down vector in the world
            c, sn = math.cos(yaw), math.sin(yaw); fwd = c * dn[1] - sn * dn[0] if False else (-sn * dn[0] + c * dn[1])   # its component along the heading
            spitch = math.degrees(math.atan2(fwd, -dn[2]))
            dr = drivers(sg, flex, abd, knee, P, spitch)
            for p in PANELS:
                fr[f"up_{p}_{s}"] = thigh @ T(0, 0, -UP_OFF) @ dr.get(f"up_{p}", np.eye(4))
                fr[f"lo_{p}_{s}"] = shin @ dr.get(f"lo_{p}", np.eye(4))
            ank[s] = (shin @ np.array([0, 0, -SHIN, 1]))[:3]
        return fr, ank
    if spec.get("kerb"):
        # lead leg R: choose the knee so the lead ankle stands on the kerb top while the trailing ankle stands on the road
        best = None
        for kn in np.arange(40.0, 110.0, 1.0):                      # scan lead and trailing knee: the lead ankle must stand 0.18 m above the trailing one
            for kl in np.arange(4.0, 50.0, 1.0):
                legs["R"] = (legs["R"][0], legs["R"][1], float(kn)); legs["L"] = (legs["L"][0], legs["L"][1], float(kl))
                fr, ank = build(HIP_Z, 0); err = abs((ank["R"][2] - ank["L"][2]) - KERB_H) + 0.0005 * kl
                if best is None or err < best[0]: best = (err, float(kn), float(kl))
        legs["R"] = (legs["R"][0], legs["R"][1], best[1]); legs["L"] = (legs["L"][0], legs["L"][1], best[2])
        fr, ank = build(HIP_Z, 0)
        pz = ANKLE_H - ank["L"][2] + HIP_Z
        fr, ank = build(pz, 0)
        spec["R"] = legs["R"]; spec["L"] = legs["L"]
    else:
        fr, ank = build(HIP_Z, 0)
        pz = HIP_Z + (ANKLE_H - min(ank["R"][2], ank["L"][2]))
        fr, ank = build(pz, 0)
    return fr, ank, legs


def rest_bone_frames():
    fr = {"pelvis": T(0, 0, HIP_Z)}
    for s, sg in SIDES.items():
        thigh, shin = rest_frames(sg)
        fr[f"thigh_{s}"], fr[f"shin_{s}"] = thigh, shin
        for p in PANELS:
            fr[f"up_{p}_{s}"] = thigh @ T(0, 0, -UP_OFF)
            fr[f"lo_{p}_{s}"] = shin
    return fr


REST = rest_bone_frames()
REST_INV = {b: np.linalg.inv(m) for b, m in REST.items()}


def skin(leg, W, frames):
    Vh = np.concatenate([leg["V"], np.ones((len(leg["V"]), 1))], 1)
    out = np.zeros_like(leg["V"])
    for b, i in BI.items():
        w = W[:, i]
        if not w.any(): continue
        out += w[:, None] * ((frames[b] @ REST_INV[b] @ Vh.T).T[:, :3])
    return out


# ------------------------------------------------------------------ metrics
def capsule_pen(P_, a, b, ra, rb):
    ab = b - a; L2 = np.dot(ab, ab)
    tt = np.clip(((P_ - a) @ ab) / L2, 0, 1); q = a + tt[:, None] * ab; d = np.linalg.norm(P_ - q, axis=1)
    r = ra + (rb - ra) * tt
    return r - d    # > 0 = inside the body


def bvh_of(V, F):
    return BVHTree.FromPolygons([tuple(map(float, v)) for v in V], [tuple(map(int, f)) for f in F], all_triangles=False)


def measure(legsV, legs, frames, ank, spec, kerb_y):
    res = {}
    t = legs["R"]["t"]; th = legs["R"]["th"]
    below = t >= 0.20
    bvh = {s: bvh_of(legsV[s], legs[s]["faces"]) for s in SIDES}
    # leg-leg: a vertex of one leg's cloth is inside the other leg's cloth when it is behind the nearest face AND a ray toward the other leg crosses its surface an odd number of times
    worst, cnt = 0.0, 0
    flags = {s: np.zeros(len(t), bool) for s in SIDES}
    for s, o in (("R", "L"), ("L", "R")):
        dirn = Vector((-1.0 if s == "R" else 1.0, 0.0, 0.0))
        for k in np.nonzero(below)[0]:
            p = Vector(map(float, legsV[s][k])); n = 0; pos = p.copy()
            for _ in range(8):
                loc, nor, idx, dist = bvh[o].ray_cast(pos, dirn)
                if loc is None: break
                n += 1; pos = loc + dirn * 1e-4
            if n % 2 == 1:
                loc, nor, idx, d = bvh[o].find_nearest(p)
                if (p - loc).dot(nor) < 0:                   # both tests agree: behind the nearest face and an odd number of crossings
                    if d > 0.01: cnt += 1; flags[s][k] = True
                    worst = max(worst, d)
    res["leg_leg"] = (worst, cnt)
    # through a leg (own or the other): cloth vertex inside the thigh or shin capsule
    body = {}
    for s, sg in SIDES.items():
        hip = frames["pelvis"] @ np.array([sg * HIP_X, 0, 0, 1]); knee = frames[f"shin_{s}"] @ np.array([0, 0, 0, 1]); ank_ = np.append(ank[s], 1)
        body[s] = (hip[:3], knee[:3], ank_[:3])
    worst_t, cnt_t = 0.0, 0
    for s in SIDES:
        Vs = legsV[s][below]
        for o in SIDES:
            h, k, a = body[o]
            pen = np.maximum(capsule_pen(Vs, h, k, 0.092, 0.066) - 0.010, capsule_pen(Vs, k, a, 0.060, 0.040) - 0.010)
            worst_t = max(worst_t, float(pen.max())); cnt_t += int((pen > 0.01).sum())
            idx = np.nonzero(below)[0]; flags[s][idx[pen > 0.01]] = True
    res["through_leg"] = (worst_t, cnt_t)
    # belly: front panel inside the torso (elliptic cylinder in the pelvis frame)
    inv = np.linalg.inv(frames["pelvis"]); worst_b, cnt_b = 0.0, 0
    front = (np.sin(th) > 0.5) & (t >= 0.12)
    for s in SIDES:
        Vp = (inv @ np.concatenate([legsV[s][front], np.ones((front.sum(), 1))], 1).T).T[:, :3]
        rho = np.sqrt((Vp[:, 0] / 0.185) ** 2 + (Vp[:, 1] / 0.135) ** 2)
        ins = (rho < 1) & (Vp[:, 2] > -0.02) & (Vp[:, 2] < 0.5)
        d = np.where(ins, (1 - rho) * 0.135 - 0.010, 0); worst_b = max(worst_b, float(d.max())); cnt_b += int((d > 0.01).sum())
    res["belly"] = (worst_b, cnt_b)
    # ground and kerb
    g = 0.0
    for s in SIDES:
        z = legsV[s][:, 2]; g = max(g, float(max(0, -z.min())))
    res["ground"] = (g, 0)
    res["hem_min_z"] = (float(min(legsV[s][legs[s]["t"] > legs[s]["hem_t"] - 0.01][:, 2].min() for s in SIDES)), 0)
    if spec.get("kerb"):
        k_pen = 0.0
        for s in SIDES:
            V = legsV[s]; ins = (V[:, 1] > kerb_y) & (V[:, 2] < KERB_H)
            if ins.any(): k_pen = max(k_pen, float(min(KERB_H - V[ins, 2].min(), V[ins, 1].max() - kerb_y)))
        res["kerb"] = (k_pen, 0)
        clear = 9.0
        for s in SIDES:
            V = legsV[s]; dy = np.maximum(kerb_y - V[:, 1], 0); dz = np.maximum(V[:, 2] - KERB_H, 0); clear = min(clear, float(np.hypot(dy, dz).min()))
        res["kerb_clear"] = (clear, 0)
    res["_flags"] = flags; res["_body"] = body
    return res


# ------------------------------------------------------------------ variants
VARIANTS = {
    "AD":   dict(profile=AD_PROFILE, mode="ts",    P=None,        label="as drawn, skinned to thigh and shin only"),
    "ADd":  dict(profile=AD_PROFILE, mode="sec",   P="drive",     label="as drawn + secondary bones with drivers"),
    "FX":   dict(profile=FX_PROFILE, mode="sec",   P="drive",     label="proposed change + secondary bones with drivers"),
    "FXn":  dict(profile=FX_PROFILE, mode="ts",    P=None,        label="proposed change, thigh and shin only (no secondary bones)"),
    "FX10": dict(profile=FX_PROFILE, mode="sec10", P="hang",      label="proposed change + 10 secondary bones (hem hang driver only)"),
    "ADd10": dict(profile=AD_PROFILE, mode="sec10", P="hang",     label="as drawn + 10 secondary bones (hem hang driver only)"),
    "BELL": dict(profile=BELL_PROFILE, mode="rigid", P=None,      label="round 4 rigid bell shorts"),
}


P_HANG = dict(k_lo=0.40, sig_in_up=0.0, sig_in_lo=0.0, g_flex=50.0, g_add=0.60, bk_bulge=0.0, bk_knee=100.0)


def run_all(P, only=None):
    out = {}
    for vn, v in VARIANTS.items():
        if only and vn not in only: continue
        legs = {s: build_leg(sg, v["profile"]) for s, sg in SIDES.items()}
        W = {s: weights(legs[s], sg, v["mode"]) for s, sg in SIDES.items()}
        out[vn] = {}
        for pn, spec in POSES.items():
            spec = json.loads(json.dumps(spec))
            PP = None if not v["P"] else (P_HANG if v["P"] == "hang" else P)
            fr, ank, angs = pose_frames(spec, PP)
            legsV = {s: skin(legs[s], W[s], fr) for s in SIDES}
            kerb_y = None
            if spec.get("kerb"): kerb_y = ank["R"][1] - 0.12
            m = measure(legsV, legs, fr, ank, spec, kerb_y)
            out[vn][pn] = dict(metrics={k: [round(a, 4), b] for k, (a, b) in m.items() if not k.startswith("_")}, angles={s: [round(x, 1) if x is not None else None for x in angs[s]] for s in SIDES}, ankle={s: [round(float(x), 3) for x in ank[s]] for s in SIDES},
                               kerb_y=kerb_y)
            out[vn][pn]["_flags"] = m["_flags"]; out[vn][pn]["_V"] = legsV; out[vn][pn]["_fr"] = fr; out[vn][pn]["_ank"] = ank; out[vn][pn]["_legs"] = legs
    return out


def max_influences(W):
    return int((W > 1e-4).sum(1).max())


def table(res, cols=("leg_leg", "through_leg", "belly", "ground")):
    pn = list(POSES)
    lines = []
    for vn in res:
        lines.append(f"[{vn}] {VARIANTS[vn]['label']}")
        lines.append("  " + f"{'pose':<14}" + "".join(f"{c:>14}" for c in cols))
        for p in pn:
            m = res[vn][p]["metrics"]
            lines.append("  " + f"{p:<14}" + "".join(f"{m[c][0] * 100:>9.1f} cm{'*' if m[c][0] > 0.02 else ' '}{'' if c in ('ground',) else '':<2}" for c in cols if c in m))
        lines.append("")
    return "\n".join(lines)


# ------------------------------------------------------------------ rendering (Blender Workbench, flat colours, depth buffer: a clip shows as a pink crease or bare thigh)
def srgb2lin(h):
    h = h.lstrip("#"); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1.0]

COL = dict(charcoal=srgb2lin("#33363a"), bone=srgb2lin("#e9e3d3"), hit=srgb2lin("#ff2bd0"), skin=srgb2lin("#b4afa6"), shoe=srgb2lin("#1b1c1e"),
           ground=srgb2lin("#e4e1da"), kerb=srgb2lin("#a8a5a0"), box=srgb2lin("#d9d4c4"))


def mk_obj(name, V, F, colors):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(map(float, v)) for v in V], [], [tuple(map(int, f)) for f in F]); me.update()
    ca = me.color_attributes.new(name="Col", type="FLOAT_COLOR", domain="POINT")
    ca.data.foreach_set("color", np.asarray(colors, dtype=np.float32).reshape(-1))
    me.color_attributes.active_color = ca
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o); return o


def prim(kind, color, mat4, scale=(1, 1, 1), **kw):
    from mathutils import Matrix
    if kind == "sphere": bpy.ops.mesh.primitive_uv_sphere_add(radius=kw.get("r", 0.1), segments=24, ring_count=12)
    elif kind == "cyl": bpy.ops.mesh.primitive_cylinder_add(radius=1, depth=1, vertices=32)
    elif kind == "cone": bpy.ops.mesh.primitive_cone_add(radius1=kw["r1"], radius2=kw["r2"], depth=1, vertices=24)
    elif kind == "cube": bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.active_object
    o.matrix_world = Matrix(mat4.tolist()) @ Matrix.Diagonal((*scale, 1.0))
    me = o.data; ca = me.color_attributes.new(name="Col", type="FLOAT_COLOR", domain="POINT")
    ca.data.foreach_set("color", np.tile(np.array(color, dtype=np.float32), len(me.vertices))); me.color_attributes.active_color = ca
    return o


def align_z(a, b):
    """matrix with origin at the midpoint of a-b and +Z along a->b"""
    d = Vector(map(float, b - a)); q = d.normalized().to_track_quat("Z", "Y"); m = q.to_matrix().to_4x4(); m.translation = Vector(map(float, (a + b) / 2))
    return np.array(m), float(d.length)


def setup_scene(w, h):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x, sc.render.resolution_y = w, h
    sh = sc.display.shading; sh.light = "STUDIO"; sh.color_type = "VERTEX"; sh.show_shadows = False; sh.show_cavity = False; sh.show_object_outline = True
    sh.object_outline_color = (0.05, 0.05, 0.05)
    sc.view_settings.view_transform = "Standard"
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.93, 0.92, 0.90)
    sc.display.render_aa = "8"
    return sc


def camera(sc, target, kind, scale):
    cam = bpy.data.cameras.new("c"); cam.type = "ORTHO"; cam.ortho_scale = scale; cam.clip_start = 0.1; cam.clip_end = 50
    co = bpy.data.objects.new("c", cam); sc.collection.objects.link(co)
    tgt = Vector(target)
    if kind == "front": off = Vector((0, 6, 0.0))
    elif kind == "side": off = Vector((6, 0, 0.0))
    else:
        yaw, pitch = math.radians(30), math.radians(38)
        off = Vector((math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), math.sin(pitch))) * 6
    co.location = tgt + off
    co.rotation_euler = (tgt - co.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = co


def render_pose(d, kind, path, spec, w=420, h=600, scale=2.0):
    sc = setup_scene(w, h)
    legs, V, fr, ank, flags = d["_legs"], d["_V"], d["_fr"], d["_ank"], d["_flags"]
    if kind != "game": prim("cube", COL["ground"], T(0, 0, -0.01), scale=(4, 4, 0.02))
    if spec.get("kerb"):
        prim("cube", COL["kerb"], T(0, d["kerb_y"] + 1.5, KERB_H / 2), scale=(6, 3, KERB_H))
    root = fr["pelvis"]
    for s, sg in SIDES.items():
        hip = (root @ np.array([sg * HIP_X, 0, 0, 1]))[:3]; knee = (fr[f"shin_{s}"] @ np.array([0, 0, 0, 1]))[:3]; a = ank[s]
        for (p0, p1, r0, r1) in ((hip, knee, 0.092, 0.066), (knee, a, 0.060, 0.040)):
            m, L = align_z(p0, p1)
            prim("cone", COL["skin"], m, scale=(1, 1, L), r1=r1, r2=r0)       # cone radius1 sits at -Z (the p0 end here is +Z? swapped on purpose: r2 at p1 end)
        yaw = math.radians(spec["yaw"])
        prim("cube", COL["shoe"], T(*(a + np.array([0, 0.07, -0.04]))) @ Rz(yaw), scale=(0.10, 0.28, 0.08))
    prim("cyl", COL["skin"], root @ T(0, 0, 0.27), scale=(0.17, 0.12, 0.52))
    prim("sphere", COL["skin"], root @ T(0, 0.02, 0.72), r=0.11)
    for s in SIDES:
        leg = legs[s]; col = np.zeros((len(leg["t"]), 4)); sw = sector_w(leg["th"])[:, 0]
        for i in range(len(col)):
            col[i] = COL["bone"] if sw[i] > 0.55 else COL["charcoal"]
            if flags[s][i]: col[i] = COL["hit"]
        mk_obj(f"cloth_{s}", V[s], leg["faces"], col)
    head = (root @ np.array([0, 0.02, 0.84, 1]))[:3]
    allv = np.concatenate([V["R"], V["L"], head[None], np.array([ank["R"], ank["L"]])])
    lo_, hi_ = allv.min(0), allv.max(0); ctr = (lo_ + hi_) / 2
    ext = max(hi_[2] - lo_[2], hi_[0] - lo_[0] if kind == "front" else hi_[1] - lo_[1], 1.3)
    camera(sc, (ctr[0], ctr[1], ctr[2]), kind, ext * 1.22)
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
