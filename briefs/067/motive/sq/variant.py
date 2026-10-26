# Motive brief 067 square probe: the site variants, as changes to HEAD's civic-block.adjust.json and mission.json (scratch copies only).
# usage: python3 -I variant.py <name> <adjust.json> <mission.json>
#   k         round 1 as committed (no change)
#   nok       round 1 with item K undone: the two planter runs back, ambush-5 back at (14, -36)
#   <name>    a candidate from CANDS: civic objects added as block structures (scratch stand-ins: category pavilion-support, so the walk grid,
#             the drawn solids and the cover generator take them as the pavilion's piers are taken), and post moves in mission.json
import json, sys
name, adj_p, mis_p = sys.argv[1:4]
adj = json.load(open(adj_p)); mis = json.load(open(mis_p))
pts = {p['id']: p for p in mis['points']}
def rect(cx, cy, lx, ly): return [[round(cx - lx / 2, 3), round(cy - ly / 2, 3)], [round(cx + lx / 2, 3), round(cy - ly / 2, 3)], [round(cx + lx / 2, 3), round(cy + ly / 2, 3)], [round(cx - lx / 2, 3), round(cy + ly / 2, 3)]]
def add(n, kind, cx, cy, lx, ly, h):
    i = len([a for a in adj['adjustments'] if a.get('op') == 'addStructure' and a['item']['id'].startswith('square-pavilion-support-')]) + 2
    adj['adjustments'].insert(-1, {'op': 'addStructure', 'item': {'id': f'square-pavilion-support-{i}', 'name': f'{kind} {n} (Motive 067 probe stand-in)', 'category': 'pavilion-support', 'polygon': rect(cx, cy, lx, ly), 'z': 0, 'height': h}, 'reason': f'probe: {kind} {n}'})
def move(pid, at): pts[pid]['at'] = at; pts[pid].pop('stand', None)
# kinds (block frame metres: lx along x = north-south, ly along y = east-west; h metres)
KIOSK = dict(lx=1.6, ly=2.6, h=2.5)        # a newspaper and ticket kiosk at the boarding pavilion: enamel booth, its long side to the square
KIOSK_X = dict(lx=2.6, ly=1.6, h=2.5)      # the same kiosk turned, long side north-south
PLINTH = dict(lx=1.1, ly=1.1, h=1.2)       # a lamp standard's plinth (the sphere lamp raised on a stone base)
BENCH = dict(lx=0.7, ly=2.4, h=1.0)        # a high-backed bench, its back to the car
CANDS = {
    'kA': [('kiosk', 'south', 20.0, -43.2, KIOSK)],
    'kB': [('kiosk', 'south', 19.6, -42.6, KIOSK)],
    'kD': [('kiosk', 'south', 18.4, -42.4, KIOSK)],
    'bA': [('bench', 'south pair', 20.5, -42.8, dict(lx=0.7, ly=4.8, h=1.05))],
    'bA1': [('bench', 'south', 20.5, -43.2, dict(lx=0.7, ly=2.4, h=1.05))],
    'pL': [('plinth', 'lamp-court-w', 14.8, -41.0, PLINTH)],
    'bAp': [('bench', 'south pair', 20.5, -42.8, dict(lx=0.7, ly=4.8, h=1.05)), ('plinth', 'lamp-court-w', 14.8, -41.0, PLINTH)],
    'kAP': [('kiosk', 'south', 20.0, -43.2, KIOSK), ('plinth', 'lamp-court-w', 14.8, -41.0, PLINTH)],
}
if name == 'k': pass
elif name == 'nok':
    pr = adj['dressing']['planterRuns']
    back = [{k: v for k, v in r.items() if k != 'reason'} for r in pr['removed']]
    pr['runs'] = pr['runs'] + back; pr['removed'] = []
    move('ambush-5', [14, -36])
else:
    spec = CANDS[name]
    for item in spec:
        if item[0] == 'move': move(item[1], item[2]); continue
        kind, n, cx, cy, k = item; add(n, kind, cx, cy, **k)
json.dump(adj, open(adj_p, 'w'), indent=1, ensure_ascii=False); json.dump(mis, open(mis_p, 'w'), indent=1, ensure_ascii=False)
print(f'variant {name}')
