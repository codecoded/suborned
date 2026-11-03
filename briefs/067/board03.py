# Glyph, brief 067, board 03 (D and F): the lock reticle, its states, a re-target and an off-screen lock; the stack tab. python3 -I briefs/067/board03.py
import sys; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/067")
from g67 import *
OUTD = R + "briefs/067/"
FIGHT = R + "out/batch/shots/getaway-car-yaw210.png"; FIGHTH = R + "out/batch/shots/getaway-car-yaw210.hud.png"
SQR = R + "out/batch/shots/start-overhead-day.png"; SQH = R + "out/batch/shots/start-overhead-day.hud.png"
GUARD = R + "out/scratch/suture/shots0122/out/01-guard-scout-3300-listen.png"; GUARDH = R + "out/scratch/suture/shots0122/out/01-guard-scout-3300-listen.hud.png"

def d1():
    f = Frame(FIGHT, blot=[(452, 212, 552, 252)]); c = (492, 249)
    reticle(f, c, "locked", cams=[SCOUT, FIXER, ENF])
    pill(f, (c[0], c[1] - 13 - 6), None, "5 s", None, "fire", harm=True)
    return f
def state(kind):
    f = Frame(SQR); c = (1065, 495)
    if kind == "set-0": reticle(f, c, "set", cams=[SCOUT], scale=1.8, alpha=0.55, flash=1.0)
    elif kind == "set-1": reticle(f, c, "set", cams=[SCOUT], scale=1.1, flash=0.3)
    elif kind == "locked": reticle(f, c, "locked", cams=[SCOUT, FIXER])
    elif kind == "held": reticle(f, c, "held", cams=[SCOUT], turn=18)
    elif kind == "released": reticle(f, c, "released", cams=[SCOUT], scale=0.85, alpha=0.4)
    return f
def retarget():
    f = Frame(SQR); A = (1318, 288); B = (1386, 296)
    handoff(f, (A[0], A[1]), (B[0], B[1]), 0.8)
    reticle(f, A, "released", cams=[], scale=0.85, alpha=0.4); reticle(f, B, "set", cams=[SCOUT, FIXER], scale=1.25, flash=0.6)
    edge_chip_lock(f, 1862, 470, 0); return f
def stack():
    f = Frame(GUARD, blot=[(738, 380, 1182, 440)])
    pill(f, (1213, 556), "Hold", "Loop · 20 s", None, "loop", faces=[TECH], stack=3, stack_i=1)
    gring(f, (1152, 612), 0.5, CREAM, 1.8, dash=(5, 4), alpha=170)                                  # the patrol, the next step
    circ = [(1268 + 15 * math.cos(math.radians(a)), 563 + 15 * math.sin(math.radians(a))) for a in range(0, 361, 8)]; gline(f, circ, CREAM, 1.8, dash=(5, 4), alpha=170)   # the camera's head
    return f

def labelled(frames, box, labels, up=1):
    ims = [f.crop(box).convert("RGB") for f in frames]; ims = [im.resize((im.width * up, im.height * up), Image.LANCZOS) for im in ims]
    gap = 14; W = sum(im.width for im in ims) + gap * (len(ims) - 1); H = ims[0].height + 40; out = Image.new("RGB", (W, H), BG); dr = ImageDraw.Draw(out); x = 0
    for im, lab in zip(ims, labels): out.paste(im, (x, 0)); dr.text((x + im.width / 2, ims[0].height + 20), lab, font=board_font(26), fill=BCREAM, anchor="mm"); x += im.width + gap
    return out

if __name__ == "__main__":
    a = d1(); sts = [state(k) for k in ("set-0", "set-1", "locked", "held", "released")]; r = retarget(); s = stack()
    B = {"lock": budget(a, FIGHTH, False), "retarget": budget(r, SQH, False), "stack": budget(s, GUARDH, False)}
    items = [
        (a.crop((340, 120, 760, 400)).convert("RGB"), 0, 0, "The lock, over a moving target", "screen-space, so cover never hides it: disc_dark_24 26 px with the kit crosshair, the shooters' faces under it (16 px with their ring), the pill's tail 6 px above it. It follows the head 1:1."),
        (labelled(sts, (1010, 440, 1120, 550), ["set 0.18 s", "set settled", "locked: still", "held: turning", "released 0.25 s"], up=1), 900, 0, "The states", "set: 1.8 to 1.0 and a ring leaving · locked: still (ours is still; threats close in or kick) · held: 70 %, a dashed ring turning 0.25 turn/s (out of range, no line, drawing, walking in) · released: fades, 1.0 to 0.8."),
        (r.crop((1260, 240, 1920, 520)).convert("RGB"), 0, 760, "A re-target, and a lock off screen", "the old reticle releases while the new one sets; a hand-off line joins them for 0.25 s. The edge chip: plate_dark 22 × 52, the lock disc and a pointer turned toward the target."),
        (s.crop((1040, 420, 1440, 700)).convert("RGB"), 1380, 760, "Two or more under the cursor: the stack tab", "plate_dark 16 px on the pill's top edge, a dot each, the active one filled; the others wear a dashed cream ring (a robot's feet, a camera's head). Z, Shift+Z, Tab and the wheel step; the tail glides in 0.12 s."),
    ]
    print(compose(items, OUTD + "board-03-lock-and-stack.png", "067 · D and F · the lock reticle and the stack tab",
          "Over 0.2.122 frames (the final fight at the getaway car, the square, the Office front), the kit's own pieces at 2 px per game px. Targets in the re-target panel are stand-ins; placement is indicative."))
    import json; print(json.dumps(B))
