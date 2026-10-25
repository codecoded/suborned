# Motive 059 S1 probe (2026-10-25): the getaway car at the service-drive stand on engine 0.31.0 with c037's inputs, to measure
# (a) the leak: a fast cell leaves the yard before the rival crew reaches it, and (b) the fight a thinned crew gives.
# Engine 0.31.0 has no rival bodies: its safe fight places fight-safe-hostiles at the site's fight.ambush spots and shows them at the
# spring (Vale with the Enforcer within 20 m of the car). The probe moves those spots to stand in for where 059's crew is:
#   leak     the square's five spots: the crew is not at the yard (059: the car has not even entered before 2:35.3)
#   arrive   five spots at rival-tail, where 059 F6 has all five step out of the rival car (all at once: harder than 2 s apart)
#   posted   svc-ambush-1..5: the crew posted round the yard stand (they ran after the car)
#   posted3  3 at svc-ambush-1..3: the tip and the warned lookout (route B's three)
#   thin1    3: svc-ambush-2, -3 and one at rival-tail: P1 taken down, the wheelman recalled by the floor (present at the spring: harder than 20 s late)
#   thin3    2 at rival-tail: all three posted taken down, the wheelman and lookout recalled
#   thin0    0: all three posted taken down, no floor (the leak in (b))
#   yard5    5: the posted three at svc-ambush-1..3, the lookout and wheelman stepping out at rival-tail (059 F6, car Parked at the fight's start)
# Writes out/scratch/motive/059-s1/root-<v>/ (copies of the engine inputs, only the site and the rulebook's fight-safe-hostiles changed) and
# out/scratch/motive/059-s1/scripts/*.json. Never touches the repo's inputs. Run from the repo root: python3 -I briefs/059/motive/s1/mkprobe.py
import json, os, shutil, copy
OUT = 'out/scratch/motive/059-s1'
site0 = json.load(open('sim/data/handover.site.json'))
stands = json.load(open('sim/data/handover.stands.json'))
mission = json.load(open('contracts/missions/handover/mission.json'))
rb0 = json.load(open('contracts/rules/rulebook.json'))
W = site0['grid']; X0, Y0, C, NX, NY = W['x0'], W['y0'], W['cell'], W['nx'], W['ny']
def free(i, j): return 0 <= i < NX and 0 <= j < NY and W['rows'][i][j] == '.'
def snap(p):
    ci, cj = int((p[0]-X0)//C), int((p[1]-Y0)//C); best = None
    for i in range(ci-8, ci+9):
        for j in range(cj-8, cj+9):
            if free(i, j):
                q = (int(X0+(i+.5)*C), int(Y0+(j+.5)*C)); d = (q[0]-p[0])**2 + (q[1]-p[1])**2
                if best is None or d < best[0]: best = (d, q)
    return list(best[1])
pts = {p['id']: p for p in mission['points']}
def cm(pid): a = pts[pid]['at']; return snap((round(a[0]*100), round(a[1]*100)))
svc = next(s for s in stands['getaway']['stands'] if s['id'] == 'service-drive')
YARD = cm('service-drive')
TAIL = [snap(p) for p in [(9000, -1750), (9100, -1750), (9000, -1650), (9100, -1650), (9050, -1850)]]
SPOTS = {
    'leak': (5, None),
    'arrive': (5, TAIL),
    'posted': (5, [cm(f'svc-ambush-{k}') for k in range(1, 6)]),
    'posted3': (3, [cm(f'svc-ambush-{k}') for k in range(1, 4)]),
    'thin1': (3, [cm('svc-ambush-2'), cm('svc-ambush-3'), TAIL[0]]),
    'thin3': (2, TAIL[:2]),
    'thin0': (0, None),
    'yard5': (5, [cm(f'svc-ambush-{k}') for k in range(1, 4)] + TAIL[:2]),  # the posted three + lookout and wheelman by car from rival-park (F6 Parked)
}
for v, (n, spots) in SPOTS.items():
    root = f'{OUT}/root-{v}'
    if os.path.exists(root): shutil.rmtree(root)
    os.makedirs(f'{root}/contracts/rules'); os.makedirs(f'{root}/contracts/people')
    rb = copy.deepcopy(rb0); rb['values']['fight-safe-hostiles']['v'] = n
    json.dump(rb, open(f'{root}/contracts/rules/rulebook.json', 'w'), indent=1)
    shutil.copy('contracts/people/people.rules.json', f'{root}/contracts/people/people.rules.json')
    shutil.copytree('sim/data', f'{root}/sim/data')
    s = copy.deepcopy(site0)
    s['places']['getaway-yard'] = {'at': YARD, 'src': 'Motive 059 S1 probe: the service-drive stand (mission point service-drive)'}
    for k in range(1, 5): s['places'][f'yard-police-{k}'] = {'at': cm(f'svc-police-{k}'), 'src': f'Motive 059 S1 probe: svc-police-{k}'}
    s['places']['yard-entry'] = {'at': s['places']['d:Official entrance']['at'], 'src': 'Motive 059 S1 probe: the loud entry nearest the yard (S2 yes)'}
    s['getaway']['car'] = 'getaway-yard'
    s['getaway']['exit'] = copy.deepcopy(svc['exit'])
    s['fight']['police'] = [f'yard-police-{k}' for k in range(1, 5)]; s['fight']['entry'] = 'yard-entry'
    if spots is not None:
        for k, p in enumerate(spots): s['places'][f'probe-ambush-{k+1}'] = {'at': p, 'src': f'Motive 059 S1 probe ({v})'}
        s['fight']['ambush'] = [f'probe-ambush-{k+1}' for k in range(len(spots))]
    json.dump(s, open(f'{root}/sim/data/handover.site.json', 'w'), indent=1)
    print(v, 'n', n, 'spots', s['fight']['ambush'] and [s['places'][x]['at'] for x in s['fight']['ambush']])
print('yard', YARD, 'tail', TAIL)

# ---- scripts
SRC = 'runs/missions/handover/c037/routes'
os.makedirs(f'{OUT}/scripts', exist_ok=True)
POSTS = {'scout': snap((7700, -1900)), 'fixer': snap((7700, -1300)), 'tech': snap((7400, -1700))}  # the yard side of the drive mouth, rival-tail 13.5-16.5 m off
print('posts', POSTS)
ROUTES = {  # vehicle: (c037 board twin, the Enforcer's enter tick, the exit tick, the ops' last pre-ending move tick to drop)
    'apress': ('route-a-pressure-board', 1060, 1440),
    'gfast': ('route-g-fast-board', 965, 1345),
    'g': ('route-g-board', 1570, 1950),
}
def front(name, enter, out):
    s = json.load(open(f'{SRC}/{name}.json'))
    keep = [o for o in s['orders'] if o['tick'] < enter or (o['tick'] == enter and o['op'] in ('enforcer',) ) or (o['tick'] == enter and o['do'] == 'call')]
    return s, keep
def save(n, s): json.dump(s, open(f'{OUT}/scripts/{n}.json', 'w'), indent=1)
for key, (name, enter, out) in ROUTES.items():
    s, keep = front(name, enter, out)
    # leak: everyone to the car; the Enforcer boards with Vale; Go every 0.5 s from then (no gate: 0.31's hold only)
    o = list(keep)
    for op in ('scout', 'fixer', 'tech'):
        o.append({'tick': enter, 'op': op, 'do': 'move', 'to': 'getaway-yard', 'run': True})
        for d in range(0, 120, 10): o.append({'tick': out - 60 + d, 'op': op, 'do': 'board', 'target': 'car'})
    o.append({'tick': out, 'op': 'enforcer', 'do': 'move', 'to': 'getaway-yard'})
    for d in range(40, 140, 5): o.append({'tick': out + d, 'op': 'enforcer', 'do': 'board', 'target': 'car'})
    # no Go here: run.py reads Vale's boarding from this pass and asks Go from the next tick (a Go before Vale is in withdraws)
    o.sort(key=lambda x: x['tick']); x = copy.deepcopy(s); x['orders'] = o; x['until'] = out + 1200; x['note'] = f'Motive 059 S1 probe {key}-leak'; save(f'{key}-leak', x)
    # fight pass 1: the three hold the drive mouth drawn, at will; the Enforcer boards with Vale; nobody else boards, no Go (read blocker.out)
    o = list(keep)
    for op, at in POSTS.items(): o.append({'tick': enter, 'op': op, 'do': 'move', 'to': at, 'run': True})
    for t in (out + 1, out + 20, out + 60, out + 120): o.append({'tick': t, 'op': 'cell', 'do': 'panic'})
    o.append({'tick': out, 'op': 'enforcer', 'do': 'move', 'to': 'getaway-yard'})
    for d in range(40, 140, 5): o.append({'tick': out + d, 'op': 'enforcer', 'do': 'board', 'target': 'car'})
    o.sort(key=lambda x: x['tick']); x = copy.deepcopy(s); x['orders'] = o; x['until'] = out + 1500; x['note'] = f'Motive 059 S1 probe {key}-fight (pass 1)'; save(f'{key}-fight', x)
# focus fire (g only): the three draw before the Enforcer comes out and fire on one rival at a time, nearest the door first (rival-1 at
# svc-ambush-1 by the service door); a player's best answer to a crew lying in wait round the yard
s, keep = front('route-g-board', 1570, 1950); out = 1950
o = list(keep)
for op, at in POSTS.items():
    o.append({'tick': 1570, 'op': op, 'do': 'move', 'to': at, 'run': True})
    o.append({'tick': out - 30, 'op': op, 'do': 'draw'})
    for k, t in ((1, out + 1), (2, out + 25), (3, out + 45), (4, out + 65), (5, out + 85)): o.append({'tick': t, 'op': op, 'do': 'fire', 'target': f'rival-{k}'})
for t in (out + 1, out + 110): o.append({'tick': t, 'op': 'cell', 'do': 'panic'})
o.append({'tick': out, 'op': 'enforcer', 'do': 'move', 'to': 'getaway-yard'})
for d in range(40, 140, 5): o.append({'tick': out + d, 'op': 'enforcer', 'do': 'board', 'target': 'car'})
o.sort(key=lambda x: x['tick']); x = copy.deepcopy(s); x['orders'] = o; x['until'] = out + 1500; x['note'] = 'Motive 059 S1 probe gff-fight (pass 1)'; save('gff-fight', x)
