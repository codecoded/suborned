# Motive brief 067: one table across candidate checks (results/cand-<tag>.txt): MC lines, each route's outcome, time, T, A, and slack.
# usage: python3 -I table.py <tag> …
import re, sys, os
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'results')
tags = sys.argv[1:]
rows, mcs, notc, slack, heads = {}, {}, {}, {}, {}
for t in tags:
    txt = open(os.path.join(D, f'cand-{t}.txt')).read()
    heads[t] = txt.splitlines()[0]
    notc[t] = set(re.findall(r'^NOT COMPOSED ([\w-]+):', txt, re.M))
    mcs[t] = {m.group(2): m.group(1) for m in re.finditer(r'^(ok|FAIL)\s+(MC-\d+)', txt, re.M)}
    for m in re.finditer(r'^        ([\w-]+) \(route (\w)\) (wins at ([\d:.]+), ([\d+()a-z ]+) T|failure ([\w-]+)|withdrawn|none)', txt, re.M):
        rows.setdefault(m.group(1), {})[t] = (f'win {m.group(4)} {m.group(5).strip()}T' if m.group(4) else (f'FAIL {m.group(6)}' if m.group(6) else m.group(3)))
    for m in re.finditer(r'^        (loud[\w-]*): (success|failure [\w-]+), (\d+) T', txt, re.M):
        rows.setdefault(m.group(1), {})[t] = f'{m.group(2)} {m.group(3)}T'
    for m in re.finditer(r'^        ([\w-]+) \((?:quiet|loud)\) ([\d:.]+)', txt, re.M):
        r = rows.setdefault(m.group(1), {})
        if t in r and r[t].startswith('success'): r[t] = f'win {m.group(2)} ' + r[t].split(' ')[-1]
    for m in re.finditer(r'^slack ([\w-]+): every planned step can slip (\d+) s', txt, re.M): slack.setdefault(m.group(1), {})[t] = m.group(2)
    for m in re.finditer(r'^        (careless-[\w-]+): (.*)$', txt, re.M): rows.setdefault(m.group(1), {})[t] = m.group(2)[:46]
w = 40
for t in tags: print(f'# {heads[t]}')
print('route'.ljust(30) + ''.join(t.ljust(w) for t in tags))
for k in sorted(rows):
    print(k.ljust(30) + ''.join(((rows[k].get(t, '-') + (' (stale)' if k in notc[t] else '')).ljust(w)) for t in tags))
print('MC'.ljust(30) + ''.join(' '.join(f'{k[3:]}{"" if v == "ok" else "!"}' for k, v in sorted(mcs[t].items(), key=lambda kv: int(kv[0][3:]))).ljust(w) for t in tags))
for k in sorted(slack): print(('slack ' + k).ljust(30) + ''.join((slack[k].get(t, '-') + ' s').ljust(w) for t in tags))
