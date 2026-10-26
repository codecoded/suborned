# The Handover's route scripts with the controls brief 039 retires translated to one input (scratch, for the drift report only):
# panic -> F (draw) on each of the four at its tick; stance, free and cease dropped (weapons free is always on). Usage: oneinput.py <src dir> <out dir>
import json, os, sys, shutil
src, out = sys.argv[1], sys.argv[2]
for f in sorted(os.listdir(src)):
    p = os.path.join(src, f)
    if not f.endswith('.json'): continue
    if f == 'routes.json': shutil.copy(p, os.path.join(out, f)); continue
    d = json.load(open(p, encoding='utf-8'))
    if 'orders' not in d: shutil.copy(p, os.path.join(out, f)); continue
    o2 = []
    for o in d['orders']:
        if o.get('do') == 'panic':
            for op in ('scout', 'fixer', 'tech', 'enforcer'): o2.append({'tick': o['tick'], 'op': op, 'do': 'draw'})
        elif o.get('do') in ('stance', 'free', 'cease'): continue
        else: o2.append(o)
    d['orders'] = o2
    json.dump(d, open(os.path.join(out, f), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('ok')
