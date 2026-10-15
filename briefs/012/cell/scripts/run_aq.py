from lib import *
from a_prompts import *
R4 = ("scout", "fixer", "tech", "enforcer")
for acc in ACCENT:
    for role in R4:
        qwen(f"A-qwen-full-{role}-{acc}", "A", qfull(role, acc), 1024, 1536, refs=[PICK[role]], setname="A", hook=f"{role} full body, {ACCENT[acc][0]}; Qwen", meta=dict(role=role, accent=acc), est=0.0145)
    qwen(f"A-qwen-lineup-{acc}", "A", qlineup(acc), 1536, 1024, refs=[PICK[k] for k in R4], setname="A", hook=f"the four picks, {ACCENT[acc][0]}; Qwen", meta=dict(accent=acc), est=0.0145)
    for role in R4:
        qwen(f"A-qwen-portrait-{role}-{acc}", "A", qportrait(role, acc), 1024, 1280, refs=[PICK[role]], setname="A", hook=f"{role} portrait, {ACCENT[acc][0]}; Qwen", meta=dict(role=role, accent=acc), est=0.0145)
