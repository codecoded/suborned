"""Forward model of URP's LDR-grading chain as this project configures it, to test one hypothesis:
the Editor RT shot clamps scene colour to [0,1] (8-bit sRGB intermediate), the player keeps HDR.
Sources: URP package (Common.hlsl ApplyTonemap/ApplyColorGrading, LutBuilderLdr.shader, Color.hlsl NeutralTonemap),
grade values from unity/Assets/Scenes/HarbourDistrict-grade.asset. Bloom, vignette and the 32^3 LUT interpolation are left out."""
import numpy as np

def srgb_to_lin(x):
    x = np.asarray(x, float)
    return np.where(x <= 0.04045, x / 12.92, ((x + 0.055) / 1.055) ** 2.4)
def lin_to_srgb(x):
    x = np.clip(np.asarray(x, float), 0, 1)
    return np.where(x <= 0.0031308, x * 12.92, 1.055 * x ** (1 / 2.4) - 0.055)

# NeutralCurve (Color.hlsl)
a, b, c_, d, e, f, white = 0.2, 0.29, 0.24, 0.272, 0.02, 0.3, 5.3
def curve(x): return ((x * (a * x + c_ * b) + d * e) / (x * (a * x + b) + d * f)) - e / f
ws = 1.0 / curve(white)
def neutral(x): return curve(np.maximum(x, 0) * ws) * ws

LC = dict(a=5.555556, b=0.047996, c=0.244161, d=0.386036)
MID = 0.4135884
def lin_to_logc(x): return LC['c'] * np.log10(LC['a'] * x + LC['b']) + LC['d']
def logc_to_lin(x): return (10 ** ((x - LC['d']) / LC['c']) - LC['b']) / LC['a']

def smoothstep(lo, hi, x):
    t = np.clip((x - lo) / (hi - lo), 0, 1); return t * t * (3 - 2 * t)
lum = lambda v: 0.2126 * v[..., 0] + 0.7152 * v[..., 1] + 0.0722 * v[..., 2]

post_exposure = 2 ** 0.12
contrast = 1 + 14 / 100.0
saturation = 1 + 14 / 100.0
colour_filter = srgb_to_lin(np.array([1.0, 0.9843137, 0.95686275]))
shadows = srgb_to_lin(np.array([0.6039216, 0.65882355, 0.84705883]))
highlights = srgb_to_lin(np.array([1.0, 0.8509804, 0.65882355]))
SH_LIM = (0.0, 0.3, 0.55, 1.0)

def lut_ldr(c, smh_highlights=highlights):
    c = np.asarray(c, float)
    c = logc_to_lin((lin_to_logc(c) - MID) * contrast + MID)       # contrast in log
    c = np.maximum(c * colour_filter, 0)                              # colour filter
    L = lum(c)
    sf = 1 - smoothstep(SH_LIM[0], SH_LIM[1], L); hf = smoothstep(SH_LIM[2], SH_LIM[3], L); mf = 1 - sf - hf
    c = c * shadows * sf[..., None] + c * mf[..., None] + c * smh_highlights * hf[..., None]
    L = lum(c); c = L[..., None] + saturation * (c - L[..., None])   # global saturation
    return np.clip(c, 0, 1)

def chain(scene_lin, clamp_scene, smh_highlights=highlights):
    c = np.asarray(scene_lin, float)
    if clamp_scene: c = np.minimum(c, 1.0)                       # 8-bit sRGB intermediate in the Editor RT path
    c = np.clip(neutral(c * post_exposure), 0, 1)                 # ApplyTonemap: Neutral, then saturate
    return lin_to_srgb(lut_ldr(c, smh_highlights)) * 255

if __name__ == "__main__":
    from itertools import product
    editor_obs = np.array([229.0, 207.8, 174.0]); player_obs = np.array([238.0, 206.3, 169.2])
    # 1) fit (G,B) scene values and an R>=1 so the clamped Editor chain reproduces the Editor sun-paving mean
    best = None
    for g in np.linspace(0.3, 1.0, 71):
        for bb in np.linspace(0.2, 1.0, 81):
            out = chain([1.5, g, bb], True)           # R is clamped, any R>=1 gives the same Editor value
            err = np.abs(out - editor_obs).sum()
            if best is None or err < best[0]: best = (err, g, bb, out)
    err, g, bb, out = best
    print(f"Editor fit: scene G={g:.3f} B={bb:.3f} (R clamped at 1.0) -> model {out.round(1)} vs observed {editor_obs}")
    # 2) the player sees the same scene colour with R unclamped: fit R only, then check G and B (free checks)
    bestr = None
    for r in np.linspace(1.0, 3.0, 401):
        out = chain([r, g, bb], False)
        err_r = abs(out[0] - player_obs[0])
        if bestr is None or err_r < bestr[0]: bestr = (err_r, r, out)
    err_r, r, outp = bestr
    print(f"Player fit : scene R={r:.3f} -> model {outp.round(1)} vs observed {player_obs}   (R fitted; G and B are predictions)")
    # 3) what if the highlights tint were off (white)?
    for rr, label in ((r, "player, SMH highlights tint OFF"), (1.0, "editor-clamped, SMH highlights tint OFF")):
        print(f"{label}: ", chain([rr, g, bb], False, np.array([1.0, 1.0, 1.0])).round(1))
