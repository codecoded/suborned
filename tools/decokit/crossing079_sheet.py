"""Brief 079 note 2 (Vela): crossing079_board.py's linear frames to pictures (python3 -I tools/decokit/crossing079_sheet.py frames <dir>), then the review (... html <dir> <out_html>).
The post is the play camera's as far as numpy can say it: lens.json bloomPlay (threshold 1.05, intensity 0.45, a two-radius blur for scatter 0.75, tint #fff5e6), ACES, the play grade (exposure 0.02, contrast 10, saturation 35)."""

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[2]
BLOOM = json.loads((REPO / "unity/Assets/Suborned/Resources/Atmosphere/lens.json").read_text())["bloomPlay"]
TINT = np.array([1.0, 0.961, 0.902], np.float32)


def blur(a, r):
    out = a
    for _ in range(3):
        for ax in (0, 1):
            k = int(r) * 2 + 1
            pad = [(0, 0)] * out.ndim
            pad[ax] = (int(r) + 1, int(r))
            c = np.cumsum(np.pad(out, pad, mode="edge"), axis=ax, dtype=np.float64)
            n = out.shape[ax]
            out = ((np.take(c, range(k, k + n), axis=ax) - np.take(c, range(0, n), axis=ax)) / k).astype(np.float32)
    return out


def aces(x):
    return np.clip((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0, 1)


def srgb(c):
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.maximum(c, 0), 1 / 2.4) - 0.055)


def frame(p):
    lin = np.load(p)
    bright = np.maximum(lin - BLOOM["threshold"], 0)
    glow = (blur(bright, 2) * 0.45 + blur(bright, 6) * 0.35 + blur(bright, 16) * 0.2) * BLOOM["intensity"] * 2.2 * TINT   # URP's pyramid, as three radii at 1080p
    x = (lin + glow) * (2 ** 0.02) * 0.8   # 0.8 = the board's sun 2.3 against the game's 1.8
    g = srgb(aces(x))
    luma = g @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    g = luma[..., None] + (g - luma[..., None]) * 1.35
    g = np.clip((g - 0.5) * 1.10 + 0.5, 0, 1)
    Image.fromarray((g * 255 + 0.5).astype(np.uint8)).save(p.with_suffix("").with_suffix(".png"))


SKY = "/Users/sky/.t3/userdata/attachments/c5c3e6a1-50ff-47a1-a9a8-4be60485dd6f-5f873090-46f7-439a-8986-3da980b7a141.png"
PLAY_CROP = (480, 180, 1360, 760)  # the crossing, 1:1 at play zoom (880 x 580 px of the 1920 x 1080 frame, ortho 14, pitch 38)
CLOSE_CROP = (360, 40, 1560, 840)


def review(d, out):
    """The review page and its images beside it. Concept | now | proposed at play zoom and close."""
    d, out = Path(d), Path(out)
    fr = out.parent / "frames"
    fr.mkdir(parents=True, exist_ok=True)
    for p in sorted(d.glob("*.png")):
        c = PLAY_CROP if p.name.endswith("-play.png") else CLOSE_CROP
        Image.open(p).crop(c).save(fr / p.name, optimize=True)
    Image.open(SKY).save(fr / "sky-frame.png", optimize=True)
    strip = []
    for s, name in (("cand-stop-day-play", "stop, in sun"), ("cand-walk-shade-play", "walk, in shade")):
        for w, cap in (("w4", "4 cm: lost"), ("w7", "7 cm: reads"), ("f", "8 cm: the pick"), ("w9", "9 cm: the colour starts to shrink")):
            im = Image.open(d.parent / w / f"{s}.png").crop((860, 420, 1080, 640)).resize((440, 440), Image.NEAREST)
            im.save(fr / f"width-{w}-{s}.png", optimize=True)
            strip.append(f"<figure><img src='frames/width-{w}-{s}.png' loading=lazy><figcaption>{cap}, {name} (play zoom, 2x)</figcaption></figure>")
    costs = json.loads((out.parent / "cost.json").read_text())
    rows = []
    label = {"stop": "Stop (red)", "walk": "Walk (green)", "clearing": "Clearing (amber; studs go dark from the kerbs)", "day": "day, in sun", "shade": "in a building's shadow", "night": "night"}
    for st, l in (("stop", "day"), ("stop", "shade"), ("stop", "night"), ("walk", "day"), ("walk", "shade"), ("walk", "night"), ("clearing", "day")):
        for v, vn in (("play", "play zoom, 1:1 px (ortho 14, pitch 38)"), ("close", "close (ortho 3.3)")):
            if st == "stop" and l == "shade" and v == "play":
                ref = ("sky-frame.png", "Sky's frame from the fight video: the game now")
            elif st == "stop":
                ref = ("../../../009/concepts/crossing-future-eye.jpg", "concept: crossing-future-eye (stop display, frames, studs)")
            else:
                ref = ("../../../009/concepts/crossing-future-play-2.jpg", "concept: crossing-future-play-2 (the chosen one)")
            rows.append(f"<h3>{label[st]}, {label[l]} &middot; {vn}</h3><div class=r><figure><img src='frames/{ref[0]}' loading=lazy><figcaption>{ref[1]}</figcaption></figure>"
                        f"<figure><img src='frames/now-{st}-{l}-{v}.png' loading=lazy><figcaption>now (same stage, the committed shaders' numbers)</figcaption></figure>"
                        f"<figure><img src='frames/cand-{st}-{l}-{v}.png' loading=lazy><figcaption>proposed</figcaption></figure></div>")
    cost = "".join(f"<tr><td>{k}</td><td>{v['now']}</td><td>{v['proposed']}</td></tr>" for k, v in costs["table"].items())
    notes = "".join(f"<li>{n}</li>" for n in costs["notes"])
    out.write_text(f"""<!doctype html><meta charset=utf-8><title>079 crossing lights</title><style>
body{{background:#17302f;color:#f7f4ec;font:15px/1.45 -apple-system,Helvetica,sans-serif;margin:24px;max-width:1800px}}h1{{font-size:24px}}h2{{margin-top:32px}}h3{{margin:26px 0 8px;font-size:15px;color:#b0cdc7}}
.w{{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}}.r{{display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px}}figure{{margin:0}}img{{width:100%;display:block;border-radius:3px}}figcaption{{font-size:12.5px;color:#b0cdc7;padding-top:3px}}table{{border-collapse:collapse}}td,th{{border:1px solid #38605c;padding:5px 10px;text-align:left}}li{{margin:4px 0}}</style>
<h1>079 note 2 &middot; crossing lights: concept | now | proposed</h1><p>Sky: "the pedestrian lights look flat color, really ruin look of game", then "the lights should have metal ring around them, as they're embedded in the floor". Candidate only: same piece ids, placements, plan, sim and collision; nothing adopted, Unity untouched.</p>
<h2>What is approximate</h2><ul>{notes}</ul><h2>Cost</h2><table><tr><th></th><th>now</th><th>proposed</th></tr>{cost}</table><h2>How wide the ring has to be</h2><div class=w>{''.join(strip)}</div><h2>Frames</h2>{''.join(rows)}""")
    print("[079]", out)


if __name__ == "__main__":
    if sys.argv[1] == "html":
        review(sys.argv[2], sys.argv[3])
    elif sys.argv[1] == "frames":
        for p in sorted(Path(sys.argv[2]).glob("*.lin.npy")):
            if len(sys.argv) > 3 and sys.argv[3] not in p.name:
                continue
            frame(p)
            print("[079 post]", p.name)
