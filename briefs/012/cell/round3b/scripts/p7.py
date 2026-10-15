"""Round 3b amendment: four completely different wardrobes (own palette, materials, fashion lineage), one team motif, animation rules kept. Codex; head crop + L1 body crop as refs."""
import sys
from lib import *

FINISH = ("One shared finish for the whole team: polished matte digital character concept art, soft even studio light, crisp clean edges, natural skin, plain warm light-grey seamless backdrop with a soft ground shadow. No text, no logos, no watermark.")
ANIM = ("ANIMATION RULES, all strict: every garment ends at the hip or mid-thigh at the very longest; no coat, tunic, dress or skirt below mid-thigh; no cape, no tails, no trailing or loose cloth; "
        "below the hem the legs wear slim trousers, structured trousers or boots; any wide trousers are stiff structured panels that hold their shape. ")
PEOPLE = {
 "scout": dict(who="the Scout, a young woman with a black undercut, an earpiece and a lean athletic build", dom="graphite charcoal nylon (#3a3f45)", acc="lime (#b5c95a) reflective piping and trim",
    mats="ripstop nylon, mesh panels, rubberised zips, reflective tape", lineage="trail-running and rooftop-parkour subculture of her home timeline"),
 "fixer": dict(who="the Fixer, a woman with a long black ponytail, slim and tall", dom="deep navy (#1f2f4f)", acc="dusty rose (#b98a8f) ribbed trim and inlay",
    mats="fine merino rib-knit, soft leather, lacquered chrome", lineage="editorial minimalism and embassy-aide tailoring of her home timeline"),
 "tech": dict(who="the Tech, a tall lean man with short locs and bare arms", dom="olive-drab green (#5d5a3a)", acc="ice blue-white (#cfe6ee) webbing and stitching",
    mats="waxed canvas, leather, enamel, chrome", lineage="clockmaker and machinist workshop culture of his home timeline"),
 "enforcer": dict(who="the Enforcer, a big older man with silver hair, a close silver beard and smoked neutral-grey glasses", dom="dark cocoa brown (#4a3326)", acc="bone white (#e8e4da) inlay",
    mats="lacquered armour plates, waxed wool, heavy leather", lineage="dockside guild-marshal and duelist-guard culture of his home timeline"),
}
MOTIFS = {
 "M1": ("Visitor's pin", "a small chrome ring-with-a-bar pin (a circle with a short bar through it, the visitors' mark), worn in a different place on each: the Scout on her pack strap at the chest, the Fixer at her collar, the Tech on the edge of his visor, the Enforcer on his shoulder"),
 "M2": ("Chrome chevron", "a small chrome chevron badge (a single upward chevron) on the left upper arm or shoulder of each, made in each person's own material: sewn on the knit, a rubber patch on the nylon, riveted to the canvas, inlaid in the lacquer"),
 "M3": ("Lamp bead", "a small round chrome-rimmed lamp bead that glows soft pale aqua, worn somewhere different on each: the Scout's earpiece, the Fixer's case latch, the Tech's visor, the rifle's inlay for the Enforcer; the lamps light when that person acts"),
}
TAKES = {
 "scout": [
  ("trail-runner", "Trail runner", "a cropped hooded windshell in graphite ripstop with lime reflective piping, fitted graphite mesh-panel shorts, bare legs, chunky graphite-and-lime trail runners, a tall rectangular graphite pack frame with lime straps", "the pack-and-hood: a tall rectangular pack above the shoulder, bare legs", "the pack with a telescoping chrome periscope mast folded along its side", "an earpiece with a lime cord"),
  ("fin-glider", "Fin-pack glider", "a cropped graphite jacket and fitted shorts with lime piping, bare legs, grippy graphite sock-boots", "twin folded fins rising behind the shoulders like a closed fan", "a pack with two folded chrome-edged graphite fins above the shoulders", "a wrist compass"),
 ],
 "fixer": [
  ("knit-tunic", "Knit tunic and leather", "a slim navy rib-knit mock-neck tunic ending at mid-thigh with dusty-rose ribbed cuffs and hem band, slim navy soft-leather trousers, flat leather boots, thin leather gloves", "the exclamation mark: a narrow column, a flat block at the hip, a tall ponytail", "her silver lacquered case with a dusty-rose inlaid panel and a coiled handset cable", "one long chrome pin at the collar"),
  ("leather-jacket", "Leather jacket column", "a hip-length fitted navy leather jacket with a sculpted collar over a dusty-rose rib-knit mock-neck, slim navy wool trousers, flat boots", "a narrow column with a sharp hip-length jacket", "her silver lacquered case with a coiled handset cable", "a ring on a chain over her glove"),
  ("knit-jumpsuit", "Knit jumpsuit", "a fitted fine-knit navy jumpsuit with a dusty-rose ribbed collar band and a leather belt, slim straight legs, flat boots", "a single narrow column, a flat case block at the hip, a tall ponytail", "her silver lacquered case with a coiled handset cable", "a slim earpiece"),
 ],
 "tech": [
  ("canvas-wide", "Structured canvas trousers", "stiff structured waxed-canvas wide-leg trousers with box pleats that hold their shape, olive-drab with ice webbing, a sleeveless leather-trimmed wrap-front top, flat boots, bare arms with chrome bracers", "the bell, held stiff: a narrow torso over rigid flared trousers", "VISOR", "chrome bracers on bare forearms"),
  ("canvas-apron", "Stiff canvas apron", "a short stiff waxed-canvas apron panel hinged at the belt ending mid-thigh over slim olive-drab canvas trousers, a sleeveless henley, a leather tool bandolier with ice stitching, flat boots, bare arms with chrome bracers", "a narrow figure with a rigid hip panel", "VISOR", "a leather tool bandolier"),
  ("tool-belt", "Tool-belt hips", "a heavy wide leather tool belt with rigid chrome tool holsters and hand tools sticking out all round, a sleeveless olive-drab waxed-canvas work vest, slim canvas trousers, flat boots, bare arms", "the jingle: a ring of tool handles round the hips under a visor bar", "VISOR", "bracers and a ring of tools"),
 ],
 "enforcer": [
  ("lacquer-staff", "Lacquer pauldrons, rifle as a staff", "a hip-length waxed-wool cocoa jacket with large lacquered cocoa pauldrons inlaid with bone, a high collar, a heavy leather belt, fitted trousers, tall heavy leather boots, gloves", "the door: massive shoulders on a straight torso, the rifle upright like a staff", "the long chrome rifle with bone inlay held upright at rest like a staff", "gloved hands folded on the rifle"),
  ("lacquer-yoke", "Lacquer vest, rifle across the shoulders", "a hip-length lacquered cocoa armoured vest with bone inlay over a short-sleeve waxed-wool base, bare forearms in heavy leather bracers, fitted trousers, tall heavy leather boots", "the yoke: a long rifle across the shoulders behind the neck, both wrists draped over it, elbows wide", "the long chrome rifle with bone inlay carried across the shoulders", "smoked glasses pushed up into his hair"),
  ("ring-collar", "Ring-collar leather jacket", "a hip-length heavy leather jacket in cocoa with a thick wool ring collar and chrome epaulettes, fitted trousers, tall heavy boots, gloves", "a thick ring collar and one long diagonal rifle", "the long chrome rifle with bone inlay resting on one shoulder", "a folded photograph tucked into the collar"),
 ],
}
VISOR = "his visor: a smoked visor with two flip-up chrome lens plates, dark and calm, lit soft pale aqua only when he works"

def prompt(role, t, motif=None):
    p = PEOPLE[role]; slug, name, outfit, sil, item, detail = t; item = VISOR if item == "VISOR" else item
    s = ("Character concept art, full body head to toe, one person standing in an expressive pose with attitude, plain warm light-grey seamless backdrop with a soft ground shadow. "
         f"Redraw {p['who']}: keep exactly their face, hair, skin tone, age and body build and height (image 1 = their face; image 2 = their body in the old uniform look: keep only the body shape, the old clothes are completely replaced). "
         f"They are dressed in their own wardrobe, unlike the rest of the team: fashion lineage = {p['lineage']}; materials = {p['mats']}; palette = {p['dom']} as the dominant colour with {p['acc']} as the one accent; no white-on-charcoal. "
         f"Clothes: {outfit}. {ANIM}Silhouette you could name from a shadow: {sil}. Signature item, theirs alone: {item}. Identity detail: {detail}. "
         "No gold, no brass, no deco ornament, no cape, no mask, no helmet; nothing red, amber, orange or violet; not cream and not seafoam. ")
    if motif: s += f"Team motif, small but visible: {MOTIFS[motif][1]}. "
    return s + FINISH

def run(role, k, motif=None, tag=""):
    t = TAKES[role][k]
    codex(f"R3b-w-{role}-{k+1}-{t[0]}{tag}", "wardrobes", prompt(role, t, motif), 1024, 1536, refs=[SCR / "refs" / f"head-{role}.png", SCR / "refs" / f"body-{role}.png"],
          refnotes="image 1 = head crop of the A portrait (face identity only); image 2 = their body from the L1 lineup (body shape only; outfit to be replaced).",
          setname="R3b-w", hook=t[1], meta=dict(role=role, silhouette=t[3], signature=VISOR if t[4] == "VISOR" else t[4], palette=PEOPLE[role]["dom"], accent=PEOPLE[role]["acc"], materials=PEOPLE[role]["mats"], lineage=PEOPLE[role]["lineage"], detail=t[5], outfit=t[2], motif=motif), keep_png=True, frame=False)

# ---- Palette B (after the first palette measured too dark and too close: dominants 12.6 to 20.5 apart, all four painted near black) ----
PEOPLE_B = {
 "scout": dict(PEOPLE["scout"], dom="khaki coyote-tan (#8a7f5a)", acc="graphite (#3a3f45) panels, zips and straps"),
 "fixer": PEOPLE["fixer"],
 "tech": dict(PEOPLE["tech"], dom="dark moss green (#3f4a2a)", acc="pale aqua (#a9d9e0) webbing and stitching"),
 "enforcer": dict(PEOPLE["enforcer"], dom="chestnut brown (#6b4226)", acc="bone white (#e8e4da) inlay"),
}
def _sub(s, pairs):
    for a, b in pairs: s = s.replace(a, b)
    return s
TAKES_B = {
 "scout": [
  ("trail-runner", "Trail runner", "a cropped hooded windshell in khaki coyote-tan ripstop with graphite panels and graphite zips, fitted khaki mesh-panel shorts with graphite side panels, bare legs, chunky khaki-and-graphite trail runners, a tall rectangular khaki pack frame with graphite straps", TAKES["scout"][0][3], TAKES["scout"][0][4], "an earpiece with a graphite cord"),
  ("fin-glider", "Fin-pack glider", "a cropped khaki ripstop jacket with graphite panels and fitted khaki shorts, bare legs, grippy khaki-and-graphite sock-boots", TAKES["scout"][1][3], "a pack with two folded chrome-edged khaki fins above the shoulders", TAKES["scout"][1][5]),
 ],
 "fixer": TAKES["fixer"],
 "tech": [(t[0], t[1], _sub(t[2], [("olive-drab", "dark moss green"), ("ice webbing", "pale-aqua webbing"), ("ice stitching", "pale-aqua stitching")]), t[3], t[4], t[5]) for t in TAKES["tech"]],
 "enforcer": [(t[0], t[1], _sub(t[2], [("cocoa", "chestnut brown")]), t[3], t[4], t[5]) for t in TAKES["enforcer"]],
}

def run_b(role, k, motif=None):
    t = TAKES_B[role][k]; P = PEOPLE_B[role]
    pp = dict(PEOPLE[role]); PEOPLE[role] = P
    try:
        pr = prompt(role, t, motif)
    finally:
        PEOPLE[role] = pp
    codex(f"R3b-wB-{role}-{k+1}-{t[0]}", "wardrobes_B", pr, 1024, 1536, refs=[SCR / "refs" / f"head-{role}.png", SCR / "refs" / f"body-{role}.png"],
          refnotes="image 1 = head crop of the A portrait (face identity only); image 2 = their body from the L1 lineup (body shape only; outfit to be replaced).",
          setname="R3b-wB", hook=t[1], meta=dict(role=role, palette_set="B", silhouette=t[3], signature=VISOR if t[4] == "VISOR" else t[4], palette=P["dom"], accent=P["acc"], materials=P["mats"], lineage=P["lineage"], detail=t[5], outfit=t[2], motif=motif), keep_png=True, frame=False)


if __name__ == "__main__":
    for spec in sys.argv[1:]:
        if spec.startswith("B"):
            r, k = spec[1:].split(":"); run_b(r, int(k))
        else:
            r, k = spec.split(":"); run(r, int(k))
