"""Does the road finish read under Glint's distance haze? (brief 048 x brief 028, Vela, 2026-10-22)

python3 -I tools/decokit/road_haze_check.py <frames_dir> <out.json>

Reads the boulevard-third frames road_board.py / road_board_post.py wrote, projects points on the carriageway, the pavement beside it and the kerb line 14 m ahead into the frame (the camera of
road_board.py), reads those pixels, fogs them as the game will (exp2, farClip 450 m, reach 0.95, haze colour by day and night) at 40 / 80 / 150 / 250 / 350 / 450 m, and reports dE2000 of the road to the
haze colour (does it still stand out of the sky end?), of the road to the pavement beside it (does the carriageway still read as a road?) and of the lit kerb line to the carriageway (the ribbon at night). The play camera takes no
fog (haze.json orthographic false), so this is third person only."""

import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
from deltae import de  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
D = Path(sys.argv[1])
LIGHT = json.loads((REPO / "unity/Assets/Suborned/Editor/World/light.json").read_text())
EYE, YAW, FOV, PITCH, EYEZ = (-9.5, -104.0), 180.0, 55.0, 7.2, 0.18 + 3.24
W_, H_ = 1920, 1080


def project(p):
    y, pt, f = math.radians(YAW), math.radians(PITCH), math.radians(FOV)
    fwd = np.array([math.sin(y) * math.cos(pt), math.cos(y) * math.cos(pt), -math.sin(pt)])
    right = np.array([math.cos(y), -math.sin(y), 0.0])
    up = np.cross(right, fwd)
    v = np.array([p[0] - EYE[0], p[1] - EYE[1], p[2] - EYEZ])
    zc = v @ fwd
    if zc <= 0.1:
        return None
    k = (H_ / 2) / math.tan(f / 2)
    return (W_ / 2 + (v @ right) / zc * k, H_ / 2 - (v @ up) / zc * k)


def px(img, p, r=3, how="mean"):
    q = project(p)
    if not q or not (r <= q[0] < W_ - r and r <= q[1] < H_ - r):
        return None
    a = img[int(q[1]) - r:int(q[1]) + r + 1, int(q[0]) - r:int(q[0]) + r + 1].reshape(-1, 3).astype(float)
    c = a.mean(0) if how == "mean" else a[a.sum(1).argmax()]
    return "#%02x%02x%02x" % tuple(int(round(x)) for x in c)


def lin(h):
    c = np.array([int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)])
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def hexs(c):
    c = np.clip(c, 0, 1)
    s = np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
    return "#%02x%02x%02x" % tuple(int(round(x * 255)) for x in s)


def main():
    """The surfaces are read where the pixels are big (14 m ahead: a 7 px window is 0.1 m of ground), then fogged by the same law the game will use: haze is geometry-free (colour = lerp(colour, haze, 1 - exp(-(k d)^2)))."""
    out = {"camera": dict(eye=EYE, yaw=YAW, fov=FOV, pitch=PITCH, eyeZ=EYEZ), "haze": {k: LIGHT["palette"][k]["hex"] for k in ("light.haze", "night.haze")}, "sampledAt": "14 m ahead", "rows": []}
    farClip, reach = 450.0, 0.95
    k = math.sqrt(-math.log(1 - reach)) / farClip
    y0 = EYE[1] - 14
    for tag, night in (("boulevard-third-now-day", False), ("boulevard-third-proposed-day", False), ("boulevard-third-now-night", True), ("boulevard-third-proposed-night", True)):
        f = D / f"{tag}.png"
        if not f.exists():
            continue
        img = np.asarray(Image.open(f).convert("RGB"))
        hz = out["haze"]["night.haze" if night else "light.haze"]
        # the west carriageway's near lane, the pavement beyond the west kerb, the kerb line (lens at night) just inside the kerb face
        base = dict(road=px(img, (-7.5, y0, 0.0), r=4), pavement=px(img, (-17.0, y0, 0.18), r=4), kerbline=px(img, (-13.9, y0, 0.0), r=1, how="max"))
        out.setdefault("near", {})[tag] = base
        for d in (40, 80, 150, 250, 350, 450):
            fog = 1 - math.exp(-((k * d) ** 2))
            fg = {n: (hexs(lin(c) * (1 - fog) + lin(hz) * fog) if c else None) for n, c in base.items()}
            row = dict(tag=tag, d=d, fog=round(fog, 3), **fg)
            row["dE_road_haze"] = round(de(fg["road"], hz), 1)
            row["dE_road_pavement"] = round(de(fg["road"], fg["pavement"]), 1)
            row["dE_kerbline_road"] = round(de(fg["kerbline"], fg["road"]), 1)
            out["rows"].append(row)
    Path(sys.argv[2]).write_text(json.dumps(out, indent=1))
    print(json.dumps(out["near"], indent=1))
    for r in out["rows"]:
        print({k: r[k] for k in ("tag", "d", "fog", "dE_road_haze", "dE_road_pavement", "dE_kerbline_road")})


main()
