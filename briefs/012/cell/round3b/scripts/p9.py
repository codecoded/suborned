"""Round 3b palette B: motif lineups (3), the animatable-uniform lineup for comparison, cosplay sheets for the top take per person."""
import sys
from lib import *
import p7
W = DEL / "wardrobes"; WB = DEL / "wardrobes_B"; T = DEL / "takes"
def wp(role, k):
    if role == "fixer": t = p7.TAKES["fixer"][k - 1]; return W / f"R3b-w-fixer-{k}-{t[0]}.png"
    t = p7.TAKES_B[role][k - 1]; return WB / f"R3b-wB-{role}-{k}-{t[0]}.png"
ORDER = ("scout", "fixer", "tech", "enforcer")
LINEUPS = {
 "WB1": (dict(scout=1, fixer=1, tech=2, enforcer=2), "M1", "Wardrobes with the Visitor's pin: trail runner, knit tunic, canvas apron, lacquer yoke"),
 "WB2": (dict(scout=2, fixer=2, tech=3, enforcer=1), "M2", "Wardrobes with the chrome chevron: fin glider, leather jacket, tool belt, lacquer staff"),
 "WB3": (dict(scout=1, fixer=3, tech=1, enforcer=3), "M3", "Wardrobes with the lamp bead: trail runner, knit jumpsuit, structured canvas bell (flagged), ring collar"),
}
FINISH = p7.FINISH
LAND = " The image must be a WIDE LANDSCAPE 3:2 picture with all four figures fully visible from head to toe, nothing cropped at the edges, with clear space between the figures and around them."
def lineup_prompt(key):
    ks, m, _ = LINEUPS[key]
    return ("Character concept art, a 4-person team lineup, full body head to toe, standing together in one row, left to right: the Scout (image 1), the Fixer (image 2), the Tech (image 3), the Enforcer (image 4). "
            "Redraw the four exactly as in their reference images: same faces, hair, builds, wardrobes, colours, materials and signature items, nothing changed: four people dressed very differently, who would never shop in the same place, held together only by one small shared motif. "
            f"Team motif, small but visible on each: {p7.MOTIFS[m][1]}. Same scale with natural height differences (the Enforcer biggest), relaxed poses with attitude. " + FINISH + LAND)
def run_lineup(key):
    ks, m, name = LINEUPS[key]
    codex(f"R3b-lineup-{key}", "lineups", lineup_prompt(key), 1536, 1024, refs=[wp(r, ks[r]) for r in ORDER],
          refnotes="images 1 to 4 = the chosen palette B wardrobe takes: Scout, Fixer, Tech, Enforcer (in order).", setname="R3b-lineup", hook=name,
          meta=dict(takes={r: f"{r}-{ks[r]}" for r in ORDER}, motif=m, motif_name=p7.MOTIFS[m][0], palette_set="B"), keep_png=True, frame=False)
def run_uniform():
    refs = [T / "R3-2-scout-1-rooftop-runner.png", T / "R3b-fixer-1-jumpsuit-column.png", T / "R3b-tech-2-stiff-apron.png", T / "R3b-enforcer-1-heavy-jacket-staff.png"]
    p = ("Character concept art, a 4-person team lineup, full body head to toe, standing together in one row, left to right: the Scout (image 1), the Fixer (image 2), the Tech (image 3), the Enforcer (image 4). "
         "Redraw the four exactly as in their reference images: same faces, hair, builds, outfits, white-on-charcoal colours and signature items, nothing changed. Same scale with natural height differences (the Enforcer biggest), relaxed poses with attitude. " + p7.FINISH.replace("for the whole team", "for the whole team") + LAND)
    codex("R3b-lineup-U-uniform-animatable", "lineups", p, 1536, 1024, refs=refs, refnotes="images 1 to 4 = the animation-ready white-on-charcoal takes (the uniform look, shorter hems).", setname="R3b-lineup",
          hook="The L1 uniform look made animatable (white on charcoal, hip-length hems): the comparison row", meta=dict(takes=dict(scout="R3-2-scout-1", fixer="R3b-fixer-1", tech="R3b-tech-2", enforcer="R3b-enforcer-1"), palette_set="uniform"), keep_png=True, frame=False)
SHEETS = [
 ("scout", 1, "the tall khaki pack with its chrome periscope mast and graphite straps", "the cropped hooded khaki ripstop windshell with graphite panels", "the mesh-panel khaki shorts and chunky khaki-and-graphite trail runners"),
 ("fixer", 1, "the silver lacquered case with its dusty-rose panel and coiled handset cable", "the navy rib-knit mock-neck tunic with dusty-rose ribbed cuffs and hem band", "the slim navy leather trousers, flat boots and chrome collar pin"),
 ("tech", 2, "the smoked visor with two flip-up chrome lens plates (lit pale aqua only when working)", "the stiff dark-moss waxed-canvas apron panel hinged at the belt", "the leather tool bandolier, pale-aqua stitching and chrome bracers"),
 ("enforcer", 2, "the long chrome rifle with bone inlay carried across the shoulders", "the lacquered chestnut-brown armoured vest with bone inlay and leather bracers", "the heavy leather boots and belt"),
]
def sheet_prompt(sig, k1, k2):
    return ("Cosplay build sheet, for someone who wants to make this costume. One plain warm light-grey sheet, WIDE LANDSCAPE 3:2. The same person as in the reference image shown full body, head to toe, in a FRONT view on the left and a BACK view in the middle (invent the back of the outfit logically, same pieces, same materials), standing straight with arms slightly away from the body (the rifle, if any, stays on the shoulders). "
            f"On the right, three close-up detail insets in a column, each joined by a thin leader line to its place on a figure and marked with a small numbered circle (only the digits 1, 2, 3, no other text): 1 = {sig}; 2 = {k1}; 3 = {k2}. "
            "Keep exactly the face, hair, build, wardrobe, colours, materials and signature item of the reference. " + FINISH)
def run_sheet(i):
    role, k, a, b, c = SHEETS[i]; t = (p7.TAKES if role == "fixer" else p7.TAKES_B)[role][k - 1]
    codex(f"R3b-sheetB-{role}-{k}-{t[0]}", "cosplay", sheet_prompt(a, b, c), 1536, 1024, refs=[wp(role, k)], refnotes="image 1 = the chosen wardrobe take.",
          setname="R3b-sheet", hook=t[1], meta=dict(role=role, take=k, palette_set="B", callouts=dict(c1=a, c2=b, c3=c)), keep_png=True, frame=False)
if __name__ == "__main__":
    for a in sys.argv[1:]:
        if a.startswith("WB"): run_lineup(a)
        elif a == "U": run_uniform()
        else: run_sheet(int(a))
