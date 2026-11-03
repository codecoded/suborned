# Motive brief 071: probe plans for the split getaway, built from HEAD's route-b-board plan (the route-B prelude: Listen and Pay on the
# driver, the false tip, the Reroute, the Take in the service bay); the getaway car is never Driven round. usage: python3 -I gen.py
import json, copy, sys
ref = json.load(open('tree/contracts/missions/handover/routes/plans/route-b-board.plan.json'))
pre = []
for s in ref['steps']:
    s = {k: v for k, v in s.items() if k != 'why'}; lab = s.get('label', '')
    if lab in ('scout-yard', 'fixer-yard', 'tech-in', 'drive', 'out', 'in', 'scout-in', 'fixer-in'): continue
    if s['do'] in ('fire', 'panic', 'go', 'board'): continue
    if s['op'] == 'tech' and s['do'] == 'move' and s.get('to') == 'd:Getaway bay': continue
    if s['op'] in ('scout', 'fixer') and s.get('to') == 'service-drive': continue
    if s['do'] == 'take': s.pop('at', None)
    pre.append(s)
AT = {'y': '5:55', 'c': '5:55', 'p': '4:30'}
WAIT = {'y': ([7300, -1900], [7300, -1100]), 'c': ('cabinet', 'cabinet')}   # y: route-b-board's yard spots; c: by the Tech's cabinet
def plan(name, note, wait, tail, start_when='split.vale-out', tech_wait=None):
    st = copy.deepcopy(pre)
    sc, fx = WAIT[wait]
    st += [{"op": "scout", "do": "move", "to": sc, "after": ["tip"], "at": AT[wait]}, {"op": "fixer", "do": "move", "to": fx, "after": ["turned"], "at": AT[wait]}]
    if tech_wait: st.append({"op": "tech", "do": "move", "to": tech_wait, "after": ["rr"]})
    st += tail(start_when)
    json.dump({"schema": ref['schema'], "mission": "handover", "note": note, "start": {}, "until": 5400, "steps": st}, open(f'plans/{name}.plan.json', 'w'), indent=1)
def walk(when, to='d:Getaway bay'):
    return [{"op": o, "do": "move", "to": to, "when": when, "label": f"{o}-walk"} for o in ('scout', 'fixer', 'tech')] + \
           [{"op": "cell", "do": "panic", "when": "fight.ambush", "label": "pan"}]
def board(when):
    out = []
    for o in ('scout', 'fixer', 'tech'):
        out += [{"op": o, "do": "move", "to": "d:Getaway bay", "after": ["pan"], "when": when}, {"op": o, "do": "board", "target": "car", "label": f"{o}-in", "every": 0.5}]
    return out + [{"op": "cell", "do": "go", "after": ["scout-in", "fixer-in", "tech-in"], "gap": 1, "every": 0.5}]
for w in 'yc':
    plan(f'split-bg-{w}', 'rearguard: board and go (board once the blocker is out)', w, lambda wh: walk(wh) + board('blocker.out'))
    plan(f'split-fo-{w}', 'rearguard: fight it out (board once the fight is over)', w, lambda wh: walk(wh) + board('fight.over'))
plan('split-none-y', 'nobody goes back for the square car', 'y', lambda wh: [])
# p: the decoy team stands at its own car before the Take ends (route-a-board's three spots by the car), as route A's three do
WAIT['p'] = ([1175, -4825], [1025, -4625])
for kind, when in (('bg', 'blocker.out'), ('fo', 'fight.over')):
    plan(f'split-{kind}-p', f'rearguard at the car before Vale is out: {kind}', 'p',
         lambda wh, when=when: [{"op": "cell", "do": "panic", "when": "fight.ambush", "label": "pan"}] + board(when), tech_wait=[675, -4625])
# ff: focus fire, all three lock the blocker (rival-2, the shortest walk to the square's lane post) at the spring, then rival-1
def ff(when):
    out = []
    for o in ('scout', 'fixer', 'tech'):
        out += [{"op": o, "do": "fire", "target": "rival-2", "label": f"{o}-l2", "every": 0.5, "when": "fight.ambush"}]
    for o in ('scout', 'fixer', 'tech'):
        out += [{"op": o, "do": "fire", "target": "rival-1", "label": f"{o}-l1", "every": 0.5, "after": [f"{o}-l2"], "when": "fight.down"}]
    out.append({"op": "cell", "do": "panic", "after": ["scout-l1"], "label": "pan", "every": 0.5})
    return out + board(when)
for kind, when in (('bg', 'blocker.out'), ('fo', 'fight.over')):
    plan(f'split-{kind}-pff', f'rearguard at the car, focus fire on the blocker: {kind}', 'p', lambda wh, when=when: ff(when), tech_wait=[675, -4625])
