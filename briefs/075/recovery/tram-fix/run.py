from pathlib import Path
import subprocess,json,hashlib,time
R=Path('/Users/sky/Projects/suborned'); D=R/'briefs/075/recovery/tram-fix'; S=R/'out/scratch/latch/tram-fix'
pin=json.loads((D/'inputs-before.json').read_text())
def check():
    bad=[p for p,h in pin.items() if hashlib.sha256((R/p).read_bytes()).hexdigest()!=h]
    if bad: raise RuntimeError('Protected inputs changed: '+str(bad))
state=[]
def step(name,method,*args):
    check(); start=time.time(); print('START',name,flush=True)
    cmd=['bun','briefs/075/oldbuild/swap/tools/unityrun.ts',str(S/(name+'.log')),'--','-executeMethod',method,*args]
    r=subprocess.run(cmd,cwd=R,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
    (S/(name+'.wrapper.log')).write_text(r.stdout)
    state.append(dict(name=name,command=cmd,exit=r.returncode,seconds=round(time.time()-start,2)))
    (D/'steps.json').write_text(json.dumps(state,indent=2)); print('END',name,r.returncode,flush=True)
    check()
    if r.returncode: raise RuntimeError(name+' failed')
SW='Suborned.OldTown.Editor.Swap.OldTownSwap'; M='Suborned.World.Review.Recovery075.Manifest'
world=str(R/'out/scratch/latch/recovery/integration/world.json')
check()
step('manifest-before',M,'-recoveryScene','Assets/Scenes/probe-recovery075-swap.unity','-recoveryOut',str(D),'-recoveryTag','manifest-before')
step('fix',SW+'.TramFix','-swapScene','probe-recovery075-swap','-swapWorld',world,'-swapOut',str(D))
step('manifest-after',M,'-recoveryScene','Assets/Scenes/probe-recovery075-tramfix.unity','-recoveryOut',str(D),'-recoveryTag','manifest-after')
step('shots',SW+'.Shoot','-swapScene','probe-recovery075-tramfix','-swapWorld',world,'-swapOut',str(D/'after'),'-swapShots','tram','-swapTag','tramfix')
(D/'inputs-after.json').write_text(json.dumps({p:hashlib.sha256((R/p).read_bytes()).hexdigest() for p in pin},indent=2))
