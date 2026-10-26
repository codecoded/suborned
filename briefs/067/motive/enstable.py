# Motive brief 067: the ensembles side by side (results/ens-<tag>.tsv): per plan, wins of n, the end time at k=0 (and the range over wins), T, A, St.
# usage: python3 -I enstable.py <tag> …
import os, sys, statistics
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'results')
tags = sys.argv[1:]; data = {}
def secs(c): m, s = c.split(':'); return int(m) * 60 + float(s)
def clock(x): return f'{int(x // 60)}:{x % 60:04.1f}'
for t in tags:
    for l in open(os.path.join(D, f'ens-{t}.tsv')):
        if l.startswith('#') or l.startswith('plan'): continue
        f = l.rstrip('\n').split('\t')
        data.setdefault(f[0], {}).setdefault(t, []).append(f)
w = 34
print('plan'.ljust(24) + ''.join(t.ljust(w) for t in tags))
for p in sorted(data):
    cells = []
    for t in tags:
        rs = data[p].get(t)
        if not rs: cells.append('-'); continue
        wins = [r for r in rs if r[2] == 'success']
        if wins:
            ends = [secs(r[4]) for r in wins]
            T = sorted({r[5] for r in wins}); A = sorted({r[6] for r in wins}); St = sorted({r[7] for r in wins})
            cells.append(f'{len(wins)}/{len(rs)} {clock(min(ends))}-{clock(max(ends))} T{"/".join(T)} A{"/".join(A)} St{"/".join(St)}')
        else:
            fails = sorted({(r[3] or r[2]) for r in rs})
            cells.append(f'0/{len(rs)} ' + ','.join(fails)[:w - 6])
    print(p.ljust(24) + ''.join(c.ljust(w) for c in cells))
