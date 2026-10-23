"""Live portrait state sheets (brief 033; Sky, 2026-10-21). python3 -I tools/figures/portraits_gen.py <scout|fixer|tech|enforcer> [--take 1]
One 3x3 sheet per cell member: nine head-and-shoulders round portraits of the same person, one per state. Reference = the front view of the
turnaround sheet (briefs/020/people/sheets). Seedream via Runware, through the shared paid helper (brief 053): add `--pass NAME --budget USD` (pass defaults to t<take>; the budget is explicit, once per pass).
Images land in paid/033-portraits/takes/ and are symlinked into briefs/033/portraits/ where the export reads them; every call is a line of paid/033-portraits/ledger.jsonl
(history before the helper: briefs/033/portraits/PROVENANCE.json, frozen, indexed in the ledger)."""
import io, os, sys, base64
from pathlib import Path
from PIL import Image
REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / 'tools/paid'))
import paid  # noqa
OUT = REPO / 'briefs/033/portraits'; JOB = '033-portraits'


def _pass(a, take):
    name = a[a.index('--pass') + 1] if '--pass' in a else f't{take}'
    return paid.Pass(JOB, name, budget=float(a[a.index('--budget') + 1]) if '--budget' in a else None, allow_raise='--raise' in a)
MODEL = 'bytedance:seedream@5.0-pro'
STATES = ["calm, idle: relaxed neutral face, a soft steady gaze at the viewer",
 "focused: watching, listening, talking; attentive narrowed eyes, head tilted a little, lips slightly parted",
 "noticed, being watched: eyes flicking sideways, tense jaw, shoulders raised, a guarded look",
 "under fire: a flinch, head ducked low, eyes squeezed half shut, shoulders hunched, teeth bared",
 "firing: gritted teeth, one eye narrowed to aim, hard set jaw, head turned slightly as if sighting a target",
 "hurt: wincing, one eye shut, a small graze and a thin trickle of blood on the cheek, a clenched jaw",
 "badly hurt: grey drawn face, sweat, a bloodied brow and lip, heavy half-open eyes, hair and collar disordered",
 "downed: eyes closed, head slumped to one side, slack mouth, pale, a bruise and a smear of blood",
 "success: a small satisfied smile, a calm bright confident look, chin a little raised"]
WHO = {
 'scout': "a lean young person with a short black undercut and a swept top, a small earpiece, an ivory hooded jacket with a black standing collar and black harness straps",
 'fixer': "a woman with long straight black hair in a high ponytail, a high halter neckline in ivory and black pinstripe panels, bare shoulders",
 'tech': "a muscular man with dreadlocks tied up, chrome and black goggles pushed up on his forehead (they stay there in every portrait), a short beard, a black wrap top with an ivory sash and thin cyan piping",
 'enforcer': "a big older man with short grey hair, a full grey beard and tinted sunglasses (the sunglasses stay on in every portrait except downed, where they have slipped down the nose and the closed eyes show), a black and ivory panelled coat with a chrome-banded standing collar"}
def prompt(who, small=False):
    """v2 (brief 033 production pass): neutral light-grey grounds and the lineup's rendering and colour (black, ivory, chrome), the head inside the ring."""
    cells = ' '.join(f"Portrait {i+1}, {s}." for i, s in enumerate(STATES))
    tail = " The hair (tied-up dreadlocks) and goggles are the tallest part: draw the whole head including the dreadlocks and goggles at only about half of the ring's height, centred low in the ring, with a wide empty margin of ground above the hair." if small else ""
    return (tail and "" or "") + ("A character expression sheet: a three by three grid of nine round portrait medallions of one person, the person in the reference image, "
     "in reading order, each head and shoulders, facing the viewer front on, the same framing, size and head position in every medallion, each on a plain light neutral grey round ground with a thin dark grey ring, with clear space between. "
     "Every head, hair, headgear and shoulder is drawn small enough to sit fully inside its ring with a clear margin of ground all round: nothing crosses or touches the ring, the head about two thirds of the ring's height, even in the ducked, slumped and tilted portraits, which tilt by at most twenty degrees. "
     "The same person in every portrait, with exactly the same face, hair, skin tone, outfit and trims as the reference; only the expression and posture change, and the changes are bold and readable at a small size. "
     "The skin tone and complexion stay exactly the same as the reference in every portrait, even the hurt ones; pallor is shown only by a slight ashen cast, never by a lighter skin colour. "
     "No two portraits alike. No text, no letters, no numbers, no logos, no watermark. "
     "The same rendering style, colour and lighting as the reference image: crisp edges, material realism, cool neutral studio light, true neutral colour with no sepia, no yellow or brown cast on the whites, black and ivory white and chrome kept clean; plain light neutral grey sheet backdrop. "
     "Original design, not resembling any real person or film character. The person: " + WHO[who] + ". " + cells + tail)
if __name__ == '__main__' and '--downed' not in sys.argv:
    a = sys.argv[1:]; who = a[0]; take = a[a.index('--take') + 1] if '--take' in a else '1'
    ref = Image.open(OUT / f'refs/{who}_front.png'); b = io.BytesIO(); ref.save(b, 'PNG')
    p = prompt(who, "--small" in a)
    paid_pass = _pass(a, take); name = f'{who}_states_t{take}.png'
    paid_pass.image(MODEL, p, 2048, 2048, name=name, est=0.12, subdir='takes', inputs={'referenceImages': ['data:image/png;base64,' + base64.b64encode(b.getvalue()).decode()]},
                    refs_log=[f'refs/{who}_front.png (crop 0,0,700,720 of briefs/020/people/sheets/{who}.png)'], link=OUT / name)


# ---- downed options (brief 033, Sky 2026-10-21: "not into the downed ones"): python3 -I tools/figures/portraits_gen.py <agent> --downed [--take 5]
DOWNED = ["(a) eyes closed and perfectly still, the head upright and level, a calm blank face, no blood and no marks, the whole portrait desaturated to nearly grey",
 "(b) slumped but clean: the head dropped a little forward and to one side, eyes half shut, mouth slightly open, no blood and no marks, the whole portrait given a cool blue-grey tint",
 "(c) a calm out-cold sleep: eyes closed, the head resting a little to one side, relaxed face, no blood, and one single thin diagonal band of cool pale light falling across the eyes and cheek like light through a slatted gap, the rest of the face in soft shade, readable at a small size"]
def downed_prompt(who):
    return ("A character expression sheet: a row of three round portrait medallions of one person, the person in the reference image, side by side, each head and shoulders, the same framing, size and head position in every medallion, each on a plain light neutral grey round ground with a thin dark grey ring, with clear space between. "
     "Every head, hair, headgear and shoulder is drawn small enough to sit fully inside its ring with a clear margin of ground all round, the head about half of the ring's height: nothing crosses or touches the ring. "
     "The same person in every portrait, with exactly the same face, hair, skin tone, outfit and trims as the reference; the three portraits are three different ways to show the person knocked unconscious. "
     "No text, no letters, no numbers, no logos, no watermark, no letter labels. "
     "The same rendering style, colour and lighting as the reference image: crisp edges, material realism, cool neutral studio light, black and ivory white and chrome kept clean; plain light neutral grey sheet backdrop. "
     "Original design, not resembling any real person or film character. The person: " + WHO[who] + ". "
     + ' '.join(f"Portrait {i+1}, {d}." for i, d in enumerate(DOWNED)))
if __name__ == '__main__' and '--downed' in sys.argv:
    a = sys.argv[1:]; who = a[0]; take = a[a.index('--take') + 1] if '--take' in a else '5'
    ref = Image.open(OUT / f'refs/{who}_front.png'); b = io.BytesIO(); ref.save(b, 'PNG'); p = downed_prompt(who)
    paid_pass = _pass(a, take); name = f'{who}_downed_options_t{take}.png'
    paid_pass.image(MODEL, p, 2048, 704, name=name, est=0.12, subdir='takes', inputs={'referenceImages': ['data:image/png;base64,' + base64.b64encode(b.getvalue()).decode()]},
                    refs_log=[f'refs/{who}_front.png'], link=OUT / name)
