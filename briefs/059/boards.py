# Glyph (2026-10-25), brief 059: boards for the rival car and its approach, what a Watch teaches, Vale's car with the driver turned, and Drive.
#   python3 -I briefs/059/carsprite.py && python3 -I briefs/059/boards.py     -> briefs/059/board-0N-*.png and briefs/059/budget.json
# Redrawn from the kit (briefs/058/glib.py) over real captures at play zoom: the square at yaw 30 (start-overhead-day), the Office front and the bay at yaw 210
# (done-call-window, office-front), the service door at yaw 210 (briefs/023/latch). Placement is indicative: Latch projects from the site export.
import sys, json; sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/058")
from glib import *
SQR = R + "out/batch/shots/start-overhead-day.png"; SQH = R + "out/batch/shots/start-overhead-day.hud.png"
BAY = R + "briefs/044/build/done-call-window.png"; OFH = R + "out/batch/shots/office-front-yaw210.hud.png"
YARD = R + "briefs/023/latch/service-door-210.png"
CAR_SPR = R + "briefs/059/rival_car_standin.png"
OUTD = R + "briefs/059/"; B = {}
TECH = "portraits/cameo_tech_normal"; ENF = "portraits/cameo_enforcer_normal"; FIX = "portraits/cameo_fixer_normal"
VALE = "badges/npc_councillor"

def rival_ring(fr, c, r=0.55):
    gring(fr, c, r, VIOLET, 2.4, under=True); gring(fr, c, r + 0.4, VIOLET, 1.8, dash=(3.5, 3.5), alpha=210)
def vline(fr, pts, ticks=True):
    gline(fr, pts, VIOLET, 2.2, under=True)
    if ticks: diamond_ticks(fr, pts, every_m=2.4)
def lane_slice(L, frm):
    """the part of a polyline lane from the point `frm` (a vertex index, plus an offset in px along that segment)"""
    return L[frm:]

# ============================================================================ 01 the rival car arrives (the square, yaw 30)
CARSQ = (938, 838)                            # the cell's car at the square's bay
LANE = [(170, 60), (250, 150), (340, 260), (450, 385), (560, 505), (640, 600), (690, 665), (725, 705), (748, 727)]
def lane_pt(t):
    segs = list(zip(LANE[:-1], LANE[1:])); L = [math.dist(a, b) for a, b in segs]; d = t * sum(L)
    for (a, b), l in zip(segs, L):
        if d <= l: return (a[0] + (b[0] - a[0]) * d / l, a[1] + (b[1] - a[1]) * d / l)
        d -= l
    return LANE[-1]
def rest_of_lane(t):
    p = lane_pt(t); out = [p]; segs = list(zip(LANE[:-1], LANE[1:])); L = [math.dist(a, b) for a, b in segs]; d = t * sum(L); acc = 0
    for (a, b), l in zip(segs, L):
        if acc + l > d: out.append(b)
        acc += l
    return out
def sq_turns_in():
    f = Frame(SQR); p = lane_pt(0.12); pts = rest_of_lane(0.12)
    vline(f, pts); paste_sprite(f.world, CAR_SPR, p[0] - 4, p[1] - 12)
    disc(f.hud, LANE[-1][0], LANE[-1][1] - 2, 22, "arrive")
    return f
def sq_drop():
    f = Frame(SQR); d = LANE[-1]
    paste_sprite(f.world, CAR_SPR, d[0] - 4, d[1] - 14)
    posts = [(1060, 700), (800, 600), (1120, 900)]
    for p in posts:
        vline(f, [(d[0] + 6, d[1] + 18), p], ticks=False) if False else None
        gline(f, [(d[0] + 10, d[1] + 20), p], VIOLET, 1.8, dash=(2.5, 5), under=True)
        rival_ring(f, p)
    vline(f, [(d[0] + 30, d[1] + 22), (860, 800), (905, 835), (1010, 925)])                       # the car goes on, round by Rocket Lane
    edge_chip(f, 1075, 985, None, 50, face_badge="badges/signal_rival")
    return f
def board1():
    a, b = sq_turns_in(), sq_drop()
    B["rival-turns-in"] = budget(a, SQH, True); B["rival-drops"] = budget(b, SQH, True)
    CR = (150, 10, 1250, 1075)
    sheet([(a.crop(CR), "2:35 · the rival car turns in", "a violet approach line from the car to its stop, diamond ticks every 2.4 m; it shortens as the car drives; the stop is an arrive disc"),
           (b.crop(CR), "2:46 · it stops, three get out and post round the cell's car", "a dotted walk to each post, each post a solid ring and a dashed ring (as built); the car's line goes on, with a pointer chip at the edge")],
          2, OUTD + "board-01-rival-car-arrives.png", "059 · the rival car arrives, in plain sight",
          "The square at yaw 30, play zoom, 2 px per game px. The car is a stand-in cut from the official car (briefs/059/carsprite.py): Vela's sedan in rival trim replaces it. The feed beat is the card as built; words are Fable's.", cap_h=104)
board1()

# ============================================================================ 02 what a Watch teaches
def bay_frame():
    f = Frame(BAY)
    # the old pop-in crew taken out: asphalt from beside it, feathered in
    W0, H0 = int(130 * S), int(110 * S); src = f.base.crop((int(1500 * S), int(800 * S), int(1500 * S) + W0, int(800 * S) + H0))
    m = Image.new("L", (W0, H0), 0); ImageDraw.Draw(m).rectangle((int(14 * S), int(14 * S), W0 - int(14 * S), H0 - int(14 * S)), fill=255); m = m.filter(ImageFilter.GaussianBlur(int(9 * S)))
    f.base.paste(src, (int(1648 * S), int(752 * S)), m)
    return f
OFF_CAR = (1640, 715); RIV_PARK = (1500, 885); PSI_ROAD = -28.7
def office_parked(watch):
    f = bay_frame()
    car_ghost(f, RIV_PARK, PSI_ROAD, VIOLET, dash=None, alpha=255, fill_alpha=60, with_disc=True)
    rival_ring(f, (1405, 945))                                         # the lookout, posted on the far kerb facing the official door
    if watch:
        window_marker(f, RIV_PARK[0], RIV_PARK[1] - 56, "nextevent", "24", 0.6)
        pts = [(RIV_PARK[0] + 20, RIV_PARK[1] - 30), (1590, 800), (OFF_CAR[0] - 10, OFF_CAR[1] + 28)]
        gline(f, pts, VIOLET, 2.0, dash=(2.5, 5), under=True); chevrons(f, pts, 3.0, VIOLET, 5.5)       # it follows Vale's car: a dotted line from the car to his
    return f
def sq_spring():
    f = Frame(SQR); gring(f, CARSQ, 20, VIOLET, 1.8, dash=(7, 6), alpha=200)
    for p in [(1060, 700), (800, 600), (1120, 900)]: rival_ring(f, p)
    paste_sprite(f.world, CAR_SPR, 748, 715)
    window_marker(f, 748, 655, "nextevent", "9", 0.4)
    return f
def board2():
    a, b, c = office_parked(False), office_parked(True), sq_spring()
    B["rivals-parked"] = budget(a, OFH, True); B["rivals-watch-office"] = budget(b, OFH, True); B["rivals-watch-square"] = budget(c, SQH, True)
    OC = (1290, 610, 1900, 960)
    ab = Image.new("RGB", (a.crop(OC).width * 2 + 24, a.crop(OC).height), BG); ab.paste(a.crop(OC).convert("RGB"), (0, 0)); ab.paste(b.crop(OC).convert("RGB"), (a.crop(OC).width + 24, 0))
    sheet([(ab, "3:10 · parked across from Vale's car, the lookout posts (left); after a Watch on the car (right)", "left: a car and a man on a ring, nothing else. right: a clock pip (the next move, draining) and a dotted violet line with chevrons from the car to Vale's: it follows him"),
           (c.crop((100, 280, 1900, 1075)), "After a Watch, on hover or Alt: the 20 m spring ring round the cell's car", "a thin dashed violet ellipse, 20 m by the ground's own scale; the three posts are the rings already on the ground")],
          1, OUTD + "board-02-watch-reads-the-rivals.png", "059 · what a Watch on the rivals teaches",
          "Three facts, three marks: the next move (clock pip), where it goes (a dotted line), how close is too close (the spring ring). Nothing is written. The parked car is a footprint stand-in (the model draws in game).", cap_h=104)
board2()

# ============================================================================ 03 Vale's car, driver turned
PSI_DRIVE = -143.8                      # ground heading whose screen direction is up-left, slope 24 deg (the service drive's axis in this view)
def g(x, y): return (x * 0.96, y * 0.96)      # displayed (2000-wide) coordinates of the yard capture -> game px
YARD_BAY = g(985, 690)
YARD_ROUTE = [g(1990, 1100), g(1620, 950), g(1300, 820), g(1090, 730), YARD_BAY]
def car_rear_door(c, psi): return ground_xy(c, -0.3, -1.5, psi)
def bay_front():
    f = bay_frame(); route = [(1640, 748), (1735, 800), (1830, 840), (1925, 870)]
    gline(f, route, CREAM, 2.4, dash=(7, 6)); chevrons(f, route, 3.0, CREAM, 6.0)
    route_plate(f, 1770, 783, [("icon", "car"), ("txt", "6:13"), ("icon", "pointer"), ("txt", "6:25")])
    edge_chip(f, 1884, 905, "car", 20); return f
def yard(state):
    f = Frame(YARD); rt = YARD_ROUTE
    if state == "route":
        gline(f, rt, CREAM, 2.4, dash=(7, 6)); chevrons(f, rt, 3.0, CREAM, 6.0)
        route_plate(f, *g(1420, 880), [("icon", "car"), ("txt", "6:13"), ("icon", "pointer"), ("txt", "6:25")])
    car_ghost(f, YARD_BAY, PSI_DRIVE, CREAM, dash=None if state == "parked" else (6, 5), alpha=255 if state == "parked" else 200)
    rd = car_rear_door(YARD_BAY, PSI_DRIVE)
    if state == "parked":
        window_marker(f, YARD_BAY[0] + 10, YARD_BAY[1] - 56, "car", "1:52", 112 / 125)
        gring(f, rd, 0.8, CREAM, 2.6); disc(f.hud, rd[0] + 25, rd[1] - 17, 22, "take")      # the hand-off ring: where the Enforcer stands to take
    return f
def board3():
    a, b, c = bay_front(), yard("route"), yard("parked")
    for n, f in zip(["bay", "yard-route", "yard-parked"], [a, b, c]): B["vale-car-" + n] = budget(f, OFH, True)
    YC = (520, 380, 1500, 1010)
    sheet([(a.crop((1330, 560, 1920, 945)), "6:10 · Vale is in; the car leaves the front", "hover the car: a dashed route, the leave and the arrive times on a plate, an edge chip where it goes off screen"),
           (b.crop(YC), "The drive round, 6:13 to 6:25", "the same dashed route arrives at the service bay; the plate keeps the two times; the car is a footprint here (the model draws in game)"),
           (c.crop(YC), "6:25 · parked: it waits for the Take", "the hand-off ring at the rear door (where the Enforcer stands to take), the wait pip counting to 8:30; the footprint turns solid")],
          3, OUTD + "board-03-vale-car-driver-turned.png", "059 · Vale's car with the driver turned",
          "Route marks are cream over ink-deep (the walk and run language); plates and the pip are plate_dark; the icon on the pip is the car. Yard at yaw 210, front at yaw 210.", cap_h=104)
board3()

# ============================================================================ 04 Drive
STAND_YARD = g(1310, 850)     # the service-drive stand: 1 m behind Vale's car, nose out
def sq_drive():
    f = Frame(SQR)
    car_ghost(f, CARSQ, -28.0, CREAM, dash=(6, 5), with_disc=False)          # the bay stand under the car
    edge_chip(f, 1862, 640, "car", 0); return f
def yard_before():
    f = Frame(YARD); car_ghost(f, YARD_BAY, PSI_DRIVE, CREAM, dash=(6, 5), alpha=190)
    rt = YARD_ROUTE; gline(f, rt, CREAM, 2.0, dash=(7, 6), alpha=170)
    ghost = ground_xy(YARD_BAY, -(CAR_L + 1.0), 0, PSI_DRIVE)
    car_ghost(f, ghost, PSI_DRIVE + 180, CREAM, dash=(3, 5), alpha=130, with_disc=False, nose=False)
    # shut until Vale's car is parked: the lock and the holder's cameo, the way a shut Go carries them
    plate_dark(f.hud, ghost[0] - 30, ghost[1] - 36, 60, 26); icon(f.hud, "lock", ghost[0] - 15, ghost[1] - 23, 14); cameo(f.hud, ghost[0] + 10, ghost[1] - 23, 18, VALE)
    return f
def yard_hover():
    f = Frame(YARD); car_ghost(f, YARD_BAY, PSI_DRIVE, CREAM, dash=None, with_disc=False)
    ghost = ground_xy(YARD_BAY, -(CAR_L + 1.0), 0, PSI_DRIVE)
    rt = [g(1990, 1100), g(1700, 990), g(1500, 930), ghost]
    gline(f, rt, CREAM, 2.4, dash=(7, 6)); chevrons(f, rt, 3.0, CREAM, 6.0)
    car_ghost(f, ghost, PSI_DRIVE + 180, TURQL, dash=None, alpha=255, with_disc=True, nose=True)
    vpill(f, (ghost[0], ghost[1] - 40), "Hold", "Drive · 28 s", "car", faces=[TECH]); return f
def board4():
    a, b, c = sq_drive(), yard_before(), yard_hover()
    for n, f in zip(["square", "yard-shut", "yard-hover"], [a, b, c]): B["drive-" + n] = budget(f, SQH if n == "square" else OFH, True)
    YC = (520, 380, 1500, 1010)
    sheet([(a.crop((150, 400, 1900, 1075)), "The car or an operative in it selected: its own stand, and a pointer to the other", "the stand under the car is a dashed footprint; the yard's stand is off screen, so an edge chip with the car and an arrow points at it"),
           (b.crop(YC), "Before Vale's car is parked: the yard stand is shut", "dimmed footprint, the lock and Vale's cameo as the holder (the same marks a shut Go wears); no route yet"),
           (c.crop(YC), "After it parks, on hover: the route and the price", "turquoise-light footprint, the route preview from the square, the verb pill with the price in seconds, the Tech's face")],
          2, OUTD + "board-04-drive.png", "059 · Drive: stands, a route, one line",
          "Pill: pill_status 38 px, disc_dark_24 with the car icon. The brief writes \"Drive · 0:28\"; the game prints times as seconds (\"28 s\") everywhere, so the pill does too.", cap_h=104)
board4()
json.dump(B, open(OUTD + "budget.json", "w"), indent=1); print(json.dumps(B, indent=1))
