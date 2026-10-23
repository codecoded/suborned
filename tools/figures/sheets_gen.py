"""Multi-view model sheets for the cell (brief 020; Sky, 2026-10-18: "do multi view character sheet for each cell player - without props").
python3 -I tools/figures/sheets_gen.py turn <scout|fixer|tech|enforcer> --pass NAME [--budget USD] [--take 1] [--variant coat_mid] [--n 1] [--size 2304x1536]
  fixer variants (new outfits; Sky 2026-10-23): suit · coat · jumpsuit · layers
One person, four views on one sheet (front, three-quarter front, side, back; the style contract's `character` type), the person cropped from
the latest cell lineup (briefs/020/people/cell-lineup-latest.png) as the reference, no props, a plain light ground. Seedream through Runware and the
shared paid helper (brief 053): the budget is explicit (--budget USD the first time a pass is used; raising it needs --raise and Sky's OK). Sheets are
saved in paid/020-people-sheets/sheets/ (ledger.jsonl there has the call, prompt, cost, sha256) and symlinked into the store's concepts/Cell/sheets/ as before;
the committed copy in briefs/020/people/sheets/ is still made by hand from there."""
import hashlib, io, os, sys, time, base64
from pathlib import Path
from PIL import Image

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / 'tools/paid'))
import paid  # noqa: E402
JOB = '020-people-sheets'
STORE = Path(os.environ.get('SUBORNED_STORE', '~/Projects/the-suborned-store')).expanduser()
LINEUP = REPO / 'briefs/020/people/cell-lineup-latest.png'
OUT = STORE / 'concepts/Cell/sheets'
REFS = STORE / 'concepts/Cell/refs'
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
# Fixer outfit directions (Sky, 2026-10-23, on the turnaround's short halter dress: "not into this dress style"). The reference gives only her face and hair.
FIXER_NEW = ("The person: the same woman as the reference image, with the same face and the same long straight black hair in a high ponytail, but dressed in a NEW "
             "outfit, described next; ignore the clothes and shoes in the reference completely. The outfit belongs to a visitor from a nearby timeline: sharp, "
             "modern and precise, ivory and black panels with thin chrome details, no retro or period tailoring, no dress. ")
FIXER = {
    # the design she already had (Sky, 2026-10-23: "she had an original design from before"): the lineup's dress, which the first sheet shortened and straightened
    'original': None,
    'original_short': None,   # Sky, 2026-10-23: "just shorten her dress in it" (an edit of the picked original sheet, take 1_1)
    'suit': "A sharp tailored trouser suit: a cropped ivory jacket with an asymmetric black front panel, a high standing collar and long fitted sleeves; a black fitted top under it; high-waisted wide-leg black trousers with a thin ivory stripe down the outer seam; black ankle boots with a low block heel and a chrome toe cap.",
    'coat': "A long fitted ivory coat to mid-calf with one black vertical panel down the front, a high standing collar, long sleeves and a slim black belt with a chrome buckle, worn open at the hem over slim black trousers and black low-heeled ankle boots.",
    'jumpsuit': "A structured long-sleeved jumpsuit in black with ivory panels at the shoulders and down the sides, a high collar, a slim chrome-buckled belt at the waist and wide straight legs to the ankle; black low-heeled ankle boots.",
    'layers': "A long-sleeved black high-neck fitted top under a cropped structured ivory waistcoat with chrome fastenings; slim black trousers under a long open ivory panel skirt split at the front, falling to the calf; black flat ankle boots with chrome heel caps.",
}
COAT = {'': "The coat ends at the knee, as in the reference.", 'coat_mid': "The coat is shortened to end at mid-thigh; everything else is exactly the same."}


def uri(img):
    b = io.BytesIO(); img.save(b, 'PNG'); return 'data:image/png;base64,' + base64.b64encode(b.getvalue()).decode()


def turn(who, take, variant, n, size, paid_pass):
    w, h = (int(x) for x in size.split('x'))
    box = BOX[who]; REFS.mkdir(parents=True, exist_ok=True)
    if who == 'fixer' and variant == 'original_short':
        src = paid.job_dir(JOB) / 'sheets/fixer_original_turnaround_1_1.png'
        ref = Image.open(src).convert('RGB'); rp = REFS / 'fixer_original_turnaround_1_1.png'; ref.save(rp); box = 'whole sheet'
    else:
        ref = Image.open(LINEUP).convert('RGB').crop(box)
        rp = REFS / f'{who}_lineup.png'; ref.save(rp)
    if who == 'fixer' and variant == 'original_short':
        prompt = ("Edit this character turnaround sheet with exactly one change: the dress is shorter. In all four views, raise the dress's hem so that its "
                  "longest point now ends at the knee and its angled hem rises to the lower thigh on the short side; keep the same asymmetric diagonal wrap, "
                  "the front slit with the black pleated pinstripe underpanel showing, and every panel, band and strap. Everything else stays exactly the same: "
                  "the same woman, face, hair, sleeves, boots, pose, scale, layout, backdrop, light and rendering style. "
                  "No text, no letters, no numbers, no logos, no watermark.")
        reflog = f'refs/fixer_original_turnaround_1_1.png (paid/{JOB}/sheets, sha256 {sha(rp.read_bytes())[:12]})'
    elif who == 'fixer' and variant == 'original':
        prompt = (f"{TURN} The person: a woman with long straight black hair in a high ponytail with a small knot at the crown, and small drop earrings. She wears EXACTLY the "
                  "dress of the reference image, faithfully: an asymmetric wrapped dress whose long ivory outer panels cross the body on a diagonal and fall in an uneven, "
                  "angled hem to mid-calf, open at the front in a long slit over a black finely pleated pinstripe underpanel that shows in a long vertical strip down the side "
                  "and the front; a high halter collar, bare shoulders and an open back crossed by fine straps; long fitted ivory sleeves worn as separate sleeves from the upper arm "
                  "to the wrist; ivory and black heeled ankle boots with a chrome heel. Keep the dress's length, its asymmetric wrap, the diagonal hem and the slit exactly as in the "
                  "reference: do not shorten it, do not straighten it into a pencil dress, no vertical stripes. The briefcase of the reference is removed: nothing is held.")
    elif who == 'fixer' and variant:
        prompt = f"{TURN} {FIXER_NEW}{FIXER[variant]} The briefcase of the reference is removed: nothing is held."
    else:
        prompt = f"{TURN} {WHO[who].format(coat=COAT[variant]) if who == 'enforcer' else WHO[who]}"
    if variant != 'original_short':
        reflog = f'refs/{who}_lineup.png (crop {box} of briefs/020/people/cell-lineup-latest.png, sha256 {sha(rp.read_bytes())[:12]})'
    for i in range(n):
        name = f"{who}{'_' + variant if variant else ''}_turnaround_{take}_{i}.png"
        paid_pass.image(MODEL, prompt, w, h, name=name, est=0.12, subdir='sheets', inputs={'referenceImages': [uri(ref)]}, refs_log=[reflog], link=OUT / name)


if __name__ == '__main__':
    a = sys.argv[1:]
    arg = lambda k, d: a[a.index(k) + 1] if k in a else d
    if a[0] == 'turn':
        pn = arg('--pass', None)
        if not pn: raise SystemExit('paid: --pass NAME is required (and --budget USD the first time a pass is used)')
        turn(a[1], arg('--take', '1'), arg('--variant', ''), int(arg('--n', '1')), arg('--size', '2304x1536'),
             paid.Pass(JOB, pn, budget=float(arg('--budget', 0)) or None, allow_raise='--raise' in a))
