"""Round 3b: motif lineups (3) from the wardrobe takes, and cosplay sheets for the top take per person."""
import sys
from lib import *
import p7
W = DEL / "wardrobes"
def wp(role, k):
    t = p7.TAKES[role][k - 1]; return W / f"R3b-w-{role}-{k}-{t[0]}.png"
ORDER = ("scout", "fixer", "tech", "enforcer")
LINEUPS = {
 "W1": (dict(scout=1, fixer=1, tech=2, enforcer=2), "M1", "Wardrobes I with the Visitor's pin: trail runner, knit tunic, canvas apron, lacquer yoke"),
 "W2": (dict(scout=2, fixer=2, tech=3, enforcer=1), "M2", "Wardrobes II with the chrome chevron: fin glider, leather jacket, tool belt, lacquer staff"),
 "W3": (dict(scout=1, fixer=3, tech=1, enforcer=3), "M3", "Wardrobes III with the lamp bead: trail runner, knit jumpsuit, structured canvas bell (flagged), ring collar"),
}
FINISH = p7.FINISH
def lineup_prompt(key):
    ks, m, _ = LINEUPS[key]
    return ("Character concept art, a 4-person team lineup, full body head to toe, standing together in one row, left to right: the Scout (image 1), the Fixer (image 2), the Tech (image 3), the Enforcer (image 4). "
            "Redraw the four exactly as in their reference images: same faces, hair, builds, wardrobes, colours, materials and signature items, nothing changed: four people dressed very differently, who would never shop in the same place, held together only by one small shared motif. "
            f"Team motif, small but visible on each: {p7.MOTIFS[m][1]}. Same scale with natural height differences (the Enforcer biggest), relaxed poses with attitude, a little space between figures so each silhouette stays separate. " + FINISH)
SHEETS = [
 ("scout", 1, "the tall graphite pack with its chrome periscope mast and lime straps", "the cropped hooded ripstop windshell with lime reflective piping", "the mesh-panel shorts and chunky graphite-and-lime trail runners"),
 ("fixer", 1, "the silver lacquered case with its dusty-rose panel and coiled handset cable", "the navy rib-knit mock-neck tunic with dusty-rose ribbed cuffs and hem band", "the slim navy leather trousers, flat boots and chrome collar pin"),
 ("tech", 2, "the smoked visor with two flip-up chrome lens plates (lit pale aqua only when working)", "the stiff waxed-canvas apron panel hinged at the belt", "the leather tool bandolier, ice stitching and chrome bracers"),
 ("enforcer", 2, "the long chrome rifle with bone inlay carried across the shoulders", "the lacquered cocoa armoured vest with bone inlay and leather bracers", "the heavy leather boots and belt"),
]
def sheet_prompt(sig, k1, k2):
    return ("Cosplay build sheet, for someone who wants to make this costume. One plain warm light-grey sheet, landscape. The same person as in the reference image shown full body, head to toe, in a FRONT view on the left and a BACK view in the middle (invent the back of the outfit logically, same pieces, same materials), standing straight with arms slightly away from the body (the rifle, if any, stays on the shoulders). "
            f"On the right, three close-up detail insets in a column, each joined by a thin leader line to its place on a figure and marked with a small numbered circle (only the digits 1, 2, 3, no other text): 1 = {sig}; 2 = {k1}; 3 = {k2}. "
            "Keep exactly the face, hair, build, wardrobe, colours, materials and signature item of the reference. " + FINISH)
def run_lineup(key):
    ks, m, name = LINEUPS[key]
    codex(f"R3b-lineup-{key}", "lineups", lineup_prompt(key), 1536, 1024, refs=[wp(r, ks[r]) for r in ORDER],
          refnotes="images 1 to 4 = the chosen wardrobe takes: Scout, Fixer, Tech, Enforcer (in order).", setname="R3b-lineup", hook=name,
          meta=dict(takes={r: f"{r}-{ks[r]}" for r in ORDER}, motif=m, motif_name=p7.MOTIFS[m][0]), keep_png=True, frame=False)
def run_sheet(i):
    role, k, a, b, c = SHEETS[i]
    codex(f"R3b-sheet-{role}-{k}-{p7.TAKES[role][k-1][0]}", "cosplay", sheet_prompt(a, b, c), 1536, 1024, refs=[wp(role, k)], refnotes="image 1 = the chosen wardrobe take.",
          setname="R3b-sheet", hook=p7.TAKES[role][k - 1][1], meta=dict(role=role, take=k, callouts=dict(c1=a, c2=b, c3=c)), keep_png=True, frame=False)
if __name__ == "__main__":
    for a in sys.argv[1:]: (run_lineup(a) if a.startswith("W") else run_sheet(int(a)))
