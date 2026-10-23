"""Loading-screen fixes, round 3: chained single-purpose edits (brief 047, Sky 2026-10-23).
python3 -I briefs/047/fixes/fix3.py <out_name> <src image, relative to briefs/047> "<one change>" [--robot] [--seed S] [--note text]
Source image = reference 1 (the previous step); the game's patrol robot render = reference 2 with --robot. One change per call keeps the rest of the picture as it was (round 2 learned that
multi-change prompts redraw the whole frame). Logged in PROVENANCE.json, phase 'fix'; cap $5 (refuses at >= $4.90)."""
import base64, fcntl, hashlib, io, json, os, sys
from pathlib import Path
from PIL import Image
OLD = Path(os.environ.get('SUBORNED_OLD', '~/Projects/the-suborned')).expanduser() / 'workbench/Tools/Concept'
sys.path.insert(0, str(OLD))
from runware import infer  # noqa
HERE = Path(__file__).resolve().parent; B47 = HERE.parent; PROV = B47 / 'PROVENANCE.json'
MODEL = 'bytedance:seedream@5.0-pro'; CAP = 5.0
HEAD = ("Edit image 1, a finished piece of key art. Make exactly one change, described below, and keep everything else identical: same composition, framing, camera angle, light, sky, architecture, props, "
        "the people's faces, hair, skin tones and costumes, and the same image sharpness. Add no new object, and change no object's size or place unless told. The change: ")
TAIL = " No text, lettering, logos or interface."
def spent(): return sum(e.get('cost_usd') or 0 for e in json.loads(PROV.read_text()) if e.get('phase') == 'fix')
a = sys.argv[1:]; name, src, change = a[0], a[1], a[2]
g = lambda k, d=None: a[a.index(k) + 1] if k in a else d
if spent() >= CAP - 0.10: raise SystemExit(f'fix spend cap reached: {spent():.4f}')
refs = [src] + (['refs/patrol_robot_game.png'] if '--robot' in a else [])
uris = []
for r in refs:
    b = io.BytesIO(); Image.open(B47 / r).convert('RGB').save(b, 'JPEG', quality=93); uris.append('data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode())
p = HEAD + change + TAIL
kw = {'seed': int(g('--seed'))} if g('--seed') else {}
for png, cost, sd in infer(MODEL, p, 2560, 1440, n=1, inputs={'referenceImages': uris}, **kw):
    (HERE / f'{name}.png').write_bytes(png)
    lk = open(B47 / '.lock', 'w'); fcntl.flock(lk, fcntl.LOCK_EX); old = json.loads(PROV.read_text())
    old.append({'file': f'fixes/{name}.png', 'phase': 'fix', 'service': 'Runware', 'model': MODEL, 'seed': sd, 'cost_usd': cost, 'size': '2560x1440', 'sha256': hashlib.sha256(png).hexdigest(),
                'refs': refs, 'note': g('--note', ''), 'prompt': p}); PROV.write_text(json.dumps(old, indent=1)); fcntl.flock(lk, fcntl.LOCK_UN)
    print(name, cost, 'fix total', round(spent(), 4))
