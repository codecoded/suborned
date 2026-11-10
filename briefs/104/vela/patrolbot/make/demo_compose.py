"""Brief 104 (Vela): joins demo.py's two passes into the demo clip and the contact sheet.
  python3 -I demo_compose.py <render_dir> <demo_dir>

render_dir holds close/####.png, play/####.png and play.json (the robot's place in the play frame). Writes demo_dir/patrolbot-demo.mp4 (H.264, 30 fps: the close pass, then
the play pass) and demo_dir/frames.png (8 frames, one per beat). Labels name the camera and the beat. The play pass stays at 1:1 (game size at 1080p) with a x3 inset of the
robot in its top-right corner, labelled as such."""

import json
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import demo_motion as M  # noqa: E402

BEATS = [(0, "1  Patrol: rolls in on a curve, leaning into it, arms swaying"), (150, "2  Stops, settles, looks around"), (258, "3  Alarm: the beacon lights and sweeps"),
         (290, "4  Draws: turns to the target, hatch opens, sidearm out, three shots"), (M.HIT, "5  Hit: a flinch, the hurt face"),
         (435, "6  Stands down: sidearm in, hatch shut, beacon off, rolls away")]
FONT = next((p for p in ("/System/Library/Fonts/Supplemental/Arial.ttf", "/System/Library/Fonts/Helvetica.ttc") if Path(p).exists()), None)
INSET, ZOOM = (180, 120), 3


def font(size):
    return ImageFont.truetype(FONT, size) if FONT else ImageFont.load_default(size)


def label(d, xy, text, size):
    f = font(size)
    x, y = xy
    l, t, r, b = d.textbbox((x, y), text, font=f)
    d.rectangle((l - 10, t - 7, r + 10, b + 7), fill=(20, 18, 16))
    d.text((x, y), text, font=f, fill=(246, 236, 214))


def beat(f):
    return [t for k, t in BEATS if f >= k][-1]


def compose(im, name, f, where, px):
    d = ImageDraw.Draw(im)
    if name == "play":
        w = where[str(f)]
        cx, cy = w["x"] * im.width, w["y"] * im.height
        box = (int(cx - INSET[0] / 2), int(cy - INSET[1] / 2), int(cx + INSET[0] / 2), int(cy + INSET[1] / 2))
        crop = im.crop(box).resize((INSET[0] * ZOOM, INSET[1] * ZOOM), Image.NEAREST)
        at = (im.width - crop.width - 40, 40)
        d.rectangle((box[0] - 2, box[1] - 2, box[2] + 2, box[3] + 2), outline=(246, 236, 214), width=2)
        im.paste(crop, at)
        d.rectangle((at[0] - 3, at[1] - 3, at[0] + crop.width + 3, at[1] + crop.height + 3), outline=(246, 236, 214), width=3)
        label(d, (at[0] + 12, at[1] + crop.height + 16), "x3 inset (the box): not game size", 22)
        label(d, (36, 36), f"Play camera: zoom 10, 38 deg down, yaw 30. Game size at 1080p: the robot is {px:.0f} px tall", 30)
    else:
        label(d, (36, 36), "Close camera, following", 30)
    label(d, (36, im.height - 76), beat(f), 30)
    return im


def main():
    src, dst = Path(sys.argv[1]), Path(sys.argv[2])
    dst.mkdir(parents=True, exist_ok=True)
    where = json.loads((src / "play.json").read_text())
    px = sorted(w["px_tall"] for w in where.values())[len(where) // 2]
    first = Image.open(src / "close/0000.png")
    w, h = first.size
    enc = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{w}x{h}", "-r", str(M.FPS), "-i", "-",
                            "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(dst / "patrolbot-demo.mp4")],
                           stdin=subprocess.PIPE)
    for name in ("close", "play"):
        for f in range(M.FRAMES):
            im = compose(Image.open(src / name / f"{f:04d}.png").convert("RGB"), name, f, where, px)
            enc.stdin.write(im.tobytes())
    enc.stdin.close()
    enc.wait()
    # the contact sheet: seven close frames and one play frame at 1:1 around the robot
    picks = [("close", 70, "1 Patrol"), ("close", 205, "2 Stop, look around"), ("close", 276, "3 Alarm"),
             ("close", 345, "4 Hatch open, sidearm out"), ("close", 350, "4 Shot"), ("close", M.HIT + 3, "5 Hit"), ("close", 545, "6 Stands down, rolls away"), ("play", 350, "Play camera, 1:1 crop (shot)")]
    tw, th = 640, 360
    sheet = Image.new("RGB", (tw * 4, th * 2), (20, 18, 16))
    for i, (name, f, text) in enumerate(picks):
        im = Image.open(src / name / f"{f:04d}.png").convert("RGB")
        if name == "play":
            c = where[str(f)]
            x, y = int(c["x"] * im.width - tw / 2), int(c["y"] * im.height - th / 2)
            im = im.crop((x, y, x + tw, y + th))
        else:
            im = im.resize((tw, th), Image.LANCZOS)
        label(ImageDraw.Draw(im), (16, th - 42), f"{text}  ({name} f{f})", 20)
        sheet.paste(im, ((i % 4) * tw, (i // 4) * th))
    sheet.save(dst / "frames.png")
    print(f"[demo104] {dst / 'patrolbot-demo.mp4'}, {dst / 'frames.png'}; play camera: robot {px:.0f} px tall (median)")


main()
