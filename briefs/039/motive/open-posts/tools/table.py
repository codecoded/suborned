# Motive 039 open-posts: the fight's numbers per ensemble run, from results/ens-<tag>.json (python3 -I table.py <tag>...)
import json, re, sys, os, statistics as st
here = os.path.dirname(os.path.abspath(sys.argv[0]))
CELL = ('scout', 'fixer', 'tech', 'enforcer')
def clk(t): return f"{int(t//60)}:{t%60:04.1f}"
for tag in sys.argv[1:]:
  d = json.load(open(os.path.join(here, '..', 'results', f'ens-{tag}.json')))
  print(f"== {tag}  RB={d['rb'] or '-'}  site={d['site']}")
  agg = {}
  for r in d['rows']:
    L = r.get('log') or []
    T = lambda c: next((l['tick']/10 for l in L if l['code'] == c), None)
    a, o = T('fight.ambush'), T('fight.over')
    downs = {}
    for l in L:
      m = re.match(r'The (\w+) drops (rival-\d)', l['text'])
      if l['code'] == 'fight.down' and m: downs[m.group(2)] = l['tick']/10 - a
    post = {}
    for l in L:
      m = re.match(r'(rival-\d) takes (?:his post|cover at (cv-\d+))', l['text'])
    blk = next((re.match(r'(rival-\d)', l['text']).group(1) for l in L if l['code'] == 'fight.blocker'), None)
    shots_on_cell = [l for l in L if l['code'] in ('shot.hit', 'shot.miss') and re.match(r'rival-\d fires on (%s)' % '|'.join(CELL), l['text'])]
    hits_on_cell = [l for l in shots_on_cell if l['code'] == 'shot.hit']
    cell_shots = [l for l in L if l['code'] in ('shot.hit', 'shot.miss') and re.match(r'(%s) fires on rival' % '|'.join(CELL), l['text']) and a and l['tick']/10 >= a]
    def acc(rv):
      s = [l for l in cell_shots if f'fires on {rv} ' in l['text']]; h = [l for l in s if l['code'] == 'shot.hit']
      pm = sorted({int(re.search(r'\((\d+) ‰\)', l['text']).group(1)) for l in s if re.search(r'\((\d+) ‰\)', l['text'])})
      return f"{len(h)}/{len(s)} {pm}"
    hp = {}
    for l in L:
      m = re.match(r'rival-\d hits the (\w+)\. (\d+) health left', l['text'])
      if m: hp[m.group(1)] = int(m.group(2))
    celldown = [l['code'] for l in L if re.match(r'(%s)\.down' % '|'.join(CELL), l['code'] or '')]
    brk = T('ai.break')
    print(f"{r['id']:14s} k{r['k']} {r['outcome']:8s} {r.get('fail') or '':18s} end {clk(r['tick']/10)} St {r['standing']} T {r['trail']} A {r['alert']} | spring {clk(a) if a else '-'} over {('+%.1f' % (o-a)) if a and o else '-'} break {('+%.1f' % (brk-a)) if brk and a else '-'} blocker {blk} | downs " +
          ' '.join(f"{k}+{v:.1f}" for k, v in sorted(downs.items(), key=lambda x: x[1])) +
          f" | rival fire {len(hits_on_cell)}/{len(shots_on_cell)} hp {hp or '-'} down {celldown or '-'} | cell on r1 {acc('rival-1')} r2 {acc('rival-2')} r3 {acc('rival-3')}")
    g = agg.setdefault(r['id'], {'win': 0, 'n': 0, 'len': [], 'r1': [], 'r2': [], 'r3': [], 'r4': [], 'r5': [], 'hits': [], 'end': []})
    g['n'] += 1; g['win'] += r['outcome'] == 'success'; g['end'].append(r['tick']/10)
    if a and o: g['len'].append(o - a)
    for i in range(1, 6):
      if f'rival-{i}' in downs: g[f'r{i}'].append(downs[f'rival-{i}'])
    g['hits'].append(len(hits_on_cell))
  for id, g in agg.items():
    f = lambda xs: f"{min(xs):.1f}-{max(xs):.1f} (med {st.median(xs):.1f}, n {len(xs)})" if xs else '-'
    print(f"  {id}: wins {g['win']}/{g['n']} · end {clk(min(g['end']))}-{clk(max(g['end']))} · fight {f(g['len'])} s · down after spring: r1 {f(g['r1'])} r2 {f(g['r2'])} r3 {f(g['r3'])} r4 {f(g['r4'])} r5 {f(g['r5'])} · hits on cell {f(g['hits'])}")
