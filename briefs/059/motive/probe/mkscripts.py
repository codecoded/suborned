# Motive 059 probe scripts (scratch). loud-yard*: the c036 loud front; the cell's car at the service-drive stand; the Enforcer brings Vale
# 7.6 m from the service lane and boards; Go is asked every 0.5 s until taken.
import json, copy, os
SRC = 'runs/missions/handover/c036/routes'; OUT = 'out/scratch/motive/059/scripts'
os.makedirs(OUT, exist_ok=True)
def load(n): return json.load(open(f'{SRC}/{n}.json'))
def save(n, s): json.dump(s, open(f'{OUT}/{n}.json', 'w'), indent=1)
def ending(front, out_tick, until, ops_go, ops_board):
    o = list(front)
    for op in ('scout', 'fixer', 'tech'):
        o.append({'tick': ops_go[op], 'op': op, 'do': 'move', 'to': 'getaway-yard', 'run': True})
        for d in (0, 3, 6, 9, 12): o.append({'tick': ops_board + d, 'op': op, 'do': 'board', 'target': 'car'})
    o.append({'tick': out_tick, 'op': 'enforcer', 'do': 'move', 'to': 'getaway-yard'})
    for t in (55, 58, 61, 64, 67, 70, 75, 80): o.append({'tick': out_tick + t, 'op': 'enforcer', 'do': 'board', 'target': 'car'})
    for t in range(out_tick + 96, until, 5): o.append({'tick': t, 'op': 'cell', 'do': 'go'})
    o.sort(key=lambda x: x['tick']); return o
L = load('loud')
front = [x for x in L['orders'] if x['tick'] <= 1577 and not (x['op'] in ('scout', 'fixer') and x['tick'] == 1510) and not (x['op'] == 'tech' and x['tick'] == 1577)]
front.append({'tick': 1578, 'op': 'tech', 'do': 'cancel'})
for name, board in (('loud-yard', 2000), ('loud-yard-late-board', 2312)):
    s = copy.deepcopy(L); s['note'] = f'Motive 059 probe {name}'; s['until'] = 3400
    s['orders'] = ending(front, 2257, 3400, {'scout': 1510, 'fixer': 1510, 'tech': 1580}, board); save(name, s)
# loud-yard-fight-<b>: the three wait on foot by the car (drawn, at will once the fight opens), the Enforcer boards; the three board at out+b s
spots = {'scout': [6400, -900], 'fixer': [7400, -900], 'tech': [6935, -2100]}
for b in (20, 40, 60):
    s = copy.deepcopy(L); s['note'] = f'Motive 059 probe loud-yard-fight-{b}'; s['until'] = 3800
    o = list(front)
    for op, at in spots.items():
        o.append({'tick': 1580 if op == 'tech' else 1510, 'op': op, 'do': 'move', 'to': at, 'run': True})
        o.append({'tick': 2257, 'op': op, 'do': 'draw'}); o.append({'tick': 2257, 'op': op, 'do': 'stance', 'stance': 'free'})
        o.append({'tick': 2257 + b * 10, 'op': op, 'do': 'move', 'to': 'getaway-yard', 'run': True})
        for d in range(30, 120, 5): o.append({'tick': 2257 + b * 10 + d, 'op': op, 'do': 'board', 'target': 'car'})
    o.append({'tick': 2257, 'op': 'enforcer', 'do': 'move', 'to': 'getaway-yard'})
    for t in (55, 58, 61, 64, 67, 70, 75, 80): o.append({'tick': 2257 + t, 'op': 'enforcer', 'do': 'board', 'target': 'car'})
    for t in range(2257 + b * 10 + 125, 3800, 5): o.append({'tick': t, 'op': 'cell', 'do': 'go'})
    o.sort(key=lambda x: x['tick']); s['orders'] = o; save(f'loud-yard-fight-{b}', s)
