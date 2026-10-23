"""Loading-screen fixes (brief 047, Sky 2026-10-23). python3 -I briefs/047/fixes/fix.py <A|B|C|D> --take N [--seed S] [--variant key] [--note text]
Edits the picked take with Seedream 5.0 Pro (picked image = reference 1, the cell lineup = reference 2, the game's patrol robot render = reference 3).
Every call is logged in briefs/047/PROVENANCE.json with phase 'fix'. Fix spend cap $5: refuses to start at >= $4.95 of phase-fix spend."""
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
PICK = {'A': ('picks/A_pick_A_t7.png', 1343806603), 'B': ('picks/B_pick_B_t5.png', 1356647389),
        'C': ('picks/C_pick_C_t7.png', 1842980866), 'D': ('picks/D_pick_D_t7.png', 940968324)}

HEAD = ("Revise reference image 1, a finished piece of key art. Keep it exactly as it is: the same composition and framing, camera angle, lighting, sky, architecture, palette, painted cinematic style and image sharpness, "
        "and the same four people in the same places, with the same faces, hair, skin tones and costumes (reference image 2 shows the four on a plain ground: Scout, Fixer, Tech, Enforcer, left to right in the lineup). "
        "Change ONLY the following. ")
ROBOT = ("The clockwork patrol robot must match reference image 3 exactly in design: an ivory box head with a seafoam brow plate, a row of five small amber lamps on its roof and a thin aerial, "
         "a flat dot-matrix display face of tiny round lamps (no round eyes, no mouth lips, nothing cartoonish), a cream barrel body with a charcoal chest plate with a small brass badge and a coral sash, short jointed chrome arms, "
         "and underneath a seafoam deck on two rubber tracked treads (a caterpillar track each side with three chrome-hubbed wheels), not a single wheel and not a unicycle. ")
CAMERA = ("Every security camera must be a 1950s-styled closed-circuit camera, not a modern box CCTV: a round streamlined chrome barrel with a deep sunshade hood over the lens and a chrome collar, "
          "mounted on a slim chrome post with a curved bracket, like a vintage studio television camera in polished chrome, no white plastic, no box housing. ")
CROWD = ("Every bystander in the street wears sleek space-age couture and is bareheaded: cropped jackets, tunics, shift dresses with chrome belts and white boots in seafoam, coral, navy and white; "
         "no hats, no suits, no ties, no 1950s dresses, no trench coats. ")
TAIL = "Do not add text, lettering, logos or interface. No other changes."

EDITS = {
 'A': {
  'face': ("The four of the cell all turn to face the viewer: each looks straight into the camera with a calm, confident, steady gaze, heads turned toward the lens (the Scout, the Fixer and the Tech turn their heads and shoulders three-quarters toward the viewer, "
           "the Enforcer turns to face the camera squarely, shoulders open), standing balanced and relaxed on the deck. Their positions on the deck stay the same. ",
           "A_face"),
  'focus': ("The four of the cell share one clear focus: all four look together at the chrome atom crowning the tall SpaceHQ tower at centre right, in the same direction, heads raised slightly, "
            "the Enforcer lifting one gloved hand to point out at it, the Fixer's and the Scout's faces in clean three-quarter profile toward it, the Tech beside them with the same gaze. Their positions on the deck stay the same. ",
            "A_focus"),
  'extra': ROBOT + CAMERA + CROWD,
 },
 'B': {
  'walk': ("The four walk toward the camera with a natural, confident stride, mid-step: relaxed shoulders, heads up and looking ahead at the camera, arms swinging naturally and loosely opposite to the legs, "
           "easy loose hands (no stiff straight arms, no fists), feet landing heel first with the weight moving forward, knees bending naturally, the bodies lightly rotating with each step, "
           "the Enforcer a half step ahead with a broad, easy swagger, the Scout and the Fixer in step with different leg phases so the group looks alive, the Tech a little back. "
           "Keep their faces and costumes, and the arrow formation. ",
           "B_walk"),
  'extra': ROBOT + CAMERA + CROWD,
 },
 'C': {
  'stance': ("Fix the Enforcer's stance: he stands upright and grounded, feet about shoulder width apart and flat on the roof with his weight evenly on both legs, hips and shoulders square and relaxed, "
             "chest open, one arm hanging loose at his side and the other hand resting lightly on the chrome railing, head level, looking out over the district with calm authority. "
             "No hunch, no twisted torso, no crossed or locked legs. The Scout, the Fixer and the Tech keep their places and poses. ",
             "C_stance"),
  'extra': ROBOT + CAMERA + CROWD,
 },
 'D': {
  'stand': ("Change the cell's poses so each has a reason. Remove the cafe table and the chair completely (nothing is left on that spot but the planter wall and paving). "
            "The Scout stands upright with the cell, a hip and one shoulder resting firmly against the solid cream stone planter wall beside the chrome lamp post, arms folded, looking toward the Records Office door; the wall is clearly solid and under their weight. "
            "The Fixer stands on her feet with the cell, upright and poised, one gloved hand holding a small coffee cup, eyes on the door and the waitress, ready to act. "
            "The Tech crouches by the planter as before. The Enforcer stands in the cypress shade as before, balanced and grounded. The four read as a team waiting together for the moment. ",
            "D_stand"),
  'extra': ROBOT + CAMERA + CROWD,
 },
}

def spent():
    return sum(e.get('cost_usd') or 0 for e in json.loads(PROV.read_text()) if e.get('phase') == 'fix')

def build(comp, variant):
    spec = EDITS[comp]
    return HEAD + spec[variant][0] + spec['extra'] + TAIL

def run(comp, take, variant, seed=None, note='', add=''):
    if spent() >= CAP - 0.05:
        raise SystemExit(f'fix spend cap reached: {spent():.4f}')
    pick, pseed = PICK[comp]
    refs = [pick, 'refs/cell_lineup.png', 'refs/patrol_robot_game.png']
    uris = []
    for r in refs:
        b = io.BytesIO(); Image.open(B47 / r).convert('RGB').save(b, 'JPEG', quality=93)
        uris.append('data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode())
    p = build(comp, variant) + (' ' + add if add else '')
    res = infer(MODEL, p, 2560, 1440, n=1, inputs={'referenceImages': uris}, seed=seed or pseed)
    for png, cost, sd in res:
        name = f'{comp}_{variant}_f{take}.png'; (HERE / name).write_bytes(png)
        lk = open(B47 / '.lock', 'w'); fcntl.flock(lk, fcntl.LOCK_EX)
        old = json.loads(PROV.read_text())
        old.append({'file': f'fixes/{name}', 'phase': 'fix', 'composition': comp, 'variant': variant, 'service': 'Runware', 'model': MODEL, 'seed': sd, 'cost_usd': cost, 'size': '2560x1440',
                    'sha256': hashlib.sha256(png).hexdigest(), 'refs': [f'{r}' for r in refs], 'note': note, 'prompt': p})
        PROV.write_text(json.dumps(old, indent=1)); fcntl.flock(lk, fcntl.LOCK_UN); print(name, cost, 'fix total', round(spent(), 4))

if __name__ == '__main__':
    a = sys.argv[1:]; comp = a[0]
    g = lambda k, d=None: a[a.index(k) + 1] if k in a else d
    run(comp, g('--take', '1'), g('--variant'), int(g('--seed')) if g('--seed') else None, g('--note', ''), g('--add', ''))
