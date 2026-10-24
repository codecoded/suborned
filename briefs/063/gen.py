# Forma, brief 063: portrait concepts (Vale, Inez, the driver), 3 options each. Runware openai:gpt-image@2 through tools/paid/paid.py.
# Usage: python3 -I gen.py <only-key ...>
import sys; sys.path.insert(0, '/Users/sky/Projects/suborned/tools/paid'); import paid
ERA = "Bright retro-future: a space age imagined as the present, built as high art, optimistic and immaculate."
PEOPLE = "People wear space-age couture as everyday clothing: tailored, chic and wearable, the imagined future pushed one step further; never period costume, never astronaut suits or fancy dress."
LAYOUT = ("A character design plate of ONE person, the same person twice on a plain warm-cream backdrop with a clear margin: on the left a large head-and-shoulders portrait "
          "(chest up, facing three-quarter toward the viewer, real face and expression); on the right a full-body front view, head to toe, relaxed A-pose with the arms a little away from the body, "
          "nothing loose or flowing below the hip, no long coat, no cape. ")
FINISH = ("Gouache and marker rendering in the tradition of 1960s fashion illustration and costume design boards, crisp confident linework, soft studio light, material realism. "
          "No text, no lettering, no logos, no watermark.")
def P(who, body): return f"{ERA} {PEOPLE} {LAYOUT}{who} {body} {FINISH}"
J = {
 'vale_A': P("Councillor Vale, a city councillor of about sixty-five, an institution figure: composed, tired authority, a man who signs orders.",
   "Silver swept-back hair, lined face, no glasses. A sleek ivory bonded-wool jacket with a tall stand-up ribbed collar, thin gold piping along the collar and cuffs, a small chrome lapel disc, over an ink-black fitted tunic and slim ink-black trousers; hem at the hip. The ivory is the dominant colour block."),
 'vale_B': P("Councillor Vale, a city councillor of about seventy, an institution figure: heavy-lidded, dignified, gently menacing courtesy.",
   "Short white hair, a close white beard, deep brown skin. An ivory tabard with a broad ink-black yoke and a wide geometric chunky collar of office in thin gold links and chrome, worn over a fitted ink-black tunic and slim trousers; hem at the hip. Ivory is the dominant colour block."),
 'vale_C': P("Councillor Vale, a city councillor of about sixty, an institution figure: precise, neat, cold intelligence.",
   "Cropped grey hair, thin round chrome-rimmed spectacles, a grey moustache. A sleek ivory single-breasted tunic-jacket with one large circular geometric cut-out at the shoulder, ink-black collar band and gold edge trim, ink-black gloves and slim ink-black trousers; hem at mid-thigh. Ivory is the dominant colour block."),
 'inez_A': P("Inez, a café waitress of about twenty-eight, nervous and in debt, secretly in love with a married door guard.",
   "Chestnut bob with straight bangs, round geometric tortoiseshell glasses, a sidelong anxious glance, one hand worrying at a small chrome locket. A short seafoam-green fitted shift dress with a white stand-up ribbed collar and white cuffs and a small white pocket apron; hem at mid-thigh; seafoam green is the one dominant colour block."),
 'inez_B': P("Inez, a café waitress of about thirty, nervous and in debt, secretly in love with a married door guard.",
   "Black hair in a high ponytail, tired eyes, biting her lip, shoulders drawn up. A coral-red glossy vinyl tabard over a fitted cream tunic and slim cream trousers, a chunky geometric chrome bangle, a small cream tray held against her chest; coral red is the one dominant colour block."),
 'inez_C': P("Inez, a café waitress of about twenty-six, nervous and in debt, secretly in love with a married door guard.",
   "Short curly dark-brown hair under a small mint pillbox cap, freckles, wide worried eyes, a smile that does not hold. A mint-green zip-front mini-tunic with a round cut-out at the chest, white tights-free slim leggings, a bubble-shaped chrome brooch; hem at mid-thigh; mint green is the one dominant colour block."),
 'driver_A': P("Official driver of a city councillor, a man of about forty-five, steady and underpaid, short of cash.",
   "Close-cropped hair, tired kind eyes, a heavy jaw, broad shoulders. A mint-green stand-collar sleeveless tunic with two big round chrome buttons over a fitted ink-black long-sleeved top, ink-black slim trousers, a chrome visor band across the forehead; mint green is the one dominant colour block."),
 'driver_B': P("Official driver of a city councillor, a man of about fifty, formal, watchful, short of cash.",
   "Receding grey hair, thin moustache, deep worry lines. A sleek ink-black fitted jacket with a stand-up collar and thin gold piping, ivory driving gloves, a slim chrome driving-visor pushed up on the brow, slim black trousers with a single gold side stripe; hem at the hip; ink black with gold is the dominant block."),
 'driver_C': P("Official driver of a city councillor, a woman of about forty, brisk, competent, short of cash.",
   "Hair in a tight low bun, strong brows, an alert glance toward the car. A deep-teal bonded-wool short jacket with a white racing-band across the chest and a stand-up ribbed collar, slim teal trousers, a small peaked chrome-badged pillbox cap; hem at the hip; deep teal is the one dominant colour block."),
}
if __name__ == '__main__':
    keys = sys.argv[1:] or list(J)
    p = paid.Pass('063-portraits', 'concepts')
    for k in keys:
        p.image('openai:gpt-image@2', J[k], 1536, 1024, name=f'{k}.png', est=0.03, subdir='concepts', note='Codex login revoked (401): Runware gpt-image@2 instead', used_in=['briefs/063-portraits.md'])
