"""Loading-screen fixes, round 2 (brief 047, Sky 2026-10-23). python3 -I briefs/047/fixes/fix2.py <A|B|C|D> <variant> --take N [--seed S] [--refs a,b] [--note text]
Reference edit of the picked take with Seedream 5.0 Pro via Runware (as gen.py). Reference 1 is always the pick; optional reference 2 = the game's patrol robot render.
Every call is logged in briefs/047/PROVENANCE.json with phase 'fix'. Fix spend cap $5 (phase 'fix' only): refuses to start at >= $4.90."""
import base64, fcntl, hashlib, io, json, os, sys
from pathlib import Path
from PIL import Image
OLD = Path(os.environ.get('SUBORNED_OLD', '~/Projects/the-suborned')).expanduser() / 'workbench/Tools/Concept'
sys.path.insert(0, str(OLD))
from runware import infer  # noqa
HERE = Path(__file__).resolve().parent
B47 = HERE.parent
PROV = B47 / 'PROVENANCE.json'
MODEL = 'bytedance:seedream@5.0-pro'
CAP = 5.0
PICK = {'A': 'picks/A_pick_A_t7.png', 'B': 'picks/B_pick_B_t5.png', 'C': 'picks/C_pick_C_t7.png', 'D': 'picks/D_pick_D_t7.png'}
ROBOT_REF = 'refs/patrol_robot_game.png'

HEAD = ("Edit image 1, a finished piece of key art. Keep everything not mentioned below exactly as it is: composition, framing, camera angle, light, sky, architecture, palette, painted style, sharpness, "
        "and the four people's faces, hair, skin tones and costumes. Make only these changes. ")
ROBOT = ("The small patrol robot must be rebuilt to match image 2 (the game's robot): a cream barrel body with a charcoal chest plate and a coral band, an ivory box head with a seafoam top and a dot-matrix lamp face, "
         "and underneath a seafoam deck on two black rubber caterpillar treads, each with three chrome-hubbed wheels, in place of its single wheel; the same size and place in the picture. ")
CAMERA = ("Replace every security camera with a 1950s chrome closed-circuit camera: a polished streamlined chrome barrel with a deep flared sunshade hood and a finned chrome collar, on a swan-neck bracket on a slim chrome post, "
          "like a vintage studio television camera; no white or grey plastic box housing, no modern bullet-shaped CCTV. ")
KEEP = ("Do not add any object that is not in image 1: the number, size and position of the cameras, robots, cars and people stay exactly as in image 1; only their design changes. ")
CROWD = ("Every bystander wears sleek space-age couture and is bareheaded (cropped jackets, tunics, shift dresses with chrome belts, white boots, in seafoam, coral, navy and white); "
         "replace any hats, suits, ties, trench coats or 1950s dresses with that. ")
TAIL = "No text, lettering, logos or interface."

EDITS = {
 'A': {
  'face': ("The four people in the foreground all turn their heads and shoulders toward the viewer and look straight into the camera with a calm, confident, steady gaze, standing relaxed and balanced on the deck where they are now, "
           "bodies in three-quarter view from behind-left to front, the Enforcer turned most squarely to the lens. ", ROBOT + CAMERA + CROWD),
  'focus': ("The four people in the foreground all look together at one clear point in the city: the chrome atom crowning the tall tower at centre right. Heads raised slightly, faces in clean three-quarter profile toward it, "
            "standing where they are now, the Enforcer lifting one gloved hand to point at it. ", ROBOT + CAMERA + CROWD),
 },
 'B': {
  'walk2': ("Redraw the four mid-stride as a natural, confident walk, clearly different from the stiff marching of image 1. Every person's elbows are bent about 25 degrees, hands loose and softly curled (no splayed fingers, no stiff straight arms), "
            "each arm swings opposite to its leg: when the right leg steps forward the left arm swings forward and the right arm back. Shoulders relaxed and slightly rotating with each step, heads up and looking at the camera, weight rolling heel to toe, "
            "a slightly longer stride, the front knee soft, the back heel lifting. Each of the four is at a different point of the stride, so the group looks alive; the Enforcer a half step ahead with a broad, loose, easy swagger, his hands hanging relaxed at his thighs. "
            "Same arrow formation, same positions, scale, faces and costumes. ", ROBOT + CAMERA + CROWD),
  'walk': ("The four walk toward the camera in a natural, confident stride, as in a film walk cycle: heads up, looking at the camera, relaxed shoulders, arms swinging loosely and opposite to the legs, hands open and easy, "
           "one heel landing in front with the weight rolling forward, the back leg pushing off, knees bending naturally; different leg phases for each person; the Enforcer a half step ahead with a broad easy swagger. "
           "Same arrow formation, same positions and scale. ", ROBOT + CAMERA + CROWD),
 },
 'C': {
  'rest': ("The Enforcer (the grey-bearded man at the far right) now stands in a grounded parade-rest stance: feet planted flat about shoulder width apart, weight even on both legs, legs straight and square, hands clasped behind his back, "
           "chest open, shoulders level and relaxed, head level, looking out over the city with calm authority; he turns his body three-quarters toward the city (to the left of the picture). No hunch, no twist, no crossed legs, no hands spread. The other three keep their places and poses. ", ROBOT + CAMERA + CROWD),
  'stance': ("The Enforcer (the grey-bearded man at the far right) stands upright and grounded: feet flat and about shoulder width apart, weight even on both legs, hips and shoulders square and relaxed, chest open, "
             "one arm hanging at his side and the other hand resting on the chrome railing, head level, looking out over the district with calm authority. No hunch, no twist, no crossed or locked legs. The other three keep their places and poses. ", ROBOT + CAMERA + CROWD),
 },
 'D': {
  'cam': ("Only one change: the security camera on the tall lamp post at the far left. Redesign that camera only, at exactly the same small size, position and angle as now. Nothing else in the picture changes at all. ", CAMERA),
  'stand': ("Remove the cafe table and chair completely: nothing is left there but planter wall, flowers and paving. "
            "The Scout (far left) stands upright with his back and shoulder resting firmly against the solid chrome lamp post and cream stone planter wall beside him, arms folded, looking toward the Records Office door; he is clearly supported by it. "
            "The Fixer (woman in the pinstripe dress) now stands on her feet next to the Tech and the Enforcer, joining the group, upright and poised, holding a small coffee cup, looking at the door. "
            "The Tech still crouches on the planter edge and the Enforcer still stands at the lamp post. The four read as a team waiting together. ", ROBOT + CAMERA + CROWD),
 },
}

def spent():
    return sum(e.get('cost_usd') or 0 for e in json.loads(PROV.read_text()) if e.get('phase') == 'fix')

def run(comp, variant, take, seed=None, note='', use_robot=True, extra=''):
    if spent() >= CAP - 0.10:
        raise SystemExit(f'fix spend cap reached: {spent():.4f}')
    change, rest = EDITS[comp][variant]
    refs = [PICK[comp]] + ([ROBOT_REF] if use_robot else [])
    uris = []
    for r in refs:
        b = io.BytesIO(); Image.open(B47 / r).convert('RGB').save(b, 'JPEG', quality=93)
        uris.append('data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode())
    p = HEAD + change + (rest if use_robot else rest.replace(ROBOT, '')) + KEEP + extra + TAIL
    kw = {'seed': seed} if seed else {}
    res = infer(MODEL, p, 2560, 1440, n=1, inputs={'referenceImages': uris}, **kw)
    for png, cost, sd in res:
        name = f'{comp}_{variant}_f{take}.png'; (HERE / name).write_bytes(png)
        lk = open(B47 / '.lock', 'w'); fcntl.flock(lk, fcntl.LOCK_EX)
        old = json.loads(PROV.read_text())
        old.append({'file': f'fixes/{name}', 'phase': 'fix', 'composition': comp, 'variant': variant, 'service': 'Runware', 'model': MODEL, 'seed': sd, 'cost_usd': cost, 'size': '2560x1440',
                    'sha256': hashlib.sha256(png).hexdigest(), 'refs': refs, 'note': note, 'prompt': p})
        PROV.write_text(json.dumps(old, indent=1)); fcntl.flock(lk, fcntl.LOCK_UN); print(name, cost, 'fix total', round(spent(), 4))

if __name__ == '__main__':
    a = sys.argv[1:]; comp, variant = a[0], a[1]
    g = lambda k, d=None: a[a.index(k) + 1] if k in a else d
    run(comp, variant, g('--take', '1'), int(g('--seed')) if g('--seed') else None, g('--note', ''), '--norobot' not in a, g('--extra', ''))
