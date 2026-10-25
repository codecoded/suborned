# Motive 2026-10-26: route-b-board lock variants for the lane-from-the-car (PROBE_YARDLANE) ruling. usage: python3 -I mk.py <src plan> <dst plan> <op=lock>,… [tech-out]
# each "op=r1>r2" gives that op lock r1 from the spring, then r2 after the next fight.down; replaces the src's fire steps.
import json, sys
src, dst, spec = sys.argv[1], sys.argv[2], sys.argv[3]
p = json.load(open(src)); st = [s for s in p['steps'] if s.get('do') != 'fire']
at = [k for k, s in enumerate(st) if s.get('label') == 'take'][0]
new = []
if len(sys.argv) > 4: new.append({"op": "tech", "do": "move", "to": [7300, -1500], "after": ["drive"], "when": "car.parked"})
for part in spec.split(','):
    op, locks = part.split('='); locks = locks.split('>')
    for k, t in enumerate(locks):
        s = {'op': op, 'do': 'fire', 'target': t, 'label': f'{op}-l{k+1}', 'every': 0.5}
        if k == 0: s['when'] = 'fight.ambush'; s['after'] = ['take']
        else: s['after'] = [f'{op}-l{k}']; s['when'] = 'fight.down'
        new.append(s)
st[at+1:at+1] = new; p['steps'] = st; p['note'] = f"Motive 2026-10-26 yard-lane probe: locks {spec}{' (tech out)' if len(sys.argv) > 4 else ''}"
json.dump(p, open(dst, 'w'), indent=1)
