"""Round 3, part 1: the Tech redrawn at the others' level of craft, shown in the 4-up lineup beside the unchanged other three (Codex, A white lineup + Tech portrait as refs)."""
import sys
from lib import *

FINISH = ("One shared finish, exactly the finish of image 1: polished matte digital character concept art, soft even studio light, crisp clean edges, natural skin and matte technical fabric with a subtle painted texture, plain warm light-grey seamless backdrop with a soft ground shadow. No text, no logos, no watermark.")
TECH = [
 ("longline", "Longline vest-tunic", "a sleeveless charcoal longline tunic-vest to mid-thigh with a sculpted high collar and one bone panel down the front, over slim tapered charcoal trousers with a bone side stripe, flat technical boots; bare arms with brushed chrome bracers"),
 ("jumpsuit", "Tailored jumpsuit", "a sleeveless fitted charcoal technical jumpsuit with bone panel seams and a structured chrome-buckled tool roll at the hip, tapered legs, flat technical boots; bare arms with matte gunmetal wrist bands"),
 ("apron-coat", "Apron-coat", "a long layered bone-and-charcoal apron-coat open at the sides worn over a fitted sleeveless base, wide tapered charcoal trousers, flat technical boots, one small chrome tool held loosely in one hand; bare arms"),
 ("asym-vest", "Asymmetric vest and wide trousers", "an asymmetric tailored charcoal vest with the single structured shoulder plate of his original vest, now crafted and chrome-edged, over a fitted sleeveless base, wide pleated charcoal trousers with a bone inner panel, flat technical boots; bare arms with wrist bands"),
 ("wide-crop", "Wide cropped trousers", "wide pleated cropped charcoal trousers with a bone inner panel, a fitted sleeveless wrap-front charcoal top with a chrome closure, sleek flat technical sock-boots; bare arms with chrome bracers"),
 ("shirt-jacket", "Short-sleeve shirt-jacket", "a short-sleeve charcoal shirt-jacket with a sculpted collar, bone cuffs and a chrome zip, tailored tapered charcoal trousers, flat technical boots; forearms bare with chrome wrist bands"),
 ("harness", "Harness over bodysuit", "a fitted sleeveless charcoal bodysuit under a bone technical harness with matte straps and chrome loops carrying a small set of tools, tailored charcoal shorts over slim leggings, flat technical boots; bare arms"),
 ("duster", "Sleeveless duster", "a long open sleeveless duster coat to the calf in matte charcoal with a bone lining, over a fitted sleeveless top and slim trousers, flat technical boots; bare arms with chrome bracers"),
]

def prompt(desc):
    return ("Edit of the team lineup in image 1 (left to right: Scout, Fixer, Tech, Enforcer). Keep the Scout, the Fixer and the Enforcer exactly as drawn: same faces, hair, outfits, poses, positions, scale and backdrop, unchanged. "
            "Redraw only the third figure, the Tech: keep exactly his face, short locs, skin tone, tall lean build and relaxed stance (image 2 is his portrait for identity), and keep his bare arms. "
            f"His clothes are replaced with crafted, tailored technical clothing at the same level of craft as the other three, in the same white-on-charcoal language (charcoal and bone-white panels, brushed chrome, matte fabric): {desc}. "
            "His visor stays a dark smoked visor, unlit and calm (it is lit only when he works), worn across his eyes as in image 1; no glowing strips, no neon, no cyan, no gold. " + FINISH)

if __name__ == "__main__":
    for k in map(int, sys.argv[1:]):
        slug, name, desc = TECH[k]
        codex(f"R3-1-tech-{k+1}-{slug}", "1_tech_in_line", prompt(desc), 1536, 1024, refs=[LINEUP, PORT["tech"]],
              refnotes="image 1 = the A white-on-charcoal team lineup (Scout, Fixer, Tech, Enforcer); image 2 = the Tech's portrait (face identity).",
              setname="R3-1", hook=name, meta=dict(option=1, outfit=name, tech_index=k + 1), keep_png=True, frame=False)
