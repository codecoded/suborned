import subprocess,os,shutil
W='/Users/sky/Projects/suborned/out/scratch/keel/wt080'; R='/Users/sky/Projects/suborned/briefs/080/waymark'; T='/Users/sky/Projects/suborned/out/scratch/waymark/patchbase'
shutil.rmtree(T,ignore_errors=True); os.makedirs(T)
def sh(*a,cwd=T,**k): return subprocess.run(a,cwd=cwd,capture_output=True,text=True,**k)
A=['sim/cli/Batch.cs','tools/missions/check/cli.ts','tools/missions/check/run.ts','tools/missions/check/judge.ts','tools/missions/check/check.test.ts','contracts/missions/routes.schema.ts','contracts/missions/handover/mission.json','tools/missions/handover/mission.test.ts','tools/game/briefstills.test.ts','tools/routines/rounds.test.ts']
RT=[l[3:] for l in sh('git','status','--short','contracts/missions/handover/routes',cwd=W).stdout.splitlines()]
sh('git','init','-q','.'); sh('git','config','user.email','x@x'); sh('git','config','user.name','x'); sh('git','config','commit.gpgsign','false')
for f in A+RT:
    r=subprocess.run(['git','show','HEAD:'+f],cwd=W,capture_output=True)
    if r.returncode==0:
        os.makedirs(os.path.dirname(os.path.join(T,f)),exist_ok=True); open(os.path.join(T,f),'wb').write(r.stdout)
sh('git','add','-A'); sh('git','commit','-qm','base')
a=sh('git','apply','--include=sim/cli/Batch.cs','--include=tools/missions/check/*','--include=contracts/missions/*','--include=tools/missions/handover/*','--include=tools/game/briefstills.test.ts','--include=tools/routines/rounds.test.ts','--include=contracts/missions/handover/routes/*','/Users/sky/Projects/suborned/briefs/080/keel/engine-0.37.0.patch'); print('keel apply',a.returncode,a.stderr[:200])
sh('git','add','-A'); sh('git','commit','-qm','keel','--allow-empty')
for f in A: shutil.copy(os.path.join(W,f),os.path.join(T,f))
for f in ['sim/cli/Adaptive.cs','tools/missions/check/bands.ts','tools/missions/check/bands.test.ts','tools/missions/check/fromBands.ts']+['contracts/missions/handover/adaptive/'+x for x in os.listdir(W+'/contracts/missions/handover/adaptive')]:
    os.makedirs(os.path.dirname(os.path.join(T,f)),exist_ok=True); shutil.copy(os.path.join(W,f),os.path.join(T,f))
sh('git','add','-A'); open(R+'/adaptive.patch','w').write(sh('git','diff','--cached').stdout); sh('git','commit','-qm','adaptive')
for f in RT:
    os.makedirs(os.path.dirname(os.path.join(T,f)),exist_ok=True); shutil.copy(os.path.join(W,f),os.path.join(T,f))
sh('git','add','-A'); open(R+'/retime-routes.patch','w').write(sh('git','diff','--cached').stdout)
print(len(RT),'route files')
for p in ('adaptive','retime-routes'):
    s=open(f'{R}/{p}.patch').read(); print(p,s.count('\ndiff ')+1,len(s))
