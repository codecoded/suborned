"""Round 3b: L1 made animatable. Takes for Enforcer, Fixer, Tech (Codex; portrait + L1 + the round 3 take as refs). Rules: hems at the hip or mid-thigh at most;
no coats below the hip, no cape or tail, no ankle-length dress or skirt; wide trousers only as stiff structured panels (flagged); silhouette from shoulders, packs, headgear, props, bulk, legs."""
import sys
from lib import *

R3 = ROOT / "briefs/012/cell/round3"
L1 = R3 / "2_icons/lineups/R3-2-lineup-L1.png"
PREV = {"fixer": R3 / "2_icons/R3-2-fixer-1-column-diplomat.png", "tech": R3 / "2_icons/R3-2-tech-2-hakama-mender.png", "enforcer": R3 / "2_icons/R3-2-enforcer-1-greatcoat.png"}
FINISH = ("One shared finish, exactly the finish of image 2: polished matte digital character concept art, soft even studio light, crisp clean edges, natural skin and matte technical fabric with a subtle painted texture, plain warm light-grey seamless backdrop with a soft ground shadow. No text, no logos, no watermark.")
ANIM = ("ANIMATION RULES, all strict: every garment ends at the hip or mid-thigh at the very longest; no coat, tunic, dress or skirt below mid-thigh; no cape, no tails, no trailing or loose cloth; "
        "the lower legs are bare trousers or boots only; any wide trousers are stiff structured panels that hold their shape like folded sheet metal. ")
TEAM = ("Team thread kept: white on charcoal (charcoal and bone-white panels), brushed chrome, matte technical fabric, sharp fitted layers, a visitor from a nearby timeline; no gold, no brass, no deco ornament, no period tailoring; nothing red, amber, orange or violet; no mask, no helmet, no cape. ")
WHO = {
 "fixer": ("the Fixer, a woman with a long black ponytail", "second figure of image 2", "#74483a", "deep sienna brown"),
 "tech": ("the Tech, a tall lean man with short locs, bare arms and a relaxed stance", "third figure of image 2", "#a9d9e0", "pale aqua"),
 "enforcer": ("the Enforcer, a big older man with silver hair and a close silver beard, smoked neutral-grey glasses", "fourth figure of image 2", "#1f3a5f", "ink blue"),
}
VISOR = "his visor: a smoked visor with two flip-up chrome lens plates, dark and calm, lit soft pale aqua only when he works"
TAKES = {
 "enforcer": [
  ("heavy-jacket-staff", "Heavy jacket, rifle as a staff", "a hip-length heavy jacket in charcoal and bone with huge chrome-edged pauldron-style shoulders and a high collar, fitted charcoal trousers, tall heavy chrome-buckled boots, gloves", "the door: massive shoulders on a straight torso, the rifle upright like a staff", "the long chrome rifle with ink-blue inlay held upright at rest like a staff", "gloved hands folded on the rifle"),
  ("yoke", "Armoured vest, rifle across the shoulders", "a hip-length armoured vest in bone and charcoal with big shoulder plates over a fitted short-sleeve base, bare forearms in chrome bracers, fitted trousers, tall heavy boots", "the yoke: a long rifle laid across the shoulders behind the neck with both wrists draped over it, elbows wide", "the long chrome rifle with ink-blue inlay carried across the shoulders", "smoked glasses pushed up into his hair"),
  ("marshal-short", "Short marshal jacket", "a hip-length double-breasted bone jacket with big chrome epaulettes over a charcoal base, fitted trousers, tall heavy boots, gloves", "a broad T of epaulettes on a straight torso", "the long chrome rifle with ink-blue inlay slung across his chest", "a single chrome whistle on a chain"),
  ("hood-ring", "Ring-collar jacket", "a hip-length heavy jacket with a thick hood worn down as a ring collar, fitted trousers, tall heavy boots, gloves", "a thick ring collar and one long diagonal rifle", "the long chrome rifle with ink-blue inlay resting on one shoulder", "a folded photograph tucked into the collar"),
 ],
 "fixer": [
  ("jumpsuit-column", "Fitted jumpsuit column", "a slim fitted charcoal jumpsuit with a bone panel down the centre front and a sculpted high collar, slim straight legs, flat boots; the body itself is the column", "the exclamation mark: a narrow column, a flat block at the hip, a tall ponytail", "her silver case with a coiled handset cable dangling from it", "one long chrome pin at the collar"),
  ("thigh-tunic", "Thigh tunic over slim trousers", "a slim mid-thigh tunic in charcoal and bone with a short side slit and a sculpted stand collar, over slim straight trousers, flat boots", "a narrow column with a short tunic hem and a flat case at the hip", "her silver case with a coiled handset cable", "a ring on a chain over her glove"),
  ("cropped-suit", "Cropped suit", "a hip-length fitted jacket with a sharp stand collar and a single short peplum flick at the hip, over high-waisted slim trousers with a bone side panel, flat boots", "a narrow column with a sharp hip flick", "her silver case with a sienna inlaid panel and a coiled handset cable", "one chrome collar pin"),
  ("overlay", "One-shoulder overlay", "a fitted sleeveless charcoal bodysuit over slim trousers with a hip-length asymmetric bone one-shoulder overlay and a chrome belt, one long fitted sleeve, flat boots", "a narrow column with one bold diagonal across the torso", "her silver case with a coiled handset cable", "a slim earpiece"),
 ],
 "tech": [
  ("structured-wide", "Structured wide trousers", "structured wide-leg trousers built from stiffened panels with box pleats that hold their shape like folded sheet metal, a fitted sleeveless wrap-front top, flat boots; bare arms with chrome bracers", "the bell, held stiff: a narrow torso over a rigid flared trouser", "VISOR", "chrome bracers on bare forearms"),
  ("stiff-apron", "Stiff apron panel", "a short stiff apron panel hinged at the belt and ending mid-thigh, over slim tapered trousers and a fitted sleeveless top, flat boots; bare arms with chrome bracers", "a narrow figure with a rigid hip panel: the shield apron", "VISOR", "a tool-roll bandolier across his chest"),
  ("tool-belt-hips", "Tool-belt hips", "a heavy wide chrome tool belt at the hips with rigid chrome tool holsters and hand tools sticking out all round, over a fitted sleeveless bodysuit and slim trousers, flat boots; bare arms", "the jingle: a ring of tool handles round the hips under a visor bar", "VISOR", "bracers and a ring of tools at the hips"),
  ("bell-shorts", "Rigid bell shorts", "rigid flared bell-shaped shorts ending mid-thigh over slim leggings, a fitted sleeveless top, flat boots; bare arms with chrome bracers", "the bell at the hip: a rigid flare above slim legs", "VISOR", "a bracer with a small chrome dial"),
 ],
}

def prompt(role, t):
    who, pos, hexc, cname = WHO[role]; slug, name, outfit, sil, item, detail = t
    item = VISOR if item == "VISOR" else item
    return ("Character concept art, full body head to toe, one person standing in an expressive pose with attitude, plain warm light-grey seamless backdrop with a soft ground shadow. "
            f"Redraw {who} ({pos}): keep exactly their face, hair, skin tone, age and body build (image 1 = the portrait; image 2 = the team lineup; image 3 = their previous design, whose signature item, accent and attitude carry over but whose clothes are replaced). "
            f"New clothes, built for animation: {outfit}. {ANIM}Silhouette you could name from a shadow: {sil}. Signature item, theirs alone: {item}. "
            f"Personal accent colour: {cname} ({hexc}) as one clear panel or lining on a charcoal and bone-white base. Identity detail: {detail}. " + TEAM + FINISH)

def run(role, k):
    t = TAKES[role][k]
    codex(f"R3b-{role}-{k+1}-{t[0]}", "takes", prompt(role, t), 1024, 1536, refs=[PORT[role], L1, PREV[role]],
          refnotes="image 1 = the A portrait (identity); image 2 = L1, the round 3 four-icons lineup (finish, body); image 3 = the round 3 take (signature item, accent).",
          setname="R3b", hook=t[1], meta=dict(role=role, silhouette=t[3], signature=VISOR if t[4] == "VISOR" else t[4], accent=WHO[role][2], accent_name=WHO[role][3], detail=t[5], outfit=t[2]), keep_png=True, frame=False)

if __name__ == "__main__":
    for spec in sys.argv[1:]:
        r, k = spec.split(":"); run(r, int(k))
