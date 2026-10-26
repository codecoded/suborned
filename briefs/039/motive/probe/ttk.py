# Time to down by the engine's counting rule (rulebook weapons.rule): each shooter keeps an accumulator per target, starting at acc-start
# (500); each shot adds the chance (per mille); a hit at 1000 or more (1000 comes off). First shot aim-new (1.0 s) after taking the target,
# then every cadence (pistol 1.5 s). Damage 1. Prints seconds from taking the target to down. chance(t) may vary with time (a pin for 4 s).
def down(hp, shooters, chance, cadence=15, aim=10, start=500, blind=None):
    a = [start] * shooters
    t = aim
    while t < 100000:
        for k in range(shooters):
            if blind and blind[0] <= t < blind[1]: continue  # no line (steam): no shot
            c = chance(t) if callable(chance) else chance
            a[k] += c
            if a[k] >= 1000:
                a[k] -= 1000; hp -= 1
                if hp <= 0: return t / 10
        t += cadence
    return float('inf')
opts = {
    'today': dict(op=4, enf=6, rival=3, officer=3, cover=500, pin=1000),
    'A': dict(op=6, enf=10, rival=5, officer=5, cover=500, pin=1000),
    'B': dict(op=5, enf=8, rival=3, officer=3, cover=250, pin=500),
    'C': dict(op=5, enf=8, rival=3, officer=3, cover=500, pin=1000),
}
EFF, UNTR = 750, 750
U = EFF * UNTR // 1000  # 562: untrained at the effective ring
rows = [
  ('an op in the open, two trained officers at 15 m', lambda o: down(o['op'], 2, EFF)),
  ('the Enforcer in the open, the same', lambda o: down(o['enf'], 2, EFF)),
  ('the Enforcer walking Vale, four rivals at 10 m', lambda o: down(o['enf'], 4, U)),
  ('  the same, under his covering fire (pinned 4 s)', lambda o: down(o['enf'], 4, lambda t: U * 500 // 1000 if t < 40 else U)),
  ('  the same, covering fire and the three firing on them (B: pinned throughout)', lambda o: down(o['enf'], 4, lambda t: U * 500 // 1000 if t < 40 or o['pin'] < 1000 else U)),
  ('  the same, behind the Tech\'s steam (no line 8 s)', lambda o: down(o['enf'], 4, U, blind=(0, 80))),
  ('a rival in the open, three of the cell at 11 m', lambda o: down(o['rival'], 3, U)),
  ('a rival in full cover, three of the cell at 11 m', lambda o: down(o['rival'], 3, U * o['cover'] // 1000)),
  ('  the same, marked by the Scout (his cover off)', lambda o: down(o['rival'], 3, U)),
  ('  the same, one flanker alone (outside the arc)', lambda o: down(o['rival'], 1, U)),
  ('an op in full cover, two trained officers at 15 m', lambda o: down(o['op'], 2, EFF * o['cover'] // 1000)),
  ('  the same, the officers pinned (under fire)', lambda o: down(o['op'], 2, EFF * o['cover'] // 1000 * o['pin'] // 1000)),
]
print('%-54s' % 's to down' + ''.join('%8s' % k for k in opts))
for name, f in rows:
    print('%-54s' % name + ''.join('%7.1fs' % f(o) for o in opts.values()))
