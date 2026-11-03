"""Brief 069 end-to-end: byte-for-byte check of the deliverables of two runs of run_e2e.sh (the same folder twice, the first copied aside, and also two
different folders: the FBX export writes no absolute path, so both must match). Every file under basis/, fit/ and a/ is hashed and
compared; the per-stage timing fields ('seconds') are the only allowed difference and are compared with them taken out of the JSON.
  python3 tools/figures/charpipe/fit/determinism.py <run1 dir> <run2 dir> <out json>"""
import hashlib, json, os, sys


def walk(d):
    out = {}
    for sub in ('basis', 'fit', 'a'):
        for r, _, fs in os.walk(os.path.join(d, sub)):
            for f in fs:
                if f.endswith('.blend1'): continue
                p = os.path.join(r, f); out[os.path.relpath(p, d)] = p
    return out


def norm_json(p):
    def strip(x):
        if isinstance(x, dict): return {k: strip(v) for k, v in x.items() if k != 'seconds'}
        if isinstance(x, list): return [strip(v) for v in x]
        return x
    return hashlib.sha256(json.dumps(strip(json.load(open(p))), sort_keys=True).encode()).hexdigest()


a, b = walk(sys.argv[1]), walk(sys.argv[2]); rows = []; same = diff = 0
for k in sorted(set(a) | set(b)):
    if k not in a or k not in b: rows.append({'file': k, 'result': 'missing in one run'}); diff += 1; continue
    ha = hashlib.sha256(open(a[k], 'rb').read()).hexdigest(); hb = hashlib.sha256(open(b[k], 'rb').read()).hexdigest()
    if ha == hb: res = 'identical'
    elif k.endswith('.json') and norm_json(a[k]) == norm_json(b[k]): res = 'identical apart from timings'
    else: res = 'DIFFERENT'
    rows.append({'file': k, 'result': res, 'sha256': ha if ha == hb else [ha, hb]})
    if res == 'DIFFERENT': diff += 1
    else: same += 1
json.dump({'run1': sys.argv[1], 'run2': sys.argv[2], 'files': len(rows), 'same': same, 'different': diff, 'rows': rows}, open(sys.argv[3], 'w'), indent=1)
print('files', len(rows), 'same', same, 'different', diff); [print(' ', r['file'], r['result']) for r in rows if r['result'] not in ('identical', 'identical apart from timings')]
