from pathlib import Path
import subprocess, json, hashlib, sys, time, os
ROOT=Path('/Users/sky/Projects/suborned'); os.chdir(ROOT)
D=ROOT/'briefs/075/recovery/latch-integration'; S=ROOT/'out/scratch/latch/recovery/integration'; L=S/'logs'; L.mkdir(exist_ok=True)
PIN=json.loads((D/'inputs-before.json').read_text())
# Others may repair checker source during the run; never allow a mixed export/walk/plan/sim/catalogue.
LOCK=[p for p in PIN if not p.startswith('unity/ProjectSettings/') and p!='briefs/055/geometry.summary.json']
def check():
 changes=[p for p in LOCK if hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=PIN[p]]
 if changes: raise RuntimeError('PINNED INPUT CHANGED; stop, do not certify mixed inputs: '+str(changes))
def step(name,method,*args):
 check(); print('START',name,flush=True); t=time.time()
 c=['bun','briefs/075/oldbuild/swap/tools/unityrun.ts',str(L/(name+'.log')),'--','-executeMethod',method,*args]
 r=subprocess.run(c,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
 (L/(name+'.wrapper.log')).write_text(r.stdout)
 print('END',name,'exit',r.returncode,'seconds',round(time.time()-t,1),flush=True)
 state.append({'step':name,'command':c,'exit':r.returncode,'seconds':round(time.time()-t,2)});(D/'steps.json').write_text(json.dumps(state,indent=2))
 check()
 if r.returncode: raise RuntimeError('failed '+name+'; see '+str(L/(name+'.log')))
state=json.loads((D/'steps.json').read_text()) if (D/'steps.json').exists() else []; W=str(S/'world.json'); P=str(S/'plan.json'); R='Suborned.World.Review.Recovery075'; B='Suborned.World.HarbourWorldBuilder'; G='Suborned.World.Review.Glint075'; M='Suborned.World.Review.Restyle075'; SW='Suborned.OldTown.Editor.Swap.OldTownSwap'
def manifest(tag,scene):
 step(tag+'-manifest',R+'.Manifest','-recoveryScene',scene,'-recoveryTag',tag)
 step(tag+'-restyle-manifest',M+'.Manifest','-restyleManifest',scene+'=0='+str(D/(tag+'-restyle.json')))
def shoot(tag,scene):
 step(tag+'-shots',G+'.Cli','-scene',scene,'-variant','today','-mode','play','-nonight','-out',str(D/tag),'-spots','trough-detail:-179.5,-222.5,210,3;office-plant-detail:-171.7,-287,30,4')
try:
 # No running Editor/build observed before launch. No model/asset generators are started here.
 if not (ROOT/'unity/Assets/Scenes/probe-recovery075-before.unity').exists(): step('import',R+'.Import')
 before='Assets/Scenes/probe-recovery075-before.unity'; paving='Assets/Scenes/probe-oldtown-ours.unity'; candidate='Assets/Scenes/probe-recovery075-foliage3.unity'
 if not (ROOT/'unity'/before).exists(): step('before-build',B+'.Build','-world',W,'-scene',before,'-civicRestyle',str(S/'baseline-restyle.json'))
 if not (D/'before.json').exists(): manifest('before',before)
 if not (D/'before/today-day-cafe-square-y030.png').exists(): shoot('before',before)
 if not (D/'paving.json').exists(): step('paving-build',B+'.Build','-world',W,'-scene',paving)
 if not (D/'paving.json').exists(): manifest('paving',paving)
 if not (D/'paving/today-day-cafe-square-y030.png').exists(): shoot('paving',paving)
 step('foliage-apply',R+'.Apply','-world',W,'-recoveryFoliage','selective','-recoveryScene',paving,'-recoverySave',candidate)
 manifest('foliage',candidate);shoot('foliage',candidate)
 step('swap-build',SW+'.Build','-swapHeights','rule','-swapHq','ours','-swapScene','probe-recovery075-swap','-swapOurs',paving,'-swapWorld',W,'-swapPlan',P,'-swapOut',str(D/'swap'))
 step('swap-verify',SW+'.Verify','-swapScene','probe-recovery075-swap','-swapOurs',paving,'-swapOut',str(D/'swap'))
 step('swap-shots',SW+'.Shoot','-swapScene','probe-recovery075-swap','-swapWorld',W,'-swapOut',str(D/'swap'),'-swapShots','tram,vista,circus,district','-swapTag','provisional056')
 print('SETTLED CHAIN COMPLETE; G-3a STILL BLOCKED',flush=True)
except Exception as e:
 (D/'chain-error.txt').write_text(str(e)+'\n');print(str(e),flush=True);sys.exit(1)
