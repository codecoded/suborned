"""Brief 065 capture clip (Forma, 2026-10-25): one H3 Fast first-frame take through the paid helper.
  python3 -I briefs/065/gen.py <keyframe.png> --take N [--dur 10] [--res 768p]"""
import base64, io, sys
from pathlib import Path
from PIL import Image
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / 'tools/paid'))
import paid
PROMPT = ("Locked-off static high-corner security-camera shot: the camera is fixed on a tripod and never pans, tilts or zooms, no cuts. A silent abduction, not a hug. "
          "The Enforcer, a big silver-bearded man in black and ivory, walks quickly to the seated man in the ivory tunic, who is standing up from the desk. "
          "The Enforcer grabs Vale from the side, locks one arm across his chest and presses a gloved hand to his neck; Vale struggles for one second, his pen drops, then his body goes completely limp and his head drops. "
          "The Enforcer catches the limp, unconscious Vale, bends, and heaves him over his shoulder in a fireman's carry, Vale's arms and legs hanging loose. "
          "The Enforcer straightens up, standing, holding Vale on his shoulder, and strides away toward the open door at the left. Quiet room, footsteps, a dropped pen clatters.")
a = sys.argv[1:]; g = lambda k, d=None: a[a.index(k) + 1] if k in a else d
key = Path(a[0]); take = g('--take', '1'); dur = int(g('--dur', 10)); res = g('--res', '768p')
im = Image.open(key).convert('RGB').resize((1344, 768), Image.LANCZOS)
b = io.BytesIO(); im.save(b, 'JPEG', quality=95)
uri = 'data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode()
p = paid.Pass('065-capture-pip', '1')
task = {'model': 'minimax:h3@fast', 'positivePrompt': PROMPT, 'duration': dur, 'resolution': res,
        'inputs': {'frameImages': [{'image': uri, 'frame': 'first'}]}}
p.video(task, name=f'capture_h3fast_t{take}.mp4', subdir='shots', est=0.08 * dur, prompt=PROMPT, duration_s=dur,
        refs_log=[str(key)], params={'mode': 'first-frame', 'resolution': res, 'keyframe': str(key), 'audio': 'native'})
