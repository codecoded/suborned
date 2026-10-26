# Motive brief 067: write a candidate rulebook from the pristine one with values replaced.
# usage: python3 -I rb.py <orig rulebook.json> <out rulebook.json> [id=value ...]   (value: an integer; the entry keeps its unit, tag and src)
import json, sys
src, out, pairs = sys.argv[1], sys.argv[2], sys.argv[3:]
d = json.load(open(src))
for p in pairs:
    k, v = p.split('=', 1)
    if k not in d['values']: sys.exit(f'rb.py: no value {k} in the rulebook (unknown = reject)')
    d['values'][k]['v'] = int(v)
json.dump(d, open(out, 'w'), indent=1, ensure_ascii=False)
open(out, 'a').write('\n')
print('rulebook:', ' '.join(pairs) if pairs else 'as committed')
