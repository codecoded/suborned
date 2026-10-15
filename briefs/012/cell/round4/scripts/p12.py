"""Round 4: lineups (3) and cosplay sheets (top take per person)."""
import sys
from lib import *
import p11
T = DEL / "takes"
def tp(role, k): t = p11.TAKES[role][k - 1]; return T / f"R4-{role}-{k}-{t[0]}.png"
ORDER = ("scout", "fixer", "tech", "enforcer")
LINEUPS = {
 "L1": (dict(scout=1, fixer=3, tech=3, enforcer=1), "Mod runner, yoke-and-disc Fixer, moulded bell Tech, yoke cuirass with the rifle as a staff"),
 "L2": (dict(scout=2, fixer=1, tech=2, enforcer=2), "Cut-out jacket with fin pack, ring-collar column, quilted tool belt, yoke cuirass with the rifle across the shoulders"),
 "L3": (dict(scout=3, fixer=2, tech=1, enforcer=3), "Radar-dish pack, chevron tunic, quilted technician tunic, pauldron discs with the rifle on the shoulder"),
 "L4": (dict(scout=4, fixer=4, tech=4, enforcer=4), "Hero-block set: each person's bold colour as the main garment (about a third of the figure), white and silver as the thread"),
}
LAND = " The image must be a WIDE LANDSCAPE 3:2 picture with all four figures fully visible from head to toe, nothing cropped at the edges, with clear space between the figures and around them."
def lineup_prompt(key):
    return ("Character concept art, a 4-person team lineup, full body head to toe, standing together in one row, left to right: the Scout (image 1), the Fixer (image 2), the Tech (image 3), the Enforcer (image 4). "
            "Redraw the four exactly as in their reference images: same faces, hair, builds, outfits, colours and signature items, nothing changed. They share one space-age white-and-silver base and the small porthole lamp motif, and each wears one bold hero colour (lime, rose pink, emerald, cerulean). "
            "Same scale with natural height differences (the Enforcer biggest), relaxed poses with attitude. " + p11.FINISH + LAND)
SHEETS = [
 ("scout", 1, "the tall white moulded pack shell with its porthole lamp and chrome periscope mast", "the cropped moulded white vinyl jacket with the lime yoke, chevron seams and stand-up collar-hood", "the white patent shorts with round chrome buckle belt and the white go-go ankle boots with lime soles"),
 ("fixer", 3, "the silver case with its round porthole window and coiled handset cable", "the sleeveless rose tunic with moulded white yoke and circular cut-out at the waist", "the round silver gauntlet cuffs, slim silver trousers and white boots"),
 ("tech", 3, "the round porthole smoked visor with two flip-up chrome disc plates (lit pale aqua only when working)", "the rigid flared moulded quilted bell shorts with the emerald band", "the sleeveless zip tunic, chrome gauntlets with round silver bezels and white boots"),
 ("enforcer", 2, "the long chrome rifle with cerulean inlay carried across the shoulders", "the sculpted moulded white shoulder-yoke cuirass with cerulean panels and silver chevron seams", "the belt with its big round buckle lamp, white gauntlets and heavy white-and-silver boots"),
]
SHEETS += [
 ("scout", 4, "the tall white moulded pack shell with lime panels, porthole lamp and chrome periscope mast", "the cropped moulded lime vinyl jacket with white yoke, collar-hood and silver ring-pull zip", "the lime hot-pant shorts with round chrome buckle belt and white go-go ankle boots"),
 ("fixer", 4, "the silver case with its round porthole window and coiled handset cable", "the sleeveless cool-pink tunic with circular cut-out at the waist and the white ring collar", "the long pink sleeves with round silver ring cuffs, slim white trousers and white boots"),
 ("tech", 4, "the round porthole smoked visor with two flip-up chrome disc plates (lit pale aqua only when working)", "the sleeveless quilted emerald tunic with white panels and the rigid emerald quilted bell shorts", "the chrome gauntlets with round silver bezels, slim silver leggings and white boots"),
 ("enforcer", 4, "the long chrome rifle with cerulean inlay, held upright like a staff", "the sculpted moulded cerulean shoulder-yoke cuirass with white and silver chevron seams", "the cerulean gauntlets with silver cuffs, the belt with its big round buckle lamp and the heavy white-and-silver boots"),
]
def sheet_prompt(sig, k1, k2, role=None):
    if role: sig, k1, k2 = p11.tone(role, sig), p11.tone(role, k1), p11.tone(role, k2)
    return ("Cosplay build sheet, for someone who wants to make this costume. One plain warm light-grey sheet, WIDE LANDSCAPE 3:2. The same person as in the reference image shown full body, head to toe, in a FRONT view on the left and a BACK view in the middle (invent the back of the outfit logically, same pieces, same materials), standing straight with arms slightly away from the body (the rifle, if any, stays on the shoulders). "
            f"On the right, three close-up detail insets in a column, each joined by a thin leader line to its place on a figure and marked with a small numbered circle (only the digits 1, 2, 3, no other text): 1 = {sig}; 2 = {k1}; 3 = {k2}. "
            "Keep exactly the face, hair, build, outfit, colours, materials and signature item of the reference. " + p11.FINISH)
def run_lineup(key):
    ks, name = LINEUPS[key]
    codex(f"R4-lineup-{key}", "lineups", lineup_prompt(key), 1536, 1024, refs=[tp(r, ks[r]) for r in ORDER], refnotes="images 1 to 4 = the chosen round 4 takes: Scout, Fixer, Tech, Enforcer (in order).",
          setname="R4-lineup", hook=name, meta=dict(takes={r: f"{r}-{ks[r]}" for r in ORDER}, motif="lamp bead"), keep_png=True, frame=False)
def run_sheet(i):
    role, k, a, b, c = SHEETS[i]; t = p11.TAKES[role][k - 1]
    codex(f"R4-sheet-{role}-{k}-{t[0]}", "cosplay", sheet_prompt(a, b, c, role), 1536, 1024, refs=[tp(role, k)], refnotes="image 1 = the chosen take.", setname="R4-sheet", hook=t[1], meta=dict(role=role, take=k, callouts=dict(c1=a, c2=b, c3=c)), keep_png=True, frame=False)
if __name__ == "__main__":
    for a in sys.argv[1:]: (run_lineup(a) if a.startswith("L") else run_sheet(int(a)))
