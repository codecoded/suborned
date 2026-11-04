# Motive, brief 096 misses: probe plans built from Keel's/Waymark's adaptive plans (never edited in place). usage: python3 -I mkplans.py <adaptive dir> <out dir>
import json, sys, copy, os
src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
def load(p): return json.load(open(os.path.join(src, p + '.adaptive.json')))
def save(p, plan, note):
    plan = copy.deepcopy(plan); plan['id'] = p; plan['note'] = 'PROBE (Motive, misses): ' + note
    json.dump(plan, open(os.path.join(out, p + '.adaptive.json'), 'w'), indent=1)
def rule(plan, rid): return next(r for r in plan['rules'] if r['id'] == rid)
def clock(plan, police):
    """the plan's thresholds read the police clock: go with whoever is in 5 s before it; carry a downed agent while 25 s are left"""
    for r in plan['rules']:
        r['when'] = [w.replace('sincelog:vale.out:50', f'sincelog:vale.out:{police - 5}').replace('!sincelog:vale.out:35', f'!sincelog:vale.out:{police - 25}') for w in r.get('when', [])]
    return plan
# 1 · quiet-b, the late Go read off the clock (60 / 75 / 90)
for P in (60, 75, 90):
    save(f'qb-c{P}', clock(load('quiet-b'), P), f'quiet-b, go-late {P-5} s and carries while {P-25} s, for split-police-s {P}')
    save(f'qbt-c{P}', clock(load('quiet-b-thinned'), P), f'quiet-b-thinned, the same clock {P}')
# 2 · quiet-b-thinned re-timed: the posted man first (posted about 2:53), the lookout after; the Scout back at our car by 6:30
t = load('quiet-b-thinned')
rule(t, 'listen-rival2')['when'] = ['posted:rival-2', 't>=:3:00', 'free:scout']
rule(t, 'pay-rival2')['when'] = ['temper:rival-2:mercenary', 'free:fixer']
rule(t, 'listen-lookout')['when'] = ['posted:rival-4', 'free:scout', 'done:listen-rival2']
rule(t, 'scout-to-lookout')['when'] = ['log:rivals.lookout-posted', 'free:scout', 'done:listen-rival2']
for P in (60, 75):
    save(f'qbt2-c{P}', clock(copy.deepcopy(t), P), f'quiet-b-thinned re-timed (the posted man from 3:00, then the lookout), clock {P}')
# 3 · the veteran, driving our car round once the hold is over (the rival car is burnt, nothing boxes the drive)
v = load('veteran')
extra = [
 {"id": "tech-to-car", "op": "tech", "do": "move", "to": "d:Getaway bay", "when": ["log:car.exploded", "t>=:4:40"], "interrupt": True, "why": "PROBE: the hold is over (response about 3:36 + 60 s): walk back to our car"},
 {"id": "tech-in", "op": "tech", "do": "board", "target": "car", "when": ["near:tech:d%Getaway bay:4", "done:tech-to-car"], "retry": 2},
 {"id": "drive", "op": "tech", "do": "drive-car", "target": "service-drive", "when": ["fin:tech-in"], "retry": 2, "why": "PROBE: our car on the drive; the driver stops at the kerb"},
 {"id": "tech-out", "op": "tech", "do": "move", "to": "cabinet", "when": ["log:car.parked"], "run": True},
]
rules = []
for r in v['rules']:
    if r['id'] == 'reroute': r['when'] = ['done:tech-out', 'free:tech']
    if r['id'] == 'take': r['when'] = ['log:car.kerbside', 'free:enforcer']
    if r['id'] == 'hide-e': r['to'] = [8150, -2100]
    for k in ('to',):
        if r.get(k) == 'd:Getaway bay' and r['id'] not in ('tech-to-car',): r[k] = 'service-drive'
    r['when'] = [w.replace('d%Getaway bay', 'service-drive') for w in r.get('when', [])]
    rules.append(r)
    if r['id'] == 'hide-t': rules.extend(extra)
v['rules'] = rules
for P in (60, 75):
    save(f'vet-drive-c{P}', clock(copy.deepcopy(v), P), f'the veteran, our car driven to the service drive after the hold, clock {P}')
for P in (60, 75, 90):
    save(f'vet-c{P}', clock(load('veteran'), P), f'the veteran as Keel wrote it, clock {P}')
    save(f'pll-c{P}', clock(load('prepared-loud-lend'), P), f'prepared-loud-lend, clock {P}')
print(sorted(os.listdir(out)))
# 4 · the late Go also before the 8:10 close (a clock past it is capped there); quiet-b and thinned again
def capped(plan, police):
    plan = clock(plan, police)
    g = rule(plan, 'go-late'); plan['rules'].append({**g, 'id': 'go-close', 'when': ['log:board.in', 't>=:8:05', '!boxed']})
    return plan
for P in (60, 75, 90):
    save(f'qbx-c{P}', capped(load('quiet-b'), P), f'quiet-b, go-late {P-5} s or 8:05, clock {P}')
    save(f'qbtx-c{P}', capped(load('quiet-b-thinned'), P), f'quiet-b-thinned, go-late {P-5} s or 8:05, clock {P}')
# 5 · loud on the lend, everyone in Popovic's car (needs the seats probe binary): the three board it once the blocker is down
def ride(plan):
    rs = [r for r in plan['rules'] if r['id'] != 'go-lent']
    for o in ('scout', 'fixer', 'tech'):
        s = o[0]
        rs.append({"id": f"tolent-{s}", "op": o, "do": "move", "to": "$at:enforcer", "when": ["log:lend.lent", "!blocker", f"up:{o}", "!log:vale.out"], "interrupt": True, "retry": 1, "why": "PROBE: the loud fight is at Popovic's car: all in it"})
        rs.append({"id": f"bl-{s}", "op": o, "do": "board", "target": "car:official", "when": [f"near:{o}:car%official:4", f"up:{o}", "!log:vale.out"], "retry": 2})
    g = {"op": "cell", "do": "go", "target": "car:official", "retry": 1, "react": "expected"}
    rs.append({**g, "id": "go-lent", "when": ["log:lend.lent", "fin:bl-s", "fin:bl-f", "fin:bl-t"]})
    rs.append({**g, "id": "go-lent-late", "when": ["log:lend.lent", "sincelog:take.done:40", "!blocker"]})
    plan['rules'] = rs; return plan
for P in (60, 75):
    save(f'vet-ride-c{P}', capped(ride(load('veteran')), P), f'the veteran, all four out in Popovic\'s car in the loud fight, clock {P}')
    save(f'pll-ride-c{P}', capped(ride(load('prepared-loud-lend')), P), f'prepared-loud-lend, all four in Popovic\'s car, clock {P}')
    save(f'vetx-c{P}', capped(load('veteran'), P), f'the veteran as written, go-late capped, clock {P}')
    save(f'pllx-c{P}', capped(load('prepared-loud-lend'), P), f'prepared-loud-lend, go-late capped, clock {P}')
# 6 · the ride, the Go waiting for each of the three aboard or down (8 combinations), the late Go at the Take + 70 s
import itertools
def ride2(plan, wait_blocker=True):
    plan = ride(plan); rs = [r for r in plan['rules'] if r['id'] not in ('go-lent', 'go-lent-late')]
    if not wait_blocker:
        for r in rs:
            if r['id'].startswith('tolent-'): r['when'] = [w for w in r['when'] if w != '!blocker']
    g = {"op": "cell", "do": "go", "target": "car:official", "retry": 1, "react": "expected"}
    for n, combo in enumerate(itertools.product((0, 1), repeat=3)):
        w = ["log:lend.lent"] + [f"fin:bl-{s}" if c else f"down:{o}" for (o, s), c in zip((('scout', 's'), ('fixer', 'f'), ('tech', 't')), combo)]
        rs.append({**g, "id": f"go-lent-{n}", "when": w})
    rs.append({**g, "id": "go-lent-late", "when": ["log:lend.lent", "sincelog:take.done:70", "!blocker"]})
    plan['rules'] = rs; return plan
for P in (60, 75):
    save(f'vet-ride2-c{P}', capped(ride2(load('veteran')), P), f"the veteran, all four in Popovic's car once the blocker is down, clock {P}")
    save(f'pll-ride2-c{P}', capped(ride2(load('prepared-loud-lend'), False), P), f"prepared-loud-lend, all four in Popovic's car, clock {P}")
# 7 · thinned, the second Listen only if it starts by 4:40 (else the Scout stays clear of the posted men); and with a takedown-free fallback
for P in (60, 75):
    t = capped(load('quiet-b-thinned'), P)
    rule(t, 'listen-rival2')['by'] = '4:40'
    rule(t, 'pay-rival2')['by'] = '5:50'
    save(f'qbt5-c{P}', t, f'quiet-b-thinned, the posted man Listened to only if it starts by 4:40 (paid by 5:50), clock {P}')
# 8 · thinned as the band means it: a posted man Listened to and Paid from his post (about 2:55), the lookout misled as in quiet-b
b = load('quiet-b'); t = load('quiet-b-thinned')
r2 = [copy.deepcopy(rule(t, 'listen-rival2')), copy.deepcopy(rule(t, 'pay-rival2'))]
r2[0]['when'] = ['posted:rival-2', 'free:scout', 'log:scout.listen.heard']; r2[1]['when'] = ['temper:rival-2:mercenary', 'free:fixer', 'log:turn.help.turned']
b['rules'] = b['rules'][:8] + r2 + b['rules'][8:]
rule(b, 'scout-to-lookout')['when'] = ['log:rivals.lookout-posted', 'free:scout', 'done:listen-rival2']
for P in (60, 75): save(f'qbt6-c{P}', capped(copy.deepcopy(b), P), f'quiet-b thinned by a posted man paid from 2:55, the lookout misled, clock {P}')
# 9 · one countdown after vale.out (needs the probe binary bin-one): the plan runs on past 8:10 (until 9:30)
for P in (75,):
    for base, nm in (('quiet-b', 'qb'), ('veteran', 'vet'), ('prepared-loud-lend', 'pll')):
        p = clock(load(base), P); p['until'] = 5700
        save(f'one-{nm}-c{P}', p, f'{base}, one police countdown {P} s after Popovic is out (no 8:10 cap), run to 9:30')
    t = load('quiet-b'); t['rules'] = b['rules']; t = clock(copy.deepcopy(t), P); t['until'] = 5700
    save(f'one-qbt6-c{P}', t, f'thinned by a posted man paid early, one countdown {P}')
# 10 · the ride, boarding once no hostile stands within 16 m of Popovic's car (the police take turns on the lane, so "no blocker" is too early)
def ride3(plan):
    plan = ride2(plan)
    for r in plan['rules']:
        if r['id'].startswith('tolent-'): r['when'] = ["log:lend.lent", "!hostilesnear:car%official:16:1", r['when'][2], "!log:vale.out"]
        if r['id'] == 'go-lent-late': r['when'] = ["log:lend.lent", "sincelog:take.done:90"]
    return plan
for P in (75,):
    save(f'vet-ride3-c{P}', capped(ride3(load('veteran')), P), f"the veteran, all in Popovic's car once the bay is clear, clock {P}")
    save(f'pll-ride3-c{P}', capped(ride3(load('prepared-loud-lend')), P), f"prepared-loud-lend, all in Popovic's car once the bay is clear, clock {P}")
