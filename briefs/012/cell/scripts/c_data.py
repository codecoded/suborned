"""Set C: team lineups (Qwen Turbo), each a coherent team: premise + four people + one shared colour language."""
from b_data import LINEAGE, FIN, TEAL, SIEN, BONE, SLATE
BLEND = "bone and dark seafoam-teal with a soft coral lining flash"

TEAMS = [
 dict(id="harbour-pilots", name="Harbour Pilots", accent=SLATE, premise="a crew who used to guide ships through a busy harbour; weathered, practical, unhurried, at home in wind and weather",
  m=["a lean woman in her 40s, Korean, short grey-streaked hair, a folding measuring rule in her hand and a slim pack frame",
     "a calm man in his 60s, Nigerian (Igbo), close white hair and beard, a coiled handset clipped at his belt and a slim earpiece cord",
     "a stocky bald man in his 60s, Welsh, big white moustache, a long listening rod held like a stick",
     "a big man in his 60s, Croatian, grey buzz cut, flattened nose, a battered long case in one hand"],
  hook="Four people who have spent their lives reading weather, water and each other."),
 dict(id="night-shift", name="Night Shift", accent=BONE, premise="four night-workers who keep a city running while it sleeps; tired-cool, dry humour, close without saying so",
  m=["a wiry young man of 19, Filipino, bleached buzz-cut top, a cocky grin, a hand mirror on a wrist strap",
     "a petite woman of 22, Indonesian, short bob, a headset around her neck, head cocked as if listening",
     "a tall pale woman in her 40s, Finnish, cropped platinum hair, a deadpan face, plain round goggles pushed up on her forehead",
     "a huge soft-faced man in his 40s, Tongan, short hair, a tiny tool held delicately in a huge hand"],
  hook="The people nobody notices at 3am, who notice everything."),
 dict(id="loupe-and-ledger", name="Loupe and Ledger", accent=TEAL, premise="a scholarly team of clerks and craftspeople; precise, inky, a little shabby-elegant",
  m=["a slight non-binary person in their 20s, South Asian, soft cropped dark hair, a small index-card case at the hip",
     "a slim non-binary person in their 40s, Japanese, sharp chin-length black bob, a tiny strapped notebook in hand",
     "a small woman in her 20s, Chinese, round face, a jeweller's loupe pushed up on her forehead",
     "a calm powerfully built man in his 50s, Punjabi Sikh, a fitted matte turban, full grey beard, a plain steel bracelet"],
  hook="Records, repairs and patience; the quietest crew in the city."),
 dict(id="found-family", name="Found Family", accent=SIEN, premise="a mixed-age crew who feel like a family that chose each other; warm, bickering, protective",
  m=["a strong woman of 25, Samoan, dark hair in a high bun, a stopwatch on a cord, an impatient grin",
     "a solid woman in her 50s, Brazilian, short grey curls, warm laugh lines, a battered thermos in hand",
     "a tall lean man in his 30s, Kenyan, shaved head, a generous grin, a screwdriver tucked behind his ear",
     "a stocky woman in her 60s, Mexican, long silver braid, strong forearms, a knowing grin"],
  hook="Mother hen, show-off, tinkerer, and the one who carries everything."),
 dict(id="old-guard", name="Old Guard", accent=SLATE, premise="four retired professionals pulled back in for one last job; dignified, dry, formidable",
  m=["an upright man in his 70s, Ethiopian, silver hair, long face, a rolled map tube across his back",
     "a small stooped man in his 70s, Armenian, thick white eyebrows, big nose, a measuring tape draped round his neck",
     "a man in his 50s, Cuban, grey afro, close beard, a stethoscope around his neck",
     "a tall stooped man in his 70s, Sri Lankan, white hair, deep-set fierce eyes, a heavy cane with a chrome cap"],
  hook="Nobody under seventy, and nobody who has ever lost an argument."),
 dict(id="young-ones", name="The Young Ones", accent=BONE, premise="four under-30s, brash, quick, a little reckless, still figuring out who they are",
  m=["a short compact woman in her 20s, Mexican, a scar through one eyebrow, hair shaved at the sides, a guarded stare",
     "a petite woman in her 20s, Moroccan, hair in a high ponytail, small hoop earrings, mid-wink",
     "a slim young woman of 19, Pakistani, a fitted matte head wrap, scowling then not quite smiling",
     "a tall broad young man in his 20s, Nigerian (Yoruba), shaved head, a shy apologetic smile, hand wraps round his wrists"],
  hook="Loud, quick and scared, in that order."),
 dict(id="stage-crew", name="Stage Crew", accent=TEAL, premise="backstage people who never take the bow; fast, quiet, trusted with everything that matters",
  m=["a slender woman in her late 20s, Ghanaian, short natural hair, dreamy, a slim notebook tucked in her sleeve",
     "a tall elegant woman in her 40s, Kazakh, high cheekbones, hair in a low knot, a small chrome card case in a gloved hand",
     "a slim young man of 24, Haitian, close-cropped hair, long fingers, a roll of slim tools on his forearm",
     "a very tall broad-shouldered woman in her 30s, Tongan, long dark hair tied back, a smooth round stone on a cord at her neck"],
  hook="They run the show from the wings."),
 dict(id="seafoam-and-bone", name="Seafoam and Bone (blend)", accent=BLEND, premise="a cohesive crew in one tailored palette (bone with dark seafoam-teal and a soft coral lining flash), tall long-line coats cut sharper than any local tailoring: visitors, not locals",
  m=["a lean man in his 30s, Lebanese, stubble, easy grin, a framing card between two fingers",
     "a polished woman in her 30s, Lebanese, dark wavy shoulder-length hair, a slim chrome briefcase at her side",
     "a brisk woman in her 40s, Egyptian, hair in a low bun, a loupe on a cord and a roll of fine tools at her hip",
     "a big man in his 50s, Cuban, close grey hair and neat grey beard, folded plain gloves in one hand"],
  hook="The 008 sheet's cohesion, with faces and an outsider's edge."),
 dict(id="clinic", name="The Clinic", accent=BONE, premise="four former medical staff, clean, calm, quietly authoritative; everything fitted and spotless",
  m=["a shy person in their 20s, South Asian, round face, big watchful eyes, a small index-card case",
     "a warm woman in her 50s, Scottish, curly grey-streaked hair, round glasses, a wry raised eyebrow, a chrome flask",
     "a gentle man in his 50s, Cuban, grey afro, kind eyes, a stethoscope around his neck",
     "a big man in his 40s, Korean, cropped hair, a soft steady face, a small first-aid roll on his belt"],
  hook="They are the ones you want beside you when it goes wrong."),
 dict(id="weather-station", name="Weather Station", accent=SIEN, premise="four people from a remote station: outdoors, weathered, observant, quietly funny",
  m=["a stocky man in his 50s, Andean (Quechua), sun-lined face, compact field glasses at his chest",
     "a heavyset man in his 50s, Ukrainian, rolled sleeves, salt-and-pepper beard, a clean cloth over one shoulder",
     "a woman in her 30s, Vietnamese, long single braid, a knotted counting cord round her wrist",
     "a broad woman in her 40s, Jamaican, short locs, a burn scar on one forearm, a small chrome whistle on a cord"],
  hook="Long silences, short sentences, nobody lost."),
 dict(id="visitors", name="The Visitors", accent=SLATE, premise="four strangers who plainly do not belong here: the same cool slate and charcoal, a different cut, standing slightly apart like people far from home",
  m=["a young woman in her 20s with a black undercut and an earpiece, a tall pack on her back, alert",
     "a woman with a long black ponytail in a long slim column dress, a silver briefcase, composed",
     "a tall man with short locs, a dark smoked visor pushed up on his forehead, wrist bands, relaxed",
     "a big older man with silver hair and a close silver beard, a heavy long coat, steady"],
  hook="Four outsiders, and the city can feel it."),
 dict(id="siblings-and-strays", name="Siblings and Strays", accent=TEAL, premise="two siblings and two strays who took them in; mismatched, affectionate, protective of each other",
  m=["a lean young woman in her 20s, Korean-American, long dark hair, a determined chin, a stack of stamped cards against her chest",
     "a heavyset man in his 50s, Cuban, close grey hair, round warm face, easy smile, hands spread in welcome",
     "a young man of 21, Peruvian, soft dark curls, an open boyish face, a small chrome oil can",
     "a huge man in his 60s, Samoan, white hair in a short knot, a mild smile, a string bag of bread"],
  hook="Two blood relations, two strays, one dinner table."),
]
HEAD = ("Character concept art, a four-person team lineup, full body head to toe, the four standing together in one row, left to right: Scout, Fixer, Tech, Enforcer, "
        "same scale with natural height differences, expressive relaxed poses that show four different personalities, plain warm light-grey studio backdrop with a soft ground shadow. ")


def prompt(t):
    ms = " ".join(f"{i+1}. {r}: {d}." for i, (r, d) in enumerate(zip(("Scout", "Fixer", "Tech", "Enforcer"), t["m"])))
    return (HEAD + f"Team concept: {t['name']}, {t['premise']}. {ms} Not a uniform: four clearly different outfits with different cuts, lengths and silhouettes, sharing only one finish and one palette (charcoal and ink-black base, cool off-white panels, {t['accent']} as the single accent). "
            + LINEAGE + FIN)


# Round 2 (v1 drifted to a matching squad uniform with cargo trousers and belts, i.e. tactical costume): a silhouette recipe per role
RECIPE = ("Silhouette recipe so the four read apart at 50 px: the Scout wears a short fitted jacket with shorts or cropped trousers and a tall pack on the back; "
          "the Fixer wears one long slim line (long coat, dress or tunic to mid-calf), nothing bulky; the Tech wears a loose layered vest, smock or apron top over wider trousers, with one small tool in hand; "
          "the Enforcer is the biggest, in a broad long heavy coat with strong shoulders. Four different garments, never matching tops. "
          "No cargo trousers, no utility belts, no pouches, no combat boots, no uniform stripes, no matching jackets; flat boots or plain shoes. ")


def prompt2(t):
    ms = " ".join(f"{i+1}. {r}: {d}." for i, (r, d) in enumerate(zip(("Scout", "Fixer", "Tech", "Enforcer"), t["m"])))
    return (HEAD + f"Team concept: {t['name']}, {t['premise']}. {ms} {RECIPE}They share one finish and one palette only (charcoal and ink-black base, cool off-white panels, {t['accent']} as the single accent). "
            + LINEAGE + FIN)
