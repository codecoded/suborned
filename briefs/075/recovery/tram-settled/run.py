"""Single settled Unity correction/capture invocation. Run from repo root."""
from pathlib import Path
import hashlib,json,subprocess,time,sys,shutil
R=Path.cwd();D=R/'briefs/075/recovery/tram-settled';S=R/'out/scratch/latch/tram-settled'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
pins=json.loads((D/'pins-before.json').read_text())
def check():
 bad=[p for p,h in pins.items() if not p.startswith('unity/Assets/Suborned/Editor/OldTown/Swap/') and sha(R/p)!=h]
 if bad:raise RuntimeError('protected input drift: '+str(bad))
check()
if (D/'unity-invocation.json').exists():
 if '--resume-domain-preflight' not in sys.argv:raise RuntimeError('invocation exists; no automatic retry')
 prior=json.loads((D/'unity-result.json').read_text())
 if (D/'complete.json').exists() or not any('tram mesh outside original 2m source domain' in e for e in prior['errors']):raise RuntimeError('resume only the aborted domain preflight, never a completed correction')
 archive=D/'aborted-domain-preflight';archive.mkdir(exist_ok=False)
 for name in ['unity-invocation.json','unity-result.json','unity-cli.log','audit-before.json']:
  shutil.copy2(D/name,archive/name)
 shutil.copy2(S/'unity.log',archive/'unity.log')
cmd=['bun','tools/game/playing.ts','unity','run',str(R/'unity'),'--timeout','600','--log-file',str(S/'unity.log'),'--no-tail','--format','json','--','-executeMethod','Suborned.OldTown.Editor.Swap.SwapTramSettled.Run']
(D/'unity-invocation.json').write_text(json.dumps({'command':cmd,'started':time.time()},indent=2)+'\n')
settings=['unity/ProjectSettings/QualitySettings.asset','unity/ProjectSettings/GraphicsSettings.asset'];held={p:(R/p).read_bytes() for p in settings}
start=time.time();result=subprocess.run(cmd,cwd=R,capture_output=True,text=True)
(D/'unity-cli.log').write_text(result.stdout+result.stderr)
# Existing frame helper's explicit settings-byte preservation, scoped to these two settings files.
restored=[]
for p,b in held.items():
 if (R/p).read_bytes()!=b:(R/p).write_bytes(b);restored.append(p)
log=(S/'unity.log').read_text(errors='replace') if (S/'unity.log').exists() else ''
(D/'unity-result.json').write_text(json.dumps({'exit':result.returncode,'seconds':time.time()-start,'settingsBytesRestored':restored,'errors':[line for line in log.splitlines() if any(s in line for s in ['Exception','error CS','[TramSettled]','source tram joins:','tram crossing ownership:','tram surface ownership:','tram bed audit:'])]},indent=2)+'\n')
check();(D/'pins-after.json').write_text(json.dumps({p:sha(R/p) for p in pins},indent=2)+'\n')
print(result.stdout+result.stderr);print('Unity exit',result.returncode,'seconds',round(time.time()-start),flush=True)
if result.returncode:raise RuntimeError('Unity failed; see retained logs, no automatic retry')
if not (D/'complete.json').exists():raise RuntimeError('Unity did not produce completion evidence')
# External encoding cannot leave a live process or pipe inside Unity during shutdown.
for frames in sorted((D/'after').glob('*-motion')):
 target=frames.with_suffix('.mp4')
 subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','12','-i',str(frames/'%04d.png'),'-c:v','libx264','-crf','16','-pix_fmt','yuv420p','-colorspace','bt709','-color_primaries','bt709','-color_trc','bt709',str(target)],check=True)
print('Encoded five targeted 3-second / 36-frame motion clips at 1280x720, 12fps',flush=True)
