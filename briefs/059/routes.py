# Brief 059 (Waymark): drive and walk measures on the Handover site, and the top-down picture.
# Reads sim/data/handover.site.json (drive grid 'r'/'y'/'#', walk grid '.'/'#', places), never writes inputs.
# usage: python3 -I routes.py <site.json> <out.png> <out.json>
import json, heapq, math, sys
from PIL import Image, ImageDraw, ImageFont

site = json.load(open(sys.argv[1]))
D, W = site['drive'], site['grid']
X0, Y0, C, NX, NY = D['x0'], D['y0'], D['cell'], D['nx'], D['ny']
ROAD_CM_S, YARD_CM_S, WALK_CM_S, RIVAL_CM_S, CARRY_CM_S = 500, 200, 140, 160, 140

def P(name): return tuple(site['places'][name]['at'])
def cell(p): return (int((p[0] - X0) // C), int((p[1] - Y0) // C))
def cen(i, j): return (X0 + (i + .5) * C, Y0 + (j + .5) * C)

def free_drive(i, j): return 0 <= i < NX and 0 <= j < NY and D['rows'][i][j] != '#'
def free_walk(i, j): return 0 <= i < NX and 0 <= j < NY and W['rows'][i][j] == '.'

def edge_cost(i, j, free, keep):
    # a car keeps off the carriageway's edge where it can (engine: lane = 165 cm from the edge); a small cost per close edge
    if not keep: return 0
    for r in range(1, 4):
        for di in range(-r, r + 1):
            for dj in range(-r, r + 1):
                if max(abs(di), abs(dj)) == r and not free(i + di, j + dj): return (4 - r) * 25
    return 0

def nearest(p, free):
    ci, cj = cell(p); best = None
    for i in range(ci - 12, ci + 13):
        for j in range(cj - 12, cj + 13):
            if free(i, j):
                q = cen(i, j); d = (q[0] - p[0]) ** 2 + (q[1] - p[1]) ** 2
                if best is None or d < best[0]: best = (d, (i, j))
    return best[1]

def astar(a, b, free, keep=False):
    a, b = nearest(a, free), nearest(b, free)
    g = {a: 0}; prev = {}; pq = [(0, a)]
    while pq:
        f, u = heapq.heappop(pq)
        if u == b: break
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                if di == dj == 0: continue
                v = (u[0] + di, u[1] + dj)
                if not free(*v): continue
                if di and dj and (not free(u[0] + di, u[1]) or not free(u[0], u[1] + dj)): continue
                ng = g[u] + (141 if di and dj else 100) + edge_cost(v[0], v[1], free, keep)
                if ng < g.get(v, 1e18):
                    g[v] = ng; prev[v] = u
                    heapq.heappush(pq, (ng + max(abs(v[0] - b[0]), abs(v[1] - b[1])) * 100, v))
    path = [b]
    while path[-1] != a: path.append(prev[path[-1]])
    return [cen(*q) for q in path[::-1]]

def length(pts): return sum(math.dist(pts[k], pts[k + 1]) for k in range(len(pts) - 1))

def drive_time(pts):
    road = yard = 0.0
    for k in range(len(pts) - 1):
        seg = math.dist(pts[k], pts[k + 1]); i, j = cell(pts[k + 1])
        if D['rows'][i][j] == 'r': road += seg
        else: yard += seg
    return road / 100, yard / 100, road / ROAD_CM_S + yard / YARD_CM_S

def mmss(s): return '%d:%04.1f' % (s // 60, s % 60)

out = {}
# the two stands this brief adds (site frame, cm): the getaway car's stand on the service drive, nose out, 1 m behind the official car in serviceBay;
# the rival car's way in at the window's edge on Orbit Walk (Pylon Row corner), the east street the top-down test names
SB = P('service-bay')                       # official car centre in serviceBay, nose -x
GS = (SB[0] + 230 + 100 + 230, SB[1])       # getaway stand on the drive: official tail + 1 m gap + half a car
RIV_IN = (3750, 1575)                       # Orbit Walk at the window's east edge (block y 15.75)
RIV_OUT_E = (8350, 1575)                    # Apron Road at the window's east edge, the official car's way on east
SQUARE_STOP = (3400, -3800)                 # Orbit Walk's north lane by the square: the rival car's drop stop (and its stop when it comes for your car)
RIV_PARK = (9050, -3100)                    # proposed: rival-park on Apron Road's south kerb (eastbound), across from Vale's car
LOOKOUT = (9400, -2700)                     # proposed: the rivals' lookout post, the far pavement of Apron Road facing the official door
MOUTH = (8200, -1500)                       # Apron Road at the service drive's mouth

legs = {
    'official-to-service': (P('d:Official drop-off'), SB),
    'getaway-to-service-drive': (P('d:Getaway bay'), GS),
    'rivals-in-to-drop': (RIV_IN, SQUARE_STOP),
    'rivals-drop-to-park': (SQUARE_STOP, RIV_PARK),
    'rivals-in-direct-today': (RIV_IN, P('rival-park')),
    'official-leaves-east': (SB, RIV_OUT_E),
    'rivals-tail-to-mouth': (RIV_PARK, MOUTH),
    'rivals-to-square': (RIV_PARK, SQUARE_STOP),
    'rivals-tip-leaves': (RIV_PARK, RIV_IN),
    'rivals-in-to-square': (RIV_IN, SQUARE_STOP),
}
paths = {}
for k, (a, b) in legs.items():
    pts = astar(a, b, free_drive, keep=True); paths[k] = pts
    road, yard, t = drive_time(pts)
    out[k] = {'m': round(length(pts) / 100, 1), 'road_m': round(road, 1), 'yard_m': round(yard, 1), 's': round(t, 1)}

walks = {
    'enforcer-start-to-service-lane': (P('crew:enforcer'), P('d:Service lane')),
    'service-lane-to-getaway-bay': (P('d:Service lane'), P('d:Getaway bay')),
    'official-rear-door-to-getaway-stand': ((SB[0] + 80, SB[1] - 190), (GS[0] - 120, GS[1] - 190)),
    'rival-park-to-lookout': (RIV_PARK, LOOKOUT),
    'drop-to-ambush-1': (SQUARE_STOP, P('ambush-1')),
    'drop-to-ambush-2': (SQUARE_STOP, P('ambush-2')),
    'drop-to-ambush-3': (SQUARE_STOP, P('ambush-3')),
    'lookout-to-ambush-4': (LOOKOUT, P('ambush-4')),
    'lookout-to-service-drive': (LOOKOUT, (GS[0], GS[1] - 600)),
    'square-ambush-1-to-service-drive': (P('ambush-1'), (GS[0], GS[1] - 600)),
    'mouth-to-official-car': (MOUTH, (SB[0] + 80, SB[1] - 190)),
    'getaway-bay-to-service-lane-cell': (P('crew:scout'), P('d:Service lane')),
}
wpaths = {}
for k, (a, b) in walks.items():
    pts = astar(a, b, free_walk); wpaths[k] = pts
    L = length(pts) / 100
    out[k] = {'m': round(L, 1), 'walk_s': round(L * 100 / WALK_CM_S, 1), 'rival_s': round(L * 100 / RIVAL_CM_S, 1)}

out['stands'] = {'getaway-service-drive': GS, 'rivals-in': RIV_IN, 'rivals-out-east': RIV_OUT_E, 'square-stop': SQUARE_STOP}
json.dump(out, open(sys.argv[3], 'w'), indent=1)
for k, v in out.items(): print(k, v)

# ---------------------------------------------------------------- the picture: block frame, x south (down), y east (right): north up, east right
S = 6
x_lo, x_hi, y_lo, y_hi = -1200, 9800, -6500, 1700
def px(p): return ((p[1] - y_lo) / C * S, (p[0] - x_lo) / C * S)
Wd, Hd = int((y_hi - y_lo) / C * S), int((x_hi - x_lo) / C * S)
LEG = 330
im = Image.new('RGB', (Wd, Hd + LEG), (250, 247, 240)); dr = ImageDraw.Draw(im)
for i in range(NX):
    for j in range(NY):
        x, y = cen(i, j)
        if not (x_lo <= x < x_hi and y_lo <= y < y_hi): continue
        r, w = D['rows'][i][j], W['rows'][i][j]
        col = (241, 235, 220) if w == '.' else (190, 184, 174)
        if r == 'r': col = (212, 216, 224)
        if r == 'y': col = (230, 211, 176)
        a, b = px((x - C / 2, y - C / 2)); dr.rectangle([a, b, a + S - 1, b + S - 1], fill=col)
A = '/System/Library/Fonts/Supplemental/Arial.ttf'; AB = '/System/Library/Fonts/Supplemental/Arial Bold.ttf'
F, FS, FB, FN = ImageFont.truetype(A, 21), ImageFont.truetype(A, 17), ImageFont.truetype(AB, 24), ImageFont.truetype(AB, 20)
def line(pts, col, w=5, dash=False):
    q = [px(p) for p in pts]
    if not dash: dr.line(q, fill=col, width=w, joint='curve'); return
    for k in range(0, len(q) - 1, 6): dr.line(q[k:k + 4], fill=col, width=w)
def dot(p, col, r=8, outline=(30, 30, 30)): a, b = px(p); dr.ellipse([a - r, b - r, a + r, b + r], fill=col, outline=outline, width=2)
def num(p, n, col, dx=0, dy=0):
    a, b = px(p); a += dx; b += dy; dr.ellipse([a - 15, b - 15, a + 15, b + 15], fill=col, outline=(255, 255, 255), width=3)
    t = str(n); w = dr.textlength(t, font=FN); dr.text((a - w / 2, b - 12), t, fill=(255, 255, 255), font=FN)
def text(p, t, col=(40, 40, 40), dx=0, dy=0, f=None): a, b = px(p); dr.text((a + dx, b + dy), t, fill=col, font=f or FS)
def car(p, col, nose_dir):
    hx, hy = (230, 90) if nose_dir[0] else (90, 230)
    a, b = px((p[0] - hx, p[1] - hy)); c2, d2 = px((p[0] + hx, p[1] + hy))
    dr.rectangle([min(a, c2), min(b, d2), max(a, c2), max(b, d2)], fill=col, outline=(20, 20, 20), width=2)
    n = px((p[0] + nose_dir[0] * (hx - 50), p[1] + nose_dir[1] * (hy - 50))); dr.ellipse([n[0] - 5, n[1] - 5, n[0] + 5, n[1] + 5], fill=(255, 255, 255))
for cam in site['cameras']:
    at = tuple(cam['at']); f = cam['facing']; L = math.hypot(*f); u = (f[0] / L, f[1] / L); n = (-u[1], u[0])
    tri = [at, (at[0] + u[0] * cam['reach'] + n[0] * cam['half'], at[1] + u[1] * cam['reach'] + n[1] * cam['half']), (at[0] + u[0] * cam['reach'] - n[0] * cam['half'], at[1] + u[1] * cam['reach'] - n[1] * cam['half'])]
    dr.polygon([px(p) for p in tri], outline=(205, 70, 55), width=2)
    text(at, cam['id'] + ' camera', (180, 50, 40), dx=-20, dy=-24)
OFF, GET, RIV, GREY = (222, 128, 30), (16, 140, 150), (118, 64, 188), (140, 140, 140)
line(wpaths['service-lane-to-getaway-bay'], GREY, 3, dash=True)
line(paths['official-leaves-east'], OFF, 3, dash=True)
line(paths['getaway-to-service-drive'], GET, 6)
line(paths['rivals-in-to-drop'], RIV, 7); line(paths['rivals-drop-to-park'], RIV, 7)
line(paths['rivals-tail-to-mouth'], RIV, 3, dash=True)
for k in ('drop-to-ambush-1', 'drop-to-ambush-2', 'drop-to-ambush-3', 'rival-park-to-lookout'): line(wpaths[k], RIV, 3, dash=True)
line(paths['official-to-service'], OFF, 8)
car(P('d:Official drop-off'), (250, 250, 250), (1, 0)); car(SB, OFF, (-1, 0)); car(GS, GET, (1, 0)); car(P('d:Getaway bay'), (160, 214, 218), (1, 0)); car(RIV_PARK, RIV, (0, 1))
for k in ('ambush-1', 'ambush-2', 'ambush-3', 'ambush-4', 'ambush-5'): dot(P(k), (200, 172, 236), 7, outline=RIV)
dot(LOOKOUT, (200, 172, 236), 8, outline=RIV)
for k in ('crew:scout', 'crew:fixer', 'crew:tech', 'crew:enforcer'): dot(P(k), (45, 45, 45), 6)
for k in ('d:Service lane', 'd:Official entrance', 'd:Office entrance'): dot(P(k), (255, 255, 255), 7)
text(P('d:Office entrance'), 'public door', dx=-105, dy=-10); text(P('d:Official entrance'), 'official door', dx=-118, dy=-26); text(P('d:Service lane'), 'service door', dx=-112, dy=-12)
text(P('d:Getaway bay'), 'your car at the start', GET, dx=20, dy=14); text(P('crew:tech'), 'the cell', (45, 45, 45), dx=14, dy=-22)
text((5200, -3300), 'Records Office', (90, 90, 90), f=F); text((300, -2600), 'the square', (90, 90, 90), f=F)
text((8550, -5900), 'Apron Road', (110, 110, 120), f=F); text((3650, -6000), 'Orbit Walk', (110, 110, 120), f=F); text((2000, -6450), 'Rocket Lane', (110, 110, 120), f=F, dx=0)
text((6000, 450), 'Pylon Row', (110, 110, 120), f=F)
num(P('d:Official drop-off'), 1, OFF, dx=-30, dy=0); num(SB, 2, OFF, dx=-30, dy=-30); num(P('d:Service lane'), 3, OFF, dx=-24, dy=26)
num(GS, 4, GET, dx=34, dy=10); num((8350, 1200), 5, OFF, dx=-26, dy=0)
num(RIV_IN, 6, RIV, dx=-22, dy=6); num(SQUARE_STOP, 7, RIV, dx=26, dy=-4); num(RIV_PARK, 8, RIV, dx=30, dy=0); num(MOUTH, 9, RIV, dx=34, dy=20)
num(P('ambush-1'), 10, RIV, dx=26, dy=18)
o = out
t_park = 372 + o['official-to-service']['s']; t_drop = 190 - o['rivals-drop-to-park']['s'] - 8; t_in = t_drop - o['rivals-in-to-drop']['s']
y0 = Hd + 12
dr.text((18, y0), "Brief 059: route B's hand-off, and the rival car's way in (north up)", fill=(30, 30, 30), font=FB)
rows = [
 (OFF, 1, "Vale's car waits at the official bay as always. Vale gets in at 6:10: an ordinary pickup for the camera and the door."),
 (OFF, 2, "With the driver turned, it drives round to the service bay: %.0f m, %.0f s; parked at %s." % (o['official-to-service']['m'], o['official-to-service']['s'], mmss(t_park))),
 (OFF, 3, "The Enforcer waits by the service door and takes Vale in the back seat (38 s, nothing to see)."),
 (GET, 4, "Your car, brought round once Vale's is in: %.0f m, %.0f s; parked nose out behind it, a %.1f m carry." % (o['getaway-to-service-drive']['m'], o['getaway-to-service-drive']['s'], o['official-rear-door-to-getaway-stand']['m'])),
 (OFF, 5, "No Take begun by 8:30: the driver gives up and drives Vale on, east (the mission fails)."),
 (RIV, 6, "The rival car turns in from Orbit Walk at %s." % mmss(t_in)),
 (RIV, 7, "It stops by the square at %s and drops three, who post round your car (%.0f-%.0f m walks)." % (mmss(t_in + o['rivals-in-to-drop']['s']), o['drop-to-ambush-3']['m'], o['drop-to-ambush-2']['m'])),
 (RIV, 8, "Round by Rocket Lane and Apron Road (%.0f s); parks across from Vale's car at 3:10; a lookout gets out." % o['rivals-drop-to-park']['s']),
 (RIV, 9, "Unless tipped away, the rival car follows Vale's car to the yard (%.0f s) and its crew reaches him first." % o['rivals-tail-to-mouth']['s']),
 (RIV, 10, "Posted rivals spring when Vale comes within 20 m of your car; if you move your car, they walk after it."),
]
for k, (col, n, t) in enumerate(rows):
    yy = y0 + 40 + k * 27; a = 32; dr.ellipse([a - 11, yy, a + 11, yy + 22], fill=col); w = dr.textlength(str(n), font=FS)
    dr.text((a - w / 2, yy + 2), str(n), fill=(255, 255, 255), font=FS); dr.text((52, yy), t, fill=(40, 40, 40), font=FS)
dr.text((18, y0 + 40 + 10 * 27 + 4), "grey dashed: today's carry on foot from the service door to the square, %.0f m. Measured on the engine's drive grid (road 5 m/s, drive 2 m/s) and walk grid (1.4 m/s; rivals 1.6 m/s)." % o['service-lane-to-getaway-bay']['m'], fill=(110, 110, 110), font=FS)
im.save(sys.argv[2]); print(im.size, 'rivals in', mmss(t_in), 'drop', mmss(t_in + o['rivals-in-to-drop']['s']), 'leave drop', mmss(t_drop + 8), 'park 3:10', 'vale car parks', mmss(t_park))
