from pathlib import Path
import sys,json,subprocess,hashlib,time,re
R=Path.cwd();D=R/'briefs/075/recovery/tram-corrected';S=R/'out/scratch/latch/tram-corrected';sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
name,method=sys.argv[1:3];extras=sys.argv[3:];pins=json.load(open(D/'pins-before.json'))
def check():
 bad=[p for p,h in pins.items() if not p.startswith('unity/Assets/Suborned/Editor/OldTown/Swap/') and sha(R/p)!=h];assert not bad,bad
check();held={p:p.read_bytes() for p in (R/'unity/Assets/Scenes').rglob('*Baking Set.asset')};held.update({R/'unity/ProjectSettings'/n:(R/'unity/ProjectSettings'/n).read_bytes() for n in ['GraphicsSettings.asset','QualitySettings.asset']})
cmd=['bun','tools/game/playing.ts','unity','run',str(R/'unity'),'--timeout','590','--log-file',str(S/(name+'.log')),'--no-tail','--format','json','--','-executeMethod',method,*extras]
start=time.time();result=subprocess.run(cmd,capture_output=True,text=True)
restored=[]
for p,b in held.items():
 if p.read_bytes()!=b:p.write_bytes(b);restored.append(str(p.relative_to(R)))
(D/(name+'-cli.log')).write_text(result.stdout+result.stderr)
log=(S/(name+'.log')).read_text(errors='replace');log=re.sub(r'(?i)(-accessToken\s+)\S+',r'\1[redacted]',log);(D/(name+'-unity.log')).write_text(log)
(D/(name+'-result.json')).write_text(json.dumps({'command':cmd,'exit':result.returncode,'seconds':time.time()-start,'restoredExactBytes':restored},indent=2)+'\n');check();print(name,result.returncode,round(time.time()-start,2),flush=True)
if result.returncode:raise RuntimeError('Unity job failed; see retained diagnostic log')
