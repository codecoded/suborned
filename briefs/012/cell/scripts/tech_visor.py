from lib import *
from a_prompts import *
for acc in ACCENT:
    name, rule = ACCENT[acc]
    p = ("Character concept art, two full-body views of the same man side by side on one sheet, same pose, same face and body as the reference image: left labelled by state only visually, the visor dark smoked glass, unlit (idle); "
         "right the same man with the visor lit from within by a calm soft pale-white glow, as it is only when he is working. Everywhere else no glow at all: the vest seams, wrist bands, trousers and sandals are matte charcoal with no cyan, no neon. "
         f"Gold trim becomes brushed chrome; accent scheme {name}. " + FINISH)
    qwen(f"A-qwen-tech-visor-states-{acc}", "A", p, 1536, 1024, refs=[PICK["tech"]], setname="A", hook=f"Tech visor idle (unlit) and working (soft lit), {name}; Qwen", meta=dict(accent=acc), est=0.0145)
