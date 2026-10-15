"""Round 3, parts 2 (lineups) and 3 (cosplay turnaround sheets). Refs = the chosen full-body takes (Codex)."""
import sys
from lib import *
from p2 import TAKES, WHO

D2 = DEL / "2_icons"
def take(role, k):
    t = TAKES[role][k - 1]; return D2 / f"R3-2-{role}-{k}-{t[0]}.png", t
ORDER = ("scout", "fixer", "tech", "enforcer")
LINEUPS = {
 "L1": (dict(scout=1, fixer=1, tech=2, enforcer=1), "Icons I: pack-and-hood, column, bell, door with a staff"),
 "L2": (dict(scout=2, fixer=3, tech=4, enforcer=4), "Icons II: coat tail and spyglass, bell trousers, tall duster and turret, hooded coat with the rifle on the shoulder"),
 "L3": (dict(scout=3, fixer=5, tech=1, enforcer=3), "Icons III: cone and coil, keyhole collar, long apron, bare-armed tower"),
 "L4": (dict(scout=5, fixer=1, tech=3, enforcer=2), "Icons IV: twin fins, column, jingle of tools, broad T of epaulettes"),
}
FINISH = ("One shared finish, exactly the finish of the references: polished matte digital character concept art, soft even studio light, crisp clean edges, natural skin and matte technical fabric with a subtle painted texture, plain warm light-grey seamless backdrop with a soft ground shadow. No text, no logos, no watermark.")

def lineup_prompt(ks):
    return ("Character concept art, a 4-person team lineup, full body head to toe, standing together in one row, left to right: the Scout (image 1), the Fixer (image 2), the Tech (image 3), the Enforcer (image 4). "
            "Redraw the four exactly as in their reference images: same faces, hair, builds, outfits, signature items and silhouettes, nothing changed, each pushed to be an iconic, different style, one team thread (white on charcoal, chrome, matte technical fabric). "
            "Same scale with their natural height differences (the Enforcer biggest), relaxed poses with attitude, a little space between figures so each silhouette stays separate. " + FINISH)

SHEETS = [
 ("scout", 2, "the pack-mounted chrome folding spyglass arm", "the long-fronted split-hem coat with fern-green piping", "the tall chrome-buckled boots and forearm route map"),
 ("scout", 1, "the pack with its telescoping chrome periscope mast", "the cropped hooded jacket", "the chunky trainers, earpiece and fern-green cord"),
 ("fixer", 3, "the silver case with its sienna inlaid panel", "the sharp jacket with long peplum tails", "the wide-leg trousers with panel and the chrome collar pin"),
 ("fixer", 1, "the silver case with its coiled handset cable", "the long slim column dress with its chrome belt and side slit", "the flat boots and the chrome collar pin"),
 ("tech", 2, "the smoked visor with two flip-up chrome lens plates (lit pale aqua only when working)", "the sleeveless wrap-front top", "the pleated hakama-cut trousers and the chrome bracers"),
 ("tech", 1, "the smoked visor with its swing-down chrome loupe lens arm", "the long layered apron-coat", "the tool-roll bandolier and chrome bracers"),
 ("enforcer", 1, "the long chrome rifle with ink-blue inlay, held upright", "the broad-shouldered greatcoat with chrome-edged panels", "the heavy boots, gloves and belt buckle"),
 ("enforcer", 4, "the long chrome rifle resting on the shoulder", "the hooded heavy coat with its thick ring collar", "the heavy boots, gloves and belt buckle"),
]

def sheet_prompt(role, k, sig, k1, k2):
    return ("Cosplay build sheet, for someone who wants to make this costume. One plain warm light-grey sheet, landscape. The same person as in the reference image shown full body, head to toe, in a FRONT view on the left and a BACK view in the middle (invent the back of the outfit logically, same pieces, same finish), standing straight with arms slightly away from the body. "
            f"On the right, three close-up detail insets in a column, each joined by a thin leader line to its place on a figure and marked with a small numbered circle (only the digits 1, 2, 3, no other text): 1 = {sig}; 2 = {k1}; 3 = {k2}. "
            "Keep exactly the face, hair, build, outfit, colours and signature item of the reference. " + FINISH)

def run_lineup(key):
    ks, name = LINEUPS[key]
    refs = [take(r, ks[r])[0] for r in ORDER]
    codex(f"R3-2-lineup-{key}", "2_icons/lineups", lineup_prompt(ks), 1536, 1024, refs=refs,
          refnotes="images 1 to 4 = the chosen full-body takes: Scout, Fixer, Tech, Enforcer (in order).", setname="R3-2", hook=name,
          meta=dict(option=2, takes={r: f"{r}-{ks[r]}" for r in ORDER}), keep_png=True, frame=False)

def run_sheet(i):
    role, k, sig, k1, k2 = SHEETS[i]; p, t = take(role, k)
    codex(f"R3-3-sheet-{role}-{k}-{t[0]}", "3_cosplay", sheet_prompt(role, k, sig, k1, k2), 1536, 1024, refs=[p],
          refnotes="image 1 = the chosen full-body take.", setname="R3-3", hook=t[1] + ": " + t[4],
          meta=dict(option=3, role=role, take=k, callouts=dict(c1=sig, c2=k1, c3=k2)), keep_png=True, frame=False)

if __name__ == "__main__":
    for a in sys.argv[1:]:
        (run_lineup(a) if a.startswith("L") else run_sheet(int(a)))
