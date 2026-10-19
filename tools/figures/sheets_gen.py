"""Multi-view model sheets for the cell (brief 020; Sky, 2026-10-18: "do multi view character sheet for each cell player - without props").
python3 -I tools/figures/sheets_gen.py turn <scout|fixer|tech|enforcer> [--take 1] [--variant coat_mid] [--n 1] [--size 2304x1536]
One person, four views on one sheet (front, three-quarter front, side, back; the style contract's `character` type), the person cropped from
the latest cell lineup (briefs/020/people/cell-lineup-latest.png) as the reference, no props, a plain light ground. Seedream through the
store's Runware client. Images go to the store (concepts/Cell/sheets/) and are logged with service, prompt, cost and sha256 in
concepts/Cell/PROVENANCE.json; spend adds to characters/cell_sheets/spend.json."""
import hashlib, io, json, os, sys, time, base64
from pathlib import Path
from PIL import Image

OLD = Path(os.environ.get('SUBORNED_OLD', '~/Projects/the-suborned')).expanduser() / 'workbench/Tools/Concept'
sys.path.insert(0, str(OLD))
from runware import infer   # noqa: E402

REPO = Path(__file__).resolve().parents[2]
STORE = Path(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store')).expanduser()
LINEUP = REPO / 'briefs/020/people/cell-lineup-latest.png'
OUT = STORE / 'concepts/Cell/sheets'
REFS = STORE / 'concepts/Cell/refs'
PROV = STORE / 'concepts/Cell/PROVENANCE.json'
SPEND = STORE / 'characters/cell_sheets/spend.json'
MODEL = 'bytedance:seedream@5.0-pro'
BOX = {'scout': (80, 80, 440, 1024), 'fixer': (440, 90, 705, 1024), 'tech': (660, 15, 1020, 1024), 'enforcer': (1000, 5, 1470, 1024)}
sha = lambda b: hashlib.sha256(b).hexdigest()

TURN = ("A character turnaround sheet for 3D modelling: one person, the person in the reference image, drawn four times on a single sheet in a row, "
        "at exactly the same scale and the same height, with clear space between the figures: front view, three-quarter front view, side view and back view. "
        "The same person in every view, with the same face, hair, outfit, cut, colours, materials, panels, trims and worn gear; the side and the back show the "
        "matching side and back of the same outfit, drawn as it would really be cut. Full body from head to toe, in a relaxed A-pose: the arms held about 30 degrees "
        "away from the body with a clear gap of air between each arm and the torso, open relaxed hands, legs straight and the feet shoulder-width apart. "
        "No props: nothing is held or carried. Plain light warm-grey backdrop, even soft shadowless studio light, no cast shadows, no floor, a clear margin all round. "
        "The same rendering style as the reference image, crisp edges. Original design, not resembling any real person or film character. "
        "No text, no letters, no numbers, no logos, no watermark.")

WHO = {
    'scout': ("The person: a lean young person with a short black undercut and a swept top, a small earpiece; an ivory hooded jacket with black panels, a standing collar and "
              "rolled three-quarter sleeves; a black cross-body harness with chrome buckles and a small black backpack worn on the back; black fingerless gloves; "
              "black panelled shorts with chrome bands; chrome and ivory high sneakers with black ankle cuffs. The harness and the backpack stay: they are worn gear."),
    'fixer': ("The person: a woman with long straight black hair in a high ponytail; a fitted dress of vertical panels in ivory and black with fine pinstripe panels, "
              "a high halter neckline with bare shoulders and a bare upper back with fine straps, long fitted ivory sleeves worn from the shoulder to the wrist as "
              "separate sleeves; the dress hem shortened to a clear hand's width above the kneecap, straight and clean, nothing flowing or trailing below it; two-tone ivory "
              "and black heeled ankle boots with a chrome heel accent. The briefcase of the reference is removed: nothing is held."),
    'tech': ("The person: a muscular man with dreadlocks tied up and chrome and black goggles pushed up on his forehead, a short beard; a sleeveless black wrap top with an "
             "ivory sash across the chest and thin cyan piping; a black belt with a chrome buckle; wide pleated black and ivory trousers with cyan piping down the outer "
             "legs, the hems ending at the ankle; chrome and black bracers on both forearms; black fingerless gloves; black strap sneakers."),
    'enforcer': ("The person: a big older man with short grey hair, a full grey beard and tinted sunglasses; a black and ivory panelled coat with chrome bands, a standing "
                 "collar, rolled sleeves and a round roundel patch on the shoulder; a black belt with a round chrome buckle; black gloves with chrome knuckle plates; black "
                 "trousers with ivory and chrome knee panels; white knee-high boots with black soles. The big chrome rifle of the reference is removed: nothing is held "
                 "and nothing leans on him. {coat}"),
}
COAT = {'': "The coat ends at the knee, as in the reference.", 'coat_mid': "The coat is shortened to end at mid-thigh; everything else is exactly the same."}


def uri(img):
    b = io.BytesIO(); img.save(b, 'PNG'); return 'data:image/png;base64,' + base64.b64encode(b.getvalue()).decode()


def log(row):
    old = json.loads(PROV.read_text()) if PROV.exists() else []
    PROV.write_text(json.dumps([r for r in old if r['file'] != row['file']] + [row], indent=1))


def turn(who, take, variant, n, size):
    w, h = (int(x) for x in size.split('x'))
    box = BOX[who]; REFS.mkdir(parents=True, exist_ok=True); OUT.mkdir(parents=True, exist_ok=True)
    ref = Image.open(LINEUP).convert('RGB').crop(box)
    rp = REFS / f'{who}_lineup.png'; ref.save(rp)
    prompt = f"{TURN} {WHO[who].format(coat=COAT[variant]) if who == 'enforcer' else WHO[who]}"
    res = infer(MODEL, prompt, w, h, n=n, inputs={'referenceImages': [uri(ref)]})
    SPEND.parent.mkdir(parents=True, exist_ok=True)
    for i, (png, cost, seed) in enumerate(res):
        name = f"{who}{'_' + variant if variant else ''}_turnaround_{take}_{i}.png"; (OUT / name).write_bytes(png)
        im = Image.open(OUT / name)
        log({'file': f'sheets/{name}', 'model': MODEL, 'seed': seed, 'cost': cost, 'size': f'{im.size[0]}x{im.size[1]}', 'sha256': sha(png),
             'reference': f'refs/{who}_lineup.png (crop {box} of briefs/020/people/cell-lineup-latest.png, sha256 {sha(rp.read_bytes())[:12]})', 'prompt': prompt})
        rows = json.loads(SPEND.read_text()) if SPEND.exists() else []
        rows.append({'at': time.strftime('%Y-%m-%d %H:%M'), 'what': name, 'usd': cost}); SPEND.write_text(json.dumps(rows, indent=1))
        print(f'[sheets] {name} ${cost}  total ${sum(r["usd"] or 0 for r in rows):.4f}')


if __name__ == '__main__':
    a = sys.argv[1:]
    arg = lambda k, d: a[a.index(k) + 1] if k in a else d
    if a[0] == 'turn': turn(a[1], arg('--take', '1'), arg('--variant', ''), int(arg('--n', '1')), arg('--size', '2304x1536'))
