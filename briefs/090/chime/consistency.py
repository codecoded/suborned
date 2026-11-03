"""Brief 090 (Chime): how steady each voice is, from the speaker embeddings (embed.py): mean cosine of a set's clips to each other and to the person's reference, against the mean cosine between different people's references (the floor a 'different voice' sits at)."""
import json, itertools, sys
from pathlib import Path
import numpy as np
R = Path(__file__).resolve().parents[3] / 'out/scratch/chime'; e = json.loads((R / 'emb.json').read_text()); pick = json.loads((R / 'ref_pick.json').read_text())
refs = {Path(w).stem.split('_')[0]: np.array(v) for w, v in e['ref_all'].items()}
cos = lambda a, b: float(np.dot(a, b))
others = [cos(refs[a], refs[b]) for a, b in itertools.combinations(refs, 2)]
res = {'between_different_people_refs': {'mean': round(float(np.mean(others)), 3), 'max': round(float(np.max(others)), 3), 'max_pair': max(itertools.combinations(refs, 2), key=lambda p: cos(refs[p[0]], refs[p[1]]))}, 'sets': {}}
for k, ws in e.items():
    if k == 'ref_all': continue
    who = k.split('_')[1]; V = [np.array(v) for v in ws.values()]; pw = [cos(a, b) for a, b in itertools.combinations(V, 2)]; tr = [cos(v, refs[who]) for v in V]
    near = [max(refs, key=lambda r: cos(v, refs[r])) for v in V]
    res['sets'][k] = {'n': len(V), 'within_mean': round(float(np.mean(pw)), 3), 'within_min': round(float(np.min(pw)), 3), 'to_own_ref_mean': round(float(np.mean(tr)), 3), 'to_own_ref_min': round(float(np.min(tr)), 3), 'nearest_ref_is_own': f'{sum(n == who for n in near)}/{len(V)}'}
(Path(__file__).resolve().parents[1] / 'consistency.json').write_text(json.dumps(res, indent=1) + '\n'); print(json.dumps(res, indent=0))
