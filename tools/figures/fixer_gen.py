"""The Fixer, P4-6, as a real 3D model (brief 020; Sky, 2026-10-18: "that fixer is the waitress").
python3 -I tools/figures/fixer_gen.py sheet [--n 2] [--take 1]       # P4-6 pick -> front A-pose model sheet(s), Seedream with the pick as reference
python3 -I tools/figures/fixer_gen.py mesh <sheet-file> <variant>    # sheet -> image-to-3D (variants: trellis, tripo_h3 (Tripo 3.1), hunyuan_pro, hunyuan_rapid)
Uses the store's Runware client (the-suborned/workbench/Tools/Concept/runware.py reads its own key; nothing here prints it). Every output is
saved in the store and logged with service, cost and sha256 in the PROVENANCE.json that sits beside its siblings:
  concepts/Citizens/model_sheets/fixer_P4-6__apose_<take>_<i>.png  -> concepts/Citizens/PROVENANCE.json
  source-art/Citizens3D/bench/fixer_P4-6__<variant>.glb            -> source-art/Citizens3D/PROVENANCE.json
Spend is added up in the store at characters/fixer_P4-6/spend.json."""
import base64, hashlib, io, json, os, sys, time, uuid
from pathlib import Path
from PIL import Image

OLD = Path(os.environ.get('SUBORNED_OLD', '~/Projects/the-suborned')).expanduser() / 'workbench/Tools/Concept'
sys.path.insert(0, str(OLD))
from runware import call, fetch, infer   # noqa: E402

STORE = Path(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store')).expanduser()
PICK = STORE / 'concepts/Cell/picks/fixer_P4-6.png'
LINEUP = Path(__file__).resolve().parents[2] / 'briefs/020/people/cell-lineup-latest.png'   # Sky, 2026-10-18: "this is our latest cell"
LINEUP_BOX = (430, 95, 720, 615)   # the Fixer, from behind, in that lineup
SHEETS = STORE / 'concepts/Citizens/model_sheets'
BENCH = STORE / 'source-art/Citizens3D/bench'
SPEND = STORE / 'characters/fixer_P4-6/spend.json'
MODEL = 'bytedance:seedream@5.0-pro'
sha = lambda b: hashlib.sha256(b).hexdigest()

PROMPT = ("Character model sheet for 3D modelling of the character in the reference image, who is seen from behind: the same woman with exactly the "
          "same outfit, cut, colours, materials and trims, now shown from the FRONT. The outfit: a fitted dress of vertical panels in ivory and black, "
          "with fine pinstripe panels, a high halter neckline with bare shoulders, long fitted ivory sleeves worn from the shoulder to the wrist as separate sleeves, "
          "the dress hem cut shorter than in the reference to sit a clear hand's width above the kneecap, so the knees show, straight and clean, nothing flowing or trailing below it; "
          "a high ponytail of long straight black hair; two-tone ivory and black heeled ankle boots with a chrome heel accent. The briefcase is removed: nothing is held or attached. "
          "Full body from head to toe, centred, front view, standing in a strict A-pose: both arms straight and held 45 degrees out from the body, "
          "a clear gap of air between each arm and the torso and between each hand and the hips, palms facing the thighs, fingers relaxed; legs straight with the feet "
          "shoulder-width apart so the thighs do not touch. Neutral expression. Even soft shadowless studio light, no cast shadows, no floor, plain background. "
          "The same rendering style as the reference image, crisp edges. Original design, not resembling any real person or film character. "
          "No text, no letters, no numbers, no logos, no watermark.")

PROMPT_AUBERGINE = PROMPT.replace('ivory and black', 'ivory and deep aubergine').replace('a clear hand\'s width above the kneecap, so the knees show, straight and clean', 'a hand\'s width above the knee, straight and clean').replace('two-tone ivory and black heeled ankle boots with a chrome heel accent', 'two-tone ivory and aubergine heeled ankle boots')   # take 1 (the P4-6 pick as reference)

MESH = {
    'trellis': ('microsoft:trellis-2@4b', 'image', {'resolution': 1024, 'textureSize': 2048, 'decimationTarget': 50000, 'textureFormat': 'PNG'}),
    'tripo_h3': ('tripo:v3.1@0', 'images', {'texture': True, 'pbr': True, 'faceLimit': 30000, 'geometryQuality': 'detailed', 'textureQuality': 'detailed', 'orientation': 'default'}),
    'hunyuan_pro': ('tencent:hunyuan-3d@3.1-pro', 'images', {'generateType': 'Normal', 'pbr': True, 'faceCount': 50000}),
    'hunyuan_rapid': ('tencent:hunyuan-3d@3.1-rapid', 'image', {'pbr': True}),
}


def uri(img):
    b = io.BytesIO(); img.save(b, 'PNG'); return 'data:image/png;base64,' + base64.b64encode(b.getvalue()).decode()


def add_spend(kind, what, cost):
    SPEND.parent.mkdir(parents=True, exist_ok=True)
    rows = json.loads(SPEND.read_text()) if SPEND.exists() else []
    rows.append({'at': time.strftime('%Y-%m-%d %H:%M'), 'kind': kind, 'what': what, 'usd': cost})
    SPEND.write_text(json.dumps(rows, indent=1))
    print(f"[fixer] +${cost}  total ${sum(r['usd'] or 0 for r in rows):.4f}")


def log(prov_path, row):
    old = json.loads(prov_path.read_text()) if prov_path.exists() else []
    old = [r for r in old if r['file'] != row['file']]
    prov_path.write_text(json.dumps(old + [row], indent=1))


def sheet(n, take, use='lineup'):
    if use == 'lineup':
        ref = Image.open(LINEUP).convert('RGBA').crop(LINEUP_BOX)
        rp = SHEETS / 'refs/fixer_P4-6_lineup.png'; rp.parent.mkdir(parents=True, exist_ok=True); ref.save(rp)
        log(SHEETS.parent / 'PROVENANCE.json', {'file': 'model_sheets/refs/fixer_P4-6_lineup.png', 'model': 'crop', 'cost': 0, 'sha256': sha(rp.read_bytes()),
                                               'from': 'briefs/020/people/cell-lineup-latest.png (Sky, 2026-10-18), box %s' % (LINEUP_BOX,)})
    else: ref = Image.open(PICK).convert('RGBA')
    ref.putpixel((0, 0), (0, 0, 0, 0))   # a transparent pixel in the reference asks for a transparent output
    res = infer(MODEL, PROMPT, 1024, 2048, n=n, inputs={'referenceImages': [uri(ref)]}, settings={'background': 'transparent'})
    for i, (png, cost, seed) in enumerate(res):
        name = f'fixer_P4-6__apose_{take}_{i}.png'; p = SHEETS / name
        p.write_bytes(png)
        im = Image.open(p)
        log(SHEETS.parent / 'PROVENANCE.json', {'file': f'model_sheets/{name}', 'model': MODEL, 'seed': seed, 'cost': cost, 'size': f'{im.size[0]}x{im.size[1]}',
                                                 'sha256': sha(png), 'reference': 'model_sheets/refs/fixer_P4-6_lineup.png' if use == 'lineup' else 'Cell/picks/fixer_P4-6.png', 'prompt': PROMPT if use == 'lineup' else PROMPT_AUBERGINE})
        add_spend('image', name, cost)
        print('[fixer] saved', p)


def mesh(sheet_file, variant):
    air, field, settings = MESH[variant]
    img = 'data:image/png;base64,' + base64.b64encode(Path(sheet_file).read_bytes()).decode()
    task = {'taskType': '3dInference', 'taskUUID': str(uuid.uuid4()), 'model': air, 'inputs': {field: [img] if field == 'images' else img}, 'settings': settings,
            'outputType': 'URL', 'outputFormat': 'GLB', 'deliveryMethod': 'async', 'includeCost': True}
    call([task])
    waited = 0.0
    while True:
        time.sleep(6); waited += 6
        done = [d for d in call([{'taskType': 'getResponse', 'taskUUID': task['taskUUID']}]) if d.get('status') in ('success', 'error')]
        if done: d = done[0]; break
        if waited > 1500: raise TimeoutError('3d task timed out')
    if d['status'] == 'error': raise RuntimeError(json.dumps(d)[:600])
    glb = fetch(d['outputs']['files'][0]['url'])
    BENCH.mkdir(parents=True, exist_ok=True)
    name = f'fixer_P4-6__{variant}.glb'; (BENCH / name).write_bytes(glb)
    log(STORE / 'source-art/Citizens3D/PROVENANCE.json', {'file': f'bench/{name}', 'model': air, 'settings': settings, 'cost': d.get('cost'), 'seconds': waited,
                                                           'bytes': len(glb), 'sha256': sha(glb), 'sheet': f'model_sheets/{Path(sheet_file).name}'})
    add_spend('3d', name, d.get('cost'))
    print('[fixer] saved', BENCH / name, len(glb))


if __name__ == '__main__':
    a = sys.argv[1:]
    arg = lambda n, d: a[a.index(n) + 1] if n in a else d
    if a[0] == 'sheet': sheet(int(arg('--n', '2')), arg('--take', '2'), arg('--ref', 'lineup'))
    elif a[0] == 'mesh': mesh(a[1], a[2])
