import json,hashlib
from pathlib import Path
D=Path(__file__).resolve().parent
b=json.loads((D/'manifest-before.json').read_text()); a=json.loads((D/'manifest-after.json').read_text())
br={r['path']:r for r in b['renderers']}; ar={r['path']:r for r in a['renderers']}
changed=[k for k in br if k in ar and br[k]!=ar[k]]
r={'collidersIdentical':b['colliders']==a['colliders'],'navIdentical':b['nav']==a['nav'],'transformsIdentical':b['transforms']==a['transforms'],'rendererPathsIdentical':br.keys()==ar.keys(),'changedRenderers':changed,'changedDetails':[{ 'path':k,'before':br[k],'after':ar[k]} for k in changed], 'inputsIdentical':json.loads((D/'inputs-before.json').read_text())==json.loads((D/'inputs-after.json').read_text())}
(D/'comparison.json').write_text(json.dumps(r,indent=2)); print({k:v for k,v in r.items() if k!='changedDetails'})
assert all(r[k] for k in ('collidersIdentical','navIdentical','transformsIdentical','rendererPathsIdentical','inputsIdentical'))
