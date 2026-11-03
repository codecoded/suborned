# Waymark c038: install scratch plans as the routes' plans (contracts/missions/handover/routes/plans/<id>.plan.json).
# Steps without a `why` take the why of the c037 plan's matching step (same op, do, target, to, approach, how), where one exists.
# usage: python3 -I install.py <id>=<scratch plan name> ...   (notes from notes.json beside this file, keyed by id)
import json, os, subprocess, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../..'))
SCR = os.path.join(ROOT, 'out/scratch/waymark/plans')
DST = os.path.join(ROOT, 'contracts/missions/handover/routes/plans')
notes = json.load(open(os.path.join(os.path.dirname(__file__), 'notes.json')))
def head_plan(pid):
    r = subprocess.run(['git', 'show', f'HEAD:contracts/missions/handover/routes/plans/{pid}.plan.json'], cwd=ROOT, capture_output=True, text=True)
    return json.loads(r.stdout) if r.returncode == 0 else None
KEY = ('op', 'do', 'target', 'to', 'approach', 'how', 'stance')
def key(s): return tuple(json.dumps(s.get(k)) for k in KEY)
for arg in sys.argv[1:]:
    pid, src = arg.split('=')
    p = json.load(open(os.path.join(SCR, src + '.plan.json')))
    old = head_plan(pid) or {'steps': []}
    whys = {}
    for s in old['steps']:
        if 'why' in s: whys.setdefault(key(s), s['why'])
    out_steps = []
    for s in p['steps']:
        s = dict(s)
        if 'why' not in s and key(s) in whys: s['why'] = whys[key(s)]
        out_steps.append(s)
    note = notes.get(pid)
    if not note: raise SystemExit(f'no note for {pid}')
    q = {'schema': p['schema'], 'mission': p['mission'], 'note': note, 'start': p.get('start', {}), 'until': p['until'], 'steps': out_steps}
    open(os.path.join(DST, pid + '.plan.json'), 'w').write(json.dumps(q, indent=1, ensure_ascii=False) + '\n')
    print(f'installed {pid} <- {src} ({len(out_steps)} steps, {sum(1 for s in out_steps if "why" in s)} whys)')
