"""Loading-screen key art (brief 047; Sky, 2026-10-22). python3 -I briefs/047/gen.py <A|B|C|D> [--take N] [--size 2560x1440] [--seed S] [--note text]
Seedream 5.0 Pro via the old store's Runware client (as tools/figures/portraits_gen.py). Every call is logged to briefs/047/PROVENANCE.json
(prompt, refs, seed, cost, sha256). Hard spend cap: refuses to start a call when the logged spend is >= CAP - 0.05."""
import base64, fcntl, hashlib, io, json, os, sys
from pathlib import Path
from PIL import Image
OLD = Path(os.environ.get('SUBORNED_OLD', '~/Projects/the-suborned')).expanduser() / 'workbench/Tools/Concept'
sys.path.insert(0, str(OLD))
from runware import infer  # noqa
HERE = Path(__file__).resolve().parent
PROV = HERE / 'PROVENANCE.json'
MODEL = 'bytedance:seedream@5.0-pro'
CAP = 5.0

CAST = ("The four people in reference image 1 are the player's cell, left to right: the Scout (lean young person, black undercut, earpiece, ivory hooded jacket, black harness straps, black shorts, white sneakers), "
 "the Fixer (woman, black ponytail, ivory and black pinstripe halter dress, long ivory gloves, ivory boots), "
 "the Tech (muscular man, tied dreadlocks, chrome goggles pushed up on his forehead, short beard, black wrap top with ivory sash and cyan piping, wide black trousers, chrome bracers), "
 "the Enforcer (big older man, short grey hair, full grey beard, tinted sunglasses, black and ivory panelled coat, chrome collar, white boots). "
 "Keep their faces, hair, skin tones, outfits and colours exactly as in the reference. Sharp matte black, ivory and chrome, standing out against the pastel city. ")
CITY = ("Streamline Moderne and Googie city: cream limestone, seafoam-teal enamel bands, chrome spheres, banners, cypress and palms, coral accents, pale paving with seafoam inlays. "
 "An alternate 1950s world with atomic power centuries ahead: 1950s-styled trams and robots, no screens. All citizens are bareheaded and wear sleek tailored space-age couture (cropped jackets, tunics, shift dresses with chrome belts) in seafoam, coral, navy and white; no hats, no suits, no 1950s dresses. ")
UNDER = ("Quiet undertone: a 1950s-styled closed-circuit camera in a chrome hood on a slim chrome post, a clockwork patrol robot (barrel body of riveted chrome on one wheel, its face a flat panel of tiny round dot-matrix lamps, no round eyes, no mouth, nothing cartoonish), a councillor's long black-and-ivory streamlined car at the kerb. ")
LOOK = ("Cinematic painted concept art, photographic material realism, gouache softness, premium video game key art, breathtaking atmosphere. "
 "No text, no lettering, no logos, no watermark, no interface elements. No gold surfaces, no glass curtain walls, no grime.")
SPACE = ("Composition keeps a calm, empty, uncluttered area free for later overlay: ")

REFS = {  # first ref is always the cell lineup
 'A': ['cell_lineup.png', 'city_hero.jpg', 'records_office.jpg', 'exposition_palace.jpg'],
 'B': ['cell_lineup.png', 'inez_cutaway.jpg', 'records_office.jpg', 'power_spire.jpg'],
 'C': ['cell_lineup.png', 'mission_block.jpg', 'blocks_saucer_spires.jpg', 'city_hero.jpg', 'exposition_palace.jpg'],
 'D': ['cell_lineup.png', 'records_office.jpg', 'inez_cutaway.jpg', 'mission_block.jpg'],
}
SCENES = {
 'A': ("Wide cinematic hero shot. The four of the cell stand together on a high terraced deck above a great plaza, in three-quarter back view looking out over the Harbour District, "
  "in the lower left third, each a third of the frame tall, faces turned so profiles read. "
  "The district falls away in cream terraces with seafoam bands: the Records Office with its chrome globe below, a coral sky rail tram on slim arches, lagoon-blue harbour, "
  "fluted cream towers, saucer-topped spires, and SpaceHQ crowned with a chrome atom over a needle mast, tallest at centre right. Shift-change dusk: low warm sun, peach and lilac sky, cream haze, long soft shadows, first lamps glowing. ",
  SPACE + "upper left is open soft sky and haze; the lowest tenth of the image is plain paving."),
 'B': ("Low street-level hero shot at the corner café beside the Records Office. Camera near the ground looking slightly up; the four of the cell walk toward the camera in an arrow formation, knees up, large and sharp, "
  "the Enforcer a step ahead, the Scout a step behind, calm and purposeful. Behind them in soft focus: the café's curved glass front with seafoam striped parasols and seated citizens, the Records Office entrance and banner, a coral tram, chrome bollards, red flower planters, palms. "
  "Golden hour, low sun from behind the camera warming cream stone, long soft shadows toward the camera, haze and bloom. The camera post, robot and car are small in the background. ",
  SPACE + "upper right is open pale sky and soft facade; the lowest tenth of the image is plain ground."),
 'C': ("Wide aerial establishing shot of the Harbour District from high above the quay, looking inland at dusk. Terraced cream-and-seafoam blocks, a skyport, a coral sky rail threading between towers, lagoon harbour with a white atomic liner, "
  "fluted towers, saucer spires, SpaceHQ crowned with a chrome atom over a needle mast, tallest at centre. In the lower right foreground, on a cream rooftop corner with chrome railing, the four of the cell stand tiny, "
  "each a twentieth of the frame tall, a readable cluster of black, ivory and chrome figures looking down at the Records Office far below. "
  "Low golden sun raking the towers, long soft shadows, warm haze layers, peach and lilac sky, distant faint rocket trails. ",
  SPACE + "upper left is open hazy sky; the lowest tenth of the image is plain ground."),
 'D': ("The moment before the Handover, outside the Civic Records Office, wide across the civic square. The Records Office at centre right: cream limestone, seafoam bands, tall fluted entrance with a chrome globe, teal banners, a canopy, a pale slate-blue uniformed guard at the door. "
  "A waitress in a coral dress with a tray and cup stands by the kerb about to stumble; the councillor's car waits with its driver. "
  "The four of the cell are spread in the left foreground waiting unnoticed: the Scout leaning on a chrome bollard, the Fixer at a café parasol table, the Tech crouched by a planter, the Enforcer still in the shade of a cypress; each a fifth of the frame tall, faces clear. "
  "Late afternoon shift-change light from the left, long soft shadows on pale paving, thin haze, tense hush under the pastel calm, SpaceHQ's chrome atom crown behind. ",
  SPACE + "upper left is open soft sky; the lowest tenth of the image is plain paving."),
}

def spent():
    return sum(e.get('cost_usd') or 0 for e in json.loads(PROV.read_text())) if PROV.exists() else 0.0

def build(comp, add=''):
    scene, space = SCENES[comp]
    return ' '.join([scene, add, CAST, CITY, UNDER, LOOK, space]).replace('  ', ' ')

def run(comp, take, size, seed=None, note='', add=''):
    if spent() >= CAP - 0.05:
        raise SystemExit(f'spend cap reached: {spent():.4f}')
    w, h = map(int, size.split('x'))
    refs = REFS[comp]; uris = []
    for r in refs:
        b = io.BytesIO(); Image.open(HERE / 'refs' / r).convert('RGB').save(b, 'JPEG', quality=92)
        uris.append('data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode())
    p = build(comp, add)
    extra = {'seed': seed} if seed else {}
    res = infer(MODEL, p, w, h, n=1, inputs={'referenceImages': uris}, **extra)
    for png, cost, sd in res:
        name = f'{comp}_t{take}.png'; (HERE / 'takes' / name).write_bytes(png)
        lk = open(HERE / '.lock', 'w'); fcntl.flock(lk, fcntl.LOCK_EX)
        old = json.loads(PROV.read_text()) if PROV.exists() else []
        old.append({'file': f'takes/{name}', 'composition': comp, 'service': 'Runware', 'model': MODEL, 'seed': sd, 'cost_usd': cost, 'size': size,
                    'sha256': hashlib.sha256(png).hexdigest(), 'refs': [f'refs/{r}' for r in refs], 'note': note, 'prompt': p})
        PROV.write_text(json.dumps(old, indent=1)); fcntl.flock(lk, fcntl.LOCK_UN); print(name, cost, 'total', round(spent(), 4))

if __name__ == '__main__':
    a = sys.argv[1:]; comp = a[0]
    g = lambda k, d=None: a[a.index(k) + 1] if k in a else d
    run(comp, g('--take', '1'), g('--size', '2560x1440'), int(g('--seed')) if g('--seed') else None, g('--note', ''), g('--add', ''))
