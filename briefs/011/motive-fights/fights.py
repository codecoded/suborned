# Motive, brief 011 (2026-10-15): a deterministic model of the PROPOSED real-time shoot-out rules (briefs/011/motive-fights.md),
# for running the numbers on the Handover's final fight. It is not the engine: no occluders (every fight spot in the site has a
# clear line to the getaway car, by its own reason text), straight-line movement, integer state at 10 Hz like the engine.
# Positions come from sim/data/handover.site.json. Run: python3 -I briefs/011/motive-fights/fights.py
import json, math, os, sys

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
SITE = json.load(open(os.path.join(REPO, 'sim/data/handover.site.json')))
def at(name):
    p = SITE['places'][name]; return tuple(p.get('stand', p['at']))

HZ = 10
# ---- the proposed values (every one is in the proposal's table, tagged [S]) ----
WEAPON = {  # damage, ticks between shots, effective and max reach in cm
    'pistol':  dict(dmg=1, every=15, eff=1500, max=2500),
    'carbine': dict(dmg=2, every=20, eff=2000, max=3000),
}
POINT_BLANK_CM = 500
ACC = dict(point=1000, eff=750, long=400)       # per mille, by band
MOD = dict(moving=500, cover=500, vale=750, trained=1000, untrained=750)  # per mille multipliers
AIM_T = 10            # 1.0 s to the first shot on a new target (weapon drawn)
AMBUSH_AIM_T = 10     # rivals waiting in ambush
WARN_T = 100          # §07: a helped Inez warns 10 s early
OP_WALK, OP_RUN, SEC_RUN = 140, 280, 220   # cm/s
SAFE_AMBUSH_CM, HOLD_CM, ACT_CM = 2000, 2500, 600
OFFICER_WEAPON = os.environ.get('OFFICER_WEAPON', 'pistol')
OFFICER_PRI = os.environ.get('OFFICER_PRI', 'shooter')
HEALTH = dict(enforcer=6, op=4, officer=3, rival=3, guard=3)

def dist(a, b): return math.isqrt((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2)

class Body:
    def __init__(s, id, side, pos, hp, weapon=None, skill='trained', cover=False, speed=0, dest=None, active=0, vale=False, retreat_at=0, role=''):
        s.id, s.side, s.pos, s.hp, s.weapon, s.skill, s.cover = id, side, pos, hp, weapon, skill, cover
        s.speed, s.dest, s.active, s.vale, s.retreat_at, s.role = speed, dest, active, vale, retreat_at, role
        s.down = s.gone = False; s.target = None; s.aim_until = 0; s.next_shot = 0; s.acc = {}; s.hits_taken = 0; s.shots = 0; s.hits = 0
        s.last_shot_by = None; s.stopped = False
    @property
    def up(s): return not s.down and not s.gone
    def moving(s): return s.dest is not None and s.pos != s.dest and not s.stopped
    def step(s):
        if not s.moving(): return
        d = dist(s.pos, s.dest); v = s.speed // HZ
        if d <= v: s.pos = s.dest; return
        s.pos = (s.pos[0] + (s.dest[0] - s.pos[0]) * v // d, s.pos[1] + (s.dest[1] - s.pos[1]) * v // d)

def accuracy(sh, tg):
    w = WEAPON[sh.weapon]; d = dist(sh.pos, tg.pos)
    if d > w['max']: return 0
    a = ACC['point'] if d <= POINT_BLANK_CM else ACC['eff'] if d <= w['eff'] else ACC['long']
    for cond, m in ((sh.moving(), MOD['moving']), (tg.cover and not tg.moving(), MOD['cover']), (sh.vale, MOD['vale']), (True, MOD[sh.skill])):
        if cond: a = a * m // 1000
    return a

def fight(bodies, branch, enforcer_policy='stop', focus=True, limit=1200, log=False):
    """branch 'safe': hostiles appear when the Enforcer brings Vale within 20 m of the car; 'loud': police run in from the start."""
    car = at('d:Getaway bay'); E = next(b for b in bodies if b.id == 'enforcer')
    sprung = 0 if branch == 'loud' else None; t = 0; events = []
    hostiles = [b for b in bodies if b.side == 'host']; friends = [b for b in bodies if b.side in ('cell', 'ally')]
    def ev(s):
        if log: events.append(f"{t / HZ:6.1f}  {s}")
    while t < limit:
        t += 1
        if sprung is None and dist(E.pos, car) <= SAFE_AMBUSH_CM:
            sprung = t; ev('ambush springs')
            for h in hostiles: h.active = t; h.aim_until = max(h.aim_until, t + h.aim_until)  # aim_until held the delay
            for f in friends:
                if f.side == 'ally': f.active = t
        live_h = [h for h in hostiles if h.up and (sprung is not None) and t >= h.active]
        # the Enforcer: walks to the car; 'stop' policy halts to fight while a hostile stands within his effective reach
        if E.up:
            near = [h for h in live_h if dist(E.pos, h.pos) <= WEAPON['carbine']['eff']]
            E.stopped = enforcer_policy == 'stop' and bool(near) and dist(E.pos, car) > ACT_CM
        for b in bodies:
            if b.side == 'host' and b.weapon:
                b.stopped = any(f.up and dist(b.pos, f.pos) <= WEAPON[b.weapon]['eff'] for f in friends)
        for b in bodies:
            if b.up and (b.side != 'host' or (sprung is not None and t >= b.active)) and (b.side != 'ally' or (sprung is not None and t >= b.active)): b.step()
        # targets
        for b in bodies:
            if not b.up or b.weapon is None: continue
            if b.side == 'host' and (sprung is None or t < b.active): continue
            if b.side == 'ally' and (sprung is None or t < b.active): continue
            foes = [h for h in hostiles if h.up and sprung is not None and t >= h.active] if b.side in ('cell', 'ally') else [f for f in friends if f.up]
            foes = [f for f in foes if dist(b.pos, f.pos) <= WEAPON[b.weapon]['max']]
            if not foes: b.target = None; continue
            if b.side == 'host':
                # proposal (Pulse's fight AI): 1 whoever is shooting at me, 2 the one holding Vale, 3 the nearest. OFFICER_PRI=enforcer swaps 1 and 2 for police.
                vale = [E] if E in foes else []
                shooter = [f for f in foes if f is b.last_shot_by]
                pri = (vale + shooter if (b.role == 'officer' and OFFICER_PRI == 'enforcer') else shooter + vale) or sorted(foes, key=lambda f: dist(b.pos, f.pos))
                want = pri[0]
            elif focus and b.side == 'cell':
                want = min(foes, key=lambda f: (dist(E.pos, f.pos), f.id))   # group focus: the hostile nearest the Enforcer
            else:
                want = min(foes, key=lambda f: (dist(b.pos, f.pos), f.id))
            if want is not b.target:
                b.target = want; b.aim_until = max(b.aim_until, t + AIM_T); b.acc[want.id] = b.acc.get(want.id, 500)
            if t >= b.aim_until and t >= b.next_shot:
                a = accuracy(b, want); b.next_shot = t + WEAPON[b.weapon]['every']; b.shots += 1
                b.acc[want.id] += a; want.last_shot_by = b
                if b.acc[want.id] >= 1000:
                    b.acc[want.id] -= 1000; b.hits += 1; want.hp -= WEAPON[b.weapon]['dmg']; want.hits_taken += 1
                    ev(f"{b.id} hits {want.id} ({max(want.hp, 0)} left)")
                    if want.hp <= 0: want.down = True; ev(f"{want.id} down")
        # rival morale (proposal: Pulse's fight AI): once more than half the crew is down, the rest run. Police hold.
        rv = [h for h in hostiles if h.role == 'rival']
        if rv and sum(1 for h in rv if h.down) * 2 > len(rv):
            for h in rv:
                if h.up: h.gone = True; ev(f"{h.id} runs with the crew")
        if not E.up: return dict(result='enforcer-down', t=t, sprung=sprung, E=E, bodies=bodies, events=events)
        held = any(h.up and dist(h.pos, car) <= HOLD_CM for h in hostiles) or (sprung is None)
        if dist(E.pos, car) <= ACT_CM and not held and not any(h.up for h in hostiles if sprung is not None and t >= h.active and dist(h.pos, E.pos) <= WEAPON['pistol']['max']):
            return dict(result='success', t=t, sprung=sprung, E=E, bodies=bodies, events=events)
    return dict(result='timeout', t=t, sprung=sprung, E=E, bodies=bodies, events=events)

# ---- the Handover's final fight set-ups ----
def cell(support, vale_from):
    car = at('d:Getaway bay')
    b = [Body('enforcer', 'cell', vale_from, HEALTH['enforcer'], 'carbine', 'trained', speed=OP_WALK, dest=car, vale=True)]
    spots = {'scout': (car[0] - 300, car[1] + 300), 'fixer': (car[0] + 300, car[1] + 300), 'tech': (car[0], car[1] - 400)}
    for op in support: b.append(Body(op, 'cell', spots[op], HEALTH['op'], 'pistol', 'untrained', cover=True))
    return b

def safe(n_rivals, warned, guard_ally, support, cover=False):
    b = cell(support, at('d:Service lane'))
    for i in range(n_rivals):
        h = Body(f'rival-{i + 1}', 'host', at(f'ambush-{i + 1}'), HEALTH['rival'], 'pistol', 'untrained', cover=cover, active=10 ** 9, retreat_at=1, role='rival')
        h.aim_until = AMBUSH_AIM_T + (WARN_T if warned else 0); b.append(h)
    if guard_ally: b.append(Body('guard', 'ally', at('d:Office entrance'), HEALTH['guard'], 'pistol', 'trained', speed=SEC_RUN, dest=at('guard-ally'), active=10 ** 9))
    return fight(b, 'safe')

RESPONSE_AT = (-62, -4457)   # where the response unit stands when the Enforcer leaves the service lane on a fastest loud run (first shot 1:33, seen: +18 s; 23 s along its route)
def loud(n_officers, support, cover=False, response=True):
    b = cell(support, at('d:Service lane'))
    entry = at('security-entry')
    if response:
        for i in range(2):
            b.append(Body(f'response-{i + 1}', 'host', (RESPONSE_AT[0], RESPONSE_AT[1] + 150 * i), HEALTH['officer'], OFFICER_WEAPON, 'trained', speed=SEC_RUN, dest=at('d:Office approach'), role='officer'))
    for i in range(n_officers):
        b.append(Body(f'officer-{i + 1}', 'host', entry, HEALTH['officer'], OFFICER_WEAPON, 'trained', cover=cover, speed=SEC_RUN, dest=at(f'police-{i + 1}'), role='officer'))
    return fight(b, 'loud')

def summary(name, r):
    E = r['E']; cellb = [b for b in r['bodies'] if b.side == 'cell']; host = [b for b in r['bodies'] if b.side == 'host']
    start = r['sprung'] or 0
    dur = (r['t'] - start) / HZ
    others = ', '.join(f"{b.id} {b.hp}/{HEALTH['op']}" for b in cellb if b.id != 'enforcer') or '-'
    out = sum(1 for h in host if h.down); ran = sum(1 for h in host if h.gone)
    return f"| {name} | {r['result']} | {dur:.1f} s | {(r['t']) / HZ:.1f} s | {E.hp}/{HEALTH['enforcer']} | {others} | {out} down, {ran} ran |"

def ttd_table():
    rows = []
    for wname, w in WEAPON.items():
        for band, a in (('point blank', ACC['point']), ('effective', ACC['eff']), ('long', ACC['long'])):
            for label, mods in (('standing', []), ('moving', [MOD['moving']])):
                cells = []
                for hp in (2, 3, 4, 6):
                    acc, n, hits = 500, 0, 0; aa = a
                    for m in mods: aa = aa * m // 1000
                    while hits * w['dmg'] < hp:
                        n += 1; acc += aa
                        if acc >= 1000: acc -= 1000; hits += 1
                    cells.append(f"{(AIM_T + (n - 1) * w['every']) / HZ:.1f} s ({n})")
                rows.append(f"| {wname} | {band} | {label} | {aa} | " + ' | '.join(cells) + ' |')
    return rows

if __name__ == '__main__':
    print('## Time to drop, one shooter, standing target, no cover (first shot after the 1.0 s aim; shots in brackets)')
    print('| weapon | band | shooter | accuracy per mille | civilian 2 | guard, officer, rival 3 | patrol, Scout/Fixer/Tech 4 | Enforcer 6 |')
    print('|---|---|---|---|---|---|---|---|')
    for r in ttd_table(): print(r)
    print()
    print('## The final fight under the proposal (fight time from the ambush or the police dispatch; total from the Enforcer leaving the service lane with Vale)')
    print('| set-up | result | fight | from service lane | Enforcer | support | hostiles |')
    print('|---|---|---|---|---|---|---|')
    S2, S3 = ['scout', 'fixer'], ['scout', 'fixer', 'tech']
    for name, r in [
        ('safe A: 5 rivals, warned, guard ally, Enforcer alone', safe(5, True, True, [])),
        ('safe A: 5 rivals, warned, guard ally, + Scout, Fixer', safe(5, True, True, S2)),
        ('safe A: 5 rivals, NOT warned, NO guard (he saw a runner), alone', safe(5, False, False, [])),
        ('safe A: 5 rivals, NOT warned, NO guard, + Scout, Fixer', safe(5, False, False, S2)),
        ('safe A: 5 rivals in cover, warned, guard, + Scout, Fixer', safe(5, True, True, S2, cover=True)),
        ('safe B: 3 rivals, not warned, guard ally, alone', safe(3, False, True, [])),
        ('safe B: 3 rivals, not warned, guard ally, + Scout, Fixer', safe(3, False, True, S2)),
        ('loud, no response unit (as the engine counts today): 2 officers, alone', loud(2, [], response=False)),
        ('loud, no response unit: 2 officers, + Scout, Fixer', loud(2, S2, response=False)),
        ('loud: response unit (2) + 2 officers, Enforcer alone', loud(2, [])),
        ('loud: response + 3 officers (Alert 3), alone', loud(3, [])),
        ('loud: response + 4 officers (Alert 3 + civilian shot), alone', loud(4, [])),
        ('loud: response + 2 officers, + Scout, Fixer', loud(2, S2)),
        ('loud: response + 3 officers, + Scout, Fixer', loud(3, S2)),
        ('loud: response + 2 officers, + Scout, Fixer, Tech', loud(2, S3)),
        ('loud: response + 4 officers, + Scout, Fixer, Tech', loud(4, S3)),
        ('loud: response + 4 officers in cover, + Scout, Fixer, Tech', loud(4, S3, cover=True)),
    ]:
        print(summary(name, r))
    if '--log' in sys.argv:
        b = cell(S2, at('d:Service lane')); entry = at('security-entry')
        for i in range(3): b.append(Body(f'officer-{i + 1}', 'host', entry, HEALTH['officer'], 'pistol', 'trained', speed=SEC_RUN, dest=at(f'police-{i + 1}'), role='officer'))
        print('\n'.join(fight(b, 'loud', log=True)['events']))
