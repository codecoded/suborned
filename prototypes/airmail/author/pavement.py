"""Metric sawn slabs. Source aggregate, joints and material channels tile across all sockets."""

from pathlib import Path
import sys
import numpy as np
from PIL import Image, ImageOps
from scipy.ndimage import gaussian_filter

src, dest, bakes, maps = (Path(a) for a in sys.argv[1:])
dest.mkdir(exist_ok=True, parents=True)
n = 1024
rng = np.random.default_rng(4579)
yy, xx = np.mgrid[:n, :n]
chips = np.asarray(ImageOps.grayscale(Image.open(src / "terrazzo.jpg")).resize((n, n)), float)
chips = (chips - chips.mean()) / chips.std()
cloud = gaussian_filter(rng.normal(size=(n, n)), 32, mode="wrap")
cloud /= cloud.std()
row = yy // 128
xo = (xx + (row % 2) * 128) % n
col = xo // 256
dist = np.minimum.reduce([xo % 256, 255 - xo % 256, yy % 128, 127 - yy % 128])
tints = rng.normal(0, 2, (8, 4, 3))
height = np.minimum(dist, 5) * 1.3 + chips * 0.23
base = (
    np.array([194, 182, 158])
    + tints[row, col]
    + (cloud * 1.45 + chips * 2.9 - 41 * np.exp(-dist / 1.25))[:, :, None]
)
normal = np.stack(
    (-np.gradient(height, axis=1) * 0.44, np.gradient(height, axis=0) * 0.44, np.ones((n, n))), axis=2
)
normal /= np.linalg.norm(normal, axis=2)[:, :, None]
Image.fromarray(np.clip(base, 0, 255).astype("uint8")).save(dest / "pavement.jpg", quality=90)
Image.fromarray(((normal * 0.5 + 0.5) * 255).astype("uint8")).save(dest / "pavement_n.png", optimize=True)
orm = Image.open(maps / "orm.png").convert("RGB")
tile = np.array(orm.crop((0, 512, 256, 768)))
tile[:, :, 0] = np.array(Image.open(bakes / "ao.png").convert("L"))
orm.paste(Image.fromarray(tile), (0, 512))
orm.save(maps / "orm.png")
normals = Image.open(maps / "normal.png").convert("RGB")
normals.paste(Image.open(bakes / "normal.png").convert("RGB"), (0, 512))
normals.save(maps / "normal.png")
