"""Round 3, part 2: four icons. Per person: lineage, silhouette, one signature item, a personal accent (>= dE 20 from cones, guard, rivals), one identity detail. 5 takes each (Codex, A portrait + A lineup as refs)."""
import sys
from lib import *

FINISH = ("One shared finish, exactly the finish of image 2: polished matte digital character concept art, soft even studio light, crisp clean edges, natural skin and matte technical fabric with a subtle painted texture, plain warm light-grey seamless backdrop with a soft ground shadow. No text, no logos, no watermark.")
TEAM = ("Team thread kept: white on charcoal (charcoal and bone-white panels), brushed chrome, matte technical fabric, sharp fitted layers, a visitor from a nearby timeline; no gold, no brass, no deco ornament, no period tailoring; nothing red, amber, orange or violet; "
        "no mask, no helmet, no cape. ")
WHO = {
    "scout": ("the Scout, a young woman with a black undercut and an earpiece (the first figure at the left of image 2)", "#4f7a4a", "fern green"),
    "fixer": ("the Fixer, a woman with a long black ponytail (the second figure of image 2, seen turning her head)", "#74483a", "deep sienna brown"),
    "tech": ("the Tech, a tall lean man with short locs and a bare-arm relaxed stance (the third figure of image 2)", "#a9d9e0", "pale aqua"),
    "enforcer": ("the Enforcer, a big older man with silver hair and a close silver beard, smoked neutral-grey glasses pushed up into his hair (the fourth figure of image 2)", "#1f3a5f", "ink blue"),
}
TAKES = {
 "scout": [
  ("rooftop-runner", "Rooftop runner", "a short cropped hooded jacket, fitted shorts, bare legs, grippy chunky trainers", "a tall rectangular pack frame rising above the shoulder over a small hood: the pack-and-hood", "the pack, with a telescoping chrome periscope mast folded along its side", "a single earpiece on a fern-green cord"),
  ("sky-rail-lookout", "Sky-rail lookout", "a long-fronted cropped coat with a high collar and a split hem to the thigh, fitted leggings, tall flat boots", "a long diagonal coat tail against a short pack: the slash and the pack", "a pack-mounted chrome folding spyglass arm swung over her shoulder", "a small route map strapped to her forearm"),
  ("alpine-surveyor", "Alpine surveyor", "a layered hooded smock, cropped trousers with technical gaiters, sturdy flat boots", "a wide triangular hood and a big coil on the back: the cone and the coil", "the pack, carrying a coil of fine chrome line and a folding rangefinder", "a chalk-white wrist tape"),
  ("courier-rider", "Courier rider", "an asymmetric one-shoulder jacket, fitted cycling shorts, tall sock-boots", "one strong diagonal tube-sling across the back: the diagonal", "a long chrome cylinder case on a diagonal sling across the back", "an arm-band stopwatch"),
  ("fin-pack-glider", "Fin-pack glider", "a tailored fitted bodysuit with a short jacket, shorts, flat gripping boots", "twin folded fins rising behind the shoulders like a closed fan: the twin fins", "a pack with two folded chrome fins like a closed fan above the shoulders", "a wrist compass"),
 ],
 "fixer": [
  ("column-diplomat", "Column diplomat", "a long slim column dress with a high collar and sleeves, flat boots", "a narrow column with a flat block at the hip and a tall ponytail: the exclamation mark", "her silver case, with a coiled handset cable dangling from it (a case that is also a telephone)", "one long chrome pin at the collar"),
  ("long-coat", "Long-coat negotiator", "a mid-calf fitted coat with a sharp stand collar and belt, slim trousers, flat boots, thin gloves", "a long narrow coat with the case at the hip: the blade", "her silver case, its handle chrome and its panel inlaid with sienna", "gloves she takes off to shake hands"),
  ("trouser-suit", "Tailored trouser suit", "a sharply cut jacket with long peplum tails over wide-leg trousers, flat boots", "a flared peplum tail over a wide leg: the bell", "her silver case with a sienna inlaid panel", "one chrome collar pin"),
  ("dispatcher", "Dispatcher", "an asymmetric long tunic-dress over slim trousers, flat boots, a slim earpiece", "a long asymmetric hem with a looped cable at the hip: the loop", "a chrome handset on a coiled cable clipped at her hip, beside her silver case", "a slim earpiece"),
  ("high-collar", "High-collar sculptor", "a long coat with an exaggerated sculpted stand collar framing her face and one oversized lapel, slim trousers, flat boots", "a keyhole: a huge collar over a narrow column", "her silver case", "a ring on a chain over her gloves"),
 ],
 "tech": [
  ("mender-smith", "Mender-smith", "a long layered apron-coat open at the sides over a fitted sleeveless base, wide tapered trousers, flat boots; bare arms", "a visor bar and a long apron: the bar and the apron", "his visor pushed further: a wide wrap-around smoked visor with a chrome swing-down loupe lens arm (lit softly pale aqua only when working)", "a tool-roll bandolier across his chest"),
  ("hakama-mender", "Wide-trouser mender", "wide pleated cropped trousers (hakama cut), a fitted sleeveless wrap-front top, sleek flat sock-boots; bare arms with chrome bracers", "a wide A-shaped trouser flare under a narrow torso: the bell and the bar", "his visor pushed further: a smoked visor with two flip-up chrome lens plates, lit soft pale aqua only when working", "chrome bracers on his bare forearms"),
  ("harness-listener", "Harness listener", "a fitted sleeveless bodysuit under a technical harness of matte straps and chrome loops with small hand tools hanging from it, tailored shorts over slim leggings, flat boots; bare arms", "a spiky ring of tool handles round the hips plus a visor bar: the jingle", "his visor pushed further: a smoked visor with two small chrome hearing horns at its sides, lit soft pale aqua only when working", "the hearing horns: he listens to machines"),
  ("duster-turret", "Duster with turret", "a long open sleeveless duster coat to the calf with a bone lining over a fitted sleeveless top and slim trousers, flat boots; bare arms with bracers", "a long vertical duster with the visor bar and a turret at one eye: the duster and the turret", "his visor pushed further: a narrow smoked visor bar with a rotating chrome lens turret over his left eye, lit soft pale aqua only when working", "a bracer with a small chrome dial"),
  ("tool-skirt", "Tool-skirt jumpsuit", "a sleeveless fitted jumpsuit with a wide chrome-buckled tool roll hanging at the hips like a skirt of tools, flat boots; bare arms", "a narrow torso over a wide hanging tool skirt: the hourglass of tools", "his visor pushed further: a wide smoked visor with chrome side fins, lit soft pale aqua only when working", "tools clicking at his hips"),
 ],
 "enforcer": [
  ("greatcoat", "Greatcoat and rifle", "a calf-length greatcoat with broad shoulders and a high collar, heavy flat boots, gloves", "a door with a staff: wide coat, the rifle upright beside him", "the long chrome rifle held upright at rest like a staff, its barrel shroud inlaid with ink blue", "gloved hands folded on the rifle"),
  ("marshal", "Double-breasted marshal", "a double-breasted bone coat with chrome epaulettes over a charcoal base, heavy flat boots, gloves", "a broad T of epaulettes over a long coat: the T", "the long chrome rifle slung across his chest", "a single chrome whistle on a chain"),
  ("open-apron-coat", "Open vest-coat", "a long sleeveless armoured vest-coat open at the front over a fitted charcoal base, bare forearms in chrome bracers, heavy flat boots", "a tower with bare arms: shoulders, long vest, bracers", "the long chrome rifle carried across both arms", "a worn bracer on each wrist"),
  ("hooded-heavy", "Hooded heavy coat", "a long heavy coat with a thick hood worn down as a heavy collar ring, heavy flat boots, gloves", "a thick ring collar over a long coat: the collar ring", "the long chrome rifle resting on his shoulder", "a folded photograph tucked into the collar"),
  ("slash-coat", "Diagonal slash coat", "a charcoal ceremonial long coat with a single bone-white diagonal panel from shoulder to hem, heavy flat boots, gloves", "one bold diagonal white slash across a dark coat: the slash", "the long chrome rifle with a chrome stock, held at his side", "his smoked glasses pushed up into his hair"),
 ],
}

def prompt(role, t):
    who, hexc, cname = WHO[role]
    slug, lineage, outfit, sil, item, detail = t
    return ("Character concept art, full body head to toe, one person standing in an expressive pose with attitude, plain warm light-grey seamless backdrop with a soft ground shadow. "
            f"Redraw {who}: keep exactly their face, hair, skin tone, age and body build as in the references (image 1 = the portrait, image 2 = the team lineup). "
            f"New style: {lineage}. Clothes: {outfit}. Silhouette you could name from a shadow: {sil}. Signature item, theirs alone: {item}. "
            f"Personal accent colour: {cname} ({hexc}), used only as small panels, piping or inlay on a charcoal and bone-white base. Identity detail: {detail}. " + TEAM + FINISH)

if __name__ == "__main__":
    for spec in sys.argv[1:]:
        role, k = spec.split(":")
        t = TAKES[role][int(k)]
        codex(f"R3-2-{role}-{int(k)+1}-{t[0]}", "2_icons", prompt(role, t), 1024, 1536, refs=[PORT[role], LINEUP],
              refnotes="image 1 = the person's portrait (identity); image 2 = the A white-on-charcoal team lineup (finish and body).",
              setname="R3-2", hook=t[1], meta=dict(option=2, role=role, lineage=t[1], silhouette=t[3], signature=t[4], accent=WHO[role][1], accent_name=WHO[role][2], detail=t[5]), keep_png=True, frame=False)
