# derive a plan from another: python3 -I derive.py <src plan> <out name> <python edit expression on `p` (dict) and helpers>
import json, sys, os
src, name, code = sys.argv[1], sys.argv[2], sys.argv[3]
p = json.load(open(src))
S = p['steps']
def find(op=None, do=None, label=None, n=1, **kw):
    """the n-th step matching"""
    k = 0
    for i, s in enumerate(S):
        if (op is None or s.get('op') == op) and (do is None or s.get('do') == do) and (label is None or s.get('label') == label) and all(s.get(a) == b for a, b in kw.items()):
            k += 1
            if k == n: return i
    raise SystemExit(f'no step op={op} do={do} label={label} {kw}')
def drop(i): del S[i]
exec(code)
out = os.path.join(os.path.dirname(__file__), '..', 'plans', name + '.plan.json')
open(out, 'w').write(json.dumps(p, indent=1, ensure_ascii=False) + '\n')
print('wrote', os.path.normpath(out), len(S), 'steps')
