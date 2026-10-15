"""Round 4: space age made modern. Four people, four corners of the vocabulary, white and silver base + one bold hero colour each, one team lamp. Animation rules kept."""
import sys
from lib import *

FINISH = ("One shared finish for the whole team: polished matte digital character concept art with crisp clean edges and a slight sheen on vinyl, patent and chrome, soft even studio light, natural skin, plain warm light-grey seamless backdrop with a soft ground shadow. No text, no logos, no watermark.")
ANIM = ("ANIMATION RULES, all strict: every garment ends at the hip or mid-thigh at the very longest; no coat, tunic, dress or skirt below mid-thigh; no cape, no tails, no trailing or loose cloth; "
        "below the hem the legs wear slim trousers, structured trousers, tights or boots; any flared shape is a stiff moulded panel that holds its shape. ")
STYLE = ("Space-age couture made modern: clean sharp geometry, colour blocking, moulded collars and yokes, bonded seams, technical knit, quilted bonded panels, vinyl, patent and lacquer, chrome ring pulls and round buckles, white boots, silver hardware. "
         "Bright, optimistic and clean: not military, not tactical, not workwear, not cyberpunk; no khaki, olive, brown, canvas, ripstop or cargo pockets. ")
BASE = "cool white (not cream) and brushed silver as the team base, charcoal only for undersuits and small parts"
LAMP = ("Team motif, small but visible: a small round porthole lamp, a chrome ring around a lens that glows soft pale aqua (it lights when that person acts), worn somewhere different on each person; "
        "{where}. ")
PEOPLE = {
 "scout": dict(who="the Scout, a young woman with a black undercut, an earpiece and a lean athletic build", hero="fresh green-lime (#7fd13b), a clean grass-leaning green, never yellow or olive", corner="a mod runner in the manner of 1960s space-age couture cut-outs and white go-go boots", lamp="on the side of her pack shell"),
 "fixer": dict(who="the Fixer, a woman with a long black ponytail, slim and tall", hero="cool candy pink (#f48fc0), a bluish pink, never salmon, coral or red-leaning", corner="a Cardin-style geometric column tunic with circles and a ring collar", lamp="in the ring of her collar"),
 "tech": dict(who="the Tech, a tall lean man with short locs and bare arms", hero="emerald green (#0b9a64)", corner="a quilted technician's coverall-tunic with porthole and disc hardware", lamp="on the porthole visor"),
 "enforcer": dict(who="the Enforcer, a big older man with silver hair, a close silver beard and smoked neutral-grey glasses", hero="cerulean blue (#12a0d8)", corner="a sculpted moulded shoulder-yoke cuirass over a knit, with gauntlets", lamp="in the round buckle of his belt"),
}
VISOR = "his visor: a round porthole-style smoked visor with two flip-up chrome disc plates, dark and calm, lit soft pale aqua only when he works"
TAKES = {
 "scout": [
  ("mod-runner", "Mod runner", "a cropped moulded white vinyl zip-front jacket with a sculpted stand-up collar-hood, chevron seams and a lime colour-block yoke and sleeves, a silver ring-pull zip, white patent hot-pant shorts with a round chrome buckle belt, bare legs, white low go-go ankle boots with lime soles, a tall white moulded pack shell with a round porthole window", "the pack-and-hood: a tall moulded pack above the shoulder behind a stand-up collar-hood, bare legs", "the pack with a chrome periscope mast folded along its side", "an earpiece with a lime cord"),
  ("cut-out-fins", "Cut-out jacket, fin pack", "a cropped white zip jacket with geometric circular cut-out windows showing a lime underlayer and quilted lime sleeves, white mini shorts with a chevron seam, bare legs, white ankle boots, silver ring-pull zips", "twin folded fins rising behind the shoulders like a closed fan", "a moulded white pack with two folded chrome-edged fins above the shoulders", "a wrist compass in a round silver bezel"),
  ("quilted-dish", "Quilted jacket, radar dish", "a quilted bonded-panel white cropped jacket with lime panels, a round silver disc collar and ring-pull zips, white shorts, bare legs, white boots", "a round disc above the shoulder: a folded radar dish on the pack", "the pack with a folded chrome radar dish above the shoulder and a short periscope", "a round silver bezel on the wrist"),
 ("hero-block", "Hero-block runner", "a cropped moulded vinyl zip-front jacket with a sculpted stand-up collar-hood and chevron seams, the whole jacket and the hot-pant shorts in lime with white yoke and silver ring-pull zips, a round chrome buckle belt, bare legs, white low go-go ankle boots, a tall white moulded pack shell with lime panels and a round porthole window", "the pack-and-hood: a tall moulded pack above the shoulder behind a stand-up collar-hood, bare legs", "the pack with a chrome periscope mast folded along its side", "an earpiece with a lime cord"),
 ],
 "fixer": [
  ("ring-collar-column", "Ring-collar column tunic", "a slim white bonded-knit zip-front column tunic ending at mid-thigh with a big chrome O-ring collar (an oversized ring around the neck), a large rose circle on the chest edged in silver, long fitted sleeves with chrome ring pulls, slim silver trousers, white low boots", "the exclamation mark: a narrow column, a flat block at the hip, a tall ponytail through the ring collar", "her silver case with a round porthole window and a coiled handset cable", "a chrome ring on the ponytail"),
  ("chevron-tunic", "Chevron zip tunic", "a fitted white zip-front mock-neck tunic to the hip with bold rose chevron seams across chest and sleeves and a silver zip, slim white trousers with a rose side stripe, white boots", "a narrow column with a sharp hip-length tunic", "her silver case with a round porthole window and a coiled handset cable", "one long chrome zip pull"),
  ("yoke-disc", "Yoke and disc tunic", "a sleeveless rose hip-length tunic with a moulded white yoke and a circular cut-out at the waist, long fitted white knit sleeves with round silver gauntlet cuffs, slim silver trousers, white boots", "a narrow column with a bold yoke and a circle at the waist", "her silver case with a round porthole window and a coiled handset cable", "a round silver cuff on each wrist"),
 ("hero-block", "Hero-block column", "a sleeveless rose hip-length tunic with a circular cut-out at the waist and long fitted rose sleeves with round silver ring cuffs, a sculpted white ring collar, slim white trousers, white low boots", "the exclamation mark: a narrow column of colour, a flat block at the hip, a tall ponytail", "her silver case with a round porthole window and a coiled handset cable", "a chrome ring on the ponytail"),
 ],
 "tech": [
  ("quilted-tunic", "Quilted technician tunic", "a sleeveless quilted bonded white tunic with emerald panels ending at mid-thigh, a rigid moulded hip panel, slim silver trousers, white boots, bare arms with chrome gauntlets", "a narrow figure with a rigid quilted hip panel: the quilted shield", "VISOR", "chrome gauntlets on bare forearms"),
  ("quilted-belt", "Quilted vest and tool belt", "a sleeveless quilted white zip vest over a white bodysuit with emerald panels, a wide moulded white belt with a round chrome buckle and rigid tool holders, slim silver trousers, white boots, bare arms with chrome gauntlets", "the jingle: a ring of tool handles round the hips under a visor bar", "VISOR", "a belt of tools with a round chrome buckle"),
  ("bell-shell", "Moulded bell shorts", "rigid flared moulded quilted white bell-shaped shorts with an emerald band ending mid-thigh over slim silver leggings, a sleeveless zip tunic, white boots, bare arms with chrome gauntlets", "the bell at the hip: a rigid flare above slim legs", "VISOR", "a round silver bezel on each gauntlet"),
 ("hero-block", "Hero-block quilted tunic", "a sleeveless quilted bonded emerald tunic with white panels ending at mid-thigh, rigid flared moulded emerald quilted bell shorts, slim silver leggings, white boots, bare arms with chrome gauntlets", "the bell at the hip: a rigid flare above slim legs", "VISOR", "a round silver bezel on each gauntlet"),
 ],
 "enforcer": [
  ("yoke-staff", "Yoke cuirass, rifle as a staff", "a sculpted moulded white shoulder-yoke cuirass with cerulean panels and silver chevron seams over a white technical-knit mock neck, hip-length, a belt with a big round chrome buckle, white gauntlets with silver cuffs, heavy white-and-silver boots", "the door: massive moulded shoulders on a straight torso, the rifle upright like a staff", "the long chrome rifle with a cerulean inlay held upright at rest like a staff", "gloved hands folded on the rifle"),
  ("yoke-across", "Yoke cuirass, rifle across the shoulders", "a sculpted moulded white shoulder-yoke cuirass with cerulean panels and silver chevron seams over a short-sleeve white knit, hip-length, a belt with a big round chrome buckle, white gauntlets with silver cuffs, heavy white-and-silver boots", "the yoke: a long rifle laid across the shoulders behind the neck, both wrists draped over it, elbows wide", "the long chrome rifle with a cerulean inlay carried across the shoulders", "smoked glasses pushed up into his hair"),
  ("quilted-discs", "Quilted jacket, pauldron discs", "a hip-length quilted white jacket with big moulded round cerulean pauldron discs and a silver ring-pull zip, white gauntlets, heavy white-and-silver boots", "two big round shoulder discs and one long diagonal rifle", "the long chrome rifle with a cerulean inlay resting on one shoulder", "a folded photograph tucked into the collar"),
  ("hero-block", "Hero-block cuirass, rifle as a staff", "a sculpted moulded shoulder-yoke cuirass mostly in cerulean with white and silver chevron seams over a white technical-knit mock neck, hip-length, a belt with a big round chrome buckle, cerulean gauntlets with silver cuffs, heavy white-and-silver boots", "the door: massive moulded cerulean shoulders on a straight torso, the rifle upright like a staff", "the long chrome rifle with a cerulean inlay held upright at rest like a staff", "gloved hands folded on the rifle"),
 ],
}
def tone(role, s):
    if role == "scout": return s.replace("lime", "fresh green-lime")
    if role == "fixer": return s.replace("rose", "cool candy pink")
    return s
def prompt(role, t, lamp=True):
    p = PEOPLE[role]; slug, name, outfit, sil, item, detail = t; item = VISOR if item == "VISOR" else item
    outfit, item, detail = tone(role, outfit), tone(role, item), tone(role, detail)
    s = ("Character concept art, full body head to toe, one person standing in an expressive pose with attitude, plain warm light-grey seamless backdrop with a soft ground shadow. "
         f"Redraw {p['who']}: keep exactly their face, hair, skin tone, age and body build and height (image 1 = their face; image 2 = their body in an earlier look: keep only the body shape, the old clothes are completely replaced). "
         f"{STYLE}Their own corner of it: {p['corner']}. Palette: {BASE}, and one bold hero colour, {p['hero']}, in clean saturated blocks. "
         f"Clothes: {outfit}. {ANIM}Silhouette you could name from a shadow: {sil}. Signature item, theirs alone: {item}. Identity detail: {detail}. "
         "No gold, no brass, no cape, no mask, no helmet; nothing red, amber, orange or violet; not cream and not seafoam. ")
    if lamp: s += LAMP.format(where=p["lamp"])
    return s + FINISH
def run(role, k):
    t = TAKES[role][k]
    codex(f"R4-{role}-{k+1}-{t[0]}", "takes", prompt(role, t), 1024, 1536, refs=[SCR / "refs" / f"head-{role}.png", SCR / "refs" / f"body-{role}.png"],
          refnotes="image 1 = head crop of the A portrait (face identity only); image 2 = their body from the L1 lineup (body shape only; outfit to be replaced).",
          setname="R4", hook=t[1], meta=dict(role=role, silhouette=t[3], signature=VISOR if t[4] == "VISOR" else t[4], hero=PEOPLE[role]["hero"], corner=PEOPLE[role]["corner"], detail=t[5], outfit=t[2], motif="lamp bead"), keep_png=True, frame=False)
if __name__ == "__main__":
    for spec in sys.argv[1:]:
        r, k = spec.split(":"); run(r, int(k))