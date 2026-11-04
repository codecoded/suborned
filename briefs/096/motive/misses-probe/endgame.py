# Motive, brief 096 misses: the lend's endgame read off a batch's log (one JSON line a play). usage: python3 -I endgame.py runs.jsonl [label]
import json, sys, collections, statistics as st
rows = [json.loads(l) for l in open(sys.argv[1]) if l.startswith('{')]
lab = sys.argv[2] if len(sys.argv) > 2 else sys.argv[1]
def first(lg, code, pred=lambda e: True):
    for e in lg:
        if e['code'] == code and pred(e): return e['tick'] / 10
    return None
def q(v):
    v = sorted(v)
    return f"n{len(v):4d} med {st.median(v):6.1f} p25 {v[len(v)//4]:6.1f} p75 {v[3*len(v)//4]:6.1f}" if v else "n   0"
D = collections.defaultdict(list); C = collections.Counter(); cap = collections.Counter(); W = 0; capN = 0; allout = 0
for r in rows:
    if 'log' not in r: C['error'] += 1; continue
    lg = r['log']; win = r['outcome'] == 'success'
    C[('win' if win else 'fail:' + r['fail'])] += 1
    vo = first(lg, 'vale.out'); 
    if not win: continue
    W += 1
    caps = [e for e in lg if e['code'] == 'captured']
    capN += len(caps); allout += (len(caps) == 0)
    for e in caps:
        t = e.get('text', ''); cap['downed' if 'downed' in t else 'standing'] += 1
    if vo is None: C['win-no-vale.out'] += 1; continue
    for c in ('ai.break', 'rivals.drives-off', 'car.exploded', 'car.unboxed', 'fight.over', 'car.leaves-split', 'split.police', 'fight.ambush', 'fight.loud', 'lend.spring'):
        t = first(lg, c)
        if t is not None: D['vale.out->' + c].append(t - vo)
    end = r['tick'] / 10; D['vale.out->end'].append(end - vo); D['vale.out (clock s)'].append(vo)
    how = [e['text'] for e in lg if e['code'] == 'success']
    C['end:' + (how[-1].split('(')[-1].rstrip(').') if how else '?')] += 1
    downs = sum(1 for e in lg if e['code'].endswith('.down') and e['tick'] / 10 >= vo and e['code'].split('.')[0] in ('scout', 'fixer', 'tech', 'enforcer'))
    C[f'cell-downs-after-out:{downs}'] += 1
print('==', lab, f'plays {len(rows)} wins {W} ({W*100/len(rows):.1f} %)  all four out {allout} ({allout*100/max(W,1):.1f} % of wins)  captured/win {capN/max(W,1):.2f}', dict(cap))
for k, v in sorted(D.items()): print(f'  {k:28s} {q(v)}')
print(' ', sorted(C.items()))
