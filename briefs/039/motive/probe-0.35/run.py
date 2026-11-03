import json, sys, os, subprocess, re
DLL='/Users/sky/Projects/suborned/sim/cli/bin/Release/net10.0/suborned-sim.dll'
here=os.path.dirname(os.path.abspath(sys.argv[0])); root=os.path.dirname(here)
variants=sys.argv[1].split(','); names=sys.argv[2].split(',') if len(sys.argv)>2 else sorted(f[:-5] for f in os.listdir(here) if f.endswith('.json'))
def clk(s): m,ss=s.split(':'); return int(m)*60+float(ss)
for n in names:
  for v in variants:
    r=subprocess.run(['dotnet',DLL,'range','play',os.path.join(here,n+'.json'),'--json'],cwd=os.path.join(root,v),capture_output=True,text=True)
    d=json.loads(r.stdout); log=d['log']
    L=[(l['tick']/10, l['code'], l['text']) for l in log]
    cell=[]; hd=[]; brk=None; hits_on_cell=0; last=0
    for c,code,txt in L:
      t=c
      if code and code.endswith('.down') and code.split('.')[0] in ('scout','fixer','tech','enforcer'): cell.append(f"{code.split('.')[0][:3]}@{t:.1f}")
      if code=='fight.down': hd.append(t)
      if code in('ai.break','rivals.retreat') and brk is None: brk=t
      if code=='shot.hit' and re.search(r'fires on (scout|fixer|tech|enforcer) ',txt): hits_on_cell+=1
      if code and code.startswith('shot.'): last=t
    print(f"{n:24s} {v:10s} hostiles down {len(hd)} (last {max(hd) if hd else '-'}) break {brk} cell down {cell or '-'} hits taken {hits_on_cell} last shot {last}")
