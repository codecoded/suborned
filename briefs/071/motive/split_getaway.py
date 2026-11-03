# Brief 071 (Motive): the split getaway on the Handover site: measures and the picture. Helpers from briefs/059/routes.py (Waymark).
# Reads sim/data/handover.site.json (drive grid 'r'/'y'/'#', walk grid '.'/'#', places), never writes inputs.
# usage: python3 -I routes.py <site.json> <out.png> <out.json>
import json, heapq, math, sys
from PIL import Image, ImageDraw, ImageFont

site = json.load(open(sys.argv[1]))
D, W = site['drive'], site['grid']
X0, Y0, C, NX, NY = D['x0'], D['y0'], D['cell'], D['nx'], D['ny']
ROAD_CM_S, YARD_CM_S, WALK_CM_S, RIVAL_CM_S, RUN_CM_S = 500, 200, 180, 160, 280  # the cell walks 1.8 m/s (op-speed-cm-s, brief 067); posted rivals run 2.8 (S3)

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


# ---------------------------------------------------------------- measures (site frame, cm)
out = {}
SB, GB, GS = P('service-bay'), P('d:Getaway bay'), P('service-drive')
OUT_E = (8350, 1575)                         # Apron Road at the window's east edge: Vale's car's way off the map
SQ_LANE = site['getaway']['stands'][0]['exit']['polyline']; YD_LANE = site['getaway']['stands'][1]['exit']['polyline']
legs = {'vale-car-official-to-bay': (P('d:Official drop-off'), SB), 'vale-car-bay-out-east': (SB, OUT_E), 'getaway-square-to-service-drive': (GB, GS)}
paths = {}
for k, (a, b) in legs.items():
    pts = astar(a, b, free_drive, keep=True); paths[k] = pts; road, yard, t = drive_time(pts)
    out[k] = {'m': round(length(pts) / 100, 1), 'road_m': round(road, 1), 'yard_m': round(yard, 1), 's': round(t, 1)}
walks = {'service-lane-to-getaway-bay': (P('d:Service lane'), GB), 'cabinet-to-getaway-bay': (P('cabinet'), GB),
         'meet-driver-to-getaway-bay': (P('d:Meet driver'), GB), 'ambush-1-to-service-bay': (P('ambush-1'), (SB[0] + 80, SB[1] - 190))}
wpaths = {}
for k, (a, b) in walks.items():
    pts = astar(a, b, free_walk); wpaths[k] = pts; L = length(pts) / 100
    out[k] = {'m': round(L, 1), 'cell_walk_s': round(L * 100 / WALK_CM_S, 1), 'rival_run_s': round(L * 100 / RUN_CM_S, 1)}
# the exit lanes: today's letter (§06: the first 30 m of the route's carriageway, the car projected onto it) and B10 (from the car, along the drive)
proj_bay = (YD_LANE[0][0], SB[1]); proj_drv = (YD_LANE[0][0], GS[1])
out['lane-start-from-car-m'] = {'square (today)': round(math.dist(GB, (GB[0], SQ_LANE[0][1])) / 100, 2),
    'yard stand service-drive (today)': round(math.dist(GS, proj_drv) / 100, 2), 'service bay, Vale car (today\'s letter)': round(math.dist(SB, proj_bay) / 100, 2),
    'service bay, Vale car (B10: from the car)': 0.0}
out['bay-lane-B10'] = {'drive_m': round(math.dist(SB, proj_bay) / 100, 2), 'road_m': round(30 - math.dist(SB, proj_bay) / 100, 2), 'post_10m_at': [SB[0] + 1000, SB[1]]}
json.dump(out, open(sys.argv[3], 'w'), indent=1)
for k, v in out.items(): print(k, v)

# ---------------------------------------------------------------- the picture: block frame, x south (down), y east (right): north up, east right
S = 6
x_lo, x_hi, y_lo, y_hi = -1000, 9800, -6400, 1700
def px(p): return ((p[1] - y_lo) / C * S, (p[0] - x_lo) / C * S)
Wd, Hd = int((y_hi - y_lo) / C * S), int((x_hi - x_lo) / C * S)
LEG = 520
im = Image.new('RGB', (Wd, Hd + LEG), (250, 247, 240)); dr = ImageDraw.Draw(im, 'RGBA')
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
def band(p0, p1, w_m, fill):  # an exit lane: a polyline segment at its width
    ux, uy = p1[0] - p0[0], p1[1] - p0[1]; L = math.hypot(ux, uy); n = (-uy / L * w_m * 50, ux / L * w_m * 50)
    dr.polygon([px((p0[0] + n[0], p0[1] + n[1])), px((p1[0] + n[0], p1[1] + n[1])), px((p1[0] - n[0], p1[1] - n[1])), px((p0[0] - n[0], p0[1] - n[1]))], fill=fill)
OFF, GET, RIV, GREY = (222, 128, 30), (16, 140, 150), (118, 64, 188), (120, 120, 120)
# exit lanes: the square's (teal), the bay's under B10 (orange: the drive at 3 m, then Apron Road at 12 m), the bay's under today's letter (hatched outline)
band(tuple(SQ_LANE[0]), tuple(SQ_LANE[1]), 12, (16, 140, 150, 55))
d_end = (YD_LANE[0][0], SB[1]); band(SB, d_end, 3, (222, 128, 30, 80)); band(d_end, (d_end[0], d_end[1] + 30 * 100 - math.dist(SB, d_end)), 12, (222, 128, 30, 60))
a, b = px((YD_LANE[0][0] - 600, YD_LANE[0][1])); c2, d2 = px((YD_LANE[1][0] + 600, YD_LANE[1][1])); dr.rectangle([a, b, c2, d2], outline=(150, 90, 30), width=2)
for cam in site['cameras']:
    at = tuple(cam['at']); f = cam['facing']; L = math.hypot(*f); u = (f[0] / L, f[1] / L); n = (-u[1], u[0])
    tri = [at, (at[0] + u[0] * cam['reach'] + n[0] * cam['half'], at[1] + u[1] * cam['reach'] + n[1] * cam['half']), (at[0] + u[0] * cam['reach'] - n[0] * cam['half'], at[1] + u[1] * cam['reach'] - n[1] * cam['half'])]
    dr.polygon([px(p) for p in tri], outline=(205, 70, 55), width=2)
    text(at, cam['id'] + ' camera', (180, 50, 40), dx=-20, dy=-24)
line(wpaths['cabinet-to-getaway-bay'], GREY, 3, dash=True); line(wpaths['meet-driver-to-getaway-bay'], GREY, 3, dash=True)
line(paths['vale-car-official-to-bay'], OFF, 6)
line(paths['vale-car-bay-out-east'], OFF, 9)
line(SQ_LANE, GET, 6)
car(P('d:Official drop-off'), (250, 250, 250), (1, 0)); car(SB, OFF, (-1, 0)); car(GB, GET, (1, 0))
for k in ('ambush-1', 'ambush-2', 'ambush-3'): dot(P(k), (200, 172, 236), 8, outline=RIV)
dot(P('cabinet'), (255, 255, 255), 6); text(P('cabinet'), 'cabinet', dx=-30, dy=10)
for k in ('d:Service lane', 'd:Official entrance', 'd:Office entrance'): dot(P(k), (255, 255, 255), 7)
text(P('d:Office entrance'), 'public door', dx=-105, dy=-10); text(P('d:Official entrance'), 'official door', dx=-118, dy=-26); text(P('d:Service lane'), 'service door', dx=-112, dy=-12)
text(GB, "the cell's car", GET, dx=22, dy=-6); text(SB, "Vale's car", OFF, dx=14, dy=-40)
text((5200, -3300), 'Records Office', (90, 90, 90), f=F); text((300, -2600), 'the square', (90, 90, 90), f=F)
text((8550, -5900), 'Apron Road', (110, 110, 120), f=F); text((3650, -6000), 'Orbit Walk', (110, 110, 120), f=F); text((1500, -6350), 'Rocket Lane', (110, 110, 120), f=F)
text((YD_LANE[1][0] + 650, YD_LANE[1][1] - 1500), "today's lane", (150, 90, 30), dx=0, dy=0)
num(P('d:Official drop-off'), 1, OFF, dx=-30); num(SB, 2, OFF, dx=-34, dy=-28); num(P('d:Service lane'), 3, OFF, dx=-24, dy=26)
num(OUT_E, 4, OFF, dx=-26, dy=-30); num(P('ambush-1'), 5, RIV, dx=28, dy=16); num(P('cabinet'), 6, GREY, dx=22, dy=-24)
num(GB, 7, GET, dx=-28, dy=30); num((SQ_LANE[1][0], SQ_LANE[1][1]), 8, GET, dx=30, dy=-70); num((SB[0] + 1000, SB[1]), 9, OFF, dx=24, dy=20)
o = out; y0 = Hd + 12
dr.text((18, y0), "Brief 071: the split getaway on the pickup way (north up)", fill=(30, 30, 30), font=FB)
rows = [
 (OFF, 1, "Vale gets into his car at the official bay (6:10, in by 6:13): an ordinary pickup for the camera and the door."),
 (OFF, 2, "The turned driver brings him round to the service bay: %.0f m, %.0f s, parked at 6:25.9 (engine)." % (o['vale-car-official-to-bay']['m'], o['vale-car-official-to-bay']['s'])),
 (OFF, 3, "The Enforcer takes Vale in the back seat (38 s, nothing to see): done 7:03.9 (engine). The driver steps out; the Enforcer takes the wheel."),
 (OFF, 4, "Go on Vale's car: down the drive and east along Apron Road, %.0f m, %.0f s, off the map. Vale is out: the win is fixed." % (o['vale-car-bay-out-east']['m'], o['vale-car-bay-out-east']['s'])),
 (RIV, 5, "The three rivals posted round the cell's car since 2:59 spring the moment Vale's car pulls away."),
 (GREY, 6, "Meanwhile the Scout, Fixer and Tech have walked back to their car (cabinet %.0f m, %.0f s; from the driver %.0f m, %.0f s)." % (o['cabinet-to-getaway-bay']['m'], o['cabinet-to-getaway-bay']['cell_walk_s'], o['meet-driver-to-getaway-bay']['m'], o['meet-driver-to-getaway-bay']['cell_walk_s'])),
 (GET, 7, "Three pistols, no Enforcer: down the blocker (focus fire 2.1 s), board, Go 4.2 s after the spring; or break the crew (5.8 s)."),
 (GET, 8, "The square's exit lane (shaded teal), unchanged; its blocker posts at the planter 11 m out."),
 (OFF, 9, "Vale's car's lane, from the car (shaded orange, the B10 clause): its post is 10 m down the drive. Today's letter starts it on Apron Road, %.1f m out (outlined)." % o['lane-start-from-car-m']["service bay, Vale car (today's letter)"]),
]
def wrap(t, f, wmax):
    words, lines, cur = t.split(), [], ''
    for w in words:
        if dr.textlength((cur + ' ' + w).strip(), font=f) > wmax: lines.append(cur); cur = w
        else: cur = (cur + ' ' + w).strip()
    return lines + [cur]
yy = y0 + 40
for col, n, t in rows:
    a = 32; dr.ellipse([a - 11, yy, a + 11, yy + 22], fill=col); w = dr.textlength(str(n), font=FS)
    dr.text((a - w / 2, yy + 2), str(n), fill=(255, 255, 255), font=FS)
    for ln in wrap(t, FS, Wd - 70): dr.text((52, yy), ln, fill=(40, 40, 40), font=FS); yy += 22
    yy += 6
for ln in wrap("Drawn from sim/data/handover.site.json (the civic block and mission.json). Drive grid: road 5 m/s, drive 2 m/s; walks at the cell's 1.8 m/s. Grey dashed: the decoy team's walks back to its car. The rival car is away on the false tip.", FS, Wd - 36):
    dr.text((18, yy + 4), ln, fill=(110, 110, 110), font=FS); yy += 22
im = im.crop((0, 0, Wd, yy + 16))
im.save(sys.argv[2]); print(im.size)
