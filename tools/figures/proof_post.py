"""Compose the proof passes (tools/figures/proof.py) into a picture at the game's grade (brief 067, Forma). System python: numpy and PIL.
The ground of the picture is a patch of a REAL frame of the game (a sunlit or a shaded stretch of road), taken back to scene light, darkened by the figures' shadows from the
render, then the figures are laid over it and the whole goes through the game's grade: post-exposure +0.15, saturation +18, contrast +6, an ACES filmic curve (Narkowicz's fit,
close to URP's ACES) and sRGB (light.json post). Relative reads only: not Unity's pixels. `check_cell()` compares against the real frame's own cell for a sanity line."""
import numpy as np
from PIL import Image

POST_EXPOSURE_EV = 0.15; SATURATION = 1.18; CONTRAST = 1.06


def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(np.asarray(c, dtype=np.float64), 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def aces(x):
    x = np.maximum(x, 0)
    return np.clip((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0, 1)


def aces_inv(y):
    y = np.clip(y, 0, 0.999)
    a = 2.43 * y - 2.51; b = 0.59 * y - 0.03; c = 0.14 * y
    d = np.maximum(b * b - 4 * a * c, 0)
    return (-b - np.sqrt(d)) / (2 * a)


LUMA = np.array([0.2126, 0.7152, 0.0722])


def grade(lin, expo=1.0):
    """Scene-linear RGB -> display sRGB float: exposure, saturation, contrast (about mid grey in log), ACES, sRGB."""
    x = lin * expo * 2 ** POST_EXPOSURE_EV
    lum = (x * LUMA).sum(-1, keepdims=True)
    x = np.maximum(lum + (x - lum) * SATURATION, 0)
    mid = 0.4135884
    x = np.where(x > 0, np.exp(np.log(np.maximum(x, 1e-6) / mid) * CONTRAST) * mid, 0)
    return lin_to_srgb(aces(x))


def ungrade(srgb8, expo=1.0):
    """Display sRGB 8-bit -> scene-linear RGB (the grade run backwards; saturation and contrast taken back approximately)."""
    y = srgb_to_lin(np.asarray(srgb8, dtype=np.float64) / 255.0)
    x = aces_inv(y)
    mid = 0.4135884
    x = np.where(x > 0, np.exp(np.log(np.maximum(x, 1e-6) / mid) / CONTRAST) * mid, 0)
    lum = (x * LUMA).sum(-1, keepdims=True)
    x = np.maximum(lum + (x - lum) / SATURATION, 0)
    return x / (expo * 2 ** POST_EXPOSURE_EV)


def load_pass(prefix, name):
    return np.load(f'{prefix}_{name}.npy').astype(np.float64)


def compose(prefix, bg_rgb8, expo=1.0, gain=1.0, bg_scale=None):
    """The picture for one proof (prefix of proof.py's three passes) over `bg_rgb8` (H x W x 3, a real ground patch): sRGB 8-bit H x W x 3."""
    G = load_pass(prefix, 'ground'); S = load_pass(prefix, 'shadow'); F = load_pass(prefix, 'figs')
    H, W = G.shape[:2]
    bg = np.asarray(Image.fromarray(np.asarray(bg_rgb8, dtype=np.uint8)).resize((W, H), Image.NEAREST), dtype=np.float64)
    bg_lin = ungrade(bg, expo)
    if bg_scale is not None: bg_lin = bg_lin * np.asarray(bg_scale)   # a shaded picture over a SUNLIT patch of the real road: the road goes down by the light the real frame loses in shade
    ratio = np.where((S[..., 3:4] > 0.5) & (G[..., :3] > 1e-4), S[..., :3] / np.maximum(G[..., :3], 1e-4), 1.0)
    ratio = np.clip(ratio, 0, 1.5)
    lin = F[..., :3] * gain + (1.0 - np.clip(F[..., 3:4], 0, 1)) * bg_lin * ratio
    return (grade(lin, expo) * 255 + 0.5).astype(np.uint8)


def ground_albedo(real_rgb8, pitch_deg=38.0, sun=1.8, sun_rgb=(1.0, 0.745, 0.631), sky_lin=(0.33, 0.325, 0.34), expo=1.0, lit=True):
    """The linear albedo a flat ground needs in proof.py so its sunlit render grades to `real_rgb8` (the real road): scene light / irradiance of an up-facing plane."""
    L = ungrade(np.asarray(real_rgb8, dtype=np.float64).reshape(1, 3), expo)[0]
    E = (np.array(sun_rgb) * sun * np.sin(np.radians(pitch_deg)) if lit else 0) + np.array(sky_lin) * 0.9
    return np.clip(L / E, 0.01, 0.95)


def enlarge(img, k=3):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)
