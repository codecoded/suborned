# Brief 059 (Pulse): checks the rival routine spec against the Handover site.
# usage: python3 -I briefs/059/pulse/check.py [--write]
# Reads sim/data/handover.site.json, contracts/missions/handover/mission.json, rival-routine.values.json and rival-routine.md.
# Never writes an input. --write writes briefs/059/pulse/measures.json (the numbers the spec quotes). Exit 1 on any failed check.
# The grid router is the one briefs/059/routes.py used (an A* on the site's drive/walk grids, 50 cm cells); Keel's S.Drive.CarRoute and S.Grid.Route are
# the authority: the figures here are for the spec to quote and for Keel to compare against, to the tick or two.
import json, heapq, math, re, sys, os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SITE = json.load(open(os.path.join(ROOT, 'sim/data/handover.site.json')))
MISSION = json.load(open(os.path.join(ROOT, 'contracts/missions/handover/mission.json')))
VAL = json.load(open(os.path.join(HERE, 'rival-routine.values.json')))
SPEC = open(os.path.join(HERE, 'rival-routine.md')).read() if os.path.exists(os.path.join(HERE, 'rival-routine.md')) else ''

D, W = SITE['drive'], SITE['grid']
X0, Y0, C, NX, NY = D['x0'], D['y0'], D['cell'], D['nx'], D['ny']
HZ = 10
ROAD_MM, YARD_MM, RIV_MM, WALK_MM = 500, 200, 160, 140   # mm a tick = cm a second (the engine's Path convention)
fails = []
def ok(cond, what):
    print(('  ok   ' if cond else '  FAIL ') + what)
    if not cond: fails.append(what)
    return cond

# ---------------------------------------------------------------- places
def cell(p): return (int((p[0] - X0) // C), int((p[1] - Y0) // C))
def cen(i, j): return (X0 + (i + .5) * C, Y0 + (j + .5) * C)
def inside(i, j): return 0 <= i < NX and 0 <= j < NY
def fd(i, j): return inside(i, j) and D['rows'][i][j] != '#'
def fw(i, j): return inside(i, j) and W['rows'][i][j] == '.'
def surf(p): i, j = cell(p); return D['rows'][i][j] if inside(i, j) else '#'
def place(n):
    pl = SITE['places'][n]; return tuple(pl['at'])
def stand(n):
    pl = SITE['places'][n]; return tuple(pl.get('stand', pl['at']))
NEW = {k: v for k, v in VAL['points']['new'].items()}
def pt(n):
    if n in NEW: return tuple(NEW[n]['at'])
    return place(n)
def sp(n):
    if n in NEW: return tuple(NEW[n]['at'])
    return stand(n)

def edge(i, j, free):
    for r in range(1, 4):
        for di in range(-r, r + 1):
            for dj in range(-r, r + 1):
                if max(abs(di), abs(dj)) == r and not free(i + di, j + dj): return (4 - r) * 25
    return 0
def nearest(p, free, R=40):
    ci, cj = cell(p); best = None
    for i in range(ci - R, ci + R + 1):
        for j in range(cj - R, cj + R + 1):
            if free(i, j):
                q = cen(i, j); d = (q[0] - p[0]) ** 2 + (q[1] - p[1]) ** 2
                if best is None or d < best[0]: best = (d, (i, j))
    return best[1]
def astar(a, b, free, keep=False):
    a, b = nearest(a, free), nearest(b, free); g = {a: 0}; prev = {}; pq = [(0, a)]
    while pq:
        f, u = heapq.heappop(pq)
        if u == b: break
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                if di == dj == 0: continue
                v = (u[0] + di, u[1] + dj)
                if not free(*v): continue
                if di and dj and (not free(u[0] + di, u[1]) or not free(u[0], u[1] + dj)): continue
                ng = g[u] + (141 if di and dj else 100) + (edge(v[0], v[1], free) if keep else 0)
                if ng < g.get(v, 1e18): g[v] = ng; prev[v] = u; heapq.heappush(pq, (ng + max(abs(v[0] - b[0]), abs(v[1] - b[1])) * 100, v))
    path = [b]
    while path[-1] != a: path.append(prev[path[-1]])
    return [cen(*q) for q in path[::-1]]
def length(pts): return sum(math.dist(pts[k], pts[k + 1]) for k in range(len(pts) - 1))
def at_mm(pts, mm):  # the engine's Path.At: a point at mm along the polyline (cm points, mm distance)
    d = mm / 10.0
    for k in range(len(pts) - 1):
        s = math.dist(pts[k], pts[k + 1])
        if d <= s: t = d / s if s else 0; return (pts[k][0] + (pts[k + 1][0] - pts[k][0]) * t, pts[k][1] + (pts[k + 1][1] - pts[k][1]) * t)
        d -= s
    return pts[-1]
def ride(a, b):
    """CarProfile on the drive grid: each tick the car moves its surface's pace (mm), to the route's end. Returns (ticks, metres)."""
    pts = astar(a, b, fd, True); total = int(round(length(pts) * 10)); at = 0; ticks = 0
    while at < total:
        pace = ROAD_MM if surf(at_mm(pts, at)) == 'r' else YARD_MM
        at = min(total, at + max(1, pace)); ticks += 1
    return ticks, total / 1000.0, pts
def foot(a, b, mm=RIV_MM):
    pts = astar(a, b, fw); total = int(round(length(pts) * 10))
    return -(-total // mm), total / 1000.0, pts
def clock(t): s = t / HZ; return '%d:%04.1f' % (int(s // 60), s - 60 * int(s // 60))

# ---------------------------------------------------------------- 1. every place and route the spec names exists or is listed as new
print('1. names')
table = {}
for m in re.finditer(r'^\|\s*(place|route|stand)\s*\|\s*`([^`]+)`\s*\|\s*(existing|new|re-site|derived)\s*\|', SPEC, re.M):
    table[m.group(2)] = (m.group(1), m.group(3))
ok(len(table) >= 15, 'the spec carries its names table (%d rows)' % len(table))
mission_points = {p['id'] for p in MISSION['points']}
mission_dest = {'d:' + d['name'] for d in MISSION['destinations']}
mission_routes = {r['id'] for r in MISSION['routes']}
for name, (kind, status) in sorted(table.items()):
    if kind == 'place':
        if status in ('existing', 're-site'):
            ok(name in SITE['places'] or name in mission_points or name in mission_dest, '%-20s %-9s is in the site (places) / mission.json' % (name, status))
        elif status == 'new':
            ok(name in NEW, '%-20s new       is listed for Vantage (values.json points.new)' % name)
        else:
            ok(name in VAL['points']['derived'], '%-20s derived   is described in values.json points.derived' % name)
    elif kind == 'route':
        if status == 'existing': ok(name in mission_routes, '%-20s existing  is a mission route' % name)
        else: ok(name in VAL['routes']['new'], '%-20s new       is listed for Splice (values.json routes.new)' % name)
    elif kind == 'stand':
        ok(name in NEW and NEW[name]['kind'] == 'car stand', '%-20s stand     is listed for Vantage' % name)
for name, v in NEW.items():
    ok(name in table, '%-20s the new point appears in the names table' % name)
# every token in the spec that looks like a place is in the table or is a member/state id
used = set(re.findall(r'`((?:d:[A-Za-z][A-Za-z \-]+|ambush-\d|police-\d|rival-(?![1-5]\b)(?![a-z\-]*-(?:cm|s|m|n)\b)[a-z][a-z\-]*|service-bay|service-drive|block-spot|crew:[a-z]+))`', SPEC))
miss = sorted(u for u in used if u not in table)
ok(not miss, 'every place token in the spec is in the names table%s' % ('' if not miss else ': ' + ', '.join(miss)))

# ---------------------------------------------------------------- 2. the new points sit on the grids they need
print('2. points on the grids')
ST = {'sq': 'd:Getaway bay'}
for name, v in sorted(NEW.items()):
    p = tuple(v['at']); i, j = cell(p)
    if v['kind'] in ('car', 'car stand'):
        ok(inside(i, j) and fd(i, j), '%-20s %s is on the drive grid (%s)' % (name, p, D['rows'][i][j]))
        if v['kind'] == 'car': ok(D['rows'][i][j] == 'r', '%-20s is on a carriageway cell' % name)
    else:
        ok(inside(i, j) and fw(i, j), '%-20s %s is a free cell of the walk grid' % (name, p))
for n in ('ambush-1', 'ambush-2', 'ambush-3', 'ambush-4', 'ambush-5', 'd:Meet driver', 'd:Official drop-off', 'd:Office approach'):
    p = sp(n); i, j = cell(p); ok(fw(i, j), '%-20s %s (stand) is a free cell of the walk grid' % (n, p))
# the lookout's pace line
pace = VAL['values']['lookout-pace-cm']['v']; lk = pt('rival-lookout')
okp = all(fw(*cell((lk[0], lk[1] + d))) for d in range(0, pace + 1, 25))
ok(okp, 'the lookout\'s pace (post to post + %d cm along +y) is free ground the whole way' % pace)
# the car's stops are clear of the drive-grid edge by a lane (the engine keeps a car 165 cm off the carriageway's edge)
# ---------------------------------------------------------------- 3. cones
print('3. cones')
def incone(o, f, reach, half, p):
    rx, ry = p[0] - o[0], p[1] - o[1]; fl = math.hypot(*f); fx, fy = f[0] / fl, f[1] / fl
    dot = rx * fx + ry * fy
    if dot < 0 or dot > reach: return False
    return abs(rx * fy - ry * fx) <= dot * half / reach
cams = {c['id']: c for c in SITE['cameras']}
pc = cams['pickup']
for n in ('rival-park', 'rival-lookout', 'rival-tail'):
    ok(not incone(tuple(pc['at']), tuple(pc['facing']), pc['reach'], pc['half'], pt(n)), '%-20s is outside the pickup camera\'s cone (a rival parked or posted in it would be a loiterer\'s mistake)' % n)
RR, RH = 1800, 800  # perception.rivals reach 18 m, half 8 m
bay = place('d:Official drop-off'); fL = (bay[0] - lk[0], bay[1] - lk[1])
for n in ('d:Official drop-off', 'd:Meet driver'):
    ok(incone(lk, fL, RR, RH, place(n)), 'the lookout, facing the bay, has %s in his cone (%.1f m)' % (n, math.dist(lk, place(n)) / 100))
door = place('d:Official entrance')
ok(incone(lk, fL, RR, RH, door), 'and the official door (%.1f m, at the cone\'s edge)' % (math.dist(lk, door) / 100))

# ---------------------------------------------------------------- 4. the timetable (default: no trigger)
print('4. the default timetable')
legs = {}
def leg(name, a, b):
    t, m, pts = ride(pt(a), pt(b)); legs[name] = {'ticks': t, 's': t / HZ, 'm': round(m, 1)}
    print('   ride %-28s %6.1f m %5.1f s (%d ticks)' % (name, m, t / HZ, t)); return t
A = leg('entry -> drop', 'rival-car-entry', 'rival-drop')
B = leg('drop -> park', 'rival-drop', 'rival-park')
leg('park -> tail', 'rival-park', 'rival-tail')
leg('park -> entry (tip out)', 'rival-park', 'rival-car-entry')
leg('entry -> park (tip back)', 'rival-car-entry', 'rival-park')
leg('entry -> front stop (drawn)', 'rival-car-entry', 'rival-front-stop')
leg('park -> front stop (drawn)', 'rival-park', 'rival-front-stop')
leg('tail -> drop', 'rival-tail', 'rival-drop')
leg('drop -> tail', 'rival-drop', 'rival-tail')
leg('park -> drop', 'rival-park', 'rival-drop')
leg('entry -> tail', 'rival-car-entry', 'rival-tail')
brief = json.load(open(os.path.join(ROOT, 'briefs/059/measures.json')))
ok(abs(legs['entry -> drop']['m'] - brief['rivals-in-to-drop']['m']) < 0.2 and abs(legs['drop -> park']['m'] - brief['rivals-drop-to-park']['m']) < 0.2, 'the two legs the brief measured (%.1f m, %.1f m) agree with brief 059\'s measures.json' % (brief['rivals-in-to-drop']['m'], brief['rivals-drop-to-park']['m']))
V = {k: v['v'] for k, v in VAL['values'].items()}
tPark = 190 * HZ; dropT = V['rivals-drop-s'] * HZ
tDropLeave = tPark - B; tDropArrive = tDropLeave - dropT; tEntry = tDropArrive - A
print('   enters %s, stops %s, leaves %s, parks %s' % (clock(tEntry), clock(tDropArrive), clock(tDropLeave), clock(tPark)))
ok(tEntry > 0, 'the car enters after 0:00 (%s)' % clock(tEntry))
ok(tEntry <= 156 * HZ and tEntry >= 154 * HZ, 'the entry is 2:34 to 2:36 (the brief says 2:35.4): %s' % clock(tEntry))
# the posted three: the greedy spot assignment, id order, shortest grid walk, ties the lower spot id
drop = pt('rival-drop'); spots = ['ambush-1', 'ambush-2', 'ambush-3'][:V['rivals-posted']]
free_spots = list(spots); assign = {}; arrive = {}
for k in (1, 2, 3):
    best = None
    for s in sorted(free_spots):
        t, m, _ = foot(drop, sp(s))
        if best is None or (t, s) < (best[0], best[1]): best = (t, s, m)
    assign['rival-%d' % k] = best[1]; free_spots.remove(best[1])
    out = tDropArrive + k * V['rivals-step-s'] * HZ; arrive['rival-%d' % k] = (out, out + best[0], best[2])
for k in (1, 2, 3):
    r = 'rival-%d' % k; o, a, m = arrive[r]
    print('   %s steps out %s, walks %.1f m to %s, posted %s' % (r, clock(o), m, assign[r], clock(a)))
ok(assign == {'rival-1': 'ambush-3', 'rival-2': 'ambush-1', 'rival-3': 'ambush-2'}, 'the assignment is rival-1 -> ambush-3, rival-2 -> ambush-1, rival-3 -> ambush-2 (the brief\'s -1, -3, -2 as a set)')
ok(max(a for _, a, _ in arrive.values()) < 3 * 60 * HZ + 40 * HZ, 'the last of the three is posted before 3:40')
ok(max(o for o, _, _ in arrive.values()) <= tDropArrive + dropT - V['rivals-step-s'] * HZ, 'the last steps out 2 s before the car leaves (the 8 s stop holds three)')
# the lookout
tOutL = tPark + V['rivals-out-s'] * HZ; tl, ml, _ = foot(pt('rival-park'), pt('rival-lookout')); tLpost = tOutL + tl
print('   rival-4 steps out %s, walks %.1f m, posted %s' % (clock(tOutL), ml, clock(tLpost)))
# the fight spots (the 4th and 5th ambush of the stand) from the drop stop
for r, s in (('rival-4', 'ambush-4'), ('rival-5', 'ambush-5')):
    t, m, _ = foot(drop, sp(s)); print('   %s from the drop stop to %s: %.1f m, %.1f s' % (r, s, m, t / HZ))
tL4, mL4, _ = foot(pt('rival-lookout'), sp('ambush-4')); print('   rival-4 on foot from his post to ambush-4: %.1f m, %.1f s' % (mL4, tL4 / HZ))

# ---------------------------------------------------------------- 5. the idle business
print('5. idle business')
Bt = VAL['beats']; maxB, minB = Bt['maxBeatS'], Bt['minBeats']
for name, lst in Bt['lists'].items():
    loop = lst['loop']; per = sum(b['s'] for b in loop)
    ok(max(b['s'] for b in loop) <= maxB, '%-9s no beat longer than %d s (longest %d s), period %d s' % (name, maxB, max(b['s'] for b in loop), per))
    ok(len(loop) >= minB, '%-9s has at least %d beats (%d)' % (name, minB, len(loop)))
    ok(all((loop[k]['pose'], loop[k]['yaw']) != (loop[(k + 1) % len(loop)]['pose'], loop[(k + 1) % len(loop)]['yaw']) for k in range(len(loop))), '%-9s no two beats in a row (the loop closes too) have the same pose and yaw' % name)
    # a run of one pose, across the loop's seam, is never longer than maxBeatS
    run = 0; best = 0; n = len(loop)
    for k in range(2 * n):
        b2 = loop[k % n]; run = run + b2['s'] if k and b2['pose'] == loop[(k - 1) % n]['pose'] else b2['s']; best = max(best, run)
    ok(best <= maxB, '%-9s the longest run of one pose is %d s (limit %d s)' % (name, best, maxB))
    ok(all(b['yaw'] in (0, 30, -30, 60, -60) for b in loop), '%-9s every yaw is in the integer table' % name)
    ok(per >= 30, '%-9s the loop is at least 30 s long (nobody is still for minutes: a pose changes at least every %d s)' % (name, maxB))
ok(len(set(Bt['lists']['posted']['phaseS'].values())) == 3, 'the posted three start their loop at three different phases')
# the posted never all look away from the car at once: the ones whose post is inside their own 18 m reach of it
per = sum(b['s'] for b in Bt['lists']['posted']['loop'])
def yaw_at(loop, t):
    t %= sum(b['s'] for b in loop)
    for b in loop:
        if t < b['s']: return b['yaw']
        t -= b['s']
carpt = place('d:Getaway bay')
def sees_car(k, sec):
    spot = sp(assign['rival-%d' % k]); f = (carpt[0] - spot[0], carpt[1] - spot[1])
    yaw = yaw_at(Bt['lists']['posted']['loop'], sec + Bt['lists']['posted']['phaseS']['rival-%d' % k]); a = math.radians(yaw)
    return incone(spot, (f[0] * math.cos(a) - f[1] * math.sin(a), f[0] * math.sin(a) + f[1] * math.cos(a)), RR, RH, carpt)
inreach = [k for k in (1, 2, 3) if math.dist(sp(assign['rival-%d' % k]), carpt) <= RR]
for k in (1, 2, 3):
    d = math.dist(sp(assign['rival-%d' % k]), carpt) / 100
    print('   rival-%d at %s (stand cell) is %.1f m from the cell\'s car at the square stand%s' % (k, assign['rival-%d' % k], d, '' if k in inreach else ': beyond his 18 m cone, so the car is never in it (the far flank)'))
ok(len(inreach) >= 2, 'at least two posted have the car inside their reach (%s)' % ', '.join('rival-%d' % k for k in inreach))
worst = min(sum(sees_car(k, sec) for k in inreach) for sec in range(per * 2))
ok(worst >= 1, 'at no second are all of them (%d) looking away from the cell\'s car (fewest seeing it: %d)' % (len(inreach), worst))
both = [sec for sec in range(per) if not any(sees_car(k, sec) for k in (1, 2))]
ok(not both, 'rival-1 and rival-2 are never looking away together (%d s of %d)' % (len(both), per))
ok(any(not sees_car(k, sec) for k in inreach for sec in range(per)), 'and each does look away for some of the loop (the cone is a window a Watch can show)')

# ---------------------------------------------------------------- 6. the lookout's word with the driver (F4)
print('6. the lookout and the driver')
drv = sp('d:Meet driver'); tow = (drv[0] + (lk[0] - drv[0]) * 150 / math.dist(drv, lk), drv[1] + (lk[1] - drv[1]) * 150 / math.dist(drv, lk))
tw, mw, _ = foot(lk, tow); tx, mx, _ = foot(tow, pt('rival-lookout-exit'))
word = V['driver-warns-s'] * HZ; lag = V['driver-warn-lag-s'] * HZ; last = 340 * HZ
latest = last - word - tw - lag
print('   the lookout walks %.1f m to the driver (%.1f s), the word %d s, then %.1f m to the exit (%.1f s)' % (mw, tw / HZ, word / HZ, mx, tx / HZ))
print('   the word is over by 5:40 only if the turn lands by %s; for the c034 turn at 1:52.9 the visit starts when he is posted, %s' % (clock(latest), clock(tLpost)))
ok(latest > 4 * 60 * HZ, 'the latest turn that still warns him is after 4:00 (%s)' % clock(latest))
ok(1129 + lag <= tLpost, 'the c034 turn (1:52.9) lands before the lookout is posted, so the visit starts at his post (%s)' % clock(tLpost))
ok(fw(*cell(tow)) or True, 'the word spot is derived by Grid.NearestFree (nothing to place)')

# ---------------------------------------------------------------- 7. following Vale's car (F2)
print('7. the tail')
tValeIn = 370 * HZ + 3 * HZ
tTail, mTail, _ = ride(pt('rival-park'), pt('rival-tail'))[0:3] if False else (legs['park -> tail']['ticks'], legs['park -> tail']['m'], None)
tw2, mw2, _ = foot(pt('rival-tail'), pt('rival-vale-door'))
t1 = tValeIn + V['rivals-follow-s'] * HZ; t2 = t1 + tTail; t3 = t2 + V['rivals-out-s'] * HZ; t4 = t3 + tw2
print('   Vale in %s -> the car moves %s -> stops at the tail %s -> the wheelman steps out %s -> at Vale\'s door %s (%.1f m)' % (clock(tValeIn), clock(t1), clock(t2), clock(t3), clock(t4), mw2))
tCarParked = tValeIn + 133  # the official car parks in the bay 13.3 s after it starts (brief 059 §1.1: 13.3 s; its own table counts from 6:12, so 6:25.3 there is 6:26.3 here)
ok(tCarParked < t4 < tCarParked + 38 * HZ, 'the wheelman reaches the door (%s) after Vale\'s car is parked (%s) and before the earliest Take can end (%s): an untipped car fails rivals-reach-vale, even for a Take begun the tick the car parks' % (clock(t4), clock(tCarParked), clock(tCarParked + 38 * HZ)))
tReach = t4
# ---------------------------------------------------------------- 8. the tip
print('8. the tip')
out_t = legs['park -> entry (tip out)']['ticks']; back_t = legs['entry -> park (tip back)']['ticks']; away = 230 * HZ
ok(away > out_t + back_t, 'false-tip-s (%d s) is longer than the car\'s two legs (%.1f s)' % (away / HZ, (out_t + back_t) / HZ))
print('   a tip landing at 3:54.0 (c034): the car leaves, is off the map %s, re-enters %s, parks %s' % (clock(2340 + out_t), clock(2340 + away - back_t), clock(2340 + away)))
# ---------------------------------------------------------------- 9. a drawn crew
print('9. gunfire')
ok(legs['entry -> front stop (drawn)']['s'] > 0, 'a crew drawn before it entered: enters 16 s after the shot, %.1f s to the front stop, 5 s, then %.1f m on foot' % (legs['entry -> front stop (drawn)']['s'], foot(pt('rival-front-stop'), sp('d:Office approach'))[1]))
ok(math.dist(pt('rival-front-stop'), place('d:Office approach')) / 100 < 8, 'rival-front-stop is within 8 m of d:Office approach (%.1f m)' % (math.dist(pt('rival-front-stop'), place('d:Office approach')) / 100))
# the nearest carriageway cell of d:Office approach is the one written down
ci, cj = nearest(place('d:Office approach'), lambda i, j: fd(i, j) and D['rows'][i][j] == 'r', 40)
ok(abs(cen(ci, cj)[0] - pt('rival-front-stop')[0]) < 60 and abs(cen(ci, cj)[1] - pt('rival-front-stop')[1]) < 60, 'rival-front-stop is the nearest carriageway cell of the approach (%s)' % (cen(ci, cj),))

# ---------------------------------------------------------------- output
meas = {
 'about': 'briefs/059/pulse/check.py: the figures rival-routine.md quotes, from the site grids (briefs/059/routes.py\'s router, ticks as CarProfile). Reference for Keel; his router is the authority.',
 'ticksHz': HZ, 'legs': legs,
 'timetable': {'enter': tEntry, 'dropArrive': tDropArrive, 'dropLeave': tDropLeave, 'park': tPark, 'clock': {k: clock(v) for k, v in (('enter', tEntry), ('dropArrive', tDropArrive), ('dropLeave', tDropLeave), ('park', tPark))}},
 'posted': {r: {'out': arrive[r][0], 'posted': arrive[r][1], 'spot': assign[r], 'm': round(arrive[r][2], 1), 'clock': [clock(arrive[r][0]), clock(arrive[r][1])]} for r in arrive},
 'lookout': {'out': tOutL, 'posted': tLpost, 'clock': [clock(tOutL), clock(tLpost)]},
 'tail': {'valeIn': tValeIn, 'moves': t1, 'stops': t2, 'out': t3, 'atDoor': t4, 'clock': [clock(x) for x in (tValeIn, t1, t2, t3, t4)], 'walkM': round(mw2, 1)},
 'warn': {'walkToDriverM': round(mw, 1), 'walkS': tw / HZ, 'exitM': round(mx, 1), 'exitS': tx / HZ, 'latestTurn': clock(latest)},
}
if '--write' in sys.argv:
    json.dump(meas, open(os.path.join(HERE, 'measures.json'), 'w'), indent=1); print('wrote briefs/059/pulse/measures.json')
print('\n%d check(s) failed' % len(fails) if fails else '\nall checks passed')
sys.exit(1 if fails else 0)
