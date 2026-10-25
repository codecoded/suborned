# Glyph (2026-10-25), brief 058: boards for the camera ring, the camera's states, the done plates, the walk-off stopgap and the price lines.
#   python3 -I briefs/058/boards.py     -> briefs/058/board-0N-*.png and briefs/058/budget.json
# Everything is redrawn from the kit (glib.py) over the Oct 25 office-front capture at play zoom, yaw 210, 2 px per game px. Placement is indicative: Latch projects.
import sys, json; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/058")
from glib import *
SHOT = R + "out/batch/shots/office-front-yaw210.png"; HUDP = R + "out/batch/shots/office-front-yaw210.hud.png"
BLOT = [(922, 413, 994, 448)]; C = (950, 392); HEAD = (950, 385); FOOT = (960, 540)
OUTD = R + "briefs/058/"; B = {}
def neck(f, src=(955, 405, 966, 416), dst=(955, 414, 966, 450)):
    # the pole's neck behind the shot's own marker plate: the clean strip above it, stretched down over the inpainted patch
    a = f.base.crop(tuple(int(v * S) for v in src)).resize((int((dst[2] - dst[0]) * S), int((dst[3] - dst[1]) * S)), Image.BICUBIC)
    f.base.paste(a, (int(dst[0] * S), int(dst[1] * S))); return f
def fresh(): return neck(Frame(SHOT, blot=BLOT))

# ------------------------------------------------------------------ 01 the ring on a camera
LOOP = tokens("Loop", [("time", "38 s"), ("trail", "+1")])
DIS = tokens("Disable", [("time", "28 s"), ("trail", "+2"), ("alert", "1")])
FIRE = tokens("Fire", [("trail", "+2"), ("alert", "≥2")], loud=True)
FIRE_FAR = tokens("Fire", [("trail", "+2"), ("alert", "≥2")], loud=True)
CROP1 = (500, 238, 1420, 678)
def ring_tech(lit=0, hold=None):
    f = fresh(); segs = [dict(icon="loop"), dict(icon="disable"), dict(icon="fire", violent=True)]
    ring_wheel(f, C, segs, lit=lit, hold=hold)
    ring_pill(f, C, 0, LOOP, lit=lit == 0); ring_pill(f, C, 120, DIS, lit=lit == 1); ring_pill(f, C, 240, FIRE, lit=lit == 2); return f
def ring_other():
    f = fresh(); ring_wheel(f, C, [dict(icon="fire", violent=True)], lit=0); ring_pill(f, C, 0, FIRE_FAR, lit=True); return f
def board1():
    a = ring_tech(); b = ring_tech(lit=2, hold=(2, 0.6)); c = ring_other()
    B["ring-tech-open"] = budget(a, HUDP); B["ring-tech-hold"] = budget(b, HUDP); B["ring-other"] = budget(c, HUDP)
    sheet([(a.crop(CROP1), "Tech on a camera: Loop · Disable · Fire", "the likely way (Loop) lit at 12 o'clock; Fire dashed with the loud mark, as the ring draws a harmful way"),
           (b.crop(CROP1), "Fire held: the 0.5 s hold fills the wedge from the middle out", "turquoise-light at 75 %, takes when full; Loop and Disable are one click"),
           (c.crop(CROP1), "Any other armed agent (Fixer, Scout, Enforcer): Fire alone", "one wedge is the whole ring; no time on a shot (it is down at once), the walk is in the hold")],
          2, OUTD + "board-01-ring-on-a-camera.png", "058 · the ring on a camera",
          "Over the office-front capture, play zoom, yaw 210, 2 px per game px. One line per way: verb, time (walk + act), Trail, Alert, then what the way leaves.", cap_h=104)
board1()

# ------------------------------------------------------------------ 02 the camera's four states
def head_dead(f, shot=False):
    if shot: disc(f.hud, HEAD[0], HEAD[1] - 20, 24, "fire", light=True)       # shot out: the light disc and the gun, harm's own pair
    else: disc(f.hud, HEAD[0], HEAD[1] - 20, 24, "disable")
CROP2 = (680, 300, 1220, 640)
def st_live():
    f = fresh(); fan(f, FOOT, 12, -25, 70); return f
def st_looped():
    f = fresh(); fan(f, FOOT, 12, -25, 70, fill=False, rim_alpha=0.5, rim_all_dashed=True)
    window_marker(f, 956, 444, "eye_shut", "1:42", 102 / 120); return f
def st_cut():
    f = fresh(); head_dead(f); window_marker(f, 956, 444, "p_alert", "14", 14 / 20); return f
def st_shot():
    f = fresh(); head_dead(f, True); return f
def board2():
    S4 = [st_live(), st_looped(), st_cut(), st_shot()]
    for n, f in zip(["live", "looped", "cut", "shot"], S4): B["camera-" + n] = budget(f, HUDP, extra_world=True)
    sheet([(S4[0].crop(CROP2), "Live (learned): the quiet cone", "red, 13 % fading to 1 %, rim 55 %, dashed far edge: the 057 rest strength, unchanged"),
           (S4[1].crop(CROP2), "Looped: the cone is hollow and dashed all round; the marker counts down", "same red at rest strength, no fill, every edge dashed; the plate is the window marker, 2:00 draining"),
           (S4[2].crop(CROP2), "Cut: no cone, scissors on the head, the desk's clock above", "the clock is the time to the desk's next scan; at the report it goes, the disc stays"),
           (S4[3].crop(CROP2), "Shot out: no cone, a gun on the light disc, no clock", "gunfire has already raised the alarm; the camera is dead to the end")],
          2, OUTD + "board-02-camera-states.png", "058 · what a camera looks like: live, looped, cut, shot",
          "Marker plates are the as-built window marker (plate_dark, 38 px, 2.5 px underline). Dead discs: the kit disc_dark_24 with the new disable icon (cut), disc_light_24 with fire (shot).", cap_h=104)
board2()

# ------------------------------------------------------------------ 03 the done plates
TECH = "portraits/cameo_tech_normal"; ENF = "portraits/cameo_enforcer_normal"
def done_loop():
    f = fresh(); mk = window_marker(f, 956, 444, "eye_shut", "2:00", 1.0)
    chip(f, 956, mk[1] - 8, TECH, "ok", [("loop", "Camera looped")]); return f
def done_cut():
    f = fresh(); head_dead(f); mk = window_marker(f, 956, 444, "p_alert", "14", 1.0)
    chip(f, 956, mk[1] - 8, TECH, "ok", [("disable", "Camera cut")]); return f
def done_shot():
    f = fresh(); head_dead(f, True); chip(f, 956, HEAD[1] - 20 - 12 - 8, ENF, "ok", [("fire", "Camera shot out")]); return f
def marker_strip():
    f = fresh(); cy = 470
    for i, (ic, lab, fr_, low) in enumerate([("eye_shut", "2:00", 1.0, False), ("eye_shut", "1:12", 0.6, False), ("eye_shut", "0:09", 0.075, True), ("p_alert", "14", 0.7, False), ("p_alert", "3", 0.15, False)]):
        window_marker(f, 560 + i * 150, cy, ic, lab, fr_, low, dim=0.75 if low else 1.0)
    return f
def board3():
    a, b, c, d = done_loop(), done_cut(), done_shot(), marker_strip()
    for n, f in zip(["loop", "cut", "shot", "strip"], [a, b, c, d]): B["done-" + n] = budget(f, HUDP)
    CR = (700, 215, 1220, 480)
    sheet([(a.crop((700, 190, 1220, 480)), "Loop: Camera looped, with its 2:00", "chip 4.2 s; the 2:00 rides the marker under it"),
           (b.crop((700, 190, 1220, 480)), "Disable: Camera cut, and when the desk will see it", "the siren marker is the time to the desk's next scan"),
           (c.crop((700, 215, 1220, 480)), "Shoot: Camera shot out", "the shooter's face; no clock (the shot already raised Alert)"),
           (d.crop((470, 395, 1230, 485)), "Marker states: 2:00 · 1:12 · 0:09 (amber, blinking) · desk 14 · 3", "m:ss when the window is 60 s or longer (the loop), plain seconds under that (the desk); amber and blinking in the last 10 s")],
          2, OUTD + "board-03-done-plates.png", "058 · the done plates",
          "Never \"Camera dark\" for a loop. Words are Fable's keys (placeholders here); icons are kit singles (loop, disable, fire, p_time, p_alert, eye_shut).", cap_h=104)
board3()

# ------------------------------------------------------------------ 04 the walk-off stopgap: the Tech's plate, and the 0.5 s hold
CAB = (1112, 672); CLICK = (1230, 560)
def roster_plate(f, state):
    # the Tech's row (F3): the activity plate beside his portrait, x 104..280, y 241..277 (as the shot's own timer-3); the shot's plate is taken out first
    x, y, w, h = 108, 244, 172, 30
    patch = f.base.crop((int(300 * S), int(246 * S), int(310 * S), int(272 * S))).resize((int(w * S), int(h * S)))
    plate_dark(f.hud, x, y, w, h)
    icon(f.hud, "s_looping", x + 17, y + h / 2, 15)
    stroke(f.hud, [(x + 36, y + h / 2), (x + w - 34, y + h / 2)], INKDEEP, 6, alpha=160, round_caps=True)
    stroke(f.hud, [(x + 36, y + h / 2), (x + w - 34, y + h / 2)], TURQL, 6, alpha=150, round_caps=True)                  # full, still: nothing counts down while he holds
    icon(f.hud, "lock", x + w - 17, y + h / 2, 14)
    if state == "squad":
        stroke(f.hud, [(x - 3, y - 3), (x + w + 3, y - 3), (x + w + 3, y + h + 3), (x - 3, y + h + 3)], TURQL, 2.0, closed=True, alpha=230)
def scene_roster(state):
    f = Frame(SHOT, blot=BLOT + [(104, 238, 284, 280)]); roster_plate(f, state); return f
def scene_walkoff(st, frac=0):
    f = fresh()
    gring(f, CAB, 0.55, CREAM, 2.4)                                  # where he stands: solid ring (the held spot)
    gring(f, CLICK, 0.5, CREAM, 2.4)                                 # the click: a walk's ring, solid as built
    gline(f, [(CAB[0] + 12, CAB[1] - 10), (CLICK[0] - 14, CLICK[1] + 8)], CREAM, 2.4, dash=(7, 6))
    head, body = ("Let go", "Camera comes back") if st == "ready" else ("Hold", "Camera comes back")
    vpill(f, (CLICK[0], CLICK[1] - 22), head, body, "loop", faces=[TECH], state=st, frac={"offer": 0, "charge": 0.55, "ready": 1}[st] * (1 if st != "ready" else 0)); return f
def board4():
    r1, r2 = scene_roster("hold"), scene_roster("squad"); w1, w2, w3 = scene_walkoff("offer"), scene_walkoff("charge"), scene_walkoff("ready")
    for n, f in zip(["roster-hold", "roster-squad", "walk-offer", "walk-charge", "walk-ready"], [r1, r2, w1, w2, w3]): B["stopgap-" + n] = budget(f, HUDP)
    RC = (0, 205, 420, 335); WC = (980, 440, 1400, 720)
    st = Image.new("RGB", (r1.crop(RC).width, r1.crop(RC).height * 2 + 12), BG); st.paste(r1.crop(RC).convert("RGB"), (0, 0)); st.paste(r2.crop(RC).convert("RGB"), (0, r1.crop(RC).height + 12))
    sheet([(st, "Tech's plate: holding (top), after a squad move (bottom)", "still full bar and the lock, nothing counts down; the plate rims 0.6 s"),
           (w1.crop(WC), "Offer: a click that would take him off", "a tap does nothing; the line is what he loses"),
           (w2.crop(WC), "Charge: hold 0.5 s", "turquoise-light sweep at 40 % inside the pill"),
           (w3.crop(WC), "Ready: Let go", "2 px ring, glow, scale 1.05; releasing walks him off and the camera comes back")],
          2, OUTD + "board-04-walkoff-stopgap.png", "058 · the walk-off stopgap (host only, ship now)",
          "The plate is the roster's activity plate; the pill is the verb pill (pill_status 38 px); the disc is disc_dark_24 with the loop icon. Words are Fable's keys.", cap_h=104)
board4()

# ------------------------------------------------------------------ 05 the price lines
def price_row(tokens_, w_note, bgbox=(980, 640, 1380, 700), lit=False):
    f = fresh(); strip = f.base.crop((int(bgbox[0] * S), int(bgbox[1] * S), int(bgbox[2] * S), int(bgbox[3] * S))).convert("RGBA")
    pw = kd.pill_width(tokens_) / S; kd.draw_pill(strip, tokens_, G(14) + kd.G(4), strip.height / 2, lit=lit); return strip.crop((0, 0, int(G(400)), strip.height)), pw
ALT_LOOP = tokens("Loop", [("eye", "shut")]); 
def alt(word, parts):
    t = [("word", word)]
    for ic, v in parts: t += [("gap", 11), ("icon", ic), ("gap", 1), ("num", v)]
    return t
def board5():
    rows = [("Loop", LOOP, "ship: Fable's price.verbs.loop (name, time, trail); time = walk 18 + act 20; the 2:00 rides the marker"),
            ("Disable", DIS, "ship: price.verbs.disable (name, time, trail, alert); Alert 1 is the first report, Alert 2 the second"),
            ("Fire at a camera or a car", tokens("Fire", [("trail", "+2"), ("alert", "≥2")]), "ship: examples.fire-camera; +2 once for the shooting, +0 if the cell already fired at it, never \"/hit\""),
            ("Option, not in the keys: Loop with its run time", tokens("Loop", [("time", "38 s"), ("trail", "+1"), ("lasts", "2:00")]), "Motive's order line; a `lasts` field: the loop icon and m:ss, after trail"),
            ("Option, not in the keys: Disable with the desk's delay", tokens("Disable", [("time", "28 s"), ("trail", "+2"), ("alert", "1"), ("in", "14 s")]), "Motive's order line; an `in` field: a dim double arrow and n s, after the siren"),
            ("Alt: Loop with every field", alt("Loop", [("s_moving", "18 s"), ("p_time", "20 s"), ("p_trail", "+1"), ("p_alert", "0"), ("eye_shut", "none")]), "Alt splits walk from act and shows the zeros, as built"),
            ("Alt: Disable with every field", alt("Disable", [("s_moving", "18 s"), ("p_time", "10 s"), ("p_trail", "+2"), ("p_alert", "1"), ("eye_shut", "none")]), "")]
    panels = []; widths = {}
    for name, tk, note in rows:
        im, pw = price_row(tk, note); panels.append((im, name, "%d game px wide%s" % (round(pw), " · " + note if note else ""))); widths[name] = round(pw)
    B["price-line-widths"] = widths
    sheet(panels, 1, OUTD + "board-05-price-lines.png", "058 · the price lines, one line per way", "On a plain strip of the capture's pavement. Every line is shorter than the shipped Pressure line (315 px) except the Alt lines, which show everything.", cap_h=100)
board5()
json.dump(B, open(OUTD + "budget.json", "w"), indent=1); print(json.dumps(B, indent=1))
