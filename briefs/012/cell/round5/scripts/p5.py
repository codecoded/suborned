"""Round 5: the same four people and the same cut as round 4, with the fruit taken out. Three directions of palette and material:
  a = restrained: graphite / gunmetal / charcoal, matte, one muted deep colour per person
  b = monochrome: bone and charcoal, identity by silhouette, value blocking and one small mark per person
  c = flight deck: ink-navy fabric and brushed metal, identity by the finish of the metal
People prompts carry no date (style contract concept_prompt.people / rule). Codex image tool only."""
import sys
from lib import *

ORDER = ("scout", "fixer", "tech", "enforcer")
BACK = "plain mid-grey seamless backdrop with a soft ground shadow"
FINISH = ("One shared finish for the whole team: matte digital character concept art, crisp clean edges, soft even studio light, natural skin, restrained and sober, no gloss except where metal is named, "
          f"{BACK}. No text, no logos, no watermark.")
ANIM = ("ANIMATION RULES, all strict: every garment ends at the hip or mid-thigh at the very longest; no coat, tunic, dress or skirt below mid-thigh; no cape, no tails, no trailing or loose cloth; "
        "below the hem the legs wear slim trousers, structured trousers, tights or boots; any flared shape is a stiff moulded panel that holds its shape. ")
STYLE = ("A team of serious, dangerous professionals who are very good at a hard trade, visitors from a nearby timeline that went further into the space age. Sleek, technical and modern, built from moulded shapes: "
         "sculpted stand-up collars, ring collars, porthole details, moulded yokes, gauntlets, bonded seams. Its space-age character comes from form, not from colour: smooth sculpted moulded shapes, big ring collars, round porthole discs and bezels, rigid geometric panels and clean seams, as if designed for the crew of a long voyage between planets. Understated and severe: not cute, not toy-like, not a pop band, not a fashion shoot, not a superhero or rangers team, "
         "no candy or pastel colour, no neon, no glitter, no high-gloss vinyl or patent, no white boots. Not deco, no period tailoring, not military camouflage, not tactical gear, not khaki, olive, brown or canvas. ")
LAMP = ("Team motif, small and quiet: a small round porthole lamp, a chrome ring around a lens that glows a faint pale aqua (it lights when that person acts), worn somewhere different on each person; {where}. ")
AVOID = "No gold, no brass, no cape, no mask, no helmet; nothing red, amber, orange or violet; not cream and not seafoam; no bright saturated colour anywhere."
POSE = {"scout": "weight on one leg, chin turned, eyes on something off to the side", "fixer": "upright and still, holding her case at her side, eyes level",
        "tech": "relaxed, one hand on his visor, eyes calm", "enforcer": "planted square, both hands folded on the upright rifle, eyes level"}
WHO = {
 "scout": "the Scout, a young woman with a black undercut, an earpiece and a lean athletic build",
 "fixer": "the Fixer, a woman with a long black ponytail, slim and tall",
 "tech": "the Tech, a tall lean man with short locs and bare arms",
 "enforcer": "the Enforcer, a big older man with silver hair, a close silver beard and smoked neutral-grey glasses"}
SIL = {"scout": "the pack-and-hood: a tall moulded pack above the shoulder behind a stand-up collar-hood, cropped jacket over shorts",
       "fixer": "the exclamation mark: a narrow column, a flat block at the hip, a tall ponytail through a ring collar",
       "tech": "the bell at the hip: a rigid flare above slim legs, a visor on the brow",
       "enforcer": "the door: massive moulded shoulders on a straight torso, the rifle upright like a staff"}
ITEM = {"scout": "the tall moulded pack shell with a small porthole lamp on its side and a slim periscope mast folded along it, and an earpiece",
        "fixer": "her case with a round porthole window and a coiled handset cable, a small ring on the ponytail",
        "tech": "his round porthole-style smoked visor with two flip-up disc plates, dark and calm, lit faint pale aqua only when he works, and the gauntlets with round bezels",
        "enforcer": "the long rifle with a slim inlay held upright at rest like a staff, smoked glasses, a photograph tucked in the collar"}
LAMPWHERE = {"scout": "on the side of her pack shell", "fixer": "in the ring of her collar", "tech": "on the porthole visor", "enforcer": "in the round buckle of his belt"}

DIR = {
 "a": dict(name="Restrained", hook="graphite, gunmetal and charcoal, matte, one muted deep colour each",
  pal=("Palette: a dark technical base of graphite, gunmetal and charcoal, all matte (technical knit, bonded matte fabric, matte rubber, satin gunmetal hardware); "
       "and ONE muted, deep, desaturated, dusty identifying colour per person, used as one clear block of about a quarter of the figure; the rest of the figure stays dark. "),
  colour=dict(scout="deep dusty pine-teal (#2f6e68), leaning green, never blue", fixer="deep wine (#6b3342)", tech="pale dusty ice-blue-grey (#b4c4d4), matte, never saturated", enforcer="dusty warm taupe-sand (#8c7a68)"),
  outfit=dict(
   scout="a cropped moulded matte graphite zip-front jacket with a sculpted stand-up collar-hood, chevron seams, a deep pine-teal block across the yoke and sleeves, a gunmetal ring-pull zip, charcoal fitted shorts over matte black technical tights to the ankle, a matte gunmetal buckle belt, dark matte low boots with gunmetal ring buckles, fingerless dark gloves, a tall graphite moulded pack shell",
   fixer="a sleeveless charcoal hip-length column tunic with a circular cut-out at the waist, long fitted deep-wine knit sleeves with round gunmetal ring cuffs, a sculpted gunmetal ring collar over a dark mock neck, slim graphite trousers, dark matte low boots",
   tech="a sleeveless quilted bonded matte charcoal tunic with pale ice-blue-grey quilted panels ending at mid-thigh, a rigid moulded matte bell panel at the hip, slim gunmetal leggings, dark matte boots with ring buckles, bare arms with satin gunmetal gauntlets",
   enforcer="a sculpted moulded shoulder-yoke cuirass in matte graphite with taupe-sand panels and gunmetal chevron seams over a charcoal technical-knit mock neck, hip-length, a belt with a big round gunmetal buckle, dark gauntlets with gunmetal cuffs, charcoal trousers, heavy dark matte boots with gunmetal fittings"),
  detail=dict(scout="an earpiece with a dark cord", fixer="a slim gunmetal ring on the ponytail", tech="round gunmetal bezels on the gauntlets", enforcer="a folded photograph in the collar")),
 "b": dict(name="Monochrome", hook="bone and charcoal; value blocking and one mark each",
  pal=("Palette: only two colours, a cool bone off-white (#e4e1da) and charcoal (#2b2d30), both matte, with satin gunmetal hardware; no other colour anywhere on the clothes. Identity comes from silhouette, from which parts are bone and which are charcoal, "
       "and from ONE small mark per person, a single geometric patch or band in the opposite value. "),
  colour=dict(scout="mark: a bone chevron patch on the left shoulder of the jacket", fixer="mark: a bone ring band around the collar", tech="mark: a bone disc patch on the chest", enforcer="mark: a bone vertical bar patch on the shoulder yoke"),
  outfit=dict(
   scout="a cropped moulded matte bone zip-front jacket with a sculpted stand-up collar-hood, chevron seams, a single charcoal chevron patch on the left shoulder, charcoal fitted shorts over matte charcoal technical tights to the ankle, a matte gunmetal buckle belt, charcoal matte low boots with gunmetal ring buckles, fingerless charcoal gloves, a tall bone moulded pack shell. Light jacket and pack over a dark lower half",
   fixer="a sleeveless charcoal hip-length column tunic with a circular cut-out at the waist, long fitted charcoal knit sleeves with round gunmetal ring cuffs, a sculpted charcoal collar with one bone ring band, slim matte bone trousers, charcoal matte low boots. Dark top over light trousers",
   tech="a sleeveless quilted bonded matte charcoal tunic ending at mid-thigh with a bone disc patch on the chest, a rigid moulded matte bone bell panel at the hip, slim charcoal leggings, charcoal matte boots with ring buckles, bare arms with satin gunmetal gauntlets. Dark, light, dark",
   enforcer="a sculpted moulded shoulder-yoke cuirass in matte bone with a charcoal vertical bar patch on the yoke, over a charcoal technical-knit mock neck, charcoal sleeves, hip-length, a belt with a big round gunmetal buckle, charcoal gauntlets with gunmetal cuffs, charcoal trousers, heavy charcoal matte boots with gunmetal fittings. Pale shoulders on a dark body"),
  detail=dict(scout="an earpiece with a charcoal cord", fixer="a slim gunmetal ring on the ponytail", tech="round gunmetal bezels on the gauntlets", enforcer="a folded photograph in the collar")),
 "c": dict(name="Flight deck", hook="ink fabric and brushed metal; the finish of the metal names the person",
  pal=("Palette: deep ink-navy and black matte fabric with large moulded panels of brushed metal, the serious crew wear of a long-duration space voyage; no bright colour at all. "
       "Identity comes from the finish and the place of each person's large metal areas: brushed steel on the collar and shoulders, warm pewter running down the body, mirror chrome on the skirt and chest plates, dark smoked titanium over the whole torso. Metal covers about a third of each figure. "),
  colour=dict(scout="cool brushed steel on the collar-hood, shoulders and pack", fixer="warm pewter grey, running down the length of the body", tech="bright mirror chrome", enforcer="dark smoked titanium over the whole torso"),
  outfit=dict(
   scout="a cropped moulded ink-navy zip-front jacket with a sculpted brushed-steel stand-up collar-hood and brushed-steel shoulder plates, chevron seams, black fitted shorts over matte black technical tights to the ankle, a satin steel buckle belt, black matte low boots with steel ring buckles, fingerless black gloves, a tall brushed-steel moulded pack shell",
   fixer="a sleeveless ink-navy hip-length column tunic with a circular cut-out at the waist, long fitted black knit sleeves with round pewter ring cuffs, a big sculpted warm-pewter ring collar, a long warm-pewter panel running down the outside of each slim black trouser leg from hip to ankle, black matte low boots with pewter fittings",
   tech="a sleeveless quilted bonded ink-navy tunic with a large mirror-chrome chest plate and mirror-chrome shoulder plates ending at mid-thigh, a rigid moulded mirror-chrome bell panel at the hip, slim black leggings, black matte boots with chrome ring buckles, bare arms with mirror-chrome gauntlets",
   enforcer="a sculpted moulded shoulder-yoke cuirass in dark smoked titanium with gunmetal chevron seams over a black technical-knit mock neck, hip-length, a belt with a big round titanium buckle, black gauntlets with titanium cuffs, ink-navy trousers, heavy black matte boots with titanium fittings"),
  detail=dict(scout="an earpiece with a black cord", fixer="a slim pewter ring on the ponytail", tech="round chrome bezels on the gauntlets", enforcer="a folded photograph in the collar")),
}
def prompt(d, role):
    D = DIR[d]
    return ("Character concept art, full body head to toe, one person standing, composed and alert like a professional on duty, " + BACK + ". "
            f"Redraw {WHO[role]}: keep exactly their face, hair, skin tone, age and body build and height (image 1 = their face; image 2 = their body in an earlier look: keep only the body shape, the old clothes are completely replaced). "
            f"{STYLE}{D['pal']}This person's identity: {D['colour'][role]}. Stance: {POSE[role]}. Clothes: {D['outfit'][role]}. {ANIM}Silhouette you could name from a shadow: {SIL[role]}. "
            f"Signature item, theirs alone: {ITEM[role]}. Identity detail: {D['detail'][role]}. {AVOID} " + LAMP.format(where=LAMPWHERE[role]) + FINISH)
def lineup_prompt(d):
    D = DIR[d]
    return ("Character concept art, a 4-person team lineup, full body head to toe, standing together in one row, left to right: the Scout (image 1), the Fixer (image 2), the Tech (image 3), the Enforcer (image 4). "
            "Redraw the four exactly as in their reference images: same faces, hair, builds, outfits, colours, materials and signature items, nothing changed, nothing brightened. "
            f"They are one serious team ({D['name'].lower()}: {D['hook']}) with the small porthole lamp motif. Same scale with natural height differences (the Enforcer biggest), composed, alert poses, no theatrics. " + FINISH +
            " The image must be a WIDE LANDSCAPE 3:2 picture with all four figures fully visible from head to toe, nothing cropped at the edges, with clear space between the figures and around them.")
REFNOTE = "image 1 = head crop of the A portrait (face identity only); image 2 = their body from an earlier lineup (body shape only; outfit to be replaced)."
def run_take(d, role):
    codex(f"R5-{d}-{role}", "takes", prompt(d, role), 1024, 1536, refs=[SCR / "refs" / f"head-{role}.png", SCR / "refs" / f"body-{role}.png"], refnotes=REFNOTE,
          setname="R5", hook=DIR[d]["name"], meta=dict(direction=d, role=role, outfit=DIR[d]["outfit"][role], identity=DIR[d]["colour"][role]), keep_png=True, frame=False)
def run_lineup(d):
    codex(f"R5-lineup-{d}", "lineups", lineup_prompt(d), 1536, 1024, refs=[DEL / "takes" / f"R5-{d}-{r}.png" for r in ORDER],
          refnotes="images 1 to 4 = the round 5 takes of this direction: Scout, Fixer, Tech, Enforcer (in order).", setname="R5-lineup", hook=f"{DIR[d]['name']}: {DIR[d]['hook']}",
          meta=dict(direction=d), keep_png=True, frame=False)
if __name__ == "__main__":
    for spec in sys.argv[1:]:
        if spec.startswith("L"): run_lineup(spec[1:])
        else: d, r = spec.split(":"); run_take(d, r)
