# Motive 059 S1 probe runner (2026-10-25). Run from the repo root, under the guard:
#   bun run guard python3 -I briefs/059/motive/s1/run.py
# Plays the scratch scripts (mkprobe.py) on the scratch engine (engine 0.31.0 + two probe switches, env off = 0.31.0 byte for byte):
#   PROBE_APPEAR  per-hostile tick at which a safe-fight hostile steps out ("+n" = n ticks after the fight starts), standing in for
#                 059 F6's arrivals by car; the blocker is picked from those on foot
#   PROBE_GATE=1  the S1 rule: Go with Vale aboard is refused while the final fight is on and its blocker is not out (down or gone)
# Pass 1 (no op boards, no Go) reads blocker.out and fight.over; pass 2 boards the three at blocker.out (board and go) or fight.over
# (fight it out) and asks Go every 0.2 s. Writes out/scratch/motive/059-s1/logs/ and prints the table results.txt keeps.
import json, os, subprocess, copy, re, sys
OUT = 'out/scratch/motive/059-s1'; ENG = f'{OUT}/engine/bin/suborned-sim.dll'
os.makedirs(f'{OUT}/logs', exist_ok=True)
F6 = '1721,1741,1761,1781,1801'  # 059 F6, car Off at H: enters 2:35.3, stops at rival-tail 2:50.1, five step out 2 s apart (Pulse measures.json)
CASES = [  # (name, vehicle, root, n, PROBE_APPEAR, gate, ending)
    ('apress-leak', 'apress', 'arrive', 5, F6, '0', 'leak'),
    ('gfast-leak', 'gfast', 'arrive', 5, F6, '0', 'leak'),
    ('apress-gate', 'apress', 'arrive', 5, F6, '1', 'fight'),
    ('gfast-gate', 'gfast', 'arrive', 5, F6, '1', 'fight'),
    ('g-posted', 'g', 'posted', 5, '', '1', 'fight'),
    ('g-posted3', 'g', 'posted3', 3, '', '1', 'fight'),
    ('g-thin1', 'g', 'thin1', 3, ',,+198', '1', 'fight'),
    ('g-thin3', 'g', 'thin3', 2, '+198,+217', '1', 'fight'),
    ('g-thin0-leak', 'g', 'thin0', 0, '', '0', 'leak'),
    ('gff-posted', 'gff', 'posted', 5, '', '1', 'fight'),
    ('gff-posted3', 'gff', 'posted3', 3, '', '1', 'fight'),
    ('gff-thin1', 'gff', 'thin1', 3, ',,+198', '1', 'fight'),
    ('gff-yard5', 'gff', 'yard5', 5, ',,,+196,+216', '1', 'fight'),
]
EXIT = {'apress': 1440, 'gfast': 1345, 'g': 1950, 'gff': 1950}
LINE = re.compile(r'^\s+(\d+):(\d+\.\d)\s+(\S+)\s+(.*)$')
def tick(m, s): return int(m) * 600 + round(float(s) * 10)
def play(script, root, appear, gate, tag):
    env = dict(os.environ, PROBE_APPEAR=appear, PROBE_GATE=gate)
    r = subprocess.run(['dotnet', os.path.abspath(ENG), 'play', os.path.abspath(script)], cwd=f'{OUT}/root-{root}', env=env, capture_output=True, text=True)
    open(f'{OUT}/logs/{tag}.log', 'w').write(r.stdout + r.stderr)
    j = subprocess.run(['dotnet', os.path.abspath(ENG), 'play', os.path.abspath(script), '--json'], cwd=f'{OUT}/root-{root}', env=env, capture_output=True, text=True)
    d = json.loads(j.stdout)
    ev = {}
    for l in r.stdout.splitlines():
        m = LINE.match(l)
        if m: ev.setdefault(m.group(3), []).append((tick(m.group(1), m.group(2)), m.group(4)))
    return d, ev, r.stdout.splitlines()[0]
def first(ev, code): return ev[code][0][0] if code in ev else None
def clock(t): return '-' if t is None else f'{t // 600}:{(t % 600) / 10:04.1f}'
rows = []
for name, veh, root, n, appear, gate, ending in CASES:
    out = EXIT[veh]
    if ending == 'leak':
        d, ev, head = play(f'{OUT}/scripts/{veh}-leak.json', root, appear, gate, name + '-pass1')
        vale = next((t for t, x in ev.get('board.in', []) if 'Vale' in x), None)
        if vale is not None:
            s = json.load(open(f'{OUT}/scripts/{veh}-leak.json'))
            s['orders'] = sorted(s['orders'] + [{'tick': t, 'op': 'cell', 'do': 'go'} for t in range(vale + 1, vale + 1200, 2)], key=lambda x: x['tick'])
            p = f'{OUT}/scripts/{name}.json'; json.dump(s, open(p, 'w'), indent=1)
            d, ev, head = play(p, root, appear, gate, name)
        rows.append((name, head, d, ev, vale, None, None)); continue
    d1, ev1, head1 = play(f'{OUT}/scripts/{veh}-fight.json', root, appear, gate, name + '-pass1')
    b = first(ev1, 'blocker.out'); f = first(ev1, 'fight.over'); vale = next((t for t, x in ev1.get('board.in', []) if 'Vale' in x), None)
    for mode, at in (('board', b), ('fightout', f)):
        if at is None: rows.append((f'{name}-{mode}', f'pass 1: no {"blocker.out" if mode == "board" else "fight.over"} ({head1})', d1, ev1, vale, b, f)); continue
        s = json.load(open(f'{OUT}/scripts/{veh}-fight.json'))
        o = [x for x in s['orders']]
        for op in ('scout', 'fixer', 'tech'):
            o.append({'tick': at + 1, 'op': op, 'do': 'move', 'to': 'getaway-yard', 'run': True})
            for k in range(5, 150, 5): o.append({'tick': at + 1 + k, 'op': op, 'do': 'board', 'target': 'car'})
        for t in range(max(at + 50, (vale or 0) + 1), at + 400, 2): o.append({'tick': t, 'op': 'cell', 'do': 'go'})
        o.sort(key=lambda x: x['tick']); s['orders'] = o; s['until'] = at + 420
        p = f'{OUT}/scripts/{name}-{mode}.json'; json.dump(s, open(p, 'w'), indent=1)
        d, ev, head = play(p, root, appear, gate, f'{name}-{mode}')
        rows.append((f'{name}-{mode}', head, d, ev, vale, b, f))
print(f"{'case':24s} {'outcome':9s} {'end':>6s} {'T':>3s} {'A':>2s} {'St':>3s}  vale-in  arrive1  blocker  b.out    f.over   downs(cell)  go-refused")
for name, head, d, ev, vale, b, f in rows:
    downs = sorted({c.split('.')[0] for c in ev if c.endswith('.down') and c.split('.')[0] in ('scout', 'fixer', 'tech', 'enforcer')})
    refused = sum(1 for t, x in ev.get('order.rejected', []) if 'go' in x.split(':')[0])
    print(f"{name:24s} {d['outcome']:9s} {clock(d['tick']):>6s} {d['trail']:>3d} {d['alert']:>2d} {d.get('standing', 0):>3d}  {clock(vale):7s}  {clock(first(ev, 'probe.arrives')):7s}  {clock(first(ev, 'fight.blocker')):7s}  {clock(first(ev, 'blocker.out') or b):7s}  {clock(first(ev, 'fight.over') or f):7s}  {','.join(downs) or '-':11s}  {refused}")
    cap = [x for t, x in ev.get('captured', [])]
    if cap or d['outcome'] != 'success': print('   ', head, '|', '; '.join(cap)[:200])
