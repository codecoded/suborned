"""Ground_Swatches (brief 026 round 1): one flat board carrying every ground role, UV = world metres, so the kit catalogue carries the ground materials and their textures
(a material no piece uses is never copied: `bun run kit` copies what an adopted piece lists) and a Unity prefab shows the roles side by side at true scale.
Latch's ground meshes use the same materials by name; this piece is not placed in the world. Layout (x right, y back, metres, pivot = board centre):
asphalt 4 x 4 | pavement 4 x 4 | lawn 4 x 4 | path 2 x 4 | terrazzo 2 x 2 + seafoam 2 x 2 | kerb 2 x 0.3 | gutter 4 x 0.375, 0.3 m apart."""

from kit import asset
from mats import pal

ROWS = [("Ground_Asphalt", 4, 4), ("Ground_Pavement", 4, 4), ("Ground_Lawn", 4, 4), ("Ground_Path", 2, 4), ("Terrazzo_Ivory", 2, 2), ("Terrazzo_Seafoam", 2, 2)]


@asset("Ground_Swatches", "Ground", bevel=0)
def ground_swatches(s):
    x = 0.0
    spans = []
    for name, w, d in ROWS:
        spans.append((name, x, w, d))
        x += w + 0.3
    cx = (x - 0.3) / 2
    for name, x0, w, d in spans:
        pts = [(x0 - cx, -2.0, 0.0), (x0 - cx + w, -2.0, 0.0), (x0 - cx + w, -2.0 + d, 0.0), (x0 - cx, -2.0 + d, 0.0)]
        # u = x, v = y in metres from the swatch corner: the materials' m_Scale is 1 / tile
        uv = ((0, 0), (w, 0), (w, d), (0, d))
        s.decal(pts, getattr(pal, name), uv, lift=0.0)
    # kerb stone and gutter strip along the front edge
    y0 = -2.0 - 0.7
    s.decal([(-cx, y0, 0.0), (-cx + 2, y0, 0.0), (-cx + 2, y0 + 0.3, 0.0), (-cx, y0 + 0.3, 0.0)], pal.Ground_Kerb, ((0, 0), (2, 0), (2, 0.3), (0, 0.3)), lift=0.0)
    y1 = y0 - 0.675
    s.decal([(-cx, y1, 0.0), (-cx + 4, y1, 0.0), (-cx + 4, y1 + 0.375, 0.0), (-cx, y1 + 0.375, 0.0)], pal.Ground_Gutter, ((0, 0), (4, 0), (4, 0.375), (0, 0.375)), lift=0.0)
